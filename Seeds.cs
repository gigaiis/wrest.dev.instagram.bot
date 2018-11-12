using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace model
{
    public class auth
    {
        public bool? authenticated;
        public bool? user;
        [JsonProperty("userId")]
        public long user_id;
        public bool oneTapPrompt;
        public string status;
    }

    public class post
    {
        public string id;
        public string __typename;
        public bool is_video;

        public long comment_count;
        public bool comments_disabled;

        public long like_count;
    }
    public class posts : List<post>
    {
        public long count;
        public bool has_next_page;
        public string end_cursor;
        public posts() => Clear();
    }
    public class profile
    {
        public bool is_private;
        public bool is_verified;

        public string full_name;
        public string username;

        public long id;
        public long followers;
        public long following;

        public posts posts;

        public async Task<bool> Unfollow() => ((id != main.User.user_id) && ((await main.InstagramApi.web.friendships.Unfollow(id)).isSuccess));

        public override bool Equals(object obj)
        {
            if (obj is profile)
            {
                var p = obj as profile;
                return (id.Equals(p.id) && username.Equals(p.username));
            }
            else return base.Equals(obj);
        }
        public override int GetHashCode() => base.GetHashCode();
        public override string ToString() => string.Format("[user_id.{0}+username.{1}+full_name.{2}]", id, username, full_name);

        public async Task<bool> Reload()
        {
            var result = await main.InstagramApi.GetProfile(username, true);
            if (!result.isSuccess) return false;
            var prof = result.GetResult();
            is_private = prof.is_private;
            is_verified = prof.is_verified;
            full_name = prof.full_name;
            id = prof.id;
            followers = prof.followers;
            following = prof.following;
            posts = prof.posts;
            return true;
        }
    }

    public class profile_hist
    {
        public profile profile;
        public long timestamp;
        public void Update(profile profile)
        {
            this.profile = profile;
            timestamp = main.Dev.GetUnixTimestamp();
        }
        public profile_hist(profile profile) => Update(profile);


        public override bool Equals(object obj)
        {
            if (obj is profile_hist) return profile.Equals((obj as profile_hist).profile);
            else return base.Equals(obj);
        }
        public override int GetHashCode() => base.GetHashCode();
    }

    public class work_list
    {
        public profile_hist profile;
        public double priority;
        public work_list(profile_hist _profile_hist)
        {
            profile = _profile_hist;
            priority = 0;
        }
    }

    public class activity_list
    {
        public long timestamp;
        public enum type { Undefined = 0, Follow = 1, Unfollow = 2, Like = 3, Unlike = 4 };
        [JsonProperty("type")]
        public type _type;
        public activity_list(type type)
        {
            timestamp = main.Dev.GetUnixTimestamp();
            _type = type;
        }
    }
    public class followers
    {
        public long count = 0;
        public bool has_next_page = false;
        public string end_cursor = null;
        public List<profile> list = new List<profile>();
    }
    public class suggests
    {
        public class suggest
        {
            public enum Type
            {
                unknown = 0,
                followed_by = 1,
                suggest_for_you = 2,
                new_to_instagram = 3,
                in_your_contacts = 4
            };

            public profile user;
            public string description
            {
                set
                {
                    if (value.Contains("Followed by")) type = Type.followed_by;
                    else if (value.Contains("Suggested for you")) type = Type.suggest_for_you;
                    else if (value.Contains("New to Instagram")) type = Type.new_to_instagram;
                    else if (value.Contains("In your contacts")) type = Type.in_your_contacts;
                    else type = Type.unknown;
                }
            }
            public Type type;

            public override string ToString() => string.Format("[user.{0}+type.{1}]", user.ToString(), type.ToString());
        }
        public bool has_next_page = false;
        public List<suggest> list = new List<suggest>();
    }
    public class activity
    {
        public enum type { Undefined = 0, GraphLikeAggregatedStory = 1, GraphFollowAggregatedStory = 3, GraphGdprConsentStory = 173 };
        public string id;
        [JsonProperty("type")]
        public type _type;
        public double timestamp;
        public profile user;
    }

    public class TEST
    {
        public string Name;
        public string Sex;
    }

    public class config
    {
        public double activity_timestamp
        {
            get => main.InstagramApi.account.activity.timestamp;
            set => main.InstagramApi.account.activity.timestamp = value;
        }
        public profile current_profile
        {
            get => main.User.user_profile;
            set => main.User.user_profile = value;
        }
        public List<main.InstagramApi.account.access_tool.act> current_action_follow_list
        {
            get => main.InstagramApi.account.access_tool.current_action_follow_list;
            set => main.InstagramApi.account.access_tool.current_action_follow_list = value;
        }
        public List<main.Web.act> current_action_web_list
        {
            get => main.Web.current_action_web_list;
            set => main.Web.current_action_web_list = value;
        }
    }
}
