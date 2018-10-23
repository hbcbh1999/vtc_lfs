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
        private const double START_POSITION_MULTIPLIER = 1.0;
        private const double END_POSITION_MULTIPLIER = 1.0;
        private const double START_ANGLE_MULTIPLIER = 0.8;
        private const double END_ANGLE_MULTIPLIER = 0.5;

        private const double POSITION_MULTIPLIER = 0.1;
        private const double ANGLE_MULTIPLIER = 1.0;

        public static Movement MatchNearestTrajectory(TrackedObject d, string classType, int minPathLength, List<Movement> trajectoryPrototypes)
        {
            //Heuristics for discarding garbage trajectories
            var distance = d.NetMovement();
            if (distance < minPathLength) return null;

            if (d.MissRatio() > 2.0) return null;

            if (d.FinalPositionCovariance() > 300.0) return null;

            var matchedTrajectoryName = BestMatchTrajectory(d.StateHistory, trajectoryPrototypes, classType);
            return matchedTrajectoryName;
        }

        public static double EndpointCost(List<StateEstimate> trajectory1, List<StateEstimate> trajectory2, double start_position_multiplier, double end_position_multiplier, double start_angle_multiplier, double end_angle_multiplier)
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

        /// <summary>
        /// Translate trajectory so that it starts at coordinates (0,0)
        /// </summary>
        /// <param name="trajectory"></param>
        /// <returns></returns>
        private static List<StateEstimate> NormalizeTrajectory(List<StateEstimate> trajectory)
        {
            var initialState = trajectory[0];
            var normalizedTrajectory = trajectory.Select(stateEstimate => 
                new StateEstimate
                {
                    X = stateEstimate.X - initialState.X,
                    Y = stateEstimate.Y - initialState.Y,
                    Vx = stateEstimate.Vx,
                    Vy = stateEstimate.Vy
                }
            ).ToList();
            return normalizedTrajectory;
        }

        public static double PathIntegralCost(List<StateEstimate> trajectory1, List<StateEstimate> trajectory2)
        {
            //Trim trajectories:
            //Trim T2 from the start of T1, and trim T1 from the end of T2.
            //var t1_first = trajectory1.First();
            //var t2_last = trajectory2.Last();
            //var t2_nearest_to_t1_first = NearestPointOnTrajectory(t1_first, trajectory2);
            //var t1_nearest_to_t2_last = NearestPointOnTrajectory(t2_last, trajectory1);

            //var t2snipStart = trajectory2.IndexOf(t2_nearest_to_t1_first);
            //if(t2snipStart >= trajectory2.Count() - 1)
            //{ 
            //    t2snipStart = trajectory2.Count() - 2; 
            //}
            //var t2snip = trajectory2.GetRange(t2snipStart,trajectory2.Count - t2snipStart);

            //var t1snipEnd = trajectory1.IndexOf(t1_nearest_to_t2_last);
            //if(t1snipEnd == 0)
            //{
            //    t1snipEnd = 1;
            //}
            //var t1snip = trajectory1.GetRange(0,t1snipEnd);

            var cost = 0.0;

            for(int i=0;i<trajectory1.Count;i++)
            {
                var trajectory1StateEstimate = trajectory1[i];
                //Get nearest index in t2
                //var j = trajectory2.Count * ((double)i/trajectory1.Count);
                var trajectory2NearestStateEstimate = NearestPointOnTrajectory(trajectory1StateEstimate, trajectory2);

                var positionCost = Math.Sqrt(Math.Pow(trajectory1StateEstimate.X - trajectory2NearestStateEstimate.X,2) + Math.Pow(trajectory1StateEstimate.Y - trajectory2NearestStateEstimate.Y,2));

                TrajectoryVector iv1 = new TrajectoryVector();
                TrajectoryVector iv2 = new TrajectoryVector();

                iv1.x = trajectory1StateEstimate.Vx;
                iv1.y = trajectory1StateEstimate.Vy;

                iv2.x = trajectory2NearestStateEstimate.Vx;
                iv2.y = trajectory2NearestStateEstimate.Vy;

                var angleDiff = CompareAngles(iv1, iv2);
                
                cost += POSITION_MULTIPLIER*positionCost + ANGLE_MULTIPLIER*angleDiff;                
            }
            
            return cost;
        }

        public static double NearestPointsCost(List<StateEstimate> trajectory1, List<StateEstimate> trajectory2)
        {
            //Calculate position costs
            var t1_first = trajectory1.First();
            var t1_last = trajectory1.Last();
            var t2_nearest_to_t1_first = NearestPointOnTrajectory(t1_first, trajectory2);
            var t2_nearest_to_t1_last = NearestPointOnTrajectory(t1_last, trajectory2);
            //1. Start position cost
            double startPositionCost = Distance(t1_first.X, t1_first.Y, t2_nearest_to_t1_first.X, t2_nearest_to_t1_first.Y);
            //2. End position cost
            double endPositionCost = Distance(t1_last.X, t1_last.Y, t2_nearest_to_t1_last.X, t2_nearest_to_t1_last.Y);

            var t2snipStart = trajectory2.IndexOf(t2_nearest_to_t1_first);
            var t2snipEnd = trajectory2.IndexOf(t2_nearest_to_t1_last);
            var t2snipIndex = (t2snipStart < t2snipEnd) ? t2snipStart : t2snipEnd;
            var t2snip = trajectory2.GetRange(t2snipIndex,Math.Abs(t2snipEnd - t2snipStart));

            var initialAngleCost = Math.PI/2; //Assume average cost (average angular difference) when snipped t2 is zero-length because we can't really compare angles in this scenario
            var finalAngleCost = Math.PI/2;

            if (t2snip.Count > 3)
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

        public static string CostExplanation(List<StateEstimate> trajectory1, List<StateEstimate> trajectory2)
        {
            var explanation = "";

            //Calculate position costs
            var t1_first = trajectory1.First();
            var t1_last = trajectory1.Last();
            var t2_nearest_to_t1_first = NearestPointOnTrajectory(t1_first, trajectory2);
            var t2_nearest_to_t1_last = NearestPointOnTrajectory(t1_last, trajectory2);
            //1. Start position cost
            double startPositionCost = Distance(t1_first.X, t1_first.Y, t2_nearest_to_t1_first.X, t2_nearest_to_t1_first.Y);
            explanation += "Start-position cost: " + START_POSITION_MULTIPLIER*startPositionCost;
            //2. End position cost
            double endPositionCost = Distance(t1_last.X, t1_last.Y, t2_nearest_to_t1_last.X, t2_nearest_to_t1_last.Y);
            explanation += ", End-position cost: " + END_POSITION_MULTIPLIER*endPositionCost;

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

            explanation += ", Initial-angle cost: " + START_ANGLE_MULTIPLIER*initialAngleCost;
            explanation += ", Final-angle cost: " + END_ANGLE_MULTIPLIER*finalAngleCost;
            
            return explanation;
        }

        private static TrajectoryVector InitialVector(List<StateEstimate> trajectory)
        {
            int sampleIndex = trajectory.Count / 5;
            double distance_magnitude = 0.0;
            TrajectoryVector v = new TrajectoryVector();
            var s1 = trajectory.First();

            while(distance_magnitude < 25)
            {
                var s2 = trajectory[sampleIndex];
                v.x = s2.X - s1.X;
                v.y = s2.Y - s1.Y;
                distance_magnitude = Math.Sqrt(Math.Pow(Math.Abs(v.x),2) + Math.Pow(Math.Abs(v.y),2));
                if(sampleIndex < trajectory.Count - 1)
                {
                    sampleIndex++; 
                }
                else
                {
                    break; 
                }
            }

            if (distance_magnitude < 25)
            {
                v.x = 0.0;
                v.y = 0.0;
            }
            else
            {
                v.x = v.x / distance_magnitude;
                v.y = v.y / distance_magnitude;    
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
            TrajectoryVector iv1 = new TrajectoryVector();
            TrajectoryVector iv2 = new TrajectoryVector();
            double angleDiff;

            try
            {
                iv1 = InitialVector(trajectory1);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }

            try
            {
                iv2 = InitialVector(trajectory2);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }

            try
            {
                angleDiff = CompareAngles(iv1, iv2);
                return angleDiff;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }
            
            return Math.PI/2;
        }

        private static double CompareFinalTrajectoryAngles(List<StateEstimate> trajectory1, List<StateEstimate> trajectory2)
        {
            try
            {
                var iv1 = FinalVector(trajectory1);
                var iv2 = FinalVector(trajectory2);
                var angleDiff = CompareAngles(iv1, iv2);
                return angleDiff;
            }
            
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }

            return Math.PI/2;
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
                var mt = new MatchTrajectory(tp.Approach, tp.Exit, tp.TrafficObjectType, tp.TurnType,tp.StateEstimates, 0);
                bool isValidPersonMatch = classType.ToLower() == "person" && tp.TrafficObjectType == ObjectType.Person;
                bool isValidVehicleMatch = classType.ToLower() != "person" && tp.TrafficObjectType != ObjectType.Person;
                if(isValidPersonMatch || isValidVehicleMatch)
                {
                    //mt.matchCost = DWTCost(matchTrajectory, mt.StateHistory);
                    //mt.matchCost = EndpointCost(matchTrajectory, mt.StateEstimates, START_POSITION_MULTIPLIER, END_POSITION_MULTIPLIER, START_ANGLE_MULTIPLIER, END_ANGLE_MULTIPLIER);
                    //mt.matchCost = NearestPointsCost(matchTrajectory, mt.StateEstimates);
                    mt.matchCost = PathIntegralCost(matchTrajectory,mt.StateEstimates);
                    matchedTrajectories.Add(mt);
                }
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

    public class MatchTrajectory : Movement
    {
        public double matchCost;

        public MatchTrajectory(string approach, string exit, ObjectType ot, Turn turn, List<StateEstimate> stateEstimates, int frame) : base(approach,exit,turn,ot,stateEstimates, frame)
        { 
        }
    }

    class MatchTrajectoryComparer : IComparer<MatchTrajectory>
    {
        public int Compare(MatchTrajectory x, MatchTrajectory y)
        {
            return x.matchCost.CompareTo(y.matchCost);
        }
    }
}
