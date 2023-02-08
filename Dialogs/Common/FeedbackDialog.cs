using AriBotV4.Services;
using AriBotV4.Helpers;
using AriBotV4.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AriBotV4.Dialogs.Common.Resources;
using System;
using AriBotV4.Models.AriQuestions;
using AriBotV4.Common;

namespace AriBotV4.Dialogs
{
    public class FeedbackDialog : ComponentDialog
    {
        #region Properties and Field
        private readonly BotStateService _botStateService;
        private AriQuestionResponse _ariQuestionResponse;

        public FeedbackDialog(string dialogId, BotStateService botStateService) : base(dialogId)
        {
            _botStateService = botStateService ?? throw new System.ArgumentNullException(nameof(botStateService));

            InitializeWaterfallDialog();
        }
        #endregion

        #region Method
        private void InitializeWaterfallDialog()
        {
            // Create Waterfall Steps
            var waterfallSteps = new WaterfallStep[]
            {
                Step1Async,
                Step2Async,
                Step3Async,
                StepEndAsync
            };

            // Add Named Dialogs
            AddDialog(new WaterfallDialog($"{nameof(FeedbackDialog)}.mainFlow", waterfallSteps));
            AddDialog(new TextPrompt($"{nameof(FeedbackDialog)}.details"));
            AddDialog(new ContactProfilingDialog($"{nameof(FeedbackDialog)}.contactProfiling", _botStateService));
            AddDialog(new AnythingElseDialog($"{nameof(AnythingElseDialog)}.AnythingElse", _botStateService));

            // Set the starting Dialog
            InitialDialogId = $"{nameof(FeedbackDialog)}.mainFlow";
        }

        // Waterfall steps
        private async Task<DialogTurnResult> Step1Async(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if((AriQuestionResponse)stepContext.ActiveDialog.State["options"] != null)
            _ariQuestionResponse = (AriQuestionResponse)stepContext.ActiveDialog.State["options"];

            UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
            userProfile.LastMessageReceived = DateTime.UtcNow;
            await _botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);

            return await stepContext.PromptAsync($"{nameof(FeedbackDialog)}.details",
                    new PromptOptions
                    {
                        Prompt = MessageFactory.Text(Utility.GenerateRandomMessages(Constants.Improve))
                    }, cancellationToken);
        }

        private async Task<DialogTurnResult> Step2Async(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
            userProfile.LastMessageReceived = DateTime.UtcNow;
            await _botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);


            userProfile.Details += (string)stepContext.Result + " ";
            // Save any state changes that might have occured during the turn.
            await _botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);

            return await stepContext.BeginDialogAsync($"{nameof(FeedbackDialog)}.contactProfiling", _ariQuestionResponse, cancellationToken);
        }

        private async Task<DialogTurnResult> Step3Async(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
            userProfile.LastMessageReceived = DateTime.UtcNow;
            await _botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);


            return await stepContext.BeginDialogAsync($"{nameof(AnythingElseDialog)}.AnythingElse", null, cancellationToken);
        }

        private async Task<DialogTurnResult> StepEndAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
            userProfile.LastMessageReceived = DateTime.UtcNow;
            await _botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);
            _ariQuestionResponse = null;
            return await stepContext.EndDialogAsync(null, cancellationToken);
        }

        #endregion 
    }
}
