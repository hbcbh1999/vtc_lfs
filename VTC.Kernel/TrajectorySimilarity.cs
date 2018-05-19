using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using NDtw;
using NDtw.Preprocessing;
using VTC.Common;

namespace VTC.Kernel
{
    class TrajectoryVector
    {
        public double x;
        public double y;
    }

    public class TrajectorySimilarity
    {
        private const double START_POSITION_MULTIPLIER = 0.1;
        private const double END_POSITION_MULTIPLIER = 1.0;
        private const double START_ANGLE_MULTIPLIER = 100.0;
        private const double END_ANGLE_MULTIPLIER = 50.0;

        public static double EndpointCost(List<StateEstimate> trajectory1, List<StateEstimate> trajectory2)
        {
            //Calculate position costs
            var t1_first = trajectory1.First();
            var t1_last = trajectory1.Last();
            var t2_first = trajectory2.First();
            var t2_last = trajectory2.Last();
            //1. Start position cost
            double startPositionCost = Distance(t1_first.X, t1_first.Y, t2_first.X, t2_first.Y);
            //2. End position cost
            double endPositionCost = Distance(t1_last.X, t1_last.Y, t2_last.X, t2_last.Y);

            //Calculate angle costs
            //3. Start angle cost
            var initialAngleCost = CompareInitialTrajectoryAngles(trajectory1, trajectory2);
            //4. End angle cost
            var finalAngleCost = CompareFinalTrajectoryAngles(trajectory1, trajectory2);
            
            double totalCost = START_POSITION_MULTIPLIER*startPositionCost + END_POSITION_MULTIPLIER*endPositionCost + START_ANGLE_MULTIPLIER*initialAngleCost + END_ANGLE_MULTIPLIER*finalAngleCost;
            return totalCost;
        }

        public static double NearestPointsCost(List<StateEstimate> trajectory1, List<StateEstimate> trajectory2)
        {
            //Calculate position costs
            var t1_first = trajectory1.First();
            var t1_last = trajectory1.Last();
            var t2_nearest_to_t1_first = NearestPointOnTrajectory(t1_first, trajectory2);
            var t2_nearest_to_t1_last = NearestPointOnTrajectory(t1_last, trajectory2);
            Console.WriteLine("t1_first.X:" + t1_first.X + " t1_first.Y:" + t1_first.Y);
            Console.WriteLine("t1_last.X:" + t1_last.X + " t1_last.Y:" + t1_last.Y);
            Console.WriteLine("t2_nearest_to_t1_first.X:" + t2_nearest_to_t1_first.X + " t2_nearest_to_t1_first.Y:" + t2_nearest_to_t1_first.Y);
            Console.WriteLine("t2_nearest_to_t1_last.X:" + t2_nearest_to_t1_last.X + " t2_nearest_to_t1_last.Y:" + t2_nearest_to_t1_last.Y);
            //1. Start position cost
            double startPositionCost = Distance(t1_first.X, t1_first.Y, t2_nearest_to_t1_first.X, t2_nearest_to_t1_first.Y);
            //2. End position cost
            double endPositionCost = Distance(t1_last.X, t1_last.Y, t2_nearest_to_t1_last.X, t2_nearest_to_t1_last.Y);

            var t2snipStart = trajectory2.IndexOf(t2_nearest_to_t1_first);
            var t2snipEnd = trajectory2.IndexOf(t2_nearest_to_t1_last);
            var t2snipIndex = (t2snipStart < t2snipEnd) ? t2snipStart : t2snipEnd;
            var t2snip = trajectory2.GetRange(t2snipIndex,Math.Abs(t2snipEnd - t2snipStart));

            var initialAngleCost = 2*Math.PI; //Assume worst-case (maximum angular difference) when snipped t2 is zero-length
            var finalAngleCost = 2 * Math.PI;

            if (t2snip.Count > 0)
            {
                //Calculate angle costs
                //3. Start angle cost
                initialAngleCost = CompareInitialTrajectoryAngles(trajectory1, t2snip);
                //4. End angle cost
                finalAngleCost = CompareFinalTrajectoryAngles(trajectory1, t2snip);
            }
            
            double totalCost = START_POSITION_MULTIPLIER*startPositionCost + END_POSITION_MULTIPLIER*endPositionCost + START_ANGLE_MULTIPLIER*initialAngleCost + END_ANGLE_MULTIPLIER*finalAngleCost;
            return totalCost;
        }

        private static TrajectoryVector InitialVector(List<StateEstimate> trajectory)
        {
            int sampleIndex = trajectory.Count / 5;
            TrajectoryVector v = new TrajectoryVector();
            var s1 = trajectory.First();
            var s2 = trajectory[sampleIndex];
            v.x = s2.X - s1.X;
            v.y = s2.Y - s1.Y;
            var magnitude = Math.Sqrt((v.x * v.x) + (v.y * v.y));
            if (magnitude < 0.0001)
            {
                v.x = 0.0;
                v.y = 0.0;
            }
            else
            {
                v.x = v.x / magnitude;
                v.y = v.y / magnitude;    
            }
            return v;
        }

