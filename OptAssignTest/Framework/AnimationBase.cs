using System;
using System.Collections.Generic;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.Structure;

namespace OptAssignTest.Framework
{
    public abstract class AnimationBase
    {
        private readonly int _width;
        private readonly int _height;
        private readonly IEnumerable<Point[]> _trajectories;

        /// <summary>
        /// Color of frame background.
        /// </summary>
        public Color BackgroundColor { get; set; } = Color.Black;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="width">Frame width.</param>
        /// <param name="height">Frame height.</param>
        /// <param name="trajectories">Steps with coordinates for trajectories.</param>
        protected AnimationBase(int width, int height, IEnumerable<Point[]> trajectories)
        {
            _width = width;
            _height = height;
            _trajectories = trajectories;
        }

        /// <summary>
        /// Get images along the timelone.
        /// </summary>
        public IEnumerable<Image<Bgr, Byte>> Frames()
        {
            var background = new Image<Bgr, byte>(_width, _height, new Bgr(BackgroundColor));

            // first frame expected to be a background
            yield return background;

            // generate frame for each for the trajectory steps
            foreach (Point[] points in _trajectories)
            {
                var image = background.Clone();

                for (int index = 0; index < points.Length; index++)
                {
                    var point = points[index];
                    DrawVehicle(image, point, index);
                }

                yield return image;
            }
        }

        /// <summary>
        /// Draw vehicle.
        /// </summary>
        /// <param name="image">Where to draw vehicle.</param>
        /// <param name="point">TrackedObject coordinates.</param>
        /// <param name="index">TrackedObject index.</param>
        protected abstract void DrawVehicle(Image<Bgr, byte> image, Point point, int index);
    }
}