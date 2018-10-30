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

            p1 = Intersection(approachRoadLine, exitRoadLine);            

            TrackedObject syntheticTrajectory = new TrackedObject(initialState,0);

            for (int i = 1; i < NumberOfInterpolatedStepsSyntheticTrajectory -1; i++)
            {
                var lastPoint = syntheticTrajectory.StateHistory.Last();
                var u = Convert.ToDouble(i) / NumberOfInterpolatedStepsSyntheticTrajectory;
                var p = QuadInterpolated(p0, p1, p2, u);
                var se = new StateEstimate();
                se.X = p.X;
                se.Y = p.Y;
                se.Vx = se.X - syntheticTrajectory.StateHistory.Last().X;
                se.Vy = se.Y - syntheticTrajectory.StateHistory.Last().Y;
                syntheticTrajectory.StateHistory.Add(se); 
                //Console.WriteLine("Last: lastPoint.X," + lastPoint.X + ",lastPoint.Y," + lastPoint.Y);
                //Console.WriteLine("Added: se.X," + se.X + ",se.Y," + se.Y + ",se.Vx," + se.Vx + ",se.Vy" + se.Vy);
            }

            syntheticTrajectory.StateHistory.Add(finalState);

            syntheticTrajectory.StateHistory[0].Vx = syntheticTrajectory.StateHistory[1].Vx;
            syntheticTrajectory.StateHistory[0].Vy = syntheticTrajectory.StateHistory[1].Vy;

            syntheticTrajectory.StateHistory.Last().Vx = syntheticTrajectory.StateHistory[syntheticTrajectory.StateHistory.Count - 2].Vx;
            syntheticTrajectory.StateHistory.Last().Vy = syntheticTrajectory.StateHistory[syntheticTrajectory.StateHistory.Count - 2].Vy;

            //for(int i=0;i<syntheticTrajectory.StateHistory.Count;i++)
            //{
            //    var thisPoint = syntheticTrajectory.StateHistory[i];
            //    var angle = Math.Atan2(-thisPoint.Vy,thisPoint.Vx);
                //Console.WriteLine("X," + thisPoint.X + ",Y," + thisPoint.Y + ",Vx," + thisPoint.Vx + ",Vy," + thisPoint.Vy + ",Angle," + angle);
            //}

            //var smoothedVelocityTrajectory = PopulateTrajectoryVelocities(syntheticTrajectory, 1);
            //for(int i=0;i<smoothedVelocityTrajectory.StateHistory.Count;i++)
            //{
            //    var thisPoint = smoothedVelocityTrajectory.StateHistory[i];
            //    var angle = Math.Atan2(-thisPoint.Vy, thisPoint.Vx);
            //    Console.WriteLine("X," + thisPoint.X + ",Y," + thisPoint.Y + ",Vx," + thisPoint.Vx + ",Vy," + thisPoint.Vy + ",Angle," + angle);
            //}
            //return smoothedVelocityTrajectory;
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

            for (int i = 1; i < NumberOfInterpolatedStepsSyntheticTrajectory - 1; i++)
            {
                var se = new StateEstimate();
                se.X = initialState.X + ((double)i/(NumberOfInterpolatedStepsSyntheticTrajectory-1)) * (finalState.X - initialState.X);
                se.Y = initialState.Y + ((double)i/(NumberOfInterpolatedStepsSyntheticTrajectory-1)) * (finalState.Y - initialState.Y);
                se.Vx = se.X - syntheticTrajectory.StateHistory.Last().X;
                se.Vy = se.Y - syntheticTrajectory.StateHistory.Last().Y;
                syntheticTrajectory.StateHistory.Add(se);
            }

            syntheticTrajectory.StateHistory.Add(finalState);

            syntheticTrajectory.StateHistory[0].Vx = syntheticTrajectory.StateHistory[1].Vx;
            syntheticTrajectory.StateHistory[0].Vy = syntheticTrajectory.StateHistory[1].Vy;
            syntheticTrajectory.StateHistory.Last().Vx = syntheticTrajectory.StateHistory[syntheticTrajectory.StateHistory.Count - 2].Vx;
            syntheticTrajectory.StateHistory.Last().Vy = syntheticTrajectory.StateHistory[syntheticTrajectory.StateHistory.Count - 2].Vy;

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

        public static TrackedObject PopulateTrajectoryVelocities(TrackedObject trackedObject, int iterations)
        {
            for(int i=0; i<iterations;i++)
            {
                for(int j=0;j<trackedObject.StateHistory.Count;j++)
                {
                    var estimatedDerivative = EstimateDerivate(trackedObject,j);
                    trackedObject.StateHistory[j].Vx = estimatedDerivative.x;
                    trackedObject.StateHistory[j].Vy = estimatedDerivative.y;
                }
            }
            return trackedObject;
        }

        public static TrajectoryVector EstimateDerivate(TrackedObject trackedObject, int index)
        {
            TrajectoryVector derivativeEstimate = new TrajectoryVector();
            var centerDerivative = CenterDerivative(trackedObject, index);
            //var neighborsVelocity = NeighborsVelocity(trackedObject, index);

            derivativeEstimate.x = centerDerivative.x;
            derivativeEstimate.y = centerDerivative.y;
            //derivativeEstimate.x = (centerDerivative.x + neighborsVelocity.x) / 2;
            //derivativeEstimate.y = (centerDerivative.x + neighborsVelocity.y) / 2;

            return derivativeEstimate;
        }

        //Calculate derivative (velocity) using forward and backward velocities, if possible
        public static TrajectoryVector CenterDerivative(TrackedObject trackedObject, int index)
        {
            TrajectoryVector vector = new TrajectoryVector();
            vector.x = 0;
            vector.y = 0;
            int numPointsUsed = 0;
            
            //If exists, get following point
            if(index + 1 < trackedObject.StateHistory.Count)
            {
                numPointsUsed++;
                var forwards = ForwardDerivative(trackedObject,index);
                vector.x += forwards.x;
                vector.y += forwards.y;
            }

            //If exists, get previous point
            if(index - 1 >= 0)
            {
                numPointsUsed++;
                var backwards = BackwardDerivative(trackedObject,index);
                vector.x += backwards.x;
                vector.y += backwards.y;
            }

            vector.x = vector.x / numPointsUsed;
            vector.y = vector.x / numPointsUsed;

            return vector;
        }

        //Get average of forward and backward neighbor's velocities, if possible
        public static TrajectoryVector NeighborsVelocity(TrackedObject trackedObject, int index)
        {
            TrajectoryVector neighborsVelocity = new TrajectoryVector();

            int numPointsUsed = 0;
            
            //If exists, get following point
            if(index + 1 < trackedObject.StateHistory.Count)
            {
                numPointsUsed++;
                var nextPoint = trackedObject.StateHistory[index + 1];
                neighborsVelocity.x += nextPoint.Vx;
                neighborsVelocity.y += nextPoint.Vy;
            }

            //If exists, get previous point
            if(index - 1 > 0)
            {
                numPointsUsed++;
                var previousPoint = trackedObject.StateHistory[index - 1];
                neighborsVelocity.x += previousPoint.Vx;
                neighborsVelocity.y += previousPoint.Vy;
            }

            neighborsVelocity.x /= numPointsUsed;
            neighborsVelocity.y /= numPointsUsed;

            return neighborsVelocity;
        }

        public static TrajectoryVector ForwardDerivative(TrackedObject trackedObject, int index)
        {
            TrajectoryVector vector = new TrajectoryVector();
            StateEstimate thisPoint = trackedObject.StateHistory[index];
            StateEstimate nextPoint = trackedObject.StateHistory[index+1];
            var forwardVx = nextPoint.X - thisPoint.X;
            var forwardVy = nextPoint.Y - thisPoint.Y;
            vector.x = forwardVx;
            vector.y = forwardVy;
            return vector;
        }

        public static TrajectoryVector BackwardDerivative(TrackedObject trackedObject, int index)
        {
            TrajectoryVector vector = new TrajectoryVector();
            StateEstimate thisPoint = trackedObject.StateHistory[index];
            StateEstimate previousPoint = trackedObject.StateHistory[index - 1];
            var backwardVx = thisPoint.X - previousPoint.X;
            var backwardVy = thisPoint.Y - previousPoint.Y;
            vector.x = backwardVx;
            vector.y = backwardVx;
            return vector;
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

            double b01_X, b01_Y, b11_X, b11_Y;

            //b01.X = Convert.ToInt32((1 - u) * p0.X + u * p1.X);
            //b01.Y = Convert.ToInt32((1 - u) * p0.Y + u * p0.Y);
            //b11.X = Convert.ToInt32((1 - u) * p1.X + u * p2.X);
            //b11.Y = Convert.ToInt32((1 - u) * p1.Y + u * p2.Y);

            b01_X = (1 - u) * p0.X + u * p1.X;
            b01_Y = (1 - u) * p0.Y + u * p0.Y;
            b11_X = (1 - u) * p1.X + u * p2.X;
            b11_Y = (1 - u) * p1.Y + u * p2.Y;

            b02.X = Convert.ToInt32((1 - u) * b01_X + u * b11_X);
            b02.Y = Convert.ToInt32((1 - u) * b01_Y + u * b11_Y);

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
