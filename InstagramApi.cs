using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace main
{
    public class ApiResult<T>
    {
        T _result = default(T);
        Exception _exception = null;
        bool _isCash = false;
        public void SetError(Exception exception) => _exception = exception;
        public Exception GetError() => _exception;
        public void SetResult(T result, bool isCash = false)
        {
            _isCash = isCash;
            _result = result;
        }
        public T GetResult() => _result;
        public bool isCash() => _isCash;
        public bool isSuccess { get => _result != null && _exception == null; }
        public ApiResult(Exception exception) => SetError(exception);
        public ApiResult(T result, bool isChsh = false) => SetResult(result, isChsh);
        public ApiResult(List<string> result) => _result = (T)result.Cast<Object>();
        public ApiResult() { }
        public static implicit operator ApiResult<T>(ApiResult v)
        {
            var r = new ApiResult<T>();
            r.SetResult((T)v.GetResult(), v.isCash());
            r.SetError(v.GetError());
            return r;
        }
    }
    public class ApiResult : ApiResult<Object>
    {
        public ApiResult(Exception exception) : base(exception) => SetResult(exception);
        public ApiResult(Object result, bool isCash = false) : base(result, isCash) => SetResult(result, isCash);
        public ApiResult() : base() { }

        static ApiResult _implicit<U>(ApiResult<U> v)
        {
            var r = new ApiResult();
            r.SetResult(v.GetResult());
            r.SetError(v.GetError());
            return r;
        }

        public static implicit operator ApiResult(ApiResult<long> v) => _implicit(v);
        public static implicit operator ApiResult(ApiResult<List<string>> v) => _implicit(v);
        public static implicit operator ApiResult(ApiResult<bool> v) => _implicit(v);
    }
    public static class User
    {
        public static model.profile user_profile;
        public static long user_id => user_profile.id;
    }
    public static class InstagramApi
    {
        public static readonly string profile_hist_name = @"profile_hist.json";
        public static readonly string ignore_list_name = @"ignore_list.json";
        public static readonly string work_list_name = @"work_list.json";
        public static readonly string activity_list_name = @"activity_list.json";

        public static Dictionary<long, model.profile_hist> profile_hist = new Dictionary<long, model.profile_hist>();
        public static List<long> ignore_list = new List<long>();
        public static List<model.work_list> work_list = new List<model.work_list>();
        public static async Task<ApiResult<bool>> Init()
        {
            try
            {
                account.activity.activity_list = await config.LoadModule<List<model.activity_list>>(activity_list_name);
                profile_hist = await config.LoadModule<Dictionary<long, model.profile_hist>>(profile_hist_name);
                ignore_list = await config.LoadModule<List<long>>(ignore_list_name);
                work_list = await config.LoadModule<List<model.work_list>>(work_list_name);
                return new ApiResult<bool>(true);
            }
            catch (Exception ex) { return new ApiResult<bool>(ex); }
        }

        public static class config
        {
            public static string csrf_token = "";
            public static string viewerId = "";

            public static async Task<T> LoadModule<T>(string module_name, bool safe_mode = true) where T : new()
            {

                var text = await Dev.ReadAsync(module_name);
                var result = JsonConvert.DeserializeObject<T>(text);
                if (result == null)
                {
                    if (safe_mode && text.Length > 0)
                        await Dev.WriteAsync(string.Format(@"migrate\{0}_{1}", Dev.GetUnixTimestamp(), module_name), text);
                    result = new T();
                    await Dev.WriteAsync(module_name, JsonConvert.SerializeObject(result));
                }
                return result;
            }
        }
        public static JObject DecodeSharedData(string input)
        {
            int ind = -1;
            if ((ind = input.IndexOf("window._sharedData = ")) < 0) return null;
            input = input.Substring(ind + 21);
            if ((ind = input.IndexOf(";</script>")) < 0) return null;
            input = input.Substring(0, ind);

            JObject o = JsonConvert.DeserializeObject<JObject>(input);
            JObject config = (JObject)o.GetValue("config");
            InstagramApi.config.csrf_token = config.GetValue("csrf_token").ToString();
            InstagramApi.config.viewerId = config.GetValue("viewerId").ToString();

            return o;
        }
        public static class account
        {
            static readonly string Link = "https://www.instagram.com/accounts/";
            public static async Task<ApiResult<bool>> Auth(string login, string password)
            {
                var link = string.Format("{0}login/", Link);
                try
                {
                    DecodeSharedData(await Web.Navigate.Get((HttpWebRequest)WebRequest.Create(link)));
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(link + "ajax");
                    request.Headers.Add("x-csrftoken", config.csrf_token);
                    var auth = JsonConvert.DeserializeObject<model.auth>(
                        await Web.Navigate.Post(request, string.Format("username={0}&password={1}", login, password) + "&queryParams={}")
                    );
                    User.user_profile = (await GetProfile(login, true)).GetResult();
                    return new ApiResult<bool>(!((auth.authenticated != true) || (auth.user != true) || (auth.status != "ok")));
                }
                catch (Exception ex) { return new ApiResult<bool>(ex); }
            }
            public static class activity
            {
                public static readonly TimeSpan cooldown = new TimeSpan(24, 0, 0);
                public static readonly Dictionary<model.activity_list.type, long> limits =
                    new Dictionary<model.activity_list.type, long>()
                {
                    {
                        // Отписка: интервал 12-22 секунд и не более 1000 в сутки от НЕвзаимных и 1000 от взаимных 
                        // (пауза между невзаимными и взаимными 15 часов);
                        model.activity_list.type.Unfollow,
                        500
                    },
                    {
                        // Не более одного в течении 28 – 36 секунд (за раз – 1000, перерыв 24 часа);
                        model.activity_list.type.Like,
                        500
                    },
                    {
                        model.activity_list.type.Unlike,
                        500
                    }
                };
                public static List<model.activity_list> activity_list = new List<model.activity_list>();
                public static async void UpdateActivityList(model.activity_list.type type)
                {
                    activity_list = activity_list.Where(e => (Dev.GetUnixTimestamp() - e.timestamp) <= cooldown.TotalSeconds).ToList();
                    activity_list.Add(new model.activity_list(type));
                    await Dev.WriteAsync(activity_list_name, JsonConvert.SerializeObject(activity_list));
                }

                static readonly string Link = account.Link + "activity/";
                static long expiring_at = 0;
                public static async Task<ApiResult<List<model.activity>>> Load(bool only_new = false, bool include_reel = true)
                {
                    List<model.activity> result = new List<model.activity>();
                    try
                    {
                        string debug_object = await Web.Navigate.Get((HttpWebRequest)WebRequest.Create(
                            string.Format("{0}?__a=1&include_reel={1}", Link, include_reel.ToString().ToLower())));

                        var obj = JsonConvert.DeserializeObject<JObject>(debug_object)
                            .Value<JObject>("graphql")
                            .Value<JObject>("user")
                            .Value<JObject>("activity_feed")
                            .Value<JObject>("edge_web_activity_feed")
                            .Value<JArray>("edges");
                        var st = obj.ToString();
                        foreach (var o in obj)
                        {
                            try
                            {
                                var node = o.Value<JObject>("node");
                                model.activity a = JsonConvert.DeserializeObject<model.activity>(node.ToString());
                                if (a._type != model.activity.type.GraphGdprConsentStory)
                                {

                                    var user = o.Value<JObject>("node").Value<JObject>("user");

                                    var profile = await GetProfile(a.user.username);
                                    if (!profile.isSuccess) throw profile.GetError();

                                    a.user = profile.GetResult();
                                    if (include_reel) a.expiring_at = Convert.ToInt64(
                                        user.Value<JObject>("reel").Value<string>("expiring_at")
                                    );

                                    result.Add(a);

                                }
                            }
                            catch (Exception ex)
                            {
                                return new ApiResult<List<model.activity>>();
                            }
                            catch
                            {
                                return new ApiResult<List<model.activity>>();
                            }
                        }
                        var _result = new ApiResult<List<model.activity>>((only_new) ? result.Where(e => e.expiring_at > expiring_at).ToList() : result);
                        if (result.Count > 0) expiring_at = result.First().expiring_at;
                        return _result;
                    }
                    catch (Exception ex) { return new ApiResult<List<model.activity>>(ex); }
                }
            }
            public static class access_tool
            {
                public class _cv
                {
                    public long current = 0;
                    //public long max = 0;
                    public _cv() { }
                }
                static readonly string Link = account.Link + "access_tool/";
                static Dictionary<string, _cv> Current_value = new Dictionary<string, _cv>()
                {
                    {"current_follow_requests", new _cv() },
                    {"accounts_following_you", new _cv() },
                    {"accounts_you_follow", new _cv() }
                };
                static async Task<ApiResult<List<string>>> LoadALL(string Link)
                {
                    string _Link = Link;
                    Current_value[_Link] = new _cv();
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

                        List<string> _list = ((JArray)data.GetValue("data")).Select(e => ((JObject)e).GetValue("text").ToString()).ToList();
                        result.AddRange(_list);
                        Current_value[_Link].current += _list.Count;

                        Object cursor = null;
                        while ((cursor = data.GetValue("cursor").ToObject<Object>()) != null)
                        {
                            data = (JObject)(JsonConvert.DeserializeObject<JObject>(
                                await Web.Navigate.Get((HttpWebRequest)WebRequest.Create(string.Format("{0}?__a=1&cursor={1}", Link, cursor)))))
                                .GetValue("data");

                            _list = ((JArray)data.GetValue("data")).Select(e => ((JObject)e).GetValue("text").ToString()).ToList();
                            result.AddRange(_list);
                            Current_value[_Link].current += _list.Count;
                        }
                    }
                    catch (Exception ex) { return new ApiResult<List<string>>(ex); }
                    return new ApiResult<List<string>>(result);
                }

                public static class current_follow_requests
                {
                    static string name = "current_follow_requests";
                    public static long current_pos => Current_value[name].current;
                    //public static long current_max => User.user_profile.followers;
                    //Current_value[name].max;
                    public static async Task<ApiResult<List<string>>> LoadALL() => await access_tool.LoadALL(name);
                }
                public static class accounts_following_you
                {
                    static string name = "accounts_following_you";
                    public static long current_pos => Current_value[name].current;
                    public static long current_max => User.user_profile.followers;
                    //Current_value[name].max;
                    public static async Task<ApiResult<List<string>>> LoadALL() => await access_tool.LoadALL(name);
                }

                public static class accounts_you_follow
                {
                    static string name = "accounts_you_follow";
                    public static long current_pos => Current_value[name].current;
                    public static long current_max => User.user_profile.following;
                    //Current_value[name].max;
                    public static async Task<ApiResult<List<string>>> LoadALL() => await access_tool.LoadALL(name);
                }
            }
        }


        public static async Task<ApiResult<model.profile>> GetProfile(string username, bool ignore_hash = false)
        {
            try
            {
                if (!ignore_hash)
                {
                    var row = profile_hist.Where(e => e.Value.profile.username.Equals(username)).FirstOrDefault();
                    if (!row.Equals(default(KeyValuePair<long, model.profile_hist>)))
                    {
                        var avg = Dev.GetUnixTimestamp() - row.Value.timestamp;
                        var ttlsec = new TimeSpan(24, 0, 0).TotalSeconds;
                        if (!(avg > ttlsec)) return new ApiResult<model.profile>(row.Value.profile, true);
                    }
                }

                JObject _decData = DecodeSharedData(
                    await Web.Navigate.Get((HttpWebRequest)WebRequest.Create(
                        string.Format("https://www.instagram.com/{0}/", username)
                    )));

                if (_decData == null) throw new WebException("", (WebExceptionStatus)429);

                JObject obj = ((JObject)((JObject)((JObject)((JArray)((JObject)
                    _decData.GetValue("entry_data")).GetValue("ProfilePage"))[0])
                    .GetValue("graphql")).GetValue("user"));

                var p = JsonConvert.DeserializeObject<model.profile>(obj.ToString());
                p.followers = Convert.ToInt64(((JObject)obj.GetValue("edge_followed_by")).GetValue("count"));
                p.following = Convert.ToInt64(((JObject)obj.GetValue("edge_follow")).GetValue("count"));

                if (profile_hist.ContainsKey(p.id))
                {
                    profile_hist[p.id].profile = p;
                    profile_hist[p.id].timestamp = Dev.GetUnixTimestamp();
                }
                else profile_hist.Add(p.id, new model.profile_hist(p));

                return new ApiResult<model.profile>(p);
            }
            catch (Exception ex) { return new ApiResult<model.profile>(ex); }
        }
        public static ApiResult<model.profile> FindProfileInHistory(long id)
        {
            try
            {
                var row = profile_hist.FirstOrDefault(e => e.Value.profile.id.Equals(id));
                if (!row.Equals(default(KeyValuePair<long, model.profile_hist>)))
                    return new ApiResult<model.profile>(row.Value.profile);
                return new ApiResult<model.profile>(new Exception("Not found"));
            }
            catch (Exception ex) { return new ApiResult<model.profile>(ex); }
        }
        public static async Task<ApiResult<long>> GetUserId(string username)
        {
            try
            {
                var row = (from element in profile_hist
                           where element.Value.profile.username == username
                           select element).FirstOrDefault();
                if (!row.Equals(default(KeyValuePair<long, model.profile_hist>)))
                    return new ApiResult<long>(row.Value.profile.id);
                var x = await GetProfile(username);
                if (x.isSuccess) return new ApiResult<long>(x.GetResult().id);
                else return new ApiResult<long>(x.GetError());
            }
            catch (Exception ex) { return new ApiResult<long>(ex); }
        }

        public static class web
        {
            public static readonly string Link = "https://www.instagram.com/web/";
            static async Task<ApiResult> Do(model.activity_list.type type, string short_link)
            {
                var limit = account.activity.limits[type];
                try
                {
                    if (limit <= account.activity.activity_list.Count(e => e._type.Equals(type)))
                        throw new Exception(string.Format("Secure system said: type {0} limit {1}", type.ToString(), limit));
                    //var z = MethodBase.GetCurrentMethod().DeclaringType.Name;
                    //var x = typeof(comments).GetType().Name;
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(
                        string.Format("{0}{1}/", Link, short_link)
                    );
                    request.Headers.Add("x-csrftoken", config.csrf_token);
                    var debug_output = await Web.Navigate.Post(request, "");
                    var obj = JsonConvert.DeserializeObject<JObject>(debug_output);
                    var status = obj.GetValue("status").ToObject<Object>();
                    if (status == null) throw new Exception("Field status not found");
                    if (status.ToString() != "ok") throw new Exception(
                        string.Format("Unknown status: {0}", status)
                    );
                    account.activity.UpdateActivityList(type);
                    return new ApiResult(obj);
                }
                catch (Exception ex) { return new ApiResult(ex); }
            }
            public static class comments
            {
                public static async Task<ApiResult> Like(long id) => await Do(
                    model.activity_list.type.Like,
                    string.Format("comments/{0}/like", id)
                );
                public static async Task<ApiResult> Unlike(long id) => await Do(
                    model.activity_list.type.Unlike,
                    string.Format("comments/{0}/unlike", id)
                );
            }
            public static class friendships
            {
                public static async Task<ApiResult> Follow(long user_id) => await Do(
                    model.activity_list.type.Follow,
                    string.Format("friendships/{0}/follow", user_id)
                );
                public static async Task<ApiResult> Unfollow(long user_id) => await Do(
                    model.activity_list.type.Unlike,
                    string.Format("friendships/{0}/unfollow", user_id)
                );
            }
            public static class likes
            {
                public static async Task<ApiResult> Like(long id) => await Do(
                     model.activity_list.type.Like,
                     string.Format("likes/{0}/like", id)
                 );
                public static async Task<ApiResult> Unlike(long id) => await Do(
                    model.activity_list.type.Unlike,
                    string.Format("likes/{0}/unlike", id)
                );
            }
        }
    }
}
