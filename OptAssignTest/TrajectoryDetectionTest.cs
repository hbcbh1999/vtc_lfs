using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OptAssignTest.Framework;
using VTC.Common.RegionConfig;
using VTC.Kernel.Video;

namespace OptAssignTest
{
    [TestClass]
    public class TrajectoryDetectionTest : ScriptedTestBase
    {
        [Ignore]
        [TestMethod]
        public void EmptyTrajectory_ShouldNotDetectVehicles()
        {
            var script = EmptyTrajectoryScript();
            int nFrames = 1000;
            RunScript(script, (vista, frame) =>
            {
                var vehicles = vista.CurrentVehicles;
                if (frame > DetectionThreshold && frame < nFrames)
                {
                    Assert.AreEqual(script.Cars.Count, vehicles.Count, "{0} cars should be detected (failed at {1} frame).", script.Cars.Count, frame);
                    // TODO: make sure that each car keeps its direction
                }
            });
        }

        [TestMethod]
        public void SingleDiagonalTrajectory_ShouldDetectSingleVehicle()
        {
            var script = SingleDiagonalTrajectoryScript();

            RunScript(script, (vista, frame) =>
            {
                var vehicles = vista.CurrentVehicles;

                if (frame > DetectionThreshold && !script.IsDone(frame))
                {
                    Assert.AreEqual(script.Cars.Count, vehicles.Count, "{0} cars should be detected (failed at {1} frame).", script.Cars.Count, frame);
                    // TODO: make sure that each car keeps its direction
                }
            });
        }

        [Ignore]
        [TestMethod]
        public void TwoDiagonalsTrajectories_ShouldDetectTwoVehicles()
        {
            var vista = CreateVista();

            var diagonals = Enumerable.Range(5 + VehicleRadius, 195).Select(x => new[] { new Point(x, x + 5), new Point(x, x - 5) });

            int count = 0;
            var generator = new CircleVehicles((int) 640, (int) 480, diagonals);
            foreach (var frame in generator.Frames())
            {
//                frame.Save(@"c:\temp\frame" + count + ".png");
                vista.Update(frame);
                if (count++ > 2) // ignoring first iterations, since it saves background (first step) and accumulates statistics(?).
                {
                    Assert.AreEqual(2, vista.CurrentVehicles.Count);
                }
            }
        }

        private Script EmptyTrajectoryScript()
        {
            var script = new Script();
            return script;
        }

        private Script SingleDiagonalTrajectoryScript()
        {
            var script = new Script();

            script
                .CreateCar()
                .AddAnglePath(20)
                .SetSize(VehicleRadius);
            return script;
        }

        public override IEnumerable<CaptureContext> GetCaptures()
        {
            // ER: dislike that it's calculated in different places (need it because it's reused in RunScript). 
            // Possible source of future errors.
            // think - maybe script should expose it somehow?

            var regionConfig = new RegionConfig();

            return new[]
            {
                new CaptureContext(new CaptureEmulator("EmptyTrajectory_ShouldNotDetectVehicles", EmptyTrajectoryScript()), regionConfig),
                new CaptureContext(new CaptureEmulator("SingleDiagonalTrajectory_ShouldDetectSingleVehicle", SingleDiagonalTrajectoryScript()), regionConfig),
            };
        }
    }
} ;
