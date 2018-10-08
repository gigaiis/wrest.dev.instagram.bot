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
                request.CookieContainer = Cookies;
                request.ContentType = "application/x-www-form-urlencoded";
                request.Method = "POST";
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                byte[] byteArr = System.Text.Encoding.UTF8.GetBytes(System.Uri.EscapeUriString(Data));
                request.ContentLength = byteArr.Length;
                using (Stream s = request.GetRequestStream())
                    s.Write(byteArr, 0, byteArr.Length);
                return (HttpWebResponse)request.GetResponse();
            })).GetResponseStream()).ReadToEnd();

            public static async Task<string> Get(HttpWebRequest request) => new StreamReader((await Task.Run(() =>
            {
                request.CookieContainer = Cookies;
                request.ContentType = "application/x-www-form-urlencoded";
                request.Method = "GET";
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                return (HttpWebResponse)request.GetResponse();
            })).GetResponseStream()).ReadToEnd();
        }

        public static async Task<string> oldNavigate(string Url,
            string Method = "GET",
            string Data = "",
            Dictionary<string, string> addHeaders = null,
            string ContentType = null)
        {
            HttpWebResponse response = await Task.Run(() =>
            {
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
                request.Proxy.Credentials = CredentialCache.DefaultCredentials;
                request.UseDefaultCredentials = true;

                request.CookieContainer = Cookies;
                request.Method = Method;

                request.ContentType = ContentType != null ? ContentType : "application/x-www-form-urlencoded";
                request.UserAgent =
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/69.0.3497.100 Safari/537.36";
                request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8";
                // request.Headers.Add("accept-encoding:", "gzip, deflate, br");
                request.Headers.Add("accept-language", "en-US,en;q=0.9");
                if (addHeaders != null)
                    foreach (var el in addHeaders)
                        request.Headers.Add(el.Key, el.Value);
                request.Timeout = 30000;
                if (Method.Equals("POST"))
                {
                    byte[] byteArr = System.Text.Encoding.UTF8.GetBytes(System.Uri.EscapeDataString(Data));
                    request.ContentLength = byteArr.Length;
                    using (Stream s = request.GetRequestStream())
                        s.Write(byteArr, 0, byteArr.Length);
                }


                var _response = (HttpWebResponse)request.GetResponse();

                //WebHeaderCollection headers = _response.Headers;
                //for (int i = 0; i < headers.Count; i++)
                //    System.Console.WriteLine("{0}: {1}", headers.GetKey(i), headers[i]);


                return _response;
            });
            StreamReader str = new StreamReader(response.GetResponseStream());
            var sR = str.ReadToEnd();
            return sR;
        }
    }
}
