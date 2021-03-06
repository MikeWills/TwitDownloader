﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
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
            string lastPage = String.Empty;

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
            // -saveLoc:"c:\twit"
            if (!String.IsNullOrEmpty(a["saveLoc"]))
            {
                saveToParent = String.Format("{0}\\", a["saveLoc"]);
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
                    string[] parseFile = File.ReadAllText(logFile).Split('|');
                    lastEpisode = DateTime.Parse(parseFile[0]);
                    try
                    {
                        lastPage = parseFile[1];
                    }
                    catch (IndexOutOfRangeException e)
                    {
                        lastPage = "1";
                    }
                }

                do
                {
                    // ========================================
                    // Get the episodes
                    if (String.IsNullOrEmpty(lastPage))
                    {
                        url = String.Format("http://twit.tv/api/v1.0/episodes?filter%5Bshows%5D={0}&sort=airingDate&page=1", showId);
                    }
                    else
                    {
                        url = String.Format("http://twit.tv/api/v1.0/episodes?filter%5Bshows%5D={0}&sort=airingDate&page={1}", showId, lastPage.Trim());
                    }

                    getResponse = client.GetAsync(url).Result;
                    seeResponse = getResponse.Content.ReadAsStringAsync().Result; // For data validation
                    jsonResponse = JsonConvert.DeserializeObject<dynamic>(getResponse.Content.ReadAsStringAsync().Result);

                    int totalEpisodes = jsonResponse.episodes.Count;
                    int recNumber = 1;

                    foreach (dynamic episode in jsonResponse.episodes)
                    {
                        if (episode.airingDate.Value <= lastEpisode)
                        {
                            recNumber++;
                            if (recNumber > totalEpisodes)
                            {
                                lastPage = IncreasePageCount(lastPage);
                            }
                            continue;
                        }

                        Console.WriteLine("=========================");
                        Console.WriteLine(String.Format("Downloading {0} - Episode {1}.", showName, episode.episodeNumber));

                        // Create the folder
                        string episodeNumber = String.Empty;

                        try
                        {
                            episodeNumber = Int32.Parse(episode.episodeNumber.Value).ToString("0000");
                        }
                        catch (Exception)
                        {
                            episodeNumber = episode.episodeNumber.Value;
                        }

                        string folderName = String.Format("{0}{1}_{2}_{3}\\", saveToParent, episodeNumber, MakeValidFileName(episode.label.Value), episode.airingDate.Value.ToString("yyyy-MM-dd"));
                        Directory.CreateDirectory(folderName);
                        Console.WriteLine(String.Format("Created {0}", folderName));

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
                                string strEpisode = episode.episodeNumber.ToString().PadLeft(3,'0');
                                filename = UrlHelper.GetFileName(String.Format(snShowNotes, strEpisode));
                                if (!File.Exists(String.Format("{0}{1}", folderName, filename)))
                                {
                                    try
                                    {
                                        Console.WriteLine(String.Format("Downloading file '{0}'.", filename));
                                        webClient.DownloadFile(String.Format(snShowNotes, strEpisode), String.Format("{0}{1}", folderName, filename));
                                    }
                                    catch (WebException e)
                                    {
                                        File.AppendAllText(errorLogFile, String.Format(snShowNotes, strEpisode) + " returned the error: " + e.Message + "\n");
                                    }
                                }

                                // Download Security Now Transcript PDF
                                filename = UrlHelper.GetFileName(String.Format(snTranscriptPdf, strEpisode));
                                if (!File.Exists(String.Format("{0}{1}", folderName, filename)))
                                {
                                    try
                                    {
                                        Console.WriteLine(String.Format("Downloading file '{0}'.", filename));
                                        webClient.DownloadFile(String.Format(snTranscriptPdf, strEpisode), String.Format("{0}{1}", folderName, filename));
                                    }
                                    catch (WebException e)
                                    {
                                        File.AppendAllText(errorLogFile, String.Format(snTranscriptPdf, strEpisode) + " returned the error: " + e.Message + "\n");
                                    }
                                }

                                // Download Security Now Transcript Text
                                filename = UrlHelper.GetFileName(String.Format(snTranscriptText, strEpisode));
                                if (!File.Exists(String.Format("{0}{1}", folderName, filename)))
                                {
                                    try
                                    {
                                        Console.WriteLine(String.Format("Downloading file '{0}'.", filename));
                                        webClient.DownloadFile(String.Format(snTranscriptText, strEpisode), String.Format("{0}{1}", folderName, filename));
                                    }
                                    catch (WebException e)
                                    {
                                        File.AppendAllText(errorLogFile, String.Format(snTranscriptText, strEpisode) + " returned the error: " + e.Message + "\n");
                                    }
                                }
                            }
                            #endregion

                            Uri parseUrl = new Uri(url);
                            lastPage = HttpUtility.ParseQueryString(parseUrl.Query).Get("page");
                            File.WriteAllText(logFile, String.Format("{0} | {1}", episode.airingDate.Value.ToString(), lastPage));
                        }

                        recNumber++;

                        if (recNumber > totalEpisodes)
                        {
                            lastPage = IncreasePageCount(lastPage);
                        }

                    }
                } while (jsonResponse._links.next != null && !String.IsNullOrEmpty(jsonResponse._links.next.href.Value));
            }
        }

        /// <summary>
        /// Get the next page of episodes
        /// </summary>
        /// <param name="lastPage"></param>
        /// <returns></returns>
        private static string IncreasePageCount(string lastPage)
        {
            return (Int32.Parse(lastPage) + 1).ToString();
        }

        /// <summary>
        /// Make sure the episode makes a valid filename
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private static string MakeValidFileName(string name)
        {
            string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

            return Regex.Replace(name, invalidRegStr, "_");
        }

        /// <summary>
        /// Create a shortcut link
        /// </summary>
        /// <param name="linkName"></param>
        /// <param name="linkUrl"></param>
        private void urlShortcutToDesktop(string folder, string linkName, string linkUrl)
        {
            using (StreamWriter writer = new StreamWriter(folder + "\\" + linkName + ".url"))
            {
                writer.WriteLine("[InternetShortcut]");
                writer.WriteLine("URL=" + linkUrl);
                writer.Flush();
            }
        }
    }
}
