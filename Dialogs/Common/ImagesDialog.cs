using AriBotV4.Common;
using AriBotV4.Dialogs.Common.Resources;
using AriBotV4.Enums;
using AriBotV4.Models;
using AriBotV4.Services;
using Microsoft.Azure.CognitiveServices.Search.ImageSearch;
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
    public class ImagesDialog : ComponentDialog
    {
        #region Properties and Fields
        private readonly BotStateService _botStateService;
        private LuisModel luisResponse;
        #endregion

        #region WaterFallSteps and Dialogs
        public ImagesDialog(string dialogId, BotStateService botStateService) : base(dialogId)
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
               ConfirmQuery,
               DisplayImages,
               GetConfirmation,
               GetMoreInformation,
               FinalAsync
            };

            // Add Named Dialogs
            AddDialog(new WaterfallDialog($"{nameof(ImagesDialog)}.mainFlow", waterfallSteps));
            AddDialog(new TextPrompt($"{nameof(ImagesDialog)}.details"));
            AddDialog(new ElaborateMoreDialog($"{nameof(ElaborateMoreDialog)}.mainFlow", _botStateService));

            // Set the starting Dialog
            InitialDialogId = $"{nameof(ImagesDialog)}.mainFlow";
        }
        private async Task<DialogTurnResult> ConfirmQuery(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((string)stepContext.ActiveDialog.State["options"] == null)
            {
                return await stepContext.PromptAsync($"{nameof(RootOptionsDialog)}.details",
                 new PromptOptions
                 {
                     Prompt = MessageFactory.Text(Utility.GenerateRandomMessages(Constants.AskAriHelp)),

                 }, cancellationToken);
            }
            return await stepContext.NextAsync(null, cancellationToken);

        }
        private async Task<DialogTurnResult> DisplayImages(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            string query = string.Empty;
            if ((string)stepContext.ActiveDialog.State["options"] != null)
            {
                query = (string)stepContext.ActiveDialog.State["options"];
            }
            else
            {
                query = stepContext.Context.Activity.Text;
            }

            if (!string.IsNullOrEmpty(Convert.ToString(stepContext.Context.Activity.From.Properties[Constants.TaskSpurTimeZone])) && (Convert.ToString(stepContext.Context.Activity.From.Properties[Constants.TaskSpurTimeZone])).Contains("/"))
            {
                query += " In " + Convert.ToString(stepContext.Context.Activity.From.Properties[Constants.TaskSpurTimeZone]).Split("/")[1] + " " + Convert.ToString(stepContext.Context.Activity.From.Properties[Constants.TaskSpurTimeZone]).Split("/")[0];
            }
            IList<Microsoft.Azure.CognitiveServices.Search.ImageSearch.Models.ImageObject> bingImageResult = GetBingImageSearchResult(query).Result;
            // Create reply
            var reply = stepContext.Context.Activity.CreateReply();



            for (int i = 0; i <= bingImageResult.Count - 1; i++)
            {
                reply.Attachments.Add(CreateImageHeroCard(bingImageResult[i]));

            }

            if (reply.Attachments.Count == 0)
            {
                reply.Text = Convert.ToString(SharedStrings.NotFound);
                await stepContext.Context.SendActivityAsync(reply, cancellationToken);
                 await stepContext.EndDialogAsync(null, cancellationToken);
                return await stepContext.BeginDialogAsync($"{nameof(AnythingElseDialog)}.AnythingElse", EnumHelpers.GetEnumDescription(AriBotV4.Enums.AskAri.General), cancellationToken);
            }
            await stepContext.Context.SendActivityAsync(reply, cancellationToken);
            return await stepContext.NextAsync(null, cancellationToken);
        }
        private async Task<DialogTurnResult> GetConfirmation(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await Task.Delay(2000);



            var confirmUser = new PromptOptions
            {
                RetryPrompt = new Microsoft.Bot.Schema.Activity
                {

                    Type = ActivityTypes.Message,
                    Text = Utility.GenerateRandomMessages(Constants.RepromptConfirmation),

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


                },

                Prompt = new Microsoft.Bot.Schema.Activity
                {

                    Type = ActivityTypes.Message,
                    Text = (Utility.GenerateRandomMessages(Constants.InternetConfirmation)),

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



            return await stepContext.PromptAsync($"{nameof(ImagesDialog)}.details", confirmUser);

        }

        private async Task<DialogTurnResult> GetMoreInformation(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Context.Activity.Text == SharedStrings.ConfirmNo)
            {
                return await stepContext.BeginDialogAsync($"{nameof(ElaborateMoreDialog)}.mainFlow", EnumHelpers.GetEnumDescription(AriBotV4.Enums.AskAri.Images) + "-" + (string)stepContext.ActiveDialog.State["options"], cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(Utility.GenerateRandomMessages(Constants.HappyWithResults)),
                        cancellationToken);
                return await stepContext.BeginDialogAsync($"{nameof(AnythingElseDialog)}.AnythingElse", EnumHelpers.GetEnumDescription(AriBotV4.Enums.AskAri.Images), cancellationToken);
            }
        }
        private async Task<DialogTurnResult> FinalAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            return await stepContext.EndDialogAsync(null, cancellationToken);
            //return await stepContext.BeginDialogAsync($"{nameof(AnythingElseDialog)}.AnythingElse", null, cancellationToken);

        }

        // Bing image search API
        private async Task<IList<Microsoft.Azure.CognitiveServices.Search.ImageSearch.Models.ImageObject>> GetBingImageSearchResult
            (string query, string filter = "", int offset = 0)
        {
            try
            {
                var client = new ImageSearchClient(new Microsoft.Azure.CognitiveServices.Search.WebSearch.ApiKeyServiceClientCredentials
                    (_botStateService._bingSettings.BingSubcriptionKey));
                return client.Images.SearchAsync(query: query, offset: offset, count: _botStateService._bingSettings.BingResultCount,
                    market: _botStateService._bingSettings.Market
                    //freshness: _botStateService._bingSettings.Freshness
                    ).Result.Value;


            }
            catch (Exception ex)
            {
                return null;
            }
        }

        // Create image hero card
        private static Attachment CreateImageHeroCard(Microsoft.Azure.CognitiveServices.Search.ImageSearch.Models.ImageObject image)
        {
            var heroCard = new HeroCard()
            {
                Images = new List<CardImage>
                    {
                        new CardImage(image.ContentUrl),
                    }

            };

            return heroCard.ToAttachment();
        }

        #endregion
    }
}
