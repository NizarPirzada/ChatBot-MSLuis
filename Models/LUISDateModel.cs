using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AriBotV4.Models
{
    public class LuisDateModel
    {
        public string query { get; set; }
        public object alteredQuery { get; set; }
        public TopScoringIntent topScoringIntent { get; set; }
        public List<Intent> intents { get; set; }
        public List<Entity> entities { get; set; }
        public object compositeEntities { get; set; }
        public object sentimentAnalysis { get; set; }
        public object connectedServiceResult { get; set; }
    }
    public class TopScoringIntent
    {
        public string intent { get; set; }
        public double score { get; set; }
    }

    public class Intent
    {
        public string intent { get; set; }
        public double score { get; set; }
    }

    public class Value
    {
        public string timex { get; set; }
        public string Mod { get; set; }
        public string type { get; set; }
        public string value { get; set; }
        public string start { get; set; }
        public string end { get; set; }
        
    }

    public class Resolution
    {
        //public List<Value> values { get; set; }

        public List<object> values { get; set; }
        public string subtype { get; set; }

        public string value { get; set; }

    }

    public class Entity
    {
        public string entity { get; set; }
        public string type { get; set; }
        public int startIndex { get; set; }
        public int endIndex { get; set; }
        public Resolution resolution { get; set; }
    }


}
