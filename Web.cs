using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
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
                //Log.Write(
                //    new List<string>() {
                //        string.Format("[POST]: {0}",request.RequestUri),
                //        string.Format("[Data]: {0}", Data),
                //        string.Format("[ContentType]: {0}", ContentType == null ? "<empty>" : ContentType)
                //    },
                //    Log.Type.info
                //);
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
                    current_action_web_list.Add(new act(request.RequestUri.ToString(), Data, act.act_type.post));
                }
                catch (WebException ex)
                {
                    Log.Write(new List<string>() { string.Format("[WebException]: {0}", ex.Message) }, Log.Type.error);
                    throw ex;
                }
                catch (Exception ex)
                {
                    Log.Write(new List<string>() { string.Format("[Exception]: {0}", ex.Message) }, Log.Type.error);
                    throw ex;
                }
                return response;
            })).GetResponseStream()).ReadToEnd();

            public static async Task<string> Get(HttpWebRequest request,
                string ContentType = "application/x-www-form-urlencoded") => new StreamReader((await Task.Run(() =>
                {
                    //Log.Write(
                    //    new List<string>()
                    //    {
                    //        string.Format("[GET]: {0}",request.RequestUri),
                    //        string.Format("[ContentType]: {0}", ContentType == null ? "<empty>" : ContentType)
                    //    },
                    //    Log.Type.info
                    //);
                    request.Method = "GET";
                    request.CookieContainer = Cookies;
                    if (ContentType != null) request.ContentType = ContentType;
                    ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                    HttpWebResponse response = null;
                    try
                    {
                        response = (HttpWebResponse)request.GetResponse();
                        current_action_web_list.Add(new act(request.RequestUri.ToString()));
                    }
                    catch (WebException ex)
                    {
                        Log.Write(new List<string>() { string.Format("[WebException]: {0}", ex.Message) }, Log.Type.error);
                        throw ex;
                    }
                    catch (Exception ex)
                    {
                        Log.Write(new List<string>() { string.Format("[Exception]: {0}", ex.Message) }, Log.Type.error);
                        throw ex;
                    }
                    return response;
                })).GetResponseStream()).ReadToEnd();
        }
        public class act
        {
            public enum act_type { unknown = 0, get = 1, post = 2, error = 3 };
            public act_type act_Type;
            public string url;
            public string data;
            public int timestamp = Dev.GetUnixTimestamp();
            public act(string url, string data = "", act_type act_Type = act_type.get)
            {
                this.url = url;
                this.data = data;
                this.act_Type = act_Type;
            }
            public override string ToString() => string.Format("[{0}] has been {1}", act_Type.ToString().ToUpper());
        }
        public static List<act> current_action_web_list = new List<act>();
    }
}
