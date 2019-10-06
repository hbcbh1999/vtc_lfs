using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimulatedScene;
using VTC.Common;

namespace SimulatedScene
{
    public class Scene
    {
        public List<SimulatedObject> SceneObjects;

        public Scene()
        {
            SceneObjects = new List<SimulatedObject>();
        }

        public Measurement[] GetMeasurements()
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

        public void LinearMotionTimestep()
        {
            foreach (var obj in SceneObjects)
            {
                obj.LinearMotionTimestep();
            }
        }
    }
}
