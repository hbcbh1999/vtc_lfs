using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Emgu.CV;
using Emgu.CV.Cvb;
using Emgu.CV.Structure;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VTC.Common;
using Point = System.Drawing.Point;
using NLog;
using VTC.Classifier;
using VTC.Common.RegionConfig;

namespace VTC.Kernel.Vistas
{
    /// <summary>
    /// A Vista takes in a stream of frames and produces tracked object measurements.
    /// Vistas perform background subtraction, MHT and overlay rendering.
    /// </summary>
    public class Vista
    {
        private static readonly Logger Logger = LogManager.GetLogger("vista");

        #region Static colors

        private static readonly Bgr WhiteColor = new Bgr(Color.White);
        private static readonly Bgr StateColorGreen = new Bgr(0.0, 255.0, 0.0);
        private static readonly Bgr StateColorRed = new Bgr(0.0, 0.0, 255.0);

        #endregion

        //************* Configuration constants ****************
        private static readonly int MeasurementArrayQueueMaxLength = 300;

        private static readonly int MHTMaxUpdateTimeMs = 1000;

        //************* Strings ****************
        private static readonly string ApproachText = "Approach";
        private static readonly string ExitText = "Exit";

        //************* Main image variables ***************
        public Image<Bgr, float> RoiImage; //Area occupied by traffic
        public int _width;
        public int _height;

        public Queue<Measurement[]> MeasurementArrayQueue;              // For displaying detection history (colored dots)
        public Image<Gray, byte> Movement_Mask { get; private set; }    //Thresholded, b&w movement mask
        public Image<Bgr, float> ColorBackground { get; private set; }  //Average Background being formed
        public Image<Bgr, byte> TrainingImage { get; private set; }     //Image to be exported for training set

        //************* Multiple Hypothesis Tracker ***************  
        private MultipleHypothesisTracker _mht;

        //************* Multiple object detector ******************  
        public YoloClassifier _yoloClassifier = new YoloClassifier();
        public YoloIntegerNameMapping _yoloNameMapping = new YoloIntegerNameMapping();

        //************* Debug statistics ******************  
        private bool _debugMode = false;   //Prevent cancellation token from killing MHT update

        private RegionConfig _regionConfiguration;
        public RegionConfig RegionConfiguration
        {
            get
            {
                return _regionConfiguration;
            }
            private set
            {
                if (_regionConfiguration == value) return;

                _regionConfiguration = value;

                RoiImage = RegionConfiguration.RoiMask.GetMask(_width, _height, WhiteColor);
            }
        }

        public List<TrackedObject> CurrentVehicles => _mht.CurrentVehicles;
        public List<TrackedObject> DeletedVehicles => _mht.DeletedVehicles;

        public bool CpuMode => _yoloClassifier.cpuMode;

        private string ApproachName(int number)
        {
            return ApproachText + " " + number;
        }

        private string ExitName(int number)
        {
            return ExitText + " " + number;
        }

        public Vista(int width, int height, RegionConfig regionConfiguration)
        {
            Console.WriteLine("New vista created");
            var tempLogger = LogManager.GetLogger("userlog");
            tempLogger.Log(LogLevel.Info, "Vista: Initializer.");

            _width = width;
            _height = height;

            tempLogger.Log(LogLevel.Info, "Vista: Initializer: Creating measurement queue.");
            MeasurementArrayQueue = new Queue<Measurement[]>(MeasurementArrayQueueMaxLength);

            tempLogger.Log(LogLevel.Info, "Vista: Initializer: Initializing configuration.");
            RegionConfiguration = regionConfiguration ?? new RegionConfig();

            tempLogger.Log(LogLevel.Info, "Vista: Initializer: Updating configuration.");
            UpdateRegionConfiguration(RegionConfiguration);

            tempLogger.Log(LogLevel.Info, "Vista: Initializer finished.");
        }

        public Measurement[] MeasurementsArray;
        public void Update(Image<Bgr, byte> newFrame, double timestep)
        {
            try
            {
                var measurementsList = _yoloClassifier.DetectFrameYolo(newFrame);
                var measurementsFilteredByROI = measurementsList.Where(m => IsContainedInROI(m));
                MeasurementsArray = measurementsFilteredByROI.Where(m => DetectionClasses.ClassDetectionWhitelist.Contains(YoloIntegerNameMapping.GetObjectTypeFromClassInteger(m.ObjectClass, _yoloNameMapping.IntegerToObjectType))).ToArray();
                MeasurementArrayQueue.Enqueue(MeasurementsArray);
                while (MeasurementArrayQueue.Count > MeasurementArrayQueueMaxLength)
                    MeasurementArrayQueue.Dequeue();

                var cts = new CancellationTokenSource();
                var ct = cts.Token;
                var mhtTask = Task.Run(() => _mht.Update(MeasurementsArray, ct, timestep));
                if (!_debugMode)
                {
                    if (mhtTask.Wait(TimeSpan.FromMilliseconds(MHTMaxUpdateTimeMs)))
                    {
                    }
                    else
                    {
                        cts.Cancel();
                        while (!mhtTask.IsCompleted)
                        {
                        }
                    }
                }
                else
                {
                    while (!mhtTask.IsCompleted)
                    {
                    }
                }

                GC.KeepAlive(newFrame);
            }
            catch (Exception e)
            {
#if DEBUG
                Logger.Log(LogLevel.Error, "In Vista:Update(), " + e.Message);
                throw;           
#else
                Logger.Log(LogLevel.Error, "In Vista:Update(), " + e.Message);
#endif
            }   
        }

