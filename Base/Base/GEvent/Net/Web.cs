#if UNITY_3_4 || UNITY_3_5 || UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1
#define UNITY
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
#if !UNITY
using System.Web;
#else
using UnityEngine;
#endif

namespace Base.GEvent
{
    class WebWrapper
    {
        class DownloadBundle
        {
            public System.Threading.AutoResetEvent waiter;
            public byte[] bytes = null;
            public bool success;
        }

        int timeout = -1;
        CookieContainer cookie = new CookieContainer();

        public int Timeout
        {
            get { return timeout; }
            set { timeout = value; }
        }

        public static string GenArgsStr(Hashtable argv)
        {
            string url = "";

            foreach (DictionaryEntry pair in argv)
#if !UNITY
            url += "&" + pair.Key.ToString() + "=" + HttpUtility.UrlEncode(pair.Value.ToString(), Encoding.UTF8);
#else
            url += "&" + pair.Key.ToString() + "=" + WWW.EscapeURL(pair.Value.ToString(), Encoding.UTF8); 
#endif
            return url.Length > 0 ? url.Substring(1) : url;
        }
        public static string CombineUrl(string url, params object[] args)
        {
            if (url.IndexOf("?") == -1)
                url += '?';

            if (url.Length == 0 || url[url.Length-1] != '&')
                url += '&';

            return url + GenArgsStr(Utility.HashTable(args));
        }

        public void AddCookie(string key, string value, string path = "", string domain = "")
        {
            if (path == "" && domain == "")
                cookie.Add(new Cookie(key, value));
            else if (domain == "")
                cookie.Add(new Cookie(key, value, path));
            else
                cookie.Add(new Cookie(key, value, path, domain));
        }

        public string Post(string url, params object[] args)
        {
            return Post(url, Utility.HashTable(args));
        }
        public string Get(string url, params object[] args)
        {
            url = CombineUrl(url, args);

            return Get(url);
        }
        public byte[] Download(string uri, string localPath = "", string QueryHeader = "BaseHeader")
        {
            using (WebClient fileDownloader = new WebClient())
            {
                fileDownloader.Encoding = System.Text.Encoding.UTF8;
                fileDownloader.Headers.Add("User-Agent", QueryHeader);
                fileDownloader.DownloadDataCompleted += new DownloadDataCompletedEventHandler(DownloadDataCompleted);

                DownloadBundle bundle = new DownloadBundle();
                bundle.waiter = new System.Threading.AutoResetEvent(false);
                bundle.success = false;

                fileDownloader.DownloadDataAsync(new Uri(uri), bundle);
                
                // block until download complete
                bundle.waiter.WaitOne();

                if (!bundle.success)
                    throw new GEventException(new ErrorBundle(EventType.WEB, ErrorCode.DownloadFailed, "Download File Failed"));

                if (localPath != "")
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(localPath)));
                    using (FileStream fs = new FileStream(localPath, FileMode.Create))
                        using (BinaryWriter bw = new BinaryWriter(fs))
                        {
                            bw.Write(bundle.bytes);
                            bw.Close();
                            fs.Close();
                        }
                }

                return bundle.bytes;
            }
        }

        protected void DownloadDataCompleted(object sender, DownloadDataCompletedEventArgs e)
        {
            DownloadBundle bundle = e.UserState as DownloadBundle;

            try
            {
                if (!e.Cancelled && e.Error == null)
                {
                    bundle.bytes = (byte[])e.Result;
                    bundle.success = true;
                }
            }
            finally
            {
                bundle.waiter.Set();
            }
        }
        
        protected virtual string Post(string url, Hashtable args)
        {
            string param = GenArgsStr(args);
            byte[] postData = Encoding.ASCII.GetBytes(param);

            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);

            req.Timeout = timeout;
            req.CookieContainer = cookie;
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded;charset=utf8";
            req.ContentLength = postData.Length;

            using (Stream reqStream = req.GetRequestStream())
            {
                reqStream.Write(postData, 0, postData.Length);
            }
            using (WebResponse wr = req.GetResponse())
            {
                Stream stream = wr.GetResponseStream();
                StreamReader rs = new StreamReader(stream);
                return rs.ReadToEnd();
            }
        }
        protected virtual string Get(string url)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Timeout = timeout;
            req.CookieContainer = cookie;
            req.Method = "GET";

            using (WebResponse wr = req.GetResponse())
            {
                Stream stream = wr.GetResponseStream();
                StreamReader rs = new StreamReader(stream);
                return rs.ReadToEnd();
            }
        }
    }

    public class WebQuest : Quest
    {
        public string Content { get; set; }
        public WebQuest(OnQuestDelegate task, Peer peer)
            : base(task, peer, EventType.WEB)
        {
        }
    }

    public class WebPeer : Peer
    {
        WebWrapper handler = new WebWrapper();

        public WebPeer() { }

        public int Timeout
        {
            set
            {
                lock (handler)
                {
                    handler.Timeout = value;
                }
            }
            get
            {
                lock (handler)
                {
                    return handler.Timeout;
                }
            }
        }

        public void AddCookie(string key, string value, string path = "", string domain = "")
        {
            handler.AddCookie(key, value, path, domain);
        }

        public Quest WebQuestHandler(Quest.OnQuestDelegate func)
        {
            return new WebQuest(
                (bundle) =>
                {
                    try
                    {
                        return func(bundle);
                    }
                    catch (System.Net.WebException exception)
                    {
                        if (exception.Status == WebExceptionStatus.Timeout)
                            throw new GEventException(new ErrorBundle(EventType.WEB, ErrorCode.Timeout, exception));
                        else if (exception.Status == WebExceptionStatus.ConnectFailure || exception.Status == WebExceptionStatus.NameResolutionFailure)
                            throw new GEventException(new ErrorBundle(EventType.WEB, ErrorCode.ConnectionFailed, exception));
                        else if (exception.Status == WebExceptionStatus.ConnectionClosed)
                            throw new GEventException(new ErrorBundle(EventType.WEB, ErrorCode.ConnectionClosed, exception));
                        else
                            throw new GEventException(new ErrorBundle(EventType.WEB, ErrorCode.WebError, exception));
                    }
                    catch (System.Exception exception)
                    {
                        throw new GEventException(new ErrorBundle(EventType.WEB, ErrorCode.WebError, exception));
                    }
                },
                this
            );
        }
        public Quest Get(string url, params object[] args)
        {
            return WebQuestHandler(
                (bundle) =>
                {
                    lock (handler)
                    {
                        return handler.Get(url, args);
                    }
                });
        }
        public Quest Post(string url, params object[] args)
        {
            return WebQuestHandler(
                (bundle) =>
                {
                    lock (handler)
                    {
                        return handler.Post(url, args);
                    }
                });
        }
        public Quest Download(string uri, string localPath = "", string queryHeader = "BaseHeader")
        {
            return WebQuestHandler(
                (bundle) =>
                {
                    lock (handler)
                    {
                        return handler.Download(uri, localPath, queryHeader);
                    }
                });
        }
    }
}
