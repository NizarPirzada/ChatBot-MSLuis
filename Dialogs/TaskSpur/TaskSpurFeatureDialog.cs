using AriBotV4.Common;
using AriBotV4.Dialogs.Common.Resources;
using AriBotV4.Enums;
using AriBotV4.Models;
using AriBotV4.Models.TaskSpur.Auth;
using AriBotV4.Models.TaskSpur.Goals.Get;
using AriBotV4.Models.TaskSpur.Tasks;
using AriBotV4.Models.TaskSpur.Tasks.Reminder;
using AriBotV4.Services;
using AutoMapper.Configuration;
using Azure;
using Azure.AI.TextAnalytics;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TimeZoneConverter;
using TokenResponse = AriBotV4.Models.TaskSpur.Auth.TokenResponse;

namespace AriBotV4.Dialogs.TaskSpur
{
    public class TaskSpurFeatureDialog : ComponentDialog
    {
        #region Properties and Fields
        private static BotStateService _botStateService;
        private readonly BotServices _botServices;
        private readonly IConfiguration _configuration;
        private static AzureKeyCredential credentials;
        private static Uri endpoint;

        #endregion



        #region WaterFallSteps and Dialogs
        public TaskSpurFeatureDialog(string dialogId, BotStateService botStateService, BotServices botServices) : base(dialogId)
        {
            _botStateService = botStateService ?? throw new System.ArgumentNullException(nameof(botStateService));
            _botServices = botServices ?? throw new System.ArgumentNullException(nameof(botServices));
            credentials = new AzureKeyCredential(botStateService._textAnalyticsSettings.Key);
            endpoint = new Uri(botStateService._textAnalyticsSettings.EndPoint);
            InitializeWaterfallDialog();
        }

        // Initializing waterfall steps
        private void InitializeWaterfallDialog()
        {
            // Create Waterfall Steps
            var waterfallSteps = new WaterfallStep[]
            {
                ConfirmCreateTask,
                CheckTaskname,
                ConfirmTaskname,
                AskTaskName,
                AskTaskDateandTime,
                CheckTaskTime,
                TaskTimeResponse,
                AskTaskTime,
                AskTaskReminder,
                AskTaskReminderTime,
               CreateTask,
               StepEndAsync
            };

            AddDialog(new WaterfallDialog($"{nameof(TaskSpurFeatureDialog)}.mainFlow", waterfallSteps));
            AddDialog(new TextPrompt($"{nameof(TaskSpurFeatureDialog)}.name", Utility.ValidateTaskName));
            AddDialog(new TextPrompt($"{nameof(TaskSpurFeatureDialog)}.checkTaskName", Utility.ValidateTaskName));
            AddDialog(new TextPrompt($"{nameof(TaskSpurFeatureDialog)}.remindMe", ValidateChoice));
            AddDialog(new TextPrompt($"{nameof(TaskSpurFeatureDialog)}.confirmTaskName"));
            AddDialog(new TextPrompt($"{nameof(TaskSpurFeatureDialog)}.remindMeTime"));
            AddDialog(new TextPrompt($"{nameof(TaskSpurFeatureDialog)}.askDateandTime"));
            AddDialog(new TextPrompt($"{nameof(TaskSpurFeatureDialog)}.askTaskTime"));
            AddDialog(new TextPrompt($"{nameof(TaskSpurFeatureDialog)}.askTaskTime2"));
            AddDialog(new DateTimePrompt($"{nameof(TaskSpurFeatureDialog)}.startTime", ValidateTime));
            AddDialog(new TextPrompt($"{nameof(TaskSpurFeatureDialog)}.confirmTask", Utility.ValidateTaskName));
            AddDialog(new CommonDialog($"{nameof(CommonDialog)}.mainFlow", _botStateService));
            InitialDialogId = $"{nameof(TaskSpurFeatureDialog)}.mainFlow";
        }


