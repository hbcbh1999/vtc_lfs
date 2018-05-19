using System;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VTC.Common;
using VTC.Common.RegionConfig;
using VTC.Reporting.ReportItems;

namespace OptAssignTest
{


    //TODO: Either start using the built-in parser or delete this DataContract
    [DataContract]
    public class JsonStateEstimate
    {
        [DataMember]
        public string Vx { get; set; }

        [DataMember]
        public string Vy { get; set; }

        [DataMember]
        public string X { get; set; }

        [DataMember]
        public string Y { get; set; }
    }

    [TestClass]
    public class ConnectivityTest
    {
        [TestMethod]
        [Ignore]
        [Description("Send and read back state values from server in order to measure end-to-end loopback time.")]
        public void ServerLoopback()
        {

            GetSingleState(); // Warm up GetRequestStream() - JIT is slow

            DateTime testStart = DateTime.Now;
            int numReps = 10;
            int maxFailedChecks = 10;
            int failedChecks = 0;

            for (int i = 0; i < numReps; i++)
            {
                DateTime iterationStart = DateTime.Now;
                SendSingleVehicle(i,10);

                var result = GetSingleState();

                while (Math.Abs(result.X - i) > 0.001)
                {
                    result = GetSingleState();
                    if (failedChecks++ > maxFailedChecks)
                        break;
                }

                TimeSpan iterationSpan = DateTime.Now - iterationStart;
                Console.WriteLine("Single iteration time: " + iterationSpan);
            }

            TimeSpan loopbackTimeSpan = new TimeSpan( (DateTime.Now.Ticks - testStart.Ticks)/numReps );
            Console.WriteLine("Average loopback time: "+loopbackTimeSpan);

            //Each POST/GET cycle should take on average less than 5s
            Assert.IsTrue(loopbackTimeSpan < TimeSpan.FromSeconds(5));
        }

        private static void SendSingleVehicle(int x, int y)
        {
            RegionConfig regionConfig = new RegionConfig();


            StateEstimate[] stateEstimates = new StateEstimate[1];
            stateEstimates[0] = new StateEstimate
            {
                X = x,
                Y = y
            };
            string postString;

            string postUrl = HttpPostReportItem.PostStateString(stateEstimates, "1",
                regionConfig.ServerURL, out postString);
            HttpPostReportItem.SendStatePost(postUrl, postString);
        }

        private static StateEstimate GetSingleState()
        {
            var regionConfig = new RegionConfig();
            var sUrl = "http://" + regionConfig.ServerURL + "/intersections/newest/1.json";
            var wrGeturl = WebRequest.Create(sUrl);
            wrGeturl.Timeout = 10000;
            
            
            StateEstimate stateEstimate = new StateEstimate();

            try
            {
                using (var wresponse = wrGeturl.GetResponse())
                {
                    using(Stream objStream = wresponse.GetResponseStream())
                    {
                        if(objStream == null)
                            return stateEstimate;

                        StreamReader readStream = new StreamReader(objStream, Encoding.UTF8);
                        string response = readStream.ReadToEnd();

                        //TODO: use a library, not ugly manual JSON parsing
                        string trimmedResponse = response.Replace("\"", ""); //also replace [,{,],}
                        trimmedResponse = trimmedResponse.Replace("[", "");
                        trimmedResponse = trimmedResponse.Replace("]", "");
                        trimmedResponse = trimmedResponse.Replace("{", "");
                        trimmedResponse = trimmedResponse.Replace("}", "");
                        var commaMatches = Regex.Matches(trimmedResponse, ",");
                        string xySubstring = trimmedResponse.Substring(commaMatches[5].Index);
                        var numberMatches = Regex.Matches(xySubstring, "[0-9]?[0-9].[0-9]" ); //Matches 15.6 or 2.3, etc

                        string xString = xySubstring.Substring(numberMatches[0].Index, numberMatches[0].Length);
                        string yString = xySubstring.Substring(numberMatches[1].Index, numberMatches[1].Length);
                        //DataContractJsonSerializer serializer =
                        //new DataContractJsonSerializer(typeof(JsonStateEstimates));
                        //if (objStream == null)
                        //    throw (new Exception("Json response is null"));

                        //JsonStateEstimates loopbackJsonStateEstimates = (JsonStateEstimates)serializer.ReadObject(objStream);
                        stateEstimate.X = Convert.ToDouble(xString);
                        stateEstimate.Y = Convert.ToDouble(yString);

                        objStream.Close();
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("Exception in GetSingleState: " + e);
            }

            
            wrGeturl.Abort();
            
           
            return stateEstimate;
        }
    }
}
