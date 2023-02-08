using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AriBotV4.Models.Common
{
    public class WebSearchCountResponse
    {
        public Data data { get; set; }
        public Toast toast { get; set; }
        
    }
    public class Data
    {
        public object id { get; set; }
        public string month { get; set; }
        public string year { get; set; }
        public int count { get; set; }
    }

    public class Toast
    {
        public int color { get; set; }
        public string message { get; set; }
    }
}
