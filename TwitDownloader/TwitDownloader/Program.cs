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
            #region variable setup
            string url = String.Empty;
            string showId = String.Empty;
            string filename = String.Empty;
            DateTime lastEpisode = DateTime.MinValue;

            // API Key
            string appid = "f6924e79";
            string appkey = "05cb3378be72373bf0b3d5607c596b44";

            // These can be overridden by arguments
            bool downloadAudio = true;
            bool downloadHdVideo = false;
            bool downloadLargeVideo = false;
            bool downloadSmallVideo = false;
            bool addYouTubeLink = false;
            string saveToParent = @"c:\twit\";

            // Additional Security Now files
            string snShowNotes = "https://www.grc.com/sn/sn-{0}-notes.pdf";
            string snTranscriptPdf = "https://www.grc.com/sn/sn-{0}.pdf";
            string snTranscriptText = "https://www.grc.com/sn/sn-{0}.txt";
            #endregion

            #region Input Arguements
            // Make sure there is at least a show id included
            if (args.Count() == 0)
            {
                Console.WriteLine("Missing required arguments.");
                return;
            }

            Arguments a = new Arguments(args);

            // Read the show ID
            if (String.IsNullOrEmpty(a["showid"]))
            {
                Console.WriteLine("Missing '-showid ####'");
                return;
            }

            showId = a["showid"];

            // Change the default save location
            if (!String.IsNullOrEmpty(a["saveLoc"]))
            {
                saveToParent = a["saveLoc"];
            }
            #endregion

            using (var client = new HttpClient())
            {
                // =======================================
                // Get the child folder name (show name)
                client.DefaultRequestHeaders.Add("app-id", appid);
                client.DefaultRequestHeaders.Add("app-key", appkey);
                url = String.Format("http://twit.tv/api/v1.0/shows?filter%5Bid%5D={0}", showId);
                HttpResponseMessage getResponse = client.GetAsync(url).Result;
                string seeResponse = getResponse.Content.ReadAsStringAsync().Result; // For data validation
                dynamic jsonResponse = JsonConvert.DeserializeObject<dynamic>(getResponse.Content.ReadAsStringAsync().Result);

                string showName = jsonResponse.shows[0].label;
                saveToParent = String.Format("{0}{1}\\", saveToParent, jsonResponse.shows[0].label);

                // Open the log file that shows the last show downloaded
                string logFile = String.Format("{0}LastEpisode.txt", saveToParent);
                string errorLogFile = String.Format("{0}error.log", saveToParent);
                if (File.Exists(logFile))
                {
                    lastEpisode = DateTime.Parse(File.ReadAllText(logFile));
                }

                do
                {
                    // ========================================
                    // Get the episodes
                    url = String.Format("http://twit.tv/api/v1.0/episodes?filter%5Bshows%5D={0}&sort=airingDate", showId);
                    getResponse = client.GetAsync(url).Result;
                    seeResponse = getResponse.Content.ReadAsStringAsync().Result; // For data validation
                    jsonResponse = JsonConvert.DeserializeObject<dynamic>(getResponse.Content.ReadAsStringAsync().Result);

                    foreach (dynamic episode in jsonResponse.episodes)
                    {
                        if (episode.airingDate.Value <= lastEpisode)
                        {
                            continue;
                        }

                        Console.WriteLine("=========================");
                        Console.WriteLine(String.Format("Downloading {0} - Episode {1}.", showName, episode.episodeNumber));
                        // Create the folder
                        string folderName = String.Format("{0}{1}_{2}_{3}\\", saveToParent, Int32.Parse(episode.episodeNumber.Value).ToString("0000"), episode.label, episode.airingDate.Value.ToString("yyyy-MM-dd"));
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
                                    try
                                    {
                                        Console.WriteLine(String.Format("Downloading file '{0}'.", filename));
                                        webClient.DownloadFile(episode.video_audio.mediaUrl.Value, String.Format("{0}{1}", folderName, filename));
                                    }
                                    catch (WebException e)
                                    {
                                        File.AppendAllText(errorLogFile, episode.video_audio.mediaUrl.Value + " returned the error: " + e.Message + "\n");
                                    }
                                }
                            }

                            #region Security Now Only
                            // The following is only applicible for Security Now
                            if (showId == "1636")
                            {
                                // Download Security Now Shownotes
                                filename = UrlHelper.GetFileName(String.Format(snShowNotes, episode.episodeNumber));
                                if (!File.Exists(String.Format("{0}{1}", folderName, filename)))
                                {
                                    try
                                    {
                                        Console.WriteLine(String.Format("Downloading file '{0}'.", filename));
                                        webClient.DownloadFile(String.Format(snShowNotes, episode.episodeNumber), String.Format("{0}{1}", folderName, filename));
                                    }
                                    catch (WebException e)
                                    {
                                        File.AppendAllText(errorLogFile, String.Format(snShowNotes, episode.episodeNumber) + " returned the error: " + e.Message + "\n");
                                    }
                                }

                                // Download Security Now Transcript PDF
                                filename = UrlHelper.GetFileName(String.Format(snTranscriptPdf, episode.episodeNumber));
                                if (!File.Exists(String.Format("{0}{1}", folderName, filename)))
                                {
                                    try
                                    {
                                        Console.WriteLine(String.Format("Downloading file '{0}'.", filename));
                                        webClient.DownloadFile(String.Format(snTranscriptPdf, episode.episodeNumber), String.Format("{0}{1}", folderName, filename));
                                    }
                                    catch (WebException e)
                                    {
                                        File.AppendAllText(errorLogFile, String.Format(snShowNotes, episode.episodeNumber) + " returned the error: " + e.Message + "\n");
                                    }
                                }

                                // Download Security Now Transcript Text
                                filename = UrlHelper.GetFileName(String.Format(snTranscriptText, episode.episodeNumber));
                                if (!File.Exists(String.Format("{0}{1}", folderName, filename)))
                                {
                                    try
                                    {
                                        Console.WriteLine(String.Format("Downloading file '{0}'.", filename));
                                        webClient.DownloadFile(String.Format(snTranscriptText, episode.episodeNumber), String.Format("{0}{1}", folderName, filename));
                                    }
                                    catch (WebException e)
                                    {
                                        File.AppendAllText(errorLogFile, String.Format(snTranscriptText, episode.episodeNumber) + " returned the error: " + e.Message + "\n");
                                    }
                                }
                            }
                            #endregion

                            File.WriteAllText(logFile, episode.airingDate.Value.ToString());
                        }
                    }
                } while (String.IsNullOrEmpty(jsonResponse._links.next.href));
            }
        }
    }
}
