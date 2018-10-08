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
    public static class InstagramApi
    {
        public static string DecodeSharedData(string input) => input = (input = input.Substring(input.IndexOf("window._sharedData = ") + 21)).Substring(0, input.IndexOf(";</script>"));
        public static async Task<bool> Auth(string login, string password)
        {
            var response = await Web.Navigate.Get((HttpWebRequest)WebRequest.Create("https://www.instagram.com/accounts/login/"));
            JObject o = JsonConvert.DeserializeObject<JObject>(DecodeSharedData(response));

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://www.instagram.com/accounts/login/ajax/");
            request.Headers.Add("x-csrftoken", ((JObject)o.GetValue("config")).GetValue("csrf_token").ToString());

            var auth = JsonConvert.DeserializeObject<_responseAuth>(
                await Web.Navigate.Post(request, string.Format("username={0}&password={1}", login, password) + "&queryParams={}"));
            if (auth.authenticated == true)
                if (auth.user == true)
                    if (auth.status != "ok") throw new Exception("error status");
                    else return true;
                else throw new Exception("error user");
            else throw new Exception("error authenticated");
        }
    }
}