        public async Task<DialogTurnResult> ConfirmCreateTask(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

          if (string.IsNullOrEmpty(Convert.ToString(stepContext.Context.Activity.From.Properties[Constants.TaskSpurToken])))
            {
                return await stepContext.BeginDialogAsync($"{nameof(CommonDialog)}.mainFlow", null, cancellationToken);
            }
            else
            {
                
                LuisModel luisResponse = (LuisModel)stepContext.ActiveDialog.State["options"];
                LuisModel.Intent topIntent = new LuisModel.Intent();
                if (luisResponse != null)
                    topIntent = luisResponse.TopIntent().intent;
                if (luisResponse.Entities._instance.TaskGoalName == null && topIntent != LuisModel.Intent.Create_Task && luisResponse.Entities._instance.Status != null)
                {

                    var opts = new PromptOptions
                    {
                        Prompt = new Microsoft.Bot.Schema.Activity
                        {

                            Type = ActivityTypes.Message,
                            Text =TaskSpur.Resources.TaskSpur.ConfirmTask,
                            SuggestedActions = new SuggestedActions()
                            {
                                Actions = new List<CardAction>()
                            {
                                new CardAction() { Title = SharedStrings.ConfirmYes, Type = ActionTypes.ImBack,
                                    Value = SharedStrings.ConfirmYes },
                                new CardAction() { Title = SharedStrings.ConfirmNo, Type = ActionTypes.ImBack,
                                    Value = SharedStrings.ConfirmNo },
                            },
                            },
                        },
                        RetryPrompt = new Microsoft.Bot.Schema.Activity
                        {

                            Type = ActivityTypes.Message,
                            Text = SharedStrings.Sorry,
                            SuggestedActions = new SuggestedActions()
                            {
                                Actions = new List<CardAction>()
                            {
                                new CardAction() { Title = SharedStrings.ConfirmYes, Type = ActionTypes.ImBack,
                                    Value = SharedStrings.ConfirmYes },
                                new CardAction() { Title = SharedStrings.ConfirmNo, Type = ActionTypes.ImBack,
                                    Value = SharedStrings.ConfirmNo },
                            },
                            },
                        },

                    };

                    // Display a Text Prompt with suggested actions and wait for input
                    return await stepContext.PromptAsync($"{nameof(TaskSpurFeatureDialog)}.confirmTask", opts);
                }

            }
            return await stepContext.NextAsync(stepContext.Context.Activity.Text, cancellationToken);

        }
        public async Task<DialogTurnResult> CheckTaskname(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
           LuisModel luisResponse = (LuisModel)stepContext.ActiveDialog.State["options"];
            Type stringType = typeof(System.String);
            Type ChoiceType = typeof(System.Boolean);

            Type resultType = stepContext.Result.GetType();
            string selectedChoice = (resultType == stringType ? Convert.ToString(stepContext.Result) :
            Convert.ToString(((FoundChoice)stepContext.Result)));

            if (Constants.YesLibrary.Any(str => str.ToLower() == (selectedChoice.ToLower())))
            {
                return await stepContext.PromptAsync($"{nameof(TaskSpurFeatureDialog)}.checkTaskName",
                     new PromptOptions
                     {
                         Prompt = MessageFactory.Text(Utility.GenerateRandomMessages(Constants.AskTaskName)),
                         RetryPrompt = MessageFactory.Text(TaskSpur.Resources.TaskSpur.TaskEmpty)
                     }, cancellationToken);
            }
            else if (Constants.NoLibrary.Any(str => str.ToLower() == (selectedChoice.ToLower())))
            {
                await stepContext.EndDialogAsync(null, cancellationToken);
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(TaskSpur.Resources.TaskSpur.GetTaskInput), cancellationToken);
                return await stepContext.BeginDialogAsync($"{nameof(RootDialog)}.contactOptions", null, cancellationToken);
            }
            else if(luisResponse.Entities._instance.TaskGoalName == null)
            {
                return await stepContext.PromptAsync($"{nameof(TaskSpurFeatureDialog)}.checkTaskName",
                     new PromptOptions
                     {
                         Prompt = MessageFactory.Text(Utility.GenerateRandomMessages(Constants.AskTaskName)),
                         RetryPrompt = MessageFactory.Text(TaskSpur.Resources.TaskSpur.TaskEmpty)
                     }, cancellationToken);
            }
            else
            {
                return await stepContext.NextAsync(stepContext.Context.Activity.Text, cancellationToken);
            }
        }

