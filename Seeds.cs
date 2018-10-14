using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace main
{
    public class _responseAuth
    {
        public bool? authenticated;
        public bool? user;
        [JsonProperty("userId")]
        public long user_id;
        public bool oneTapPrompt;
        public string status;
    }

    public class profile
    {
        public bool is_business_account;
        public bool is_private;
        public bool is_verified;

        public string full_name;
        public string username;

        public long id;
        public long followers;
        public long following;

        public async Task<bool> Unfollow() => (await InstagramApi.Unfollow(id)).isSuccess;
    }

    public class profile_hist
    {
        public profile profile;
        public long timestamp;
        public void Update(profile profile)
        {
            this.profile = profile;
            timestamp = Dev.GetUnixTimestamp();
        }
        public profile_hist(profile profile) => Update(profile);
    }
}
