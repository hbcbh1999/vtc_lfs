﻿using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using VTC.Common;

namespace VTC.Common
{
    public class MovementCount : Dictionary<Movement,long>
    {

    }

    [DataContract]
    public class Movement : IComparable<Movement>
    {
        [DataMember] public string Approach;

        [DataMember] public string Exit;

        [DataMember] [JsonConverter(typeof(StringEnumConverter))]
        public Turn TurnType;

        [DataMember] [JsonConverter(typeof(StringEnumConverter))]
        public ObjectType TrafficObjectType;

        [DataMember] public List<StateEstimate> StateEstimates;

        [DataMember] public DateTime Timestamp;

        public Movement(string approach, string exit, Turn turn, ObjectType objectType, List<StateEstimate> stateEstimates)
        {
            Approach = approach;
            Exit = exit;
            TurnType = turn;
            TrafficObjectType = objectType;
            StateEstimates = stateEstimates;
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
            return other.Approach == Approach && other.Exit == Exit && other.TrafficObjectType == TrafficObjectType &&
                   other.TurnType == TurnType;
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public int CompareTo(Movement that)
        {
            return ToString().CompareTo(that.ToString());
        }
    }
}