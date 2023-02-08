using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace AriBotV4.Common
{
    public static class Constants
    {
        #region Properties and Fields

        // Get bot id and bot password

        // Travel
        public const string TravelIntent = "FindDealsTravel_Intent";

        // Food
        public const string FoodIntent = "FindDealsFood_Intent";

        // Hotel
        public const string HotelIntent = "FindDealsHotel_Intent";


        // Time
        public const string TimeIntent = "Time_Intent";

        // News
        public const string NewsIntent = "News_Intent";

        // Images
        public const string ImagesIntent = "Images_Intent";
        
        // Get Tasks Intent
        public const string Get_Tasks_Intent = "Get_Tasks_Intent";
        public const string Create_Task_Intent = "Create_Task_Intent";
        public const string Delete_Task_Intent = "Delete_Task_Intent";
        public const string Edit_Task_Intent = "Edit_Task_Intent";

        // Get Goal Intent
        
        public const string Create_Goal_Intent = "Create_Goal_Intent";
        public const string Delete_Goal_Intent = "Delete_Goal_Intent";
        public const string Edit_Goal_Intent = "Edit_Goal_Intent";

        public const string NoTasksFound = "There is no task found.";


        // Luis time
        public const string Time = "time";

        // Luis datetime
        public const string DateTime = "datetime";

        // Luis date
        public const string Date = "date";

        // Luis daterange 
        public const string DateRange = "daterange";

        // Luis daterange 
        public const string DateTimeRange = "datetimerange";

        // Luis daterange 
        public const string TimeRange = "timerange";

        // Luis duration
        public const string Duration = "duration";

        // Luis Now
        public const string Now = "PRESENT_REF";
        

        // Weather
        public const string WeatherIntent = "Weather_Intent";

        // Weather
        

        //Date constants
        public const string DateFormat = "dddd, dd MMM yyyy";

        public const string TimeFormat = "{0:hh:mm:ss tt}";

        public const string TaskTimeFormat = "{0:HH:mm}";

        // Create task date format
        public const string CreateTaskDate = "MM/dd/yyyy";

        // Create task confirm date format
        public const string confirmDate = "M/d/yyyy";

        // Create task time validation format
        public const string TaskTimeValidation = "HH:mm:ss";

        
        // Bing news api 
        public static IList<string> BingNewsList = new List<string> { "news", "information", "story", "report", "broadcast" };

        // Bing image api 
        public static IList<string> BingImageList = new List<string> { "image", "images", "picture", "pic", "jpg", "png", "tiff" };

        // Bing video api
        public static IList<string> BingVideoList = new List<string> { "videos", "video", "Youtube", "gif" };

        // Happy with results list
        public static string[] HappyWithResults = { "Awesome! Happy to help.", "Glad to be of assistance!", "Fantastic!",
        "Nailed it!",
        };
        // Anything else list
        public static string[] AnythingElse = {"Do you have anything else in mind?",
                "Anything else that I can help you with?" };


        // Yes library for positive response
        public static string[] YesLibrary = { "yes", "great", "nice", "relevant", "sounds good","yes, it helps",
        "yeah", "okay", "certainly", "definitely", "affirmative", "good enough", "it's alright"};

        // No library for positive response
        public static string[] NoLibrary = { "no", "not really", "nope", "nah", "still no","doesn't sound right",
        "doesn't sound relevant", "doesn't sound right to me", "doesn't seem helpful to me",
        "no, doesn't help"};

        // Confirm general library
        public static string[] ConfirmGeneralLibrary = {"Is there anything else you want to ask me?", "Do you have any more questions for me?",
        "Is there anything else you’re curious about?", "Do you have something else to ask me?", "Is there something else you want to ask about?"};


        public static string[] ConfirmNewsLibrary = { "Do you want to see more news stories?", "Do you have any more news-related questions for me?" };

        public static string[] ConfirmImagesLibrary = { "Can I help you with something else?", "Do you have any more image queries for me?", "Do you want to do another image search?"};

        public static string[] ConfirmCreateGoalLibrary = { "Do you want to create another goal?", "Do you want me to create another goal?" };

        public static string[] GoalAlreadyExistsLibrary = { "Continue with a different goal name?", "Do you have a new goal name in mind?", "Would you like to enter a different name?" };


        public static string[] ConfirmCreateTaskLibrary = { "Do you want to create another task?", "Do you want me to create another task?" };
        public static string[] ConfirmGetCalendarLibrary = { "Is there anything else I can help you with?", "Do you have any more questions for me?",
            "Do you have any more questions about your schedule?","Do you have any more questions about your calendar?"
        };

        public static string[] CalendarScheduleSample = { "Show me my schedule on friday", "what's on my schedule today?" };

        public static string[] CalendarAppointmentSample = { "Do I have any appointments today?", "When is my next appointment?", "How many appointments do I have on friday?" };

        public static string[] TasksCreateSample = { "create a task", "create task" };
        public static string[] TasksEditSample = {"Edit a task", "Delete a task" };
        public static string[] AskAriSample = { "How's the weather today?", "Search the web for updates on Coronavirus", "Show me a picture of a capybara", "What time is it in Sydney?" };
        public static string[] CalendarTasksSample = {"Show me my schedule on friday", "what's on my schedule today?","What's my next task for today?",
            "Do I have high priority tasks tomorrow?","When is my next meeting?", "How many tasks do I have on friday?","Do I have any appointments today?", "When is my next appointment?", "How many appointments do I have on friday?"};

        public static string[] GoodByeLibrary = { "Okay, talk to you later!", "Great! Glad to help", "Alright, have a nice day!", "Okay. See you later",
            "Laters!", "Okay, have a good time", "Thanks, catch you later!", "Okay. Let me know when you need anything else.","Thank you and goodbye"

        };
        public static string[] GoalCreateSample = { "Create a goal" };
        public static string[] GoalEditSample = { "Edit a goal"};
        public static string[] GoalDeleteSample = {"Delete a goal" };

        public static string PersonalLifeGoal = "Personal Life";
        public static string[] PersonalLibrary = { "personal life", "personal" };
        public static string Funds = "Funds";
        public static string[] FundsLibrary = { "funds", "finances", "finance" };

        public static string[] WorkLibrary = { "work", "Career" };
        public static string WorkCareer = "Work and Career";

        public static string[] HealthLibrary = { "self care and wellness", "health", "fitness" };
        public static string Health = "Self care and Wellness";





        // Sunny weather messages 
        public static string[] SunnyWeather = {"{0} will be sunny with temperatures from around {1} to {2} degrees Celsius. Be sure to stay hydrated!",
            "The weather’s looking good on {0}...up to {1} degrees Celsius and partly sunny.", "Looks like it’s going to be sunny on {0}. The temperature will be around {1} to {2} degrees Celsius so don’t forget your sunscreen",
            "Yes, it’s going to be sunny on {0}. You can expect temperatures of around {1} to {2} degrees Celsius.", "Right now, it’s {0} degrees Celsius outside. Expect a high of {1} degrees throughout the day."};


        public static string[] RainyWeather = { "Oops, you might want to get your rain gear ready..{0} is looking a little rainy.","Yes, it’s going to pour on {0}. Don’t forget to pack an umbrella if you’re heading out!",
        "There’s a pretty good chance that it will rain on {0}", "Expect rainfall in {0} on {1} with a low of {2} degrees Celsius.",
        "It might rain at {0} on {1}.","Be sure to have the appropriate rain gear with you.","Looks like you can expect a shower in {0} {1}.An umbrella would come in handy!"
        };
        public static string[] StormyWeather = { "Expect a stormy weather {0} with rain and thunderstorms in {1}", "You might want to reschedule your errands. A stormy weather may hit {0} on {1}." +
                " Keep safe!","The weather’s not looking friendly {0}. Expect a storm.", "It looks like a storm is coming {0} with occasional wind and heavy rainfall. Keep warm and stay cozy."};

        public static string[] DrizzleWeather = { "The forecast expects a drizzle in {0} this day. Bring a raincoat in case it pours!", "Today’s forecast predicts a slightly showery weather with temperatures of around {0} to {1} degrees Celsius.",
            "You might want to bring your rain gear! It’s expected to be partly drizzly in your area {0}" };

        public static string[] CloudyWeather = { "It’s going to be a cloudy day in {0} on {1}", "On {0}, you can look forward to a cloudy weather with a low of {1} degrees.",
        "The weather should be cloudy {0}. You can expect temperatures of around {1} to {2} degrees Celsius","Expect a cloudy weather today with a low of {0} degrees Celsius", "We should be getting some clouds {1}. Here’s the forecast."};

        // Task type list
        public static string[] TaskType = {"Unscheduled", "Appointment", "Start date with no time",
            "Start date with time", "Start and End date with no time", "Start and End date with time" };

        public static string[] NoTaskFound = {"I found no task with that name. Can you try again?", "Oops! Looks like we have no record of this task. Try again",
            "It looks like you don't have a task with that name. Try again"
            };

        public static string[] NoGoalFound = {"I found no goal with that name. Can you try again?", "Oops! Looks like we have no record of this goal. Try again",
            "It looks like you don't have a goal with that name. Try again"
            };

        public static string HtmlBr = "<br>";

       public static string TaskSpurToken = "Token";

        public static string ConversationId = "conversationId";

        public static string ConversationToken = "conversionToken";

        // TaskSpur refresh token
       // public static string TaskSpurRefreshToken = "refreshToken";

        // TaskSpur TimeZone
        public static string TaskSpurTimeZone = "timeZone";

        // TaskSpur TimeZone
        public static string TaskSpurLocation = "location";
        
        // TaskSpur UserId
        public static string TaskSpurUserId = "id";
        public static string Version = "1.0";
        



        // TaskSpur Code
        public static string TaskSpurTokenCode = "code";

        // Weather Images
        public static string CloudImage = "Mostly%20Cloudy-Square.png";
        public static string DizzleImage = "Drizzle-Square.png";
        public static string rainImage = "rain.png";
        public static string stormImage = "storm.png";
        public static string sunImage = "sun.png";

        public static string WeatherJsonPath = @".\Dialogs\Common\Resources\Weather.json";

        // Constant name for TaskSpur goal dictionary  
        public static string GoalName = "GoalName";

        // Constant name for taskSpur task dictionary
        public static string TaskName = "TaskName";
        public static string TaskDescription = "TaskDescription";
        public static string GoalResponse = "GoalResponse";
        public static string GoalId = "GoalId";
        public static string PriorityId = "PriorityId";
        public static string StartDate = "StartDate";
        public static string EndDate = "EndDate";
        public static string StartTime = "StartTime";
        public static string EndTime = "EndTime";
        public static string TaskTypes = "TaskType";
        public static string RemindMe = "RemindMe";
        public static string SameReminder = "SameReminder";
        public static string ReminderTime = "ReminderTime";
        public static string NumericReminder = "NumericReminder";
        public static string ReminderDays = "Days";
        public static string ReminderHour = "Hours";
        public static string ReminderMinutes = "Minutes";
        public static string ReminderTimeId = "0";
        public static string ChangeReminder = "ChangeReminder";


        public static string TaskId = "TaskId";
        
        public static string SelectedEditOption = "SelectedEditOption";
        
        // Common random arrays
        public static string[] ChooseDeals = {"How can I help you today?", "What kind of assistance do you need?",
            "Please choose an option to get started"
            };
        public static string[] ChooseAriOptions = { "What type of search are looking for?", "For which category will you be searching?",
        "Please choose a search category to get more specific results"};

        public static string[] AskAriHelp = { "Tell me what you have in mind", "Let's see what you got", "What are you curious about?" };

        public static string[] InternetConfirmation = { "This is what I found for you on the internet. Is it what you are looking for?",
            "Here's what I found. Is this what you need?", 
            "Here are the top results from my search. Are these what you're looking for?" };

        public static string[] SearchAriImprove = { "Oops! It looks like you are not happy with the results. I have taken your input and will try to do better in the future.",
        "Thanks for letting me know. I'll try to get better results next time","It sure seems like I can do better"
        };

        public static string[] AskMoreInfo = { "You can choose to give me more details or stop your search and leave a feedback instead",
        "Do you want to change your search term? If so, please Elaborate More","Please elaborate more if you want to change your search keyword"
        };

        public static string[] ElaborateMoreDetails = { "Adding more details to your search keyword would help me give you a better result",
        "A few more details would be helpful!","To get a more specific result, please add more details to your search term"
        };

        public static string[] RepromptConfirmation = { "I encountered an error with your search term. Please select an option before moving on",
        "Oops! I think you entered another keyword before selecting an option. Please try again","I admit I'm still learning. Please choose a feedback first before searching again."

        };

        public static string[] Improve = { "Thank you for helping me improve. Can you tell me why you were unhappy with the results?",
        "Thank you for helping me improve. Please tell me more about the issue so I can give you better results in the future.",
        "This partnership only works with your help so thank you for giving me a feedback. Tell me what can be better"
        };

        public static string[] AskName = { "Can I have your name please?", "How should I call you?", "How do you want to be called?" };

        public static string[] AskEmail = { "Got it {0}! And can I have your email address please?", "Thanks, {0}. Please provide me a working email address", "Thanks, {0}. At which email address can we reach you?" };

        public static string[] ThankyouForFeedback = { "Thank you for making me better", "Got your feedback! Much appreciated", "Your word is in! Thank you for your generous feedback." };

        public static string[] AskTaskName = { "Sure! What would you like to call this task?","How do you want to call this task?", "What should we name this task?" };

        public static string[] AskTaskDescription = { "Please enter the task details", "What do you need to do to accomplish in this task?", "Does this task involve steps? If so, please enter the details" };


        #endregion

        public static class PollyContextItems
        {
            public const string HttpClientInstance = nameof(HttpClientInstance);
        }
    }


}
