using System.Collections.Generic;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.Structure;

namespace OptAssignTest.Framework
{
    /// <summary>
    /// Generate "video" frames of circles moving by trajectories.
    /// </summary>
    public class CircleVehicles : AnimationBase
    {
        /// <summary>
        /// Size of the vehicle.
        /// </summary>
        public int VehicleSize { get; set; } = 3;

        /// <summary>
        /// Color of the vehicle.
        /// </summary>
        public Bgr VehicleColor { get; set; } = new Bgr(Color.White);

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="width">Frame width.</param>
        /// <param name="height">Frame height.</param>
        /// <param name="trajectories">Steps with coordinates for multiple vehicles.</param>
        public CircleVehicles(int width, int height, IEnumerable<Point[]> trajectories) : base(width, height, trajectories)
        {
        }

        protected override void DrawVehicle(Image<Bgr, byte> image, Point point, int index)
        {
            image.Draw(new CircleF(new PointF(point.X, point.Y), VehicleSize), VehicleColor, 0);
        }
    }
}
