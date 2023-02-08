using AriBotV4.Models;
using AriBotV4.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Azure.CognitiveServices.Search.WebSearch;
using Microsoft.Azure.CognitiveServices.Search.WebSearch.Models;

namespace AriBotV4.Dialogs
{
    public class SearchInternetDialog : ComponentDialog
    {
        private readonly BotStateService _botStateService;

        public SearchInternetDialog(string dialogId, BotStateService botStateService) : base(dialogId)
        {
            _botStateService = botStateService ?? throw new System.ArgumentNullException(nameof(botStateService));

            InitializeWaterfallDialog();
        }

        private void InitializeWaterfallDialog()
        {
            // Create Waterfall Steps
            var waterfallSteps = new WaterfallStep[]
            {
                Step1Async,
                Step2Async,
                StepEndAsync
            };

            // Add Named Dialogs
            AddDialog(new WaterfallDialog($"{nameof(SearchInternetDialog)}.mainFlow", waterfallSteps));
            AddDialog(new TextPrompt($"{nameof(SearchInternetDialog)}.search"));

            // Set the starting Dialog
            InitialDialogId = $"{nameof(SearchInternetDialog)}.mainFlow";
        }

        // Waterfall steps
        private async Task<DialogTurnResult> Step1Async(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync($"{nameof(SearchInternetDialog)}.search",
                 new PromptOptions
                 {
                     Prompt = MessageFactory.Text("What do you want to look for?")
                 }, cancellationToken);
        }

        private async Task<DialogTurnResult> Step2Async(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var resp = (string)stepContext.Result;

            UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
            userProfile.Details += resp + " ";
            // First, we use the dispatch model to determine which cognitive service (LUIS or QnA) to use.
            //var recognizerResult = await _botStateService._botServices.Dispatch.RecognizeAsync(stepContext.Context, cancellationToken);

            // Top intent tell us which cognitive service to use.
            //var topIntent = recognizerResult.GetTopScoringIntent();

            try
            {
                var reply = stepContext.Context.Activity.CreateReply("");

                var bingResult = BingSearch((string)stepContext.Result).Result.WebPages.Value;
                foreach (var article in bingResult)
                {
                    reply.Attachments.Add(CreateSearchHeroCard(article));
                }

                // Save any state changes that might have occured during the turn.
                await _botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);

                await stepContext.Context.SendActivityAsync(reply, cancellationToken);

            }
            catch (Exception ex)
            {
                throw ex;
            }

            //await stepContext.Context.SendActivityAsync(MessageFactory.Text("I don't get you but I'm getting smarter."), cancellationToken);
            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> StepEndAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.EndDialogAsync(null, cancellationToken);
        }

        private static Attachment CreateSearchHeroCard(WebPage article)
        {
            var heroCard = new HeroCard()
            {
                Title = article.Name,
                Subtitle = article.DisplayUrl,
                Text = article.Snippet,
                Buttons = new List<CardAction>
                    {
                        new CardAction(ActionTypes.OpenUrl, "View", value: article.Url),
                    },
            };

            return heroCard.ToAttachment();
        }

        #region SearchAPI
        private async Task<SearchResponse> BingSearch(string query, string strResponseFilter = "", int offset = 0, int count = 5)
        {
            try
            {
                var client = new WebSearchClient(new ApiKeyServiceClientCredentials("10c7410c8e2b4ed5b18facc791cd34a2"));
                return await client.Web.SearchAsync(query: query, offset: offset, count: count);
            }
            catch
            {
                return null;
            }
        }
        #endregion
    }
}
