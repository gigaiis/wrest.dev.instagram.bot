using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.IO;

namespace main
{
    public class Program
    {
        public static long user_id;
        public static Thread thread = new Thread(new ThreadStart(async () =>
        {
            new Task(async () =>
            {
                var r = JsonConvert.DeserializeObject<Dictionary<long, profile_hist>>(await Dev.ReadAsync(InstagramApi.profile_hist_name));
                if (r != null) InstagramApi.profile_hist = r;
            }).Start();

            new Task(async () =>
            {
                var r = JsonConvert.DeserializeObject<List<long>>(await Dev.ReadAsync(InstagramApi.ignore_list_name));
                if (r != null) InstagramApi.ignore_list = r;
            }).Start();

            ApiResult api_res = null;
            try
            {
                List<string> listUnfollow = new List<string>();

                Console.WriteLine("Auth...");
                await InstagramApi.Auth("login", "password");

                //var id = await InstagramApi.GetUserId("scarlxrd");
                //var d = await InstagramApi.GetProfilePage("scarlxrd");



                Console.SetCursorPosition(0, 0);
                Console.WriteLine("Load follow request...");

                var follow_request = await InstagramApi.access_tool.current_follow_requests.LoadALL();   // Запросы на подписоту (приват)
                var Task_followers = InstagramApi.access_tool.accounts_following_you.LoadALL();          // Наша подписота
                var Task_following = InstagramApi.access_tool.accounts_you_follow.LoadALL();             // Мы подписаны 

                // TASK UNFOLLOW FROM REQUEST FOLLOW LIST

                var sleep_next_unfollow = new TimeSpan(0, 0, 15);
                var sleep_try_next = new TimeSpan(0, 15, 0);

                if (follow_request.isSuccess)
                {
                    while (listUnfollow.Count > 0)
                    {
                        try
                        {
                            if (!(api_res = await InstagramApi.GetUserId(listUnfollow.First())).isSuccess) throw api_res.GetError();
                            if (!(api_res = await InstagramApi.Unfollow(user_id)).isSuccess) throw api_res.GetError();
                            Console.WriteLine("Success unfollow, user_id: {0}", user_id);
                            listUnfollow.RemoveAt(0);
                            Thread.Sleep(sleep_next_unfollow);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error: {0}\r\nWait {1}", ex.Message, sleep_try_next.ToString());
                            Thread.Sleep(sleep_try_next);
                        }
                    }
                }
                else throw follow_request.GetError();

                while (!Task_followers.IsCompleted || !Task_following.IsCompleted)
                {
                    //Console.Clear();
                    Console.SetCursorPosition(0, 0);
                    Console.WriteLine("Task_followers.IsCompleted({0}), Task_following.IsCompleted({1})", Task_followers.IsCompleted.ToString(), Task_following.IsCompleted.ToString());
                    Thread.Sleep(33);
                }

                var api_followers = Task_followers.Result;
                var api_following = Task_following.Result;

                if (!api_followers.isSuccess) throw new Exception("Followers return error", api_followers.GetError());
                if (!api_following.isSuccess) throw new Exception("Following return error", api_following.GetError());

                var followers = api_followers.GetResult();
                var following = api_following.GetResult();

                var x = following.Except(followers).ToList();

                var l = "";

                Console.WriteLine("Enter ignore or empty for next step");
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

                Dev.WriteAsync(InstagramApi.ignore_list_name, JsonConvert.SerializeObject(InstagramApi.ignore_list));

                Console.WriteLine("RESULT TASK: ");

                int ind = 0;
                while (ind < x.Count)
                {
                    var id = (await InstagramApi.GetProfilePage(x.ElementAt(ind))).GetResult().id;
                    if (InstagramApi.ignore_list.Contains(id))
                    {
                        Console.WriteLine("Ignored: {0}", x.ElementAt(ind));
                        x.RemoveAt(ind);
                    }
                    else ind++;
                }

                while (x.Count > 0)
                {
                    try
                    {
                        var i = x.First();
                        Console.Write("Unfollow from {0}...", i);
                        await (await InstagramApi.GetProfilePage(i)).GetResult().Unfollow();
                        Console.Write("Ok\r\n");
                        x.RemoveAt(0);
                        Thread.Sleep(sleep_next_unfollow);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error in final task: {0}\r\nWait {1}", ex.Message, sleep_try_next.ToString());
                        Thread.Sleep(sleep_try_next);
                    }
                }

                Console.WriteLine("End task");
            }
            catch (Exception ex) { Console.WriteLine("Exception in thread: {0}", ex.Message); }

            thread.Abort();
        }));
        static void Main(string[] args)
        {
            thread.Start();
            while (thread.ThreadState != ThreadState.Aborted)
                Thread.Sleep(33);

            Console.WriteLine("Save...");
            Dev.WriteAsync(InstagramApi.profile_hist_name, JsonConvert.SerializeObject(InstagramApi.profile_hist));
            Console.WriteLine("Press key to exit");
            Console.ReadKey();
        }
    }
}
