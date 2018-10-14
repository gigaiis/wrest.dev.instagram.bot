using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace main
{
    public class ApiResult<T>
    {
        T _result = default(T);
        Exception _exception = null;
        public void SetError(Exception exception) => _exception = exception;
        public Exception GetError() => _exception;
        public void SetResult(T result) => _result = result;
        public T GetResult() => _result;
        public bool isSuccess { get => _result != null && _exception == null; }
        public ApiResult(Exception exception) => SetError(exception);
        public ApiResult(T result) => SetResult(result);
        public ApiResult()
        {

        }
        public static implicit operator ApiResult<T>(ApiResult v)
        {
            var r = new ApiResult<T>();
            r.SetResult((T)v.GetResult());
            r.SetError(v.GetError());
            return r;
        }
    }
    public class ApiResult : ApiResult<Object>
    {
        public ApiResult(Exception exception) : base(exception) => SetResult(exception);
        public ApiResult(Object result) : base(result) => SetResult(result);
        public ApiResult() : base() { }
        public static implicit operator ApiResult(ApiResult<long> v)
        {
            var r = new ApiResult();
            r.SetResult(v.GetResult());
            r.SetError(v.GetError());
            return r;
        }

        public static implicit operator ApiResult(ApiResult<List<string>> v)
        {
            var r = new ApiResult();
            r.SetResult(v.GetResult());
            r.SetError(v.GetError());
            return r;
        }
    }

    public static class InstagramApi
    {
        public static readonly string profile_hist_name = @"profile_hist.json";
        public static readonly string ignore_list_name = @"ignore_list.json";

        public static Dictionary<long, profile_hist> profile_hist = new Dictionary<long, profile_hist>();
        public static List<long> ignore_list = new List<long>();

        public static class config
        {
            public static string csrf_token = "";
            public static string viewerId = "";
        }
        public static JObject DecodeSharedData(string input)
        {
            JObject o = JsonConvert.DeserializeObject<JObject>(
                input = (input = input.Substring(input.IndexOf("window._sharedData = ") + 21)).Substring(0, input.IndexOf(";</script>"))
            );

            JObject config = (JObject)o.GetValue("config");
            InstagramApi.config.csrf_token = config.GetValue("csrf_token").ToString();
            InstagramApi.config.viewerId = config.GetValue("viewerId").ToString();

            return o;
        }

        public static class access_tool
        {
            static readonly string Link = "https://www.instagram.com/accounts/access_tool/";
            static async Task<ApiResult<List<string>>> LoadALL(string Link)
            {
                Link = access_tool.Link + Link;
                List<string> result = new List<string>();
                try
                {
                    JObject _DecodeSharedData = DecodeSharedData(await Web.Navigate.Get((HttpWebRequest)WebRequest.Create(Link)));
                    string debug_object = _DecodeSharedData.ToString();

                    JObject data = (JObject)((JObject)((JArray)((JObject)
                                _DecodeSharedData
                                .GetValue("entry_data"))
                            .GetValue("SettingsPages"))[0])
                        .GetValue("data");
                    result.AddRange(((JArray)data.GetValue("data")).Select(e => ((JObject)e).GetValue("text").ToString()).ToList());

                    Object cursor = null;
                    while ((cursor = data.GetValue("cursor").ToObject<Object>()) != null)
                    {
                        data = (JObject)(JsonConvert.DeserializeObject<JObject>(
                            await Web.Navigate.Get((HttpWebRequest)WebRequest.Create(string.Format("{0}?__a=1&cursor={1}", Link, cursor)))))
                            .GetValue("data");
                        result.AddRange(((JArray)data.GetValue("data")).Select(e => ((JObject)e).GetValue("text").ToString()).ToList());
                    }
                }
                catch (Exception ex) { return new ApiResult<List<string>>(ex); }
                return new ApiResult<List<string>>(result);
            }

            public static class current_follow_requests
            {
                public static async Task<ApiResult<List<string>>> LoadALL() => await access_tool.LoadALL("current_follow_requests");
            }
            public static class accounts_following_you
            {
                public static async Task<ApiResult<List<string>>> LoadALL() => await access_tool.LoadALL("accounts_following_you");
            }

            public static class accounts_you_follow
            {
                public static async Task<ApiResult<List<string>>> LoadALL() => await access_tool.LoadALL("accounts_you_follow");
            }
        }

        public static async Task<ApiResult<bool>> Auth(string login, string password)
        {
            try
            {
                DecodeSharedData(
                    await Web.Navigate.Get((HttpWebRequest)WebRequest.Create("https://www.instagram.com/accounts/login/"))
                );
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://www.instagram.com/accounts/login/ajax/");
                request.Headers.Add("x-csrftoken", config.csrf_token);
                var auth = JsonConvert.DeserializeObject<_responseAuth>(
                    await Web.Navigate.Post(request, string.Format("username={0}&password={1}", login, password) + "&queryParams={}")
                );
                return new ApiResult<bool>(!((auth.authenticated != true) || (auth.user != true) || (auth.status != "ok")));
            }
            catch (Exception ex) { return new ApiResult<bool>(ex); }
        }
        public static async Task<ApiResult<profile>> GetProfilePage(string username)
        {
            try
            {
                JObject obj = ((JObject)((JObject)((JObject)((JArray)((JObject)
                    DecodeSharedData(await Web.Navigate.Get((HttpWebRequest)WebRequest.Create(
                        string.Format("https://www.instagram.com/{0}/", username)
                    ))).GetValue("entry_data")).GetValue("ProfilePage"))[0])
                    .GetValue("graphql")).GetValue("user"));

                var p = JsonConvert.DeserializeObject<profile>(obj.ToString());
                p.followers = Convert.ToInt64(((JObject)obj.GetValue("edge_followed_by")).GetValue("count"));
                p.following = Convert.ToInt64(((JObject)obj.GetValue("edge_follow")).GetValue("count"));

                if (profile_hist.ContainsKey(p.id))
                {
                    profile_hist[p.id].profile = p;
                    profile_hist[p.id].timestamp = Dev.GetUnixTimestamp();
                }
                else profile_hist.Add(p.id, new profile_hist(p));

                return new ApiResult<profile>(p);
            }
            catch (Exception ex) { return new ApiResult<profile>(ex); }
        }
        public static async Task<ApiResult<long>> GetUserId(string username)
        {
            var row = (from element in profile_hist
                      where element.Value.profile.username == username
                      select element).FirstOrDefault();
            if (!row.Equals(default(KeyValuePair<long, profile_hist>)))
                return new ApiResult<long>(row.Value.profile.id);
            var x = await GetProfilePage(username);
            if (x.isSuccess) return new ApiResult<long>(x.GetResult().id);
            else return new ApiResult<long>(x.GetError());
        }
        public static async Task<ApiResult> Unfollow(long user_id)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(
                    string.Format("https://www.instagram.com/web/friendships/{0}/unfollow/", user_id)
                );
                request.Headers.Add("x-csrftoken", config.csrf_token);
                var status = JsonConvert.DeserializeObject<JObject>(await Web.Navigate.Post(request, ""))
                    .GetValue("status").ToObject<Object>();

                if (status == null) throw new Exception(string.Format("Status for user_id: {0} not found", user_id));
                if (status.ToString() != "ok") throw new Exception(
                    string.Format("Unknown status: {0} for user_id: {1} not found", status, user_id)
                );

                return new ApiResult(new { response = user_id });
            }
            catch (Exception ex) { return new ApiResult(ex); }
        }
    }
}
