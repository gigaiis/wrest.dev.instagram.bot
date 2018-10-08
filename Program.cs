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


namespace main
{
    public class Program
    {
        public static long user_id;
        public static Thread thread = new Thread(new ThreadStart(async () =>
        {
            try
            {
                List<string> listUnfollow = new List<string>();

                await InstagramApi.Auth("login", "password");
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(
                    "https://www.instagram.com/accounts/access_tool/current_follow_requests");
                JObject data = (JObject)(
                            (JObject)(
                                (JArray)(
                                    (JObject)
                                        JsonConvert.DeserializeObject<JObject>(
                                            InstagramApi.DecodeSharedData(await Web.Navigate.Get(request)))
                                    .GetValue("entry_data"))
                                .GetValue("SettingsPages"))[0])
                            .GetValue("data");
                Object cursor = data.GetValue("cursor").ToObject<Object>();

                List<string> data2 = ((JArray)data.GetValue("data")).Select(e => ((JObject)e).GetValue("text").ToString()).ToList();
                listUnfollow.AddRange(data2);

                var page = 1;
                var count = data2.Count;
                Console.WriteLine("Page = {0}", page);

                foreach (var link in data2)
                    Console.WriteLine(">> {0}", link);
                while (cursor != null)
                {
                    data = (JObject)(JsonConvert.DeserializeObject<JObject>(
                        await Web.Navigate.Get((HttpWebRequest)WebRequest.Create(
                            string.Format(
                                "https://www.instagram.com/accounts/access_tool/current_follow_requests?__a=1&cursor={0}", cursor)))))
                        .GetValue("data");
                    cursor = data.GetValue("cursor").ToObject<Object>();

                    data2 = ((JArray)data.GetValue("data")).Select(e => ((JObject)e).GetValue("text").ToString()).ToList();
                    listUnfollow.AddRange(data2);

                    page++;
                    Console.WriteLine("Page = {0}", page);
                    count += data2.Count;
                    foreach (var link in data2)
                        Console.WriteLine(">> {0}", link);
                }
                Console.WriteLine("Full count = {0}", count);

                var sleep_next_unfollow = new TimeSpan(0, 0, 30);           // 0 hours, 0 minutes, 30 seconds
                var max_count_unfollow = 3;
                try
                {
                    while ((listUnfollow.Count > 0) && (max_count_unfollow > 0))
                    {
                        var itm = listUnfollow.First();
                        var o = JsonConvert.DeserializeObject<JObject>(
                            InstagramApi.DecodeSharedData(
                                await Web.Navigate.Get((HttpWebRequest)WebRequest.Create(
                                    string.Format("https://www.instagram.com/{0}/", itm)))));
                        var user_id = ((JObject)(
                                (JObject)(
                                    (JObject)(
                                        (JArray)(
                                            (JObject)o
                                            .GetValue("entry_data"))
                                        .GetValue("ProfilePage"))[0])
                                    .GetValue("graphql"))
                                 .GetValue("user"))
                            .GetValue("id").ToString();
                        HttpWebRequest new_request = (HttpWebRequest)WebRequest.Create(
                            string.Format("https://www.instagram.com/web/friendships/{0}/unfollow/", user_id));
                        new_request.Headers.Add("x-csrftoken", ((JObject)o.GetValue("config")).GetValue("csrf_token").ToString());
                        var status = JsonConvert.DeserializeObject<JObject>(await Web.Navigate.Post(new_request, ""))
                            .GetValue("status").ToObject<Object>();
                        if (status != null)
                            if (status.ToString() != "ok") throw new Exception(string.Format("Unknows status: {0} for user_id: {1} not found", status, user_id));
                            else Console.WriteLine("Success unfollow from {0}, user_id: {1}", itm, user_id);
                        else throw new Exception(string.Format("Status for user_id: {0} not found", user_id));
                        listUnfollow.RemoveAt(0);
                        max_count_unfollow--;
                        Thread.Sleep(sleep_next_unfollow);
                    }
                }
                catch (Exception ex) { Console.WriteLine("Error: {0}", ex.Message); }
            }
            catch (Exception ex) { Console.WriteLine("Exception in thread: {0}", ex.Message); }
        }));
        static void Main(string[] args)
        {
            thread.Start();
            Console.Read();
        }
    }
}
