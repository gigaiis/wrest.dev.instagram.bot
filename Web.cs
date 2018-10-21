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
        public static class Navigate
        {
            public static async Task<string> Post(HttpWebRequest request, string Data) => new StreamReader((await Task.Run(() =>
            {
                Log.Write(string.Format("POST {0} {1}", request.RequestUri, Data));
                request.CookieContainer = Cookies;
                request.ContentType = "application/x-www-form-urlencoded";
                request.Method = "POST";
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                byte[] byteArr = System.Text.Encoding.UTF8.GetBytes(System.Uri.EscapeUriString(Data));
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
                    throw ex;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                return response;
            })).GetResponseStream()).ReadToEnd();

            public static async Task<string> Get(HttpWebRequest request) => new StreamReader((await Task.Run(() =>
            {
                Log.Write(string.Format("GET {0}", request.RequestUri));
                request.CookieContainer = Cookies;
                request.ContentType = "application/x-www-form-urlencoded";
                request.Method = "GET";
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                HttpWebResponse response = null;
                try
                {
                    response = (HttpWebResponse)request.GetResponse();
                }
                catch (WebException ex)
                {
                    throw ex;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                return response;
            })).GetResponseStream()).ReadToEnd();
        }
    }
}
