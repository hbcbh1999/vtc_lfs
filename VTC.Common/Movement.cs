using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using VTC.Common;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Npgsql;

namespace VTC.Common
{
    public class MovementCount : Dictionary<Movement,long>
    {
        public long TotalCount()
        {
            long count = 0;
            foreach(var mc in this)
            {
                count += mc.Value;
            }
            return count;
        }
    }

    [DataContract]
    public class Movement : IComparable<Movement>
    {
        [DataMember] public readonly string Approach;

        [DataMember] public readonly string Exit;

        [DataMember] [JsonConverter(typeof(StringEnumConverter))]
        public readonly Turn TurnType;

        [DataMember] [JsonConverter(typeof(StringEnumConverter))]
        public readonly ObjectType TrafficObjectType;

        [DataMember] public StateEstimateList StateEstimates;

        [DataMember] public readonly DateTime Timestamp;

        [DataMember] public int FirstDetectionFrame;

        [DataMember] public bool Ignored;

        [DataMember] public int JobId;

        [DataMember] public bool Synthetic;

        [DataMember] public int Id;

        public Movement(string approach, string exit, Turn turn, ObjectType objectType, StateEstimateList stateEstimates, DateTime timeStamp, bool ignored, int job = 0)
        {
            Approach = approach;
            Exit = exit;
            TurnType = turn;
            TrafficObjectType = objectType;
            StateEstimates = stateEstimates;
            Timestamp = timeStamp;
            Ignored = ignored;
            JobId = job;
        }

        public Movement()
        {
        }

        public override string ToString()
        {
            var s = Approach + " to " + Exit + ", " + Enum.GetName(typeof(Turn),TurnType) + ", " + Enum.GetName(typeof(ObjectType),TrafficObjectType);
            return s;
        }

        public override bool Equals(object obj)
        {
            var typeThat = obj.GetType();
            if(typeThat != typeof(Movement) && !typeThat.IsSubclassOf(typeof(Movement)))
                return false;

            if (obj == null)
                return false;

            var other = (Movement) obj;
            return other.Approach == Approach && other.Exit == Exit;
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public int CompareTo(Movement that)
        {
            return ToString().CompareTo(that.ToString());
        }

        public double MissRatio()
        {
            var missRatio = (double) StateEstimates.Sum(se => se.TotalMissedDetections) / StateEstimates.Count();
            return missRatio;
        }

        public void Save(NpgsqlConnection dbConnection)
        {
            try
            {
                var result = new NpgsqlCommand(
                    $"INSERT INTO public.movement(jobid,approach,exit,turntype,trafficobjecttype,synthetic,ignored) VALUES({JobId},'{Approach}','{Exit}','{TurnType}','{TrafficObjectType}',{Synthetic},{Ignored}) RETURNING id",
                    dbConnection).ExecuteScalar();
                
                Id = int.Parse(result.ToString());

                foreach (var s in StateEstimates)
                {
                    s.MovementId = Id;
                    s.Save(dbConnection);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Movement.Save:" + e.Message);
            }
        }
    }
}