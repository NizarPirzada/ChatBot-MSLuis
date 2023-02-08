using AriBotV4.Common;
using AriBotV4.Dialogs.Common.Resources;
using AriBotV4.Enums;
using AriBotV4.Models;
using AriBotV4.Models.TaskSpur;
using AriBotV4.Models.TaskSpur.Tasks;
using AriBotV4.Models.TaskSpur.Tasks.Id;
using AriBotV4.Models.TaskSpur.Tasks.Reminder;
using AriBotV4.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
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
    public class EditTaskDialog : ComponentDialog
    {

        #region Properties and Fields
        private readonly BotStateService _botStateService;
        private readonly BotServices _botServices;


        #endregion

        public EditTaskDialog(string dialogId, BotStateService botStateService, BotServices botServices) : base(dialogId)
        {
            _botStateService = botStateService ?? throw new System.ArgumentNullException(nameof(botStateService));
            _botServices = botServices ?? throw new System.ArgumentNullException(nameof(botStateService));

            InitializeWaterfallDialog();
        }
        private void InitializeWaterfallDialog()
        {
            //Create Waterfall Steps
            var waterfallSteps = new WaterfallStep[]
            {
                AskTaskName,
                GetTask,
                EditTaskOptions,
                SelectEditOption,
                AskTaskStartDate,
                ValidateStartDate,
                ConfirmStartDate,
                AskTaskEndDate,
                ValidateEndDate,
                ConfirmEndDate,
                AskTaskStartTime,
                AskTaskEndTime,
                ReminderOptions,
                ReminderOptionsResponse,
                AskTaskReminder,
                AskTaskReminderTime,
            GetReminderTimeId,
                UpdateTask,
                FinalAsync

            };
            AddDialog(new WaterfallDialog($"{nameof(EditTaskDialog)}.mainFlow", waterfallSteps));
            AddDialog(new TextPrompt($"{nameof(EditTaskDialog)}.name", ValidateTaskName));
            AddDialog(new TextPrompt($"{nameof(EditTaskDialog)}.editTaskName"));
            AddDialog(new ChoicePrompt($"{nameof(EditTaskDialog)}.editTaskOption"));
            AddDialog(new ChoicePrompt($"{nameof(EditTaskDialog)}.checkReminder"));
            AddDialog(new ChoicePrompt($"{nameof(EditTaskDialog)}.edit"));
            AddDialog(new ChoicePrompt($"{nameof(EditTaskDialog)}.priority"));
            AddDialog(new ChoicePrompt($"{nameof(EditTaskDialog)}.taskType"));
            InitialDialogId = $"{nameof(EditTaskDialog)}.mainFlow";
            AddDialog(new DateTimePrompt($"{nameof(EditTaskDialog)}.startDate", ValidateDate));
            AddDialog(new DateTimePrompt($"{nameof(EditTaskDialog)}.endDate", ValidateDate));
            AddDialog(new DateTimePrompt($"{nameof(EditTaskDialog)}.startTime", ValidateTime));
            AddDialog(new DateTimePrompt($"{nameof(EditTaskDialog)}.endTime", ValidateTime));
            AddDialog(new ChoicePrompt($"{nameof(EditTaskDialog)}.confirmDate"));
            AddDialog(new CommonDialog($"{nameof(CommonDialog)}.mainFlow", _botStateService));
            AddDialog(new ConfirmPrompt($"{nameof(EditTaskDialog)}.remindMe"));
            AddDialog(new ConfirmPrompt($"{nameof(EditTaskDialog)}.confirmReminderId"));
            AddDialog(new ConfirmPrompt($"{nameof(EditTaskDialog)}.continueReminderFlow"));
            AddDialog(new TextPrompt($"{nameof(EditTaskDialog)}.remindMeTime"));
            AddDialog(new ChoicePrompt($"{nameof(EditTaskDialog)}.confirmDate"));
            AddDialog(new ChoicePrompt($"{nameof(EditTaskDialog)}.confirmReminderTime"));
        }

        public async Task<DialogTurnResult> AskTaskName(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            
            if (string.IsNullOrEmpty(Convert.ToString(stepContext.Context.Activity.From.Properties[Constants.TaskSpurToken])))
            {
                return await stepContext.BeginDialogAsync($"{nameof(CommonDialog)}.mainFlow", null, cancellationToken);
            }
            else
            {
                return await stepContext.PromptAsync($"{nameof(EditTaskDialog)}.name",
                     new PromptOptions
                     {
                         Prompt = MessageFactory.Text(TaskSpur.Resources.TaskSpur.SearchTaskName),
                         RetryPrompt = MessageFactory.Text(TaskSpur.Resources.TaskSpur.TaskEmpty)
                     }, cancellationToken);
            }
        }
        public async Task<DialogTurnResult> GetTask(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Call get task api
            GetTasksResponse response = await _botStateService._taskSpurApiClient.GetTask(
                Convert.ToString(stepContext.Context.Activity.From.Properties[Constants.TaskSpurToken]), (string)stepContext.Result);
            // Check token authrozation
            //if (response.statusCode == 401)
            //{

            //    // Get refersh token
            //    Models.TaskSpur.Auth.TokenResponse refreshTokenResponse = await _botStateService._taskSpurApiClient.GetRefreshToken(stepContext);

            //    // Create goal with refresh token
            //    if (!string.IsNullOrEmpty(refreshTokenResponse.data.token))
            //    {
            //        response = await _botStateService._taskSpurApiClient.GetTask(
            //    refreshTokenResponse.data.token, (string)stepContext.Result);
            //       // await stepContext.Context.SendActivityAsync(MessageFactory.Text(response.toast.message));
            //    }

            //    // Update users's from property
            //    await _botStateService._taskSpurApiClient.UpdateToken(stepContext, refreshTokenResponse);


            //}


            if (response.data != null)
            {
                // Create reply
                var reply = stepContext.Context.Activity.CreateReply();
                if (response.data.data.Count > 0)
                {
                    for (int i = 0; i <= response.data.data[0].toDoTasks.Count - 1; i++)
                    {
                        reply.Attachments.Add(EditTasks(response.data.data[0].toDoTasks[i]));

                    }
                    for (int i = 0; i <= response.data.data[0].doingTasks.Count - 1; i++)
                    {
                        reply.Attachments.Add(EditTasks(response.data.data[0].doingTasks[i]));

                    }
                    for (int i = 0; i <= response.data.data[0].laterTasks.Count - 1; i++)
                    {
                        reply.Attachments.Add(EditTasks(response.data.data[0].laterTasks[i]));

                    }
                    for (int i = 0; i <= response.data.data[0].archivedTasks.Count - 1; i++)
                    {
                        reply.Attachments.Add(EditTasks(response.data.data[0].archivedTasks[i]));

                    }
                    for (int i = 0; i <= response.data.data[0].doneTasks.Count - 1; i++)
                    {
                        reply.Attachments.Add(EditTasks(response.data.data[0].doneTasks[i]));

                    }
                    for (int i = 0; i <= response.data.data[0].unscheduledTasks.Count - 1; i++)
                    {
                        reply.Attachments.Add(EditTasks(response.data.data[0].unscheduledTasks[i]));

                    }
                }
                if (reply.Attachments.Count == 0)
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(response.toast.message));
                }
                else
                {
                    var entity = new Microsoft.Bot.Schema.Entity();
                    entity.SetAs(new Mention()
                    {

                        Mentioned = new ChannelAccount()
                        {
                             Role = "Tasks"
                        }
                    });
                    reply.Entities.Add(entity);
                    await stepContext.Context.SendActivityAsync(reply, cancellationToken);
                }
                //await stepContext.Context.SendActivityAsync(MessageFactory.Text(response.toast.message));

            }
            else
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(response.toast.message));

            await stepContext.Context.SendActivityAsync(MessageFactory.Text(TaskSpur.Resources.TaskSpur.CannotFindTask));
            await stepContext.Context.SendActivityAsync(MessageFactory.SuggestedActions(new CardAction[]
        {
        new CardAction(title: SharedStrings.SearchAgain, type: ActionTypes.PostBack, value: SharedStrings.SearchAgain),
        new CardAction(title: SharedStrings.Exit, type: ActionTypes.PostBack, value: SharedStrings.Exit),
               }));
            return new DialogTurnResult(DialogTurnStatus.Waiting);
        }
        public async Task<DialogTurnResult> EditTaskOptions(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((string)stepContext.Result == SharedStrings.SearchAgain)
            {
                return await stepContext.ReplaceDialogAsync(InitialDialogId);
            }
            else if ((string)stepContext.Result == SharedStrings.Exit)
            {
                await stepContext.EndDialogAsync(null, cancellationToken);
                return await stepContext.BeginDialogAsync($"{nameof(AnythingElseDialog)}.AnythingElse", null, cancellationToken);
            }
            else
            {
                stepContext.Values[Constants.TaskId] = (string)stepContext.Result.ToString().Split("|")[1];

                // get task by id
                GetTaskByIdResponse response = await _botStateService._taskSpurApiClient.GetTaskById(
                Convert.ToString(stepContext.Context.Activity.From.Properties[Constants.TaskSpurToken]), Convert.ToInt32(stepContext.Values[Constants.TaskId]));

                if (response.data != null)
                {
                    stepContext.Values[Constants.RemindMe] = response.data.remindMe;
                    stepContext.Values[Constants.ReminderTimeId] = response.data.reminderTimeId;
                    stepContext.Values[Constants.ReminderTime] = response.data.reminderTime;

                }


                var edit = new PromptOptions
                {
                    Prompt = MessageFactory.Text(TaskSpur.Resources.TaskSpur.EditTaskOption),
                    Choices = ChoiceFactory.ToChoices(new List<string>
                {
                        TaskSpur.Resources.TaskSpur.TaskName,
                        TaskSpur.Resources.TaskSpur.Priority,
                        TaskSpur.Resources.TaskSpur.Reschedule
                })
                };

                return await stepContext.PromptAsync($"{nameof(EditTaskDialog)}.editTaskOption", edit, cancellationToken);
            }


        }
        public async Task<DialogTurnResult> SelectEditOption(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
        
            var selectedChoice = ((FoundChoice)stepContext.Result).Value;
            stepContext.Values[Constants.SelectedEditOption] = selectedChoice;
            if (selectedChoice == TaskSpur.Resources.TaskSpur.TaskName)
            {
                return await stepContext.PromptAsync($"{nameof(EditTaskDialog)}.editTaskName",
                     new PromptOptions
                     {
                         Prompt = MessageFactory.Text(TaskSpur.Resources.TaskSpur.EditTaskName),

                     }, cancellationToken);
                //    return await stepContext.NextAsync(null, cancellationToken);

            }
            else if (selectedChoice == TaskSpur.Resources.TaskSpur.Priority)
            {

                var prompt = new PromptOptions
                {
                    Prompt = MessageFactory.Text(TaskSpur.Resources.TaskSpur.EditTaskPriority),
                    Choices = ChoiceFactory.ToChoices(new List<string> {
                   EnumHelpers.GetEnumDescription(AriBotV4.Enums.Priority.Low),
                        EnumHelpers.GetEnumDescription(AriBotV4.Enums.Priority.Medium),
                        EnumHelpers.GetEnumDescription(AriBotV4.Enums.Priority.High),
                    }),
                    Style = ListStyle.SuggestedAction,

                };

                return await stepContext.PromptAsync($"{nameof(EditTaskDialog)}.priority", prompt, cancellationToken);

            }
            else
            {

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

                return await stepContext.PromptAsync($"{nameof(EditTaskDialog)}.taskType", prompt, cancellationToken);

            }


        }



        // Ask user task start date if task type is not unscheduled
        public async Task<DialogTurnResult> AskTaskStartDate(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
           
            Type choiceType = typeof(FoundChoice);
            Type stringType = typeof(String);
            Type resultType = stepContext.Result.GetType();
            if (resultType == choiceType && stepContext.Values[Constants.SelectedEditOption] != TaskSpur.Resources.TaskSpur.Reschedule)
            {

                stepContext.Values[Constants.PriorityId] = (int)Enum.Parse<AriBotV4.Enums.PriorityEnum>(((FoundChoice)stepContext.Result).Value.ToLower());
                //stepContext.Values[Constants.PriorityId] = (int)Enum.Parse<AriBotV4.Enums.PriorityEnum>((string)stepContext.Result.ToString().ToLower());
            }
            else if (resultType == stringType)
            {
                stepContext.Values[Constants.TaskName] = (string)stepContext.Result;
            }



            if (stepContext.Values[Constants.SelectedEditOption] == TaskSpur.Resources.TaskSpur.Reschedule)
            {
                AriBotV4.Enums.TaskType taskType = new TaskType();

                UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());

                if (stepContext.Context.Activity.Text != SharedStrings.ConfirmNo)
                {
                    taskType = EnumHelpers.GetValueFromDescription<AriBotV4.Enums.TaskType>(((FoundChoice)stepContext.Result).Value);


                    stepContext.Values[Constants.TaskTypes] = taskType;
                    await _botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);
                }
                else
                {
                    taskType = (TaskType)Enum.Parse(typeof(TaskType), Convert.ToString(stepContext.Values[Constants.TaskTypes]));

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
                    return await stepContext.PromptAsync($"{nameof(EditTaskDialog)}.startDate", prompt, cancellationToken);
                }

            }

            return await stepContext.NextAsync(stepContext, cancellationToken);

        }

        // check whether user enter proper start date
        public async Task<DialogTurnResult> ValidateStartDate(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
           
            if (stepContext.Values[Constants.SelectedEditOption] == TaskSpur.Resources.TaskSpur.Reschedule)
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

                    if (stepContext.Values.ContainsKey(Constants.StartDate))
                        stepContext.Values[Constants.StartDate] = startDate1;

                    else
                        stepContext.Values[Constants.StartDate] = startDate1;

                    await _botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);


                }

                if (!isValidDate)
                {

                    DateTime startDate;

                    // The first 'TryParse...' that succeeds will store the parsed date in 'startdate'
                    DateTime.TryParseExact(startDate1, AriBotV4.Common.Constants.confirmDate, null,
                         DateTimeStyles.None, out startDate);

                    return await stepContext.PromptAsync($"{nameof(EditTaskDialog)}.confirmDate",
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
            }

            return await stepContext.NextAsync(stepContext, cancellationToken);



        }

        // confirm with user start date
        public async Task<DialogTurnResult> ConfirmStartDate(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
           
            if (stepContext.Values[Constants.SelectedEditOption] == TaskSpur.Resources.TaskSpur.Reschedule)
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
            return await stepContext.NextAsync(stepContext, cancellationToken);

        }

        // Ask user task end date if task type is not unscheduled, start date with no time , start date with time
        public async Task<DialogTurnResult> AskTaskEndDate(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
           
            if (stepContext.Values[Constants.SelectedEditOption] == TaskSpur.Resources.TaskSpur.Reschedule)
            {

                UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
                AriBotV4.Enums.TaskType taskType = (TaskType)stepContext.Values[Constants.TaskTypes];

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
                    return await stepContext.PromptAsync($"{nameof(EditTaskDialog)}.endDate", prompt, cancellationToken);
                }
            }
            return await stepContext.NextAsync(stepContext.Context.Activity.Text, cancellationToken);

        }

        // check whether user enter proper end date
        public async Task<DialogTurnResult> ValidateEndDate(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
           
            if (stepContext.Values[Constants.SelectedEditOption] == TaskSpur.Resources.TaskSpur.Reschedule)
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
                    if (stepContext.Values.ContainsKey(Constants.EndDate))
                        stepContext.Values[Constants.EndDate] = endDate1;

                    else
                        stepContext.Values[Constants.EndDate] = endDate1;
                    await _botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);


                }

                if (!isValidDate)
                {

                    DateTime endDate;

                    // The first 'TryParse...' that succeeds will store the parsed date in 'endDate'
                    DateTime.TryParseExact(endDate1, AriBotV4.Common.Constants.confirmDate, null,
                         DateTimeStyles.None, out endDate);

                    return await stepContext.PromptAsync($"{nameof(EditTaskDialog)}.confirmDate",
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
            }
            return await stepContext.NextAsync(stepContext, cancellationToken);
        }

        // confirm with user end date
        public async Task<DialogTurnResult> ConfirmEndDate(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
          
            if (stepContext.Values[Constants.SelectedEditOption] == TaskSpur.Resources.TaskSpur.Reschedule)
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
            return await stepContext.NextAsync(stepContext, cancellationToken);
        }


        // Ask user task start time if task type is not unscheduled, start date with no time , start end date with no time
        public async Task<DialogTurnResult> AskTaskStartTime(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
           
            if (stepContext.Values[Constants.SelectedEditOption] == TaskSpur.Resources.TaskSpur.Reschedule)
            {

                UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
                AriBotV4.Enums.TaskType taskType = (TaskType)stepContext.Values[Constants.TaskTypes];


                if (taskType == AriBotV4.Enums.TaskType.Unscheduled ||
                taskType == AriBotV4.Enums.TaskType.StartDateNoTime ||
                taskType == AriBotV4.Enums.TaskType.StartEndNoTime)
                {

                    stepContext.Values[Constants.ReminderTimeId] = 1;
                    stepContext.Values[Constants.ReminderTime] = 0;
                    stepContext.Values[Constants.RemindMe] = false;

                }

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
                    return await stepContext.PromptAsync($"{nameof(EditTaskDialog)}.startTime", prompt, cancellationToken);
                }
            }
            return await stepContext.NextAsync(stepContext.Context.Activity.Text, cancellationToken);

        }
        // Ask user task end time if task type is not unscheduled, start date with no time , start end date with no time
        public async Task<DialogTurnResult> AskTaskEndTime(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            
            if (stepContext.Values[Constants.SelectedEditOption] == TaskSpur.Resources.TaskSpur.Reschedule)
            {
                UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
                string startT = string.Empty;
                AriBotV4.Enums.TaskType taskType = (TaskType)stepContext.Values[Constants.TaskTypes];

                if (stepContext.Result is IList<DateTimeResolution> datetimes)
                {
                    string startD = (string)stepContext.Values[Constants.StartDate];
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



                    stepContext.Values[Constants.StartTime] = startT;

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
                    return await stepContext.PromptAsync($"{nameof(EditTaskDialog)}.endTime", prompt, cancellationToken);
                }
            }
            return await stepContext.NextAsync(stepContext.Context.Activity.Text, cancellationToken);
        }

        // check reminder
        public async Task<DialogTurnResult> ReminderOptions(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            if (stepContext.Values[Constants.SelectedEditOption] == TaskSpur.Resources.TaskSpur.Reschedule)
            {
                AriBotV4.Enums.TaskType taskType = (TaskType)stepContext.Values[Constants.TaskTypes];
                string endT = string.Empty;


                if (stepContext.Values.ContainsKey(Constants.EndDate))
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

                        stepContext.Values[Constants.EndTime] = endT;


                 }
                }

            }
            if (stepContext.Values.ContainsKey(Constants.RemindMe) && (bool)stepContext.Values[Constants.RemindMe])

            {
                // call rimender time id 
                GetReminderResponse response = await _botStateService._taskSpurApiClient.GetReminderList();

                AriBotV4.Models.TaskSpur.Tasks.Reminder.Datum reminderTime = response.data.Find(x => x.id == (int)stepContext.Values[Constants.ReminderTimeId]);

                var reminder = new PromptOptions
                {
                    Prompt = MessageFactory.Text(string.Format(TaskSpur.Resources.TaskSpur.ConfirmRemind, reminderTime.name)),
                    Choices = ChoiceFactory.ToChoices(new List<string>
                {
                        TaskSpur.Resources.TaskSpur.Yes,
                        TaskSpur.Resources.TaskSpur.Change,
                        TaskSpur.Resources.TaskSpur.Remove
                })
                };

                return await stepContext.PromptAsync($"{nameof(EditTaskDialog)}.checkReminder", reminder, cancellationToken);
            }



            return await stepContext.NextAsync(stepContext.Context.Activity.Text, cancellationToken);
        }

        public async Task<DialogTurnResult> ReminderOptionsResponse(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
         
            stepContext.Values["SameReminder"] = false;
            if (stepContext.Values.ContainsKey(Constants.RemindMe) && (bool)stepContext.Values[Constants.RemindMe])
            {
               var selectedChoice = ((FoundChoice)stepContext.Result).Value;

                if (selectedChoice == TaskSpur.Resources.TaskSpur.Yes)
                {
                    stepContext.Values["SameReminder"] = true;
                    return await stepContext.NextAsync(stepContext.Context.Activity.Text, cancellationToken);
                }
                else if (selectedChoice == TaskSpur.Resources.TaskSpur.Remove)
                {
                    if (stepContext.Values.ContainsKey(Constants.ReminderTimeId))
                        stepContext.Values.Remove(Constants.ReminderTimeId);
                    if (stepContext.Values.ContainsKey(Constants.ReminderTime))
                        stepContext.Values.Remove(Constants.ReminderTime);
                    if (stepContext.Values.ContainsKey(Constants.RemindMe))
                        stepContext.Values[Constants.RemindMe] = false;
                    return await stepContext.NextAsync(stepContext.Context.Activity.Text, cancellationToken);

                }
                else
                {
                    stepContext.Values[Constants.ChangeReminder] = true;
                    return await stepContext.NextAsync(stepContext.Context.Activity.Text, cancellationToken);
                }
            }
            return await stepContext.NextAsync(stepContext.Context.Activity.Text, cancellationToken);
        }

        public async Task<DialogTurnResult> AskTaskReminder(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

          if ((stepContext.Values.ContainsKey(Constants.TaskTypes) && (TaskType)stepContext.Values[Constants.TaskTypes] != AriBotV4.Enums.TaskType.Unscheduled &&
                    (TaskType)stepContext.Values[Constants.TaskTypes] != AriBotV4.Enums.TaskType.StartDateNoTime && (TaskType)stepContext.Values[Constants.TaskTypes] != AriBotV4.Enums.TaskType.StartEndNoTime)
                     && !(bool)stepContext.Values[Constants.RemindMe]
                     )
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
                return await stepContext.PromptAsync($"{nameof(EditTaskDialog)}.remindMe", opts);
            }
            else
            {
                //return await stepContext.NextAsync(stepContext.Context.Activity.Text, cancellationToken);
                return await stepContext.NextAsync(stepContext.Context, cancellationToken);
            }

        }

        public async Task<DialogTurnResult> AskTaskReminderTime(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
       
            if (stepContext.Context.Activity.Text == SharedStrings.ConfirmYes)
            {
                stepContext.Values.Remove(Constants.RemindMe);
                stepContext.Values.Add(Constants.RemindMe, true);
            }
                       

            if ((stepContext.Values.ContainsKey(Constants.TaskTypes) && (TaskType)stepContext.Values[Constants.TaskTypes] != AriBotV4.Enums.TaskType.Unscheduled &&
                     (TaskType)stepContext.Values[Constants.TaskTypes] != AriBotV4.Enums.TaskType.StartDateNoTime && (TaskType)stepContext.Values[Constants.TaskTypes] != AriBotV4.Enums.TaskType.StartEndNoTime && stepContext.Values.ContainsKey(Constants.SameReminder) && !(bool)stepContext.Values[Constants.SameReminder])
                     || (stepContext.Values.ContainsKey(Constants.ChangeReminder) && (bool)stepContext.Values[Constants.ChangeReminder])
                     )
                  {
                                   
                    if ((bool)stepContext.Values[Constants.RemindMe])
                    {
                        return await stepContext.PromptAsync($"{nameof(EditTaskDialog)}.remindMeTime",
                             new PromptOptions
                             {
                                 Prompt = MessageFactory.Text(TaskSpur.Resources.TaskSpur.TaskRemindTime)
                             }, cancellationToken);
                    }
                }
            
            return await stepContext.NextAsync(stepContext.Context.Activity.Text, cancellationToken);
        }


        public async Task<DialogTurnResult> GetReminderTimeId(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            
            if ((stepContext.Values.ContainsKey(Constants.TaskTypes) && (TaskType)stepContext.Values[Constants.TaskTypes] != AriBotV4.Enums.TaskType.Unscheduled &&
                     (TaskType)stepContext.Values[Constants.TaskTypes] != AriBotV4.Enums.TaskType.StartDateNoTime && (TaskType)stepContext.Values[Constants.TaskTypes] != AriBotV4.Enums.TaskType.StartEndNoTime &&  stepContext.Values.ContainsKey(Constants.SameReminder) && !(bool)stepContext.Values[Constants.SameReminder])
                     || (stepContext.Values.ContainsKey(Constants.ChangeReminder) && (bool)stepContext.Values[Constants.ChangeReminder])
                     )

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
                    if (stepContext.Values.ContainsKey(Constants.ReminderTime))
                    {
                        stepContext.Values.Remove(Constants.ReminderTime);
                        stepContext.Values.Add(Constants.ReminderTime, Convert.ToInt32(timeSpan.TotalMinutes));
                    }
                }


                // call rimender time id 
                GetReminderResponse response = await _botStateService._taskSpurApiClient.GetReminderList();
                int reminder = (int)stepContext.Values[Constants.ReminderTime];
                var closest = response.data.OrderBy(item => Math.Abs(reminder - item.minutes)).First();
                if (reminder == 0)
                    closest = response.data[1];
                else if (closest.minutes == 0)
                    closest = response.data[2];
                    stepContext.Values.Remove(Constants.ReminderTimeId);
                    stepContext.Values.Add(Constants.ReminderTimeId, closest.id);
                    return await stepContext.NextAsync(stepContext, cancellationToken);
                    //}
                }
            else
            {
                stepContext.ActiveDialog.State["stepIndex"] = (int)stepContext.ActiveDialog.State["stepIndex"] - 1;
                return await AskTaskReminderTime(stepContext, cancellationToken);
            }
        
                

            }
            return await stepContext.NextAsync(stepContext, cancellationToken);





        }
    

        public async Task<DialogTurnResult> UpdateTask(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            
            // Call get task api
            GetTaskByIdResponse response = await _botStateService._taskSpurApiClient.GetTaskById(
                Convert.ToString(stepContext.Context.Activity.From.Properties[Constants.TaskSpurToken]), Convert.ToInt32(stepContext.Values[Constants.TaskId]));
            // Check token authrozation
            //if (response.statusCode == 401)
            //{

            //    // Get refersh token
            //    Models.TaskSpur.Auth.TokenResponse refreshTokenResponse = await _botStateService._taskSpurApiClient.GetRefreshToken(stepContext);

            //    // Create goal with refresh token
            //    if (!string.IsNullOrEmpty(refreshTokenResponse.data.token))
            //    {
            //        response = await _botStateService._taskSpurApiClient.GetTaskById(
            //    refreshTokenResponse.data.token, Convert.ToInt32(stepContext.Values[Constants.TaskId]));
            //        //await stepContext.Context.SendActivityAsync(MessageFactory.Text(response.toast.message));
            //    }

            //    // Update users's from property
            //    await _botStateService._taskSpurApiClient.UpdateToken(stepContext, refreshTokenResponse);


            //}
            if (response.data != null)
            {
                EditTaskRequest editTaskRequest = new EditTaskRequest();
                if (stepContext.Values[Constants.SelectedEditOption] == TaskSpur.Resources.TaskSpur.TaskName)
                {
                    editTaskRequest.name = (string)stepContext.Values[Constants.TaskName];
                    editTaskRequest.priorityId = response.data.priorityId;
                    if (response.data.startOn != null && Convert.ToDateTime(response.data.startOn.date) != DateTime.MinValue)
                        editTaskRequest.startDate = Convert.ToDateTime(response.data.startOn.date).ToString("MM/dd/yyyy").Replace("-", "/");
                    if (response.data.endOn != null && Convert.ToDateTime(response.data.startOn.date) != DateTime.MinValue)
                        editTaskRequest.endDate = Convert.ToDateTime(response.data.endOn.date).ToString("MM/dd/yyyy").Replace("-", "/");
                    if (response.data.startOn.time != null)
                        editTaskRequest.startTime = response.data.startOn.time;
                    if (response.data.endOn.time != null)
                        editTaskRequest.endTime = response.data.endOn.time;
                }
                else if (stepContext.Values[Constants.SelectedEditOption] == TaskSpur.Resources.TaskSpur.Priority)
                {
                    editTaskRequest.name = response.data.name;
                    editTaskRequest.priorityId = (int)stepContext.Values[Constants.PriorityId];
                    if (response.data.startOn != null && Convert.ToDateTime(response.data.startOn.date) != DateTime.MinValue)
                        editTaskRequest.startDate = Convert.ToDateTime(response.data.startOn.date).ToString("MM/dd/yyyy").Replace("-", "/");
                    if (response.data.endOn != null && Convert.ToDateTime(response.data.endOn.date) != DateTime.MinValue)
                        editTaskRequest.endDate = Convert.ToDateTime(response.data.endOn.date).ToString("MM/dd/yyyy").Replace("-", "/");
                    if (response.data.startOn.time != null)
                        editTaskRequest.startTime = response.data.startOn.time;
                    if (response.data.endOn.time != null)
                        editTaskRequest.endTime = response.data.endOn.time;
                }
                else
                {
                    editTaskRequest.name = response.data.name;
                    editTaskRequest.priorityId = response.data.priorityId;
                    if (stepContext.Values.ContainsKey(Constants.StartDate) && Convert.ToDateTime(stepContext.Values[Constants.StartDate]) != DateTime.MinValue)
                        editTaskRequest.startDate = (string)stepContext.Values[Constants.StartDate];
                    if (stepContext.Values.ContainsKey(Constants.EndDate) && Convert.ToDateTime(stepContext.Values[Constants.EndDate]) != DateTime.MinValue)
                        editTaskRequest.endDate = (string)stepContext.Values[Constants.EndDate];
                    if (stepContext.Values.ContainsKey(Constants.StartTime))
                        editTaskRequest.startTime = (string)stepContext.Values[Constants.StartTime];
                    if (stepContext.Values.ContainsKey(Constants.EndTime))
                        editTaskRequest.endTime = (string)stepContext.Values[Constants.EndTime];
                }

                editTaskRequest.taskId = Convert.ToInt32(stepContext.Values[Constants.TaskId]);

                editTaskRequest.description = response.data.description;

                editTaskRequest.goalId = response.data.goalId;
                editTaskRequest.remindMe = response.data.remindMe;
                editTaskRequest.timeZone = Convert.ToString(stepContext.Context.Activity.From.Properties[AriBotV4.Common.Constants.TaskSpurTimeZone]);

                if (stepContext.Values.ContainsKey(Constants.RemindMe))
                    editTaskRequest.remindMe = (bool)stepContext.Values[Constants.RemindMe];
                if (stepContext.Values.ContainsKey(Constants.ReminderTimeId) && stepContext.Values[Constants.ReminderTimeId] !=null)
                    editTaskRequest.reminderTimeId = (int)stepContext.Values[Constants.ReminderTimeId];
                if (stepContext.Values.ContainsKey(Constants.ReminderTime) && stepContext.Values[Constants.ReminderTime] == null)
                {
                    editTaskRequest.reminderTime = 0;
                }
                else if (stepContext.Values.ContainsKey(Constants.ReminderTime))
                {
                    editTaskRequest.reminderTime = (int)stepContext.Values[Constants.ReminderTime];
                }

                // Call get task api
                CreateTaskResponse editResponse = await _botStateService._taskSpurApiClient.UpdateTask(
                 Convert.ToString(stepContext.Context.Activity.From.Properties[Constants.TaskSpurToken]), editTaskRequest);


                //if (editResponse.statusCode == 401)
                //{

                //    // Get refersh token
                //    Models.TaskSpur.Auth.TokenResponse refreshTokenResponse = await _botStateService._taskSpurApiClient.GetRefreshToken(stepContext);

                //    // Create goal with refresh token
                //    if (!string.IsNullOrEmpty(refreshTokenResponse.data.token))
                //    {
                //        editResponse = await _botStateService._taskSpurApiClient.UpdateTask(
                // refreshTokenResponse.data.token, editTaskRequest);
                //        //await stepContext.Context.SendActivityAsync(MessageFactory.Text(response.toast.message));
                //    }

                //    // Update users's from property
                //    await _botStateService._taskSpurApiClient.UpdateToken(stepContext, refreshTokenResponse);


                //}
                if (editResponse != null)
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(editResponse.toast.message));
                }
                else
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(SharedStrings.Sorry), cancellationToken);

                }



            }
            return await stepContext.NextAsync(stepContext, cancellationToken);
        }
        private async Task<DialogTurnResult> FinalAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            await stepContext.EndDialogAsync(null, cancellationToken);
            return await stepContext.BeginDialogAsync($"{nameof(AnythingElseDialog)}.AnythingElse", null, cancellationToken);
        }
        // Validate task name
        private static async Task<bool> ValidateTaskName(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {

            if (string.IsNullOrWhiteSpace(promptContext.Recognized.Value))
            {
                return false;
            }
            else
            {
                return true;
            }
        }


        public static Microsoft.Bot.Schema.Attachment EditTasks(Object data)
        {
            var heroCard = new HeroCard()
            {
                Title = Convert.ToString(data.GetType().GetProperty("name")?.GetValue(data, null)),
                Subtitle = Convert.ToString(data.GetType().GetProperty("description")?.GetValue(data, null)),
                Text = Newtonsoft.Json.JsonConvert.SerializeObject(data),
                Buttons = new List<CardAction>
                    {
                        new CardAction(ActionTypes.PostBack, TaskSpur.Resources.TaskSpur.TaskSpurChoose, value: Convert.ToString(data.GetType().GetProperty("name")?.GetValue(data, null)) + "|"+ Convert.ToString(data.GetType().GetProperty("id")?.GetValue(data, null))),
                    },
                //  Text = data.priority

            };

            return heroCard.ToAttachment();
        }
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

    }
}
