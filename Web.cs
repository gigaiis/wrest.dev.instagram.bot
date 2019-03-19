using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace main
{
    public class Web
    {
        public class WebResult<T>
        {
            T _result = default;
            Exception _exception = null;
            HttpWebRequest _request = null;
            HttpWebResponse _response = null;

            public void SetError(Exception exception) => _exception = exception;
            public Exception GetError() => _exception;
            public HttpWebRequest GetRequest() => _request;
            public HttpWebResponse GetResponse() => _response;
            public HttpStatusCode GetStatusCode() => _response != null ? _response.StatusCode : new HttpStatusCode();
            public string GetDescription() => _response != null ? _response.StatusDescription : "";
            public WebResult<T> SetResult(T result, HttpWebRequest request = null, HttpWebResponse response = null)
            {
                _result = result;
                _request = request;
                _response = response;
                return this;
            }
            public T GetResult()
            {
                if (_result == null)
                    if (_exception != null) throw _exception;
                    else return default;
                return _result;
            }
            public bool IsSuccess { get => _result != null && _exception == null; }
            public WebResult(Exception exception) => SetError(exception);
            public WebResult(T result, HttpWebRequest request = null, HttpWebResponse response = null) =>
                SetResult(result, request, response);
        }

        public static CookieContainer Cookies = new CookieContainer();
        public static HttpWebRequest GenXRequest(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Headers.Add("x-requested-with", "XMLHttpRequest");
            request.Headers.Add("x-instagram-ajax", InstagramApi.Config.rollout_hash);
            request.Headers.Add("x-csrftoken", InstagramApi.Config.Csrf_token);
            return request;
        }
        public static class Navigate
        {
            static Navigate()
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                ServicePointManager.ServerCertificateValidationCallback += delegate { return true; };
            }

            public static class Limited
            {
                static bool isLocked = false;
                const double targ_limited_time = 0;
                const double get_limited_time = 0.625;  // 625 ms
                public static async Task<string> Get(HttpWebRequest request,
                string ContentType = "application/x-www-form-urlencoded")
                {
                    var rand_wait = new Random().Next(66) + 1;
                    while (isLocked)
                        Thread.Sleep(rand_wait);
                    isLocked = true;
                    var z = Dev.GetUnixTimestamp();
                    var y = z - targ_limited_time;
                    if (y < get_limited_time)
                        Thread.Sleep((int)Math.Round((get_limited_time - y) * 1000));
                    var result = await (new StreamReader((await Task.Run(async () =>
                    {
                        try
                        {
                            request.Method = "GET";
                            request.CookieContainer = Cookies;
                            if (ContentType != null) request.ContentType = ContentType;
                            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                            HttpWebResponse response = null;
                            try
                            {
                                response = (HttpWebResponse)request.GetResponse();
                                current_action_web_list.Add(new Act(request.RequestUri.ToString()));
                            }
                            catch (WebException ex)
                            {
                                await Log.Write(new List<string>() { string.Format("[WebException]: {0}", ex.Message) }, Log.Type.error);
                                current_action_web_list.Add(new Act(request.RequestUri.ToString(), "", Act.Type.error));
                            }
                            catch (Exception ex)
                            {
                                await Log.Write(new List<string>() { string.Format("[Exception]: {0}", ex.Message) }, Log.Type.error);
                                current_action_web_list.Add(new Act(request.RequestUri.ToString(), "", Act.Type.error));
                                throw ex;
                            }
                            return response;
                        }
                        catch (Exception ex)
                        {
                            throw ex;
                        }
                    })).GetResponseStream())).ReadToEndAsync();
                    isLocked = false;
                    return result;
                }
            }
            public static async Task<WebResult<string>> Post(HttpWebRequest request, string Data)
            {
                Data = Uri.EscapeUriString(Data);
                request.Method = "POST";
                request.CookieContainer = Cookies;
                if (Data.Length > 0)
                    request.ContentType = "application/x-www-form-urlencoded";
                byte[] byteArr = Encoding.UTF8.GetBytes(Data);
                request.ContentLength = byteArr.Length;
                using (Stream s = request.GetRequestStream())
                    s.Write(byteArr, 0, byteArr.Length);
                try
                {
                    using (var response = request.GetResponse() as HttpWebResponse)
                    {
                        if (request.HaveResponse && response != null)
                            using (var reader = new StreamReader(response.GetResponseStream()))
                            {
                                current_action_web_list.Add(new Act(request.RequestUri.ToString(), Data, Act.Type.post));
                                return new WebResult<string>(reader.ReadToEnd());
                            }
                    }
                    throw new Exception("No result");
                }
                catch (WebException ex)
                {
                    current_action_web_list.Add(new Act(request.RequestUri.ToString(), Data, Act.Type.error));
                    if (ex.Response != null)
                    {
                        using (var response = (HttpWebResponse)ex.Response)
                        {
                            using (var reader = new StreamReader(response.GetResponseStream()))
                            {
                                return new WebResult<string>(reader.ReadToEnd(), request, response);
                            }
                        }
                    }
                    else
                    {
                        await Log.Write(new List<string>() { string.Format("[WebException]: {0}", ex.Message) }, Log.Type.error);
                        throw ex;
                    }
                }
                catch (Exception ex)
                {
                    await Log.Write(new List<string>() { string.Format("[Exception]: {0}", ex.Message) }, Log.Type.error);
                    current_action_web_list.Add(new Act(request.RequestUri.ToString(), Data, Act.Type.error));
                    throw ex;
                }
            }
            public static async Task<string> GetRedirectLocation(string url,
                string Method = "GET",
                string ContentType = "application/x-www-form-urlencoded") => await Task.Run(async () =>
                {
                    var request = (HttpWebRequest)WebRequest.Create(url);
                    request.AllowAutoRedirect = false;
                    request.Method = Method;
                    request.CookieContainer = Cookies;
                    if (ContentType != null) request.ContentType = ContentType;
                    HttpWebResponse response = null;
                    try
                    {
                        response = (HttpWebResponse)request.GetResponse();
                        current_action_web_list.Add(new Act(request.RequestUri.ToString(), "", Act.Type.get));
                        if (response.StatusCode == HttpStatusCode.Redirect)
                            return response.Headers["location"];
                    }
                    catch (Exception ex)
                    {
                        await Log.Write(new List<string>() { string.Format("[Exception]: {0}", ex.Message) }, Log.Type.error);
                        current_action_web_list.Add(new Act(request.RequestUri.ToString(), "", Act.Type.error));
                        throw ex;
                    }
                    return null;
                });
            public static async Task<WebResult<string>> Get(HttpWebRequest request)
            {
                request.Method = "GET";
                request.CookieContainer = Cookies;
                try
                {
                    using (var response = request.GetResponse() as HttpWebResponse)
                    {
                        if (request.HaveResponse && response != null)
                            using (var reader = new StreamReader(response.GetResponseStream()))
                            {
                                current_action_web_list.Add(new Act(request.RequestUri.ToString(), "", Act.Type.get));
                                return new WebResult<string>(reader.ReadToEnd());
                            }
                    }
                    throw new Exception("No result");
                }
                catch (WebException ex)
                {
                    current_action_web_list.Add(new Act(request.RequestUri.ToString(), "", Act.Type.error));
                    if (ex.Response != null)
                    {
                        using (var response = (HttpWebResponse)ex.Response)
                        {
                            using (var reader = new StreamReader(response.GetResponseStream()))
                            {
                                return new WebResult<string>(ex).SetResult(reader.ReadToEnd(), request, response);
                            }
                        }
                    }
                    else
                    {
                        await Log.Write(new List<string>() { string.Format("[WebException]: {0}", ex.Message) }, Log.Type.error);
                        throw ex;
                    }
                }
                catch (Exception ex)
                {
                    await Log.Write(new List<string>() { string.Format("[Exception]: {0}", ex.Message) }, Log.Type.error);
                    current_action_web_list.Add(new Act(request.RequestUri.ToString(), "", Act.Type.error));
                    throw ex;
                }
            }
        }
        public class Act
        {
            public enum Type { unknown = 0, get = 1, post = 2, error = 3 };
            public Type type;
            public string url;
            public string data;
            public double timestamp = Dev.GetUnixTimestamp();
            public Act(string url, string data = "", Type type = Type.get)
            {
                this.url = url;
                this.data = data;
                this.type = type;
            }
            public override string ToString() => string.Format("[{0}] has been ?", type.ToString().ToUpper());
        }
        public static List<Act> current_action_web_list = new List<Act>();
    }
}
