using System;
using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OptAssignTest.Framework
{
    /// <summary>
    /// Self tests for test helpers.
    /// </summary>
    [TestClass]
    public class PathCreatorTests : ScriptedTestBase
    {
        [TestMethod]
        [Description("Path with single path section should detect section bounds correctly.")]
        public void SinglePathCompleteness() 
        {
            // create generator with vertical path
            IPathGenerator generator = Path
                                        .New()
                                        .StraightFrom(Direction.South);

            Assert.IsFalse(generator.IsDone(0), "Should not be done for beginning.");
            Assert.IsFalse(generator.IsDone(480 - 1), "Should not be done for last point.");
            Assert.IsTrue(generator.IsDone(480), "Should be done for next after the last point.");
        }

        [Ignore]
        [TestMethod]
        [Description("Path with straight South->North path section should detect section bounds correctly.")]
        public void ScriptWithSingleSouthPathCompleteness()
        {
            SingleVerticalPathTest(Direction.South);
        }

        [Ignore]
        [TestMethod]
        [Description("Path with straight North->South path section should detect section bounds correctly.")]
        public void ScriptWithSingleNorthPathCompleteness() 
        {
            SingleVerticalPathTest(Direction.North);
        }

        [Ignore]
        [TestMethod]
        [Description("Path with straight West->East path section should detect section bounds correctly.")]
        public void ScriptWithSingleWestPathCompleteness() 
        {
            SingleHorizontalPathTest(Direction.West);
        }

        [Ignore]
        [TestMethod]
        [Description("Path with straight East->West path section should detect section bounds correctly.")]
        public void ScriptWithSingleEastPathCompleteness() 
        {
            SingleHorizontalPathTest(Direction.East);
        }

        [TestMethod]
        [Description("Check points generated for South->North path.")]
        public void FromSouthDirection()
        {
            VerifyDirection(Direction.South, delegate(Point currPos, Point prevPos)
                                                {
                                                    Assert.AreEqual(currPos.X, prevPos.X, "No change in X position is expected.");
                                                    Assert.IsTrue(currPos.Y >= prevPos.Y, "The path should go up.");
                                                });
        }

        [TestMethod]
        [Description("Check points generated for North->South path.")]
        public void FromNorthDirection()
        {
            VerifyDirection(Direction.North, delegate(Point currPos, Point prevPos)
                                                {
                                                    Assert.AreEqual(currPos.X, prevPos.X, "No change in X position is expected.");
                                                    Assert.IsTrue(currPos.Y <= prevPos.Y, "The path should go down.");
                                                });
        }

        [TestMethod]
        [Description("Check points generated for East->West path.")]
        public void FromEastDirection()
        {
            VerifyDirection(Direction.East, delegate(Point currPos, Point prevPos)
                                                {
                                                    Assert.AreEqual(currPos.Y, prevPos.Y, "No change in Y position is expected.");
                                                    Assert.IsTrue(currPos.X <= prevPos.X, "The path should go left.");
                                                });
        }

        [TestMethod]
        [Description("Check points generated for West->East path.")]
        public void FromWestDirection()
        {
            VerifyDirection(Direction.West, delegate(Point currPos, Point prevPos)
                                                {
                                                    Assert.AreEqual(currPos.Y, prevPos.Y, "No change in Y position is expected.");
                                                    Assert.IsTrue(currPos.X >= prevPos.X, "The path should go right.");
                                                });
        }

        [TestMethod]
        [Description("Check points generated for West->East path.")]
        public void FromSouthAndTurnToEast()
        {

            IPathGenerator generator = Path
                .New()
                .EnterAndTurn(Direction.South, Direction.East);

            Point? firstPoint = null, lastPoint = null;
            for (uint frame = 0; !generator.IsDone(frame); frame++)
            {
                Point? position = generator.GetPosition(frame);
                if (!position.HasValue) continue;

                if (! firstPoint.HasValue)
                {
                    firstPoint = position.Value;
                }
                lastPoint = position;
            }

            if (firstPoint != null)
            {
                var firstPointXError = (uint) Math.Abs( (640 / 2) - firstPoint.Value.X);
                var firstPointYError = (uint)Math.Abs(VehicleRadius - firstPoint.Value.Y);
                Assert.IsTrue(firstPointXError < 5);
                Assert.IsTrue(firstPointYError < 5);
            }

            if (lastPoint != null)
            {
                var lastPointXError = (uint)Math.Abs(640 - lastPoint.Value.X);
                var lastPointYError = (uint)Math.Abs((480 / 2) - lastPoint.Value.Y);
                Assert.IsTrue(lastPointXError < 5);
                Assert.IsTrue(lastPointYError < 5);
            }
        }

        private void VerifyDirection(Direction direction, Action<Point, Point> validation)
        {

            IPathGenerator generator = Path
                .New()
                .StraightFrom(direction);

            Point? prevPoint = null;
            for (uint frame = 0; !generator.IsDone(frame); frame++)
            {
                Point? position = generator.GetPosition(frame);
                if (! position.HasValue) continue;

                if (prevPoint.HasValue)
                {
                    validation(position.Value, prevPoint.Value);
                }
                prevPoint = position;
            }
        }

        [Ignore]
        [TestMethod]
        [Description("Empty script should behave as a completed script.")]
        public void EmptyScriptCompleteness() 
        {
            var script = new Script();

            Assert.IsTrue(script.IsDone(0), "Should be done for beginning.");
        }

        [TestMethod]
        [Description("Offset should be taken in account.")]
        public void PathOffsetTest() 
        {
            const int horizontalOffset = 10;

            IPathGenerator generator = Path
                .New()
                .StraightFrom(Direction.South, new Path.Vector(horizontalOffset, 0));

            IPathGenerator generator2 = Path
                .New()
                .StraightFrom(Direction.South);

            for (uint frame = 0; !generator.IsDone(frame) && !generator2.IsDone(frame); frame++)
            {
                Point? position = generator.GetPosition(frame);
                Point? position2 = generator2.GetPosition(frame);
                if (!position.HasValue || !position2.HasValue) continue;

                Assert.AreEqual(horizontalOffset, position.Value.X - position2.Value.X);
                Assert.AreEqual(position.Value.Y, position2.Value.Y);
            }
        }

        private void SingleVerticalPathTest(Direction direction)
        {

            // create generator with vertical path
            Script script = new Script();
            script.CreateCar().SetSize(VehicleRadius).AddVerticalPath(direction);

            Assert.IsFalse(script.IsDone(0), "Should not be done for beginning.");
            Assert.IsFalse(script.IsDone(480- 1), "Should not be done for last point.");
            Assert.IsTrue(script.IsDone(480), "Should be done for next after the last point.");
        }

        private void SingleHorizontalPathTest(Direction direction)
        {

            // create generator with horizontal path
            Script script = new Script();
            script.CreateCar().SetSize(VehicleRadius).AddHorizontalPath(direction);

            Assert.IsFalse(script.IsDone(0), "Should not be done for beginning.");
            Assert.IsFalse(script.IsDone(640- 1), "Should not be done for last point.");
            Assert.IsTrue(script.IsDone(640), "Should be done for next after the last point.");
        }
    }
}
