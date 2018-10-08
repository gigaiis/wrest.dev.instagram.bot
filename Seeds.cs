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
}
