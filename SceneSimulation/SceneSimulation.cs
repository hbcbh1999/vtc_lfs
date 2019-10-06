using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SceneSimulation;
using VTC.Common;

namespace SceneSimulation
{
    public class SceneSimulation
    {
        private List<SimulatedObject> SceneObjects;

        SceneSimulation()
        {
            SceneObjects = new List<SimulatedObject>();
        }

        Measurement[] GetMeasurements()
        {
            var measurements = new List<Measurement>();

            foreach (var obj in SceneObjects)
            {
                var m = new Measurement();
                m.Size = obj.Width * obj.Height;
                m.X = obj.X;
                m.Y = obj.Y;
                m.Red = obj.R;
                m.Green = obj.G;
                m.Blue = obj.B;
                m.ObjectClass = 0;
                measurements.Add(m);
            }

            return measurements.ToArray();
        }

        void LinearMotionTimestep()
        {
            foreach (var obj in SceneObjects)
            {
                obj.LinearMotionTimestep();
            }
        }
    }
}
