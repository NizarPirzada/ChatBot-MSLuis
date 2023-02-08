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
    public class ScheduleTaskDialog : ComponentDialog
    {
        #region Properties and Fields
        private static BotStateService _botStateService;
        private readonly BotServices _botServices;
        private readonly IConfiguration _configuration;
        private static AzureKeyCredential credentials;
        private static Uri endpoint;

        #endregion



        #region WaterFallSteps and Dialogs
        public ScheduleTaskDialog(string dialogId, BotStateService botStateService, BotServices botServices) : base(dialogId)
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
                ConfirmDate,
                AskTaskReminder,
                AskTaskReminderTime,
              CreateTask,
              StepEndAsync
            };

            AddDialog(new WaterfallDialog($"{nameof(ScheduleTaskDialog)}.mainFlow", waterfallSteps));
           //  AddDialog(new ConfirmPrompt($"{nameof(ScheduleTaskDialog)}.remindMe"));
            AddDialog(new TextPrompt($"{nameof(ScheduleTaskDialog)}.remindMe", ValidateChoice));
            AddDialog(new DateTimePrompt($"{nameof(ScheduleTaskDialog)}.startDate", ValidateDate));
            AddDialog(new TextPrompt($"{nameof(ScheduleTaskDialog)}.remindMeTime"));
            InitialDialogId = $"{nameof(ScheduleTaskDialog)}.mainFlow";
        }

        public async Task<DialogTurnResult> ConfirmDate(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {


            LuisModel luisResponse = (LuisModel)stepContext.ActiveDialog.State["options"];
            var result = JsonConvert.DeserializeObject<LuisDateModel>(JsonConvert.SerializeObject(luisResponse.Properties["luisResult"]));
            if (!result.entities.Any(s => s.type.Contains("builtin.datetimeV2.datetime") || s.type.Contains("builtin.datetimeV2.date") || s.type.Contains("builtin.datetimeV2.duration")))
            {
                var prompt = new PromptOptions
                {
                    Prompt = MessageFactory.Text(TaskSpur.Resources.TaskSpur.TaskStartDate),
                    RetryPrompt = MessageFactory.Text(TaskSpur.Resources.TaskSpur.TaskInvalidDate),
                    Choices = ChoiceFactory.ToChoices(AriBotV4.Common.Constants.TaskType),
                    Style = ListStyle.List,

                };
                return await stepContext.PromptAsync($"{nameof(ScheduleTaskDialog)}.startDate", prompt, cancellationToken);
            }
            else
            {
                return await stepContext.NextAsync(stepContext.Context.Activity.Text, cancellationToken);
            }
        }

        public async Task<DialogTurnResult> AskTaskReminder(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            string startDate = string.Empty;
            string startTime = string.Empty;
            string endDate = string.Empty;
            string endTime = string.Empty;
            DateTime start = new DateTime();
            DateTime end = new DateTime();
            
            //LuisModel luisResponse = (LuisModel)stepContext.ActiveDialog.State["options"];
            RecognizerResult recognizerResult = await _botServices.Dispatch.RecognizeAsync(stepContext.Context, cancellationToken);
            LuisModel luisResponse = JsonConvert.DeserializeObject<LuisModel>(JsonConvert.SerializeObject(recognizerResult));
            var result = JsonConvert.DeserializeObject<LuisDateModel>(JsonConvert.SerializeObject(luisResponse.Properties["luisResult"]));
            TimeZoneInfo timeInfo = TZConvert.GetTimeZoneInfo(Convert.ToString(stepContext.Context.Activity.From.Properties[Constants.TaskSpurTimeZone]));
            TimeZoneInfo cstZone = TimeZoneInfo.FindSystemTimeZoneById(timeInfo.Id);

            UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
            userProfile.CreateTask = new Dictionary<string, object>();


            foreach (var entity in result.entities)
            {
                if (entity.type == "builtin.datetimeV2.date" || entity.type == "builtin.datetimeV2.datetime")
                {


                    DateTime sDate = Convert.ToDateTime(JObject.Parse(Convert.ToString(entity.resolution.values[0]))["value"]);
                    DateTime utc = TimeZoneInfo.ConvertTimeToUtc(sDate);
                    start = TimeZoneInfo.ConvertTimeFromUtc(utc, timeInfo);

                    if (entity.type == "builtin.datetimeV2.datetime")
                        startTime = string.Format(AriBotV4.Common.Constants.TaskTimeFormat, Convert.ToDateTime(JObject.Parse(Convert.ToString(entity.resolution.values[0]))["value"]));

                }
                else if (entity.type == "builtin.datetimeV2.duration")
                {
                    DateTime sDate = DateTime.UtcNow.AddSeconds(Convert.ToDouble(JObject.Parse(Convert.ToString(entity.resolution.values[0]))["value"]));
                    DateTime utc = TimeZoneInfo.ConvertTimeToUtc(sDate);
                    start = TimeZoneInfo.ConvertTimeFromUtc(utc, timeInfo);
                    //DateTime utc = DateTime.SpecifyKind(DateTime.UtcNow.AddSeconds(Convert.ToDouble(JObject.Parse(Convert.ToString(entity.resolution.values[0]))["value"])).ToUniversalTime(), DateTimeKind.Unspecified);
                    //start = TimeZoneInfo.ConvertTimeFromUtc(utc, cstZone);
                    //start = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow.AddSeconds(Convert.ToDouble(JObject.Parse(Convert.ToString(entity.resolution.values[0]))["value"])),timeInfo.Id);
                }
                else if (entity.type == "builtin.datetimeV2.daterange" || entity.type == "builtin.datetimeV2.datetimerange")
                {
                    DateTime sDate = Convert.ToDateTime(JObject.Parse(Convert.ToString(entity.resolution.values[0]))["start"]);
                    DateTime eDate = Convert.ToDateTime(JObject.Parse(Convert.ToString(entity.resolution.values[0]))["end"]);
                    DateTime utcStart = TimeZoneInfo.ConvertTimeToUtc(sDate);
                    start = TimeZoneInfo.ConvertTimeFromUtc(utcStart, timeInfo);
                    DateTime utcEnd = TimeZoneInfo.ConvertTimeToUtc(eDate);
                    end = TimeZoneInfo.ConvertTimeFromUtc(utcEnd, timeInfo);
                    //DateTime utcStart = DateTime.SpecifyKind(Convert.ToDateTime(JObject.Parse(Convert.ToString(entity.resolution.values[0]))["start"]).ToUniversalTime(), DateTimeKind.Unspecified);
                    //DateTime utcEnd = DateTime.SpecifyKind(Convert.ToDateTime(JObject.Parse(Convert.ToString(entity.resolution.values[0]))["end"]).ToUniversalTime(), DateTimeKind.Unspecified);
                    //start = TimeZoneInfo.ConvertTimeFromUtc(utcStart, cstZone);
                    //end = TimeZoneInfo.ConvertTimeFromUtc(utcEnd, cstZone);

                }


                if (entity.type == "builtin.datetimeV2.time")
                {
                    startTime = string.Format(AriBotV4.Common.Constants.TaskTimeFormat, Convert.ToDateTime(JObject.Parse(Convert.ToString(entity.resolution.values[0]))["value"]));
                    if (start == DateTime.MinValue)
                        start = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, timeInfo.Id);
                    if (end != DateTime.MinValue)
                    {
                        endTime = string.Format(AriBotV4.Common.Constants.TaskTimeFormat, end);
                    }
                }

            }

            //UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());

            if (start != DateTime.MinValue)
                startDate = start.ToString(AriBotV4.Common.Constants.CreateTaskDate).Replace("-", "/");
            if (end != DateTime.MinValue)
            {
                endDate = end.ToString(AriBotV4.Common.Constants.CreateTaskDate).Replace("-", "/");
            }

            if (userProfile.CreateTask.ContainsKey(Constants.StartDate))
                    userProfile.CreateTask[Constants.StartDate] = startDate;

                else
                    userProfile.CreateTask.Add(Constants.StartDate, startDate);

            if (userProfile.CreateTask.ContainsKey(Constants.StartTime))
                userProfile.CreateTask[Constants.StartTime] = startTime;

            else
                userProfile.CreateTask.Add(Constants.StartTime, startTime);


            if (userProfile.CreateTask.ContainsKey(Constants.EndDate))
                userProfile.CreateTask[Constants.EndDate] = endDate;

            else
                userProfile.CreateTask.Add(Constants.EndDate, endDate);


            if (userProfile.CreateTask.ContainsKey(Constants.EndTime))
                userProfile.CreateTask[Constants.EndTime] = endTime;

            else
                userProfile.CreateTask.Add(Constants.EndTime, endTime);

            await _botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);


            

           
            
            if (result.entities.Any(s => s.type.Contains("builtin.datetimeV2.datetime") || s.type.Contains("builtin.datetimeV2.time")))
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
                return await stepContext.PromptAsync($"{nameof(ScheduleTaskDialog)}.remindMe", opts);
            }
            else
            {
                return await stepContext.NextAsync(stepContext.Context.Activity.Text, cancellationToken);
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
                if (isResponseYes)
                {
                    
                    if (!userProfile.CreateTask.ContainsKey(Constants.RemindMe))
                    {
                        userProfile.CreateTask.Add(Constants.RemindMe, true);
                        await _botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);
                    }
                    return await stepContext.PromptAsync($"{nameof(ScheduleTaskDialog)}.remindMeTime",
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
            else if(isResponseNo)
            {
                return await stepContext.NextAsync(stepContext.Context.Activity.Text, cancellationToken);
            }
            else
            {
                return await stepContext.NextAsync(stepContext.Context.Activity.Text, cancellationToken);
            }
        }

         

private async Task<DialogTurnResult> CreateTask(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
         
            UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
            CreateTaskRequest createTaskRequest = new CreateTaskRequest();
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
                    stepContext.ActiveDialog.State["stepIndex"] = (int)stepContext.ActiveDialog.State["stepIndex"] - 1;
                    return await AskTaskReminderTime(stepContext, cancellationToken);
                }

            }
            LuisModel luisResponse = (LuisModel)stepContext.ActiveDialog.State["options"];
           
            if ((LuisModel)stepContext.ActiveDialog.State["options"] != null)

            {
               
                if (!userProfile.CreateTask.ContainsKey(Constants.StartDate))
                {
                    string startDate = string.Empty;
                    string startTime = string.Empty;
                    string endDate = string.Empty;
                    string endTime = string.Empty;
                    DateTime start = new DateTime();
                    DateTime end = new DateTime();
                    TimeZoneInfo timeInfo = TZConvert.GetTimeZoneInfo(Convert.ToString(stepContext.Context.Activity.From.Properties[Constants.TaskSpurTimeZone]));
                    TimeZoneInfo cstZone = TimeZoneInfo.FindSystemTimeZoneById(timeInfo.Id);
                    
                    var result = JsonConvert.DeserializeObject<LuisDateModel>(JsonConvert.SerializeObject(luisResponse.Properties["luisResult"]));
                    
                    foreach (var entity in result.entities)
                    {
                        if (entity.type == "builtin.datetimeV2.date" || entity.type == "builtin.datetimeV2.datetime")
                        {


                            DateTime utc = DateTime.SpecifyKind(Convert.ToDateTime(JObject.Parse(Convert.ToString(entity.resolution.values[0]))["value"]).ToUniversalTime(), DateTimeKind.Unspecified);
                            start = TimeZoneInfo.ConvertTimeFromUtc(utc, cstZone);

                            if (entity.type == "builtin.datetimeV2.datetime")
                                startTime = string.Format(AriBotV4.Common.Constants.TaskTimeFormat, Convert.ToDateTime(JObject.Parse(Convert.ToString(entity.resolution.values[0]))["value"]));

                        }
                        else if (entity.type == "builtin.datetimeV2.duration")
                        {
                            DateTime utc = DateTime.SpecifyKind(DateTime.UtcNow.AddSeconds(Convert.ToDouble(JObject.Parse(Convert.ToString(entity.resolution.values[0]))["value"])).ToUniversalTime(), DateTimeKind.Unspecified);
                            start = TimeZoneInfo.ConvertTimeFromUtc(utc, cstZone);
                            //start = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow.AddSeconds(Convert.ToDouble(JObject.Parse(Convert.ToString(entity.resolution.values[0]))["value"])),timeInfo.Id);
                        }
                        else if (entity.type == "builtin.datetimeV2.daterange" || entity.type == "builtin.datetimeV2.datetimerange")
                        {
                            DateTime utcStart = DateTime.SpecifyKind(Convert.ToDateTime(JObject.Parse(Convert.ToString(entity.resolution.values[0]))["start"]).ToUniversalTime(), DateTimeKind.Unspecified);
                            DateTime utcEnd = DateTime.SpecifyKind(Convert.ToDateTime(JObject.Parse(Convert.ToString(entity.resolution.values[0]))["end"]).ToUniversalTime(), DateTimeKind.Unspecified);
                            start = TimeZoneInfo.ConvertTimeFromUtc(utcStart, cstZone);
                            end = TimeZoneInfo.ConvertTimeFromUtc(utcEnd, cstZone);

                        }


                        if (entity.type == "builtin.datetimeV2.time")
                        {
                            startTime = string.Format(AriBotV4.Common.Constants.TaskTimeFormat, Convert.ToDateTime(JObject.Parse(Convert.ToString(entity.resolution.values[0]))["value"]));
                            if (start == DateTime.MinValue)
                                start = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, timeInfo.Id);
                            if (end != DateTime.MinValue)
                            {
                                endTime = string.Format(AriBotV4.Common.Constants.TaskTimeFormat, end);
                            }
                        }


                    }


                    if (start != DateTime.MinValue)
                        startDate = start.ToString(AriBotV4.Common.Constants.CreateTaskDate).Replace("-", "/");
                    if (end != DateTime.MinValue)
                    {
                        endDate = end.ToString(AriBotV4.Common.Constants.CreateTaskDate).Replace("-", "/");
                    }


                    if (userProfile.CreateTask.ContainsKey(Constants.StartDate))
                        userProfile.CreateTask[Constants.StartDate] = start;

                    else
                        userProfile.CreateTask.Add(Constants.StartDate, start);

                    if (userProfile.CreateTask.ContainsKey(Constants.StartTime))
                        userProfile.CreateTask[Constants.StartTime] = startTime;

                    else
                        userProfile.CreateTask.Add(Constants.StartTime, startTime);


                    if (userProfile.CreateTask.ContainsKey(Constants.EndDate))
                        userProfile.CreateTask[Constants.EndDate] = end;

                    else
                        userProfile.CreateTask.Add(Constants.EndDate, end);


                    if (userProfile.CreateTask.ContainsKey(Constants.EndTime))
                        userProfile.CreateTask[Constants.EndTime] = endTime;

                    else
                        userProfile.CreateTask.Add(Constants.EndTime, endTime);

                    
                }
                //get task name
                var client = new TextAnalyticsClient(endpoint, credentials);
                    Task<List<string>> keywords = Utility.KeyPhraseExtractionExample(client, (string)luisResponse.Text.Replace("create", "").Replace("task","").Replace("tasks",""));

                
                createTaskRequest.name = keywords.Result[0];
                createTaskRequest.description = keywords.Result[0];
                if (luisResponse.Entities._instance.priority != null)
                {
                    AriBotV4.Enums.PriorityEnum Priority = EnumHelpers.GetValueFromDescription<AriBotV4.Enums.PriorityEnum>(luisResponse.Entities._instance.priority[0].Text);
                    createTaskRequest.priorityId = (int)Priority;
             
                }
                else
                {
                    createTaskRequest.priorityId = 1;
                }
                

                if (userProfile.CreateTask != null && userProfile.CreateTask.ContainsKey(Constants.RemindMe))
                    createTaskRequest.remindMe = (bool)userProfile.CreateTask[Constants.RemindMe];
                if (userProfile.CreateTask != null && userProfile.CreateTask.ContainsKey(Constants.ReminderTimeId))
                    createTaskRequest.reminderTimeId = (int)userProfile.CreateTask[Constants.ReminderTimeId];
                if (userProfile.CreateTask != null && userProfile.CreateTask.ContainsKey(Constants.ReminderTime))
                    createTaskRequest.reminderTime = (int)userProfile.CreateTask[Constants.ReminderTime];

                // get goal id
                string category = string.Empty;
                if (luisResponse.Entities._instance.category != null)
                {
                    category = luisResponse.Entities._instance.category[0].Text;
                    //Enum Category = EnumHelpers.GetValueFromDescription<AriBotV4.Enums.GoalCategoryEnum>(luisResponse.Entities._instance.category[0].Text);
                    //    //Enum.Parse<AriBotV4.Enums.GoalCategoryEnum>(luisResponse.Entities._instance.category[0].Text);
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
                //        (refreshTokenResponse.data.token,Constants.PersonalLifeGoal);
                //        // Update users's from property
                //        await _botStateService._taskSpurApiClient.UpdateToken(stepContext, refreshTokenResponse);
                //    }




                //}
                if(goalResponse != null)
                createTaskRequest.goalId = goalResponse.data.data[0].id;



                createTaskRequest.startDate = Convert.ToString(userProfile.CreateTask[Constants.StartDate]);
                if (!string.IsNullOrEmpty(Convert.ToString(userProfile.CreateTask[Constants.EndDate])))
                    createTaskRequest.endDate = Convert.ToString(userProfile.CreateTask[Constants.EndDate]);
                if (!string.IsNullOrEmpty((string)userProfile.CreateTask[Constants.StartTime]))
                {
                    createTaskRequest.startTime = (string)userProfile.CreateTask[Constants.StartTime];



                }
                if (!string.IsNullOrEmpty((string)userProfile.CreateTask[Constants.EndTime]))
                {
                    createTaskRequest.endTime = (string)userProfile.CreateTask[Constants.EndTime];

                }


                createTaskRequest.timeZone = Convert.ToString(stepContext.Context.Activity.From.Properties[AriBotV4.Common.Constants.TaskSpurTimeZone]);

                CreateTaskResponse response = await _botStateService._taskSpurApiClient.CreateTask(createTaskRequest,
               Convert.ToString(stepContext.Context.Activity.From.Properties[AriBotV4.Common.Constants.TaskSpurToken]));

                //// Check token authrozation
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
                if(response != null)
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(response.toast.message));
                }
                else
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(SharedStrings.Sorry), cancellationToken);

                }



            }
            userProfile.CreateTask = null;
            return await stepContext.BeginDialogAsync($"{nameof(AnythingElseDialog)}.AnythingElse", null, cancellationToken);


        }

        private async Task<DialogTurnResult> StepEndAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
            userProfile.LastMessageReceived = DateTime.UtcNow;
            await _botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);

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

        private async Task<bool> ValidateDate(PromptValidatorContext<IList<DateTimeResolution>> promptContext, CancellationToken cancellationToken)
        {

            // Get LUIS response from last dialog
            var recognizerResult = await _botServices.Dispatch.RecognizeAsync(promptContext.Context, cancellationToken);
            LuisModel luisResponse = JsonConvert.DeserializeObject<LuisModel>(JsonConvert.SerializeObject(recognizerResult));
            if (luisResponse.Entities.datetime != null)
            {
                bool isValidDate = luisResponse.Entities.datetime.Any(str => str.Type.ToLower() == (Constants.Time) || str.Type.ToLower() == (Constants.DateTime) ||
                str.Type.ToLower() == (Constants.DateTimeRange) || str.Type.ToLower() == (Constants.TimeRange) || str.Type.ToLower() == (Constants.Date) || str.Type.ToLower() == (Constants.Duration));
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
        #endregion
    }
}
