using AriBotV4.Helpers;
using AriBotV4.Models;
using AriBotV4.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using RestSharp;
using Newtonsoft.Json;

using Microsoft.Azure.CognitiveServices.Search.WebSearch;
using Microsoft.Azure.CognitiveServices.Search.WebSearch.Models;

namespace AriBotV4.Dialogs.Deals.Others
{
    public class OthersDialog : ComponentDialog
    {
        #region Properties and Fields
        private readonly BotStateService _botStateService;
        #endregion

        #region Method
        public OthersDialog(string dialogId, BotStateService botStateService) : base(dialogId)
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
            AddDialog(new WaterfallDialog($"{nameof(OthersDialog)}.mainFlow", waterfallSteps));
            AddDialog(new TextPrompt($"{nameof(OthersDialog)}.howCanI"));

            // Set the starting Dialog
            InitialDialogId = $"{nameof(OthersDialog)}.mainFlow";
        }

        // Waterfall steps
        private async Task<DialogTurnResult> Step1Async(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync($"{nameof(RootOptionsDialog)}.details",
                 new PromptOptions
                 {
                     Prompt = MessageFactory.Text("Tell me how I can help")
                 }, cancellationToken);
        }

        private async Task<DialogTurnResult> Step2Async(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var resp = (string) stepContext.Result;

            UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
            userProfile.Details += resp + " ";
            // First, we use the dispatch model to determine which cognitive service (LUIS or QnA) to use.
            //var recognizerResult = await _botStateService._botServices.Dispatch.RecognizeAsync(stepContext.Context, cancellationToken);

            // Top intent tell us which cognitive service to use.
            //var topIntent = recognizerResult.GetTopScoringIntent();

            try
            {
                
                // var reply = stepContext.Context.Activity.CreateReply("I could not find what you are looking for in my repository but here is the top 5 results  from Google: ");

                var qnaMakerResult = QnaSearch((string)stepContext.Result);
                var reply = stepContext.Context.Activity.CreateReply(qnaMakerResult);

                if (qnaMakerResult.Contains("I could not find what you are looking for"))
                {
                    if (resp.Contains("news"))
                    {
                        var newsResult = NewsSearch((string)stepContext.Result);
                        foreach (var article in newsResult)
                        {
                            reply.Attachments.Add(CreateNewsHeroCard(article));
                        }
                    } else {
                        var bingResult = BingSearch((string)stepContext.Result).Result.WebPages.Value;
                        foreach (var article in bingResult)
                        {
                            reply.Attachments.Add(CreateSearchHeroCard(article));
                        }

                        // Save any state changes that might have occured during the turn.
                        await _botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);

                        var alert = new EmailMessage
                        {
                            ToAddress = "ajai.kolarikal@lifeintelligencegroup.com",
                            FromAddress = userProfile.Email,
                            Subject = "[Ari Bot] " + userProfile.Subject,
                            Content = userProfile.Name + ", " + userProfile.Email + " : " + userProfile.Details
                        };

                        // Send email of what Ari could not find
                        await _botStateService._emailService.SendAsync(alert);
                        // Reset the conversation details
                        userProfile.Details = "";
                    } 
                }

                await stepContext.Context.SendActivityAsync(reply, cancellationToken);

            }
            catch(Exception ex)
            {
                throw ex;
            }

            //await stepContext.Context.SendActivityAsync(MessageFactory.Text("I don't get you but I'm getting smarter."), cancellationToken);
            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> StepEndAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            //return await stepContext.PromptAsync("", new PromptOptions { }, cancellationToken);
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

        private static Attachment CreateNewsHeroCard(Models.NewsArticle article)
        {
            var heroCard = new HeroCard()
            {
                Title = article.title,
                Subtitle = article.publishedAt.ToString(),
                Text = article.description,
                Images = new List<CardImage>
                    {
                        new CardImage(article.urlToImage),
                    },
                Buttons = new List<CardAction>
                    {
                        new CardAction(ActionTypes.OpenUrl, "View Article", value: article.url),
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

        private List<AriBotV4.Models.NewsArticle> NewsSearch(string query, string strResponseFilter = "", int offset = 0, int count = 5)
        {
            try
            {
                var url = $"https://newsapi.org/v2/top-headlines?country=au&pageSize=10&apiKey=1db81ac8b6b64bdd9dfb5bed2647f495";
                var jsonData = new WebClient().DownloadString(url);
                NewsResponse response = Newtonsoft.Json.JsonConvert.DeserializeObject<NewsResponse>(jsonData);
                return response.articles.Skip(offset).Take(count).ToList();
            }
            catch
            {
                return null;
            }
        }

        private string QnaSearch(string query)
        {
            try
            {
                var qnaMakerHost = "https://ligknowledgebase.azurewebsites.net/qnamaker";
                var qnaMakerKBId = "70a56f84-e8b2-43d1-84de-b89b48e7248b";
                var qnaMakerEndPointKey = "0b25f442-1bfc-42cf-9778-0b1df4b3b3ea";
                var qnaMakerFormatJson = "application/json";
                var qnaMakerAnswerNotFound = "I could not find what you are looking for so I searched the internet for you.";

                var client = new RestClient(qnaMakerHost + "/knowledgebases/" + qnaMakerKBId + "/generateAnswer");
                var request = new RestRequest(Method.POST);
                request.AddHeader("authorization", "EndpointKey " + qnaMakerEndPointKey);
                request.AddParameter(qnaMakerFormatJson, "{\"question\": \"" + query + "\"}", ParameterType.RequestBody);
                var response = client.Execute(request);

                var result = JsonConvert.DeserializeObject<QnAMaker>(response.Content);

                if (result.Answers.Count > 0)
                {
                    var respuesta = result.Answers[0].Answer;
                    var score = result.Answers[0].Score;
                    if (!respuesta.ToLower().Equals(qnaMakerAnswerNotFound) && score > 40)
                        return respuesta;
                }
                return qnaMakerAnswerNotFound;

            }
            catch
            {
                return null;
            }
        }
        #endregion

        #endregion
    }
}
