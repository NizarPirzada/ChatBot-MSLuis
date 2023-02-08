using AriBotV4.Common;
using AriBotV4.Dialogs.Common.Resources;
using AriBotV4.Enums;
using AriBotV4.Models;
using AriBotV4.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AriBotV4.Dialogs.TaskSpur
{
    public class TaskSpurDialog : ComponentDialog
    {
        #region Properties and Fields
        private static BotStateService _botStateService;
        private readonly BotServices _botServices;
        private readonly IConfiguration _configuration;
        private LuisModel luisResponse;
        #endregion


        #region WaterFallSteps and Dialogs
        public TaskSpurDialog(string dialogId, BotStateService botStateService, BotServices botServices, IConfiguration configuration) : base(dialogId)
        {
            _botStateService = botStateService ?? throw new System.ArgumentNullException(nameof(botStateService));
            _botServices = botServices ?? throw new System.ArgumentNullException(nameof(botServices));
            _configuration = configuration ?? throw new System.ArgumentNullException(nameof(configuration));
            InitializeWaterfallDialog();
        }

        // Initializing waterfall steps
        private void InitializeWaterfallDialog()
        {
            // Create Waterfall Steps
            var waterfallSteps = new WaterfallStep[]
            {
                //TaskSpurSignInOptions,
                TaskSpurOptions,
                ChoiceResultStepAsync,
                ManageTasks
             //   FinalAsync
            };


            // Add Named Dialogs
            AddDialog(new WaterfallDialog($"{nameof(TaskSpurDialog)}.mainFlow", waterfallSteps));
            AddDialog(new ChoicePrompt($"{nameof(TaskSpurDialog)}.signInOptions"));
            AddDialog(new ChoicePrompt($"{nameof(TaskSpurDialog)}.options"));
            AddDialog(new CreateGoalDialog($"{nameof(CreateGoalDialog)}.mainFlow", _botStateService));
            AddDialog(new CreateTaskDialog($"{nameof(CreateTaskDialog)}.mainFlow", _botStateService, _botServices));
            AddDialog(new GetTasksDialog($"{nameof(GetTasksDialog)}.mainFlow", _botStateService, _botServices));
            AddDialog(new TextPrompt($"{nameof(TaskSpurDialog)}.userInput"));
            AddDialog(new EditTaskDialog($"{nameof(EditTaskDialog)}.mainFlow", _botStateService, _botServices));
            AddDialog(new DeleteTaskDialog($"{nameof(DeleteTaskDialog)}.mainFlow", _botStateService, _botServices));
            AddDialog(new DeleteGoalDialog($"{nameof(DeleteGoalDialog)}.mainFlow", _botStateService));
            AddDialog(new EditGoalDialog($"{nameof(EditGoalDialog)}.mainFlow", _botStateService));
            // Set the starting Dialog
            InitialDialogId = $"{nameof(TaskSpurDialog)}.mainFlow";

        }

        // Step 1 check user is new or already registered
        private async Task<DialogTurnResult> TaskSpurSignInOptions(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
                       
            UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
            if (string.IsNullOrEmpty(Convert.ToString(stepContext.Context.Activity.From.Properties[Constants.TaskSpurToken])))
            {
                var chooseDeals = new PromptOptions
                {
                    Prompt = new Activity
                    {

                        Type = ActivityTypes.Message,
                        SuggestedActions = new SuggestedActions()
                        {
                            Actions = new List<CardAction>()
                           {

                                new CardAction(ActionTypes.OpenUrl,(TaskSpur.Resources.TaskSpur.TaskSpurInfo),value
                                :_botStateService._taskSpurSettings.TaskSpur),

                                new CardAction(ActionTypes.OpenUrl,(TaskSpur.Resources.TaskSpur.SignIn),value
                                :_botStateService._taskSpurSettings.LoginURL),


                                new CardAction(ActionTypes.OpenUrl,(TaskSpur.Resources.TaskSpur.SignUp),value
                                :_botStateService._taskSpurSettings.SignUpURL),

                               },
                        },

                    }

                };

                return await stepContext.PromptAsync($"{nameof(TaskSpurDialog)}.signInOptions", chooseDeals);
            }
            else
            {
                return await stepContext.NextAsync(stepContext.Context.Activity.Text, cancellationToken);

            }
        }



        // Step 1 ask user for taskspur option to search
        private async Task<DialogTurnResult> TaskSpurOptions(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {



            List<string> choice = new List<string>();
            if (_configuration.GetValue<bool>("TaskSpurToggleSettings:CreateGoals"))
                choice.Add(TaskSpur.Resources.TaskSpur.CreateGoals);
            if (_configuration.GetValue<bool>("TaskSpurToggleSettings:CreateTasks"))
                choice.Add(TaskSpur.Resources.TaskSpur.CreateTasks);
            if (_configuration.GetValue<bool>("TaskSpurToggleSettings:GetTasks"))
                choice.Add(TaskSpur.Resources.TaskSpur.Tasks);
            if (_configuration.GetValue<bool>("TaskSpurToggleSettings:Goals"))
                choice.Add(TaskSpur.Resources.TaskSpur.Goals);
            // if (_configuration.GetValue<bool>("TaskSpurToggleSettings:Calendar"))
            //choice.Add(TaskSpur.Resources.TaskSpur.GetTasks);


            var prompt = new PromptOptions
            {


                Choices = ChoiceFactory.ToChoices(choice
            )
            };
            return await stepContext.PromptAsync($"{nameof(TaskSpurDialog)}.options",
                prompt, cancellationToken);
        }

        // Step 2 get user selected option
        private async Task<DialogTurnResult> ChoiceResultStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var selectedChoice = ((FoundChoice)stepContext.Result).Value;
            // var selectedChoice = ((string)stepContext.Result);
            UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
            userProfile.Subject = selectedChoice;
            userProfile.LastMessageReceived = DateTime.UtcNow;
            await _botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);

            if (selectedChoice.Contains(TaskSpur.Resources.TaskSpur.CreateGoals))
            {
                return await stepContext.BeginDialogAsync($"{nameof(CreateGoalDialog)}.mainFlow", null, cancellationToken);
            }
            if (selectedChoice.Contains(TaskSpur.Resources.TaskSpur.CreateTasks))
            {
                return await stepContext.BeginDialogAsync($"{nameof(CreateTaskDialog)}.mainFlow", null, cancellationToken);
            }
            else if (selectedChoice.Contains(TaskSpur.Resources.TaskSpur.Tasks))
            {

                return await stepContext.PromptAsync($"{nameof(TaskSpurDialog)}.userInput",
                        new PromptOptions
                        {
                            Prompt = MessageFactory.Text(Resources.TaskSpur.GetTaskInput +Constants.HtmlBr  + "Some examples:" + Constants.HtmlBr +
                          Utility.GenerateRandomMessages(Constants.TasksCreateSample) + Constants.HtmlBr +  Utility.GenerateRandomMessages(Constants.TasksEditSample) + Constants.HtmlBr +  Utility.GenerateRandomMessages(Constants.CalendarTasksSample)),

                        }, cancellationToken);

            }
            else if (selectedChoice.Contains(TaskSpur.Resources.TaskSpur.Goals))
            {
                return await stepContext.PromptAsync($"{nameof(TaskSpurDialog)}.userInput",
                      new PromptOptions
                      {
                          Prompt = MessageFactory.Text(Resources.TaskSpur.GetTaskInput + Constants.HtmlBr +  "Some examples:" + Constants.HtmlBr + 
                        Utility.GenerateRandomMessages(Constants.GoalCreateSample) + Constants.HtmlBr  + Utility.GenerateRandomMessages(Constants.GoalEditSample) + Constants.HtmlBr +  Utility.GenerateRandomMessages(Constants.GoalDeleteSample)),

                      }, cancellationToken);

                //await stepContext.Context.SendActivityAsync(SharedStrings.StillInProgress);

                //return await stepContext.BeginDialogAsync($"{nameof(AnythingElseDialog)}.AnythingElse", null, cancellationToken);
            }

            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(SharedStrings.Sorry), cancellationToken);
            }
            return await stepContext.NextAsync(null, cancellationToken);

        }

        private async Task<DialogTurnResult> ManageTasks(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Context.Activity.Text != SharedStrings.ConfirmNo)
            {
                //return await stepContext.BeginDialogAsync($"{nameof(DeleteTaskDialog)}.mainFlow", null, cancellationToken);
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

                // Top intent tell us which cognitive service to use
                if (luisResponse != null)
                    topIntent = luisResponse.TopIntent().intent;

                if (recognizerResult.Entities.Count > 1)
                {
                    //To continue, must be time Intentl
                    if ( luisResponse.Entities._instance.edit != null && luisResponse.Entities._instance.tasks != null)
                    {
                        return await stepContext.BeginDialogAsync($"{nameof(EditTaskDialog)}.mainFlow", null, cancellationToken);

                    }
                    else if (luisResponse.Entities._instance.create != null && luisResponse.Entities._instance.tasks != null)
                    {
                        return await stepContext.BeginDialogAsync($"{nameof(CreateTaskDialog)}.mainFlow", null, cancellationToken);
                    }
                    else if (luisResponse.Entities._instance.delete != null && luisResponse.Entities._instance.tasks != null)
                    {

                        return await stepContext.BeginDialogAsync($"{nameof(DeleteTaskDialog)}.mainFlow", null, cancellationToken);
                    }
                    else if ((luisResponse.Entities._instance.tasks != null || luisResponse.Entities._instance.appointment != null || luisResponse.Entities._instance.next != null))
                    {
                        return await stepContext.BeginDialogAsync($"{nameof(GetTasksDialog)}.mainFlow", null, cancellationToken);
                    }
                    else if (luisResponse.Entities._instance.create != null && luisResponse.Entities._instance.goal != null)
                    {
                        return await stepContext.BeginDialogAsync($"{nameof(CreateGoalDialog)}.mainFlow", null, cancellationToken);
                    }
                    else if (luisResponse.Entities._instance.edit != null && luisResponse.Entities._instance.goal != null)
                    {
                        return await stepContext.BeginDialogAsync($"{nameof(EditGoalDialog)}.mainFlow", null, cancellationToken);
                    }
                    else if (luisResponse.Entities._instance.delete != null && luisResponse.Entities._instance.goal != null)
                    {
                        return await stepContext.BeginDialogAsync($"{nameof(DeleteGoalDialog)}.mainFlow", null, cancellationToken);
                    }
                    else
                    {

                        await stepContext.Context.SendActivityAsync(SharedStrings.NotFound);
                        return await stepContext.BeginDialogAsync($"{nameof(AnythingElseDialog)}.AnythingElse", null, cancellationToken);
                    }
                }
                else
                {
                    await stepContext.Context.SendActivityAsync(SharedStrings.NotFound);
                    return await stepContext.BeginDialogAsync($"{nameof(AnythingElseDialog)}.AnythingElse", null, cancellationToken);
                }
            }
            return await stepContext.NextAsync(null, cancellationToken);
        }
        // Step 3 close the current dialog
        private async Task<DialogTurnResult> FinalAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
            userProfile.LastMessageReceived = DateTime.UtcNow;
            await _botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);
            return null;
        }


        #endregion
    }
}
