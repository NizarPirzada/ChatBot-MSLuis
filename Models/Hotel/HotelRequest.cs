using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AriBotV4.Models.Hotel
{
    public class HotelRequest
    {
        #region Properties
        public string HotelName { get; set; }
        public string Location { get; set; }
        public string PriceString { get; set; }
        public decimal Price { get; set; }
        public double PaxCount { get; set; }
        public string CheckInDate { get; set; }
        public string CheckOutDate { get; set; }

        #endregion
    }
}
