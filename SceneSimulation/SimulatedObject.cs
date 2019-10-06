using System;
using System.Collections.Generic;
using System.Text;

namespace SceneSimulation
{
    public class SimulatedObject
    {
        public string ObjectClass = "Unknown";

        public double X;
        public double Y;
        public double vX;
        public double vY;

        public double destinationX;
        public double destinationY;

        public double Width;
        public double Height;

        public double R, G, B;

        public void LinearMotionTimestep()
        {
            X = X + vX;
            Y = Y + vY;
        }
    }
}
