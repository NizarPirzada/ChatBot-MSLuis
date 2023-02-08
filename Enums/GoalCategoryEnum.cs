using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace AriBotV4.Enums
{
    public enum GoalCategoryEnum
    {
        [Description("personal life")]
        PersonalLife = 1,
        [Description("personal")]
        Personal = 1,
        [Description("self care and wellness")]
        SelfCareAndWellness = 2,
        [Description("health")]
        Health = 2,
        [Description("fitness")]
        Fitness = 2,
        [Description("work and career")]
        WorkAndCareer = 3,
        [Description("work")]
        Work = 3,
        [Description("career")]
        Career = 3,
        [Description("professional")]
        Professional = 3,
        [Description("funds")]
        Funds = 4,
        [Description("finance")]
        Finance = 4,
        [Description("finances")]
        Finances = 4,
        [Description("other")]
        Other
    }
}
