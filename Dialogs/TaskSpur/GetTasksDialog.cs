using AriBotV4.Common;
using AriBotV4.Dialogs.Common.Resources;
using AriBotV4.Enums;
using AriBotV4.Models;
using AriBotV4.Models.TaskSpur;
using AriBotV4.Models.TaskSpur.Tasks;
using AriBotV4.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using TimeZoneConverter;

namespace AriBotV4.Dialogs.TaskSpur
{
    public class GetTasksDialog : ComponentDialog
    {
        #region Properties and Fields
        private readonly BotStateService _botStateService;
        private readonly BotServices _botServices;

        private LuisModel luisResponse;


        #endregion
        public GetTasksDialog(string dialogId, BotStateService botStateService, BotServices botServices) : base(dialogId)
        {
            _botStateService = botStateService ?? throw new System.ArgumentNullException(nameof(botStateService));
            _botServices = botServices ?? throw new System.ArgumentNullException(nameof(botStateService));
            InitializeWaterfallDialog();
        }
        private void InitializeWaterfallDialog()
        {
            // Create Waterfall Steps
            var waterfallSteps = new WaterfallStep[]
            {
       //         AskUserInput,
                GetTasks,
                FinalAsync

            };

            // Add Named Dialogs
            AddDialog(new WaterfallDialog($"{nameof(GetTasksDialog)}.mainFlow", waterfallSteps));
            AddDialog(new TextPrompt($"{nameof(GetTasksDialog)}.userInput"));
            AddDialog(new CommonDialog($"{nameof(CommonDialog)}.mainFlow", _botStateService));

            // Set the starting Dialog
            InitialDialogId = $"{nameof(GetTasksDialog)}.mainFlow";
        }



   

