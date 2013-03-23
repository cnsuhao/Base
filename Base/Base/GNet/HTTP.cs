﻿#if UNITY_3_4 || UNITY_3_5 || UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1
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

namespace Base.GNet
{
    class WebHandler
    {
        int timeout = -1;

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

            return url + GenArgsStr(Utility.GenHashTable(args));
        }

        public string POST(string url, params object[] args)
        {
            return POST(url, Utility.GenHashTable(args));
        }
        public string GET(string url, params object[] args)
        {
            url = CombineUrl(url, args);

            return GET(url);
        }
        
        protected virtual string POST(string url, Hashtable args)
        {
            string param = GenArgsStr(args);
            byte[] postData = Encoding.ASCII.GetBytes(param);

            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(url);
            if (timeout >= 0)
                req.Timeout = timeout;
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
        protected virtual string GET(string url)
        {
            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(url);
            if (timeout >= 0)
                req.Timeout = timeout;
            req.Method = "GET";

            using (WebResponse wr = req.GetResponse())
            {
                Stream stream = wr.GetResponseStream();
                StreamReader rs = new StreamReader(stream);
                return rs.ReadToEnd();
            } 
        }
    }

    class Web
    {
    }
}
