using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using NDtw;
using NDtw.Preprocessing;
using VTC.Common;

namespace VTC.Kernel
{
    public class TrajectoryVector
    {
        public double x;
        public double y;

        public double AngleRad()
        {
            return Math.Atan2(-y, x);
        }
    }

    public class TrajectorySimilarity
    {
        private const double POSITION_MULTIPLIER = 0.012;
        private const double ANGLE_MULTIPLIER = 0.01;
        private const double CURVATURE_MULTIPLIER = 400.0;

        public static Movement MatchNearestTrajectory(TrackedObject d, string classType, int minPathLength, List<Movement> trajectoryPrototypes)
        {
            //Heuristics for discarding garbage trajectories
            var distance = d.NetMovement();
            if (distance < minPathLength)
            {
                Console.WriteLine("Trajectory rejected: path too short (" + Math.Round(distance) + ")");
                return null; 
            }

            if (d.MissRatio() > 2.5)
            {
                Console.WriteLine("Trajectory rejected: miss ratio too high.");
                return null; 
            }

            if (d.FinalPositionCovariance() > 300.0)
            { 
                Console.WriteLine("Trajectory rejected: final position covariance too high.");
                return null; 
            }

            var matchedTrajectoryName = BestMatchTrajectory(d.StateHistory, trajectoryPrototypes, classType);
            return matchedTrajectoryName;
        }

        public static double PathIntegralCost(List<StateEstimate> trajectory1, List<StateEstimate> trajectory2)
        {
            var cost = 0.0;
            //Adjust the speed estimate for the first element of trajectory1.
            //This is necessary because MHT does not assign the speed of the object initially.
            trajectory1.First().Vx = trajectory1[1].Vx;
            trajectory1.First().Vy = trajectory1[1].Vy;

            for(int i=0;i<trajectory1.Count;i++)
            {
                var trajectory1StateEstimate = trajectory1[i];
                var trajectory2NearestStateEstimate = NearestPointOnTrajectory(trajectory1StateEstimate, trajectory2);

                var positionCost = Math.Sqrt(Math.Pow(trajectory1StateEstimate.X - trajectory2NearestStateEstimate.X,2) + Math.Pow(trajectory1StateEstimate.Y - trajectory2NearestStateEstimate.Y,2));

                TrajectoryVector iv1 = new TrajectoryVector();
                TrajectoryVector iv2 = new TrajectoryVector();

                iv1.x = trajectory1StateEstimate.Vx;
                iv1.y = trajectory1StateEstimate.Vy;

                iv2.x = trajectory2NearestStateEstimate.Vx;
                iv2.y = trajectory2NearestStateEstimate.Vy;

                var angle1 = Math.Atan2(-iv1.y, iv1.x);
                var angle2 = Math.Atan2(-iv2.y, iv2.x);

                var angleDiff = CompareAngles(iv1, iv2);
                var velocityMagnitude = Math.Sqrt(Math.Pow(iv1.x,2) + Math.Pow(iv1.y,2));
                var thisPointCost = POSITION_MULTIPLIER*positionCost + ANGLE_MULTIPLIER*angleDiff*velocityMagnitude;
                //Console.WriteLine("TotalCost," + thisPointCost + ",PositionCost," + positionCost + ",AngleCost," + angleDiff + ",Angle1," + iv1.AngleRad() + ",Angle2," + iv2.AngleRad() + ",Vx1," + iv1.x + ",Vy1," + iv1.y + ",Vx2," + iv2.x + ",Vy2," + iv2.y);
                cost += thisPointCost;
            }

            var curvature1 = Curvature(trajectory1);
            var curvature2 = Curvature(trajectory2);
            var curvatureDifference = Math.Abs(curvature1 - curvature2);
            cost += curvatureDifference*CURVATURE_MULTIPLIER;
            
            //Console.WriteLine("Final cost: " + cost);
            return cost;
        }

