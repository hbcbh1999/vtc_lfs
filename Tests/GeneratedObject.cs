using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics;
using VTC.Common;

namespace Tests
{
    class GeneratedObject
    {
        public double X;
        public double Y;
        public double Red;
        public double Green;
        public double Blue;
        public double Width;
        public double Height;
        public int ObjectClass;
        public double vX;
        public double vY;
        public double vHeight;
        public double vWidth;

        //Destination variables
        public double X_target;
        public double Y_target;

        //Control-system variables
        public double Velocity_controller_P = 0.0;
        public double Velocity_controller_I = 0.001;
        public double Velocity_controller_D = 0.0;
        public double Velocity_controller_error_integral_X = 0.0;
        public double Velocity_controller_last_error_X = 0.0;
        public double Velocity_controller_error_integral_Y = 0.0;
        public double Velocity_controller_last_error_Y = 0.0;

        //Noise or misdetection statistics
        public double[] P_detection = { 0.05, 0.95, 1.00 };
        public double P_classification; //Probability of being classified correctly, per frame
        public double R_position;
        public double R_color;
        private Random rand = new Random();

        public void UpdateObjectState()
        { 
            //Calculate updates
            X = X + vX;
            Y = Y + vY;
            Width = Width + vWidth >= 1? Width + vWidth : Width;
            Height = Height + vHeight >= 1 ? Height + vHeight : Height;
            
            //Update velocities (navigate towards target)
            var X_error = X_target - X;
            Velocity_controller_error_integral_X += X_error;
            var X_delta = X_error - Velocity_controller_last_error_X;
            vX = vX + (Velocity_controller_P*X_error) + (Velocity_controller_I * Velocity_controller_error_integral_X) + (Velocity_controller_D * X_delta);
            Velocity_controller_last_error_X = X_error;

            var Y_error = Y_target - Y;
            Velocity_controller_error_integral_Y += Y_error;
            var Y_delta = Y_error - Velocity_controller_last_error_Y;
            vY = vY + (Velocity_controller_P * Y_error) + (Velocity_controller_I * Velocity_controller_error_integral_Y) + (Velocity_controller_D * Y_delta);
            Velocity_controller_last_error_Y = Y_error;
        }

        //An object may be detected zero times (detection failure), once (correct detection) or multiple times (over-detection).
        //Thus, a single-frame 'observation' should return a list of detections.
        public List<Measurement> GetObjectMeasurements()
        { 
            List<Measurement> observations = new List<Measurement>();

            var numDetections = SampleNumDetections();

            var xDistribution = new MathNet.Numerics.Distributions.Normal(X, R_position);
            var yDistribution = new MathNet.Numerics.Distributions.Normal(Y, R_position);
            var rDistribution = new MathNet.Numerics.Distributions.Normal(Red, R_color);
            var gDistribution = new MathNet.Numerics.Distributions.Normal(Green, R_color);
            var bDistribution = new MathNet.Numerics.Distributions.Normal(Blue, R_color);
            var widthDistribution = new MathNet.Numerics.Distributions.Normal(Width, R_position);
            var heightDistribution = new MathNet.Numerics.Distributions.Normal(Height, R_position);

            for (int i=0;i<numDetections;i++)
            {
                var classObserved = ObjectClass;
                var classificationError = rand.Next(Convert.ToInt32(100.0)) <= P_classification*100.0 ? false : true;
                if (classificationError)
                {
                    classObserved = SelectRandomClass(ObjectClass, 6);
                }

                var measurement = new Measurement();
                measurement.X = xDistribution.Sample();
                measurement.Y = yDistribution.Sample();
                measurement.Red = rDistribution.Sample();
                measurement.Green = gDistribution.Sample();
                measurement.Blue = bDistribution.Sample();
                measurement.Height = heightDistribution.Sample();
                measurement.Width = widthDistribution.Sample();
                measurement.Size = measurement.Width * measurement.Height;
                measurement.ObjectClass = classObserved;

                observations.Add(measurement);
            }
            
            return observations;
        }

        //Use this function to model misclassification.
        int SelectRandomClass(int objectClass, int maxClass)
        {
            //Generate a list of all classes other than this one
            var range = Enumerable.Range(0,maxClass);
            var rangeAfterExclusion = range.Except(new List<int>{objectClass});

            //Randomly select an element
            var selectedIndex = rand.Next(rangeAfterExclusion.Count());
            var selectedClass = rangeAfterExclusion.ElementAt(selectedIndex);
            return selectedClass;
        }

        public int SampleNumDetections()
        {
            var randomValue = rand.NextDouble();
            for(int i=0;i<P_detection.Length;i++)
            { 
                if(randomValue < P_detection[i])
                { 
                    return i;    
                }
            }

            throw new NumericalBreakdownException();
        }

    }


}