        public async Task<DialogTurnResult> ConfirmTaskname(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            LuisModel luisResponse = (LuisModel)stepContext.ActiveDialog.State["options"];
            UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
            userProfile.CreateTask = new Dictionary<string, object>();
           
            // get goal id
            string category = string.Empty;
            if (luisResponse.Entities._instance.category != null)
            {

                if (Constants.PersonalLibrary.Contains(luisResponse.Entities._instance.category[0].Text))
                    category = GoalCategoryEnum.PersonalLife.ToString();

                else if (Constants.FundsLibrary.Contains(luisResponse.Entities._instance.category[0].Text))
                category = GoalCategoryEnum.Funds.ToString();

                else if (Constants.HealthLibrary.Contains(luisResponse.Entities._instance.category[0].Text))
                    category = GoalCategoryEnum.SelfCareAndWellness.ToString();

                else if (Constants.WorkLibrary.Contains(luisResponse.Entities._instance.category[0].Text))
                    category = GoalCategoryEnum.WorkAndCareer.ToString();

            }
            else
            {
                category = Constants.PersonalLifeGoal;
            }

            GetGoalsResponse goalResponse = await _botStateService._taskSpurApiClient.GetGoals
                              (Convert.ToString(stepContext.Context.Activity.From.Properties[AriBotV4.Common.Constants.TaskSpurToken]), category);



            // Check token authrozation
            //if (goalResponse.statusCode == 401)
            //{

            //    // Get refersh token
            //    TokenResponse refreshTokenResponse = await _botStateService._taskSpurApiClient.GetRefreshToken(stepContext);

            //    // Get goal with refresh token
            //    if (!string.IsNullOrEmpty(refreshTokenResponse.data.token))
            //    {
            //        goalResponse = await _botStateService._taskSpurApiClient.GetGoals
            //        (refreshTokenResponse.data.token, Constants.PersonalLifeGoal);
            //        // Update users's from property
            //        await _botStateService._taskSpurApiClient.UpdateToken(stepContext, refreshTokenResponse);
            //    }




            //}
            if (goalResponse != null)
                userProfile.CreateTask.Add(Constants.GoalId, goalResponse.data.data[0].id);



            // get priority
            if (luisResponse.Entities._instance.priority != null)
            {
                AriBotV4.Enums.PriorityEnum Priority = EnumHelpers.GetValueFromDescription<AriBotV4.Enums.PriorityEnum>(luisResponse.Entities._instance.priority[0].Text);
                userProfile.CreateTask.Add(Constants.PriorityId, (int)Priority);

            }
            else
            {
                userProfile.CreateTask.Add(Constants.PriorityId, 1);
            }


            // Add start time
            AddTaskStartTime(luisResponse, userProfile);
            // Add start and End Date
            var result = JsonConvert.DeserializeObject<LuisDateModel>(JsonConvert.SerializeObject(luisResponse.Properties["luisResult"]));
            TimeZoneInfo timeInfo = TZConvert.GetTimeZoneInfo(Convert.ToString(stepContext.Context.Activity.From.Properties[Constants.TaskSpurTimeZone]));
            AddTaskDates(result, timeInfo, userProfile);


            if (luisResponse.Entities._instance.TaskGoalName != null)
            {
                
                var reply = stepContext.Context.Activity.CreateReply();
                reply.Text = Convert.ToString(TaskSpur.Resources.TaskSpur.SetupTask);
                await stepContext.Context.SendActivityAsync(reply, cancellationToken);


                // Add task name
                userProfile.CreateTask.Add(Constants.TaskName, luisResponse.Entities._instance.TaskGoalName[0].Text);

                // Add task description
                userProfile.CreateTask.Add(Constants.TaskDescription, luisResponse.Entities._instance.TaskGoalName[0].Text);

                var opts = new PromptOptions
                {
                    Prompt = new Microsoft.Bot.Schema.Activity
                    {

                        Type = ActivityTypes.Message,
                        Text = Convert.ToString(TaskSpur.Resources.TaskSpur.ConfirmTaskName) + Constants.HtmlBr +
                        luisResponse.Entities._instance.TaskGoalName[0].Text,

                        SuggestedActions = new SuggestedActions()
                        {
                            Actions = new List<CardAction>()
                            {
                                new CardAction() { Title = SharedStrings.ConfirmYes, Type = ActionTypes.ImBack,
                                    Value = SharedStrings.ConfirmYes },
                                new CardAction() { Title = SharedStrings.Rename, Type = ActionTypes.ImBack,
                                    Value = SharedStrings.Rename },
                            },
                        },
                    },

                };

                // Display a Text Prompt with suggested actions and wait for input
                return await stepContext.PromptAsync($"{nameof(TaskSpurFeatureDialog)}.confirmTaskName", opts);

            }
          else 
            {
                userProfile.CreateTask.Add(Constants.TaskName, stepContext.Context.Activity.Text);
                userProfile.CreateTask.Add(Constants.TaskDescription, stepContext.Context.Activity.Text);
            }
            return await stepContext.NextAsync(stepContext, cancellationToken);

        }
        // Ask user task name
        public async Task<DialogTurnResult> AskTaskName(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            
            LuisModel luisResponse = (LuisModel)stepContext.ActiveDialog.State["options"];
            if (luisResponse.Entities._instance.TaskGoalName != null)
            {
                 UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
                
                Type stringType = typeof(System.String);
                Type ChoiceType = typeof(System.Boolean);

                Type resultType = stepContext.Result.GetType();
                string selectedChoice = (resultType == stringType ? Convert.ToString(stepContext.Result) :
                Convert.ToString(((FoundChoice)stepContext.Result)));

                if (Constants.YesLibrary.Any(str => str.ToLower() == (selectedChoice.ToLower())))
                {


                    return await stepContext.NextAsync(stepContext.Context.Activity.Text, cancellationToken);

                }
                else if (selectedChoice.Contains("Rename"))
                {

                    userProfile.CreateTask.Remove(Constants.TaskName);
                    userProfile.CreateTask.Remove(Constants.TaskDescription);
                    return await stepContext.PromptAsync($"{nameof(TaskSpurFeatureDialog)}.name",
                         new PromptOptions
                         {
                             Prompt = MessageFactory.Text(Utility.GenerateRandomMessages(Constants.AskTaskName)),
                             RetryPrompt = MessageFactory.Text(TaskSpur.Resources.TaskSpur.TaskEmpty)
                         }, cancellationToken);
                }
                else
                {
                    return await stepContext.ReplaceDialogAsync(InitialDialogId);
                }
            }
            return await stepContext.NextAsync(stepContext, cancellationToken);

        }


        public async Task<DialogTurnResult> AskTaskDateandTime(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
            if (!userProfile.CreateTask.ContainsKey(Constants.TaskName))
            {
                userProfile.CreateTask.Add(Constants.TaskName, stepContext.Context.Activity.Text);
                userProfile.CreateTask.Add(Constants.TaskDescription, stepContext.Context.Activity.Text);
            }

            LuisModel luisresponse = (LuisModel)stepContext.ActiveDialog.State["options"];
            if(luisresponse.Entities._instance.datetime == null)
            {
                {
                    return await stepContext.PromptAsync($"{nameof(TaskSpurFeatureDialog)}.askDateandTime",
           new PromptOptions
           {
               Prompt = MessageFactory.Text(TaskSpur.Resources.TaskSpur.TaskDateAndTime)
           }, cancellationToken);

                }
            }
            else
            {
                return await stepContext.NextAsync(stepContext.Context.Activity.Text, cancellationToken);
            }

        }

        public async Task<DialogTurnResult> CheckTaskTime(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
            RecognizerResult recognizerResult = await _botServices.Dispatch.RecognizeAsync(stepContext.Context, cancellationToken);
           
            LuisModel luisResponse = JsonConvert.DeserializeObject<LuisModel>(JsonConvert.SerializeObject(recognizerResult));

            var result = JsonConvert.DeserializeObject<LuisDateModel>(JsonConvert.SerializeObject(luisResponse.Properties["luisResult"]));
            TimeZoneInfo timeInfo = TZConvert.GetTimeZoneInfo(Convert.ToString(stepContext.Context.Activity.From.Properties[Constants.TaskSpurTimeZone]));


            // Add start time
            AddTaskStartTime(luisResponse, userProfile);
            // Add start and End Date
            AddTaskDates(result, timeInfo, userProfile);

           if (luisResponse != null && (luisResponse.Entities.datetime != null && luisResponse.Entities.datetime[0].Type.ToLower() == "time") 
                || (luisResponse.Entities.datetime == null && luisResponse.Entities.number != null) || (luisResponse.Entities.datetime == null) || (luisResponse.Entities.number == null))
            {
                return await stepContext.NextAsync(stepContext.Context.Activity.Text, cancellationToken);
            }
           
            if (userProfile.CreateTask != null && !userProfile.CreateTask.ContainsKey(Constants.StartTime))
            {
                {
                    return await stepContext.PromptAsync($"{nameof(TaskSpurFeatureDialog)}.askTaskTime",
           new PromptOptions
           {
               Prompt = MessageFactory.Text(TaskSpur.Resources.TaskSpur.TaskStartTime)
           }, cancellationToken);

                }
            }

            return await stepContext.NextAsync(stepContext.Context.Activity.Text, cancellationToken);

        }
        public async Task<DialogTurnResult> TaskTimeResponse(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
            var recognizerResult = await _botServices.Dispatch.RecognizeAsync(stepContext.Context, cancellationToken);
           
            LuisModel luisResponse = JsonConvert.DeserializeObject<LuisModel>(JsonConvert.SerializeObject(recognizerResult));
            
            if (stepContext.Context.Activity.Text.ToLower() == "yes" && userProfile.CreateTask != null && !userProfile.CreateTask.ContainsKey(Constants.StartTime))
            {
                {
                    return await stepContext.PromptAsync($"{nameof(TaskSpurFeatureDialog)}.askTaskTime2",
           new PromptOptions
           {
               Prompt = MessageFactory.Text(TaskSpur.Resources.TaskSpur.TaskTime)
           }, cancellationToken);

                }

            }
             else
            {
                return await stepContext.NextAsync(stepContext.Context.Activity.Text, cancellationToken);
            }

        }

