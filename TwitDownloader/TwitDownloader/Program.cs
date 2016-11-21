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
            string appid = String.Empty;
            string appkey = String.Empty;
            string showId = String.Empty;

            Arguments a = new Arguments(args);

            if (!String.IsNullOrEmpty(a["s"]))
            {
                Console.WriteLine("Missing Show Id. Please include the parameter '-s <showid>'");
                return;
            }

            showId = a["s"];
            showId = "1636";

            using (HttpClientHandler handler = new HttpClientHandler { AllowAutoRedirect = true, UseCookies = true, CookieContainer = new CookieContainer() })
            {
                using (var client = new HttpClient(handler))
                {
                    url = String.Format("http://twit.tv/api/v1.0/episodes?filter%5Bshows%5D={0}", showId);
                    var getResponse = client.GetAsync(url).Result;
                    string seeResponse = getResponse.Content.ReadAsStringAsync().Result; // For data validation
                    dynamic jsonResponse = JsonConvert.DeserializeObject<dynamic>(getResponse.Content.ReadAsStringAsync().Result);
                }
            }
        }
    }
}
