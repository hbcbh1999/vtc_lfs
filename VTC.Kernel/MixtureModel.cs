using System;
using System.Linq;
using System.Threading;
using MathNet.Numerics.Distributions;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace VTC.Kernel
{
    /// <summary>
    /// 2D array of Gaussian mixture models, used to identify foreground pixels in video
    /// </summary>
    public class MoGBackground
    {
        /// <summary>
        /// 2D array of mixture models
        /// </summary>
        public readonly MixtureModel[,] MmImage;
        public Image<Bgr, float> RoiImage; 
        private int Width => MmImage.GetLength(0);
        private int Height => MmImage.GetLength(1);

        /// <summary>
        /// A mutex to track asynchronous background updates
        /// </summary>
        private readonly Mutex _updateMutex = new Mutex();

        public MoGBackground(int width, int height, Image<Bgr, float> roiImage )
        {
            RoiImage = roiImage;
            MmImage = new MixtureModel[width, height];
            for (int i = 0; i < width; i++)
                for (int j = 0; j < height; j++)
                    MmImage[i, j] = new MixtureModel();
        }

        public void TryUpdatingAsync(Image<Bgr, Byte> frame)
        {
            Task.Factory.StartNew(() => Update(frame));
        }

        public void Update(Image<Bgr, Byte> frame)
        {
            // If we're already busy updating background, just abort
            if (!_updateMutex.WaitOne(0))
                return;
            
            try
            {
                var newImageSample = frame.Convert<Bgr, float>();
                for (var i = 0; i < newImageSample.Width; i++)
                    for (var j = 0; j < newImageSample.Height; j++)
                    {
                        if (RoiImage.Data[j, i, 0] != 0)
                        {
                            var samplePoint = new int[] { frame.Data[j, i, 0], frame.Data[j, i, 1], frame.Data[j, i, 2] };
                            MmImage[i, j].TrainIncremental(samplePoint);
                        }
                    }
            }
            finally
            {
                _updateMutex.ReleaseMutex();
            }
            
        }

        /// <summary>
        /// An image representation of the MoG background model
        /// </summary>
        public Image<Bgr, float> BackgroundImage()
        {
            var background = new Image<Bgr, float>(Width, Height);
            for(var i=0; i<Width;i++)
                for (var j = 0; j < Height; j++)
                {
                    if (RoiImage.Data[j, i, 0] != 0)
                    {
                        var pixel = MmImage[i, j].SampleBackground();
                        background.Data[j, i, 0] = pixel[0];
                        background.Data[j, i, 1] = pixel[1];
                        background.Data[j, i, 2] = pixel[2];
                    }
                    
                }

            return background;
        }

        /// <summary>
        /// An image representation of MoG foreground pixels
        /// </summary>
        public Image<Gray, byte> ForegroundMask(Image<Bgr, Byte> frame)
        {
            var foreground = new Image<Gray, byte>(Width, Height);
            for (var i = 0; i < Width; i++)
                for (var j = 0; j < Height; j++)
                {
                    if (RoiImage.Data[j, i, 0] != 0)
                    {
                        var sample = new int[] { frame.Data[j, i, 0], frame.Data[j, i, 1], frame.Data[j, i, 2] };
                        foreground.Data[j, i, 0] = (byte)(MmImage[i, j].IsBackgroundSample(sample) ? 0 : byte.MaxValue);
                    }
                    
                }

            return foreground;
        }

    }

    /// <summary>
    /// A multidimensional Gaussian distribution
    /// </summary>
    public class GaussianComponent
    {
        /// <summary>
        /// Single, multidimensional Gaussian
        /// </summary>
        private readonly Normal[] _mGaussian; 
        public int Dimensionality => _mGaussian.Length;
        public double Variance => Math.Sqrt(_mGaussian.Aggregate(0.0, (d, normal) => d + Math.Pow(normal.Variance, 2)));

        /// <summary>
        /// Multidimensional Gaussian distribution: use this constructor when distribution parameters are unknown
        /// </summary>
        /// <param name="numDimensions">Dimensionality of distribution</param>
        /// <param name="defaultMean">Initialization value for distribution mean</param>
        /// <param name="defaultVariance">Initialization value for distribution variance</param>
        /// <param name="defaultWeight">Initialization value for weight</param>
        public GaussianComponent(int numDimensions, double defaultMean, double defaultVariance, double defaultWeight)
        {
            Weight = defaultWeight;
            _mGaussian = new Normal[numDimensions];
            for (var i = 0; i < numDimensions; i++)
                _mGaussian[i] = new Normal(defaultMean, defaultVariance);
        }

        /// <summary>
        /// Multidimensional Gaussian: use this constructor to create a component based on a sample
        /// </summary>
        /// <param name="sample">sample from which to extract initialization means</param>
        /// <param name="defaultVariance"></param>
        /// <param name="defaultWeight">Initialization value for weight</param>
        public GaussianComponent(double[] sample, double defaultVariance, double defaultWeight)
        {
            Weight = defaultWeight;
            _mGaussian = new Normal[sample.Length];
            for (var i = 0; i < sample.Length; i++)
                _mGaussian[i] = new Normal(sample[i], defaultVariance);
        }

        /// <summary>
        /// Percentage of observations accounted for by this component
        /// </summary>
        public double Weight;

        /// <summary>
        /// Update distribution properties assuming that this sample originated from this Gaussian
        /// </summary>
        /// <param name="sample">Pixel RGB values</param>
        /// <param name="alpha">Update rate</param>
        public void UpdateParameters(double[] sample, double alpha)
        {
            var gaussianWithSampleValues = AssociateDimensionsWithSamples(sample);
            //var p = alpha*SampleProbability(sample);
            var p = alpha; // Deviating from the Stauffer and Grimson implementation. Need to update faster.

            foreach (var gaussianWithSample in gaussianWithSampleValues)
            {
                var singleDimSample = gaussianWithSample.Item1;
                var singleDimMean = gaussianWithSample.Item2.Mean;
                var singleDimVariance = gaussianWithSample.Item2.Variance;

                gaussianWithSample.Item2.Mean = (1 - p)* singleDimMean + p * singleDimSample;
                gaussianWithSample.Item2.Variance = (1 - p)*singleDimVariance +
                                                    p*(singleDimSample - singleDimMean)*
                                                    (singleDimSample - singleDimMean);


            }

            Weight = (1-alpha)*Weight + alpha;
        }

        public void DecreaseWeight(double alpha)
        {
            Weight = (1 - alpha) * Weight;
        }

        /// <summary>
        /// Zip a multidimensional sample to the Gaussian distribution for dimension
        /// </summary>
        /// <param name="sample"></param>
        /// <returns></returns>
        private List<Tuple<double, Normal>> AssociateDimensionsWithSamples(double[] sample)
        {
            var sampleList = sample.ToList();
            return sampleList.Zip(_mGaussian, (d, normal) => new Tuple<double, Normal>(d, normal)).ToList();
        }

        /// <summary>
        /// Check if a sample is within some mahalanobis distance from this distribution
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="stdDevThreshold"></param>
        /// <returns></returns>
        public bool SampleMatch(double[] sample, double stdDevThreshold = 2.5)
        {
           var mahalanobis = Math.Sqrt(AssociateDimensionsWithSamples(sample).Aggregate(0.0, (p, tuple) => p + (tuple.Item1 - tuple.Item2.Mean)* (tuple.Item1 - tuple.Item2.Mean)/tuple.Item2.StdDev ));
            if (mahalanobis > stdDevThreshold)
                return false;

            return true;
        }

        public int[] Sample()
        {
            return _mGaussian.Select(x => (int) x.Mean).ToArray();
        }
    }

    /// <summary>
    /// A distribution composed of K Gaussians. Used to model pixel-process RGB values in order to perform background subtraction.
    /// </summary>
    public class MixtureModel
    {
        /// <summary>
        /// List of gaussian components present in the mixture
        /// </summary>
        private readonly List<GaussianComponent> _components = new List<GaussianComponent>();

        /// <summary>
        /// Update rate for learning
        /// </summary>
        private const double Alpha = 0.05;

        /// <summary>
        /// Minimum proportion of samples that should be accounted for by the background model
        /// </summary>
        private const double T = 0.8;

        /// <summary>
        /// Initial variance for components on first observation
        /// </summary>
        private const double DefaultVariance = 200;

        /// <summary>
        /// Initial weight for components on first observation
        /// </summary>
        private const double DefaultWeight = 0.01;

        private const int MaxComponents = 3;

        /// <summary>
        /// Online Mixture-of-Gaussians incremental calculation,  
        /// taken from Stauffer and Grimson: "Adaptive background mixture models for real-time tracking"
        /// </summary>
        /// <param name="sample">Single multidimensional sample</param>
        public void TrainIncremental(int[] sample)
        {
            var dSample = sample.Select(Convert.ToDouble).ToArray();
            if (MatchesAny(sample))
            {
                var match = NearestMatchOrNull(sample);
                foreach(var c in _components)
                    if (c != match)
                        c.DecreaseWeight(Alpha);

                match.UpdateParameters(dSample, Alpha);
            }
            else
                AddNewComponent(sample);

            RenormalizeWeights();
        }

        private void RenormalizeWeights()
        {
            var totalWeight = _components.Aggregate(0.0, (w, g) => w + g.Weight);
            foreach (var c in _components)
                c.Weight = c.Weight/totalWeight;
        }

        /// <summary>
        /// Determines whether a sample is likely to have originated from one of the background components
        /// </summary>
        /// <param name="sample"></param>
        /// <returns></returns>
        public bool IsBackgroundSample(int[] sample)
        {
            var dSample = sample.Select(Convert.ToDouble).ToArray();
            var bgComponents = BackgroundComponents();
            return bgComponents.Any(c => c.SampleMatch(dSample));
        }

        /// <summary>
        /// Determines whether a sample is likely to have originated from any of the model components
        /// </summary>
        /// <param name="sample"></param>
        /// <returns></returns>
        private bool MatchesAny(int[] sample)
        {
            var dSample = sample.Select(Convert.ToDouble).ToArray();
            return _components.Any(c => c.SampleMatch(dSample));
        }

        private GaussianComponent NearestMatchOrNull(int[] sample)
        {
            var dSample = sample.Select(Convert.ToDouble).ToArray();
            for(var i=0; i < _components.Count; i++)
                if (_components.ElementAt(i).SampleMatch(dSample))
                    return _components.ElementAt(i);

            return null;
        }

        /// <summary>
        /// Model components heuristically determined to represent the background. 
        /// </summary>
        /// <returns>Components heuristically determined to represent the background</returns>
        private List<GaussianComponent> BackgroundComponents()
        {
            SortComponents();
            var bComponents = new List<GaussianComponent>();
            var proportion = 0.0;
            for (var i = 0; i < _components.Count; i++)
            {
                proportion += _components.ElementAt(i).Weight;
                bComponents.Add(_components.ElementAt(i));
                if (proportion >= T)
                    break;
            }

            return bComponents;
        }

        /// <summary>
        /// Model components heuristically sorted by likelihood
        /// </summary>
        /// <returns>Components heuristically sorted by likelihood</returns>
        public void SortComponents()
        {
            if (_components.Count == 0)
                return;

            if (_components.Count == 1)
                return;

            _components.Sort(
                 delegate (GaussianComponent a, GaussianComponent b)
                 {
                     if (a.Weight / a.Variance == b.Weight / b.Variance)
                         return 0;

                     if (a.Weight / a.Variance > b.Weight / b.Variance)
                         return 1;

                     return -1;
                 });
            _components.Reverse(); //Sorted in order of decreasing background-ness
        }

        /// <summary>
        /// Create new multidimensional Gaussian distribution component based on a sample, add to components list
        /// </summary>
        /// <param name="sample"></param>
        private void AddNewComponent(int[] sample)
        {
            SortComponents();
            if(_components.Count >= MaxComponents)
                _components.RemoveAt(_components.Count-1);
            var dSample = sample.Select(Convert.ToDouble).ToArray();
            var newComponent = new GaussianComponent(dSample, DefaultVariance, DefaultWeight);
            _components.Add(newComponent);
        }

        public int[] SampleBackground(int expectedDimensionality=3)
        {
            var bgComponents = BackgroundComponents();
            if (bgComponents != null && bgComponents.Count > 0)
            {
                var dominantBackground = bgComponents.First();
                return dominantBackground.Sample();
            }
            
            return new int[expectedDimensionality];
        }
    }
}
