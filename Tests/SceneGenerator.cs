using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VTC.Common;

namespace Tests
{
    class SceneGenerator
    {
        public List<GeneratedObject> SceneObjects = new List<GeneratedObject>();
        
        public List<Measurement> GetNextFrame()
        { 
            List<Measurement> measurements = new List<Measurement>();

            foreach(var gen in SceneObjects)
            { 
                gen.UpdateObjectState();
                measurements.AddRange(gen.GetObjectMeasurements());
            }
            
            return measurements;
        }

        public void CreateObject(int X, int Y, int X_target, int Y_target, double vX = 0.0, double vY = 0.0, double width = 30.0, double height = 10.0, int objectClass = 1, int red = 100, int blue = 100, int green = 100, double pClassification = 0.8, double rPosition = 2.0, double rColor = 10.0)
        {
            var go = new GeneratedObject();

            go.X = X;
            go.Y = Y;
            go.X_target = X_target;
            go.Y_target = Y_target;
            go.vX = vX;
            go.vY = vY;
            go.Width = width;
            go.Height = height;
            go.ObjectClass = objectClass;
            go.P_classification = pClassification;
            go.Green = green;
            go.Red = red;
            go.Blue = blue;
            go.R_position = rPosition;
            go.R_color = rColor;

            SceneObjects.Add(go);
        }
    }
}
