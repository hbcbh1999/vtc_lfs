using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using VTC.Common;

namespace VTC.Reporting.ReportItems
{
    public class HttpPostReportItem
    {

        public static void SendStatePost(string postUrl, string postString)
        {
                var objRequest = (HttpWebRequest)WebRequest.Create(postUrl);
                objRequest.KeepAlive = true;
                objRequest.Pipelined = true;
                objRequest.Timeout = 2000;
                objRequest.Method = "POST";
                objRequest.ContentLength = postString.Length;
                objRequest.ContentType = "application/x-www-form-urlencoded";
                objRequest.Proxy = null;

                //Trying different things to prevent so many timeouts:
                objRequest.KeepAlive = false;
                ServicePointManager.Expect100Continue = false;

                //// post data is sent as a stream
                using(var myWriter = new StreamWriter(objRequest.GetRequestStream()))
                {
                    myWriter.Write(postString);
                }
                objRequest.GetResponse().Close();
                objRequest.Abort();   
        }

        public static string PostStateString(StateEstimate[] stateEstimates, string intersectionId, string serverUrl, out string postString)
        {
            var postValues = new Dictionary<string, string>();
            for (var vehicleCount = 0; vehicleCount < stateEstimates.Length; vehicleCount++)
            {
                var x = stateEstimates[vehicleCount].X.ToString(CultureInfo.InvariantCulture);
                var y = stateEstimates[vehicleCount].Y.ToString(CultureInfo.InvariantCulture);
                var vx = stateEstimates[vehicleCount].Vx.ToString(CultureInfo.InvariantCulture);
                var vy = stateEstimates[vehicleCount].Vy.ToString(CultureInfo.InvariantCulture);
                const string zero = "0";
                postValues.Add("state_sample[states_attributes][" + vehicleCount + "][x]", x);
                postValues.Add("state_sample[states_attributes][" + vehicleCount + "][vx]", vx);
                postValues.Add("state_sample[states_attributes][" + vehicleCount + "][y]", y);
                postValues.Add("state_sample[states_attributes][" + vehicleCount + "][vy]", vy);
                postValues.Add("state_sample[states_attributes][" + vehicleCount + "][_destroy]", zero);
            }

            if (stateEstimates.Length == 0)
                postValues.Add("state_sample[states_attributes][]", "");

            postValues.Add("intersection_id", intersectionId);


            postString = postValues.Aggregate("", (current, postValue) => current + (postValue.Key + "=" + HttpUtility.UrlEncode(postValue.Value) + "&"));
            postString = postString.TrimEnd('&');

            //Upload state to server
            var postUrl = "http://" + serverUrl + "/state_samples";
            return postUrl;
        }
    }
}