        private static TrajectoryVector FinalVector(List<StateEstimate> trajectory)
        {
            TrajectoryVector v = new TrajectoryVector();
            int sampleIndex = trajectory.Count / 5;
            var s1 = trajectory[trajectory.Count - 1 - sampleIndex];
            var s2 = trajectory.Last();
            v.x = s2.X - s1.X;
            v.y = s2.Y - s1.Y;
            var magnitude = Math.Sqrt((v.x * v.x) + (v.y * v.y));
            if (magnitude < 0.0001)
            {
                v.x = 0.0;
                v.y = 0.0;
            }
            else
            {
                v.x = v.x / magnitude;
                v.y = v.y / magnitude;    
            }
            return v;
        }

        private static double CompareAngles(TrajectoryVector v1, TrajectoryVector v2)
        {
            var angle1 = Math.Atan2(v1.y, v1.x);
            var angle2 = Math.Atan2(v2.y, v2.x);
            var angle_diff = Math.Abs(angle1 - angle2);
            return angle_diff;
        }

        private static double CompareInitialTrajectoryAngles(List<StateEstimate> trajectory1, List<StateEstimate> trajectory2)
        {
            var iv1 = InitialVector(trajectory1);
            var iv2 = InitialVector(trajectory2);
            var angleDiff = CompareAngles(iv1, iv2);
            return angleDiff;
        }

        private static double CompareFinalTrajectoryAngles(List<StateEstimate> trajectory1, List<StateEstimate> trajectory2)
        {
            var iv1 = FinalVector(trajectory1);
            var iv2 = FinalVector(trajectory2);
            var angleDiff = CompareAngles(iv1, iv2);
            return angleDiff;
        }

        public static double Distance(double x1, double y1, double x2, double y2)
        {
            return Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
        }

        public static double DWTCost(List<StateEstimate> trajectory1, List<StateEstimate> trajectory2)
        {
            var series1x = trajectory1.Select(s => s.X).ToArray();
            var series1y = trajectory1.Select(s => s.Y).ToArray();
            

            var series2x = trajectory2.Select(s => s.X).ToArray();
            var series2y = trajectory2.Select(s => s.Y).ToArray();

            var seriesX = new SeriesVariable(series1x, series2x, "Series X", new CentralizationPreprocessor());
            var seriesY = new SeriesVariable(series1y, series2y, "Series Y", new CentralizationPreprocessor());

            var seriesArray = new SeriesVariable[]{seriesX, seriesY};

            var cost = new Dtw(seriesArray, DistanceMeasure.Euclidean, true, false, null, null, null).GetCost();
            return cost;
        }

        public static Movement BestMatchTrajectory(List<StateEstimate> matchTrajectory,
            List<Movement> trajectoryPrototypes, string classType)
        {
            var match = trajectoryPrototypes.First();
            var matchedTrajectories = new List<MatchTrajectory>();
            foreach (var tp in trajectoryPrototypes)
            {
                var mt = new MatchTrajectory();
                mt.StateEstimates = tp.StateEstimates;
                mt.Approach = tp.Approach;
                mt.Exit = tp.Exit;
                mt.TrafficObjectType = tp.TrafficObjectType;
                mt.TurnType = tp.TurnType;
                //mt.matchCost = DWTCost(matchTrajectory, mt.StateHistory);
                //mt.matchCost = EndpointCost(matchTrajectory, mt.StateEstimates);
                mt.matchCost = NearestPointsCost(matchTrajectory, mt.StateEstimates);

                //If we're comparing a person against a Car-type synthetic trajectory, treat this as an impossible match.
                if (classType.ToLower().Contains("person") && tp.TrafficObjectType == ObjectType.Car)
                {
                    mt.matchCost = double.PositiveInfinity;
                }

                //If we're comparing a non-person (possibly a Car, a Motorcycle, etc) against a Person-type synthetic trajectory, treat this as an impossible match.
                if (!classType.ToLower().Contains("person") && tp.TrafficObjectType == ObjectType.Person)
                {
                    mt.matchCost = double.PositiveInfinity;
                }    

                matchedTrajectories.Add(mt);
            }

            matchedTrajectories.Sort(new MatchTrajectoryComparer());
            match = matchedTrajectories.FirstOrDefault();
            return match;
        }

        static StateEstimate NearestPointOnTrajectory(StateEstimate point, List<StateEstimate> trajectory)
        {
            return trajectory.OrderBy(se => StateEstimatesDistance(point, se)).First();
        }

        static double StateEstimatesDistance(StateEstimate point1, StateEstimate point2)
        {
            var distance = Math.Sqrt(Math.Pow(point1.X - point2.X,2) + Math.Pow(point1.Y - point2.Y,2));
            return distance;
        }
    }

    class MatchTrajectory : Movement
    {
        public double matchCost;
    }

    class MatchTrajectoryComparer : IComparer<MatchTrajectory>
    {
        public int Compare(MatchTrajectory x, MatchTrajectory y)
        {
            return x.matchCost.CompareTo(y.matchCost);
        }
    }
}
