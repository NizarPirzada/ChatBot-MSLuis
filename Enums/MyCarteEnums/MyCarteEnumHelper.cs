using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace AriBotV4.Enums.MyCarteEnums
{
    public class MyCarteEnumHelper
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


    public enum LookingFor
    {
        [Description("I am looking for men's shoes")]
        MenShoes,
        [Description("What are the best restaurants near me?")]
        Restaurants,
        [Description("When is my next order pick up date?")]
        NextOrderPickUpDates,
        [Description("Any good deals for me today?")]
        GoodDeals,
        [Description("View my orders list")]
        MyOrderList

    }

    public enum MyCarteDeals
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
}
