using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VTC.Common;
using VTC.Common.RegionConfig;

namespace VTC.Kernel
{
    public static class PolygonTrajectorySynthesizer
    {
        private const int NumberOfInterpolatedStepsSyntheticTrajectory = 50;

        public static TrackedObject SyntheticTrajectory(Polygon approach, Polygon exit, RoadLine approachRoadLine, RoadLine exitRoadLine)
        {
            var initialState = new StateEstimate();
            initialState.X = approach.Centroid.X;
            initialState.Y = approach.Centroid.Y;
            var p0 = new Point(Convert.ToInt32(initialState.X),Convert.ToInt32(initialState.Y));

            var finalState = new StateEstimate();
            finalState.X = exit.Centroid.X;
            finalState.Y = exit.Centroid.Y;
            var p2 = new Point(Convert.ToInt32(finalState.X), Convert.ToInt32(finalState.Y));

            var p1 = new Point();

            //If approach and exit roadlines are identical, use a straight-line approximation
            if (Math.Abs(approachRoadLine.ApproachCentroidY - exitRoadLine.ApproachCentroidY) < 0.5 &&
                Math.Abs(approachRoadLine.ApproachCentroidX - exitRoadLine.ApproachCentroidX) < 0.5 &&
                         Math.Abs(approachRoadLine.ExitCentroidY - exitRoadLine.ExitCentroidY) < 0.5 &&
                                  Math.Abs(approachRoadLine.ExitCentroidX - exitRoadLine.ExitCentroidX) < 0.5)
            {
                p1.X = Convert.ToInt32((approachRoadLine.ApproachCentroidX + approachRoadLine.ExitCentroidX) / 2);
                p1.Y = Convert.ToInt32((approachRoadLine.ApproachCentroidY + approachRoadLine.ExitCentroidY) / 2);
            }
            else
            {
                p1 = Intersection(approachRoadLine, exitRoadLine);
            }

            TrackedObject syntheticTrajectory = new TrackedObject(initialState,0);
            syntheticTrajectory.StateHistory.Add(initialState);

            for (int i = 0; i < NumberOfInterpolatedStepsSyntheticTrajectory; i++)
            {
                var u = Convert.ToDouble(i) / NumberOfInterpolatedStepsSyntheticTrajectory;
                var p = QuadInterpolated(p0, p1, p2, u);
                var se = new StateEstimate();
                se.X = p.X;
                se.Y = p.Y;
                se.Vx = se.X - syntheticTrajectory.StateHistory.Last().X;
                se.Vy = se.Y - syntheticTrajectory.StateHistory.Last().Y;
                syntheticTrajectory.StateHistory.Add(se);
            }

            syntheticTrajectory.StateHistory.Add(finalState);
            return syntheticTrajectory;
        }

        public static TrackedObject SyntheticTrajectory(Polygon approach, Polygon exit, RoadLine approachRoadLine)
        {
            var initialState = new StateEstimate();
            initialState.X = approach.Centroid.X;
            initialState.Y = approach.Centroid.Y;
            var p0 = new Point(Convert.ToInt32(initialState.X),Convert.ToInt32(initialState.Y));

            var finalState = new StateEstimate();
            finalState.X = exit.Centroid.X;
            finalState.Y = exit.Centroid.Y;
            var p2 = new Point(Convert.ToInt32(finalState.X), Convert.ToInt32(finalState.Y));

            var p1 = new Point();

            //Use a straight-line approximation
            
            p1.X = Convert.ToInt32((approachRoadLine.ApproachCentroidX + approachRoadLine.ExitCentroidX) / 2);
            p1.Y = Convert.ToInt32((approachRoadLine.ApproachCentroidY + approachRoadLine.ExitCentroidY) / 2);

            TrackedObject syntheticTrajectory = new TrackedObject(initialState,0);
            syntheticTrajectory.StateHistory.Add(initialState);

            for (int i = 0; i < NumberOfInterpolatedStepsSyntheticTrajectory; i++)
            {
                var se = new StateEstimate();
                se.X = initialState.X + ((double)i/(NumberOfInterpolatedStepsSyntheticTrajectory-1)) * (finalState.X - initialState.X);
                se.Y = initialState.Y + ((double)i/(NumberOfInterpolatedStepsSyntheticTrajectory-1)) * (finalState.Y - initialState.Y);
                se.Vx = se.X - syntheticTrajectory.StateHistory.Last().X;
                se.Vy = se.Y - syntheticTrajectory.StateHistory.Last().Y;
                syntheticTrajectory.StateHistory.Add(se);
            }

            syntheticTrajectory.StateHistory.Add(finalState);
            return syntheticTrajectory;
        }

        public static Point Intersection(RoadLine approachLine, RoadLine exitLine)
        {
            var roadIntersection = new Point();

            var x1 = approachLine.ApproachCentroidX;
            var y1 = approachLine.ApproachCentroidY;
            var x2 = approachLine.ExitCentroidX;
            var y2 = approachLine.ExitCentroidY;

            var x3 = exitLine.ApproachCentroidX;
            var y3 = exitLine.ApproachCentroidY;
            var x4 = exitLine.ExitCentroidX;
            var y4 = exitLine.ExitCentroidY;

            roadIntersection.X = Convert.ToInt32(((x1 * y2 - y1 * x2) * (x3 - x4) - (x1 - x2) * (x3 * y4 - y3 * x4)) / ((x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4)));
            roadIntersection.Y = Convert.ToInt32(((x1 * y2 - y1 * x2) * (y3 - y4) - (y1 - y2) * (x3 * y4 - y3 * x4)) / ((x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4)));

            return roadIntersection;
        }

        /// <summary>
        /// 2nd-order (quadratic) Bezier interpolation of two lines
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="u"></param>
        /// <returns></returns>
        public static Point QuadInterpolated(Point p0, Point p1, Point p2, double u)
        {
            var b01 = new Point();
            var b11 = new Point();
            var b02 = new Point();

            b01.X = Convert.ToInt32((1 - u) * p0.X + u * p1.X);
            b01.Y = Convert.ToInt32((1 - u) * p0.Y + u * p0.Y);
            b11.X = Convert.ToInt32((1 - u) * p1.X + u * p2.X);
            b11.Y = Convert.ToInt32((1 - u) * p1.Y + u * p2.Y);

            b02.X = Convert.ToInt32((1 - u) * b01.X + u * b11.X);
            b02.Y = Convert.ToInt32((1 - u) * b01.Y + u * b11.Y);

            return b02;
        }

    }

    public class RoadLine
    {
        public double ApproachCentroidX;
        public double ApproachCentroidY;

        public double ExitCentroidX;
        public double ExitCentroidY;

        public Point Interpolate(double u)
        {
            var p = new Point();

            p.X = Convert.ToInt32((1 - u) * ApproachCentroidX + u * ExitCentroidX);
            p.Y = Convert.ToInt32((1 - u) * ApproachCentroidY + u * ExitCentroidY);

            return p;
        }
    }
}
