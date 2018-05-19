using System;
using VTC.Common;

namespace OptAssignTest.Framework
{
    public static class CarUtilities
    {
        public static Car AddVerticalPath(this Car car, Direction from = Direction.South, Path.Vector? offset = null)
        {
            if (! (from == Direction.South || from == Direction.North)) 
                throw new ArgumentOutOfRangeException(nameof(from), "Wrong direction");

            var path = Path.New().StraightFrom(from, offset);
            return car.SetPath(path);
        }

        public static Car AddHorizontalPath(this Car car, Direction from = Direction.East, Path.Vector? offset = null)
        {
            if (!(from == Direction.East || from == Direction.West))
                throw new ArgumentOutOfRangeException(nameof(from), "Wrong direction");

            var path = Path.New().StraightFrom(from, offset);
            return car.SetPath(path);
        }

        public static Car AddAnglePath(this Car car, int angle, Path.Vector? offset = null)
        {
            var path = Path.New().AngledFrom(angle, offset);
            return car.SetPath(path);
        }

        public static Car AddTurn(this Car car, Direction from, Direction turn, Path.Vector? offset = null)
        {
            var path = Path
                        .New()
                        .EnterAndTurn(from, turn, offset);

            return car.SetPath(path);
        }

        public static Car StraightPathFrom(this Car car, Direction from, Path.Vector? offset = null)
        {
            var path = Path.New().StraightFrom(from, offset);
            return car.SetPath(path);
        }


        public static uint VerticalPathLength() // TODO: should be merged to path generator.
        {
            return (uint) 480;
        }

        public static uint HorizontalPathLength() // TODO: should be merged to path generator.
        {
            return (uint) 640;
        }
    }
}
