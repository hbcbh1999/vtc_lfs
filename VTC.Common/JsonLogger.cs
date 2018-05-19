using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace VTC.Common
{
    public static class JsonLogger<T> where T : new()
    {
        public static string ToJsonLogString(T obj)
        {
            var isDataContract = Attribute.IsDefined(typeof(T), typeof(DataContractAttribute));
            if (!isDataContract)
            {
                throw new Exception("JsonLogger recieved an object that does not implement the DataContract attribute.");
            }
            return JsonConvert.SerializeObject(obj);
        }

        public static T FromJsonString(string jsonString) 
        {
            var m = JsonConvert.DeserializeObject<T>(jsonString);
            return m;
        }
    }
}
