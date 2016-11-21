using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
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
            string showId = String.Empty;
            string filename = String.Empty;
            string appid = "f6924e79";
            string appkey = "05cb3378be72373bf0b3d5607c596b44";
            bool downloadAudio = true;
            bool downloadHdVideo = false;
            bool downloadLargeVideo = false;
            bool downloadSmallVideo = false;
            bool addYouTubeLink = false;
            string saveToParent = @"c:\twit\";

            // Show files
            string showMP3 = String.Empty;
            string showVideoHD = String.Empty;
            string showVideoLarge = String.Empty;
            string showVideoSmall = String.Empty;
            string showYouTube = String.Empty;

            // Additional security now files
            string snShowNotes = "https://www.grc.com/sn/sn-{0}-notes.pdf";
            string snTranscriptPdf = "https://www.grc.com/sn/sn-{0}.pdf";
            string snTranscriptText = "https://www.grc.com/sn/sn-{0}.txt";

            // Make sure there is at least a show id included
            if (args.Count() == 0)
            {
                Console.WriteLine("Missing required arguments.");
                return;
            }

            Arguments a = new Arguments(args);

            // Make sure -showid is in there
            if (String.IsNullOrEmpty(a["showid"]))
            {
                Console.WriteLine("Missing Show Id. Please include the parameter '-s <showid>'");
                return;
            }

            // Get the showid
            showId = a["showid"];

            // Save to location
            if (!String.IsNullOrEmpty(a["saveLoc"]))
            {
                saveToParent = a["saveLoc"];
            }

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("app-id", appid);
                client.DefaultRequestHeaders.Add("app-key", appkey);
                url = String.Format("http://twit.tv/api/v1.0/episodes?filter%5Bshows%5D={0}", showId);
                var getResponse = client.GetAsync(url).Result;
                string seeResponse = getResponse.Content.ReadAsStringAsync().Result; // For data validation
                dynamic jsonResponse = JsonConvert.DeserializeObject<dynamic>(getResponse.Content.ReadAsStringAsync().Result);

                foreach (dynamic episode in jsonResponse.episodes)
                {
                    Console.WriteLine("=========================");
                    Console.WriteLine(String.Format("Downloading episode {0}.", episode.episodeNumber));
                    // Create the folder
                    string folderName = String.Format("{0}{1}_{2}_{3}\\", saveToParent, episode.episodeNumber, episode.label, episode.airingDate.Value.ToString("yyyy-MM-dd"));
                    Directory.CreateDirectory(folderName);

                    // Download the files
                    using (var webClient = new WebClient())
                    {
                        // Download Audio File
                        if (downloadAudio)
                        {
                            filename = UrlHelper.GetFileName(episode.video_audio.mediaUrl.Value);
                            if (!File.Exists(String.Format("{0}{1}", folderName, filename)))
                            {
                                Console.WriteLine(String.Format("Downloading file '{0}'.", filename));
                                webClient.DownloadFile(episode.video_audio.mediaUrl.Value, String.Format("{0}{1}", folderName, filename));
                            }
                        }

                        // Download Security Now Shownotes
                        filename = UrlHelper.GetFileName(String.Format(snShowNotes, episode.episodeNumber));
                        if (!File.Exists(String.Format("{0}{1}", folderName, filename)))
                        {
                            Console.WriteLine(String.Format("Downloading file '{0}'.", filename));
                            webClient.DownloadFile(String.Format(snShowNotes, episode.episodeNumber), String.Format("{0}{1}", folderName, filename));
                        }

                        // Download Security Now Transcript PDF
                        filename = UrlHelper.GetFileName(String.Format(snTranscriptPdf, episode.episodeNumber));
                        if (!File.Exists(String.Format("{0}{1}", folderName, filename)))
                        {
                            Console.WriteLine(String.Format("Downloading file '{0}'.", filename));
                            webClient.DownloadFile(String.Format(snTranscriptPdf, episode.episodeNumber), String.Format("{0}{1}", folderName, filename));
                        }

                        // Download Security Now Transcript Text
                        filename = UrlHelper.GetFileName(String.Format(snTranscriptText, episode.episodeNumber));
                        if (!File.Exists(String.Format("{0}{1}", folderName, filename)))
                        {
                            Console.WriteLine(String.Format("Downloading file '{0}'.", filename));
                            webClient.DownloadFile(String.Format(snTranscriptText, episode.episodeNumber), String.Format("{0}{1}", folderName, filename));
                        }
                    }
                }
            }
        }
    }
}
