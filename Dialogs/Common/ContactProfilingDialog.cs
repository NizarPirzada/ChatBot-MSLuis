
using AriBotV4.Common;
using AriBotV4.Dialogs.Common.Resources;
using AriBotV4.Enums;
using AriBotV4.Enums.AriQuestion;
using AriBotV4.Models;
using AriBotV4.Models.AriQuestions;
using AriBotV4.Services;
using Hangfire;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AriBotV4.Dialogs
{
    public class ContactProfilingDialog : ComponentDialog
    {
        #region Properties and Fields
        private readonly BotStateService _botStateService;
        private AriQuestionResponse _ariQuestionResponse;
        private AriQuestionUpdateRequest _ariQuestionUpdateRequest;
        #endregion

        #region Method
        public ContactProfilingDialog(string dialogId, BotStateService botStateService) : base(dialogId)
        {
            _botStateService = botStateService ?? throw new System.ArgumentNullException(nameof(botStateService));
            InitializeWaterfallDialog();
        }

        private void InitializeWaterfallDialog()
        {
            // Create Waterfall Steps
            var waterfallSteps = new WaterfallStep[]
            {
                InitialStepAsync,
                GetNameStepAsync,
                GetEmailStepAsync,
                FinalStepAsync,
            };

            // Add Named Dialogs
            AddDialog(new WaterfallDialog($"{nameof(ContactProfilingDialog)}.mainFlow", waterfallSteps));
            AddDialog(new TextPrompt($"{nameof(ContactProfilingDialog)}.details"));
            AddDialog(new TextPrompt($"{nameof(ContactProfilingDialog)}.name"));
            AddDialog(new TextPrompt($"{nameof(ContactProfilingDialog)}.email", _botStateService._emailService.EmailValidatorAsync));


            // Set the starting Dialog
            InitialDialogId = $"{nameof(ContactProfilingDialog)}.mainFlow";
        }

        private async Task<DialogTurnResult> InitialStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((AriQuestionResponse)stepContext.ActiveDialog.State["options"] != null)
            {
                _ariQuestionResponse = (AriQuestionResponse)stepContext.ActiveDialog.State["options"];
                _ariQuestionUpdateRequest = new AriQuestionUpdateRequest();
                if (_ariQuestionResponse.data != null)
                    _ariQuestionUpdateRequest.Id = _ariQuestionResponse.data.id;

                _ariQuestionUpdateRequest.AriTrainingType = (int)AriTrainingType.Feedback;

                UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
                userProfile.LastMessageReceived = DateTime.UtcNow;
                await _botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);

                _ariQuestionUpdateRequest.Answer = userProfile.Details;
            }
            if (!string.IsNullOrEmpty(Convert.ToString(stepContext.Context.Activity.From.Properties[AriBotV4.Common.Constants.TaskSpurUserId])))
            {
                _ariQuestionUpdateRequest.UserId = Convert.ToString(stepContext.Context.Activity.From.Properties[AriBotV4.Common.Constants.TaskSpurUserId]);
            }
            return await stepContext.NextAsync(null, cancellationToken);


        }

        private async Task<DialogTurnResult> GetNameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
            userProfile.LastMessageReceived = DateTime.UtcNow;
            await _botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);


            userProfile.Details += (string)stepContext.Result + " ";

            if (string.IsNullOrEmpty(userProfile.Name))
            {
                return await stepContext.PromptAsync($"{nameof(ContactProfilingDialog)}.name",
                    new PromptOptions
                    {
                        Prompt = MessageFactory.Text(Utility.GenerateRandomMessages(Constants.AskName))
                    }, cancellationToken);
            }
            else
            {
                if (_ariQuestionUpdateRequest != null)
                    _ariQuestionUpdateRequest.Name = userProfile.Name;
                return await stepContext.NextAsync(null, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> GetEmailStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
            userProfile.LastMessageReceived = DateTime.UtcNow;
            await _botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);

            if (string.IsNullOrEmpty(userProfile.Name))
            {
                if (_ariQuestionUpdateRequest != null)
                    _ariQuestionUpdateRequest.Name = (string)stepContext.Result;
                // Set the name
                var userProfileName = (string)stepContext.Result;
                userProfile.Name = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(userProfileName.ToLower());

                // Save any state changes that might have occured during the turn.
                await _botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);
            }

            if (string.IsNullOrEmpty(userProfile.Email))
            {
                return await stepContext.PromptAsync($"{nameof(ContactProfilingDialog)}.email",
                    new PromptOptions
                    {
                        Prompt = MessageFactory.Text(string.Format(Utility.GenerateRandomMessages(Constants.AskEmail), userProfile.Name)),
                        RetryPrompt = MessageFactory.Text(SharedStrings.ValidEmail),
                    }, cancellationToken);
            }
            else
            {
                if (_ariQuestionUpdateRequest != null)
                    _ariQuestionUpdateRequest.UserEmail = userProfile.Email;
                return await stepContext.NextAsync(null, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
            userProfile.LastMessageReceived = DateTime.UtcNow;
            await _botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);

            if (string.IsNullOrEmpty(userProfile.Email))
            {
                userProfile.Email = (string)stepContext.Result;
                if (_ariQuestionUpdateRequest != null)
                    _ariQuestionUpdateRequest.UserEmail = (string)stepContext.Result;
            }
            // Save any state changes that might have occured during the turn.
            await _botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);
            Models.TaskSpur.Auth.TokenResponse tokenResponse = await
                      _botStateService._taskSpurApiClient.GetToken();
            try
            {
                if (_ariQuestionUpdateRequest != null)
                {
                    // update ari question
                    if (tokenResponse.data != null)
                    {
                        AriQuestionResponse ariQuestionResponse = await _botStateService._ariQuestionsApiClient.UpdateAriQuestion(_ariQuestionUpdateRequest, tokenResponse.data.token);
                        //if (ariQuestionResponse.statusCode == 401)
                        //{

                        //    // Get refersh token
                        //    Models.TaskSpur.Auth.TokenResponse refreshTokenResponse = await _botStateService._taskSpurApiClient.GetRefreshToken(null, tokenResponse.data.token, tokenResponse.data.refreshToken);
                        //    // Create goal with refresh token
                        //    if (!string.IsNullOrEmpty(refreshTokenResponse.data.token))
                        //    {

                        //        ariQuestionResponse = await _botStateService._ariQuestionsApiClient.UpdateAriQuestion(_ariQuestionUpdateRequest, tokenResponse.data.token);

                        //    }

                        //}
                    }
                    _ariQuestionUpdateRequest = null;
                    _ariQuestionResponse = null;
                }
            }
            catch (Exception ex)
            {
                
                await _botStateService._ariQuestionsApiClient.SendEmail(tokenResponse.data.token, ex.Message);

            }
            var alert = new EmailMessage
            {
                ToAddress = _botStateService._appSetting.EmailAdminToNotify,
                FromAddress = userProfile.Email,
                Subject = SharedStrings.AriBot + userProfile.Subject,
                Content = userProfile.Name + ", " + userProfile.Email + " : " + userProfile.Details
            };

            await _botStateService._emailService.SendAsync(alert);

            // Reset the conversation details
            userProfile.Details = "";

            await stepContext.Context.SendActivityAsync(MessageFactory.Text(Utility.GenerateRandomMessages(Constants.ThankyouForFeedback)), cancellationToken);

            _ariQuestionResponse = null;
            _ariQuestionUpdateRequest = null;
             return await stepContext.EndDialogAsync(null, cancellationToken);

        }




        #endregion

    }
}