            public async Task<DialogTurnResult> AskTaskTime(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

           
            UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
            

            int i = 0;
            LuisModel luisResponse = (LuisModel)stepContext.ActiveDialog.State["options"];

            if (luisResponse.Entities._instance.datetime != null && userProfile.CreateTask != null && !userProfile.CreateTask.ContainsKey(Constants.StartTime))
            {
                var recognizerResult = await _botServices.Dispatch.RecognizeAsync(stepContext.Context, cancellationToken);
                LuisModel response = JsonConvert.DeserializeObject<LuisModel>(JsonConvert.SerializeObject(recognizerResult));
                if ((response.Entities._instance.datetime != null && response.Entities._instance.datetime[0].Type == "builtin.datetimeV2.time") || (response.Entities._instance.datetime != null && response.Entities._instance.datetime[0].Type == "builtin.datetimeV2.datetime"))
                {
                    var remindMe = Microsoft.Recognizers.Text.DataTypes.TimexExpression.TimexResolver.Resolve
                 (new[] { response.Entities.datetime[0].Expressions[0] },
                     System.DateTime.Today);
                    string startT = string.Format(AriBotV4.Common.Constants.TaskTimeFormat, Convert.ToDateTime(remindMe.Values[0].Value));
                    userProfile.CreateTask.Add(Constants.StartTime, startT);
                    return await stepContext.NextAsync(stepContext.Context.Activity.Text, cancellationToken);
                }
                
            }

            var result = JsonConvert.DeserializeObject<LuisDateModel>(JsonConvert.SerializeObject(luisResponse.Properties["luisResult"]));
            var getTime = result.entities.FirstOrDefault(item => item.type == "builtin.datetimeV2.datetime" || item.type == "builtin.datetimeV2.time" || item.type == "builtin.number");
            if((getTime!= null && getTime.type== "builtin.datetimeV2.datetime" && getTime.resolution.values.Count > 1 ) || (getTime != null && getTime.type == "builtin.datetimeV2.time" && getTime.resolution.values.Count > 1) ||
                (getTime != null && getTime.type == "builtin.number") )
            {
                return await stepContext.PromptAsync($"{nameof(TaskSpurFeatureDialog)}.startTime",
       new PromptOptions
       {
           Prompt = MessageFactory.Text(string.Format(TaskSpur.Resources.TaskSpur.ConfirmAMPM, getTime.entity,getTime.entity))
       }, cancellationToken) ;
               

            }
            else if(int.TryParse(stepContext.Context.Activity.Text, out i))
            {

                return await stepContext.PromptAsync($"{nameof(TaskSpurFeatureDialog)}.startTime",
               new PromptOptions
               {
                   Prompt = MessageFactory.Text(string.Format(TaskSpur.Resources.TaskSpur.ConfirmAMPM, i,i))
               }, cancellationToken);
            }



            return await stepContext.NextAsync(stepContext.Context.Activity.Text, cancellationToken);


        }

