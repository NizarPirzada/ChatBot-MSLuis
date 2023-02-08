using AriBotV4.Common;
using AriBotV4.Dialogs.Common.Resources;
using AriBotV4.Enums;
using AriBotV4.Models.TaskSpur.Goals.Delete;
using AriBotV4.Models.TaskSpur.Goals.Get;
using AriBotV4.Models.TaskSpur.Goals.Id;
using AriBotV4.Models.TaskSpur.Tasks;
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
    public class EditGoalDialog : ComponentDialog
    {
        #region Properties and Fields
        private readonly BotStateService _botStateService;
        private readonly BotServices _botServices;

        #endregion

        public EditGoalDialog(string dialogId, BotStateService botStateService) : base(dialogId)
        {
            _botStateService = botStateService ?? throw new System.ArgumentNullException(nameof(botStateService));
            

            InitializeWaterfallDialog();
        }

        private void InitializeWaterfallDialog()
        {
            //Create Waterfall Steps
            var waterfallSteps = new WaterfallStep[]
            {
                AskGoalName,
                GetGoals,
                EditGoalOptions,
                SelectEditGoalOption,
                UpdateGoal,
                FinalAsync

            };
            AddDialog(new WaterfallDialog($"{nameof(EditGoalDialog)}.mainFlow", waterfallSteps));
            AddDialog(new TextPrompt($"{nameof(EditGoalDialog)}.name", DeleteGoalDialog.ValidateGoalName));
            AddDialog(new ChoicePrompt($"{nameof(EditGoalDialog)}.editGoalOption"));
            AddDialog(new ChoicePrompt($"{nameof(EditGoalDialog)}.category"));
            AddDialog(new ChoicePrompt($"{nameof(EditGoalDialog)}.confirmDelete"));
            AddDialog(new TextPrompt($"{nameof(EditGoalDialog)}.editGoalName"));
            AddDialog(new CommonDialog($"{nameof(CommonDialog)}.mainFlow", _botStateService));
            InitialDialogId = $"{nameof(EditGoalDialog)}.mainFlow";

        }


        public async Task<DialogTurnResult> AskGoalName(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            

            if (string.IsNullOrEmpty(Convert.ToString(stepContext.Context.Activity.From.Properties[Constants.TaskSpurToken])))
            {
                return await stepContext.BeginDialogAsync($"{nameof(CommonDialog)}.mainFlow", null, cancellationToken);
            }
            else
            {
                return await stepContext.PromptAsync($"{nameof(EditGoalDialog)}.name",
                     new PromptOptions
                     {
                         Prompt = MessageFactory.Text(TaskSpur.Resources.TaskSpur.EditGoalName),
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
                        reply.Attachments.Add(EditGoal(response.data.data[i]));
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

        public async Task<DialogTurnResult> EditGoalOptions(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((string)stepContext.Result == SharedStrings.SearchAgain)
            {
                return await stepContext.ReplaceDialogAsync(InitialDialogId);
            }
            else if((string)stepContext.Result == SharedStrings.Exit)
            {
                await stepContext.EndDialogAsync(null, cancellationToken);
                return await stepContext.BeginDialogAsync($"{nameof(AnythingElseDialog)}.AnythingElse", null, cancellationToken);
            }
            else
            {
                stepContext.Values[Constants.GoalId] = (string)stepContext.Result.ToString().Split("|")[1];
                var edit = new PromptOptions
                {
                    Prompt = MessageFactory.Text(TaskSpur.Resources.TaskSpur.EditTaskOption),
                    Choices = ChoiceFactory.ToChoices(new List<string>
                {
                        TaskSpur.Resources.TaskSpur.TaskName,
                        TaskSpur.Resources.TaskSpur.GoalCategory

                })
                };

                return await stepContext.PromptAsync($"{nameof(EditGoalDialog)}.editGoalOption", edit, cancellationToken);
            }


        }
        public async Task<DialogTurnResult> SelectEditGoalOption(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            var selectedChoice = ((FoundChoice)stepContext.Result).Value;
            stepContext.Values[Constants.SelectedEditOption] = selectedChoice;
            if (selectedChoice == TaskSpur.Resources.TaskSpur.TaskName)
            {
                return await stepContext.PromptAsync($"{nameof(EditGoalDialog)}.editGoalName",
                     new PromptOptions
                     {
                         Prompt = MessageFactory.Text(TaskSpur.Resources.TaskSpur.EditGoalNewname),

                     }, cancellationToken);

            }
            else 
            {

                var prompt = new PromptOptions
                {
                    Prompt = MessageFactory.Text(TaskSpur.Resources.TaskSpur.EditGoalCategory),
                    Choices = ChoiceFactory.ToChoices(new List<string> {
                   EnumHelpers.GetEnumDescription(AriBotV4.Enums.GoalCategoryEnum.Funds),
                        EnumHelpers.GetEnumDescription(AriBotV4.Enums.GoalCategoryEnum.PersonalLife),
                        EnumHelpers.GetEnumDescription(AriBotV4.Enums.GoalCategoryEnum.SelfCareAndWellness),
                        EnumHelpers.GetEnumDescription(AriBotV4.Enums.GoalCategoryEnum.WorkAndCareer),
                    }),
                    Style = ListStyle.SuggestedAction,

                };

                return await stepContext.PromptAsync($"{nameof(EditGoalDialog)}.category", prompt, cancellationToken);

            }
            

        }

        public async Task<DialogTurnResult> UpdateGoal(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
                        // Call get task api
            GetGoalByIdResponse response = await _botStateService._taskSpurApiClient.GetGoalById(
                Convert.ToString(stepContext.Context.Activity.From.Properties[Constants.TaskSpurToken]), Convert.ToInt32(stepContext.Values[Constants.GoalId]));

            
            // Check token authrozation
            //if (response.statusCode == 401)
            //{

            //    // Get refersh token
            //    Models.TaskSpur.Auth.TokenResponse refreshTokenResponse = await _botStateService._taskSpurApiClient.GetRefreshToken(stepContext);

            //    // Create goal with refresh token
            //    if (!string.IsNullOrEmpty(refreshTokenResponse.data.token))
            //    {
            //        response = await _botStateService._taskSpurApiClient.GetGoalById(
            //    refreshTokenResponse.data.token, Convert.ToInt32(stepContext.Values[Constants.GoalId]));
            //        //await stepContext.Context.SendActivityAsync(MessageFactory.Text(response.toast.message));
            //    }

            //    // Update users's from property
            //    await _botStateService._taskSpurApiClient.UpdateToken(stepContext, refreshTokenResponse);


            //}

            if (response.data != null)
            {
                EditGoalRequest editGoalRequest = new EditGoalRequest();
                if (stepContext.Values[Constants.SelectedEditOption] == TaskSpur.Resources.TaskSpur.TaskName)
                {
                    editGoalRequest.name = (string)stepContext.Result;
                    editGoalRequest.categoryId = response.data.categoryId;

                }
                else
                {
                    editGoalRequest.name = response.data.name;
                    editGoalRequest.categoryId = (int)EnumHelpers.GetValueFromDescription<AriBotV4.Enums.GoalCategoryEnum>(((FoundChoice)stepContext.Result).Value);
                  //  editGoalRequest.categoryId = (int)Enum.Parse<AriBotV4.Enums.GoalCategoryEnum>(((FoundChoice)stepContext.Result).Value.ToString());
                
                }


                editGoalRequest.id = Convert.ToInt32(stepContext.Values[Constants.GoalId]);
                if(string.IsNullOrEmpty(response.data.description))
                editGoalRequest.description = string.Empty;
                else
                    editGoalRequest.description = response.data.description;




                // Call get task api
                DeleteGoalResponse editResponse = await _botStateService._taskSpurApiClient.UpdateGoal(
                 Convert.ToString(stepContext.Context.Activity.From.Properties[Constants.TaskSpurToken]), editGoalRequest);
                

                //if (editResponse.statusCode == 401)
                
                //{

                //    // Get refersh token
                //    Models.TaskSpur.Auth.TokenResponse refreshTokenResponse = await _botStateService._taskSpurApiClient.GetRefreshToken(stepContext);

                //    // Create goal with refresh token
                //    if (!string.IsNullOrEmpty(refreshTokenResponse.data.token))
                //    {
                //        editResponse = await _botStateService._taskSpurApiClient.UpdateGoal(
                // Convert.ToString(stepContext.Context.Activity.From.Properties[Constants.TaskSpurToken]), editGoalRequest);
                        
                //    }

                //    // Update users's from property
                //    await _botStateService._taskSpurApiClient.UpdateToken(stepContext, refreshTokenResponse);


                //}
                
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(editResponse.toast.message));



            }
            return await stepContext.NextAsync(stepContext, cancellationToken);

        }

        private async Task<DialogTurnResult> FinalAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            await stepContext.EndDialogAsync(null, cancellationToken);
            return await stepContext.BeginDialogAsync($"{nameof(AnythingElseDialog)}.AnythingElse", null, cancellationToken);
        }
        public static Microsoft.Bot.Schema.Attachment EditGoal(Object data)
        {
            if (Convert.ToString(data.GetType().GetProperty("system")?.GetValue(data, null)) != "False")
            {
                var heroCard = new HeroCard()
                {
                    Title = Convert.ToString(data.GetType().GetProperty("name")?.GetValue(data, null)),
                    Subtitle = TaskSpur.Resources.TaskSpur.GoalNotEditable,
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
    }

   
}
