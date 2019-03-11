using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VTC.Common;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;

namespace VTC
{
    public class RemoteServer
    {
        const string MovementsRoute = "/movements";
        const string SitesRoute = "/sites/";
        private static readonly HttpClient Client = new HttpClient();

        public async Task<HttpStatusCode> SendMovement(Movement movement, string site, string serverUrl)
        { 
            //Get remote server
            var createMovementUrl = serverUrl + MovementsRoute;
            var jsonString = "{ \"movement\": { \"site_id\": \"" + site + "\", \"approach\": \"" + movement.Approach + "\", \"exit\": \"" + movement.Exit + "\", \"turntype\": \"" + movement.TurnType + "\", \"objectclass\": \"" + movement.TrafficObjectType + "\"} }";
            var content = new StringContent(jsonString, Encoding.UTF8, "application/json");
            
            //Transmit
            var response = await Client.PostAsync(createMovementUrl, content);
            return response.StatusCode;
        }

        public async Task<HttpStatusCode> SendImage(Image image, string site, string serverUrl)
        { 
            //Get remote server
            var uploadImageUrl = serverUrl + SitesRoute + site;
            var mp = new MultipartFormDataContent();

            var stream = new MemoryStream();
            image.Save(stream,image.RawFormat);
            var imageBytes = stream.ToArray();
            var byteContent = new ByteArrayContent(imageBytes);
            //var streamContent = new StreamContent(stream);
            mp.Add(byteContent, "site[image]", "image.png");
            
            //Transmit
            var response = await Client.PutAsync(uploadImageUrl, mp);
            return response.StatusCode;
        }

    }
}