        public async Task<DialogTurnResult> AskTaskReminder(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
            if (userProfile.CreateTask != null && !userProfile.CreateTask.ContainsKey(Constants.TaskName))
            {
               // userProfile.CreateTask = new Dictionary<string, object>();
               
                userProfile.CreateTask.Add(Constants.TaskName, (string)stepContext.Result);
                 
                userProfile.CreateTask.Add(Constants.TaskDescription, (string)stepContext.Result);
            }
            var recognizerResult = await _botServices.Dispatch.RecognizeAsync(stepContext.Context, cancellationToken);
            TimeZoneInfo timeInfo = TZConvert.GetTimeZoneInfo(Convert.ToString(stepContext.Context.Activity.From.Properties[Constants.TaskSpurTimeZone]));
            LuisModel response = JsonConvert.DeserializeObject<LuisModel>(JsonConvert.SerializeObject(recognizerResult));
            var result = JsonConvert.DeserializeObject<LuisDateModel>(JsonConvert.SerializeObject(response.Properties["luisResult"]));
            AddTaskStartTime(response, userProfile);
            AddTaskDates(result, timeInfo, userProfile);
           //if (response.Entities.datetime != null)
           // //if (stepContext.Result is IList<DateTimeResolution> datetimes)
           // {
           //     if (userProfile.CreateTask != null && userProfile.CreateTask.ContainsKey(Constants.StartTime))
           //     {
           //         userProfile.CreateTask.Remove(Constants.StartTime);
           //      }


            //     //string startT = string.Format(AriBotV4.Common.Constants.TaskTimeFormat, Convert.ToDateTime(datetimes.First().Value));
            //     //userProfile.CreateTask.Add(Constants.StartTime, startT);

            //     //if (datetimes[0].Start != null)
            //     //{

            //         if (userProfile.CreateTask != null && !userProfile.CreateTask.ContainsKey(Constants.StartDate))
            //         {
            //            // userProfile.CreateTask.Remove(Constants.StartDate);

            //         TimeZoneInfo timeInfo = TZConvert.GetTimeZoneInfo(Convert.ToString(stepContext.Context.Activity.From.Properties[Constants.TaskSpurTimeZone]));
            //         DateTime start = new DateTime();
            //         DateTime localDate = DateTime.UtcNow;
            //         DateTime utcTime = localDate.ToUniversalTime();
            //         string startDate = string.Empty;
            //         // Get time info for user timezone
            //         // Convert time to UTC
            //         DateTime userDateTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, timeInfo);
            //         if (userDateTime.Hour < Convert.ToDateTime(string.Format(AriBotV4.Common.Constants.TaskTimeFormat, userProfile.CreateTask[Constants.StartTime])).Hour)
            //         {
            //             start = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, timeInfo.Id);
            //         }
            //         else
            //         {
            //             start = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow.AddDays(1), timeInfo.Id);
            //         }
            //         if (start != DateTime.MinValue && userProfile.CreateTask != null && !userProfile.CreateTask.ContainsKey(Constants.StartDate))
            //         {
            //             startDate = start.ToString(AriBotV4.Common.Constants.CreateTaskDate).Replace("-", "/");
            //             userProfile.CreateTask.Add(Constants.StartDate, startDate);
            //         }
            //     }
            // }

            if (userProfile.CreateTask != null && userProfile.CreateTask.ContainsKey(Constants.StartTime))
           {
                    var opts = new PromptOptions
                    {
                        Prompt = new Microsoft.Bot.Schema.Activity
                        {

                            Type = ActivityTypes.Message,
                            Text = TaskSpur.Resources.TaskSpur.TaskRemindMe,
                            SuggestedActions = new SuggestedActions()
                            {
                                Actions = new List<CardAction>()
                            {
                                new CardAction() { Title = SharedStrings.ConfirmYes, Type = ActionTypes.ImBack,
                                    Value = SharedStrings.ConfirmYes },
                                new CardAction() { Title = SharedStrings.ConfirmNo, Type = ActionTypes.ImBack,
                                    Value = SharedStrings.ConfirmNo },
                            },
                            },
                        },
                        RetryPrompt = new Microsoft.Bot.Schema.Activity
                        {

                            Type = ActivityTypes.Message,
                            Text = SharedStrings.Sorry,
                            SuggestedActions = new SuggestedActions()
                            {
                                Actions = new List<CardAction>()
                            {
                                new CardAction() { Title = SharedStrings.ConfirmYes, Type = ActionTypes.ImBack,
                                    Value = SharedStrings.ConfirmYes },
                                new CardAction() { Title = SharedStrings.ConfirmNo, Type = ActionTypes.ImBack,
                                    Value = SharedStrings.ConfirmNo },
                            },
                            },
                        },

                    };

                    // Display a Text Prompt with suggested actions and wait for input
                    return await stepContext.PromptAsync($"{nameof(TaskSpurFeatureDialog)}.remindMe", opts);
                }
                else
                {
                    //return await stepContext.NextAsync(stepContext.Context.Activity.Text, cancellationToken);
                    return await CreateTask(stepContext, cancellationToken);
                }
            
        }
        public async Task<DialogTurnResult> AskTaskReminderTime(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
           Type stringType = typeof(System.String);
            Type ChoiceType = typeof(System.Boolean);
           
            Type resultType = stepContext.Result.GetType();

            
            string selectedChoice = (resultType == stringType ? Convert.ToString(stepContext.Result) :
            Convert.ToString(((FoundChoice)stepContext.Result)));

            bool isResponseYes = Constants.YesLibrary.Any(str => str.ToLower() == (selectedChoice.ToLower()));
            bool isResponseNo = Constants.NoLibrary.Any(str => str.ToLower() == (selectedChoice.ToLower()));

            UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
            

           
                if (isResponseYes)
                {
                    
                    if (!userProfile.CreateTask.ContainsKey(Constants.RemindMe))
                    {
                        userProfile.CreateTask.Add(Constants.RemindMe, true);
                        await _botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);
                    }
                    return await stepContext.PromptAsync($"{nameof(TaskSpurFeatureDialog)}.remindMeTime",
                         new PromptOptions
                         {
                             Prompt = MessageFactory.Text(TaskSpur.Resources.TaskSpur.TaskRemindTime)
                         }, cancellationToken);
                }
            else if (userProfile.CreateTask != null && !userProfile.CreateTask.ContainsKey(Constants.ReminderTime) && userProfile.CreateTask.ContainsKey(Constants.RemindMe))
            {
                return await stepContext.PromptAsync($"{nameof(TaskSpurFeatureDialog)}.remindMeTime",
                     new PromptOptions
                     {
                         Prompt = MessageFactory.Text(TaskSpur.Resources.TaskSpur.TaskRemindTime)
                     }, cancellationToken);
            }
            else
                    {
                    return await stepContext.NextAsync(stepContext.Context.Activity.Text, cancellationToken);
                }
           
        }

         

