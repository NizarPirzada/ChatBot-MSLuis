using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AriBotV4.Models.Activity
{
    public class AriActivities
    {
        public List<Activity> activities { get; set; }
        public string watermark { get; set; }
    }

    public class From
    {
        public string id { get; set; }
        public string name { get; set; }
        public string username { get; set; }
        public string token { get; set; }
        public string timeZone { get; set; }
        public string refreshToken { get; set; }
        public int project { get; set; }
        public string assistantName { get; set; }
        public string RefreshToken { get; set; }
        public string Token { get; set; }
        public string firstName { get; set; }
        public string location { get; set; }
    }

    public class Conversation
    {
        public string id { get; set; }
    }

    public class Action
    {
        public string type { get; set; }
        public string title { get; set; }
        public string value { get; set; }
    }

    public class SuggestedActions
    {
        public List<Action> actions { get; set; }
    }

    public class Activity
    {
        public string type { get; set; }
        public string id { get; set; }
        public DateTime timestamp { get; set; }
        public string serviceUrl { get; set; }
        public string channelId { get; set; }
        public From from { get; set; }
        public Conversation conversation { get; set; }
        public string text { get; set; }
        public string inputHint { get; set; }
        public List<object> attachments { get; set; }
        public List<object> entities { get; set; }
        public string replyToId { get; set; }
        public SuggestedActions suggestedActions { get; set; }
    }
}
