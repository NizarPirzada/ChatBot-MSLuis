
using AriBotV4.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AriBotV4.Dialogs.TaskSpur.Resources;
using AriBotV4.Models.TaskSpur.Auth;
using System.Net.Http;
using AriBotV4.Models;
using Microsoft.Bot.Builder.Dialogs.Choices;
using AriBotV4.Enums;

namespace AriBotV4.Dialogs.TaskSpur
{
    public class SignInDialog : ComponentDialog
    {
        #region Properties and Fields
        private readonly BotStateService _botStateService;

        private string userName;
        #endregion

        #region WaterFallSteps and Dialogs
        public SignInDialog(string dialogId, BotStateService botStateService) : base(dialogId)
        {
            _botStateService = botStateService ?? throw new System.ArgumentNullException(nameof(botStateService));
            InitializeWaterfallDialog();
        }


        // Initializing waterfall steps
        private void InitializeWaterfallDialog()
        {
            // Create Waterfall Steps
            var waterfallSteps = new WaterfallStep[]
            {
                AskUserNameAsync,
                AskPasswordAsync,
                AuthorizeAsync,
                FinalAsync
            };

            // Add Named Dialogs
            AddDialog(new WaterfallDialog($"{nameof(SignInDialog)}.mainFlow", waterfallSteps));
            AddDialog(new TextPrompt($"{nameof(SignInDialog)}.userName", _botStateService._emailService.EmailValidatorAsync));
            AddDialog(new TextPrompt($"{nameof(SignInDialog)}.password"));
            AddDialog(new ChoicePrompt($"{nameof(SignInDialog)}.name"));


            // Set the starting Dialog
            InitialDialogId = $"{nameof(SignInDialog)}.mainFlow";
        }
        // Step 1 ask user for email ID
        private async Task<DialogTurnResult> AskUserNameAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
            userProfile.LastMessageReceived = DateTime.UtcNow;
            await _botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);

           
            return await stepContext.PromptAsync($"{nameof(SignInDialog)}.userName",
                 new PromptOptions
                 {
                     Prompt = MessageFactory.Text(AriBotV4.Dialogs.TaskSpur.Resources.TaskSpur.AskEmailID),

                 }, cancellationToken);
        }

        // Step 2 ask user for password
        private async Task<DialogTurnResult> AskPasswordAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
            userProfile.UserName = (string)stepContext.Result;
            await _botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);

            return await stepContext.PromptAsync($"{nameof(SignInDialog)}.password",
                 new PromptOptions
                 {
                     Prompt = MessageFactory.Text(AriBotV4.Dialogs.TaskSpur.Resources.TaskSpur.AskPassword),

                 }, cancellationToken);
        }
        // Check whether email and password are authorize 
        private async Task<DialogTurnResult> AuthorizeAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
            userProfile.Password = (string)stepContext.Result;
            await _botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);

        
            TokenResponse tokenResponse = await _botStateService._taskSpurApiClient.GetAuthToken(new TokenRequest
            {
                UserName = userProfile.UserName,
                Password = userProfile.Password,
                LastLocalTimeLoggedIn = DateTime.UtcNow,
                TimeZone = "Australia/Sydney"
            });
            if (tokenResponse.message == null)
            {
                await stepContext.Context.SendActivityAsync(TaskSpur.Resources.TaskSpur.SuccessfulLogin);
                return await SignInOptions(stepContext, cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync(tokenResponse.message.text);

            }

            return await stepContext.NextAsync();
        }

        // Once the user is succesfully sign in, ask them for help
        private async Task<DialogTurnResult> SignInOptions(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            var prompt = new PromptOptions
            {
                Prompt = MessageFactory.Text(TaskSpur.Resources.TaskSpur.SignInOption),
                Choices = ChoiceFactory.ToChoices(new List<string> {
                    EnumHelpers.GetEnumDescription(TaskSpurSignIn.Assist),
                     EnumHelpers.GetEnumDescription(TaskSpurSignIn.Help),
                    }),
                Style = ListStyle.SuggestedAction,

            };

            UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
            userProfile.LastMessageReceived = DateTime.UtcNow;

            await _botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);

          
            return await stepContext.PromptAsync($"{nameof(SignInDialog)}.name", prompt, cancellationToken);
        }

        // Final step to close all dialogs
        private async Task<DialogTurnResult> FinalAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
            userProfile.LastMessageReceived = DateTime.UtcNow;
            await _botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);

            return await stepContext.EndDialogAsync(null, cancellationToken);
        }
        #endregion
    }
}