private async Task<DialogTurnResult> CreateTask(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
                    UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
            if (userProfile.CreateTask != null &&  userProfile.CreateTask.ContainsKey(Constants.RemindMe))
            {

                RecognizerResult recognizerResult = new RecognizerResult();
                LuisModel luisDateResponse = new LuisModel();
                

                // First, we use the dispatch model to determine which cognitive service (LUIS or Qna) to use
                try
                {
                    recognizerResult = await _botServices.Dispatch.RecognizeAsync(stepContext.Context, cancellationToken);
                    luisDateResponse = JsonConvert.DeserializeObject<LuisModel>(JsonConvert.SerializeObject(recognizerResult));
                    //Convert
                }
                catch (Exception ex)
                {

                }
                if (luisDateResponse.Entities.datetime != null)
                {

                    var remindMe = Microsoft.Recognizers.Text.DataTypes.TimexExpression.TimexResolver.Resolve
                (new[] { luisDateResponse.Entities.datetime[0].Expressions[0] },
                    System.DateTime.Today);

                    if (remindMe.Values != null && remindMe.Values[0] != null)
                    {
                        TimeSpan timeSpan = TimeSpan.FromSeconds(Convert.ToDouble(remindMe.Values[0].Value));
                        if (!userProfile.CreateTask.ContainsKey(Constants.ReminderTime))
                            userProfile.CreateTask.Add(Constants.ReminderTime, Convert.ToInt32(timeSpan.TotalMinutes));
                    }


                    GetReminderResponse reminderResponse = await _botStateService._taskSpurApiClient.GetReminderList();
                    int reminder = (int)userProfile.CreateTask[Constants.ReminderTime];
                    var closest = reminderResponse.data.OrderBy(item => Math.Abs(reminder - item.minutes)).First();
                    if (reminder == 0)
                        closest = reminderResponse.data[1];
                    else if (closest.minutes == 0)
                        closest = reminderResponse.data[2];
                    userProfile.CreateTask.TryAdd(Constants.ReminderTimeId, closest.id);
                }
                else
                {
                    userProfile.CreateTask.Remove(Constants.ReminderTime);
                    stepContext.ActiveDialog.State["stepIndex"] = (int)stepContext.ActiveDialog.State["stepIndex"] - 1;
                    return await AskTaskReminderTime(stepContext, cancellationToken);
                }

            }

            if ((LuisModel)stepContext.ActiveDialog.State["options"] != null)

            {

                
               //LuisModel luisResponse = (LuisModel)stepContext.ActiveDialog.State["options"];
                            
                CreateTaskRequest createTaskRequest = new CreateTaskRequest();
                createTaskRequest.name = (string)userProfile.CreateTask[Constants.TaskName];
                createTaskRequest.description = (string)userProfile.CreateTask[Constants.TaskDescription];
                //if (luisResponse.Entities._instance.priority != null)
                //{
                //    AriBotV4.Enums.PriorityEnum Priority = EnumHelpers.GetValueFromDescription<AriBotV4.Enums.PriorityEnum>(luisResponse.Entities._instance.priority[0].Text);
                //    createTaskRequest.priorityId = (int)Priority;
             
                //}
                //else
                //{
                //    createTaskRequest.priorityId = 1;
                //}
                

                if (userProfile.CreateTask != null && userProfile.CreateTask.ContainsKey(Constants.RemindMe))
                    createTaskRequest.remindMe = (bool)userProfile.CreateTask[Constants.RemindMe];
                if (userProfile.CreateTask != null && userProfile.CreateTask.ContainsKey(Constants.ReminderTimeId))
                    createTaskRequest.reminderTimeId = (int)userProfile.CreateTask[Constants.ReminderTimeId];
                if (userProfile.CreateTask != null && userProfile.CreateTask.ContainsKey(Constants.ReminderTime))
                    createTaskRequest.reminderTime = (int)userProfile.CreateTask[Constants.ReminderTime];
                if (userProfile.CreateTask != null && userProfile.CreateTask.ContainsKey(Constants.PriorityId))
                    createTaskRequest.priorityId = (int)userProfile.CreateTask[Constants.PriorityId];
                if (userProfile.CreateTask != null && userProfile.CreateTask.ContainsKey(Constants.GoalId))
                    createTaskRequest.goalId = (int)userProfile.CreateTask[Constants.GoalId];

                //// get goal id
                //string category = string.Empty;
                //if (luisResponse.Entities._instance.category != null)
                //{
                //    category = luisResponse.Entities._instance.category[0].Text;

                //}
                //else
                //{
                //    category = Constants.PersonalLifeGoal;
                //}

                //GetGoalsResponse goalResponse = await _botStateService._taskSpurApiClient.GetGoals
                //  (Convert.ToString(stepContext.Context.Activity.From.Properties[AriBotV4.Common.Constants.TaskSpurToken]), category);



                //// Check token authrozation
                //if (goalResponse.statusCode == 401)
                //{

                //    // Get refersh token
                //    TokenResponse refreshTokenResponse = await _botStateService._taskSpurApiClient.GetRefreshToken(stepContext);

                //    // Get goal with refresh token
                //    if (!string.IsNullOrEmpty(refreshTokenResponse.data.token))
                //    {
                //        goalResponse = await _botStateService._taskSpurApiClient.GetGoals
                //        (refreshTokenResponse.data.token,Constants.PersonalLifeGoal);
                //        // Update users's from property
                //        await _botStateService._taskSpurApiClient.UpdateToken(stepContext, refreshTokenResponse);
                //    }




                //}
                //if(goalResponse != null)
                //createTaskRequest.goalId = goalResponse.data.data[0].id;

                if (userProfile.CreateTask != null && userProfile.CreateTask.ContainsKey(Constants.StartDate))
                createTaskRequest.startDate = (string)userProfile.CreateTask[Constants.StartDate].ToString();
                //if (end != DateTime.MinValue)
                if (userProfile.CreateTask != null && userProfile.CreateTask.ContainsKey(Constants.EndDate))
                    createTaskRequest.endDate = (string)userProfile.CreateTask[Constants.EndDate].ToString();
                if (userProfile.CreateTask.ContainsKey(Constants.StartTime))
                {
                    createTaskRequest.startTime = (string)userProfile.CreateTask[Constants.StartTime];
                   


                }
                if (userProfile.CreateTask.ContainsKey(Constants.EndTime))
                {
                    createTaskRequest.endTime = (string)userProfile.CreateTask[Constants.EndTime];
                 
                }

              createTaskRequest.timeZone = Convert.ToString(stepContext.Context.Activity.From.Properties[AriBotV4.Common.Constants.TaskSpurTimeZone]);

                CreateTaskResponse response = await _botStateService._taskSpurApiClient.CreateTask(createTaskRequest,
               Convert.ToString(stepContext.Context.Activity.From.Properties[AriBotV4.Common.Constants.TaskSpurToken]));

                // Check token authrozation
                //if (response.statusCode == 401)
                //{
                //    GetRefreshTokenRequest tokenRequest = new GetRefreshTokenRequest();
                //    tokenRequest.token = Convert.ToString(stepContext.Context.Activity.From.Properties[AriBotV4.Common.Constants.TaskSpurToken]);
                //    tokenRequest.refreshToken = Convert.ToString(stepContext.Context.Activity.From.Properties[AriBotV4.Common.Constants.TaskSpurRefreshToken]);

                //    // Get refersh token
                //    TokenResponse refreshTokenResponse = await _botStateService._taskSpurApiClient.GetRefreshToken(stepContext);

                //    // Create tast with refresh token
                //    if (!string.IsNullOrEmpty(refreshTokenResponse.data.token))
                //    {
                //        response = await _botStateService._taskSpurApiClient.CreateTask(createTaskRequest,
                //        refreshTokenResponse.data.token);
                //        await stepContext.Context.SendActivityAsync(MessageFactory.Text(response.toast.message));

                //    }

                //    // Update users's from property
                //    await _botStateService._taskSpurApiClient.UpdateToken(stepContext, refreshTokenResponse);

                //}
                //else

                //{
                if (response != null)
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(response.toast.message));
                }
                else
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(SharedStrings.Sorry), cancellationToken);

                }

                //}


            }
            userProfile.CreateTask = null;
            await stepContext.EndDialogAsync(null, cancellationToken);
            return await stepContext.BeginDialogAsync($"{nameof(AnythingElseDialog)}.AnythingElse", null, cancellationToken);


        }

        private async Task<DialogTurnResult> StepEndAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
           return await stepContext.EndDialogAsync(null, cancellationToken);
        }
        private static async Task<bool> ValidateChoice(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            bool isResponseYes = Constants.YesLibrary.Any(str => str.ToLower() == (promptContext.Recognized.Value.ToLower()));
            bool isResponseNo = Constants.NoLibrary.Any(str => str.ToLower() == (promptContext.Recognized.Value.ToLower()));
            if (isResponseYes)
                return true;
            else if (isResponseNo)
                return true;

            else
                return false;
        }
        private  async Task<bool> ValidateTime(PromptValidatorContext<IList<DateTimeResolution>> promptContext, CancellationToken cancellationToken)
        {

            // Get LUIS response from last dialog
            var recognizerResult = await _botServices.Dispatch.RecognizeAsync(promptContext.Context, cancellationToken);
            LuisModel luisResponse = JsonConvert.DeserializeObject<LuisModel>(JsonConvert.SerializeObject(recognizerResult));
            if (luisResponse.Entities.datetime != null)
            {
                bool isValidTime = luisResponse.Entities.datetime.Any(str => str.Type.ToLower() == (Constants.Date) || str.Type.ToLower() == (Constants.DateRange) ||
            str.Type.ToLower() == (Constants.DateTime) ||
            str.Type.ToLower() == (Constants.Duration) || str.Type.ToLower() == (Constants.DateTimeRange) || str.Type.ToLower() == (Constants.TimeRange));

                if (luisResponse.Entities.datetime[0].Expressions[0] == Constants.Now)
                {
                    return true;
                }
                else if (isValidTime)
                {
                    return false;
                }

                else
                {
                    return true;
                }
            }
            else
                return false;



        }

        private async Task<bool> ValidateDate(PromptValidatorContext<IList<DateTimeResolution>> promptContext, CancellationToken cancellationToken)
        {

            // Get LUIS response from last dialog
            var recognizerResult = await _botServices.Dispatch.RecognizeAsync(promptContext.Context, cancellationToken);
            LuisModel luisResponse = JsonConvert.DeserializeObject<LuisModel>(JsonConvert.SerializeObject(recognizerResult));
            if (luisResponse.Entities.datetime != null)
            {
                bool isValidDate = luisResponse.Entities.datetime.Any(str => str.Type.ToLower() == (Constants.Time) || str.Type.ToLower() == (Constants.DateTime) ||
                str.Type.ToLower() == (Constants.DateTimeRange) || str.Type.ToLower() == (Constants.TimeRange) || str.Type.ToLower() == (Constants.Date));
                if (isValidDate)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            else
                return false;

        }


        private async Task AddTaskStartTime(LuisModel luisResponse, UserProfile userProfile)
        {
            if (luisResponse.Entities._instance.datetime != null)
            {
                //UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
                if (luisResponse.Entities._instance.datetime[0].Type == "builtin.datetimeV2.time" || luisResponse.Entities._instance.datetime[0].Type == "builtin.datetimeV2.datetime")
                {
                    var remindMe = Microsoft.Recognizers.Text.DataTypes.TimexExpression.TimexResolver.Resolve
                 (new[] { luisResponse.Entities.datetime[0].Expressions[0] },
                     System.DateTime.Today);
                    string startT = string.Format(AriBotV4.Common.Constants.TaskTimeFormat, Convert.ToDateTime(remindMe.Values[0].Value));
                    if(userProfile.CreateTask.ContainsKey(Constants.StartTime))
                    {
                        userProfile.CreateTask.Remove(Constants.StartTime);
                    }
                    userProfile.CreateTask.Add(Constants.StartTime, startT);
                }
            }

        }
        private async Task AddTaskDates( LuisDateModel result, TimeZoneInfo timeInfo, UserProfile userProfile)
        {
            //UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());

           // TimeZoneInfo timeInfo = TZConvert.GetTimeZoneInfo(Convert.ToString(stepContext.Context.Activity.From.Properties[Constants.TaskSpurTimeZone]));
            string startDate = string.Empty;
            string startTime = string.Empty;
            string endDate = string.Empty;
            string endTime = string.Empty;
            DateTime start = new DateTime();
            DateTime end = new DateTime();
            
            if (result.entities.Any(ent => ent.type == "builtin.datetimeV2.date"))
            {
                DateTime sDate = new DateTime();
                var entity = result.entities.Find(x => x.type == "builtin.datetimeV2.date");

                if (entity.resolution.values.Count > 1)
                {
                     sDate = Convert.ToDateTime(JObject.Parse(Convert.ToString(entity.resolution.values[1]))["value"]);
                }
                else
                {
                     sDate = Convert.ToDateTime(JObject.Parse(Convert.ToString(entity.resolution.values[0]))["value"]);
                }
                //  DateTime utc = TimeZoneInfo.ConvertTimeToUtc(sDate);
                // start = TimeZoneInfo.ConvertTimeFromUtc(utc, timeInfo);
                start = sDate;
            }

            else if (result.entities.Any(ent => ent.type == "builtin.datetimeV2.datetime"))
            {
                var entity = result.entities.Find(x => x.type == "builtin.datetimeV2.datetime");


                DateTime sDate = Convert.ToDateTime(JObject.Parse(Convert.ToString(entity.resolution.values[0]))["value"]);
                //DateTime utc = TimeZoneInfo.ConvertTimeToUtc(sDate);
                //start = TimeZoneInfo.ConvertTimeFromUtc(utc, timeInfo);
                start = sDate;
                startTime = (string)userProfile.CreateTask[Constants.StartTime];


            }
            else if (result.entities.Any(ent => ent.type == "builtin.datetimeV2.duration"))
            {

                var entity = result.entities.Find(x => x.type == "builtin.datetimeV2.duration");
                DateTime sDate = DateTime.UtcNow.AddSeconds(Convert.ToDouble(JObject.Parse(Convert.ToString(entity.resolution.values[0]))["value"]));
                //DateTime utc = TimeZoneInfo.ConvertTimeToUtc(sDate);
                //start = TimeZoneInfo.ConvertTimeFromUtc(utc, timeInfo);
                start = sDate;


            }
            else if (result.entities.Any(ent => ent.type == "builtin.datetimeV2.daterange"))
            {
                var entity = result.entities.Find(x => x.type == "builtin.datetimeV2.daterange");
                DateTime sDate = Convert.ToDateTime(JObject.Parse(Convert.ToString(entity.resolution.values[0]))["start"]);
                DateTime eDate = Convert.ToDateTime(JObject.Parse(Convert.ToString(entity.resolution.values[0]))["end"]);
                //DateTime utcStart = TimeZoneInfo.ConvertTimeToUtc(sDate);
                //start = TimeZoneInfo.ConvertTimeFromUtc(utcStart, timeInfo);
                //DateTime utcEnd = TimeZoneInfo.ConvertTimeToUtc(eDate);
                //end = TimeZoneInfo.ConvertTimeFromUtc(utcEnd, timeInfo);
                start = sDate;
                end = eDate;



            }

            else if (result.entities.Any(ent => ent.type == "builtin.datetimeV2.datetimerange"))
            {
                var entity = result.entities.Find(x => x.type == "builtin.datetimeV2.datetimerange");
                DateTime sDate = Convert.ToDateTime(JObject.Parse(Convert.ToString(entity.resolution.values[0]))["start"]);
                DateTime eDate = Convert.ToDateTime(JObject.Parse(Convert.ToString(entity.resolution.values[0]))["end"]);
                //DateTime utcStart = TimeZoneInfo.ConvertTimeToUtc(sDate);
                //start = TimeZoneInfo.ConvertTimeFromUtc(utcStart, timeInfo);
                //DateTime utcEnd = TimeZoneInfo.ConvertTimeToUtc(eDate);
                //end = TimeZoneInfo.ConvertTimeFromUtc(utcEnd, timeInfo);
                start = sDate;
                end = eDate;



            }
            if (start == DateTime.MinValue && userProfile.CreateTask.ContainsKey(Constants.StartTime))
            {
                
                DateTime localDate = DateTime.UtcNow;
                DateTime utcTime = localDate.ToUniversalTime();
                // Get time info for user timezone
                // Convert time to UTC
                DateTime userDateTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, timeInfo);
                if (Convert.ToDateTime(string.Format(AriBotV4.Common.Constants.TaskTimeFormat, userDateTime)).Hour <  Convert.ToDateTime(string.Format(AriBotV4.Common.Constants.TaskTimeFormat,userProfile.CreateTask[Constants.StartTime])).Hour)
                {
                    start = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, timeInfo.Id);
                }
                else
                {
                    start = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow.AddDays(1), timeInfo.Id);
                }

            }

            if (end != DateTime.MinValue && userProfile.CreateTask.ContainsKey(Constants.StartTime))
            {
                endDate = start.ToString(AriBotV4.Common.Constants.CreateTaskDate).Replace("-", "/");
                userProfile.CreateTask.Add(Constants.EndDate, endDate);
                endTime = string.Format(AriBotV4.Common.Constants.TaskTimeFormat, end);
            }
            if (start != DateTime.MinValue)
            {
                startDate = start.ToString(AriBotV4.Common.Constants.CreateTaskDate).Replace("-", "/");
                userProfile.CreateTask.Add(Constants.StartDate, startDate);
            }
        }

        
        #endregion
    }
}
