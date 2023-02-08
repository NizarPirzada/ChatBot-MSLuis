using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AriBotV4.Models.Food
{
    public class FoodRequest
    {
        #region Properties
        public string Cuisine { get; set; }
        public double PaxCount { get; set; }
        public string PriceString { get; set; }
        public decimal Price { get; set; }
        public string Location { get; set; }

        #endregion

    }
}