        public static string CostExplanation(List<StateEstimate> trajectory1, List<StateEstimate> trajectory2)
        {
            var explanation = "";
            var cost = 0.0;
            var totalAngleCost = 0.0;
            var totalPositionCost = 0.0;
            //Adjust the speed estimate for the first element of trajectory1.
            //This is necessary because MHT does not assign the speed of the object initially.
            trajectory1.First().Vx = trajectory1[1].Vx;
            trajectory1.First().Vy = trajectory1[1].Vy;

            for(int i=0;i<trajectory1.Count;i++)
            {
                var trajectory1StateEstimate = trajectory1[i];
                var trajectory2NearestStateEstimate = NearestPointOnTrajectory(trajectory1StateEstimate, trajectory2);

                var positionCost = Math.Sqrt(Math.Pow(trajectory1StateEstimate.X - trajectory2NearestStateEstimate.X,2) + Math.Pow(trajectory1StateEstimate.Y - trajectory2NearestStateEstimate.Y,2));
                totalPositionCost += positionCost;

                TrajectoryVector iv1 = new TrajectoryVector();
                TrajectoryVector iv2 = new TrajectoryVector();

                iv1.x = trajectory1StateEstimate.Vx;
                iv1.y = trajectory1StateEstimate.Vy;

                iv2.x = trajectory2NearestStateEstimate.Vx;
                iv2.y = trajectory2NearestStateEstimate.Vy;

                var angle1 = Math.Atan2(-iv1.y, iv1.x);
                var angle2 = Math.Atan2(-iv2.y, iv2.x);

                var angleDiff = CompareAngles(iv1, iv2);
                var velocityMagnitude = Math.Sqrt(Math.Pow(iv1.x,2) + Math.Pow(iv1.y,2));
                totalAngleCost += angleDiff * velocityMagnitude;
                var thisPointCost = POSITION_MULTIPLIER*positionCost + ANGLE_MULTIPLIER*angleDiff*velocityMagnitude;  
                cost += thisPointCost;
            }
            
            var curvature1 = Curvature(trajectory1);
            var curvature2 = Curvature(trajectory2);
            var curvatureDifference = Math.Abs(curvature1 - curvature2);
            explanation += "Curvature: " + Math.Round(curvature1,3) + ", compared-curvature: " + Math.Round(curvature2,3);
            explanation += ", Angle-cost: " + Math.Round(totalAngleCost,1) + ", Position-cost: " + Math.Round(totalPositionCost) + ", Curvature-cost: " + Math.Round(curvatureDifference,3);
            var weightedAngleCost = totalAngleCost*ANGLE_MULTIPLIER;
            var weightedPositionCost = totalPositionCost*POSITION_MULTIPLIER;
            var weightedCurvatureCost = curvatureDifference * CURVATURE_MULTIPLIER;
            explanation += ", Weighted-angle-cost: " + Math.Round(weightedAngleCost,1) + ", Weighted-position-cost: " + Math.Round(weightedPositionCost,1) + ", Weighted-curvature-cost: " + Math.Round(weightedCurvatureCost,1);
            return explanation;
        }

        public static double Curvature(List<StateEstimate> trajectory)
        { 
            var curvature = 0.0;
            var firstStateEstimate = trajectory.First();
            TrajectoryVector ivfirst = new TrajectoryVector();

            ivfirst.x = firstStateEstimate.Vx;
            ivfirst.y = firstStateEstimate.Vy;

            var lastAngle = Math.Atan2(-ivfirst.y, ivfirst.x);
            var maxVelocityMagnitude = 1.0;

            for(int i=0;i<trajectory.Count;i++)
            {
                var trajectoryStateEstimate = trajectory[i];
                //var velocityUncertainty = Math.Sqrt(Math.Pow(trajectoryStateEstimate.CovVx,2) + Math.Pow(trajectoryStateEstimate.CovVy,2))/100.0;
                var velocityMagnitude = Math.Sqrt(Math.Pow(trajectoryStateEstimate.Vx,2) + Math.Pow(trajectoryStateEstimate.Vy,2));
                if(velocityMagnitude > maxVelocityMagnitude)
                {
                    maxVelocityMagnitude = velocityMagnitude;
                }
                TrajectoryVector iv = new TrajectoryVector();
                iv.x = trajectoryStateEstimate.Vx;
                iv.y = trajectoryStateEstimate.Vy;
                var angle = Math.Atan2(-iv.y, iv.x);
                var angle_diff = MathHelper.WrapAngle(angle - lastAngle);
                curvature += angle_diff*velocityMagnitude;
                lastAngle = angle;
            }

            curvature /= Math.Pow(maxVelocityMagnitude,2); //Normalize by maximum velocity
            return curvature;
        }

