using System;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.Structure;

namespace OptAssignTest.Framework
{
    public class Car
    {
        private static readonly Bgr WhiteColor = new Bgr(Color.White);

        private static readonly Func<uint, Bgr> AlwaysWhiteFunc = _ => WhiteColor;
        private static readonly Func<uint, bool> AlwaysVisibleFunc = _ => true;

        private Func<uint, Bgr> _carColor = AlwaysWhiteFunc; // car is white by default
        private Func<uint, bool> _visibilityFunc = AlwaysVisibleFunc; // car is always visible by default

        private IPathGenerator _path;

        private Func<uint, uint> _carRadius; 

        /// <summary>
        /// Set car size.
        /// </summary>
        public Car SetSize(uint carSize)
        {
            _carRadius = _ => carSize;
            return this;
        }

        /// <summary>
        /// Set car size.
        /// </summary>
        /// <param name="carSizeFunc">Calculate car size based in frame number.</param>
        public Car SetSize(Func<uint, uint> carSizeFunc)
        {
            _carRadius = carSizeFunc;
            return this;
        }

        /// <summary>
        /// Set car as always visible.
        /// </summary>
        public Car AlwaysVisible()
        {
            return Visibility(AlwaysVisibleFunc);
        }

        /// <summary>
        /// Set custom visibility for the car.
        /// </summary>
        /// <param name="visibilityFunc">
        /// Function which returns visibility for the car for the given frame. 
        /// </param>
        public Car Visibility(Func<uint, bool> visibilityFunc)
        {
            _visibilityFunc = visibilityFunc;
            return this;
        }

        /// <summary>
        /// Set custom car coloring for the car.
        /// </summary>
        /// <param name="colorFunc">Function which returns color of the car for the given frame.</param>
        public Car CarColor(Func<uint, Bgr> colorFunc)
        {
            _carColor = colorFunc;
            return this;
        }

        /// <summary>
        /// Set custom car coloring for the car.
        /// </summary>
        public Car CarColor(Color color)
        {
            return CarColor(new Bgr(color));
        }

        /// <summary>
        /// Set custom car coloring for the car.
        /// </summary>
        public Car CarColor(Bgr color)
        {
            return CarColor(_ => color);
        }

        public Car SetPath(IPathGenerator path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            _path = path;
            return this;
        }

        public void Draw(uint frame, Image<Bgr, byte> scene)
        {
            // must be visible
            if (! _visibilityFunc(frame)) return;

            Point? position = _path.GetPosition(frame);
            if (position.HasValue)
            {
                uint carSize = _carRadius(frame);
                var carColor = _carColor(frame);

                scene.Draw(new CircleF(new PointF(position.Value.X, position.Value.Y), carSize), carColor, 0); // TODO: someday move out actual drawing out of here
            }
        }

        public bool IsDone(uint frame)
        {
            return _path.IsDone(frame);
        }

        public bool NearComplete(uint frame)
        {
            return _path.NearComplete(frame);
        }
    }
}