        private async Task<DialogTurnResult> GetTasks(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

           
            if (string.IsNullOrEmpty(Convert.ToString(stepContext.Context.Activity.From.Properties[Constants.TaskSpurToken])))
            {
                await stepContext.EndDialogAsync(null, cancellationToken);
                return await stepContext.BeginDialogAsync($"{nameof(CommonDialog)}.mainFlow", null, cancellationToken);
            }
            else
            {


                RecognizerResult recognizerResult = new RecognizerResult();
                // First, we use the dispatch model to determine which cognitive service (LUIS or Qna) to use
                try
                {
                    recognizerResult = await _botServices.Dispatch.RecognizeAsync(stepContext.Context, cancellationToken);
                    luisResponse = JsonConvert.DeserializeObject<LuisModel>(JsonConvert.SerializeObject(recognizerResult));
                }
                catch (Exception ex)
                {

                }
                LuisModel.Intent topIntent = new LuisModel.Intent();
                GetTasksRequest getTasks = new GetTasksRequest();
                // Top intent tell us which cognitive service to use
                if (luisResponse != null)
                    topIntent = luisResponse.TopIntent().intent;

                //To continue, must be time Intent
                // if (Convert.ToString(topIntent) == Constants.Get_Tasks_Intent)
                //if (luisResponse.Entities._instance.tasks != null || luisResponse.Entities._instance.appointment != null
                //    || luisResponse.Entities._instance.next != null || luisResponse.Entities._instance.meeting != null)
                //{
                UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
                TimeZoneInfo timeInfo = TZConvert.GetTimeZoneInfo(Convert.ToString(stepContext.Context.Activity.From.Properties[Constants.TaskSpurTimeZone]));
                var result = JsonConvert.DeserializeObject<LuisDateModel>(JsonConvert.SerializeObject(luisResponse.Properties["luisResult"]));

                if (luisResponse.Entities._instance.Status != null)
                {
                    AriBotV4.Enums.StatusEnum status = EnumHelpers.GetValueFromDescription<AriBotV4.Enums.StatusEnum>(luisResponse.Entities._instance.Status[0].Text);
                    getTasks.Status = (int)status;


                }

                if (luisResponse.Entities.datetime != null)
                    {

                        // Convert LUIS date expression to DateTime
                        if (luisResponse.Entities.datetime[0].Expressions[0] == AriBotV4.Common.Constants.Now)
                        {
                            getTasks.StartDate = Convert.ToString(DateTime.UtcNow);
                            getTasks.EndDate = Convert.ToString(DateTime.UtcNow);
                        }
                        else
                        {

                        AddTaskStartTime(luisResponse, getTasks);
                        AddTaskDates(result, timeInfo, getTasks);
                        //    var datetimes = Microsoft.Recognizers.Text.DataTypes.TimexExpression.TimexResolver.Resolve
                        //    (new[] { luisResponse.Entities.datetime[0].Expressions[0] },
                        //        System.DateTime.Today);
                        //    DateTime startDate;
                        //string startT = string.Format(AriBotV4.Common.Constants.TaskTimeFormat, Convert.ToDateTime(datetimes.Values[0].Value));
                        //if(!string.IsNullOrEmpty(startT))
                        //{
                        //    getTasks.StartTime = startT;
                        //}
                        //if (datetimes.Values != null && datetimes.Values.Count > 0)
                        //{
                        //    int count = datetimes.Values.Count;

                        //if (datetimes.Values[0].Type == "time")
                        //{
                        //    getTasks.StartTime = string.Format(AriBotV4.Common.Constants.TaskTimeFormat, Convert.ToDateTime(datetimes.Values[0].Value));
                        //}
                        //if (datetimes.Values[0].Start != null)
                        //    {
                        //        var parsed = new Microsoft.Recognizers.Text.DataTypes.TimexExpression.TimexProperty(datetimes.Values[0].Timex);
                        //        if (count == 1 && parsed.WeekOfYear != null)
                        //        {
                        //            getTasks.StartDate = Convert.ToString(Convert.ToDateTime(datetimes.Values[0].Start).AddDays(7));
                        //            getTasks.EndDate = Convert.ToString(Convert.ToDateTime(datetimes.Values[0].End).AddDays(7));

                        //        }
                        //        else
                        //        {
                        //            getTasks.StartDate = Convert.ToString(Convert.ToDateTime(datetimes.Values[count - 1].Start));
                        //            getTasks.EndDate = Convert.ToString(Convert.ToDateTime(datetimes.Values[count - 1].End));
                        //        }
                        //        //getTasks.EndDate = Convert.ToString(Convert.ToDateTime(datetimes.Values[count - 1].Start));
                        //        //getTasks.StartDate = Convert.ToString(Convert.ToDateTime(datetimes.Values[count - 1].End));
                        //    }

                        //    else if (DateTime.TryParse(datetimes.Values[count - 1].Value, out startDate))
                        //    {
                        //        getTasks.StartDate = Convert.ToString(Convert.ToDateTime(datetimes.Values[count - 1].Value));
                        //        getTasks.EndDate = Convert.ToString(Convert.ToDateTime(datetimes.Values[count - 1].Value));
                        //    }
                        //    else
                        //    {
                        //        getTasks.StartDate = Convert.ToString(DateTime.Now.AddSeconds(Convert.ToDouble(datetimes.Values[0].Value)));
                        //        getTasks.EndDate = Convert.ToString(DateTime.Now.AddSeconds(Convert.ToDouble(datetimes.Values[0].Value)));
                        //    }
                        //}

                    }

                    }
                    else if (getTasks.Status == 0 && luisResponse.Entities._instance.category == null && luisResponse.Entities._instance.priority == null)
                    {
                        getTasks.StartDate = Convert.ToString(DateTime.UtcNow);
                        getTasks.EndDate = Convert.ToString(DateTime.UtcNow);
                        //weatherDate = DateTime.UtcNow;
                    }
                    
                    getTasks.StartDate = Convert.ToDateTime(getTasks.StartDate).ToString("yyyy-MM-dd");
                    if(!string.IsNullOrEmpty(getTasks.EndDate))
                    getTasks.EndDate = Convert.ToDateTime(getTasks.EndDate).ToString("yyyy-MM-dd");
                

                    if (luisResponse.Entities._instance.appointment != null || luisResponse.Entities._instance.appointments != null)
                    {
                        getTasks.IsAppointment = true;
                    }
                    if (luisResponse.Entities._instance.next != null || (luisResponse.Entities._instance.appointment != null)
                    || (luisResponse.Entities._instance.meeting != null) || (luisResponse.Entities._instance.task != null)
                    )
                    {
                        getTasks.PageSize = 1;
                    }
                    if (luisResponse.Entities._instance.priority != null)
                    {
                    AriBotV4.Enums.PriorityEnum priority = EnumHelpers.GetValueFromDescription<AriBotV4.Enums.PriorityEnum>(luisResponse.Entities._instance.priority[0].Text);
                    getTasks.Priority = (int)priority;


                    }
                if (luisResponse.Entities._instance.Status != null)
                {
                    AriBotV4.Enums.StatusEnum status = EnumHelpers.GetValueFromDescription<AriBotV4.Enums.StatusEnum>(luisResponse.Entities._instance.Status[0].Text);
                    getTasks.Status = (int)status;
                   

                }
                if (luisResponse.Entities._instance.category != null)
                    {
                    AriBotV4.Enums.GoalCategoryEnum category = EnumHelpers.GetValueFromDescription<AriBotV4.Enums.GoalCategoryEnum>(luisResponse.Entities._instance.category[0].Text);
                    getTasks.Category = (int)category;
                    }

                    // Call create goal api
                    GetAriTasksResponse response = await _botStateService._taskSpurApiClient.GetTasks(getTasks,
                        Convert.ToString(stepContext.Context.Activity.From.Properties[Constants.TaskSpurToken]));
                    // Check token authrozation
                    
                    if (response.count != 0)
                    {




                        // Create reply
                        var reply = stepContext.Context.Activity.CreateReply();
                        if (response.count > 0)
                        {
                            

                            //if (Convert.ToDateTime(getTasks.StartDate) == DateTime.Today)
                            //{
                            for (int i = 0; i <= response.activities[0].attachments.Count - 1; i++)
                            {
                                reply.Attachments.Add(DisplayTasks(response.activities[0].attachments[i].content));

                            }

                            //    if (luisResponse.Entities._instance.next == null || (luisResponse.Entities._instance.next != null && reply.Attachments.Count == 0))
                            //    {
                            //        for (int i = 0; i <= response.data.data[0].toDoTasks.Count - 1; i++)
                            //        {
                            //            reply.Attachments.Add(DisplayTasks(response.data.data[0].toDoTasks[i]));
                            //        }

                            //    }
                            //    if (!string.IsNullOrEmpty(getTasks.EndDate))
                            //    {
                            //        for (int i = 0; i <= response.data.data[0].laterTasks.Count - 1; i++)
                            //        {
                            //            reply.Attachments.Add(DisplayTasks(response.data.data[0].laterTasks[i]));

                            //        }
                            //    }

                            //}


                            //else
                            //{

                            //    for (int i = 0; i <= response.data.data[0].doingTasks.Count - 1; i++)
                            //    {
                            //        reply.Attachments.Add(DisplayTasks(response.data.data[0].doingTasks[i]));

                            //    }
                            //    for (int i = 0; i <= response.data.data[0].archivedTasks.Count - 1; i++)
                            //    {
                            //        reply.Attachments.Add(DisplayTasks(response.data.data[0].archivedTasks[i]));

                            //    }

                            //    for (int i = 0; i <= response.data.data[0].doneTasks.Count - 1; i++)
                            //    {
                            //        reply.Attachments.Add(DisplayTasks(response.data.data[0].doneTasks[i]));

                            //    }

                            //    for (int i = 0; i <= response.data.data[0].toDoTasks.Count - 1; i++)
                            //    {
                            //        reply.Attachments.Add(DisplayTasks(response.data.data[0].toDoTasks[i]));

                            //    }



                            //    for (int i = 0; i <= response.data.data[0].laterTasks.Count - 1; i++)
                            //    {
                            //        reply.Attachments.Add(DisplayTasks(response.data.data[0].laterTasks[i]));

                            //    }
                            //}
                            if (reply.Attachments.Count == 0)
                            {
                                await stepContext.Context.SendActivityAsync(MessageFactory.Text(Constants.NoTasksFound));
                            }
                            else
                            {
                                var entity = new Microsoft.Bot.Schema.Entity();
                                entity.SetAs(new Mention()
                                {
                                    
                                    Mentioned = new ChannelAccount()
                                    {
                                        Id = Convert.ToString(response.count),
                                        Name = response.url,
                                        Role = "Tasks"
                                    }
                                });
                                reply.Entities.Add(entity);
                                

                                await stepContext.Context.SendActivityAsync(reply, cancellationToken);
                            }
                            //await stepContext.Context.SendActivityAsync(MessageFactory.Text(response.toast.message));
                        }

                    }
                    else
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text(Constants.NoTasksFound));





                //}
                //else
                //{
                //    await stepContext.Context.SendActivityAsync(MessageFactory.Text(SharedStrings.Sorry), cancellationToken);
                //}


                return await stepContext.NextAsync(stepContext, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> FinalAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            await stepContext.EndDialogAsync(null, cancellationToken);
            return await stepContext.BeginDialogAsync($"{nameof(AnythingElseDialog)}.AnythingElse",null, cancellationToken);
        }

        private static Microsoft.Bot.Schema.Attachment DisplayTasks(Object data)
        {
            var heroCard = new HeroCard()
            {
                Title = Convert.ToString(data.GetType().GetProperty("title")?.GetValue(data, null)),
                Subtitle = Convert.ToString(data.GetType().GetProperty("subtitle")?.GetValue(data, null)),
                Text = Convert.ToString(data.GetType().GetProperty("text")?.GetValue(data, null))
                //  Text = data.priority

            };

            return heroCard.ToAttachment();
        }

        private async Task AddTaskStartTime(LuisModel luisResponse, GetTasksRequest getTasks)
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
                    //if (userProfile.CreateTask.ContainsKey(Constants.StartTime))
                    //{
                    //    userProfile.CreateTask.Remove(Constants.StartTime);
                    //}
                    //userProfile.CreateTask.Add(Constants.StartTime, startT);
                    getTasks.StartTime = startT;
                }
            }

        }
        private async Task AddTaskDates(LuisDateModel result, TimeZoneInfo timeInfo, GetTasksRequest getTasks)
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
                DateTime eDate = new DateTime();
                var entity = result.entities.Find(x => x.type == "builtin.datetimeV2.date");

                if (entity.resolution.values.Count > 1)
                {
                    sDate = Convert.ToDateTime(JObject.Parse(Convert.ToString(entity.resolution.values[1]))["value"]);
                    eDate = Convert.ToDateTime(JObject.Parse(Convert.ToString(entity.resolution.values[1]))["value"]);
                }
                else
                {
                    sDate = Convert.ToDateTime(JObject.Parse(Convert.ToString(entity.resolution.values[0]))["value"]);
                    eDate = Convert.ToDateTime(JObject.Parse(Convert.ToString(entity.resolution.values[0]))["value"]);
                }
                //  DateTime utc = TimeZoneInfo.ConvertTimeToUtc(sDate);
                // start = TimeZoneInfo.ConvertTimeFromUtc(utc, timeInfo);
                start = sDate;
                end = eDate;
            }

            else if (result.entities.Any(ent => ent.type == "builtin.datetimeV2.datetime"))
            {
                var entity = result.entities.Find(x => x.type == "builtin.datetimeV2.datetime");
                DateTime sDate = new DateTime();
                DateTime eDate = new DateTime();
                if (entity.resolution.values.Count > 1)
                {
                     sDate = Convert.ToDateTime(JObject.Parse(Convert.ToString(entity.resolution.values[1]))["value"]);
                     eDate = Convert.ToDateTime(JObject.Parse(Convert.ToString(entity.resolution.values[1]))["value"]);
                }
                else
                {
                     sDate = Convert.ToDateTime(JObject.Parse(Convert.ToString(entity.resolution.values[0]))["value"]);
                     eDate = Convert.ToDateTime(JObject.Parse(Convert.ToString(entity.resolution.values[0]))["value"]);
                }
                
                //DateTime utc = TimeZoneInfo.ConvertTimeToUtc(sDate);
                //start = TimeZoneInfo.ConvertTimeFromUtc(utc, timeInfo);
                start = sDate;
                end = eDate;
              //  startTime = (string)userProfile.CreateTask[Constants.StartTime];


            }
            else if (result.entities.Any(ent => ent.type == "builtin.datetimeV2.duration"))
            {

                var entity = result.entities.Find(x => x.type == "builtin.datetimeV2.duration");
                DateTime sDate = DateTime.UtcNow.AddSeconds(Convert.ToDouble(JObject.Parse(Convert.ToString(entity.resolution.values[0]))["value"]));
                DateTime eDate = DateTime.UtcNow.AddSeconds(Convert.ToDouble(JObject.Parse(Convert.ToString(entity.resolution.values[0]))["value"]));
                //DateTime utc = TimeZoneInfo.ConvertTimeToUtc(sDate);
                //start = TimeZoneInfo.ConvertTimeFromUtc(utc, timeInfo);
                start = sDate;
                end = eDate;


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
            if (start == DateTime.MinValue && !string.IsNullOrEmpty(getTasks.StartTime))
            {

                DateTime localDate = DateTime.UtcNow;
                DateTime utcTime = localDate.ToUniversalTime();
                // Get time info for user timezone
                // Convert time to UTC
                DateTime userDateTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, timeInfo);
                start = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, timeInfo.Id);
                end  = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, timeInfo.Id);
                //if (Convert.ToDateTime(string.Format(AriBotV4.Common.Constants.TaskTimeFormat, userDateTime)).Hour < Convert.ToDateTime(string.Format(AriBotV4.Common.Constants.TaskTimeFormat, getTasks.StartTime)).Hour)
                //{
                //    start = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, timeInfo.Id);
                //}
                //else
                //{
                //    start = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow.AddDays(1), timeInfo.Id);
                //}

            }

            if (end != DateTime.MinValue)
            {
                //endDate = start.ToString(AriBotV4.Common.Constants.CreateTaskDate).Replace("-", "/");
                //userProfile.CreateTask.Add(Constants.EndDate, endDate);
                getTasks.EndDate = Convert.ToString(end);
                //endTime = string.Format(AriBotV4.Common.Constants.TaskTimeFormat, end);
            }
            if (start != DateTime.MinValue)
            {
                //startDate = start.ToString(AriBotV4.Common.Constants.CreateTaskDate).Replace("-", "/");
                getTasks.StartDate = Convert.ToString(start);
                //userProfile.CreateTask.Add(Constants.StartDate, startDate);
            }
        }
    }


}

