using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace PornhubPhotoDownloader
{
    class Program
    {
        const int SETTING_RETRYCOUNT = 5;

        const string ARG_HELP = "?";
        const string ARG_DOWNLOADDIR = "dir";
        const string ARG_ALL = "all";
        const string ARG_STARTINDEX = "index";
        const string ARG_LENGTH = "length";
        const string ARG_DEBUG = "debug";
        const string ARG_DEBUG_INFO = "info";
        const string ARG_LANG = "lang";
        const string ARG_RENAME = "rename";

        const string REPLACE_DOWNLOADDIR_ALBUM = "{album}";
        const string REPLACE_DOWNLOADDIR_ID = "{id}";
        const string REPLACE_DOWNLOADDIR_PAGE = "{page}";

        const string REPLACE_PHOTO_INDEX = "{index}";
        const string REPLACE_PHOTO_ID = "{id}";
        const string REPLACE_PHOTO_ALBUM_ID = "{albumid}";
        const string REPLACE_PHOTO_ALBUM = "{album}";
        const string REPLACE_PHOTO_EXT = "{ext}";

        static WebClient WebClient = new WebClient();
        static Dictionary<string, string> Args = new Dictionary<string, string>();
        static bool IsDebug;
        static bool IsDebugInfo;
        static string DefaultDownloadDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Downloads\\{id}");

        static void Main(string[] args)
        {
#if TEST_ARGS
            args = new string[]
            {
                "https://cn.pornhub.com/album/48071401",
                "-all",
                "-debug",
            };
#endif
            #region Parse Args
            foreach (string arg in args)
            {
                if (arg[0] == '-' || arg[0] == '/')
                {
                    int index = arg.IndexOf(":");
                    string key = arg.Substring(1, index == -1 ? arg.Length - 1 : index - 1).ToLower();
                    string value = arg.Substring(index + 1, arg.Length - index - 1);

                    if (Args.ContainsKey(key))
                    {
                        Args.Remove(key);
                    }

                    Args.Add(key, value);
                }
            }

            if (Args.ContainsKey(ARG_LANG))
            {
                try
                {
                    Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(Args[ARG_LANG]);
                }
                catch (Exception e)
                {
                    ThrowException(e);
                    Console.WriteLine();
                    return;
                }
            }

            if (args.Length < 1 || Args.ContainsKey(ARG_HELP))
            {
                Console.WriteLine(string.Format("{0}", Properties.Resources.Msg_Help_Use));
                Console.WriteLine();
                Console.WriteLine(string.Format("   -All                {0}", Properties.Resources.Msg_Help_All));
                Console.WriteLine(string.Format("   -Dir:<path>         {0}", Properties.Resources.Msg_Help_DownloadDir));
                Console.WriteLine(string.Format("   -Index:<value>      {0}", Properties.Resources.Msg_Help_StartIndex));
                Console.WriteLine(string.Format("   -Length:<value>     {0}", Properties.Resources.Msg_Help_Length));
                Console.WriteLine(string.Format("   -Debug              {0}", Properties.Resources.Msg_Help_Debug));
                Console.WriteLine(string.Format("   -Info               {0}", Properties.Resources.Msg_Help_DebugInfo));
                Console.WriteLine(string.Format("   -Rename:<pattern>   {0}", Properties.Resources.Msg_Help_Rename));
                Console.WriteLine();
                Console.WriteLine(Properties.Resources.Msg_Help_Rename_Keywords);
                Console.WriteLine();
                Console.WriteLine(Properties.Resources.Msg_Help_DownloadDir_Keywords);
                Console.WriteLine();
                return;
            }

            if (!Args.ContainsKey(ARG_DOWNLOADDIR))
            {
                Args.Add(ARG_DOWNLOADDIR, DefaultDownloadDir);
            }
            if (string.IsNullOrWhiteSpace(Args[ARG_DOWNLOADDIR]))
            {
                Args[ARG_DOWNLOADDIR] = DefaultDownloadDir;
            }

            IsDebug = Args.ContainsKey(ARG_DEBUG);
            IsDebugInfo = Args.ContainsKey(ARG_DEBUG_INFO);

            if (!LinkInfo.TryParse(args[0], out LinkInfo argLink))
            {
                if (IsDebug)
                {
                    Console.WriteLine(string.Format(Properties.Resources.Msg_InvalidLink, args[0]));
                }
                Console.WriteLine();
                return;
            }
            #endregion

            #region Console Title
            Console.Title = $"{Properties.Resources.Title}: {getArgsString(args)}";
            string getArgsString(string[] _args)
            {
                string result = string.Empty;
                foreach (string _arg in _args)
                {
                    result += $"{_arg} ";
                }
                return result.Trim();
            }
            #endregion

            try
            {
                if (argLink.Type.ToLower() == "album")
                {
                    Regex loadingRegex = new Regex("<body onload=\"go\\(\\)\">\\s*Loading \\.\\.\\.\\s*</body>");
                    if (Args.ContainsKey(ARG_ALL))
                    {
                        Regex corruptRegex = new Regex("<div class=\"photoBlockBox\">\\s*<p class=\"corruptMessage\">[\\S\\s]*</p>");
                        int pageIndex = 1;
                        argLink.Args = $"?page={pageIndex}";
                        Console.WriteLine(string.Format(Properties.Resources.Msg_Album_Download, argLink.FullUrl));
                        string albumHtmlText = GetHtmlText(argLink.FullUrl);

                        while (!corruptRegex.IsMatch(albumHtmlText) || loadingRegex.IsMatch(albumHtmlText))
                        {
                            if (loadingRegex.IsMatch(albumHtmlText))
                            {
                                if (IsDebug)
                                {
                                    Console.ForegroundColor = ConsoleColor.Yellow;
                                    Console.WriteLine($"Regex '{loadingRegex}' is matched. Retrying in 5 seconds.");
                                    Console.ResetColor();
                                }

                                Thread.Sleep(5000);
                                albumHtmlText = GetHtmlText(argLink.FullUrl);
                                continue;
                            }

                            DownloadAlbum(albumHtmlText, argLink, GetDownloadDir(argLink, albumHtmlText, pageIndex.ToString()));
                            pageIndex++;
                            argLink.Args = $"?page={pageIndex}";
                            Console.WriteLine(string.Format(Properties.Resources.Msg_Album_Download, argLink.FullUrl));
                            albumHtmlText = GetHtmlText(argLink.FullUrl);
                        }
                        ConsoleBackLine(); //Empty page.
                    }
                    else
                    {
                        Console.WriteLine(string.Format(Properties.Resources.Msg_Album_Download, argLink.FullUrl));
                        string albumHtmlText = GetHtmlText(argLink.FullUrl);

                        while (loadingRegex.IsMatch(albumHtmlText))
                        {
                            if (IsDebug)
                            {
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine($"Regex '{loadingRegex}' is matched. Retrying in 5 seconds.");
                                Console.ResetColor();
                            }

                            Thread.Sleep(5000);
                            albumHtmlText = GetHtmlText(argLink.FullUrl);
                        }

                        DownloadAlbum(albumHtmlText, argLink,
                            GetDownloadDir(argLink, albumHtmlText, "1"),
                            Args.ContainsKey(ARG_STARTINDEX) ? Convert.ToInt32(Args[ARG_STARTINDEX]) : 0,
                            Args.ContainsKey(ARG_LENGTH) ? Convert.ToInt32(Args[ARG_LENGTH]) : 0);
                    }
                }
                else if (argLink.Type.ToLower() == "photo")
                {
                    DownloadPhoto(argLink, GetDownloadDir(argLink, string.Empty));
                }
            }
            catch (Exception e)
            {
                ThrowException(e);
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(Properties.Resources.Msg_AllDone);
            Console.ResetColor();
            Console.WriteLine();
        }

        static string GetDownloadDir(LinkInfo link, string htmlText, string pageIndex = "0")
        {
            string albumName = $"{link.Type}-{link.ID}";
            Regex albumNameRegex = new Regex("<h1 class=\"photoAlbumTitleV2\">\\s*(?<album>[\\S\\s].*?)\\s*<span .*?></span>\\s*</h1>");
            if (albumNameRegex.IsMatch(htmlText))
            {
                albumName = albumNameRegex.Match(htmlText).Groups["album"].Value;
            }

            string result = Args[ARG_DOWNLOADDIR]
                .Replace(REPLACE_DOWNLOADDIR_ALBUM, albumName)
                .Replace(REPLACE_DOWNLOADDIR_ID, link.ID)
                .Replace(REPLACE_DOWNLOADDIR_PAGE, pageIndex);
            foreach (char ch in Path.GetInvalidPathChars())
            {
                result = result.Replace(ch.ToString(), string.Empty);
            }

            return result;
        }

        static void DownloadAlbum(string albumHtmlText, LinkInfo albumLink, string downloadDir, int startIndex = 0, int length = 0)
        {
            List<LinkInfo> photoLinks = new List<LinkInfo>();
            Regex regex = new Regex("<a href=\"(?<photoLink>/photo/[0-9]+)\">");
            foreach (Match match in regex.Matches(albumHtmlText))
            {
                LinkInfo photoLink = LinkInfo.Parse($"{albumLink.Host}{match.Groups["photoLink"].Value}");

                if (photoLink != null)
                {
                    photoLinks.Add(photoLink);
                }
            }

            DownloadPhotos(photoLinks.ToArray(), downloadDir, startIndex, length);
        }

        static void DownloadPhoto(LinkInfo photoLink, string downloadDir)
        {
            DownloadPhotos(new LinkInfo[] { photoLink }, downloadDir);
        }

        static void DownloadPhotos(LinkInfo[] photoLinks, string downloadDir, int startIndex = 0, int length = 0)
        {
            int retryCount = 0;
            int _startIndex = (startIndex < 0 || startIndex >= photoLinks.Length) ? 0 : startIndex;
            int _length = (length + _startIndex < 1 || length + _startIndex > photoLinks.Length) ? photoLinks.Length : length + _startIndex;

            if (IsDebug)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"StartIndex: {_startIndex}({startIndex})    Length: {_length}({length})    Count: {photoLinks.Length}");
                Console.WriteLine($"Dir: {Path.GetFullPath(downloadDir)}");
                Console.ResetColor();
            }

            if (!Directory.Exists(downloadDir))
            {
                Directory.CreateDirectory(downloadDir);
                if (IsDebug)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Dir created!");
                    Console.ResetColor();
                }
            }

            for (int i = _startIndex; i < _length; i++)
            {
                LinkInfo photoLink = photoLinks[i];
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine(string.Format(
                    $"{(retryCount == 0 ? $"{Properties.Resources.Msg_Photo_Download}" : $"{Properties.Resources.Msg_Photo_Download_Retrying}")}",
                    photoLink.FullUrl, i + 1, photoLinks.Length, retryCount));
                Console.ResetColor();
                try
                {
                    string photoHtmlText = GetHtmlText(photoLink.FullUrl);
                    string photoSourceUrl = GetPhotoSourceUrl(photoHtmlText);
                    //TODO: Loading page match.
                    ConsoleBackLine();
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine(string.Format(Properties.Resources.Msg_Photo_Downloading, Path.GetFileName(photoSourceUrl), i + 1, photoLinks.Length));
                    Console.ResetColor();

                    string fileName = Path.GetFileName(photoSourceUrl);
                    if (Args.ContainsKey(ARG_RENAME) && !string.IsNullOrWhiteSpace(Args[ARG_RENAME]))
                    {
                        Regex fromAlbumRegex = new Regex("<a href=\"/album/(?<album_id>[0-9]+)\">(?<album>.*?)</a>");
                        Match fromAlbumMatch = fromAlbumRegex.Match(photoHtmlText);
                        string r_index = i.ToString();
                        string r_id = photoLink.ID;
                        string r_albumid = fromAlbumMatch.Groups["album_id"].Value;
                        string r_album = fromAlbumMatch.Groups["album"].Value;
                        string r_ext = Path.GetExtension(photoSourceUrl);

                        fileName = Args[ARG_RENAME]
                            .Replace(REPLACE_PHOTO_INDEX, r_index)
                            .Replace(REPLACE_PHOTO_ID, r_id)
                            .Replace(REPLACE_PHOTO_ALBUM_ID, r_albumid)
                            .Replace(REPLACE_PHOTO_ALBUM, r_album)
                            .Replace(REPLACE_PHOTO_EXT, r_ext);

                        foreach (char ch in Path.GetInvalidFileNameChars())
                        {
                            fileName = fileName.Replace(ch.ToString(), string.Empty);
                        }
                    }

                    WebClient.DownloadFile(photoSourceUrl, Path.Combine(downloadDir, fileName));
                    ConsoleBackLine();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(string.Format(Properties.Resources.Msg_Photo_DownloadCompleted, photoLink.FullUrl /*Path.GetFileName(photoSourceUrl)*/, i + 1, photoLinks.Length, fileName));
                    Console.ResetColor();
                    retryCount = 0;
                }
                catch (Exception e)
                {
                    if (IsDebug)
                    {
                        ConsoleBackLine();
                    }
                    ThrowException(e);
                    if (retryCount < SETTING_RETRYCOUNT)
                    {
                        retryCount++;
                        if (IsDebug)
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine($"Download failed. Retrying in 5 seconds. ({retryCount})");
                            Console.ResetColor();
                        }
                        Thread.Sleep(5000);
                        /*if (IsDebug)*/
                        ConsoleBackLine();
                        i--;
                        continue;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(string.Format(Properties.Resources.Msg_Photo_DownloadFailed, photoLink.FullUrl, i + 1, photoLinks.Length));
                        Console.ResetColor();
                        retryCount = 0;
                    }
                }
            }
        }

        static string GetHtmlText(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Timeout = 30 * 1000;
            request.AllowAutoRedirect = true;
            request.UserAgent = "Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)";
            request.Method = "GET";
            request.KeepAlive = true;
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }

        static string GetPhotoSourceUrl(string htmlText)
        {
            Regex regex = new Regex("<a href=\"/photo/[0-9]+\">\\s*<img src=\"(?<src>\\S+/\\S+original_[0-9]+\\.jpg)\"[\\S\\s]*?>\\s*</a>");
            if (regex.IsMatch(htmlText))
            {
                Match match = regex.Match(htmlText);
                return match.Groups["src"].Value;
            }
            return null;
        }

        static void ConsoleBackLine()
        {
            Console.SetCursorPosition(0, Console.CursorTop - 1);
            Console.Write(string.Empty.PadRight(Console.WindowWidth));
            Console.SetCursorPosition(0, Console.CursorTop - 1);
        }

        static void ThrowException(Exception e)
        {
            if (IsDebug)
            {
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e.Message);
                if (IsDebugInfo)
                {
                    if (e.InnerException != null)
                    {
                        Console.WriteLine($"InnerException: {e.InnerException.Message}");
                    }
                    Console.WriteLine($"StackTrace:\r\n{e.StackTrace}");
                    Console.WriteLine();
                }
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Pornhub album or photo format link.
        /// </summary>
        class LinkInfo
        {
            #region Properties
            public string Protocol { get; set; }
            //public string Region { get; set; }
            public string Type { get; set; }
            public string ID { get; set; }
            public string Host { get; set; }
            public string Args { get; set; }
            public string UrlWithoutArgs
            {
                get
                {
                    return $"{Host}/{Type}/{ID}";
                }
            }
            public string FullUrl
            {
                get
                {
                    return $"{Host}/{Type}/{ID}{Args}";
                }
            }

            private static Regex regex = new Regex("(?<url_without_args>(?<host>(?:(?<protocol>[http|https]+)://)?(?:(?<region>\\w{2,})\\.)?pornhub\\.com)/(?<type>\\w+)/(?<id>[0-9]+))(?<args>\\?\\S+)?");
            #endregion

            #region Methods
            public static LinkInfo Parse(string url)
            {
                if (regex.IsMatch(url))
                {
                    Match match = regex.Match(url);
                    return new LinkInfo()
                    {
                        Protocol = match.Groups["protocol"].Value,
                        //Region = match.Groups["region"].Value,
                        Type = match.Groups["type"].Value,
                        ID = match.Groups["id"].Value,
                        Host = match.Groups["host"].Value,
                        Args = match.Groups["args"].Value,
                        //UrlWithoutArgs = match.Groups["url_without_args"].Value,
                        //FullUrl = url,
                    };
                }
                return null;
            }

            public static bool TryParse(string url, out LinkInfo result)
            {
                return (result = Parse(url)) != null;
            }
            #endregion
        }
    }
}
