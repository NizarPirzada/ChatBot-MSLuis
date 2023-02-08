using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AriBotV4.Models.Travel
{
    public class TravelRequest
    {
        #region Properties

        public string Specifics { get; set; }
        public string FromLocation { get; set; } //get locality of user?
        public string ToLocation { get; set; }

        public string DepartureDate { get; set; }
        public string ReturnDate { get; set; }
        public int? PeopleCount { get; set; }
        public decimal? TicketBudget { get; set; }
        public string AirlineChoice { get; set; }
        public List<string> DealsOffered { get; set; }

        #endregion

    }
}
