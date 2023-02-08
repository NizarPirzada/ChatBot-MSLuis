using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AriBotV4.Enums;
using Microsoft.Bot.Builder.Dialogs;

namespace AriBotV4.Models
{
    // Defines a state property used to track information about the user.
    public class UserProfile
    {
        #region Properties
        public string Name { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Subject { get; set; }
        public string Details { get; set; }
        public DateTime CallbackTime { get; set; }
        public string PhoneNumber { get; set; }
        public string Bug { get; set; }

        public string CurrentAriOptions { get; set; }
        public string DealChosen { get; set; }
        public DateTime LastMessageReceived { get; set; }

        public Dictionary<string,string> CreateGoal { get; set; }
        public Dictionary<string, object> CreateTask { get; set; }

    #endregion
}
}

