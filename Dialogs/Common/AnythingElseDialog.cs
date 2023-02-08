using AriBotV4.Common;
using AriBotV4.Dialogs.Common;
using AriBotV4.Dialogs.Common.Resources;
using AriBotV4.Dialogs.TaskSpur;
using AriBotV4.Enums;
using AriBotV4.Models;
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

namespace AriBotV4.Dialogs
{
    public class AnythingElseDialog : ComponentDialog
    {

        #region Properties and Fields
        private readonly BotStateService _botStateService;
        #endregion


        #region Method
        public AnythingElseDialog(string dialogId, BotStateService botStateService) : base(dialogId)
        {
            _botStateService = botStateService ?? throw new System.ArgumentNullException(nameof(botStateService));

            InitializeWaterfallDialog();
        }

        private void InitializeWaterfallDialog()
        {
            // Create Waterfall Steps
            var waterfallSteps = new WaterfallStep[]
            {
               // ConfirmContinueSameAction,
                //Confirmed,
                Step1Async,
                Step2Async,
               // StepEndAsync
            };

            // Add Named Dialogs
            AddDialog(new WaterfallDialog($"{nameof(AnythingElseDialog)}.mainFlow", waterfallSteps));
            AddDialog(new TextPrompt($"{nameof(AnythingElseDialog)}.anythingElse"));
            AddDialog(new ChoicePrompt($"{nameof(AnythingElseDialog)}.continueAction"));

            // Set the starting Dialog
            InitialDialogId = $"{nameof(AnythingElseDialog)}.mainFlow";
        }

        
            // Waterfall steps
            private async Task<DialogTurnResult> Step1Async(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            
            UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
            userProfile.LastMessageReceived = DateTime.UtcNow;
            await _botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);

            var opts = new PromptOptions
            {
                Prompt = new Microsoft.Bot.Schema.Activity
                {

                    Type = ActivityTypes.Message,
                    Text = Utility.GenerateRandomMessages(Constants.AnythingElse),
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
            return await stepContext.PromptAsync($"{nameof(AnythingElseDialog)}.anythingElse", opts);

            
        }

        private async Task<DialogTurnResult> Step2Async(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
            userProfile.LastMessageReceived = DateTime.UtcNow;
            await _botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);

            Type stringType = typeof(System.String);
            Type ChoiceType = typeof(System.Boolean);

            Type resultType = stepContext.Result.GetType();
            string selectedChoice = (resultType == stringType ? Convert.ToString(stepContext.Result) :
            Convert.ToString(((FoundChoice)stepContext.Result)));

            if (Constants.YesLibrary.Any(str => str.ToLower() == (selectedChoice.ToLower())))
            //if (selectedChoice.Contains(SharedStrings.ConfirmYes))
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(TaskSpur.Resources.TaskSpur.GetTaskInput), cancellationToken);
                
               // return await stepContext.BeginDialogAsync($"{nameof(AskAriDialog)}.mainFlow", null, cancellationToken);
                return await stepContext.BeginDialogAsync($"{nameof(RootDialog)}.contactOptions", null, cancellationToken);
            }
            else if (Constants.NoLibrary.Any(str => str.ToLower() == (selectedChoice.ToLower())))
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(Utility.GenerateRandomMessages(Constants.GoodByeLibrary)), cancellationToken);

                //await stepContext.Context.SendActivityAsync(MessageFactory.Text(SharedStrings.Goodbye), cancellationToken);
                return await stepContext.EndDialogAsync(null, cancellationToken);

            }
            else
            {
                await stepContext.EndDialogAsync(null, cancellationToken);
                return await stepContext.BeginDialogAsync($"{nameof(RootDialog)}.contactOptions", stepContext.Context.Activity.Text, cancellationToken);
            }
        }
            
        private async Task<DialogTurnResult> StepEndAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
            userProfile.LastMessageReceived = DateTime.UtcNow;
            await _botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);
           
            return await stepContext.EndDialogAsync(null, cancellationToken);
        }

        #endregion 
    }
}
