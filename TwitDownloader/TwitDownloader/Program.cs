using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TwitDownloader.Library;

namespace TwitDownloader
{
    class Program
    {
        static void Main(string[] args)
        {
            string url = String.Empty;
            string appid = "f6924e79";
            string appkey = "05cb3378be72373bf0b3d5607c596b44";
            string showId = String.Empty;

            // Make sure there is at least a show id included
            if (args.Count() == 0)
            {
                Console.WriteLine("Missing required arguments.");
                return;
            }

            Arguments a = new Arguments(args);

            // Make sure -s is in there
            if (String.IsNullOrEmpty(a["s"]))
            {
                Console.WriteLine("Missing Show Id. Please include the parameter '-s <showid>'");
                return;
            }

            // Get the showid
            showId = a["s"];

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("app-id", appid);
                client.DefaultRequestHeaders.Add("app-key", appkey);
                url = String.Format("http://twit.tv/api/v1.0/episodes?filter%5Bshows%5D={0}", showId);
                var getResponse = client.GetAsync(url).Result;
                string seeResponse = getResponse.Content.ReadAsStringAsync().Result; // For data validation
                dynamic jsonResponse = JsonConvert.DeserializeObject<dynamic>(getResponse.Content.ReadAsStringAsync().Result);


            }
        }
    }
}
