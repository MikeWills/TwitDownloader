using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwitDownloader.Library
{
    public static class UrlHelper
    {
        public static string GetFileName(string url)
        {
            Uri uri = new Uri(url);
            if (uri.IsFile)
            {
                return System.IO.Path.GetFileName(uri.LocalPath);
            }

            return string.Empty;
        }
    }
}
