using AriBotV4.Common;
using AriBotV4.Dialogs.Common.Resources;
using AriBotV4.Enums;
using AriBotV4.Models;
using AriBotV4.Models.TaskSpur.Auth;
using AriBotV4.Models.TaskSpur.Goals;
using AriBotV4.Models.TaskSpur.Goals.Get;
using AriBotV4.Models.TaskSpur.Tasks;
using AriBotV4.Models.TaskSpur.Tasks.Reminder;
using AriBotV4.Services;
using LuisEntityHelpers;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TimeZoneConverter;

namespace AriBotV4.Dialogs.TaskSpur
{
    public class CreateTaskDialog : ComponentDialog
    {

        #region Properties and Fields
        private readonly BotStateService _botStateService;
      
        private readonly BotServices _botServices;

        #endregion

        public CreateTaskDialog(string dialogId, BotStateService botStateService, BotServices botServices) : base(dialogId)
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
                AskTaskName,
                AskTaskDescription,
                AskTaskGoal,
                AskTaskPriority,
                AskTaskType,
                AskTaskStartDate,
                ValidateStartDate,
                ConfirmStartDate,
                AskTaskEndDate,
                ValidateEndDate,
                ConfirmEndDate,
                AskTaskStartTime,
                AskTaskEndTime,
                AskTaskReminder,
                AskTaskReminderTime,
                ValidateReminderTime,
                GetRimenderTimeId,
                ConfirmedRimenderTimeId,
                ContinueReminderFLow,
                CreateTask


            };

            // Add Named Dialogs
            AddDialog(new WaterfallDialog($"{nameof(CreateTaskDialog)}.mainFlow", waterfallSteps));
            AddDialog(new TextPrompt($"{nameof(CreateTaskDialog)}.name", Utility.ValidateTaskName));
            AddDialog(new TextPrompt($"{nameof(CreateTaskDialog)}.description"));
            AddDialog(new ChoicePrompt($"{nameof(CreateTaskDialog)}.goal"));
            AddDialog(new ChoicePrompt($"{nameof(CreateTaskDialog)}.priority"));
            AddDialog(new ChoicePrompt($"{nameof(CreateTaskDialog)}.taskType"));
            AddDialog(new DateTimePrompt($"{nameof(CreateTaskDialog)}.startDate", ValidateDate));
            AddDialog(new DateTimePrompt($"{nameof(CreateTaskDialog)}.endDate", ValidateDate));
            AddDialog(new DateTimePrompt($"{nameof(CreateTaskDialog)}.startTime", ValidateTime));
            AddDialog(new DateTimePrompt($"{nameof(CreateTaskDialog)}.endTime", ValidateTime));
            AddDialog(new ConfirmPrompt($"{nameof(CreateTaskDialog)}.remindMe"));
            AddDialog(new ConfirmPrompt($"{nameof(CreateTaskDialog)}.confirmReminderId"));
            AddDialog(new ConfirmPrompt($"{nameof(CreateTaskDialog)}.continueReminderFlow"));
            AddDialog(new TextPrompt($"{nameof(CreateTaskDialog)}.remindMeTime"));
            AddDialog(new ChoicePrompt($"{nameof(CreateTaskDialog)}.confirmDate"));
            AddDialog(new ChoicePrompt($"{nameof(CreateTaskDialog)}.confirmReminderTime"));
            AddDialog(new CommonDialog($"{nameof(CommonDialog)}.mainFlow", _botStateService));