        public static double CompareAngles(TrajectoryVector v1, TrajectoryVector v2)
        {
            var angle1 = Math.Atan2(-v1.y, v1.x);
            var angle2 = Math.Atan2(-v2.y, v2.x);
            var angle_diff = Math.Abs(MathHelper.WrapAngle(angle1 - angle2));
            return angle_diff;
        }

        public static double Distance(double x1, double y1, double x2, double y2)
        {
            return Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
        }

        public static Movement BestMatchTrajectory(List<StateEstimate> matchTrajectory,
            List<Movement> trajectoryPrototypes, string classType)
        {
            var match = trajectoryPrototypes.First();
            var matchedTrajectories = new ConcurrentBag<MatchTrajectory>();
           
            Parallel.ForEach(trajectoryPrototypes, (tp) =>
            {
                var mt = new MatchTrajectory(tp.Approach, tp.Exit, tp.TrafficObjectType, tp.TurnType,tp.StateEstimates, 0);
                bool isValidPersonMatch = classType.ToLower() == "person" && tp.TrafficObjectType == ObjectType.Person;
                bool isValidVehicleMatch = classType.ToLower() != "person" && tp.TrafficObjectType != ObjectType.Person;
                if(isValidPersonMatch || isValidVehicleMatch)
                {
                    //Console.WriteLine("Comparing " + mt.Approach + " to " + mt.Exit);
                    mt.matchCost = PathIntegralCost(matchTrajectory,mt.StateEstimates);
                    matchedTrajectories.Add(mt);
                }
            });

            var sortedMatchTrajectores = matchedTrajectories.ToList<MatchTrajectory>();
            sortedMatchTrajectores.Sort(new MatchTrajectoryComparer());
            match = sortedMatchTrajectores.FirstOrDefault();
            return match;
        }

        static StateEstimate NearestPointOnTrajectory(StateEstimate point, List<StateEstimate> trajectory)
        {
            return trajectory.OrderBy(se => StateEstimatesDistance(point, se)).First();
        }

        static StateEstimate MostSimilarPointOnTrajectory(StateEstimate point, List<StateEstimate> trajectory)
        {
            return trajectory.OrderBy(se => InertialDistance(point, se)).First();
        }

        static double StateEstimatesDistance(StateEstimate point1, StateEstimate point2)
        {
            var distance = Math.Sqrt(Math.Pow(point1.X - point2.X,2) + Math.Pow(point1.Y - point2.Y,2));
            return distance;
        }

        static double InertialDistance(StateEstimate point1, StateEstimate point2)
        {
            var positionDistance = Math.Sqrt(Math.Pow(point1.X - point2.X,2) + Math.Pow(point1.Y - point2.Y,2));
            
            TrajectoryVector iv1 = new TrajectoryVector();
            TrajectoryVector iv2 = new TrajectoryVector();
            iv1.x = point1.Vx;
            iv1.y = point1.Vy;
            iv2.x = point2.Vx;
            iv2.y = point2.Vy;

            var angleDiff = CompareAngles(iv1, iv2);
            var velocityMagnitude = Math.Sqrt(Math.Pow(iv1.x,2) + Math.Pow(iv1.y,2));
            var thisPointCost = POSITION_MULTIPLIER*positionDistance + ANGLE_MULTIPLIER*angleDiff*velocityMagnitude;  
            return thisPointCost;
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
