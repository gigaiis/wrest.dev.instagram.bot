using main;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace model
{
    public class Challenge
    {
        public class Field
        {
            [JsonProperty("label")]
            public string text;
            public int value;
        }
        public class Result
        {
            public string location;
            public string type;
            public string status;
        }
    }
    public class Auth
    {
        public string message;
        public string checkpoint_url;
        [JsonProperty("lock")]
        public bool? IsLock;
        public bool? authenticated;
        public bool? user;
        [JsonProperty("userId")]
        public long user_id;
        public bool oneTapPrompt;
        public string status;
        public override string ToString() => string.Format("[message.{0}+checkpoint_url.{1}+lock.{2}+authenticated.{3}+user.{4}+userId.{5}+oneTapPrompt.{6}+status.{7}",
            message, checkpoint_url, IsLock, authenticated, user, user_id, oneTapPrompt, status);
    }
    public class Post
    {
        public string id;
        public string __typename;
        public string shortcode;
        public bool is_video;
        public long comment_count;
        public bool comments_disabled;
        public long like_count;
        public async Task<bool> Like() => (await InstagramApi.Web.Likes.Like(id)).IsSuccess;
        public override string ToString() => string.Format("[id.{0}+__typename.{1}]", id, __typename.ToString());
    }
    public class Posts : List<Post>
    {
        /// <summary>Всего постов</summary>
        public long count;
        public bool has_next_page;
        public string end_cursor;
        public Posts() => Clear();
    }
    public class Ignore_list_object
    {
        public long id;
        public double expired_in;
        public Ignore_list_object(long id, double expired_in = 0)
        {
            this.id = id;
            this.expired_in = expired_in;
        }
        public bool IsExpired() => (expired_in != 0) && (expired_in < Dev.GetUnixTimestamp());
        public override bool Equals(object obj) => (obj is Ignore_list_object) ? id.Equals((obj as Ignore_list_object).id) : base.Equals(obj);
        public override int GetHashCode() => id != 0 ? (int)id : base.GetHashCode();
    }
    public class Profile
    {
        public class Edge_mutual_followed_by
        {
            public long count;
            public List<string> items;
        }
        public string biography;
        public bool blocked_by_viewer;
        public bool country_block;
        public string external_url;
        public string external_url_linkshimmed;
        public bool followed_by_viewer;
        public bool follows_viewer;
        public bool has_channel;
        public bool has_blocked_viewer;
        public long highlight_reel_count;
        public bool has_requested_viewer;
        public bool is_business_account;
        public bool is_joined_recently;
        public string business_category_name;
        public string business_email;
        public string business_phone_number;
        public string business_address_json;
        public bool is_private;
        public bool is_verified;
        public Edge_mutual_followed_by mutual_followed_by;
        public string profile_pic_url;
        public string profile_pic_url_hd;
        public bool requested_by_viewer;
        public string full_name;
        public string username;
        public long id;
        public long followers;
        public long following;
        public object connected_fb_page;
        public object edge_felix_video_timeline;
        public object edge_saved_media;
        public object edge_media_collections;
        public Posts posts;                     // edge_owner_to_timeline_media
        public async Task<bool> LoadMorePosts(int count = 12)
        {
            if (this.posts.has_next_page)
            {
                var api_posts = await InstagramApi.Web.Posts.Load(Convert.ToString(id), this.posts.end_cursor, count);
                if (!api_posts.IsSuccess)
                    return false;
                var posts = api_posts.GetResult();
                this.posts.has_next_page = posts.has_next_page;
                this.posts.end_cursor = posts.end_cursor;
                this.posts.AddRange(posts);
            }
            return true;
        }
        public async Task<bool> Unfollow()
        {
            var res = (id != User.profile.id) && (await InstagramApi.Web.Friendships.Unfollow(id)).IsSuccess;
            if (res = res && User.following.Remove(username))
            {
                if (InstagramApi.profile_hist.TryGetValue(id, out Profile_hist profile_hist))
                    profile_hist.SetUnfollowed();
                User.profile.following--;
                Program.preupdate_profile.following--;
                InstagramApi.Account.Access_tool.current_action_follow_list.Add(
                    new InstagramApi.Account.Access_tool.Act(username, InstagramApi.Account.Access_tool.Act.Type.following_unfollow));
            }
            return res;
        }
        public async Task<bool> Follow()
        {
            var res = (id != User.profile.id) && (await InstagramApi.Web.Friendships.Follow(id)).IsSuccess;
            if (res = res && User.following.Insert(0, username) == 1)
            {
                User.profile.following++;
                Program.preupdate_profile.following++;
                InstagramApi.Account.Access_tool.current_action_follow_list.Add(
                    new InstagramApi.Account.Access_tool.Act(username, InstagramApi.Account.Access_tool.Act.Type.following_follow));
            }
            return res;
        }
        public async Task<bool> Reload()
        {
            var result = await InstagramApi.GetProfile(username, true);
            if (!result.IsSuccess) return false;
            var profile = result.GetResult();
            is_private = profile.is_private;
            is_verified = profile.is_verified;
            full_name = profile.full_name;
            id = profile.id;
            followers = profile.followers;
            following = profile.following;
            posts = profile.posts;
            return true;
        }
        public override bool Equals(object obj)
        {
            if (!(obj is Profile)) return base.Equals(obj);
            var prof = obj as Profile;
            return (id != 0 && prof.id != 0) ? prof.id.Equals(id) : prof.username.Equals(username);
        }
        public override int GetHashCode() => (id != 0) ? (int)id : username.GetHashCode();
        public override string ToString() => string.Format("[user_id.{0}+username.{1}+full_name.{2}]", id, username, full_name);
    }
    public class Profile_hist
    {
        public Profile profile;
        public double timestamp;
        public double unfollowed_at;
        public void SetUnfollowed() => unfollowed_at = Dev.GetUnixTimestamp();
        public void Update(Profile profile)
        {
            this.profile = profile;
            timestamp = Dev.GetUnixTimestamp();
        }
        public Profile_hist(Profile profile) => Update(profile);
        public override bool Equals(object obj)
        {
            if (obj is Profile_hist) return profile.Equals((obj as Profile_hist).profile);
            else return base.Equals(obj);
        }
        public override int GetHashCode() => base.GetHashCode();
    }
    public class Work_list
    {
        public Profile_hist profile;
        public double priority;
        public Work_list(Profile_hist _profile_hist)
        {
            profile = _profile_hist;
            priority = 0;
        }
    }
    public class Activity_list
    {
        public double timestamp;
        public enum Type { Undefined = 0, Follow = 1, Unfollow = 2, Like = 3, Unlike = 4 };
        public Type type;
        public Activity_list(Type type)
        {
            timestamp = Dev.GetUnixTimestamp();
            this.type = type;
        }
    }
    public class Followers
    {
        public long count = 0;
        public bool has_next_page = false;
        public string end_cursor = null;
        public List<Profile> list = new List<Profile>();
    }
    public class Suggestchains
    {
        public class Suggestchain
        {
            public string id;
            /// <summary>Заблокирован вами</summary>
            public bool blocked_by_viewer;
            /// <summary>Вы подписаны</summary>
            public bool followed_by_viewer;
            /// <summary>На вас подписаны</summary>
            public bool follows_viewer;
            public string full_name;
            /// <summary>Вас заблокировали</summary>
            public bool has_blocked_viewer;
            /// <summary>Запросил подписку на вас</summary>
            public bool has_requested_viewer;
            public bool is_private;
            public bool is_verified;
            public string profile_pic_url;
            /// <summary>Вы запросили подписку</summary>
            public bool requested_by_viewer;
            public string username;
            public override string ToString() => string.Format("[id.{0}+username.{1}]", id.ToString(), username.ToString());
        }
        public List<Suggestchain> list = new List<Suggestchain>();
    }
    public class Suggests
    {
        public class Suggest
        {
            string _description;
            public enum Type
            {
                unknown = 0,
                followed_by = 1,
                suggest_for_you = 2,
                new_to_instagram = 3,
                in_your_contacts = 4,
                follows_you = 5,
                popular = 6
            };
            public Profile user;
            public string Description
            {
                set
                {
                    if ((_description = value) != null)
                        if (value.Contains("Followed by")) type = Type.followed_by;
                        else if (value.Contains("Suggested for you")) type = Type.suggest_for_you;
                        else if (value.Contains("New to Instagram")) type = Type.new_to_instagram;
                        else if (value.Contains("In your contacts")) type = Type.in_your_contacts;
                        else if (value.Contains("Follows you")) type = Type.follows_you;
                        else if (value.Contains("Popular")) type = Type.popular;
                        else type = Type.unknown;
                }
                get => _description;
            }
            public Type type;

            public override string ToString() => string.Format("[user.{0}+type.{1}]", user.ToString(), type.ToString());
        }
        public bool has_next_page = false;
        public List<Suggest> list = new List<Suggest>();
    }
    public class Feeds
    {
        public class Feed
        {
            public struct Dimension
            {
                public int height;
                public int width;
            }
            public struct Display_resource
            {
                public string src;
                public int config_width;
                public int config_height;
            }
            public struct Edge_media_preview_like
            {
                public int count;
                public object edges;
                public override string ToString() => Convert.ToString(count);
            }
            public class Location
            {
                public string id;
                public bool has_public_page;
                public string name;
                public string slug;
            }
            public struct Owner
            {
                public string id;
                public string profile_pic_url;
                public string username;
                public bool followed_by_viewer;
                public string full_name;
                public bool is_private;
                public bool requested_by_viewer;
                public bool blocked_by_viewer;
                public bool has_blocked_viewer;
            }

            public string id;
            public Dimension Dimensions;
            public string display_url;
            public List<Display_resource> Display_resources;
            public object follow_hashtag_info;
            public bool is_video;
            public bool should_log_client_event;
            public string tracking_token;
            public object edge_media_to_tagged_user;
            public string accessibility_caption;
            public object attribution;
            public string shortcode;
            public object edge_media_to_caption;
            public object edge_media_to_comment;
            public object gating_info;
            public string media_preview;
            public bool comments_disabled;
            public long taken_at_timestamp;
            [JsonProperty("edge_media_preview_like")]
            public Edge_media_preview_like Likes;
            public object edge_media_to_sponsor_user;
            public Location location;
            public bool viewer_has_liked;
            public bool viewer_has_saved;
            public bool viewer_has_saved_to_collection;
            public bool viewer_in_photo_of_you;
            public bool viewer_can_reshare;
            [JsonProperty("owner")]
            public Owner owner;
            public async Task<bool> Like() => viewer_has_liked || (await InstagramApi.Web.Likes.Like(id)).IsSuccess;
            public async Task<bool> Unlike() => !viewer_has_liked || (await InstagramApi.Web.Likes.Unlike(id)).IsSuccess;
            public override string ToString() => string.Format("[id.{0}]", id);
        }
        public bool has_next_page = false;
        public string end_cursor;
        public List<Feed> list = new List<Feed>();
    }
    public class Activity
    {
        public enum Type { Undefined = 0, GraphLikeAggregatedStory = 1, GraphFollowAggregatedStory = 3, GraphGdprConsentStory = 173 };
        public string id;
        public Type type;
        public double timestamp;
        public Profile user;
    }
    public class Config
    {
        public double Activity_timestamp
        {
            get => InstagramApi.Account.Activity.timestamp;
            set => InstagramApi.Account.Activity.timestamp = value;
        }
        public Profile Current_profile
        {
            get => User.profile;
            set => User.profile = value;
        }
        public List<InstagramApi.Account.Access_tool.Act> Current_action_follow_list
        {
            get => InstagramApi.Account.Access_tool.current_action_follow_list;
            set => InstagramApi.Account.Access_tool.current_action_follow_list = value;
        }
        public List<Web.Act> Current_action_web_list
        {
            get => Web.current_action_web_list;
            set => Web.current_action_web_list = value;
        }
        public long Last_feed_id
        {
            get => InstagramApi.Feed.last_feed_id;
            set => InstagramApi.Feed.last_feed_id = value;
        }
    }
}
