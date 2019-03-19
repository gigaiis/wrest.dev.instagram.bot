using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace main
{
    public class Program
    {
        public static double init_time = Dev.GetUnixTimestamp();

        public static Log.Row log_autounfollow, log_ignored;
        public static model.Profile preupdate_profile = new model.Profile();
        public static Thread thread = new Thread(new ThreadStart(async () =>
        {
            var sleep_next_unfollow = new TimeSpan(0, 0, 15);
            var sleep_try_next = new TimeSpan(0, 15, 0);
            ApiResult api = null;
            try
            {
                Console.Write("Init...");
                if (!(api = await InstagramApi.Init()).IsSuccess)
                    throw api.GetError();
                Console.WriteLine("Done.");
                if (User.profile != null)
                {
                    preupdate_profile.id = User.profile.id;
                    preupdate_profile.followers = User.profile.followers;
                    preupdate_profile.following = User.profile.following;
                }
                var Log_auth = await Log.Write(new List<string>() { "Auth..." });
                while (true)
                {
                    Console.Write("Auth...");
                    var Task_auth = InstagramApi.Account.Auth(Login, Password);
                    using (var progress = new ProgressBar())
                    {
                        int i = 0;
                        while (!Task_auth.IsCompleted)
                        {
                            progress.Report((double)i / 100);
                            Thread.Sleep(33);
                            i++;
                            if (i > 100) i = 0;
                        }
                    }
                    if (!Task_auth.Result.IsSuccess)
                        throw Task_auth.Result.GetError();
                    if (Task_auth.Result.GetResult())
                        break;
                    var auth = InstagramApi.Account.auth;
                    if (auth.IsLock.HasValue && auth.IsLock.Value)
                        throw new Exception(string.Format("Locked: {0}", auth.ToString()));
                    else if (InstagramApi.Challenge.IsNeed())
                    {
                        var l = await InstagramApi.Challenge.GetChoices();
                        if (!l.IsSuccess)
                            throw l.GetError();
                        var l_r = l.GetResult();
                        Console.WriteLine("Instagram challenge, you choice: ");
                        foreach (var c in l_r)
                            Console.WriteLine("{0} - {1}", c.value, c.text);
                        var choice = 0;
                        while (!int.TryParse(Console.ReadLine(), out choice) &&
                            l_r.Count(e => e.value == choice) <= 0)
                            Console.WriteLine("Try again");
                        var sc_api = await InstagramApi.Challenge.SendChoice(choice);
                        if (!sc_api.IsSuccess)
                            throw sc_api.GetError();
                        var sc = sc_api.GetResult();
                        if (!sc.status.Equals("ok"))
                            throw new Exception("Error");
                        Console.WriteLine("Enter security code: ");
                        var security_code = 0;
                        while (!int.TryParse(Console.ReadLine(), out security_code))
                            Console.WriteLine("Try again");
                        var sa_api = await InstagramApi.Challenge.SendAnswer(security_code);
                        if (!sa_api.IsSuccess)
                            throw sa_api.GetError();
                        var sa = sa_api.GetResult();
                        Console.WriteLine("Result = {0}", sa.status);
                    }
                    else
                        throw new Exception("Unknown challenge");
                }
                #region new method get followers/following and any
                //var __followers = new List<model.profile>();
                //var __z = (await InstagramApi.followers.Get(Convert.ToString(User.user_id), true, true, 24)).GetResult();
                //while (__z.has_next_page)
                //{
                //    __followers.AddRange(__z.list);
                //    //Thread.Sleep(5000);
                //    __z = (await InstagramApi.followers.Get(Convert.ToString(User.user_id), true, false, 12, __z.end_cursor)).GetResult();
                //}

                //var test_result2 = await InstagramApi.following.All(Convert.ToString(User.user_id));
                //var test_result = await InstagramApi.followers.All(Convert.ToString(User.user_id));

                //user_id = (await InstagramApi.GetUserId(Login)).GetResult();

                //var activity = await InstagramApi.account.activity.Load();
                //if (!activity.isSuccess) throw activity.GetError();
                #endregion
                Log_auth.text[0] += string.Format("Done. -> followers: {0}, following: {1}", User.profile.followers, User.profile.following);
                Console.WriteLine("Done. -> followers: {0}, following: {1}", User.profile.followers, User.profile.following);
                #region transform string to list
                //var name = "friend_list.json";
                //var st_load_obj = JsonConvert.DeserializeObject<List<string>>(await Dev.ReadAsync(name));
                //var result = new List<long>();
                //var count = st_load_obj.Count;
                //var _targ = 0;
                //var o = Log.Write(new List<string>() { string.Format("{0} of {1}", _targ, count) });
                //foreach (var i in st_load_obj)
                //{
                //    _targ++;
                //    var api = await InstagramApi.GetProfile(i);
                //    var profile = api.GetResult();
                //    result.Add(profile.id);
                //    o.text[0] = string.Format("{0} of {1}", _targ, count);
                //    if (!api.isCash())
                //        Thread.Sleep(new TimeSpan(0, 1, 0));
                //}
                //await Dev.WriteAsync(name, JsonConvert.SerializeObject(result));
                #endregion
                #region follow_request [OLD]

                //Console.Write("Load follow request...");
                // List<string> listUnfollow = new List<string>();
                //var follow_request = await InstagramApi.account.access_tool.current_follow_requests.LoadALL();   // Запросы на подписоту (приват)
                //if (follow_request.isSuccess)
                //{
                //    while (listUnfollow.Count > 0)
                //    {
                //        try
                //        {
                //            if (!(api_res = await InstagramApi.GetUserId(listUnfollow.First())).isSuccess) throw api_res.GetError();
                //            if (!(api_res = await InstagramApi.web.friendships.Unfollow(User.user_id)).isSuccess) throw api_res.GetError();
                //            Console.WriteLine("Success unfollow, user_id: {0}", User.user_id);
                //            listUnfollow.RemoveAt(0);
                //            Thread.Sleep(sleep_next_unfollow);
                //        }
                //        catch (Exception ex)
                //        {
                //            Console.WriteLine("Error: {0}\r\nWait {1}", ex.Message, sleep_try_next.ToString());
                //            Thread.Sleep(sleep_try_next);
                //        }
                //    }
                //}
                //else throw follow_request.GetError();
                //Console.WriteLine("Done.");

                #endregion


                // await User.profile.LoadMorePosts();


                var Task_followers = Task.Run(() => new ApiResult<HashSet<string>>(User.followers));
                var Task_following = Task.Run(() => new ApiResult<HashSet<string>>(User.following));
                var targ_update = 0;
                log_ignored = await Log.Write(new List<string>() { "[Ignored]", "Wait..." }, Log.Type.info);
                log_autounfollow = Log.Write(new List<string>() { "[Auto unfollow]", "Wait..." }, Log.Type.info).Result;
                var isFllowUpdate = false;
                while (true)
                {
                    isFllowUpdate = true;
                    if (preupdate_profile.id != User.profile.id)
                    {
                        // Change account

                        Console.WriteLine("Load followers {0}", User.profile.followers);
                        Task_followers = InstagramApi.Account.Access_tool.Accounts_following_you.LoadALL(
                             User.profile.followers);          // Наша подписота
                        Console.WriteLine("Load following {0}", User.profile.following);
                        Task_following = InstagramApi.Account.Access_tool.Accounts_you_follow.LoadALL(
                             User.profile.following);             // Мы подписаны 
                    }
                    else
                    {
                        if (preupdate_profile.followers != User.profile.followers)
                        {
                            Console.WriteLine("Change count followers {0} -> {1}", preupdate_profile.followers, User.profile.followers);
                            Task_followers = InstagramApi.Account.Access_tool.Accounts_following_you.LoadALL(
                                User.profile.followers - preupdate_profile.followers);          // Наша подписота
                        }
                        if (preupdate_profile.following != User.profile.following)
                        {
                            Console.WriteLine("Change count following {0} -> {1}", preupdate_profile.following, User.profile.following);
                            Task_following = InstagramApi.Account.Access_tool.Accounts_you_follow.LoadALL(
                                User.profile.following - preupdate_profile.following);             // Мы подписаны 
                        }
                    }
                    Console.Write("Task_followers...");
                    using (var progress = new ProgressBar())
                    {
                        while (!Task_followers.IsCompleted)
                        {
                            progress.Report(
                                (double)InstagramApi.Account.Access_tool.Accounts_following_you.Current_pos /
                                InstagramApi.Account.Access_tool.Accounts_following_you.Current_max);
                            Thread.Sleep(33);
                        }
                    }
                    Console.WriteLine("Done.");
                    Console.Write("Task_following...");
                    using (var progress = new ProgressBar())
                    {
                        while (!Task_following.IsCompleted)
                        {
                            progress.Report(
                                (double)InstagramApi.Account.Access_tool.Accounts_you_follow.Current_pos /
                                InstagramApi.Account.Access_tool.Accounts_you_follow.Current_max);
                            Thread.Sleep(33);
                        }
                    }
                    Console.WriteLine("Done.");
                    preupdate_profile.id = User.profile.id;
                    preupdate_profile.followers = User.profile.followers;
                    preupdate_profile.following = User.profile.following;

                    Log.isWork = true;

                    var api_followers = Task_followers.Result;
                    var api_following = Task_following.Result;

                    if (!api_followers.IsSuccess)
                        throw new Exception("Followers return error", api_followers.GetError());
                    if (!api_following.IsSuccess)
                        throw new Exception("Following return error", api_following.GetError());

                    User.followers = api_followers.GetResult();
                    User.following = api_following.GetResult();

                    Console.WriteLine("followers = {0}", User.followers.Count);
                    Console.WriteLine("following = {0}", User.following.Count);

                    var x = User.following.Except(User.followers).ToHashSet();

                    #region Unfollow ignore list
                    //Console.WriteLine("\t --- unfollow ignore list [{0}] ---", InstagramApi.ignore_list.Count);
                    //foreach (var i in InstagramApi.ignore_list)
                    //{
                    //    var profile = InstagramApi.FindProfileInHistory(i.id);
                    //    if (profile.isSuccess) Console.WriteLine("{0}", profile.GetResult().username);
                    //    else Console.WriteLine(" >>> Error find profile in hist [{0}]: {1} <<<", i, profile.GetError().Message);
                    //}
                    #endregion
                    #region Current unfollow filst
                    //Console.WriteLine("\t --- Current unfollow list ---");
                    //foreach (var i in x)
                    //    Console.WriteLine("{0}", i.username);
                    //Console.WriteLine("\t --- Count = {0} ---", x.Count);
                    #endregion
                    #region Enter unfollow ignore list elements
                    //if (targ_update == 0)
                    //{
                    //    Console.WriteLine("\r\nEnter unfollow ignore or empty for next step");
                    //    var l = "";
                    //    while ((l = Console.ReadLine()) != "")
                    //    {
                    //        try
                    //        {
                    //            var UserPage = await InstagramApi.GetUserId(l);
                    //            if (UserPage.isSuccess)
                    //            {
                    //                var id = UserPage.GetResult();
                    //                if (InstagramApi.ignore_list.Contains(new model.ignore_list_object(id))) throw new Exception("User already exist");
                    //                InstagramApi.ignore_list.Add(new model.ignore_list_object(id));
                    //            }
                    //            else throw UserPage.GetError();
                    //        }
                    //        catch (Exception ex) { Console.WriteLine(ex.Message); }
                    //    }
                    //}
                    #endregion

                    int max_counter = x.Count;

                    #region OLD

                    //using (var progress = new ProgressBar())
                    //{
                    //    while (ind < x.Count)
                    //    {
                    //        try
                    //        {

                    //        }
                    //        catch (WebException ex)
                    //        {
                    //            HttpWebResponse response = (HttpWebResponse)ex.Response;
                    //            var code = response.StatusCode;
                    //            Console.WriteLine("Server return error: {0} for username: {1}", code, x.ElementAt(ind));
                    //            if (Convert.ToInt32(code) == 429)
                    //            {
                    //                string err = "Too many request per second!";
                    //                Console.WriteLine("{0}\r\nwait 3 min...", err);
                    //                await Log.Write(new List<string>() { string.Format("WebException {0}", err) }, Log.Type.error);

                    //                var Task_auto_save =
                    //                    Dev.WriteAsync(InstagramApi.profile_hist_name, JsonConvert.SerializeObject(InstagramApi.profile_hist));

                    //                Thread.Sleep(new TimeSpan(0, 3, 0));

                    //                while (!Task_auto_save.IsCompleted)
                    //                    Thread.Sleep(33);
                    //            }
                    //            else if (code == HttpStatusCode.NotFound)
                    //            {
                    //                x.RemoveAt(ind);
                    //            }
                    //            else throw new Exception(string.Format("Unknown code WebException: [{0}]", Convert.ToInt32(code)), ex);
                    //        }
                    //        catch (Exception ex)
                    //        {
                    //            await Log.Write(new List<string>() { "Exception", ex.Message, ex.StackTrace }, Log.Type.error);
                    //            Console.WriteLine("\r\n >>>Error for username: {0}<<< \r\n", x.ElementAt(ind));
                    //            throw ex;
                    //        }
                    //        progress.Report((double)++unflw_targ / unflw_max);
                    //    }
                    //}
                    //Console.WriteLine("Done.");

                    #endregion
                    int unflw_max = x.Count;
                    log_ignored.text.RemoveRange(1, log_ignored.text.Count - 1);
                    var z = User.following.Except(x).ToHashSet();       // work list
                    var max_unfollow_count = Math.Min(new Random().Next(3, 7), x.Count);
                    var s_max_unfollow_count = max_unfollow_count;
                    Console.WriteLine("Start unfollow from {0} of {1}", max_unfollow_count, x.Count);

                    log_autounfollow.text[1] = string.Format("[{0}] Start unfollow from {1} of {2}",
                        DateTime.Now.ToString("MM/dd/yyyy hh:mm tt"),
                        s_max_unfollow_count - max_unfollow_count,
                        s_max_unfollow_count);
                    var ind = 0;
                    while ((ind < x.Count) && (x.Count > 0) && (max_unfollow_count > 0))
                    {
                        try
                        {
                            var e_x = x.ElementAt(ind);
                            // Console.Write("Update info {0}...", e_x);

                            var api_profile_hist = InstagramApi.FindProfileInHistory(e_x);
                            if (!api_profile_hist.IsSuccess)
                            {
                                var api_profile = await InstagramApi.GetProfile(x.ElementAt(ind), true);
                                if (!api_profile.IsSuccess)
                                    throw api_profile.GetError();
                                api_profile_hist.SetResult(new model.Profile_hist(api_profile.GetResult()));
                            }
                            var profile = api_profile_hist.GetResult().profile;
                            var id = profile.id;
                            InstagramApi.ignore_list = InstagramApi.ignore_list.Where(e => !e.IsExpired()).ToList();
                            var obj = new model.Ignore_list_object(id);
                            var indx = -1;
                            if ((indx = InstagramApi.ignore_list.IndexOf(obj)) >= 0)
                            {
                                var el = InstagramApi.ignore_list.ElementAt(indx);
                                var time = el.expired_in == 0 ? 0 :
                                    Dev.TimestampToDateTime(el.expired_in).Subtract(DateTime.UtcNow).TotalHours;
                                var koff = time / 36;
                                var R = 255 * (1 - koff);
                                var G = 255 * koff;
                                log_ignored.text.Insert(1,
                                    string.Format(
                                        "<div style=\"margin: 10px 0px 0px 0px; background-color: rgba({4},{5},0,{3:0.##});\">" +
                                            "<img src=\"{0}\" style=\"border-radius: 50px; height: 34; width: 34px; margin: 0px 8px 0px 0px;\">" +
                                            "<a target=\"_blank\" href='https://www.instagram.com/{1}/'>{1}</a>" +
                                            (time != 0 ? "<span> ~ {2:0.##} hour(s) to unfollow</span>" : "") +
                                        "</div>",
                                        profile.profile_pic_url,
                                        profile.username,
                                        time,
                                        koff,
                                        (int)R, (int)G
                                    )
                                );
                                x.RemoveAt(ind);
                            }
                            else if (max_unfollow_count > 0)
                            {
                                if (await profile.Unfollow())
                                {
                                    Console.WriteLine("Ok");
                                    log_autounfollow.text.Insert(2, string.Format("[{0}] <a target=\"_blank\" href='https://www.instagram.com/{1}/' style='color: green;'>{1}</a>",
                                        DateTime.Now.ToString("hh:mm:ss tt"), e_x.ToString()));
                                }
                                else
                                {
                                    Console.Write("Err");
                                    log_autounfollow.type = Log.Type.error;
                                    log_autounfollow.text.Insert(2, string.Format("[{0}] <a target=\"_blank\" href='https://www.instagram.com/{1}/' style='color: red;'>{1}</a>",
                                        DateTime.Now.ToString("hh:mm:ss tt"), e_x.ToString()));
                                }
                                var sel = InstagramApi.work_list.Where(e => e.profile.Equals(profile)).ToList();
                                InstagramApi.work_list = InstagramApi.work_list.Except(sel).ToList();
                                max_unfollow_count--;
                                log_autounfollow.text[1] = string.Format("[{0}] Start unfollow from {1} of {2}",
                                    DateTime.Now.ToString("MM/dd/yyyy hh:mm tt"),
                                    s_max_unfollow_count - max_unfollow_count,
                                    s_max_unfollow_count);
                                x.RemoveAt(ind);
                                Thread.Sleep(sleep_next_unfollow);
                            }
                            else
                                ind++;
                            if (!api_profile_hist.IsCash())
                                Thread.Sleep(3334);
                        }
                        catch (Exception ex)
                        {
                            await Log.Write(new List<string>() { "Exception", ex.Message, ex.StackTrace }, Log.Type.error);
                            Console.WriteLine("Error in final task: {0}\r\nWait {1}", ex.Message, sleep_try_next.ToString());
                            Thread.Sleep(sleep_try_next);
                        }
                    }

                    Console.Write("Save...");
                    try
                    {
                        await Dev.WriteAsync(InstagramApi.followers_name, JsonConvert.SerializeObject(User.followers));
                        await Dev.WriteAsync(InstagramApi.following_name, JsonConvert.SerializeObject(User.following));
                        await Log.Save();
                        await Dev.WriteAsync(InstagramApi.ignore_list_name, JsonConvert.SerializeObject(InstagramApi.ignore_list));
                        await Dev.WriteAsync(InstagramApi.Config.name, JsonConvert.SerializeObject(InstagramApi.Config.value));
                        await Dev.WriteAsync(InstagramApi.profile_hist_name, JsonConvert.SerializeObject(InstagramApi.profile_hist));
                        await Dev.WriteAsync(InstagramApi.work_list_name, JsonConvert.SerializeObject(InstagramApi.work_list));
                        Console.WriteLine("Done.");
                    }
                    catch (Exception ex) { Console.WriteLine("{0}.", ex.Message); }

                    isFllowUpdate = false;
                    if (targ_update == 0)
                    {
                        #region task_auto_like_feed

                        var task_auto_like_feed = Task.Run(async () =>
                        {
                            var min_wait_time = 5000;
                            var max_wait_time = 240000;
                            var dynamic_sleep = 10000;
                            var max_items = 11;
                            var step = 0;
                            long _Last_feed_id = 0;
                            var work_list_feed = new List<model.Feeds.Feed>();

                            var log = await Log.Write(new List<string>() { "Start task_auto_like_feed" }, Log.Type.info);
                            try
                            {
                                var task_listener = Task.Run(async () =>
                                {
                                    while (true)
                                    {
                                        var work_list_count = work_list_feed.Count;
                                        if (work_list_count > 0)
                                        {
                                            var i = 0;
                                            while (i < work_list_count)
                                            {
                                                var itm = work_list_feed[i];
                                                log.text.Insert(1, string.Format("[{0}] Like feed item {1}", DateTime.Now.ToString("hh:mm:ss tt"), itm));
                                                if (!await itm.Like())
                                                    throw new Exception(string.Format("[{0}] Error like feed item: {1}", DateTime.Now.ToString("hh:mm:ss tt"), itm));
                                                Thread.Sleep(new Random().Next(2000, 3500));
                                                i++;
                                            }
                                            work_list_feed.RemoveRange(0, work_list_count);
                                        }
                                        Thread.Sleep(33);
                                    }
                                });
                                while (true)
                                {
                                    var api_feed = await InstagramApi.Feed.Get(true);
                                    if (!api_feed.IsSuccess)
                                        throw api_feed.GetError();
                                    var result = api_feed.GetResult();
                                    if (result.list.Count > 0)
                                    {
                                        work_list_feed.AddRange(result.list);
                                        if (step != 0)
                                            dynamic_sleep -= (dynamic_sleep - min_wait_time) * (result.list.Count / max_items);   // - 9%..100%
                                    }
                                    else
                                    {
                                        dynamic_sleep = (int)Math.Round(dynamic_sleep * 1.1);   // + 10%
                                        if (dynamic_sleep > max_wait_time) dynamic_sleep = max_wait_time;
                                    }
                                    log.text[0] = string.Format("Work task_auto_like_feed [step = {0}, dynamic_sleep = {1}]", step++, dynamic_sleep);
                                    try
                                    {
                                        if (_Last_feed_id != InstagramApi.Feed.last_feed_id)
                                        {
                                            _Last_feed_id = InstagramApi.Feed.last_feed_id;
                                            log.text.Add(string.Format("[{0}] Save Last_feed_id = {1}", DateTime.Now.ToString("hh:mm:ss tt"), _Last_feed_id));
                                            await Dev.WriteAsync(InstagramApi.Config.name, JsonConvert.SerializeObject(InstagramApi.Config.value));
                                        }
                                    }
                                    catch { }
                                    Thread.Sleep(dynamic_sleep);
                                }
                            }
                            catch (Exception ex)
                            {
                                log.type = Log.Type.error;
                                log.text.AddRange(new List<string>() { "ERROR", ex.Message, ex.StackTrace });
                            }
                            log.text.Add("END TASK");
                        });

                        #endregion
                        #region task_auto_like_activiry_users

                        //var task_auto_like_activiry_users = Task.Run(async () =>
                        //{
                        //    #region get activity
                        //    //var activity = (await InstagramApi.account.activity.Load(true)).GetResult();
                        //    //Console.WriteLine("\t --- Activity list ---");
                        //    //foreach (var i in activity)
                        //    //{
                        //    //    Console.WriteLine(" >>> {0} => {1} <<<", i.user.ToString(), i._type.ToString());
                        //    //}
                        //    //Console.WriteLine("\t --- Count = {0} ---", activity.Count);
                        //    #endregion
                        //    var work_list_activity = new List<model.activity>();

                        //    var min_wait_time = 5000;
                        //    var max_wait_time = 240000;
                        //    var dynamic_sleep = 30000;
                        //    var max_items = 100;
                        //    var step = 0;
                        //    var o = Log.Write(new List<string>() { "Start task_auto_like_activiry_users" }, Log.Type.info);
                        //    try
                        //    {
                        //        var task_listener = Task.Run(async () =>
                        //        {
                        //            while (true)
                        //            {
                        //                var work_list_count = work_list_activity.Count;
                        //                if (work_list_count > 0)
                        //                {
                        //                    var i = 0;
                        //                    while (i < work_list_count)
                        //                    {
                        //                        var itm = work_list_activity[i];
                        //                        o.text.Add(string.Format("-> {0} => {1} <<<", itm.user.username, itm._type.ToString()));
                        //                        if (itm._type == model.activity.type.GraphLikeAggregatedStory)
                        //                        {
                        //                            var api_profile = await InstagramApi.GetProfile(itm.user.username);
                        //                            if (!api_profile.isSuccess) throw api_profile.GetError();
                        //                            if (!api_profile.isCash())
                        //                            {
                        //                                var profile = api_profile.GetResult();
                        //                                int i1 = 0;
                        //                                while ((i1 < profile.posts.Count) && (i1 < 2))
                        //                                {
                        //                                    if (!await profile.posts[i1].Like())
                        //                                        throw new Exception(string.Format("Cant like post {0}", profile.posts[i1]));
                        //                                    Thread.Sleep(new Random().Next(1000, 2000));
                        //                                    i1++;
                        //                                }
                        //                                Thread.Sleep(new Random().Next(2000, 3500));
                        //                            }
                        //                        }
                        //                        i++;
                        //                    }
                        //                    work_list_activity.RemoveRange(0, work_list_count);
                        //                }
                        //                Thread.Sleep(33);
                        //            }
                        //        });
                        //        while (true)
                        //        {
                        //            var api = await InstagramApi.account.activity.Load(true);
                        //            if (!api.isSuccess) throw api.GetError();
                        //            var result = api.GetResult();
                        //            if (result.Count > 0)
                        //            {
                        //                work_list_activity.AddRange(result);
                        //                if (step != 0)
                        //                    dynamic_sleep -= ((dynamic_sleep - min_wait_time) * (result.Count / max_items));   // - 1%..100%
                        //            }
                        //            else
                        //            {
                        //                dynamic_sleep = (int)Math.Round(dynamic_sleep * 1.4);
                        //                if (dynamic_sleep > max_wait_time) dynamic_sleep = max_wait_time;
                        //            }
                        //            o.text[0] = string.Format("Work task_auto_like_activiry_users [step = {0}, dynamic_sleep = {1}]", step++, dynamic_sleep);
                        //            Thread.Sleep(dynamic_sleep);
                        //        }
                        //    }
                        //    catch (Exception ex)
                        //    {
                        //        o.type = Log.Type.error;
                        //        Log.Write(new List<string>() { "ERROR", ex.Message, ex.StackTrace }, Log.Type.error);
                        //    }
                        //});

                        #endregion
                        #region task_auto_follow_on_suggest_chain

                        var task_auto_follow_on_suggest_chain = Task.Run(async () =>
                        {
                            var work_list_activity = new List<model.Activity>();
                            HashSet<string> work_list = new HashSet<string>(z);

                            var log = await Log.Write(new List<string>() { "Start task_auto_follow_on_suggest_chain" }, Log.Type.info);
                            try
                            {
                                var friend_name = "friend_list.json";
                                var friend_list = JsonConvert.DeserializeObject<List<long>>(await Dev.ReadAsync(friend_name));
                                var friend_list_username = new List<string>();
                                foreach (var i in friend_list)
                                {
                                    var api_profile = await InstagramApi.GetProfile(i);
                                    if (!api_profile.IsSuccess) throw api_profile.GetError();
                                    model.Profile profile = api_profile.GetResult();
                                    friend_list_username.Add(profile.username);
                                    work_list.Remove(profile.username);     // EXPEREMENTAL 
                                    if (!api_profile.IsCash())
                                        Thread.Sleep(500);
                                }

                                while (work_list.Count > 0)
                                {
                                    try
                                    {
                                        var i1 = new Random().Next(work_list.Count);

                                        while (isFllowUpdate)
                                            Thread.Sleep(333);

                                        model.Profile current_profile = null;
                                        var isUnfollowed = false;
                                        var work_item = work_list.ElementAt(i1);
                                        if (!work_list.RemoveAt(i1))
                                            throw new Exception("Can't remove work item");

                                        var api_profile_hist = InstagramApi.FindProfileInHistory(work_item);
                                        if (api_profile_hist.IsSuccess)
                                        {
                                            var profile_hist = api_profile_hist.GetResult();
                                            current_profile = profile_hist.profile;
                                            isUnfollowed = profile_hist.unfollowed_at > 0;
                                        }
                                        else
                                        {
                                            var api_profile1 = await InstagramApi.GetProfile(work_item, true);
                                            if (!api_profile1.IsSuccess)
                                                throw api_profile1.GetError();
                                            current_profile = api_profile1.GetResult();
                                        }

                                        if (isUnfollowed)
                                            continue;

                                        var a1 = friend_list_username.Except(friend_list_username.Except(current_profile.mutual_followed_by.items).ToList()).ToList();
                                        if (a1.Count > 0)   // Have mutual friends 
                                        {
                                            Thread.Sleep(3000);
                                            continue;
                                        }
                                        Thread.Sleep(new Random().Next(1500, 4500));

                                        while (isFllowUpdate)
                                            Thread.Sleep(333);

                                        var api_suggest = await InstagramApi.Suggest.GetFromUserId(Convert.ToString(current_profile.id));
                                        if (!api_suggest.IsSuccess)
                                            throw api_suggest.GetError();
                                        var suggestchains = api_suggest.GetResult().list;

                                        while (suggestchains.Count > 0)
                                        {
                                            try
                                            {
                                                var i2 = new Random().Next(suggestchains.Count);
                                                var chain = suggestchains.ElementAt(i2);

                                                api_profile_hist = InstagramApi.FindProfileInHistory(Convert.ToInt64(chain.id));
                                                if (api_profile_hist.IsSuccess)
                                                {
                                                    var profile_hist = api_profile_hist.GetResult();
                                                    current_profile = profile_hist.profile;
                                                    isUnfollowed = profile_hist.unfollowed_at > 0;
                                                }
                                                else
                                                {
                                                    var api_profile1 = await InstagramApi.GetProfile(chain.username, true);
                                                    if (!api_profile1.IsSuccess)
                                                        throw api_profile1.GetError();
                                                    current_profile = api_profile1.GetResult();
                                                }

                                                if (!isUnfollowed && !chain.blocked_by_viewer &&
                                                    !chain.follows_viewer && !chain.followed_by_viewer &&
                                                    !chain.has_blocked_viewer && !chain.has_requested_viewer &&
                                                    !chain.is_private)
                                                {
                                                    Thread.Sleep(new Random().Next(1500, 4500));

                                                    while (isFllowUpdate)
                                                        Thread.Sleep(333);

                                                    var a2 = friend_list_username.Except(friend_list_username.Except(current_profile.mutual_followed_by.items).ToList()).ToList();
                                                    var K = (double)current_profile.followers / current_profile.following;
                                                    if (a2.Count > 0 || K > 10)   // Have mutual friends 
                                                    {
                                                        Thread.Sleep(3000);
                                                        if (!suggestchains.Remove(chain))
                                                            throw new Exception("Can't remove chain");
                                                        continue;
                                                    }
                                                    log.text.Insert(1, string.Format("[{0}] <a target=\"_blank\" href='https://www.instagram.com/{1}/'>{1}{2}</a> " +
                                                        "[K = {3:0.##}], posts_count = {4} of {5}",
                                                        DateTime.Now.ToString("hh:mm:ss tt"),
                                                        current_profile.username,
                                                        "<img src=\"" + current_profile.profile_pic_url + "\" style=\"border-radius: 50px; " +
                                                            "height: 34; width: 34px;\">",
                                                        K,
                                                        current_profile.posts.Count, current_profile.posts.count));

                                                    while (isFllowUpdate)
                                                        Thread.Sleep(333);

                                                    log.text[1] += " -> Follow";
                                                    if (!await current_profile.Follow())
                                                        throw new Exception(string.Format("Cant follow on {0}", current_profile));
                                                    var time_unfollow = Dev.GetUnixTimestamp() + ((long)Math.Round(new TimeSpan(36, 0, 0).TotalSeconds));

                                                    while (isFllowUpdate)
                                                        Thread.Sleep(333);

                                                    InstagramApi.ignore_list.Add(new model.Ignore_list_object(
                                                        current_profile.id,
                                                        time_unfollow  // 36 hours and unfollow if expired
                                                    ));
                                                    try
                                                    {
                                                        await Dev.WriteAsync(InstagramApi.ignore_list_name, JsonConvert.SerializeObject(InstagramApi.ignore_list));
                                                    }
                                                    catch { }

                                                    log.text[1] += string.Format("[unflw at {0}]", Dev.TimestampToDateTime(time_unfollow));
                                                    Thread.Sleep(1000);
                                                    var count = Math.Min(current_profile.posts.Count, 2);
                                                    log.text[1] += string.Format(" -> Like posts[count = {0}]: ", count);
                                                    var posts = current_profile.posts;
                                                    for (int _p = 0; _p < count; _p++)
                                                    {
                                                        var indx = new Random().Next(posts.Count);
                                                        var post = posts[indx];
                                                        posts.RemoveAt(indx);

                                                        while (isFllowUpdate)
                                                            Thread.Sleep(333);

                                                        log.text[1] +=
                                                            string.Format("<a target=\"_blank\" href='https://www.instagram.com/p/{0}/' style='color: {1}'>{0}</a>; ",
                                                            post.shortcode,
                                                            await post.Like() ? "green" : "red"
                                                        );
                                                        Thread.Sleep(1000);
                                                    }
                                                    Thread.Sleep(new Random().Next(180000, 240000));
                                                }
                                                if (!suggestchains.Remove(chain))
                                                    throw new Exception("Can't remove chain");
                                            }
                                            catch (Exception ex)
                                            {
                                                log.type = Log.Type.error;
                                                log.text.AddRange(new List<string>() { "ERROR", ex.Message, ex.StackTrace });
                                                log.text.Add("Wait 10 min [r1]");
                                                Thread.Sleep(new TimeSpan(0, 10, 0));
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        log.type = Log.Type.error;
                                        log.text.AddRange(new List<string>() { "ERROR", ex.Message, ex.StackTrace });
                                        log.text.Add("Wait 10 min [r2]");
                                        Thread.Sleep(new TimeSpan(0, 10, 0));
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                log.type = Log.Type.error;
                                log.text.AddRange(new List<string>() { "ERROR", ex.Message, ex.StackTrace });
                                log.text.Add("Wait 10 min [r3]");
                                Thread.Sleep(new TimeSpan(0, 10, 0));
                            }
                            log.text.Add("END TASK");
                        });

                        #endregion
                    }

                    #region task_auto_follow_on_suggests

                    //if (targ_update == 0 || targ_update % 2 == 0)
                    //{
                    //    var task_auto_follow_on_suggests = Task.Run(async () =>
                    //    {
                    //        // await InstagramApi.suggest.GetFromUserId(Convert.ToString(User.user_id));

                    //        var o = await Log.Write(new List<string>() { "Start task_auto_follow_on_suggests" }, Log.Type.info);
                    //        try
                    //        {
                    //            var ignore_name = "ignore_list.json";
                    //            var ignore_list = JsonConvert.DeserializeObject<HashSet<long>>(await Dev.ReadAsync(ignore_name));
                    //            if (ignore_list == null) ignore_list = new HashSet<long>();

                    //            var friend_name = "friend_list.json";
                    //            var friend_list = JsonConvert.DeserializeObject<List<long>>(await Dev.ReadAsync(friend_name));
                    //            var friend_list_username = new List<string>();
                    //            foreach (var i in friend_list)
                    //            {
                    //                var api_profile = await InstagramApi.GetProfile(i); //InstagramApi.FindProfileInHistory(i);
                    //                if (!api_profile.IsSuccess) throw api_profile.GetError();
                    //                model.Profile profile = api_profile.GetResult();
                    //                friend_list_username.Add(profile.username);
                    //            }

                    //            if (ignore_list.Count > 25)
                    //            {
                    //                var new_lsit = new HashSet<long>();
                    //                while (new_lsit.Count < 25)
                    //                {
                    //                    var i = new Random().Next(ignore_list.Count);
                    //                    new_lsit.Add(ignore_list.ElementAt(i));
                    //                    ignore_list.RemoveAt(i);
                    //                }
                    //            }

                    //            var api = await InstagramApi.Suggest.Get(ignore_list.Select(e => Convert.ToString(e)).ToList());
                    //            if (!api.IsSuccess) throw api.GetError();
                    //            var result = api.GetResult();
                    //            var work_profiles = new List<model.Profile>();
                    //            foreach (var i in result.list)
                    //            {
                    //                if (i.type == model.Suggests.Suggest.Type.unknown)
                    //                    await Log.Write(new List<string>() { string.Format("Unknown description detected! [{0}]", i.Description) }, Log.Type.warning);
                    //                else if (i.type == model.Suggests.Suggest.Type.in_your_contacts)
                    //                    ignore_list.Add(i.user.id);
                    //                else
                    //                {
                    //                    var api_current_profile = await InstagramApi.GetProfile(i.user.username, true);
                    //                    if (!api_current_profile.IsSuccess) throw api_current_profile.GetError();

                    //                    var current_profile = api_current_profile.GetResult();
                    //                    var first_mutual_followed_by_list = current_profile.mutual_followed_by.items;
                    //                    if (current_profile.mutual_followed_by.count > 1)
                    //                    {
                    //                        var a = friend_list_username.Except(friend_list_username.Except(first_mutual_followed_by_list).ToList()).ToList();
                    //                        if (a.Count == 0)   // No mutual friends 
                    //                        {
                    //                            o.text.Add(string.Format("-> {0} [K = {1:0.##}], posts_count = {2} of {3}, isPrivate = {4}",
                    //                                current_profile.username,
                    //                                (double)current_profile.followers / current_profile.following,
                    //                                current_profile.posts.Count,
                    //                                current_profile.posts.count,
                    //                                current_profile.is_private));
                    //                            if (current_profile.is_private || current_profile.posts.count == 0)
                    //                                ignore_list.Add(current_profile.id);        //work_profiles.Add(current_profile);
                    //                            else
                    //                            {
                    //                                o.text[o.text.Count - 1] += " -> Follow";
                    //                                if (User.following.Contains(current_profile.username))
                    //                                    throw new Exception(string.Format("WTF? {0} already exist in following list", current_profile));
                    //                                if (!await current_profile.Follow())
                    //                                    throw new Exception(string.Format("Cant follow on {0}", current_profile));
                    //                                var time_unfollow = Dev.GetUnixTimestamp() +
                    //                                    ((long)Math.Round((new TimeSpan(36, 0, 0)).TotalSeconds));
                    //                                InstagramApi.ignore_list.Add(
                    //                                    new model.Ignore_list_object(
                    //                                        current_profile.id,
                    //                                        time_unfollow  // 36 hours and unfollow if expired
                    //                                    )
                    //                                );
                    //                                o.text[o.text.Count - 1] += string.Format("[unflw at {0}]", Dev.TimestampToDateTime(time_unfollow));
                    //                                Thread.Sleep(1000);
                    //                                var count = Math.Min(current_profile.posts.Count, 2);
                    //                                o.text[o.text.Count - 1] += string.Format(" -> Like posts[count = {0}]: ", count);
                    //                                for (int _p = 0; _p < count; _p++)
                    //                                {
                    //                                    var post = current_profile.posts[_p];
                    //                                    if (!await post.Like())
                    //                                        throw new Exception(string.Format("Cant like post {0}", current_profile.posts[_p]));
                    //                                    o.text[o.text.Count - 1] += string.Format("{0}; ", post.shortcode);
                    //                                    Thread.Sleep(1000);
                    //                                }
                    //                            }
                    //                        }
                    //                        else ignore_list.Add(current_profile.id);
                    //                    }
                    //                    else ignore_list.Add(current_profile.id);
                    //                    if (!api_current_profile.IsCash()) Thread.Sleep(1000);
                    //                }
                    //            }

                    //            await Dev.WriteAsync(ignore_name, JsonConvert.SerializeObject(ignore_list));
                    //            o.text[0] = "Success end task_auto_follow_on_suggests";
                    //        }
                    //        catch (Exception ex)
                    //        {
                    //            o.type = Log.Type.error;
                    //            await Log.Write(new List<string>() { "ERROR", ex.Message, ex.StackTrace }, Log.Type.error);
                    //        }
                    //    });
                    //}

                    #endregion

                    targ_update++;

                    Console.Write("Wait...");
                    int targ = 0;
                    using (var progress = new ProgressBar())
                    {
                        while (targ < 100)
                        {
                            progress.Report((double)++targ / 100);
                            Thread.Sleep(3 * 1000);
                        }
                    }
                    Console.Clear();
                    if (!await User.profile.Reload())
                        throw new Exception("Can't reload profile");
                }
                // Console.WriteLine("End task");
            }
            catch (Exception ex)
            {
                Console.BackgroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("Exception in thread: {0}\r\n{1}", ex.Message, ex.StackTrace);
                if (ex.InnerException != null)
                    Console.WriteLine(
                        "[InnerException]: \r\n\tMessage = {0}\r\n{1}",
                        ex.InnerException.Message,
                        ex.InnerException.StackTrace
                    );
            }
            try
            {
                await InstagramApi.Account.Logout();
            }
            catch (Exception ex)
            {
                Console.BackgroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("Exception in thread [logout]: {0}\r\n{1}", ex.Message, ex.StackTrace);
                if (ex.InnerException != null)
                    Console.WriteLine(
                        "[InnerException]: \r\n\tMessage = {0}\r\n{1}",
                        ex.InnerException.Message,
                        ex.InnerException.StackTrace
                    );
            }
            Thread.Sleep(new TimeSpan(0, 5, 0));
            await Dev.RestartApp();
            thread.Abort();
        }));
        public static async Task Main(string[] args)
        {
            Console.BackgroundColor = ConsoleColor.DarkBlue;
            var server = Task.Run(() => { return new HTTPServer.Server(IPAddress.Any, 7000); });
            thread.Start();
            while (thread != null && thread.ThreadState != ThreadState.Aborted)
                Thread.Sleep(33);
            await Task.Run(async () =>
            {
                Console.Write("Save...");
                try
                {
                    await Log.Save();
                    await Dev.WriteAsync(InstagramApi.ignore_list_name, JsonConvert.SerializeObject(InstagramApi.ignore_list));
                    await Dev.WriteAsync(InstagramApi.Config.name, JsonConvert.SerializeObject(InstagramApi.Config.value));
                    await Dev.WriteAsync(InstagramApi.profile_hist_name, JsonConvert.SerializeObject(InstagramApi.profile_hist));
                    await Dev.WriteAsync(InstagramApi.work_list_name, JsonConvert.SerializeObject(InstagramApi.work_list));
                    Console.WriteLine("Success");
                }
                catch (Exception ex) { Console.WriteLine("Error: {0}", ex.Message); }
            });
            Console.WriteLine("Press key to exit");
            Console.ReadKey();
        }
    }
}