            // Set the starting Dialog
            InitialDialogId = $"{nameof(CreateTaskDialog)}.mainFlow";
        }
        // Ask user task name
        public async Task<DialogTurnResult> AskTaskName(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            

            if (string.IsNullOrEmpty(Convert.ToString(stepContext.Context.Activity.From.Properties[Constants.TaskSpurToken])))
            {
                return await stepContext.BeginDialogAsync($"{nameof(CommonDialog)}.mainFlow", null, cancellationToken);
            }
            else
            {
                return await stepContext.PromptAsync($"{nameof(CreateTaskDialog)}.name",
                     new PromptOptions
                     {
                         Prompt = MessageFactory.Text(Utility.GenerateRandomMessages(Constants.AskTaskName)),
                         RetryPrompt = MessageFactory.Text(TaskSpur.Resources.TaskSpur.TaskEmpty)
                     }, cancellationToken);
            }
        }

        // Ask user task description
        public async Task<DialogTurnResult> AskTaskDescription(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
            userProfile.CreateTask = new Dictionary<string, object>();
            userProfile.CreateTask.Add(Constants.TaskName, (string)stepContext.Result);
            await _botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);
                      

            return await stepContext.PromptAsync($"{nameof(CreateTaskDialog)}.description",
                     new PromptOptions
                     {
                         Prompt = MessageFactory.Text(Utility.GenerateRandomMessages(Constants.AskTaskDescription))
                     }, cancellationToken);
        }

        // Ask user task goal
        public async Task<DialogTurnResult> AskTaskGoal(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            
            UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());

                userProfile.CreateTask.Add(Constants.TaskDescription, (string)stepContext.Result);
                await _botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);
            
            GetGoalsResponse goalResponse = await _botStateService._taskSpurApiClient.GetGoals
                (Convert.ToString(stepContext.Context.Activity.From.Properties[AriBotV4.Common.Constants.TaskSpurToken]));



          
            //// Check token authrozation
            //if (goalResponse.statusCode == 401)
            //{

            //    // Get refersh token
            //    TokenResponse refreshTokenResponse = await _botStateService._taskSpurApiClient.GetRefreshToken(stepContext);

            //    // Get goal with refresh token
            //    if (!string.IsNullOrEmpty(refreshTokenResponse.data.token))
            //    {
            //        goalResponse = await _botStateService._taskSpurApiClient.GetGoals
            //        (refreshTokenResponse.data.token);
            //        // Update users's from property
            //        await _botStateService._taskSpurApiClient.UpdateToken(stepContext, refreshTokenResponse);
            //    }

                


            //}
            userProfile.CreateTask.Add(Constants.GoalResponse, goalResponse);
            await _botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);


            var prompt = new PromptOptions
            {
                Prompt = MessageFactory.Text(TaskSpur.Resources.TaskSpur.AskGoalType),
                Choices = ChoiceFactory.ToChoices(goalResponse.data.data.Select(g => g.name).ToList())
                ,
                Style = ListStyle.SuggestedAction,

            };
            return await stepContext.PromptAsync($"{nameof(CreateTaskDialog)}.goal", prompt, cancellationToken);
        }

        // Ask user task priority
        public async Task<DialogTurnResult> AskTaskPriority(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
            userProfile.CreateTask.Add(Constants.GoalId, ((GetGoalsResponse)userProfile.CreateTask[Constants.GoalResponse]).data.data[((FoundChoice)stepContext.Result).Index].id);
            await _botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);
            
            var prompt = new PromptOptions
            {
                Prompt = MessageFactory.Text(TaskSpur.Resources.TaskSpur.AskTaskPriority),
                Choices = ChoiceFactory.ToChoices(new List<string>
                {
                        EnumHelpers.GetEnumDescription(AriBotV4.Enums.Priority.Low),
                        EnumHelpers.GetEnumDescription(AriBotV4.Enums.Priority.Medium),
                        EnumHelpers.GetEnumDescription(AriBotV4.Enums.Priority.High),
                }),
                Style = ListStyle.SuggestedAction,

            };
            return await stepContext.PromptAsync($"{nameof(CreateTaskDialog)}.priority", prompt, cancellationToken);

        }

        // Ask user task type
        public async Task<DialogTurnResult> AskTaskType(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            AriBotV4.Enums.Priority priority = (AriBotV4.Enums.Priority)Enum.Parse
                (typeof(AriBotV4.Enums.Priority), ((FoundChoice)stepContext.Result).Value, true);

            
            UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());

            userProfile.CreateTask.Add(Constants.PriorityId,priority);
            await _botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);


            var prompt = new PromptOptions
            {
                Choices = ChoiceFactory.ToChoices(new List<string>
                {
                       EnumHelpers.GetEnumDescription(AriBotV4.Enums.TaskType.Unscheduled),
                        EnumHelpers.GetEnumDescription(AriBotV4.Enums.TaskType.Appointment),
                        EnumHelpers.GetEnumDescription(AriBotV4.Enums.TaskType.StartDateNoTime),
                        EnumHelpers.GetEnumDescription(AriBotV4.Enums.TaskType.StartDateWithTime),
                        EnumHelpers.GetEnumDescription(AriBotV4.Enums.TaskType.StartEndNoTime),
                        EnumHelpers.GetEnumDescription(AriBotV4.Enums.TaskType.StartEndWithTime),
                }),
                Style = ListStyle.SuggestedAction,
            };

            return await stepContext.PromptAsync($"{nameof(CreateTaskDialog)}.taskType", prompt, cancellationToken);

        }

        // Ask user task start date if task type is not unscheduled
        public async Task<DialogTurnResult> AskTaskStartDate(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            AriBotV4.Enums.TaskType taskType = new TaskType();
            
            UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
            
            if (stepContext.Context.Activity.Text != SharedStrings.ConfirmNo)
            {
                taskType = EnumHelpers.GetValueFromDescription<AriBotV4.Enums.TaskType>(((FoundChoice)stepContext.Result).Value);

                
                userProfile.CreateTask.Add(Constants.TaskTypes, taskType);
                await _botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);
            }
            else
            {
                taskType =  (TaskType)Enum.Parse(typeof(TaskType),Convert.ToString(userProfile.CreateTask[Constants.TaskTypes]));
             
            }


            if (Convert.ToString(taskType) != Convert.ToString(AriBotV4.Enums.TaskType.Unscheduled))
            {

                var prompt = new PromptOptions
                {
                    Prompt = MessageFactory.Text(TaskSpur.Resources.TaskSpur.TaskStartDate),
                    RetryPrompt = MessageFactory.Text(TaskSpur.Resources.TaskSpur.TaskInvalidDate),
                    Choices = ChoiceFactory.ToChoices(AriBotV4.Common.Constants.TaskType),
                    Style = ListStyle.List,

                };
                return await stepContext.PromptAsync($"{nameof(CreateTaskDialog)}.startDate", prompt, cancellationToken);
            }
            return await stepContext.NextAsync(stepContext, cancellationToken);
        }

        // check whether user enter proper start date
        public async Task<DialogTurnResult> ValidateStartDate(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            string startDate1 = string.Empty;
            //StartOn startOn = new StartOn();
            bool isValidDate = true;
            DateTime start = new DateTime();
            if (stepContext.Result is IList<DateTimeResolution> datetimes)
            {
                if (datetimes.Last().Start != null)
                {
                    start = Convert.ToDateTime(datetimes.Last().Start);
                    isValidDate = false;
                }

                else if (DateTime.TryParse(datetimes.Last().Value, out start))
                {
                    start = Convert.ToDateTime(datetimes.Last().Value);
                }
                else
                {

                    start = DateTime.Now.AddSeconds(Convert.ToDouble(datetimes.Last().Value));
                    isValidDate = false;
                }

                startDate1 = start.ToString(AriBotV4.Common.Constants.CreateTaskDate).Replace("-", "/");
              
                UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
               
                    if (userProfile.CreateTask.ContainsKey(Constants.StartDate))
                        userProfile.CreateTask[Constants.StartDate] = startDate1;

                    else
                        userProfile.CreateTask.Add(Constants.StartDate, startDate1);
                
                await _botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);


            }

            if (!isValidDate)
            {

                DateTime startDate;

                // The first 'TryParse...' that succeeds will store the parsed date in 'startdate'
                DateTime.TryParseExact(startDate1, AriBotV4.Common.Constants.confirmDate, null,
                     DateTimeStyles.None, out startDate);

                return await stepContext.PromptAsync($"{nameof(CreateTaskDialog)}.confirmDate",
            new PromptOptions
            {
                Style = ListStyle.SuggestedAction,
                Prompt = MessageFactory.Text(string.Format(TaskSpur.Resources.TaskSpur.TaskConfirmDate,
                startDate.ToString(AriBotV4.Common.Constants.DateFormat))),
                Choices = ChoiceFactory.ToChoices(new List<string>
                {
                       SharedStrings.ConfirmYes,
                       SharedStrings.ConfirmNo
                }),
            }, cancellationToken);

            }

            return await stepContext.NextAsync(stepContext, cancellationToken);
        }

        // confirm with user start date
        public async Task<DialogTurnResult> ConfirmStartDate(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            Type choiceType = typeof(FoundChoice);
            Type resultType = stepContext.Result.GetType();
            if (resultType == choiceType)
            {
                var selectedChoice = ((FoundChoice)stepContext.Result).Value;

                if (selectedChoice.Contains(SharedStrings.ConfirmYes))
                {
                    return await stepContext.NextAsync(stepContext, cancellationToken);
                }
                else
                {
                    stepContext.ActiveDialog.State["stepIndex"] = (int)stepContext.ActiveDialog.State["stepIndex"] - 2;
                    return await AskTaskStartDate(stepContext, cancellationToken);
                }
            }
            else
            {
                return await stepContext.NextAsync(stepContext, cancellationToken);
            }
        }

        // Ask user task end date if task type is not unscheduled, start date with no time , start date with time
        public async Task<DialogTurnResult> AskTaskEndDate(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
            AriBotV4.Enums.TaskType taskType = (TaskType)userProfile.CreateTask[Constants.TaskTypes];

            if (taskType != AriBotV4.Enums.TaskType.Unscheduled &&
            taskType != AriBotV4.Enums.TaskType.StartDateNoTime &&
            taskType != AriBotV4.Enums.TaskType.StartDateWithTime
            )
            {
                var prompt = new PromptOptions
                {
                    Prompt = MessageFactory.Text(TaskSpur.Resources.TaskSpur.TaskEndDate),
                    RetryPrompt = MessageFactory.Text(TaskSpur.Resources.TaskSpur.TaskInvalidDate),
                    Choices = ChoiceFactory.ToChoices(AriBotV4.Common.Constants.TaskType),
                    Style = ListStyle.List,

                };
                return await stepContext.PromptAsync($"{nameof(CreateTaskDialog)}.endDate", prompt, cancellationToken);
            }
            return await stepContext.NextAsync(stepContext.Context.Activity.Text, cancellationToken);

        }

        // check whether user enter proper end date
        public async Task<DialogTurnResult> ValidateEndDate(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            bool isValidDate = true;
            string endDate1 = string.Empty;
            //EndOn endOn = new EndOn();
            DateTime end = new DateTime();
            if (stepContext.Result is IList<DateTimeResolution> datetimes)
            {
                if (datetimes.Last().Start != null)
                {
                    end = Convert.ToDateTime(datetimes.Last().Start);
                    isValidDate = false;
                }
                else if (DateTime.TryParse(datetimes.Last().Value, out end))
                {
                    end = Convert.ToDateTime(datetimes.Last().Value);
                }
                else
                {
                    end = DateTime.Now.AddSeconds(Convert.ToDouble(datetimes.Last().Value));
                    isValidDate = false;
                }

                endDate1 = end.ToString(AriBotV4.Common.Constants.CreateTaskDate).Replace("-", "/");
                
                UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
                if (userProfile.CreateTask.ContainsKey(Constants.EndDate))
                    userProfile.CreateTask[Constants.EndDate] = endDate1;

                else
                    userProfile.CreateTask.Add(Constants.EndDate, endDate1);
                await _botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);


            }

            if (!isValidDate)
            {

                DateTime endDate;

                // The first 'TryParse...' that succeeds will store the parsed date in 'endDate'
                DateTime.TryParseExact(endDate1, AriBotV4.Common.Constants.confirmDate, null,
                     DateTimeStyles.None, out endDate);

                return await stepContext.PromptAsync($"{nameof(CreateTaskDialog)}.confirmDate",
            new PromptOptions
            {
                Style = ListStyle.SuggestedAction,
                Prompt = MessageFactory.Text(string.Format(TaskSpur.Resources.TaskSpur.TaskConfirmDate,
                endDate.ToString(AriBotV4.Common.Constants.DateFormat))),
                Choices = ChoiceFactory.ToChoices(new List<string>
                {
                       SharedStrings.ConfirmYes,
                       SharedStrings.ConfirmNo
                }),
            }, cancellationToken);

            }

            return await stepContext.NextAsync(stepContext, cancellationToken);
        }

        // confirm with user end date
        public async Task<DialogTurnResult> ConfirmEndDate(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            Type choiceType = typeof(FoundChoice);
            Type resultType = stepContext.Result.GetType();
            if (resultType == choiceType)
            {
                var selectedChoice = ((FoundChoice)stepContext.Result).Value;

                if (selectedChoice.Contains(SharedStrings.ConfirmYes))
                {
                    return await stepContext.NextAsync(stepContext, cancellationToken);
                }
                else
                {
                    stepContext.ActiveDialog.State["stepIndex"] = (int)stepContext.ActiveDialog.State["stepIndex"] - 2;
                    return await AskTaskEndDate(stepContext, cancellationToken);
                }
            }
            else
            {
                return await stepContext.NextAsync(stepContext, cancellationToken);
            }
        }


        // Ask user task start time if task type is not unscheduled, start date with no time , start end date with no time
        public async Task<DialogTurnResult> AskTaskStartTime(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
            AriBotV4.Enums.TaskType taskType = (TaskType)userProfile.CreateTask[Constants.TaskTypes];

            if (taskType != AriBotV4.Enums.TaskType.Unscheduled &&
            taskType != AriBotV4.Enums.TaskType.StartDateNoTime &&
            taskType != AriBotV4.Enums.TaskType.StartEndNoTime)
            {
                var prompt = new PromptOptions
                {
                    Prompt = MessageFactory.Text(TaskSpur.Resources.TaskSpur.TaskStartTime),
                    RetryPrompt = MessageFactory.Text(TaskSpur.Resources.TaskSpur.TaskValidTime),
                    Choices = ChoiceFactory.ToChoices(AriBotV4.Common.Constants.TaskType),
                    Style = ListStyle.List,

                };
                return await stepContext.PromptAsync($"{nameof(CreateTaskDialog)}.startTime", prompt, cancellationToken);
            }
            return await stepContext.NextAsync(stepContext.Context.Activity.Text, cancellationToken);

        }
        // Ask user task end time if task type is not unscheduled, start date with no time , start end date with no time
        public async Task<DialogTurnResult> AskTaskEndTime(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());

            string startT = string.Empty;
            AriBotV4.Enums.TaskType taskType = (TaskType)userProfile.CreateTask[Constants.TaskTypes];

            if (stepContext.Result is IList<DateTimeResolution> datetimes)
            {
                string startD = (string)userProfile.CreateTask[Constants.StartDate];
                if (datetimes.First().Timex != null && datetimes.First().Timex == Constants.Now)
                {
                    DateTime localDate = DateTime.UtcNow;
                    DateTime utcTime = localDate.ToUniversalTime();
                    // Get time info for user timezone
                    TimeZoneInfo timeInfo = TZConvert.GetTimeZoneInfo(Convert.ToString(stepContext.Context.Activity.From.Properties[Constants.TaskSpurTimeZone]));
                    // Convert time to UTC
                    DateTime userDateTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, timeInfo);
                    startT = string.Format(AriBotV4.Common.Constants.TaskTimeFormat, userDateTime);
                }
                else
                {
                    startT = string.Format(AriBotV4.Common.Constants.TaskTimeFormat, Convert.ToDateTime(datetimes.First().Value));
                }


                
                
                userProfile.CreateTask.Add(Constants.StartTime, startT);
                await _botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);

            }


            if (taskType != AriBotV4.Enums.TaskType.Unscheduled &&
                taskType != AriBotV4.Enums.TaskType.StartDateNoTime &&
                taskType != AriBotV4.Enums.TaskType.StartEndNoTime)

            {
                var prompt = new PromptOptions
                {
                    Prompt = MessageFactory.Text(TaskSpur.Resources.TaskSpur.TaskEndTime),
                    RetryPrompt = MessageFactory.Text(TaskSpur.Resources.TaskSpur.TaskValidTime),
                    Choices = ChoiceFactory.ToChoices(AriBotV4.Common.Constants.TaskType),
                    Style = ListStyle.List,

                };
                return await stepContext.PromptAsync($"{nameof(CreateTaskDialog)}.endTime", prompt, cancellationToken);
            }
            return await stepContext.NextAsync(stepContext.Context.Activity.Text, cancellationToken);
        }

        // Ask user to remind for task
        public async Task<DialogTurnResult> AskTaskReminder(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
            string endT = string.Empty;
            AriBotV4.Enums.TaskType taskType = (TaskType)userProfile.CreateTask[Constants.TaskTypes];

            if (userProfile.CreateTask.ContainsKey(Constants.EndDate))
            {
                if (stepContext.Result is IList<DateTimeResolution> datetimes)
                {

                    //EndOn endOn = (EndOn)userProfile.CreateTask[Constants.EndDate];

                    if (datetimes.First().Timex != null && datetimes.First().Timex == Constants.Now)
                    {
                        DateTime localDate = DateTime.UtcNow;
                        DateTime utcTime = localDate.ToUniversalTime();
                        // Get time info for user timezone
                        TimeZoneInfo timeInfo = TZConvert.GetTimeZoneInfo(Convert.ToString(stepContext.Context.Activity.From.Properties
                            [Constants.TaskSpurTimeZone]));
                        // Convert time to UTC
                        DateTime userDateTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, timeInfo);
                        endT = string.Format(AriBotV4.Common.Constants.TaskTimeFormat, userDateTime);
                    }
                    else
                    {
                        endT = string.Format(AriBotV4.Common.Constants.TaskTimeFormat, Convert.ToDateTime(datetimes.First().Value));
                    }

                    userProfile.CreateTask.Add(Constants.EndTime, endT);
                    await _botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);

                    //createTaskRequest.endOn = endOn;
                }
            }

            if (taskType != AriBotV4.Enums.TaskType.Unscheduled &&
         taskType != AriBotV4.Enums.TaskType.StartDateNoTime &&
         taskType != AriBotV4.Enums.TaskType.StartEndNoTime)
            {
                return await stepContext.PromptAsync($"{nameof(CreateTaskDialog)}.remindMe",
        new PromptOptions
        {
            Prompt = MessageFactory.Text(TaskSpur.Resources.TaskSpur.TaskRemindMe)
        }, cancellationToken);
            }
            else
            {
                return await stepContext.NextAsync(stepContext.Context.Activity.Text, cancellationToken);
            }
        }

        // If reminder yes, then how much before time 
        public async Task<DialogTurnResult> AskTaskReminderTime(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());

            AriBotV4.Enums.TaskType taskType = (TaskType)userProfile.CreateTask[Constants.TaskTypes];
            if (taskType != AriBotV4.Enums.TaskType.Unscheduled &&
       taskType != AriBotV4.Enums.TaskType.StartDateNoTime &&
       taskType != AriBotV4.Enums.TaskType.StartEndNoTime)
            {
                
                //createTaskRequest.remindMe = (bool)stepContext.Result;
                if (!userProfile.CreateTask.ContainsKey(Constants.RemindMe))
                {
                    userProfile.CreateTask.Add(Constants.RemindMe, (bool)stepContext.Result);
                    await _botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);
                }
                if ((bool)userProfile.CreateTask[Constants.RemindMe])
                {
                    return await stepContext.PromptAsync($"{nameof(CreateTaskDialog)}.remindMeTime",
                         new PromptOptions
                         {
                             Prompt = MessageFactory.Text(TaskSpur.Resources.TaskSpur.TaskRemindTime)
                         }, cancellationToken);
                }
            }
            return await stepContext.NextAsync(stepContext.Context.Activity.Text, cancellationToken);
        }


        public async Task<DialogTurnResult> GetRimenderTimeId(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            
            UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());

            AriBotV4.Enums.TaskType taskType = (TaskType)userProfile.CreateTask[Constants.TaskTypes];
            if (taskType != AriBotV4.Enums.TaskType.Unscheduled &&
       taskType != AriBotV4.Enums.TaskType.StartDateNoTime &&
       taskType != AriBotV4.Enums.TaskType.StartEndNoTime)
            {
                if ((bool)userProfile.CreateTask[Constants.RemindMe])
                {
                    if (!userProfile.CreateTask.ContainsKey(Constants.ReminderTime))
                    {

                        if (((FoundChoice)stepContext.Result).Value.Contains(Constants.ReminderMinutes))
                        {

                            userProfile.CreateTask.Add(Constants.ReminderTime, (int)userProfile.CreateTask[Constants.NumericReminder]);

                        }
                        else if (((FoundChoice)stepContext.Result).Value.Contains(Constants.ReminderHour))
                        {

                            TimeSpan timeSpan = TimeSpan.FromMinutes((int)userProfile.CreateTask[Constants.NumericReminder] * 60);
                            userProfile.CreateTask.Add(Constants.ReminderTime, Convert.ToInt32(timeSpan.TotalMinutes));

                        }
                        else
                        {

                            TimeSpan timeSpan = TimeSpan.FromMinutes((int)userProfile.CreateTask[Constants.NumericReminder] * 24 * 60);
                            userProfile.CreateTask.Add(Constants.ReminderTime, Convert.ToInt32(timeSpan.TotalMinutes));

                        }

                      

                    }
                    else
                    {
                        
                        // call rimender time id 
                        GetReminderResponse response = await _botStateService._taskSpurApiClient.GetReminderList();
                        int reminder = (int)userProfile.CreateTask[Constants.ReminderTime];
                        var closest = response.data.OrderBy(item => Math.Abs(reminder - item.minutes)).First();
                        if (reminder == 0)
                            closest = response.data[1];
                        else if(closest.minutes == 0)
                            closest = response.data[2];
                        userProfile.CreateTask.TryAdd(Constants.ReminderTimeId, closest.id);
                        return await stepContext.PromptAsync($"{nameof(CreateTaskDialog)}.confirmReminderId",
             new PromptOptions
             {
                 Prompt = MessageFactory.Text(string.Format(TaskSpur.Resources.TaskSpur.ReminderClosestMessage, closest.name))
             }, cancellationToken);
                    }
                }
            }


            return await stepContext.NextAsync(stepContext, cancellationToken);





        }
        public async Task<DialogTurnResult> ConfirmedRimenderTimeId(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());

            AriBotV4.Enums.TaskType taskType = (TaskType)userProfile.CreateTask[Constants.TaskTypes];
            if (taskType != AriBotV4.Enums.TaskType.Unscheduled &&
       taskType != AriBotV4.Enums.TaskType.StartDateNoTime &&
       taskType != AriBotV4.Enums.TaskType.StartEndNoTime)
            {


                if ((bool)userProfile.CreateTask[Constants.RemindMe])
                {

                    if(stepContext.Context.Activity.Text == "No")
                    {
                        return await stepContext.PromptAsync($"{nameof(CreateTaskDialog)}.continueReminderFlow",
new PromptOptions
{
Prompt = MessageFactory.Text(TaskSpur.Resources.TaskSpur.ReminderHappyMessage)
}, cancellationToken);
                    }
                }
            }
            return await stepContext.NextAsync(stepContext, cancellationToken);


        }
        public async Task<DialogTurnResult> ContinueReminderFLow(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());

            AriBotV4.Enums.TaskType taskType = (TaskType)userProfile.CreateTask[Constants.TaskTypes];
            if (taskType != AriBotV4.Enums.TaskType.Unscheduled &&
       taskType != AriBotV4.Enums.TaskType.StartDateNoTime &&
       taskType != AriBotV4.Enums.TaskType.StartEndNoTime)
            {


                if ((bool)userProfile.CreateTask[Constants.RemindMe])
                {
                    Type boolType = typeof(System.Boolean);
                    Type resultType = stepContext.Result.GetType();
                    
                    
                    if (resultType== boolType && (bool)stepContext.Result)
                    {
                        stepContext.ActiveDialog.State["stepIndex"] = (int)stepContext.ActiveDialog.State["stepIndex"] - 4;
                        return await AskTaskReminderTime(stepContext, cancellationToken);

                    }
                }
            }
            return await stepContext.NextAsync(stepContext, cancellationToken);
        }
        public async Task<DialogTurnResult> ValidateReminderTime(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());

            AriBotV4.Enums.TaskType taskType = (TaskType)userProfile.CreateTask[Constants.TaskTypes];
            if (taskType != AriBotV4.Enums.TaskType.Unscheduled &&
       taskType != AriBotV4.Enums.TaskType.StartDateNoTime &&
       taskType != AriBotV4.Enums.TaskType.StartEndNoTime)
            {


                if ((bool)userProfile.CreateTask[Constants.RemindMe])
                {
                    int isNumeric;
                    var recognizerResult = await _botServices.Dispatch.RecognizeAsync(stepContext.Context, cancellationToken);
                    LuisModel luisResponse = JsonConvert.DeserializeObject<LuisModel>(JsonConvert.SerializeObject(recognizerResult));

                    if (luisResponse.Entities.datetime != null)
                    {

                        var remindMe = Microsoft.Recognizers.Text.DataTypes.TimexExpression.TimexResolver.Resolve
                    (new[] { luisResponse.Entities.datetime[0].Expressions[0] },
                        System.DateTime.Today);

                        if (remindMe.Values != null && remindMe.Values[0] != null)
                        {
                            TimeSpan timeSpan = TimeSpan.FromSeconds(Convert.ToDouble(remindMe.Values[0].Value));
                            if (!userProfile.CreateTask.ContainsKey(Constants.ReminderTime))
                                userProfile.CreateTask.Add(Constants.ReminderTime, Convert.ToInt32(timeSpan.TotalMinutes));

                        }

                        return await stepContext.NextAsync(stepContext.Context.Activity.Text, cancellationToken);
                    }

                    else if (int.TryParse(stepContext.Context.Activity.Text, out isNumeric))
                    {
                        return await GetReminderConfirmation(stepContext, cancellationToken);
                    }
                    else
                    {
                        stepContext.ActiveDialog.State["stepIndex"] = (int)stepContext.ActiveDialog.State["stepIndex"] - 1;
                        return await AskTaskReminderTime(stepContext, cancellationToken);
                    }


                }
            }
            return await stepContext.NextAsync(stepContext.Context.Activity.Text, cancellationToken);
        }

        public async Task<DialogTurnResult> GetReminderConfirmation(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());

            AriBotV4.Enums.TaskType taskType = (TaskType)userProfile.CreateTask[Constants.TaskTypes];
            if (taskType != AriBotV4.Enums.TaskType.Unscheduled &&
       taskType != AriBotV4.Enums.TaskType.StartDateNoTime &&
       taskType != AriBotV4.Enums.TaskType.StartEndNoTime)
            {
            
                userProfile.CreateTask.Add(Constants.NumericReminder, Convert.ToInt32(stepContext.Result));

                var prompt = new PromptOptions
                {
                    Prompt = MessageFactory.Text(TaskSpur.Resources.TaskSpur.ConfirmReminder),
                    Choices = ChoiceFactory.ToChoices(new List<string> {
                    Constants.ReminderMinutes,
                    Constants.ReminderHour,
                     Constants.ReminderDays,
                    
                      
                       //EnumHelpers.GetEnumDescription(AriBotV4.Enums.AskAri.Videos),

                    }),
                    Style = ListStyle.SuggestedAction,

                };




                return await stepContext.PromptAsync($"{nameof(CreateTaskDialog)}.confirmReminderTime", prompt, cancellationToken);
            }

            return await stepContext.NextAsync(stepContext.Context.Activity.Text, cancellationToken);
        }
        // Create task
        public async Task<DialogTurnResult> CreateTask(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            
            UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
                      
            CreateTaskRequest createTaskRequest = new CreateTaskRequest();
            createTaskRequest.name = (string)userProfile.CreateTask[Constants.TaskName];
            createTaskRequest.description = (string)userProfile.CreateTask[Constants.TaskDescription];
            createTaskRequest.priorityId = (int)userProfile.CreateTask[Constants.PriorityId];
            createTaskRequest.goalId = (int)userProfile.CreateTask[Constants.GoalId];
            if (userProfile.CreateTask.ContainsKey(Constants.RemindMe))
                createTaskRequest.remindMe = (bool)userProfile.CreateTask[Constants.RemindMe];
            if (userProfile.CreateTask.ContainsKey(Constants.ReminderTimeId))
                createTaskRequest.reminderTimeId = (int)userProfile.CreateTask[Constants.ReminderTimeId];
            if (userProfile.CreateTask.ContainsKey(Constants.ReminderTime))
                createTaskRequest.reminderTime = (int)userProfile.CreateTask[Constants.ReminderTime];
            if (userProfile.CreateTask.ContainsKey(Constants.StartDate))
                createTaskRequest.startDate = (string)userProfile.CreateTask[Constants.StartDate];
            if (userProfile.CreateTask.ContainsKey(Constants.EndDate))
                createTaskRequest.endDate = (string)userProfile.CreateTask[Constants.EndDate];
            if (userProfile.CreateTask.ContainsKey(Constants.StartTime))
                createTaskRequest.startTime = (string)userProfile.CreateTask[Constants.StartTime];
            if (userProfile.CreateTask.ContainsKey(Constants.EndTime))
                createTaskRequest.endTime = (string)userProfile.CreateTask[Constants.EndTime];

            
                createTaskRequest.timeZone = Convert.ToString(stepContext.Context.Activity.From.Properties[AriBotV4.Common.Constants.TaskSpurTimeZone]);
            
               
            //createTaskRequest.reminderTimeId = 1;
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
                                 
            

            return await stepContext.BeginDialogAsync($"{nameof(AnythingElseDialog)}.AnythingElse", TaskSpur.Resources.TaskSpur.CreateTasks, cancellationToken);


        }

   

        // Validate start time and end time
        private async Task<bool> ValidateTime(PromptValidatorContext<IList<DateTimeResolution>> promptContext, CancellationToken cancellationToken)
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


  
        // Validate start date and end date

        private async Task<bool> ValidateDate(PromptValidatorContext<IList<DateTimeResolution>> promptContext, CancellationToken cancellationToken)
        {

            // Get LUIS response from last dialog
            var recognizerResult = await _botServices.Dispatch.RecognizeAsync(promptContext.Context, cancellationToken);
            LuisModel luisResponse = JsonConvert.DeserializeObject<LuisModel>(JsonConvert.SerializeObject(recognizerResult));
            if (luisResponse.Entities.datetime != null)
            {
                bool isValidDate = luisResponse.Entities.datetime.Any(str => str.Type.ToLower() == (Constants.Time) || str.Type.ToLower() == (Constants.DateTime) ||
                str.Type.ToLower() == (Constants.DateTimeRange) || str.Type.ToLower() == (Constants.TimeRange));
                if (isValidDate)
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
    }
}