        public void UpdateRegionConfiguration(RegionConfig newRegionConfig)
        {
            RegionConfiguration = newRegionConfig;
            var vf = new VelocityField(_width, _height);
            _mht = new MultipleHypothesisTracker(newRegionConfig, vf);
            for (int i = 1; i <= 4; i++)
            {
                var approachName = ApproachName(i);
                var exitName = ExitName(i);

                if (!RegionConfiguration.Regions.ContainsKey(approachName))
                    RegionConfiguration.Regions.Add(approachName, new Polygon());

                if (!RegionConfiguration.Regions.ContainsKey(exitName))
                    RegionConfiguration.Regions.Add(exitName, new Polygon());
            }
            RoiImage = RegionConfiguration.RoiMask.GetMask(_width, _height, WhiteColor);
        }

        public void DrawVelocityField<TColor, TDepth>(Image<TColor, TDepth> image, TColor color, int thickness)
           where TColor : struct, IColor
           where TDepth : new()
        {
            _mht?.VelocityField.Draw(image, color, thickness);
        }

        public Image<Bgr, float> GetBackgroundImage()
        {
            var lightColor = new Bgr(50,50,50);
            Image<Bgr, float> overlay = new Image<Bgr, float>(_width, _height, lightColor);
            overlay = overlay.And(RoiImage);
            if (ColorBackground != null)
            {
                overlay = overlay + ColorBackground;    
            }
            return overlay;
        }

        public Image<Bgr, byte> GetCurrentStateImage(Image<Bgr, byte> frame)
        {
            var stateImage = frame.Clone();

            List<TrackedObject> vehicles = _mht.MostLikelyStateHypothesis().Vehicles;

            vehicles.ForEach(delegate(TrackedObject vehicle)
            {
                if (vehicle.NetMovement() < 10.0)
                {
                    return;
                }

                var lastState = vehicle.StateHistory.Last();
                var x = (float) lastState.X;
                var y = (float) lastState.Y;

                var validationRegionDeviation = _mht.ValidationRegionDeviation;
#if DEBUG
                // Draw class
                var g = Graphics.FromImage(stateImage.Bitmap);
                var className = YoloIntegerNameMapping.GetObjectNameFromClassInteger(lastState.MostFrequentClassId(), _yoloNameMapping.IntegerToObjectName);
                g.DrawString(className, new Font(FontFamily.GenericMonospace, (float) 10.0), new SolidBrush(Color.White), (float)vehicle.StateHistory.Last().X, (float)vehicle.StateHistory.Last().Y);  
                    
                // Draw uncertainty circle
                var radius = validationRegionDeviation *
                            (float) Math.Sqrt(Math.Pow(lastState.CovX, 2) + (float) Math.Pow(lastState.CovY, 2));
                if (radius < 2.0)
                    radius = (float) 2.0;

                if (radius > 50.0)
                    radius = (float) 50.0;

                stateImage.Draw(new CircleF(new PointF(x, y), radius), StateColorGreen);


                // Draw velocity vector
                stateImage.Draw(
                new LineSegment2D(new Point((int) x, (int) y),
                    new Point((int) (x + lastState.Vx), (int) (y + lastState.Vy))), StateColorRed, 1);
#else
                stateImage.Draw(new CircleF(new PointF(x, y), 10),
                    new Bgr(vehicle.StateHistory.Last().Blue, vehicle.StateHistory.Last().Green,
                        vehicle.StateHistory.Last().Red), 2);
                stateImage.Draw(new CircleF(new PointF(x, y), 2), StateColorGreen);
#endif
            });

            foreach (var t in _mht.Trajectories)
            {
                var distance = Math.Sqrt(Math.Pow(t.StateEstimates.First().X - t.StateEstimates.Last().X, 2) +
                                            Math.Pow(t.StateEstimates.First().Y - t.StateEstimates.Last().Y, 2));

                if (!(distance > _regionConfiguration.MinPathLength)) continue;

                var ageFactor = 1.0 - (double) (DateTime.Now - t.ExitTime).Ticks / TimeSpan.FromSeconds(3).Ticks;
                var trajectoryColor = new Bgr(0, 0, 200*ageFactor);
                var trajectoryRendering = t.StateEstimates.Select(s => new Point((int)s.X, (int)s.Y)).ToArray();
                stateImage.DrawPolyline(trajectoryRendering, false, trajectoryColor);
            }

            var latestMeasurements = MeasurementArrayQueue.Last();
            foreach (var m in latestMeasurements)
            {
                stateImage.Draw(new Rectangle((int) (m.X - m.Width/2), (int) (m.Y - m.Height/2), (int) m.Width, (int) m.Height), new Bgr(Color.Chartreuse), 1);
            }


            return stateImage;
        }

        private bool IsContainedInROI(Measurement m)
        {
            if (m.X >= RoiImage.Width || m.Y >= RoiImage.Height || m.X < 0 || m.Y < 0)
            {return false;}

            return ( (int) RoiImage[ (int) m.Y, (int) m.X].Blue != 0);
        }

    }
}

public class BlobAreaComparer : IComparer<CvBlob>
{
     public int Compare(CvBlob x, CvBlob y)
     {
         return x.Area < y.Area ? 1 : -1;
     }
}
        