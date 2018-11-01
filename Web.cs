using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace main
{
    public class Web
    {
        public static CookieContainer Cookies = new CookieContainer();
        public static HttpWebRequest GenXRequest(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Headers.Add("x-requested-with", "XMLHttpRequest");
            request.Headers.Add("x-instagram-ajax", InstagramApi.config.rollout_hash);
            request.Headers.Add("x-csrftoken", InstagramApi.config.csrf_token);
            return request;
        }
        public static class Navigate
        {
            public static async Task<string> Post(HttpWebRequest request,
                string Data, string ContentType = "application/x-www-form-urlencoded") => new StreamReader((await Task.Run(() =>
            {        
                Log.Write(
                    string.Format("\r\n\t[POST]: {0}\r\n\t[Data]: {1}\r\n\t[ContentType]: {2}",
                        request.RequestUri,
                        Data,
                        ContentType == null ? "<empty>" : ContentType
                    )
                );
                Data = Uri.EscapeUriString(Data);
                request.Method = "POST";
                request.CookieContainer = Cookies;
                if (ContentType != null) request.ContentType = ContentType;
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                byte[] byteArr = System.Text.Encoding.UTF8.GetBytes(Data);
                request.ContentLength = byteArr.Length;
                using (Stream s = request.GetRequestStream())
                    s.Write(byteArr, 0, byteArr.Length);
                HttpWebResponse response = null;
                try
                {
                    response = (HttpWebResponse)request.GetResponse();
                }
                catch (WebException ex)
                {
                    Log.Write(string.Format("\t|\t[WebException]: {0}", ex.Message), false);
                    throw ex;
                }
                catch (Exception ex)
                {
                    Log.Write(string.Format("\t|\t[Exception]: {0}", ex.Message), false);
                    throw ex;
                }
                return response;
            })).GetResponseStream()).ReadToEnd();

            public static async Task<string> Get(HttpWebRequest request,
                string ContentType = "application/x-www-form-urlencoded") => new StreamReader((await Task.Run(() =>
                {
                    Log.Write(
                        string.Format("\r\n\t[GET]: {0}\r\n\t[ContentType]: {1}",
                            request.RequestUri,
                            ContentType == null ? "<empty>" : ContentType
                        )
                    );
                    request.Method = "GET";
                    request.CookieContainer = Cookies;
                    if (ContentType != null) request.ContentType = ContentType;
                    ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                    HttpWebResponse response = null;
                    try
                    {
                        response = (HttpWebResponse)request.GetResponse();
                    }
                    catch (WebException ex)
                    {
                        Log.Write(string.Format("\t|\t[WebException]: {0}", ex.Message), false);
                        throw ex;
                    }
                    catch (Exception ex)
                    {
                        Log.Write(string.Format("\t|\t[Exception]: {0}", ex.Message), false);
                        throw ex;
                    }
                    return response;
                })).GetResponseStream()).ReadToEnd();
        }
    }
}
