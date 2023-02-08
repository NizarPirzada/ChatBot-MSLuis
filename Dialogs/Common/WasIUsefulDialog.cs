using AriBotV4.Dialogs.Common.Resources;
using AriBotV4.Models;
using AriBotV4.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AriBotV4.Dialogs.Common
{
    public class WasIUsefulDialog : ComponentDialog
    {
        #region Properties and Fields
        private readonly BotStateService _botStateService;
        #endregion

        #region Method
        public WasIUsefulDialog(string dialogId, BotStateService botStateService) : base(dialogId)
        {
            _botStateService = botStateService ?? throw new System.ArgumentNullException(nameof(botStateService));

            InitializeWaterfallDialog();
        }

        private void InitializeWaterfallDialog()
        {
            // Create Waterfall Steps
            var waterfallSteps = new WaterfallStep[]
            {
                WasIHelpful,
                UserConfirmation,
                FinalStepAsync
            };

            // Add Named Dialogs
            AddDialog(new WaterfallDialog($"{nameof(WasIUsefulDialog)}.mainFlow", waterfallSteps));
            AddDialog(new ChoicePrompt($"{nameof(WasIUsefulDialog)}.wasIHelpful"));
            AddDialog(new TextPrompt($"{nameof(WasIUsefulDialog)}.details"));

            // Set the starting Dialog
            InitialDialogId = $"{nameof(WasIUsefulDialog)}.mainFlow";
        }

        public async Task<DialogTurnResult> WasIHelpful(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await Task.Delay(2000);


            UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
            userProfile.LastMessageReceived = DateTime.UtcNow;
            await _botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);

            if (userProfile.Subject.Contains("I have an awesome idea") || userProfile.Subject.Contains("Subscribe to your Newsletters"))
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(SharedStrings.CloseDialog), cancellationToken);
                return await stepContext.EndDialogAsync(null, cancellationToken);
            }
            else
            {
                try
                {
                    var opts = new PromptOptions
                    {
                        Prompt = new Activity
                        {

                            Type = ActivityTypes.Message,
                            Text = SharedStrings.Helpful,
                            SuggestedActions = new SuggestedActions()
                            {
                                Actions = new List<CardAction>()
                                    {
                                        new CardAction() { Title = SharedStrings.ConfirmYes , Type = ActionTypes.ImBack,
                                            Value = SharedStrings.ConfirmYes },
                                        new CardAction() { Title = SharedStrings.ConfirmNo, Type = ActionTypes.ImBack,
                                            Value = SharedStrings.ConfirmNo },
                                    },
                            },
                        }
                    };

                    // Display a Text Prompt with suggested actions and wait for input
                    return await stepContext.PromptAsync($"{nameof(WasIUsefulDialog)}.details", opts);
                }
                catch (Exception ex)
                {
                    return null;
                }
            }

        }

        private async Task<DialogTurnResult> UserConfirmation(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            try
            {
                UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
                userProfile.LastMessageReceived = DateTime.UtcNow;
                var selectedChoice = Convert.ToString(stepContext.Result);
                await _botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);

                if (selectedChoice.Contains(SharedStrings.ConfirmYes))
                {
                    // isAnythingElseNeeded = false;
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(SharedStrings.HappyToHelp), cancellationToken);
                    return await stepContext.BeginDialogAsync($"{nameof(AnythingElseDialog)}.AnythingElse", null, cancellationToken);
                }
                else if (selectedChoice.Contains(SharedStrings.ConfirmNo))
                {

                    //isAnythingElseNeeded = false;
                    return await stepContext.BeginDialogAsync($"{nameof(FeedbackDialog)}.Feedback", null, cancellationToken);
                }
                else
                {
                    //isAnythingElseNeeded = true;

                    return await stepContext.BeginDialogAsync($"{nameof(AskAriDialog)}.AskAri", null, cancellationToken);
                }

            }
            catch (Exception ex)
            {
                return null;
            }

        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
            userProfile.LastMessageReceived = DateTime.UtcNow;
            await _botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);

            return await stepContext.EndDialogAsync(null, cancellationToken);
        }


        #endregion
    }
}
