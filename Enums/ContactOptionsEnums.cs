using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace AriBotV4.Enums
{
    public static class EnumHelpers
    {
        public static string GetEnumDescription(Enum value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());

            DescriptionAttribute[] attributes = fi.GetCustomAttributes(typeof(DescriptionAttribute), false) as DescriptionAttribute[];

            if (attributes != null && attributes.Any())
            {
                return attributes.First().Description;
            }

            return value.ToString();
        }
        public static T GetValueFromDescription<T>(string description)
        {
            var type = typeof(T);
            if (!type.IsEnum) throw new InvalidOperationException();
            foreach (var field in type.GetFields())
            {
                var attribute = Attribute.GetCustomAttribute(field,
                    typeof(DescriptionAttribute)) as DescriptionAttribute;
                if (attribute != null)
                {
                    if (attribute.Description == description)
                        return (T)field.GetValue(null);
                }
                else
                {
                    if (field.Name == description)
                        return (T)field.GetValue(null);
                }
            }
            throw new ArgumentException("Not found.", nameof(description));
            // or return default(T);
        }

    }


    // Main options
    public enum MainOptions
    {
        [Description("Ask Ari")]
        AskAri,
        [Description("Find Deals (Demo)")]
        FindDeals,
        [Description("TaskSpur")]
        TaskSpur

    }
    // Deal options
    public enum Deal
    {
        [Description("Travel")]
        Travel = 1,
        [Description("Food")]
        Food = 2,
        [Description("Hotel")]
        Hotel = 3,
        [Description("Others")]
        Others = 4
    }

    //Ask Ari options
    public enum AskAri
    {
        [Description("General")]
        General,
        [Description("News")]
        News,
        [Description("Images")]
        Images,
        [Description("Videos")]
        Videos
    }
    // Taskspur options

    public enum TaskSpur
    {
        [Description("Personal Life")]
        Life = 1,
        [Description("Self-care and Wellness")]
        Health = 2,
        [Description("Work and Career")]
        Work = 3,
        [Description("Funds")]
        Finance = 4
        
    }

    public enum SearchType
    {
        [Description("QnA")]
        QnA,
        [Description("LUIS")]
        LUIS,
        [Description("General")]
        General

    }
        

    // Task type
    public enum TaskType
    {
        [Description("Unscheduled")]
        Unscheduled = 1,
        [Description("Appointment")]
        Appointment = 2,
        [Description("Start date with no time")]
        StartDateNoTime = 3,
        [Description("Start date with time")]
        StartDateWithTime = 4,
        [Description("Start and End date with no time")]
        StartEndNoTime = 5,
        [Description("Start and End date with time")]
        StartEndWithTime = 6
    }

    // TaskSpur Priority
    public enum Priority
    {
        [Description("Low")]
        Low = 1,
        [Description("Medium")]
        Medium = 2,
        [Description("High")]
        High = 3,
    }

    // Weather Clouds
    public enum WeatherClouds
    {
        [Description("overcast clouds")]
        OvercastClouds,
        [Description("clouds")]
        clouds,
        [Description("sky")]
        sky,
        [Description("rain")]
        rain,
        [Description("storm")]
        storm,
        [Description("thunder")]
        thunder,
    }

    public enum Cuisine
    {
        [Description("Thai")]
        Thai = 1,
        [Description("Indian")]
        Indian = 2,
        [Description("Chinese")]
        Chinese = 3,
        [Description("European")]
        European = 4,
        [Description("Korean")]
        Korean = 5,
        [Description("Japanese")]
        Japanese = 6
    }
    public enum StatusEnum
    {
        [Description("unscheduled")]
        unscheduled = 0,
        [Description("later")]
        later = 1,
        [Description("todo")]
        todo = 2,
        [Description("to do")]
        todo2 = 2,
        [Description("todos")]
        todos = 2,
        [Description("to dos")]
        todos2 = 2,
        [Description("doing")]
        doing = 3,
        [Description("done")]
        done = 4,
        [Description("none")]
        None = 5
    }
}
