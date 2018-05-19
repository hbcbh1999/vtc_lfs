using System;
using System.Collections.Generic;
using Emgu.CV.Structure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OptAssignTest.Framework;
using VTC.Common.RegionConfig;
using VTC.Kernel.Video;

namespace OptAssignTest
{
    [TestClass]
    public class ColorChangeTests : ScriptedTestBase
    {
        private static readonly Bgr[] Colors = {
                                                    new Bgr(0xff, 0xff, 0xff),
                                                    new Bgr(0xbb, 0xbb, 0xbb),
                                                    new Bgr(0x88, 0x88, 0x88),
                                                    new Bgr(0xbb, 0xbb, 0xbb),
                                                    new Bgr(0xff, 0xff, 0xff),
                                                };

        [TestMethod]
        [Description("TrackedObject might (slightly?) change color, and it should not affect recognition")]
        public void OftenChangedCarColor_ShouldNotAffectTracking()
        {
            
            var script = OftenColorChangeScript();
            
            RunScript(script, (vista, frame) =>
            {
                var vehicles = vista.CurrentVehicles;

                if (frame > DetectionThreshold)
                {
                    Assert.AreEqual(script.Cars.Count, vehicles.Count, "Car should be detected.");
                }
            });
        }

        [TestMethod]
        [Description("TrackedObject changes color along the path, and it should not affect recognition")]
        public void SlowlyChangedCarColor_ShouldNotAffectTracking()
        {
            var script = SlowlyChangedColorScript();
            RunScript(script, (vista, frame) =>
            {
                var vehicles = vista.CurrentVehicles;

                if (frame > DetectionThreshold)
                {
                    if(script.Cars.Count != vehicles.Count)
                        Console.WriteLine("Not equal");

                    Assert.AreEqual(script.Cars.Count, vehicles.Count, "Car should be detected.");
                }
            });
        }

        public override IEnumerable<CaptureContext> GetCaptures()
        {
            return new[]
            {
                new CaptureContext(new CaptureEmulator("Often color change", OftenColorChangeScript()), new RegionConfig()),
                new CaptureContext(new CaptureEmulator("Slowly color change", SlowlyChangedColorScript()), new RegionConfig())
            };
        }

        private Script OftenColorChangeScript()
        {
            var script = new Script();
            script
                .CreateCar()
                .SetSize(VehicleRadius)
                .AddVerticalPath()
                .CarColor(frame => Colors[frame%Colors.Length]); // car slightly changes color *each* frame
            return script;
        }

        private Script SlowlyChangedColorScript()
        {
            // car changes color on each segment
            var segmentLength = (int) 480/Colors.Length;

            var script = new Script();
            script
                .CreateCar()
                .SetSize(VehicleRadius)
                .AddVerticalPath()
                .CarColor(frame => Colors[frame/segmentLength]);
            return script;
        }
    }
}
