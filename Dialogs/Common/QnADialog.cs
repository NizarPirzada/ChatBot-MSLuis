using AriBotV4.Common;
using AriBotV4.Dialogs.AskAri.Resources;
using AriBotV4.Dialogs.Common.Resources;
using AriBotV4.Enums;
using AriBotV4.Enums.AriQuestion;
using AriBotV4.Models;
using AriBotV4.Models.AriQuestions;
using AriBotV4.Services;
using Microsoft.Azure.CognitiveServices.Search.EntitySearch;
using Microsoft.Azure.CognitiveServices.Search.WebSearch;
using Microsoft.Azure.CognitiveServices.Search.WebSearch.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace AriBotV4.Dialogs.Common
{
    public class QnADialog : ComponentDialog
    {
        #region Properties and Fields
        private readonly BotStateService _botStateService;
        private LuisModel luisResponse;
        
        #endregion

        #region WaterFallSteps and Dialogs
        public QnADialog(string dialogId, BotStateService botStateService) : base(dialogId)
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
               DisplayGeneralInfo,
               GetConfirmation,
               GetMoreInformation,
               FinalAsync
            };

            // Add Named Dialogs
            AddDialog(new WaterfallDialog($"{nameof(QnADialog)}.mainFlow", waterfallSteps));
            AddDialog(new TextPrompt($"{nameof(QnADialog)}.details"));
            
            AddDialog(new ElaborateMoreDialog($"{nameof(ElaborateMoreDialog)}.mainFlow", _botStateService));

            // Set the starting Dialog
            InitialDialogId = $"{nameof(QnADialog)}.mainFlow";
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
        private async Task<DialogTurnResult> DisplayGeneralInfo(WaterfallStepContext stepContext, CancellationToken cancellationToken)
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

            QnAMaker qnaResult  = GetQnaSearchResult(query);
            

            // Create reply
            var reply = stepContext.Context.Activity.CreateReply();
            // Results found in our knowledge base
            
            if (qnaResult != null)
            {
                if (CreateQnASearchHeroCard(qnaResult.Answers[0].Answer) != null)
                {
                    reply.Attachments.Add(CreateQnASearchHeroCard(qnaResult.Answers[0].Answer));

                    
                }
                else
                {
                    reply.Text = qnaResult.Answers[0].Answer;
                  
                }
                await stepContext.Context.SendActivityAsync(reply);
                if (qnaResult.Answers[0].context != null && qnaResult.Answers[0].context.prompts.Count > 0)
                {
                    //var reply2 = stepContext.Context.Activity.CreateReply();
                    foreach (var prompt in qnaResult.Answers[0].context.prompts)
                    {

                    await stepContext.Context.SendActivityAsync(MessageFactory.SuggestedActions(new CardAction[]
                {
                          new CardAction(title: prompt.displayText, type: ActionTypes.ImBack, value: prompt.displayText),

                 }));
                    }
                    // await stepContext.Context.SendActivityAsync(reply, cancellationToken);

                }
                //if (qnaResult.Answers[0].context != null && qnaResult.Answers[0].context.prompts.Count > 0)
                //{
                //    foreach (var prompt in qnaResult.Answers[0].context.prompts)
                //    {
                //        reply.Attachments.Add(CreateQnASearchFollowUpPrompts(prompt));
                //    }
                //}
                //await stepContext.Context.SendActivityAsync(reply);
            }
                
            return await stepContext.NextAsync(qnaResult, cancellationToken);


        }
        private async Task<DialogTurnResult> GetConfirmation(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            QnAMaker qnAMaker = (QnAMaker)stepContext.Result;

            
            if (qnAMaker.Answers[0].Source != "qna_chitchat_Caring.tsv")
            {


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
                        Text = (!string.IsNullOrEmpty(qnAMaker.Answers[0].Answer) && (!qnAMaker.Answers[0].Answer.Contains(SearchAri.QnAMakerAnswerNotFound))) ? SearchAri.Confirmation :
                Utility.GenerateRandomMessages(Constants.InternetConfirmation),

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



                return await stepContext.PromptAsync($"{nameof(QnADialog)}.details", confirmUser);

            }
            else
            {
                await stepContext.EndDialogAsync(null, cancellationToken);
                return await stepContext.BeginDialogAsync($"{nameof(RootDialog)}.contactOptions", null, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> GetMoreInformation(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (Constants.NoLibrary.Any(str => str.ToLower() == (stepContext.Context.Activity.Text.ToLower())))
            {
                return await stepContext.BeginDialogAsync($"{nameof(ElaborateMoreDialog)}.mainFlow", EnumHelpers.GetEnumDescription(AriBotV4.Enums.AskAri.General), cancellationToken);
            }
            else if (Constants.YesLibrary.Any(str => str.ToLower() == (stepContext.Context.Activity.Text.ToLower())))
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(Utility.GenerateRandomMessages(Constants.HappyWithResults)),
                        cancellationToken);
                return await stepContext.BeginDialogAsync($"{nameof(AnythingElseDialog)}.AnythingElse", EnumHelpers.GetEnumDescription(AriBotV4.Enums.AskAri.General), cancellationToken);
            }
            else
            {
                await stepContext.EndDialogAsync(null, cancellationToken);
                return await stepContext.BeginDialogAsync($"{nameof(RootDialog)}.contactOptions", stepContext.Context.Activity.Text, cancellationToken);
            }
        }
        private async Task<DialogTurnResult> FinalAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            return await stepContext.EndDialogAsync(null, cancellationToken);
        

        }

        private QnAMaker GetQnaSearchResult(string query)
        {
            try
            {
                var qnaMakerHost = _botStateService._qnaSettings.QnAMakerHost;
                var qnaMakerKBId = _botStateService._qnaSettings.QnAMakerId;
                var qnaMakerEndPointKey = _botStateService._qnaSettings.QnAMakerEndPointKey;
                var qnaMakerFormatJson = _botStateService._qnaSettings.QnAMakerFormatJson;
                var qnaMakerAnswerNotFound = SearchAri.QnAMakerAnswerNotFound;

                var client = new RestClient(qnaMakerHost + "/knowledgebases/" + qnaMakerKBId + "/generateAnswer");
                var qnaRequest = new RestRequest(Method.POST);
                qnaRequest.AddHeader("authorization", "EndpointKey " + qnaMakerEndPointKey);
                qnaRequest.AddParameter(qnaMakerFormatJson, "{\"question\": \"" + query + "\"}", ParameterType.RequestBody);
                var qnaResponse = client.Execute(qnaRequest);

                var qnaSearchList = JsonConvert.DeserializeObject<QnAMaker>(qnaResponse.Content);

                
                if (qnaSearchList.Answers.Count > 0)
                {
                    return qnaSearchList;
                    //var qnaFirstAnswer = qnaSearchList.Answers[0].Answer;
                    //var qnaScore = qnaSearchList.Answers[0].Score;
                    //if (!qnaFirstAnswer.ToLower().Equals(qnaMakerAnswerNotFound) && qnaScore >= _botStateService._qnaSettings.ScorePercentage)
                    //              return qnaSearchList;
                    //else
                    //{
                    //    qnaSearchList.Answers[0].Answer = qnaMakerAnswerNotFound;
                    //    qnaSearchList.Answers[0].Source = "Editorial";
                    //    return qnaSearchList;
                    //}
                }
                return null;
               

            }
            catch
            {
                return null;
            }
        }

        // Create hero card for displaying QnA resultd
        private static Attachment CreateQnASearchHeroCard(string article)
        {
            if (!string.IsNullOrEmpty(Utility.GetUrlFromString(article)))
            {
                // Remove url from answer
                string articleWithoutUrl = article.Replace(Utility.GetUrlFromString(article), "");
                var heroCard = new HeroCard()
                {
                    // Title = article.Name,
                    Text = articleWithoutUrl,
                    Buttons = new List<CardAction>
                    {
                        new CardAction(ActionTypes.OpenUrl, SearchAri.ViewMore, value: Utility.GetUrlFromString(article))
                    },
                };
                return heroCard.ToAttachment();
            }
            else
            {
                return null;
            }


        }
        private static Attachment CreateQnASearchFollowUpPrompts(Prompt prompt)
        {
         
                var heroCard = new HeroCard()
                {
                    // Title = article.Name,
                   Buttons = new List<CardAction>
                    {
                        new CardAction(ActionTypes.ImBack, prompt.displayText, value: prompt.displayText)
                    },
                };
             
            return heroCard.ToAttachment();
                    }

        // Create hero card for displaying bing results
        private static Attachment CreateWebSearchHeroCard(WebPage article)
        {
            var heroCard = new HeroCard()
            {
                Title = article.Name,
                Subtitle = article.DisplayUrl,
                Text = article.Snippet,
                Buttons = new List<CardAction>
                    {
                        new CardAction(ActionTypes.OpenUrl, SearchAri.ViewMore, value: article.Url),
                    },
            };

            return heroCard.ToAttachment();
        }
        private async Task<Microsoft.Azure.CognitiveServices.Search.WebSearch.Models.SearchResponse> GetBingWebSearchResult(string query, string filter = "", int offset = 0)
        {
            try
            {

                var uriQuery = "https://api.cognitive.microsoft.com/bing/v7.0/search?q=" + Uri.EscapeDataString(query);

                // Perform request and get a response.
                WebRequest request = HttpWebRequest.Create(uriQuery);
                request.Headers["Ocp-Apim-Subscription-Key"] = _botStateService._bingSettings.BingSubcriptionKey;
                HttpWebResponse response = (HttpWebResponse)request.GetResponseAsync().Result;
                string result = new StreamReader(response.GetResponseStream()).ReadToEnd();
               return JsonConvert.DeserializeObject<Microsoft.Azure.CognitiveServices.Search.WebSearch.Models.SearchResponse>(result);

            }
            catch(Exception ex)
            {
                return null;
            }
        }

        private async Task<Microsoft.Azure.CognitiveServices.Search.EntitySearch.Models.SearchResponse> GetBingEntitySearchResult(string query)
        {
            try
            {

                var client = new EntitySearchClient(new Microsoft.Azure.CognitiveServices.Search.WebSearch.ApiKeyServiceClientCredentials
                    (_botStateService._bingSettings.BingSubcriptionKey));
                return await client.Entities.SearchAsync(query: query);
            }
            catch
            {
                return null;
            }
        }

        #endregion
    }
}
