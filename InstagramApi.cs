using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace main
{
    public class ApiResult<T>
    {
        T _result = default;
        Exception _exception = null;
        bool _isCash = false;
        public void SetError(Exception exception) => _exception = exception;
        public Exception GetError() => _exception;
        public ApiResult<T> SetResult(T result, bool isCash = false)
        {
            _isCash = isCash;
            _result = result;
            return this;
        }
        public T GetResult()
        {
            if (_result == null)
                if (_exception != null) throw _exception;
                else return default;
            return _result;
        }
        public bool IsCash() => _isCash;
        public bool IsSuccess { get => _result != null && _exception == null; }
        public ApiResult(Exception exception) => SetError(exception);
        public ApiResult(T result, bool isChsh = false) => SetResult(result, isChsh);
        public ApiResult(List<string> result) => _result = (T)result.Cast<Object>();
        public ApiResult(HashSet<string> result) => _result = JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(result));
        public ApiResult() { }
        public static implicit operator ApiResult<T>(ApiResult v)
        {
            var r = new ApiResult<T>();
            var res = v.GetResult();
            if (!(res is Exception)) r.SetResult((T)v.GetResult(), v.IsCash());
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
        public static implicit operator ApiResult(ApiResult<HashSet<string>> v) => _implicit(v);
        public static implicit operator ApiResult(ApiResult<bool> v) => _implicit(v);
    }
    public static class User
    {
        /// <summary>Сохраненый используемый профиль в локальном хранилище</summary>
        public static model.Profile profile;
        /// <summary>Сохраненая подписота в локальном хранилище</summary>
        public static HashSet<string> followers;
        /// <summary>Сохраненые подписки в локальном хранилище</summary>
        public static HashSet<string> following;
    }
    public static class InstagramApi
    {
        public static readonly string profile_hist_name = @"profile_hist.json";
        public static readonly string ignore_list_name = @"unfollow_ignore_list.json";
        public static readonly string work_list_name = @"work_list.json";
        public static readonly string activity_list_name = @"activity_list.json";
        public static readonly string followers_name = @"followers.json";
        public static readonly string following_name = @"following.json";
        /// <summary>Сохраненая список (история) профилей в локальном хранилище</summary>
        public static ConcurrentDictionary<long, model.Profile_hist> profile_hist = new ConcurrentDictionary<long, model.Profile_hist>();
        /// <summary>Сохраненый список игнорируемых профилей в локальном хранилище</summary>
        public static List<model.Ignore_list_object> ignore_list = new List<model.Ignore_list_object>();
        public static List<model.Work_list> work_list = new List<model.Work_list>();
        public static async Task<ApiResult<bool>> Init()
        {
            try
            {
                try { Log.Init(); }
                catch (Exception ex) { Console.WriteLine("[WARNING]: Log.Init error: {0}", ex.Message); }
                Account.Activity.activity_list = await Config.LoadModule<List<model.Activity_list>>(activity_list_name);
                var Task_config = Config.LoadModule<model.Config>(Config.name);
                var Task_profile_hist = Config.LoadModule<ConcurrentDictionary<long, model.Profile_hist>>(profile_hist_name);
                var Task_ignore_list = Config.LoadModule<List<model.Ignore_list_object>>(ignore_list_name);
                var Task_work_list = Config.LoadModule<List<model.Work_list>>(work_list_name);
                var Task_read_followers = Dev.ReadAsync(followers_name);
                var Task_read_following = Dev.ReadAsync(following_name);
                while (!Task_config.IsCompleted || !Task_profile_hist.IsCompleted ||
                    !Task_ignore_list.IsCompleted || !Task_work_list.IsCompleted ||
                    !Task_read_followers.IsCompleted || !Task_read_following.IsCompleted)
                    Thread.Sleep(33);

                User.followers = JsonConvert.DeserializeObject<HashSet<string>>(Task_read_followers.Result);
                if (User.followers == null) User.followers = new HashSet<string>();
                User.following = JsonConvert.DeserializeObject<HashSet<string>>(Task_read_following.Result);
                if (User.following == null) User.following = new HashSet<string>();
                Config.value = Task_config.Result ?? new model.Config();

                // remove less 24 hours
                //config.value.current_action_follow_list = config.value.current_action_follow_list.Where(
                //    e => e.timestamp >= Dev.GetUnixTimestamp() - (new TimeSpan(24, 0, 0)).TotalSeconds).ToList();
                //config.value.current_action_web_list = config.value.current_action_web_list.Where(
                //                    e => e.timestamp >= Dev.GetUnixTimestamp() - (new TimeSpan(24, 0, 0)).TotalSeconds).ToList();
                //throw new Exception("OK");

                profile_hist = Task_profile_hist.Result ?? new ConcurrentDictionary<long, model.Profile_hist>();
                ignore_list = Task_ignore_list.Result ?? new List<model.Ignore_list_object>();
                work_list = Task_work_list.Result ?? new List<model.Work_list>();

                return new ApiResult<bool>(true);
            }
            catch (Exception ex) { return new ApiResult<bool>(ex); }
        }
        public static class Config
        {
            public static readonly string name = @"config.json";
            public static Dictionary<string, string> consumer = new Dictionary<string, string>()
            {
                {"edge_followed_by", "" },
                {"edge_follow", "" }
            };
            public static Dictionary<string, string> consumercommons = new Dictionary<string, string>()
            {
                // BASE
                {"edge_suggested_users", "" },
                // METRO
                {"SUL_QUERY_ID", "" },
                {"FEED_PAGE_EXTRAS_QUERY_ID", "" },
                {"SUGGESTED_USER_COUNT_QUERY_ID", "" }
            };
            public static Dictionary<string, string> profilepagecontainer = new Dictionary<string, string>()
            {
                { "edge_chaining", "" },
                { "query_hash", "" }
            };
            private static string s_csrf_token = "";
            public static string Csrf_token
            {
                get
                {
                    try
                    {
                        Hashtable table = (Hashtable)main.Web.Cookies.GetType().InvokeMember(
                            "m_domainTable", BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance,
                            null, main.Web.Cookies, new object[] { });
                        var x = JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(table[".instagram.com"]));
                        var a = (x.GetValue("Values") as JArray);
                        foreach (JArray e in a)
                            foreach (JObject o in e)
                                if (o.Value<string>("Name") == "csrftoken")
                                    return s_csrf_token = o.Value<string>("Value");
                    }
                    catch (Exception) { }
                    return s_csrf_token;
                }
                set => s_csrf_token = value;
            }
            public static string rollout_hash = "";
            public static string viewerId = "";
            public static string mode = null;
            public static model.Config value = new model.Config();
            public static async Task<T> LoadModule<T>(string module_name, bool safe_mode = true) where T : new()
            {
                var text = await Dev.ReadAsync(module_name);
                T result = default;
                try { result = JsonConvert.DeserializeObject<T>(text); } catch { }
                if (result == null || result.Equals(default(T)))
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

            Hashtable table = (Hashtable)main.Web.Cookies.GetType().InvokeMember("m_domainTable",
                                                                         BindingFlags.NonPublic |
                                                                         BindingFlags.GetField |
                                                                         BindingFlags.Instance,
                                                                         null,
                                                                         main.Web.Cookies,
                                                                         new object[] { });

            Config.Csrf_token = config.GetValue("csrf_token").ToString();
            Config.viewerId = config.GetValue("viewerId").ToString();
            Config.rollout_hash = o.Value<string>("rollout_hash");
            return o;
        }
        static async Task<ApiResult<model.Followers>> Followers_following_do(string what_do, string user_id, bool _include_reel = true, bool _fetch_mutual = true, long _first = 24, string _after = null)
        {
            try
            {
                if (Config.consumer.ContainsKey(what_do))
                {
                    model.Followers result = new model.Followers();

                    object o = new { id = user_id, include_reel = _include_reel, fetch_mutual = _fetch_mutual, first = _first };
                    if (_after != null) o = new { id = user_id, include_reel = _include_reel, fetch_mutual = _fetch_mutual, first = _first, after = _after };

                    var el = await main.Web.Navigate.Get(
                        main.Web.GenXRequest(
                            string.Format("https://www.instagram.com/graphql/query/?query_hash={0}&variables={1}",
                                Config.consumer[what_do],
                                Uri.EscapeDataString(JsonConvert.SerializeObject(o))
                            )
                        ));
                    if (!el.IsSuccess)
                        throw el.GetError();
                    var data = JsonConvert.DeserializeObject<JObject>(el.GetResult()).GetValue("data") as JObject;
                    var user = data.GetValue("user") as JObject;
                    var edge = user.GetValue(what_do) as JObject;
                    result.count = edge.Value<long>("count");
                    var page_info = edge.GetValue("page_info") as JObject;
                    result.has_next_page = page_info.Value<bool>("has_next_page");
                    result.end_cursor = page_info.Value<string>("end_cursor");
                    var edges = edge.GetValue("edges") as JArray;
                    edges = JArray.FromObject(edges.Select(e => (e as JObject).GetValue("node") as JObject));
                    result.list = JsonConvert.DeserializeObject<List<model.Profile>>(edges.ToString());
                    // edge_followed_by ~ followers
                    // edge_follow      ~ following
                    return new ApiResult(result);
                }
                else throw new Exception(string.Format("\"{0}\" not found in consumer", what_do));
            }
            catch (Exception ex) { return new ApiResult(ex); }
        }
        public static class Followers
        {
            public static async Task<ApiResult<model.Followers>> All(string user_id, bool _include_reel = true, bool _fetch_mutual = true, long count_pre_step = 24)
            {
                try
                {
                    model.Followers result = new model.Followers();
                    model.Followers temp = new model.Followers();
                    string after = null;
                    do
                    {
                        var item = await Get(user_id, _include_reel, _fetch_mutual, count_pre_step, after);
                        if (!item.IsSuccess) throw item.GetError();
                        temp = item.GetResult();
                        result.list.AddRange(temp.list);
                        after = temp.end_cursor;
                        Thread.Sleep(1000);
                    } while (temp.has_next_page);
                    result.count = temp.count;
                    return new ApiResult<model.Followers>(result);
                }
                catch (Exception ex) { return new ApiResult<model.Followers>(ex); }
            }
            public static Task<ApiResult<model.Followers>> Get(
                string user_id, bool _include_reel = true, bool _fetch_mutual = true, long _first = 24, string _after = null) =>
                    Followers_following_do("edge_followed_by", user_id, _include_reel, _fetch_mutual, _first, _after);
        }
        public static class Following
        {
            public static async Task<ApiResult<model.Followers>> All(string user_id, bool _include_reel = true, bool _fetch_mutual = false, long count_pre_step = 24)
            {
                try
                {
                    model.Followers result = new model.Followers();
                    model.Followers temp = new model.Followers();
                    string after = null;
                    do
                    {
                        temp = (await Get(user_id, _include_reel, _fetch_mutual, count_pre_step, after)).GetResult();
                        result.list.AddRange(temp.list);
                        after = temp.end_cursor;
                    } while (temp.has_next_page);
                    result.count = temp.count;
                    return new ApiResult<model.Followers>(result);
                }
                catch (Exception ex) { return new ApiResult<model.Followers>(ex); }
            }
            public static async Task<ApiResult<model.Followers>> Get(
                string user_id, bool _include_reel = true, bool _fetch_mutual = true, long _first = 24, string _after = null) =>
                    await Followers_following_do("edge_follow", user_id, _include_reel, _fetch_mutual, _first, _after);
        }
        public static class Suggest
        {
            //            {"fetch_media_count":0,"fetch_suggested_count":10,"ignore_cache":true,"filter_followed_friends":true,"seen_ids":[],"include_reel":true}
            //            {...,"ignore_cache":false, ... ,"seen_ids":["5392365233", ...], ...}
            public static async Task<ApiResult<model.Suggests>> Get(List<string> _seen_ids, long _fetch_media_count = 0, long _fetch_suggested_count = 10,
                bool _ignore_cache = true, bool _filter_followed_friends = true, bool _include_reel = true)
            {
                try
                {
                    var result = new model.Suggests();
                    var type = (Config.mode == "metro") ? "SUL_QUERY_ID" : "edge_suggested_users";
                    if (_seen_ids.Count > 0) _ignore_cache = false;
                    object o = new
                    {
                        fetch_media_count = _fetch_media_count,
                        fetch_suggested_count = _fetch_suggested_count,
                        ignore_cache = _ignore_cache,
                        filter_followed_friends = _filter_followed_friends,
                        seen_ids = _seen_ids,
                        include_reel = _include_reel
                    };
                    var el = await main.Web.Navigate.Get(
                           main.Web.GenXRequest(
                               string.Format("https://www.instagram.com/graphql/query/?query_hash={0}&variables={1}",
                                   Config.consumercommons[type],
                                   Uri.EscapeDataString(JsonConvert.SerializeObject(o))
                               )
                           ));
                    if (!el.IsSuccess)
                        throw el.GetError();
                    var data = JsonConvert.DeserializeObject<JObject>(el.GetResult()).GetValue("data") as JObject;
                    var user = data.GetValue("user") as JObject;
                    var edge = user.GetValue("edge_suggested_users") as JObject;
                    var page_info = edge.GetValue("page_info") as JObject;
                    result.has_next_page = page_info.Value<bool>("has_next_page");
                    var edges = edge.GetValue("edges") as JArray;
                    edges = JArray.FromObject(edges.Select(e => (e as JObject).GetValue("node") as JObject));
                    result.list = JsonConvert.DeserializeObject<List<model.Suggests.Suggest>>(edges.ToString());
                    return new ApiResult(result);
                }
                catch (Exception ex) { return new ApiResult<model.Suggests>(ex); }
            }

            public static async Task<ApiResult<model.Suggestchains>> GetFromUserId(string _user_id, bool _include_chaining = true, bool _include_reel = true,
                bool _include_suggested_users = false, bool _include_logged_out_extras = false, bool _include_highlight_reels = false)
            {
                // {"user_id":"4180538504","include_chaining":true,"include_reel":true,"include_suggested_users":false,"include_logged_out_extras":false,"include_highlight_reels":false}
                try
                {
                    if (Config.mode != "metro")
                        throw new Exception("Not allowed this mode");
                    var type = "edge_chaining";
                    var result = new model.Suggestchains();
                    var el = await main.Web.Navigate.Get(
                        main.Web.GenXRequest(
                            string.Format("https://www.instagram.com/graphql/query/?query_hash={0}&variables={1}",
                                Config.profilepagecontainer[type],
                                Uri.EscapeDataString(JsonConvert.SerializeObject(new
                                {
                                    user_id = _user_id,
                                    include_chaining = _include_chaining,
                                    include_reel = _include_reel,
                                    include_suggested_users = _include_suggested_users,
                                    include_logged_out_extras = _include_logged_out_extras,
                                    include_highlight_reels = _include_highlight_reels
                                }))
                            )
                        )
                    );
                    if (!el.IsSuccess)
                        throw el.GetError();
                    var data = JsonConvert.DeserializeObject<JObject>(el.GetResult()).GetValue("data") as JObject;
                    var user = data.GetValue("user") as JObject;
                    var edge = user.GetValue(type) as JObject;
                    var edges = edge.GetValue("edges") as JArray;
                    edges = JArray.FromObject(edges.Select(e => (e as JObject).GetValue("node") as JObject));
                    result.list = JsonConvert.DeserializeObject<List<model.Suggestchains.Suggestchain>>(edges.ToString());
                    return new ApiResult(result);
                }
                catch (Exception ex) { return new ApiResult<model.Suggestchains>(ex); }
            }
        }
        public static class Feed
        {
            public static long last_feed_id = 0;
            static long fail_counter = 0;
            public static async Task<ApiResult<model.Feeds>> Get(bool only_new = false)
            {
                var debug_string = "";
                try
                {
                    var result = new model.Feeds();
                    var type = "edge_web_feed_timeline";
                    debug_string = await main.Web.Navigate.Limited.Get(main.Web.GenXRequest("https://www.instagram.com/"), null);
                    debug_string = debug_string.Substring(debug_string.IndexOf("window.__additionalDataLoaded('feed',") + 37);
                    debug_string = debug_string.Substring(0, debug_string.IndexOf(";</script>") - 1);

                    var user = JsonConvert.DeserializeObject<JObject>(debug_string).GetValue("user") as JObject;
                    var edge = user.GetValue(type) as JObject;
                    var page_info = edge.GetValue("page_info") as JObject;
                    result.has_next_page = page_info.Value<bool>("has_next_page");
                    result.end_cursor = page_info.Value<string>("end_cursor");
                    var edges = edge.GetValue("edges") as JArray;
                    edges = JArray.FromObject(edges.Select(e => (e as JObject).GetValue("node") as JObject));
                    result.list = JsonConvert.DeserializeObject<List<model.Feeds.Feed>>(edges.ToString());

                    if (only_new)
                    {
                        int ind = 0;
                        while (ind < result.list.Count)
                        {
                            if (Convert.ToInt64(result.list[ind].id) == last_feed_id)
                                result.list.RemoveRange(ind, result.list.Count - ind);
                            else ind++;
                        }
                    }
                    if (result.list.Count > 0)
                        last_feed_id = Convert.ToInt64(result.list.First().id);
                    fail_counter = 0;
                    return new ApiResult(result);
                }
                catch (Exception ex)
                {
                    if (fail_counter++ > 3) return new ApiResult<model.Feeds>(ex);
                    else
                    {
                        Thread.Sleep((int)(300 * (fail_counter + 1)));
                        return await Get(only_new);
                    }
                }
            }
        }
        public static class Account
        {
            static readonly string Link = "https://www.instagram.com/accounts/";
            public static model.Auth auth = null;
            public static async Task<ApiResult<bool>> Auth(string login, string password)
            {
                var link = string.Format("{0}login/", Link);
                try
                {
                    List<string> mode = new List<string>() { "base", "metro" };
                    var debug = await main.Web.Navigate.Get((HttpWebRequest)WebRequest.Create(link));
                    if (!debug.IsSuccess)
                        throw debug.GetError();
                    var debug_string = debug.GetResult();
                    var consumer_link = debug_string;
                    var consumercommons_link = debug_string;
                    var profilepagecontainer_link = debug_string;

                    var consumer = "";
                    var consumercommons = "";
                    var profilepagecontainer = "";

                    int ind = -1;

                    for (int _i = 0; _i < mode.Count; _i++)
                    {
                        if ((ind = consumer_link.IndexOf(string.Format("/static/bundles/{0}/Consumer.js/", mode[_i]))) >= 0)
                        {
                            Config.mode = mode[_i];

                            consumer_link = string.Format("https://www.instagram.com{0}", consumer_link.Substring(ind));
                            if ((ind = consumer_link.IndexOf("\"")) < 0) throw new Exception("Last step find consumer_link fail");
                            consumer_link = consumer_link.Substring(0, ind);
                            var web_consumer = await main.Web.Navigate.Get((HttpWebRequest)WebRequest.Create(consumer_link));
                            if (!web_consumer.IsSuccess)
                                throw web_consumer.GetError();
                            consumer = web_consumer.GetResult();
                            for (int i = 0; i < Config.consumer.Count; i++)
                            {
                                var k = Config.consumer.ElementAt(i).Key;
                                Regex _r = new Regex(string.Format("[a-z]=(?<field>[a-z]),[a-z]=\"{0}\"", k));
                                var _m = _r.Match(consumer);
                                if (_m.Success)
                                {
                                    var field = _m.Groups["field"].ToString();
                                    var __r = new Regex("(" + field + ")=\"(?<value>[a-z0-9]{32})\"");
                                    var __m = __r.Match(consumer);
                                    if (__m.Success) Config.consumer[k] = __m.Groups["value"].ToString();
                                }
                            }
                        }

                        if ((ind = consumercommons_link.IndexOf(string.Format("/static/bundles/{0}/ConsumerCommons.js/", mode[_i]))) >= 0)
                        {
                            Config.mode = mode[_i];

                            consumercommons_link = string.Format("https://www.instagram.com{0}", consumercommons_link.Substring(ind));
                            if ((ind = consumercommons_link.IndexOf("\"")) < 0) throw new Exception("Last step find consumer_link fail");
                            consumercommons_link = consumercommons_link.Substring(0, ind);
                            var web_consumercommons = await main.Web.Navigate.Get((HttpWebRequest)WebRequest.Create(consumercommons_link));
                            if (!web_consumercommons.IsSuccess)
                                throw web_consumercommons.GetError();
                            consumercommons = web_consumercommons.GetResult();
                        }

                        if ((ind = profilepagecontainer_link.IndexOf(string.Format("/static/bundles/{0}/ProfilePageContainer.js/", mode[_i]))) >= 0)
                        {
                            Config.mode = mode[_i];
                            profilepagecontainer_link = string.Format("https://www.instagram.com{0}", profilepagecontainer_link.Substring(ind));
                            if ((ind = profilepagecontainer_link.IndexOf("\"")) < 0) throw new Exception("Last step find consumer_link fail");
                            profilepagecontainer_link = profilepagecontainer_link.Substring(0, ind);
                            var web_profilepagecontainer = await main.Web.Navigate.Get((HttpWebRequest)WebRequest.Create(profilepagecontainer_link));
                            if (!web_profilepagecontainer.IsSuccess)
                                throw web_profilepagecontainer.GetError();
                            profilepagecontainer = web_profilepagecontainer.GetResult();
                        }
                    }
                    if (Config.mode == "metro") // ALLOW IN METRO MODE
                    {
                        /// --- consumercommons ---
                        Regex r = new Regex("FEED_PAGE_EXTRAS_QUERY_ID=\"(?<FEED_PAGE_EXTRAS_QUERY_ID>[a-z0-9]{32})\"");
                        var m = r.Match(consumercommons);
                        if (m.Success) // ACCESS FOR FEED POSTS
                            Config.consumercommons["FEED_PAGE_EXTRAS_QUERY_ID"] = m.Groups["FEED_PAGE_EXTRAS_QUERY_ID"].ToString();

                        r = new Regex("SUL_QUERY_ID=\"(?<SUL_QUERY_ID>[a-z0-9]{32})\"");
                        m = r.Match(consumercommons);
                        if (m.Success) // ACCESS FOR SUGGESTION FOR YOU
                            Config.consumercommons["SUL_QUERY_ID"] = m.Groups["SUL_QUERY_ID"].ToString();

                        r = new Regex("SUGGESTED_USER_COUNT_QUERY_ID=\"(?<SUGGESTED_USER_COUNT_QUERY_ID>[a-z0-9]{32})\"");
                        m = r.Match(consumercommons);
                        if (m.Success) // ???
                            Config.consumercommons["SUGGESTED_USER_COUNT_QUERY_ID"] = m.Groups["SUGGESTED_USER_COUNT_QUERY_ID"].ToString();

                        /// --- profilepagecontainer ---
                        /// 


                        var cloned_content = profilepagecontainer;
                        cloned_content = cloned_content.Substring(0, cloned_content.IndexOf("PROFILE_POSTS_UPDATED"));
                        r = new Regex("queryId:\"(?<query_hash>[a-z0-9]{32})\"");
                        var mC = r.Matches(cloned_content);
                        if (mC.Count > 0)
                            Config.profilepagecontainer["query_hash"] = mC[mC.Count - 1].Groups["query_hash"].ToString();


                        r = new Regex("=\"(?<edge_chaining>[a-z0-9]{32})\"");
                        m = r.Match(profilepagecontainer);
                        if (m.Success)
                            Config.profilepagecontainer["edge_chaining"] = m.Groups["edge_chaining"].ToString();
                    }
                    else if (Config.mode == "base")
                    {
                        Regex r = new Regex("[a-z],[a-z],[a-z],[a-z]=\"[a-z0-9]{32}\",[a-z]=\"[a-z0-9]{32}\",[a-z]=\"[a-z0-9]{32}\",[a-z]=\"(?<edge_suggested_users>[a-z0-9]{32})\"");
                        var m = r.Match(consumercommons);
                        if (m.Success) Config.consumercommons["edge_suggested_users"] = m.Groups["edge_suggested_users"].ToString();
                    }
                    else throw new Exception("Unknown mode or not found");

                    var o = DecodeSharedData(debug_string);
                    var web_auth = await main.Web.Navigate.Post(
                            main.Web.GenXRequest(link + "ajax/"),
                            string.Format("username={0}&password={1}", login, password) + "&queryParams={}");
                    if (!web_auth.IsSuccess)
                        throw web_auth.GetError();
                    auth = JsonConvert.DeserializeObject<model.Auth>(web_auth.GetResult());
                    var result = !((auth.authenticated != true) /*|| (auth.user != true)*/ || (auth.status != "ok"));
                    if (result)
                        User.profile = (await GetProfile(login, true)).GetResult();
                    return new ApiResult<bool>(result);
                }
                catch (Exception ex) { return new ApiResult<bool>(ex); }
            }
            public static async Task<ApiResult<bool>> Logout()
            {
                try
                {
                    var debug_string = await main.Web.Navigate.Post(
                        main.Web.GenXRequest("https://www.instagram.com/accounts/logout/"),
                        string.Format("csrfmiddlewaretoken={0}", Config.Csrf_token)
                    );
                    return new ApiResult<bool>(true);
                }
                catch (Exception ex) { return new ApiResult<bool>(ex); }
            }
            public static class Activity
            {
                static readonly string Link = Account.Link + "activity/";
                public static double timestamp = 0;
                static readonly TimeSpan cooldown = new TimeSpan(24, 0, 0);
                public static readonly Dictionary<model.Activity_list.Type, long> limits =
                    new Dictionary<model.Activity_list.Type, long>()
                {
                    {
                        // Отписка: интервал 12-22 секунд и не более 1000 в сутки от НЕвзаимных и 1000 от взаимных 
                        // (пауза между невзаимными и взаимными 15 часов);
                        model.Activity_list.Type.Follow,
                        50000
                    },
                    {
                        model.Activity_list.Type.Unfollow,
                        50000
                    },
                    {
                        // Не более одного в течении 28 – 36 секунд (за раз – 1000, перерыв 24 часа);
                        model.Activity_list.Type.Like,
                        50000
                    },
                    {
                        model.Activity_list.Type.Unlike,
                        50000
                    }
                };
                public static List<model.Activity_list> activity_list = new List<model.Activity_list>();
                public static async void UpdateActivityList(model.Activity_list.Type type)
                {
                    activity_list = activity_list.Where(e => (Dev.GetUnixTimestamp() - e.timestamp) <= cooldown.TotalSeconds).ToList();
                    activity_list.Add(new model.Activity_list(type));
                    try
                    {
                        await Dev.WriteAsync(activity_list_name, JsonConvert.SerializeObject(activity_list));
                    }
                    catch { }
                }
                public static async Task<ApiResult<List<model.Activity>>> Load(bool only_new = false, bool include_reel = true)
                {
                    List<model.Activity> result = new List<model.Activity>();
                    try
                    {
                        var debug = await main.Web.Navigate.Get((HttpWebRequest)WebRequest.Create(
                            string.Format("{0}?__a=1&include_reel={1}", Link, include_reel.ToString().ToLower())));
                        if (!debug.IsSuccess)
                            throw debug.GetError();
                        var debug_string = debug.GetResult();
                        var obj = JsonConvert.DeserializeObject<JObject>(debug_string)
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
                                model.Activity a = JsonConvert.DeserializeObject<model.Activity>(node.ToString());
                                if (!(a.timestamp > timestamp)) break;
                                if (a.type != model.Activity.Type.GraphGdprConsentStory)
                                {
                                    var user = o.Value<JObject>("node").Value<JObject>("user");
                                    var profile = await GetProfile(a.user.username);
                                    if (!profile.IsSuccess) throw profile.GetError();
                                    a.user = profile.GetResult();
                                    //if (include_reel) a.timestamp = Convert.ToInt64(user.Value<JObject>("reel").Value<string>("expiring_at"));
                                    result.Add(a);
                                }
                            }
                            catch (Exception ex) { return new ApiResult<List<model.Activity>>(new Exception(string.Format("o = {0}", o.ToString()), ex)); }
                        }
                        var _result = new ApiResult<List<model.Activity>>((only_new) ? result.Where(e => e.timestamp > timestamp).ToList() : result);
                        if (_result.GetResult().Count > 0) timestamp = _result.GetResult().First().timestamp;
                        return _result;
                    }
                    catch (Exception ex) { return new ApiResult<List<model.Activity>>(ex); }
                }
            }
            public static class Access_tool
            {
                public class Act
                {
                    public enum Type { unknown = 0, followers_follow = 1, followers_unfollow = 2, following_follow = 3, following_unfollow = 4 };
                    public Type type;
                    public string username;
                    public double timestamp = Dev.GetUnixTimestamp();
                    public Act(string username, Type type)
                    {
                        this.username = username;
                        this.type = type;
                    }
                    public override string ToString() => string.Format("{0} has been {1}", username, type.ToString());
                }
                public static List<Act> current_action_follow_list = new List<Act>();
                public class _cv
                {
                    public long current = 0;
                    public long update_target = 0;
                }
                static readonly string Link = Account.Link + "access_tool/";
                static readonly Dictionary<string, _cv> Current_value = new Dictionary<string, _cv>()
                {
                    {"current_follow_requests", new _cv() },
                    {"accounts_following_you", new _cv() },
                    {"accounts_you_follow", new _cv() }
                };
                static async Task UpdateList(string Link, int offset, int count, HashSet<string> b)
                {
                    HashSet<string> _a = null;
                    Act.Type ftype = Act.Type.unknown;
                    Act.Type uftype = Act.Type.unknown;
                    if (Link == "accounts_following_you")
                    {
                        _a = User.followers;
                        ftype = Act.Type.followers_follow;
                        uftype = Act.Type.followers_unfollow;
                    }
                    else if (Link == "accounts_you_follow")
                    {
                        _a = User.following;
                        ftype = Act.Type.following_follow;
                        uftype = Act.Type.following_unfollow;
                    }
                    if ((Current_value[Link].update_target == 0) || (_a == null))
                        return;

                    var a = _a.Range(offset, count);
                    var x1 = a.Except(b).ToHashSet();
                    var x2 = b.Except(a).ToHashSet();
                    var prof1 = x1.Count > 0 ? x1.First() : null;
                    var prof2 = x2.Count > 0 ? x2.First() : null;
                    var p1 = (x1.Count > 0) ? a.IndexOf(prof1) : -1;
                    var p2 = (x2.Count > 0) ? b.IndexOf(prof2) : -1;
                    if (p1 > -1) prof1 = a.ElementAt(p1);
                    if (p2 > -1) prof2 = b.ElementAt(p2);
                    while (p1 > -1 || p2 > -1)
                    {
                        if (p1 > -1 && p2 > -1)
                        {
                            if (p1 < p2)
                            {
                                if (!_a.Remove(prof1))
                                    throw new Exception(string.Format("Cant delete element \"{0}\", not found", prof1));
                                Current_value[Link].update_target++;
                                current_action_follow_list.Add(new Act(prof1, uftype));
                            }
                            else if (p1 > p2)
                            {
                                var code = _a.Insert(offset + p2, prof2, true);
                                if (code == 0)
                                    await Log.Write(new List<string>() { string.Format("Error insert {0}", prof2) }, Log.Type.warning);
                                else if ((code & 3) == 3)
                                    await Log.Write(new List<string>() { string.Format("Used override for insert({0}, {1}, true)", offset + p2, prof2) }, Log.Type.warning);
                                Current_value[Link].update_target--;
                                current_action_follow_list.Add(new Act(prof2, ftype));
                            }
                            else
                            {
                                // 0 ~ false, 1 ~ true, 3 ~ true + isOverrided
                                var code = _a.Replace(offset + p2, prof2);
                                if (code == 0)
                                    await Log.Write(new List<string>() { string.Format("Replace({0}, {1}) return 0!", offset + p2, prof2) }, Log.Type.warning);
                                else
                                {
                                    current_action_follow_list.Add(new Act(prof1, uftype));
                                    if ((code & 3) == 1) current_action_follow_list.Add(new Act(prof2, ftype));
                                    else Current_value[Link].update_target++;
                                }

                                //_a[_a.ElementAt(offset + p2).Key] = x2[v2];
                            }
                        }
                        else if (p1 > -1)
                        {
                            current_action_follow_list.Add(new Act(prof1, uftype));
                            if (!_a.Remove(prof1))
                                throw new Exception(string.Format("Cant delete element \"{0}\", not found", prof1));
                            Current_value[Link].update_target++;
                        }
                        else if (p2 > -1)
                        {
                            var code = _a.Insert(offset + p2, prof2, true);
                            if (code == 0)
                                await Log.Write(new List<string>() { string.Format("Error insert {0}", prof2) }, Log.Type.warning);
                            else if ((code & 3) == 3)
                                await Log.Write(new List<string>() { string.Format("Used override for insert({0}, {1}, true)", offset + p2, prof2) }, Log.Type.warning);
                            Current_value[Link].update_target--;
                            current_action_follow_list.Add(new Act(prof2, ftype));
                        }

                        a = _a.Range(offset, count);
                        x1 = a.Except(b).ToHashSet();
                        x2 = b.Except(a).ToHashSet();
                        prof1 = x1.Count > 0 ? x1.First() : null;
                        prof2 = x2.Count > 0 ? x2.First() : null;
                        p1 = (x1.Count > 0) ? a.IndexOf(prof1) : -1;
                        p2 = (x2.Count > 0) ? b.IndexOf(prof2) : -1;
                        if (p1 > -1) prof1 = a.ElementAt(p1);
                        if (p2 > -1) prof2 = b.ElementAt(p2);
                    }
                }
                static async Task<ApiResult<HashSet<string>>> LoadALL(string Link, long first_update = 0)
                {
                    string _Link = Link;
                    Current_value[_Link] = new _cv
                    {
                        update_target = first_update
                    };
                    Link = Access_tool.Link + string.Format("{0}?__a=1", Link);
                    HashSet<string> result = new HashSet<string>();
                    try
                    {
                        var debug = await main.Web.Navigate.Get((HttpWebRequest)WebRequest.Create(Link));
                        if (!debug.IsSuccess)
                            throw debug.GetError();
                        var debug_string = debug.GetResult();
                        JObject data = JsonConvert.DeserializeObject<JObject>(debug_string).GetValue("data") as JObject;

                        HashSet<string> _list = ((JArray)data.GetValue("data")).Select(e => ((JObject)e).GetValue("text").ToString()).ToHashSet();
                        await UpdateList(_Link, result.Count, _list.Count, _list);
                        result.AddRange(_list);
                        Current_value[_Link].current += _list.Count;

                        object cursor = null;
                        while (((cursor = data.GetValue("cursor").ToObject<object>()) != null) && (Current_value[_Link].update_target != 0))
                        {
                            debug = await main.Web.Navigate.Get((HttpWebRequest)WebRequest.Create(string.Format("{0}&cursor={1}", Link, cursor)));
                            if (!debug.IsSuccess)
                                throw debug.GetError();
                            debug_string = debug.GetResult();
                            data = JsonConvert.DeserializeObject<JObject>(debug_string).GetValue("data") as JObject;
                            _list = ((JArray)data.GetValue("data")).Select(e => ((JObject)e).GetValue("text").ToString()).ToHashSet();
                            await UpdateList(_Link, result.Count, _list.Count, _list);
                            result.AddRange(_list);
                            Current_value[_Link].current += _list.Count;
                        }
                        if (Current_value[_Link].update_target != 0)
                            await Log.Write(new List<string>() {
                                string.Format("~ update_target for {0} = {1}", _Link, Current_value[_Link].update_target)
                            }, Log.Type.warning);
                    }
                    catch (Exception ex) { return new ApiResult<HashSet<string>>(ex); }
                    //return new ApiResult<HashSet<string>>(result);
                    return new ApiResult<HashSet<string>>(_Link == "accounts_following_you" ? User.followers : _Link == "accounts_you_follow" ? User.following : result);
                }
                public static class Current_follow_requests
                {
                    static readonly string name = "current_follow_requests";
                    public static long Current_pos => Current_value[name].current;
                    //public static long current_max => User.profile.followers;
                    //Current_value[name].max;
                    public static Task<ApiResult<HashSet<string>>> LoadALL(long first_update = 0) => Access_tool.LoadALL(name, first_update);
                }
                public static class Accounts_following_you
                {
                    static readonly string name = "accounts_following_you";
                    public static long Current_pos => Current_value[name].current;
                    public static long Current_max => User.profile.followers;
                    //Current_value[name].max;
                    public static Task<ApiResult<HashSet<string>>> LoadALL(long first_update = 0) => Access_tool.LoadALL(name, first_update);
                }
                public static class Accounts_you_follow
                {
                    static readonly string name = "accounts_you_follow";
                    public static long Current_pos => Current_value[name].current;
                    public static long Current_max => User.profile.following;
                    //Current_value[name].max;
                    public static Task<ApiResult<HashSet<string>>> LoadALL(long first_update = 0) => Access_tool.LoadALL(name, first_update);
                }
            }
        }
        public static class Challenge
        {
            public static bool IsNeed() => Account.auth != null && Account.auth.message.Equals("checkpoint_required");
            public static async Task<ApiResult<List<model.Challenge.Field>>> GetChoices()
            {
                try
                {
                    if (Account.auth == null)
                        throw new Exception("Challenge available after auth");
                    var debug = await main.Web.Navigate.Get((HttpWebRequest)WebRequest.Create(
                        string.Format("https://www.instagram.com{0}?__a=1", Account.auth.checkpoint_url)
                    ));
                    if (!debug.IsSuccess)
                        throw debug.GetError();
                    var debug_string = debug.GetResult();
                    JObject obj = JsonConvert.DeserializeObject<JObject>(debug_string);
                    var challengeType = obj.Value<string>("challengeType");
                    if (!challengeType.Equals("SelectVerificationMethodForm"))
                        throw new Exception(string.Format("Unknown challenge type: {0}", challengeType));
                    var extraData = obj.Value<JObject>("extraData");
                    var content = extraData.Value<JArray>("content");
                    for (var i = 0; i < content.Count; i++)
                    {
                        var __typename = content[i].Value<string>("__typename");
                        if (__typename.Equals("GraphChallengePageForm"))
                        {
                            var fields = content[i].Value<JArray>("fields")[0] as JObject;
                            debug_string = fields.Value<JArray>("values").ToString();
                            var result = JsonConvert.DeserializeObject<List<model.Challenge.Field>>(debug_string);
                            return new ApiResult<List<model.Challenge.Field>>(result);
                        }
                    }
                    return new ApiResult<List<model.Challenge.Field>>(new List<model.Challenge.Field>());
                }
                catch (Exception ex) { return new ApiResult<List<model.Challenge.Field>>(ex); }
            }
            public static async Task<ApiResult<model.Challenge.Result>> SendChoice(int choice)
            {
                try
                {
                    if (Account.auth == null)
                        throw new Exception("SendChoice available after auth");

                    var debug = await main.Web.Navigate.Post(
                        main.Web.GenXRequest(string.Format("https://www.instagram.com{0}", Account.auth.checkpoint_url)),
                        string.Format("choice={0}", choice)
                    );
                    if (!debug.IsSuccess)
                        throw debug.GetError();
                    var debug_string = debug.GetResult();
                    return new ApiResult<model.Challenge.Result>(JsonConvert.DeserializeObject<model.Challenge.Result>(debug_string));
                }
                catch (Exception ex) { return new ApiResult<model.Challenge.Result>(ex); }
            }
            public static async Task<ApiResult<model.Challenge.Result>> SendAnswer(int security_code)
            {
                try
                {
                    if (Account.auth == null)
                        throw new Exception("SendAnswer available after auth");
                    var debug = await main.Web.Navigate.Post(
                        main.Web.GenXRequest(string.Format("https://www.instagram.com{0}", Account.auth.checkpoint_url)),
                        string.Format("security_code={0}", security_code)
                    );
                    if (!debug.IsSuccess)
                        throw debug.GetError();
                    var debug_string = debug.GetResult();
                    return new ApiResult<model.Challenge.Result>(JsonConvert.DeserializeObject<model.Challenge.Result>(debug_string));
                }
                catch (Exception ex) { return new ApiResult<model.Challenge.Result>(ex); }
            }
        }
        public static async Task<ApiResult<model.Profile>> GetProfile(long id, bool ignore_hash = false)
        {
            try
            {
                if (!ignore_hash)
                {
                    var api_profile_hist = FindProfileInHistory(id);
                    if (api_profile_hist.IsSuccess)
                    {
                        var profile_hist = api_profile_hist.GetResult();
                        var avg = Dev.GetUnixTimestamp() - profile_hist.timestamp;
                        if (!(avg > new TimeSpan(72, 0, 0).TotalSeconds))
                            return new ApiResult<model.Profile>(profile_hist.profile, true);
                    }
                }
                var res = await main.Web.Navigate.GetRedirectLocation(
                    string.Format("https://www.instagram.com/web/friendships/{0}/follow/", id));
                if (res == null)
                    throw new Exception("Cant get redirect location");
                res = res.Substring(26);
                res = res.Substring(0, res.Length - 1);
                return await GetProfile(res, ignore_hash);
            }
            catch (Exception ex) { return new ApiResult<model.Profile>(ex); }
        }
        public static async Task<ApiResult<model.Profile>> GetProfile(string username, bool ignore_hash = false)
        {
            try
            {
                if (!ignore_hash)
                {
                    var api_profile_hist = FindProfileInHistory(username);
                    if (api_profile_hist.IsSuccess)
                    {
                        var profile_hist = api_profile_hist.GetResult();
                        var avg = Dev.GetUnixTimestamp() - profile_hist.timestamp;
                        if (!(avg > new TimeSpan(72, 0, 0).TotalSeconds))
                            return new ApiResult<model.Profile>(profile_hist.profile, true);
                    }
                }
                var debug = await main.Web.Navigate.Get((HttpWebRequest)WebRequest.Create(
                    string.Format("https://www.instagram.com/{0}/?__a=1", username)
                ));
                if (!debug.IsSuccess)
                    throw debug.GetError();
                var debug_string = debug.GetResult();
                JObject o = JsonConvert.DeserializeObject<JObject>(debug_string);
                JObject obj = (o.GetValue("graphql") as JObject).GetValue("user") as JObject;
                var p = JsonConvert.DeserializeObject<model.Profile>(obj.ToString());
                p.followers = Convert.ToInt64(((JObject)obj.GetValue("edge_followed_by")).GetValue("count"));
                p.following = Convert.ToInt64(((JObject)obj.GetValue("edge_follow")).GetValue("count"));

                var yT = obj.GetValue("edge_mutual_followed_by") as JObject;
                p.mutual_followed_by = new model.Profile.Edge_mutual_followed_by
                {
                    items = new List<string>(),
                    count = yT.Value<long>("count")
                };
                var arr = yT.GetValue("edges").Value<JArray>();
                foreach (JObject e in arr)
                {
                    var node = e.GetValue("node") as JObject;
                    p.mutual_followed_by.items.Add(node.Value<string>("username"));
                }

                var xT = obj.GetValue("edge_owner_to_timeline_media") as JObject;
                var page_info_obj = xT.GetValue("page_info") as JObject;
                p.posts = new model.Posts
                {
                    count = xT.Value<long>("count"),
                    has_next_page = page_info_obj.Value<bool>("has_next_page"),
                    end_cursor = page_info_obj.Value<string>("end_cursor")
                };
                arr = xT.GetValue("edges").Value<JArray>();
                foreach (JObject e in arr)
                {
                    var node = e.GetValue("node") as JObject;
                    model.Post post = JsonConvert.DeserializeObject<model.Post>(node.ToString());
                    post.comment_count = (node.GetValue("edge_media_to_comment") as JObject).Value<long>("count");
                    post.like_count = (node.GetValue("edge_liked_by") as JObject).Value<long>("count");
                    p.posts.Add(post);
                }
                if (profile_hist.ContainsKey(p.id))
                {
                    profile_hist[p.id].profile = p;
                    profile_hist[p.id].timestamp = Dev.GetUnixTimestamp();
                }
                else
                    if (!profile_hist.TryAdd(p.id, new model.Profile_hist(p)))
                    await Log.Write(new List<string>() { "TryAdd return false" }, Log.Type.warning);

                return new ApiResult<model.Profile>(p);
            }
            catch (Exception ex) { return new ApiResult<model.Profile>(ex); }
        }
        public static ApiResult<model.Profile_hist> FindProfileInHistory(long id)
        {
            try
            {
                return new ApiResult<model.Profile_hist>(
                    profile_hist.First(e => e.Value.profile.id.Equals(id)).Value,
                    true
                );
            }
            catch (Exception ex) { return new ApiResult<model.Profile_hist>(ex); }
        }
        public static ApiResult<model.Profile_hist> FindProfileInHistory(string username)
        {
            try
            {
                return new ApiResult<model.Profile_hist>(
                    profile_hist.First(e => e.Value.profile.username.Equals(username)).Value,
                    true
                );
            }
            catch (Exception ex) { return new ApiResult<model.Profile_hist>(ex); }
        }
        public static async Task<ApiResult<long>> GetUserId(string username)
        {
            try
            {
                var row = (from element in profile_hist
                           where element.Value.profile.username == username
                           select element).FirstOrDefault();
                if (!row.Equals(default(KeyValuePair<long, model.Profile_hist>)))
                    return new ApiResult<long>(row.Value.profile.id);
                var x = await GetProfile(username);
                if (x.IsSuccess) return new ApiResult<long>(x.GetResult().id);
                else return new ApiResult<long>(x.GetError());
            }
            catch (Exception ex) { return new ApiResult<long>(ex); }
        }
        public static class Web
        {
            public static readonly string Link = "https://www.instagram.com/web/";
            static async Task<ApiResult> Do(model.Activity_list.Type type, string short_link)
            {
                var limit = Account.Activity.limits[type];
                try
                {
                    if (limit <= Account.Activity.activity_list.Count(e => e.type.Equals(type)))
                        throw new Exception(string.Format("Secure system said: type {0} limit {1}", type.ToString(), limit));
                    //var z = MethodBase.GetCurrentMethod().DeclaringType.Name;
                    //var x = typeof(comments).GetType().Name;
                    var debug = await main.Web.Navigate.Post(main.Web.GenXRequest(string.Format("{0}{1}/", Link, short_link)), "");
                    if (!debug.IsSuccess)
                        throw debug.GetError();
                    var debug_string = debug.GetResult();
                    var obj = JsonConvert.DeserializeObject<JObject>(debug_string);
                    var status = obj.GetValue("status").ToObject<Object>();
                    if (status == null) throw new Exception("Field status not found");
                    if (status.ToString() != "ok") throw new Exception(
                        string.Format("Unknown status: {0}", status)
                    );
                    Account.Activity.UpdateActivityList(type);
                    return new ApiResult(obj);
                }
                catch (Exception ex) { return new ApiResult(ex); }
            }
            public static class Posts
            {
                /// <param name="after">end_cursor</param>
                public static async Task<ApiResult<model.Posts>> Load(string id, string after, int first = 12)
                {
                    var debug = await main.Web.Navigate.Get(
                        main.Web.GenXRequest(
                        string.Format("https://www.instagram.com/graphql/query/?query_hash={0}&variables={1}",
                            Config.profilepagecontainer["query_hash"],
                            Uri.EscapeDataString(JsonConvert.SerializeObject(new { id, first, after })))
                    ));
                    if (!debug.IsSuccess)
                        throw debug.GetError();
                    var debug_string = debug.GetResult();
                    JObject o = JsonConvert.DeserializeObject<JObject>(debug_string),
                        obj = (o.GetValue("data") as JObject).GetValue("user") as JObject;
                    var xT = obj.GetValue("edge_owner_to_timeline_media") as JObject;
                    var page_info = xT.GetValue("page_info") as JObject;
                    var posts = new model.Posts
                    {
                        count = xT.Value<long>("count"),
                        has_next_page = page_info.Value<bool>("has_next_page"),
                        end_cursor = page_info.Value<string>("end_cursor")
                    };
                    foreach (JObject e in xT.GetValue("edges").Value<JArray>())
                    {
                        var node = e.GetValue("node") as JObject;
                        var post = JsonConvert.DeserializeObject<model.Post>(node.ToString());
                        post.comment_count = (node.GetValue("edge_media_to_comment") as JObject).Value<long>("count");
                        post.like_count = (node.GetValue("edge_media_preview_like") as JObject).Value<long>("count");
                        posts.Add(post);
                    }
                    return new ApiResult<model.Posts>(posts);
                }
            }
            public static class Comments
            {
                public static async Task<ApiResult> Like(long id) => await Do(
                    model.Activity_list.Type.Like,
                    string.Format("comments/{0}/like", id)
                );
                public static async Task<ApiResult> Unlike(long id) => await Do(
                    model.Activity_list.Type.Unlike,
                    string.Format("comments/{0}/unlike", id)
                );
            }
            public static class Friendships
            {
                public static async Task<ApiResult> Follow(long user_id) => await Do(
                    model.Activity_list.Type.Follow,
                    string.Format("friendships/{0}/follow", user_id)
                );
                public static async Task<ApiResult> Unfollow(long user_id) => await Do(
                    model.Activity_list.Type.Unfollow,
                    string.Format("friendships/{0}/unfollow", user_id)
                );
            }
            public static class Likes
            {
                public static async Task<ApiResult> Like(string id) => await Do(
                     model.Activity_list.Type.Like,
                     string.Format("likes/{0}/like", id)
                 );
                public static async Task<ApiResult> Unlike(string id) => await Do(
                    model.Activity_list.Type.Unlike,
                    string.Format("likes/{0}/unlike", id)
                );
            }
        }
    }
}
