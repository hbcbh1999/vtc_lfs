using System;
using System.Collections.Generic;
using System.Linq;

namespace VTC.Common
{
    public struct TrackedObject
    {
        public string ObjectType;
        public List<StateEstimate> StateHistory;

        public TrackedObject(StateEstimate initialState)
        {
            StateHistory = new List<StateEstimate> {initialState};
            ObjectType = "unknown";
        }

        public TrackedObject(IEnumerable<StateEstimate> stateHistoryOld, StateEstimate currentState)
        {
            StateHistory = new List<StateEstimate>(stateHistoryOld) {currentState};
            ObjectType = "unknown";
        }

        public double DistanceTravelled()
        {
            double distance_integral = 0;
            StateEstimate current_position = StateHistory.First();
            for (int i = 0; i < StateHistory.Count(); i++)
            {
                var next_position = StateHistory.ElementAt(i);
                var x_increment = next_position.X - current_position.X;
                var y_increment = next_position.Y - current_position.Y;
                var total_increment = Math.Sqrt(Math.Pow(x_increment, 2) + Math.Pow(y_increment, 2));
                distance_integral += total_increment;
            }

            //StateEstimate initialPosition = StateHistory.First();
            //StateEstimate finalPosition = StateHistory.Last();
            //var distance = Math.Sqrt(Math.Pow(finalPosition.X - initialPosition.X,  2) + Math.Pow(finalPosition.Y - initialPosition.Y,2));
            //return distance;

            return distance_integral;
        }
    }
}