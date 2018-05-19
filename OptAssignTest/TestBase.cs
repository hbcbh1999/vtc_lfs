using System.Drawing;
using VTC.Common;
using VTC.Common.RegionConfig;
using VTC.Kernel.Vistas;

namespace OptAssignTest
{
    public class TestBase
    {
        /// <summary>
        /// Default vehicle size.
        /// </summary>
        protected const int VehicleRadius = 3; // in pixels

        /// <summary>
        /// Creates initialized intersection vista to be used for tests.
        /// </summary>
        /// <returns></returns>
        protected static Vista CreateVista()
        {
            // create mask for the whole image
            var polygon = new Polygon();
            polygon.AddRange(new[]
            {
                new Point(0, 0), 
                new Point(0, (int) 480),
                new Point((int) 640, (int) 480), 
                new Point((int) 640, 0),
                new Point(0, 0)
            });

            var regionConfig = new RegionConfig
            {
                RoiMask = polygon, 
                MinObjectSize = 5,
            };

            var v = new Vista((int) 640, (int) 480, regionConfig);
            v.UpdateRegionConfiguration(regionConfig);
            return v;
        }
    }
}