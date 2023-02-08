using AriBotV4.Common;
using AriBotV4.Dialogs.Common.Resources;
using AriBotV4.Models.TaskSpur;
using AriBotV4.Models.TaskSpur.Tasks.Delete;
using AriBotV4.Models.TaskSpur.Tasks.Id;
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
    public class DeleteTaskDialog : ComponentDialog
    {
        #region Properties and Fields
        private readonly BotStateService _botStateService;
        private readonly BotServices _botServices;


        #endregion

        public DeleteTaskDialog(string dialogId, BotStateService botStateService, BotServices botServices) : base(dialogId)
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
                ConfirmDelete,
                DeleteTask,
                FinalAsync

            };
            AddDialog(new WaterfallDialog($"{nameof(DeleteTaskDialog)}.mainFlow", waterfallSteps));
            AddDialog(new TextPrompt($"{nameof(DeleteTaskDialog)}.name", ValidateTaskName));
            AddDialog(new TextPrompt($"{nameof(DeleteTaskDialog)}.editTaskName"));
            AddDialog(new ChoicePrompt($"{nameof(DeleteTaskDialog)}.confirmDelete"));
            AddDialog(new CommonDialog($"{nameof(CommonDialog)}.mainFlow", _botStateService));
            InitialDialogId = $"{nameof(DeleteTaskDialog)}.mainFlow";
            
        }

        public async Task<DialogTurnResult> AskTaskName(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            
            if (string.IsNullOrEmpty(Convert.ToString(stepContext.Context.Activity.From.Properties[Constants.TaskSpurToken])))
            {
                return await stepContext.BeginDialogAsync($"{nameof(CommonDialog)}.mainFlow", null, cancellationToken);
            }
            else
            {
                return await stepContext.PromptAsync($"{nameof(DeleteTaskDialog)}.name",
                     new PromptOptions
                     {
                         Prompt = MessageFactory.Text(TaskSpur.Resources.TaskSpur.DeleteTaskName),
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
                        reply.Attachments.Add(EditTaskDialog.EditTasks(response.data.data[0].toDoTasks[i]));

                    }
                    for (int i = 0; i <= response.data.data[0].doingTasks.Count - 1; i++)
                    {
                        reply.Attachments.Add(EditTaskDialog.EditTasks(response.data.data[0].doingTasks[i]));

                    }
                    for (int i = 0; i <= response.data.data[0].laterTasks.Count - 1; i++)
                    {
                        reply.Attachments.Add(EditTaskDialog.EditTasks(response.data.data[0].laterTasks[i]));

                    }
                    for (int i = 0; i <= response.data.data[0].archivedTasks.Count - 1; i++)
                    {
                        reply.Attachments.Add(EditTaskDialog.EditTasks(response.data.data[0].archivedTasks[i]));

                    }
                    for (int i = 0; i <= response.data.data[0].doneTasks.Count - 1; i++)
                    {
                        reply.Attachments.Add(EditTaskDialog.EditTasks(response.data.data[0].doneTasks[i]));

                    }
                    for (int i = 0; i <= response.data.data[0].unscheduledTasks.Count - 1; i++)
                    {
                        reply.Attachments.Add(EditTaskDialog.EditTasks(response.data.data[0].unscheduledTasks[i]));

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

            await stepContext.Context.SendActivityAsync(MessageFactory.Text(TaskSpur.Resources.TaskSpur.CannotFindTask));
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
                stepContext.Values[Constants.TaskId] = (string)stepContext.Result.ToString().Split("|")[1];

                return await stepContext.PromptAsync($"{nameof(DeleteTaskDialog)}.confirmDelete",
                    new PromptOptions
                    {
                        Style = ListStyle.SuggestedAction,
                        Prompt = MessageFactory.Text(TaskSpur.Resources.TaskSpur.ConfirmDelete),
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

                // delete task
                DeleteTaskResponse response = await _botStateService._taskSpurApiClient.DeleteTaskById(
                 Convert.ToString(stepContext.Context.Activity.From.Properties[Constants.TaskSpurToken]), Convert.ToInt32(stepContext.Values[Constants.TaskId]));
                //if (response.statusCode == 401)
                //{

                //    // Get refersh token
                //    Models.TaskSpur.Auth.TokenResponse refreshTokenResponse = await _botStateService._taskSpurApiClient.GetRefreshToken(stepContext);

                //    // Create goal with refresh token
                //    if (!string.IsNullOrEmpty(refreshTokenResponse.data.token))
                //    {
                //        response = await _botStateService._taskSpurApiClient.DeleteTaskById(
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

        // Validaste task name
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
    }
}
