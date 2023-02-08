using AriBotV4.Common;
using AriBotV4.Dialogs.Common.Resources;
using AriBotV4.Models.TaskSpur;
using AriBotV4.Models.TaskSpur.Goals.Delete;
using AriBotV4.Models.TaskSpur.Goals.Get;
using AriBotV4.Models.TaskSpur.Goals.Id;
using AriBotV4.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AriBotV4.Dialogs.TaskSpur
{
    public class DeleteGoalDialog : ComponentDialog
    {
        #region Properties and Fields
        private readonly BotStateService _botStateService;
        private readonly BotServices _botServices;


        #endregion

        public DeleteGoalDialog(string dialogId, BotStateService botStateService) : base(dialogId)
        {
            _botStateService = botStateService ?? throw new System.ArgumentNullException(nameof(botStateService));
            //_botServices = botServices ?? throw new System.ArgumentNullException(nameof(botStateService));

            InitializeWaterfallDialog();
        }
        private void InitializeWaterfallDialog()
        {
            //Create Waterfall Steps
            var waterfallSteps = new WaterfallStep[]
            {
                AskGoalName,
                GetGoals,
                ConfirmDelete,
                DeleteTask,
                FinalAsync

            };
            AddDialog(new WaterfallDialog($"{nameof(DeleteGoalDialog)}.mainFlow", waterfallSteps));
            AddDialog(new TextPrompt($"{nameof(DeleteGoalDialog)}.name", ValidateGoalName));
            AddDialog(new TextPrompt($"{nameof(DeleteGoalDialog)}.editTaskName"));
            AddDialog(new ChoicePrompt($"{nameof(DeleteGoalDialog)}.confirmDelete"));
            AddDialog(new CommonDialog($"{nameof(CommonDialog)}.mainFlow", _botStateService));
            InitialDialogId = $"{nameof(DeleteGoalDialog)}.mainFlow";

        }   

        public async Task<DialogTurnResult> AskGoalName(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            
            if (string.IsNullOrEmpty(Convert.ToString(stepContext.Context.Activity.From.Properties[Constants.TaskSpurToken])))
            {
                return await stepContext.BeginDialogAsync($"{nameof(CommonDialog)}.mainFlow", null, cancellationToken);
            }
            else
            {
                return await stepContext.PromptAsync($"{nameof(DeleteGoalDialog)}.name",
                     new PromptOptions
                     {
                         Prompt = MessageFactory.Text(TaskSpur.Resources.TaskSpur.DeleteGoalName),
                         RetryPrompt = MessageFactory.Text(TaskSpur.Resources.TaskSpur.GoalEmpty)
                     }, cancellationToken);
            }
        }
        public async Task<DialogTurnResult> GetGoals(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            
            // Call get task api
            GetGoalsResponse response = await _botStateService._taskSpurApiClient.GetGoals(
                Convert.ToString(stepContext.Context.Activity.From.Properties[Constants.TaskSpurToken]), (string)stepContext.Result);
            // Check token authrozation
            //if (response.statusCode == 401)
            //{

            //    // Get refersh token
            //    Models.TaskSpur.Auth.TokenResponse refreshTokenResponse = await _botStateService._taskSpurApiClient.GetRefreshToken(stepContext);

            //    // Create goal with refresh token
            //    if (!string.IsNullOrEmpty(refreshTokenResponse.data.token))
            //    {
            //        response = await _botStateService._taskSpurApiClient.GetGoals(
            //    refreshTokenResponse.data.token);

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
                    for (int i = 0; i <= response.data.data.Count - 1; i++)
                    {
                        reply.Attachments.Add(DisplayGoal(response.data.data[i]));
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
            {

                await stepContext.Context.SendActivityAsync(MessageFactory.Text(response.toast.message));
            }

            await stepContext.Context.SendActivityAsync(MessageFactory.Text(TaskSpur.Resources.TaskSpur.CannotFindGoal));
            await stepContext.Context.SendActivityAsync(MessageFactory.SuggestedActions(new CardAction[]
        {
        new CardAction(title: SharedStrings.SearchAgain, type: ActionTypes.PostBack, value: SharedStrings.SearchAgain),
        new CardAction(title: SharedStrings.Exit, type: ActionTypes.PostBack, value: SharedStrings.Exit),
               }));


          
            return new DialogTurnResult(DialogTurnStatus.Waiting);
        }


        public async Task<DialogTurnResult> ConfirmDelete(WaterfallStepContext stepContext, CancellationToken cancellationToken)
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
                stepContext.Values[Constants.GoalId] = (string)stepContext.Result.ToString().Split("|")[1];

                return await stepContext.PromptAsync($"{nameof(DeleteGoalDialog)}.confirmDelete",
                    new PromptOptions
                    {
                        Style = ListStyle.SuggestedAction,
                        Prompt = MessageFactory.Text(TaskSpur.Resources.TaskSpur.ConfirmGoalDelete),
                        Choices = ChoiceFactory.ToChoices(new List<string>
                        {
                       SharedStrings.ConfirmYes,
                       SharedStrings.ConfirmNo
                        }),
                    }, cancellationToken);
            }


        }
        public async Task<DialogTurnResult> DeleteTask(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            
            var selectedChoice = ((FoundChoice)stepContext.Result).Value;

            if (selectedChoice.Contains(SharedStrings.ConfirmYes))
            {

                // delete goal
                DeleteGoalResponse response = await _botStateService._taskSpurApiClient.DeleteGoalById(
                 Convert.ToString(stepContext.Context.Activity.From.Properties[Constants.TaskSpurToken]), Convert.ToInt32(stepContext.Values[Constants.GoalId]));
                //if (response.statusCode == 401)
                //{

                //    // Get refersh token
                //    Models.TaskSpur.Auth.TokenResponse refreshTokenResponse = await _botStateService._taskSpurApiClient.GetRefreshToken(stepContext);

                //    // Create goal with refresh token
                //    if (!string.IsNullOrEmpty(refreshTokenResponse.data.token))
                //    {
                //        response = await _botStateService._taskSpurApiClient.DeleteGoalById(
                // refreshTokenResponse.data.token, Convert.ToInt32(stepContext.Values[Constants.TaskId]));
                //        //await stepContext.Context.SendActivityAsync(MessageFactory.Text(response.toast.message));
                //    }

                //    // Update users's from property
                //    await _botStateService._taskSpurApiClient.UpdateToken(stepContext, refreshTokenResponse);


                //}

                if (response.data != null)
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(response.toast.message));
                }

            }
            return await stepContext.NextAsync(stepContext, cancellationToken);



        }
        private async Task<DialogTurnResult> FinalAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            await stepContext.EndDialogAsync(null, cancellationToken);
            return await stepContext.BeginDialogAsync($"{nameof(AnythingElseDialog)}.AnythingElse", null, cancellationToken);
        }


        public static Microsoft.Bot.Schema.Attachment DisplayGoal(Object data)
        {
            if (Convert.ToString(data.GetType().GetProperty("system")?.GetValue(data, null)) != "False")
            {
                var heroCard = new HeroCard()
                {
                    Title = Convert.ToString(data.GetType().GetProperty("name")?.GetValue(data, null)),
                    Subtitle = TaskSpur.Resources.TaskSpur.GoalNotDeletable,
                    Text = Newtonsoft.Json.JsonConvert.SerializeObject(data),

                    //  Text = data.priority

                };

                return heroCard.ToAttachment();
            }
            else
            {
                var heroCard = new HeroCard()
                {
                    Title = Convert.ToString(data.GetType().GetProperty("name")?.GetValue(data, null)),
                    //Subtitle = Convert.ToString(data.GetType().GetProperty("description")?.GetValue(data, null)),
                    Text = Newtonsoft.Json.JsonConvert.SerializeObject(data),
                    Buttons = new List<CardAction>
                    {
                        new CardAction(ActionTypes.PostBack, TaskSpur.Resources.TaskSpur.TaskSpurChoose, value: Convert.ToString(data.GetType().GetProperty("name")?.GetValue(data, null)) + "|"+ Convert.ToString(data.GetType().GetProperty("id")?.GetValue(data, null))),

                    },
                    //  Text = data.priority

                };

                return heroCard.ToAttachment();
            }
        }

        // Validaste task name
        public static async Task<bool> ValidateGoalName(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
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
    }
}
