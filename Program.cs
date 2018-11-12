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
        public static int init_time = Dev.GetUnixTimestamp();
        

        public static Thread thread = new Thread(new ThreadStart(async () =>
        {
            ApiResult api_res = null;
            try
            {
                Console.Write("Init...");
                if (!(api_res = (await InstagramApi.Init())).isSuccess) throw api_res.GetError();
                Console.WriteLine("Done.");

                var preupdate_profile = User.user_profile == null ? new model.profile() : User.user_profile;

                var Log_auth = Log.Write(new List<string>() { "Auth..." });
                Console.Write("Auth...");
                var Task_auth = InstagramApi.account.Auth(Login, Password);
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

                Log.Write(new List<string>());
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

                Log_auth.text[0] += string.Format("Done. -> followers: {0}, following: {1}", User.user_profile.followers, User.user_profile.following);
                Console.WriteLine("Done. -> followers: {0}, following: {1}", User.user_profile.followers, User.user_profile.following);

                Console.WriteLine("\t --- Current ignore list ---");
                foreach (var i in InstagramApi.ignore_list)
                {
                    var profile = InstagramApi.FindProfileInHistory(i);
                    if (profile.isSuccess) Console.WriteLine("{0}", profile.GetResult().username);
                    else Console.WriteLine(" >>> Error find profile in hist [{0}]: {1} <<<", i, profile.GetError().Message);
                }
                Console.WriteLine("\t --- Count = {0} ---", InstagramApi.ignore_list.Count);
                Console.WriteLine("\r\nEnter ignore or empty for next step");
                var l = "";
                while ((l = Console.ReadLine()) != "")
                {
                    try
                    {
                        var UserPage = await InstagramApi.GetUserId(l);
                        if (UserPage.isSuccess)
                        {
                            var id = UserPage.GetResult();
                            if (InstagramApi.ignore_list.Contains(id)) throw new Exception("User already exist");
                            InstagramApi.ignore_list.Add(id);
                        }
                        else throw UserPage.GetError();
                    }
                    catch (Exception ex) { Console.WriteLine(ex.Message); }
                }
                await Dev.WriteAsync(InstagramApi.ignore_list_name, JsonConvert.SerializeObject(InstagramApi.ignore_list));

                //var _test = await InstagramApi.suggest.Get(new List<string>() { });
                //if (!_test.isSuccess) throw _test.GetError();
                //foreach (var i in _test.GetResult().list)
                //    Console.WriteLine(i.ToString());


                //var activity = (await InstagramApi.account.activity.Load(true)).GetResult();
                //Console.WriteLine("\t --- Activity list ---");
                //foreach (var i in activity)
                //{
                //    Console.WriteLine(" >>> {0} => {1} <<<", i.user.ToString(), i._type.ToString());
                //}
                //Console.WriteLine("\t --- Count = {0} ---", activity.Count);


                //thread.Abort();
                //return;

                User.followers = JsonConvert.DeserializeObject<Dictionary<string, model.profile>>(
                    await Dev.ReadAsync("followers.json")
                );
                User.following = JsonConvert.DeserializeObject<Dictionary<string, model.profile>>(
                    await Dev.ReadAsync("following.json")
                );

                var sleep_next_unfollow = new TimeSpan(0, 0, 15);
                var sleep_try_next = new TimeSpan(0, 15, 0);

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

                var Task_followers = Task.Run(() => new ApiResult<Dictionary<string, model.profile>>(User.followers));
                var Task_following = Task.Run(() => new ApiResult<Dictionary<string, model.profile>>(User.following));

                while (true)
                {
                    if (preupdate_profile.id != User.user_profile.id)
                    {
                        Console.WriteLine("Change count followers {0} -> {1}", preupdate_profile.followers, User.user_profile.followers);
                        Task_followers = InstagramApi.account.access_tool.accounts_following_you.LoadALL(
                            User.user_profile.followers - preupdate_profile.followers);          // Наша подписота
                        Console.WriteLine("Change count following {0} -> {1}", preupdate_profile.following, User.user_profile.following);
                        Task_following = InstagramApi.account.access_tool.accounts_you_follow.LoadALL(
                            User.user_profile.following - preupdate_profile.following);             // Мы подписаны 
                    }
                    else
                    {
                        if (preupdate_profile.followers != User.user_profile.followers)
                        {
                            Console.WriteLine("Change count followers {0} -> {1}", preupdate_profile.followers, User.user_profile.followers);
                            Task_followers = InstagramApi.account.access_tool.accounts_following_you.LoadALL(
                                User.user_profile.followers - preupdate_profile.followers);          // Наша подписота
                        }

                        if (preupdate_profile.following != User.user_profile.following)
                        {
                            Console.WriteLine("Change count following {0} -> {1}", preupdate_profile.following, User.user_profile.following);
                            Task_following = InstagramApi.account.access_tool.accounts_you_follow.LoadALL(
                                User.user_profile.following - preupdate_profile.following);             // Мы подписаны 
                        }
                    }

                    Console.Write("Task_followers...");
                    using (var progress = new ProgressBar())
                    {
                        while (!Task_followers.IsCompleted)
                        {
                            progress.Report(
                                (double)InstagramApi.account.access_tool.accounts_following_you.current_pos /
                                InstagramApi.account.access_tool.accounts_following_you.current_max);
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
                                (double)InstagramApi.account.access_tool.accounts_you_follow.current_pos /
                                InstagramApi.account.access_tool.accounts_you_follow.current_max);
                            Thread.Sleep(33);
                        }
                    }
                    Console.WriteLine("Done.");

                    preupdate_profile.followers = User.user_profile.followers;
                    preupdate_profile.following = User.user_profile.following;

                    //Console.WriteLine("current_action_follow_list: ");
                    //foreach (var i in InstagramApi.account.access_tool.current_action_follow_list)
                    //    Console.WriteLine("-> {0} in {1}", i, Dev.TimestampToDateTime(i.timestamp));
                    //InstagramApi.account.access_tool.current_action_follow_list.Clear();

                    Log.isWork = true;

                    var api_followers = Task_followers.Result;
                    var api_following = Task_following.Result;

                    if (!api_followers.isSuccess) throw new Exception("Followers return error", api_followers.GetError());
                    if (!api_following.isSuccess) throw new Exception("Following return error", api_following.GetError());

                    User.followers = api_followers.GetResult();
                    User.following = api_following.GetResult();

                    Console.WriteLine("followers = {0}", User.followers.Count);
                    Console.WriteLine("following = {0}", User.following.Count);

                    var x = User.following.Except(User.followers).ToDictionary(t => t.Key, t => t.Value);
                    int next_step_sleep = 5;

                    int ind = 0;
                    int counter = 0;
                    int counter2 = 0;
                    int max_counter = x.Count;

                    Console.WriteLine("UpdateUnfollowList...");
                    int unflw_targ = 0;
                    int unflw_max = x.Count;
                    List<string> ignored = new List<string>();
                    using (var progress = new ProgressBar())
                    {
                        while (ind < x.Count)
                        {
                            try
                            {
                                var profile = await InstagramApi.GetProfile(x.ElementAt(ind).Value.username);
                                if (!profile.isSuccess) throw profile.GetError();
                                var id = profile.GetResult().id;
                                if (InstagramApi.ignore_list.Contains(id))
                                {
                                    ignored.Add(x.ElementAt(ind).Key);
                                    x.RemoveAt(ind);
                                }
                                else ind++;
                                counter++;
                                if (!profile.isCash())
                                {
                                    if (++counter2 >= 500)
                                    {
                                        counter2 = 0;
                                        var v = (int)Math.Round(next_step_sleep * 0.16);
                                        if (v != next_step_sleep)
                                        {
                                            next_step_sleep = v;
                                            Console.WriteLine("step_sleep = {0}", next_step_sleep);
                                        }
                                    }
                                    Thread.Sleep(next_step_sleep + 500);
                                }
                            }
                            catch (WebException ex)
                            {
                                HttpWebResponse response = ((HttpWebResponse)ex.Response);
                                var code = response.StatusCode;
                                Console.WriteLine("Server return error: {0} for username: {1}", code, x.ElementAt(ind));
                                if (Convert.ToInt32(code) == 429)
                                {
                                    string err = "Too many request per second!";
                                    Console.WriteLine("{0}\r\nwait 3 min...", err);
                                    Log.Write(new List<string>() { string.Format("WebException {0}", err) }, Log.Type.error);

                                    var Task_auto_save =
                                        Dev.WriteAsync(InstagramApi.profile_hist_name, JsonConvert.SerializeObject(InstagramApi.profile_hist));

                                    Thread.Sleep(new TimeSpan(0, 3, 0));

                                    while (!Task_auto_save.IsCompleted)
                                        Thread.Sleep(33);

                                    next_step_sleep = (next_step_sleep * 2) + 100;
                                    Console.WriteLine("step_sleep = {0}", next_step_sleep);
                                }
                                else if (code == HttpStatusCode.NotFound)
                                {
                                    x.RemoveAt(ind);
                                }
                                else throw new Exception(string.Format("Unknown code WebException: [{0}]", Convert.ToInt32(code)), ex);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("\r\n >>> {0} of {1} for username: {2}<<< \r\n", counter, max_counter, x.ElementAt(ind));
                                throw ex;
                            }
                            progress.Report((double)++unflw_targ / unflw_max);
                        }
                    }
                    Console.WriteLine("Done.");
                    foreach (var i in ignored)
                        Console.WriteLine("Ignored: {0}", i);

                    var z = User.following.Except(x).ToList();       // work list

                    while (x.Count > 0)
                    {
                        try
                        {
                            var i = x.First();
                            Console.Write("Unfollow from {0}...", i);
                            model.profile profile = (await InstagramApi.GetProfile(i.Key)).GetResult();
                            if (await profile.Unfollow())
                            {
                                User.user_profile.following--;
                                User.following.Remove(i.Key);
                                InstagramApi.account.access_tool.current_action_follow_list.Add(
                                    new InstagramApi.account.access_tool.act(profile.username, InstagramApi.account.access_tool.act.act_type.following_unfollow));
                                Console.WriteLine("Ok");
                            }
                            else Console.WriteLine("Err");

                            var sel = InstagramApi.work_list.Where(e => e.profile.Equals(profile)).ToList();
                            InstagramApi.work_list = InstagramApi.work_list.Except(sel).ToList();

                            x.RemoveAt(0);
                            Thread.Sleep(sleep_next_unfollow);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error in final task: {0}\r\nWait {1}", ex.Message, sleep_try_next.ToString());
                            Thread.Sleep(sleep_try_next);
                        }
                    }

                    await Dev.WriteAsync("followers.json", JsonConvert.SerializeObject(User.followers));
                    await Dev.WriteAsync("following.json", JsonConvert.SerializeObject(User.following));

                    Console.Write("Save...");
                    var Task_save = Task.Run(async () =>
                    {
                        await Dev.WriteAsync(InstagramApi.config.name, JsonConvert.SerializeObject(InstagramApi.config.value));
                        await Dev.WriteAsync(InstagramApi.profile_hist_name, JsonConvert.SerializeObject(InstagramApi.profile_hist));
                        await Dev.WriteAsync(InstagramApi.work_list_name, JsonConvert.SerializeObject(InstagramApi.work_list));
                    });
                    while (!Task_save.IsCompleted)
                        Thread.Sleep(33);
                    Console.WriteLine("Done.");
                    await User.user_profile.Reload();
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
                }
                Console.WriteLine("End task");
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

            thread.Abort();
        }));
        static void Main(string[] args)
        {
            Console.BackgroundColor = ConsoleColor.DarkBlue;

            Console.WriteLine(Dev.GetTranslitText("Привет ебнврт, как дела?))"));

            var server = Task.Run(() => { return new HTTPServer.Server(); });
            thread.Start();
            while (thread.ThreadState != ThreadState.Aborted)
                Thread.Sleep(33);

            Console.Write("Save...");
            var Task_save = Task.Run(async () =>
            {
                await Dev.WriteAsync(InstagramApi.config.name, JsonConvert.SerializeObject(InstagramApi.config.value));
                await Dev.WriteAsync(InstagramApi.profile_hist_name, JsonConvert.SerializeObject(InstagramApi.profile_hist));
                await Dev.WriteAsync(InstagramApi.work_list_name, JsonConvert.SerializeObject(InstagramApi.work_list));
            });
            while (!Task_save.IsCompleted)
                Thread.Sleep(33);
            Console.WriteLine("Done.");
            Console.WriteLine("Press key to exit");
            Console.ReadKey();
        }
    }
}
