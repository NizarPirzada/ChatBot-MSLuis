using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace AriBotV4.Enums
{
     public enum PriorityEnum
        {
        [Description("low priority")]
        low = 1,
        [Description("lowest priority")]
        lowest = 1,
        [Description("medium priority")]
        medium = 2,
        [Description("mid priority")]
        mid = 2,
        [Description("high priority")]
        high = 3,
        [Description("highest priority")]
        highest = 3
        }
    
}
