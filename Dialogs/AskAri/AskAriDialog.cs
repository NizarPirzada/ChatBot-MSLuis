using AriBotV4.Common;
using AriBotV4.Dialogs.AskAri.Resources;
using AriBotV4.Dialogs.Common;
using AriBotV4.Dialogs.Common.Resources;
using AriBotV4.Enums;
using AriBotV4.Enums.AriQuestion;
using AriBotV4.Models;
using AriBotV4.Models.AriQuestions;
using AriBotV4.Models.Common;
using AriBotV4.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.CognitiveServices.Search.EntitySearch;
using Microsoft.Azure.CognitiveServices.Search.ImageSearch;
using Microsoft.Azure.CognitiveServices.Search.NewsSearch;
using Microsoft.Azure.CognitiveServices.Search.VideoSearch;
using Microsoft.Azure.CognitiveServices.Search.WebSearch;
//using Microsoft.Azure.CognitiveServices.Search.WebSearch;
//using Microsoft.Azure.CognitiveServices.Search.NewsSearch;
using Microsoft.Azure.CognitiveServices.Search.WebSearch.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NodaTime.TimeZones;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using TimeZoneConverter;

namespace AriBotV4.Dialogs
{
    public class AskAriDialog : ComponentDialog
    {
        #region Properties and Fields
        private readonly BotStateService _botStateService;
        private int counter = 0;
        private WaterfallStepContext SearchQueryAsync2;
        private IList<WebPage> bingResult;
        private IList<Microsoft.Azure.CognitiveServices.Search.ImageSearch.Models.ImageObject> bingImageResult;
        private IList<Microsoft.Azure.CognitiveServices.Search.NewsSearch.Models.NewsArticle> bingNewResult;
        private IList<Microsoft.Azure.CognitiveServices.Search.VideoSearch.Models.VideoObject> bingVideoResult;
        //private Microsoft.Azure.CognitiveServices.Search.EntitySearch.Models.SearchResponse queryEnitity;
        private string qnaResult = string.Empty;

        private readonly BotServices _botServices;
        private LuisModel luisResponse;
        private AriQuestionRequest ariQuestionRequest;
        private AriQuestionUpdateRequest ariQuestionUpdateRequest;
        private AriQuestionResponse ariQuestionResponse;
        private bool isAriQuestionCreated;
        

        #endregion


        #region WaterFallSteps and Dialogs

        public AskAriDialog(string dialogId, BotStateService botStateService, BotServices botServices
           ) : base(dialogId)
        {
            _botStateService = botStateService ?? throw new System.ArgumentNullException(nameof(botStateService));
            _botServices = botServices ?? throw new System.ArgumentNullException(nameof(botStateService));
            
            InitializeWaterfallDialog();
        }

        // Initializing waterfall steps
        private void InitializeWaterfallDialog()
        {
            // Create Waterfall Steps
            var waterfallSteps = new WaterfallStep[]
            {
                AskAriOptions,
                AskQueryAsync,
                SearchQueryAsync,
                GetConfirmationAsync,
                GetMoreInformation,
                FinalAsync
            };

            // Add Named Dialogs
            AddDialog(new WaterfallDialog($"{nameof(AskAriDialog)}.mainFlow", waterfallSteps));
            AddDialog(new ChoicePrompt($"{nameof(AskAriDialog)}.WasIUseful"));
            AddDialog(new TextPrompt($"{nameof(WasIUsefulDialog)}.details"));
            AddDialog(new TextPrompt($"{nameof(AskAriDialog)}.details", ValidateChoice));
            AddDialog(new TextPrompt($"{nameof(AskAriDialog)}.city"));







            // Set the starting Dialog
            InitialDialogId = $"{nameof(AskAriDialog)}.mainFlow";
        }

        // Step 1 ask user for option to search
        private async Task<DialogTurnResult> AskAriOptions(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // set the results to show counter to 0
            counter = 0;
            ariQuestionRequest = null;
            ariQuestionUpdateRequest = null;
            ariQuestionResponse = null;
            isAriQuestionCreated = false;

            UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
            userProfile.LastMessageReceived = DateTime.UtcNow;

            await _botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);
            if ((string)stepContext.ActiveDialog.State["options"] != null)
            {
                
                    var reply = stepContext.Context.Activity.CreateReply();
                    reply.Text = Convert.ToString(stepContext.Context.Activity.Text);
                    stepContext.Context.Activity.Text = (string)stepContext.ActiveDialog.State["options"];
                    return await stepContext.NextAsync(reply.Text, cancellationToken);
                
            }

                    if (stepContext.Context.Activity.Text == SearchAri.AskAri ||
                stepContext.Context.Activity.Text == SearchAri.ConfirmAskForMoreInfo)
            {


                var prompt = new PromptOptions
                {
                    Prompt = MessageFactory.Text(Utility.GenerateRandomMessages(Constants.ChooseAriOptions)),
                    Choices = ChoiceFactory.ToChoices(new List<string> {
                    EnumHelpers.GetEnumDescription(AriBotV4.Enums.AskAri.General),
                     EnumHelpers.GetEnumDescription(AriBotV4.Enums.AskAri.News),
                      EnumHelpers.GetEnumDescription(AriBotV4.Enums.AskAri.Images),
                       //EnumHelpers.GetEnumDescription(AriBotV4.Enums.AskAri.Videos),

                    }),
                    Style = ListStyle.SuggestedAction,

                };



                return await stepContext.PromptAsync($"{nameof(RootOptionsDialog)}.name", prompt, cancellationToken);
            }
            else
            {
                var reply = stepContext.Context.Activity.CreateReply();
                reply.Text = Convert.ToString(stepContext.Context.Activity.Text);
                return await stepContext.NextAsync(reply.Text, cancellationToken);
            }


        }


        // Step 2 ask user input to search
        private async Task<DialogTurnResult> AskQueryAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {



            if (stepContext.Context.Activity != null &&
                (stepContext.Context.Activity.Text == Convert.ToString(AriBotV4.Enums.AskAri.General) ||
                stepContext.Context.Activity.Text == Convert.ToString(AriBotV4.Enums.AskAri.News) ||
                stepContext.Context.Activity.Text == Convert.ToString(AriBotV4.Enums.AskAri.Images) ||
                stepContext.Context.Activity.Text == Convert.ToString(AriBotV4.Enums.AskAri.Videos) ||
                stepContext.Context.Activity.Text == SearchAri.ConfirmAskForMoreInfo))
            {


                UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());

                Type stringType = typeof(System.String);
                Type resultType = stepContext.Result.GetType();
                string selectedChoice = (resultType == stringType ? stepContext.Context.Activity.Text :
                Convert.ToString(((FoundChoice)stepContext.Result).Value));
                userProfile.CurrentAriOptions = selectedChoice;

                await _botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);
                // _currrentAriOption = ((FoundChoice)stepContext.Result).Value;
                return await stepContext.PromptAsync($"{nameof(RootOptionsDialog)}.details",
                 new PromptOptions
                 {
                     Prompt = MessageFactory.Text(Utility.GenerateRandomMessages(Constants.AskAriHelp)),

                 }, cancellationToken);
            }
            else
            {
                var reply = stepContext.Context.Activity.CreateReply();
                reply.Text = Convert.ToString(stepContext.Context.Activity.Text);
                return await stepContext.NextAsync(reply.Text, cancellationToken);
            }

        }

        // Step 3 Get user input and pass it search either in QnA or Bing
        private async Task<DialogTurnResult> SearchQueryAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
                        try
                {
                UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
                userProfile.CurrentAriOptions = userProfile.CurrentAriOptions;
                await _botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);

                // Get user input
                var userInputQuery = (string)stepContext.Result;

                //    userProfile.Details += userInputQuery + " ";


                RecognizerResult recognizerResult = new RecognizerResult();
                // First, we use the dispatch model to determine which cognitive service (LUIS or Qna) to use
                try
                {
                    recognizerResult = await _botServices.Dispatch.RecognizeAsync(stepContext.Context, cancellationToken);
                    luisResponse = JsonConvert.DeserializeObject<LuisModel>(JsonConvert.SerializeObject(recognizerResult));
                }
                catch (Exception ex)
                {

                }
                LuisModel.Intent topIntent = new LuisModel.Intent();

                // Top intent tell us which cognitive service to use
                if (luisResponse != null)
                    topIntent = luisResponse.TopIntent().intent;
                

                //To continue, must be time Intent
                if (Convert.ToString(topIntent) == Constants.TimeIntent && luisResponse.Entities._instance.time != null && (luisResponse.Entities._instance.datetime != null || luisResponse.Entities._instance.geographyV2 != null 
                    || luisResponse.TopIntent().score >= 0.99))
                {

                    // check user ask for any country 
                    if (luisResponse.Entities.geographyV2 != null)
                    {
                        //
                        var getZoneId = TzdbDateTimeZoneSource.Default.ZoneLocations.Where(x => x.CountryName.ToLower()
                         == (luisResponse.Entities.geographyV2[0].Location.ToLower())).AsQueryable();

                        if (getZoneId.Count() != 1)
                            getZoneId = TzdbDateTimeZoneSource.Default.ZoneLocations.Where(x => x.ZoneId.ToLower()
                           .Contains(luisResponse.Entities.geographyV2[0].Location.ToLower())).AsQueryable();


                        if (getZoneId.Count() > 1)
                        {
                            return await stepContext.PromptAsync($"{nameof(AskAriDialog)}.city",
                            new PromptOptions
                            {
                                Prompt = MessageFactory.Text(SearchAri.AskCityorState)
                            }, cancellationToken);
                        }
                        else
                        {
                            return await GetTimeFinalResults(stepContext, cancellationToken);
                        }
                    }
                    else
                    {
                        return await GetTimeFinalResults(stepContext, cancellationToken);
                    }

                }
                else if (Convert.ToString(topIntent) == Constants.WeatherIntent && luisResponse.Entities._instance.weather != null)
                {


                    // Begin ask anything else dialog 
                    return await stepContext.BeginDialogAsync($"{nameof(WeatherDialog)}.mainFlow", recognizerResult, cancellationToken);



                }

                else
                {
                    // Process user input in QnA or Bing 
                    return await GetQnAOrBingFinalResults(stepContext, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        // Get search query results
        private async Task<DialogTurnResult> GetQnAOrBingFinalResults(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            
            SearchQueryAsync2 = stepContext;

            UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
            userProfile.CurrentAriOptions = userProfile.CurrentAriOptions;
            await _botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);

            string finalQuery = stepContext.Result + " " + userProfile.CurrentAriOptions;

            
            string[] splitedResult = new string[] { Convert.ToString(finalQuery).ToLower() };

            // split words with spaces
            if (Convert.ToString(finalQuery).Contains(' '))
            {
                splitedResult = Convert.ToString(finalQuery).ToLower().Split(' ');
            }



            // Compare news list with query
            List<string> matchingNewsResults = Constants.BingNewsList.Intersect(splitedResult).ToList();

            // Compare news list with query
            List<string> matchingImageResults = Constants.BingImageList.Intersect(splitedResult).ToList();

            List<string> matchingVideoResults = Constants.BingVideoList.Intersect(splitedResult).ToList();


            if (matchingNewsResults.Count > 0)
            {
                // Search into bing news search API
                bingNewResult = GetBingNewsSearchResult((string)stepContext.Result).Result;

            }

            else if (matchingImageResults.Count > 0)
            {
                // Search into bing image search API
                bingImageResult = GetBingImageSearchResult((string)stepContext.Result).Result;
            }
            else if (matchingVideoResults.Count > 0)
            {
                // Search into bing video search API
                bingVideoResult = GetBingVideosSearchResult((string)stepContext.Result).Result;
            }
            else
            {
                // Search into QnA Knowledge base
                qnaResult = GetQnaSearchResult((string)stepContext.Result);

                var bingWebPages = GetBingWebSearchResult((string)stepContext.Result).Result.WebPages;
                if (bingWebPages != null)
                {
                    // Search into Bing web search API
                    bingResult = bingWebPages.Value;
                }
                // Search into Bing web search API
                //bingResult = GetBingWebSearchResult((string)stepContext.Result).Result.WebPages.Value;
            }

            
            // Create reply
            var reply = stepContext.Context.Activity.CreateReply();


            if (bingNewResult != null && bingNewResult.Count > 0 && counter < bingNewResult.Count && counter < bingNewResult.Count && userProfile.CurrentAriOptions == Convert.ToString(AriBotV4.Enums.AskAri.News))

            {

                for (int i = 0; i <= bingNewResult.Count - 1; i++)
                {
                    reply.Attachments.Add(CreateNewsHeroCard(bingNewResult[i]));

                }

                if (reply.Attachments.Count == 0)
                {
                    reply.Text = Convert.ToString(SharedStrings.NotFound);
                }
                await stepContext.Context.SendActivityAsync(reply, cancellationToken);

            }

            else if (bingImageResult != null && bingImageResult.Count > 0 && counter < bingImageResult.Count && userProfile.CurrentAriOptions == Convert.ToString(AriBotV4.Enums.AskAri.Images))
            {

                for (int i = 0; i <= bingImageResult.Count - 1; i++)
                {

                    bool isValidImage = IsUrlImage(bingImageResult[i].ContentUrl);
                    if (isValidImage)
                    {
                        reply.Attachments.Add(CreateImageHeroCard(bingImageResult[i]));


                    }
                    

                }
                if(reply.Attachments.Count == 0)
                {
                    reply.Text = Convert.ToString(SharedStrings.NotFound);
                }
                await stepContext.Context.SendActivityAsync(reply, cancellationToken);

            }

            else if (bingVideoResult != null && bingVideoResult.Count > 0)
            {
                reply.Attachments.Add(CreateVideoCard(bingVideoResult[counter]));
                await stepContext.Context.SendActivityAsync(reply, cancellationToken);
            }

            else
            {
                // Results found in our knowledge base
                if (counter == 0 && (qnaResult != null && !qnaResult.Contains(SearchAri.QnAMakerAnswerNotFound)))
                {
                    if (CreateQnASearchHeroCard(qnaResult) != null)
                    {
                        reply.Attachments.Add(CreateQnASearchHeroCard(qnaResult));
                        await stepContext.Context.SendActivityAsync(reply, cancellationToken);
                    }
                    else
                        // await stepContext.Context.SendActivityAsync(reply, cancellationToken);
                        await stepContext.Context.SendActivityAsync(qnaResult);
                }
                // Results found in Bing
                else
                {


                    if (bingResult != null && bingResult.Count > 0 && userProfile.CurrentAriOptions == Convert.ToString(AriBotV4.Enums.AskAri.General))
                    {

                        for (int i = 0; i <= bingResult.Count - 1; i++)
                        {

                            reply.Attachments.Add(CreateWebSearchHeroCard(bingResult[i]));
                            Models.TaskSpur.Auth.TokenResponse tokenResponse = await _botStateService._taskSpurApiClient.GetToken();
                            try
                            {

                                if (ariQuestionRequest == null)
                                {
                                    ariQuestionRequest = new AriQuestionRequest();
                                    ariQuestionResponse = new AriQuestionResponse();
                                    ariQuestionRequest.AriTrainingType = (int)AriTrainingType.General;
                                }
                                if (i == 0)
                                {
                                    ariQuestionRequest.Question = (string)stepContext.Result;
                                    if (!string.IsNullOrEmpty(Convert.ToString(stepContext.Context.Activity.From.Properties[AriBotV4.Common.Constants.TaskSpurTimeZone])))
                                    {
                                        ariQuestionRequest.TimeZone = Convert.ToString(stepContext.Context.Activity.From.Properties[AriBotV4.Common.Constants.TaskSpurTimeZone]);
                                    }
                                    if (!string.IsNullOrEmpty(Convert.ToString(stepContext.Context.Activity.From.Properties[AriBotV4.Common.Constants.TaskSpurUserId])))
                                    {
                                        ariQuestionRequest.UserId = Convert.ToString(stepContext.Context.Activity.From.Properties[AriBotV4.Common.Constants.TaskSpurUserId]);
                                        ariQuestionUpdateRequest.UserId = Convert.ToString(stepContext.Context.Activity.From.Properties[AriBotV4.Common.Constants.TaskSpurUserId]);
                                    }

                                    ariQuestionRequest.Version = Convert.ToDecimal(Constants.Version);

                                    ariQuestionRequest.Answer = bingResult[i].Url;

                                    var queryEnitity = await GetBingEntitySearchResult((string)stepContext.Result);
                                    if (queryEnitity.Entities != null)
                                    {
                                        ariQuestionRequest.Keywords = queryEnitity.Entities.Value[0].Name;
                                        ariQuestionRequest.EntityScenario = queryEnitity.Entities.Value[0].EntityPresentationInfo.EntityScenario;
                                        ariQuestionRequest.EntityTypeHint = queryEnitity.Entities.Value[0].EntityPresentationInfo.EntityTypeHints[0];
                                    }

                                    if (tokenResponse?.data != null)
                                    {
                                        ariQuestionResponse = await _botStateService._ariQuestionsApiClient.CreateAriQuestion(ariQuestionRequest, tokenResponse.data.token);

                                        //if (ariQuestionResponse.statusCode == 401)
                                        //{

                                        //    // Get refersh token
                                        //    Models.TaskSpur.Auth.TokenResponse refreshTokenResponse = await _botStateService._taskSpurApiClient.GetRefreshToken(null, tokenResponse.data.token, tokenResponse.data.refreshToken);
                                        //    // Create goal with refresh token
                                        //    if (!string.IsNullOrEmpty(refreshTokenResponse.data.token))
                                        //    {

                                        //        _botStateService._ariQuestionsApiClient.CreateAriQuestion(ariQuestionRequest, tokenResponse.data.token);

                                        //    }

                                        //}
                                    }

                                }

                                if (i > 0)
                                {
                                    if (ariQuestionUpdateRequest == null)
                                    {

                                        ariQuestionUpdateRequest = new AriQuestionUpdateRequest();
                                        ariQuestionUpdateRequest.AriTrainingType = (int)AriTrainingType.General;
                                    }


                                    if (tokenResponse.data != null)
                                    {
                                        if (ariQuestionResponse.data != null)
                                            ariQuestionUpdateRequest.Id = ariQuestionResponse.data.id;
                                        ariQuestionUpdateRequest.Answer = bingResult[i].Url;
                                        ariQuestionResponse = await _botStateService._ariQuestionsApiClient.UpdateAriQuestion(ariQuestionUpdateRequest, tokenResponse.data.token);

                                        //if (ariQuestionResponse.statusCode == 401)
                                        //{

                                        //    // Get refersh token
                                        //    Models.TaskSpur.Auth.TokenResponse refreshTokenResponse = await _botStateService._taskSpurApiClient.GetRefreshToken(null, tokenResponse.data.token, tokenResponse.data.refreshToken);
                                        //    // Create goal with refresh token
                                        //    if (!string.IsNullOrEmpty(refreshTokenResponse.data.token))
                                        //    {

                                        //        ariQuestionResponse = await _botStateService._ariQuestionsApiClient.UpdateAriQuestion(ariQuestionUpdateRequest, tokenResponse.data.token);

                                        //    }

                                        //}

                                    }

                                }
                            }
                            catch (Exception ex)
                            {
                               
                                await _botStateService._ariQuestionsApiClient.SendEmail(tokenResponse.data.token, ex.Message);

                            }
                        }
                        await stepContext.Context.SendActivityAsync(reply, cancellationToken);
                    }
                    else
                    {
                        await stepContext.Context.SendActivityAsync(SearchAri.NoResultsFound);
                        return await stepContext.BeginDialogAsync($"{nameof(AnythingElseDialog)}.AnythingElse", userProfile.CurrentAriOptions, cancellationToken);


                    }
                    //isAriQuestionCreated = true;
                }
            }


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
                    Text = (!string.IsNullOrEmpty(qnaResult) && (!qnaResult.Contains(SearchAri.QnAMakerAnswerNotFound))) ? SearchAri.Confirmation :
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



            return await stepContext.PromptAsync($"{nameof(AskAriDialog)}.details", confirmUser);

        }


        // Get current time for the user
        private async Task<DialogTurnResult> GetTimeFinalResults(WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
            userProfile.CurrentAriOptions = userProfile.CurrentAriOptions;
            await _botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);
            try
            {
                
                // Check whether user is asking for specific country time
                if (luisResponse.Entities.geographyV2 != null)
                {
                    var getZoneId = TzdbDateTimeZoneSource.Default.ZoneLocations.Where(x => x.CountryName.ToLower()
                         == (luisResponse.Entities.geographyV2[0].Location.ToLower())).AsQueryable();

                    if (getZoneId.Count() != 1)
                        getZoneId = TzdbDateTimeZoneSource.Default.ZoneLocations.Where(x => x.ZoneId.ToLower()
                       .Contains(luisResponse.Entities.geographyV2[0].Location.ToLower())).AsQueryable();

                    if (getZoneId.Count() != 1)
                        getZoneId = TzdbDateTimeZoneSource.Default.ZoneLocations.Where(x => x.ZoneId.ToLower()
                       .Contains((string)stepContext.Result.ToString().ToLower())).AsQueryable();

                    if (getZoneId.Count() > 0)
                    {
                        // Get local date time
                        DateTime localDate = DateTime.UtcNow;
                        DateTime utcTime = localDate.ToUniversalTime();
                        // Get time info for user timezone
                        TimeZoneInfo timeInfo = TZConvert.GetTimeZoneInfo(getZoneId.First().ZoneId);
                        // Convert time to UTC
                        DateTime userDateTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, timeInfo);

                        await stepContext.Context.SendActivityAsync(MessageFactory.Text("It's " +
                       userDateTime.Date.ToString(Constants.DateFormat) + " " +
                       string.Format(Constants.TimeFormat, userDateTime)));
                    }
                    else
                    {
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text
                            (SearchAri.NoResultsFound));
                    }

                }

                else if (!string.IsNullOrEmpty(Convert.ToString(stepContext.Context.Activity.From.Properties[Constants.TaskSpurTimeZone])))
                {


                    // Get local date time
                    DateTime localDate = DateTime.UtcNow;
                    DateTime utcTime = localDate.ToUniversalTime();
                    // Get time info for user timezone
                    TimeZoneInfo timeInfo = TZConvert.GetTimeZoneInfo(Convert.ToString(stepContext.Context.Activity.From.Properties[Constants.TaskSpurTimeZone]));
                    // Convert time to UTC
                    DateTime userDateTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, timeInfo);

                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("It's " +
                        userDateTime.Date.ToString(Constants.DateFormat) + " " +
                        string.Format(Constants.TimeFormat, userDateTime)));
                }
                else
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(SearchAri.NoResultsFound));
                }

                // Clear luis result and luis intent
                luisResponse = null;
                // Begin ask anything else dialog 
                return await stepContext.BeginDialogAsync($"{nameof(AnythingElseDialog)}.AnythingElse", null, cancellationToken);
            }
            catch (Exception ex)
            {
                // Begin ask anything else dialog 
                return await stepContext.BeginDialogAsync($"{nameof(AnythingElseDialog)}.AnythingElse", null, cancellationToken);
            }

        }

        // Confirmation from user whether results are satisfied or not
        private async Task<DialogTurnResult> GetConfirmationAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
            userProfile.CurrentAriOptions = userProfile.CurrentAriOptions;
            await _botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);
            if (luisResponse == null || stepContext.Result == null)
            {
                return await FinalAsync(stepContext, cancellationToken);
            }
            else
            if (Convert.ToString(luisResponse.TopIntent().intent) == Constants.TimeIntent  && luisResponse.Entities._instance.time != null 
                && (luisResponse.Entities._instance.datetime != null || luisResponse.Entities._instance.geographyV2 != null
                    || luisResponse.TopIntent().score >= 0.99))
            {
                return await GetTimeFinalResults(stepContext, cancellationToken);
            }

            else
            {

                Type stringType = typeof(System.String);
                Type resultType = stepContext.Result.GetType();

                string selectedChoice = (resultType == stringType ? Convert.ToString(stepContext.Result) :
                Convert.ToString(((FoundChoice)stepContext.Result)));

                bool isResponseYes = Constants.YesLibrary.Any(str => str.ToLower() == (selectedChoice.ToLower()));
                bool isResponseNo = Constants.NoLibrary.Any(str => str.ToLower() == (selectedChoice.ToLower()));

                // User satisfied with results
                
                if (isResponseYes)
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(Utility.GenerateRandomMessages(Constants.HappyWithResults)),
                        cancellationToken);
                    // Counter to restart
                    counter = 0;
                    bingImageResult = null;
                    bingNewResult = null;
                    bingVideoResult = null;
                    bingResult = null;
                    ariQuestionRequest = null;
                    ariQuestionUpdateRequest = null;


                    // calling next waterfall step

                    // return await FinalAsync(stepContext, cancellationToken);
                    return await stepContext.BeginDialogAsync($"{nameof(AnythingElseDialog)}.AnythingElse", userProfile.CurrentAriOptions, cancellationToken);

                }
                // User not satisfied with results
                else
                {

                    

                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(Utility.GenerateRandomMessages(Constants.SearchAriImprove)), cancellationToken);

                    // Save any state changes that might have occured during the turn.
                    await _botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);

                    // Send mail to admin query which is not found in knowledge base
                    var alert = new EmailMessage
                    {
                        ToAddress = _botStateService._appSetting.EmailAdminToNotify,
                        FromAddress = userProfile.Email,
                        Subject = SharedStrings.AriBot + userProfile.Subject,
                        Content = userProfile.Name + ", " + userProfile.Email + " : " + userProfile.Details
                    };
                    await _botStateService._emailService.SendAsync(alert);

                    //Reset the conversation details
                    userProfile.Details = "";

                    // Ask user for more information
                    // Ask to user for more information
                    var confirmUser = new PromptOptions
                    {
                        Prompt = MessageFactory.Text(Utility.GenerateRandomMessages(Constants.AskMoreInfo)),
                        Choices = ChoiceFactory.ToChoices(new List<string>
                {
                        SearchAri.ConfirmAskForMoreInfo,
                        SearchAri.ConfirmFeedback
                })
                    };
                    return await stepContext.PromptAsync($"{nameof(AskAriDialog)}.WasIUseful", confirmUser, cancellationToken);

                    //}
                }
            }
        }

        private static async Task<bool> ValidateChoice(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            bool isResponseYes = Constants.YesLibrary.Any(str => str.ToLower() == (promptContext.Recognized.Value.ToLower()));
            bool isResponseNo = Constants.NoLibrary.Any(str => str.ToLower() == (promptContext.Recognized.Value.ToLower()));
            if (isResponseYes)
                return true;
            else if (isResponseNo)
                return true;

            else
                return false;
        }

        // Asking for more detail information and Feedback
        private async Task<DialogTurnResult> GetMoreInformation(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
            userProfile.CurrentAriOptions = userProfile.CurrentAriOptions;
            await _botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);

            if (stepContext.Context.Activity.Text != "No")
            {
                //if (counter > 0)
                //{
                // Counter to restart
                counter = 0;
                bingImageResult = null;
                bingNewResult = null;
                bingVideoResult = null;
                bingResult = null;

                ariQuestionRequest = new AriQuestionRequest();
                ariQuestionUpdateRequest = new AriQuestionUpdateRequest();


                var selectedChoice = ((FoundChoice)stepContext.Result).Value;
                if (userProfile.CurrentAriOptions == Convert.ToString(AriBotV4.Enums.AskAri.General))
                {

                    ariQuestionRequest.AriTrainingType = (int)AriTrainingType.Elaborate;
                    ariQuestionUpdateRequest.AriTrainingType = (int)AriTrainingType.Elaborate;
                    if (ariQuestionResponse != null)
                        ariQuestionRequest.LinkedQuestionId = ariQuestionResponse.data.id;

                    isAriQuestionCreated = false;
                }

                // User satisfied with results
                if (selectedChoice.Contains(SearchAri.ConfirmAskForMoreInfo))
                {

                    return await GetDetailedInformation(stepContext, cancellationToken);
                    // return await stepContext.BeginDialogAsync($"{nameof(AskAriDialog)}.AskAri", null, cancellationToken);
                }
                // Ask for feedback
                else
                {
                    return await stepContext.BeginDialogAsync($"{nameof(FeedbackDialog)}.Feedback", ariQuestionResponse, cancellationToken);
                }
            }
            else
            {
                // If user is satisfied with results move to next step
                return await stepContext.NextAsync(null, cancellationToken);

            }
        }

        // Get more information from user when he is not satisfied
        private async Task<DialogTurnResult> GetDetailedInformation(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync($"{nameof(WasIUsefulDialog)}.details",
                   new PromptOptions
                   {
                       Prompt = MessageFactory.Text(Utility.GenerateRandomMessages(Constants.ElaborateMoreDetails))
                   }, cancellationToken);

            //return await GeFinalResults(stepContext, cancellationToken);

        }

        // Final step to end dialog in current context
        private async Task<DialogTurnResult> FinalAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Result != null)
            {

                stepContext.ActiveDialog.State["stepIndex"] = (int)stepContext.ActiveDialog.State["stepIndex"] - 3;
                return await GetQnAOrBingFinalResults(stepContext, cancellationToken);
            }
            else
            {
                UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
                userProfile.LastMessageReceived = DateTime.UtcNow;
                await _botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);

                return await stepContext.EndDialogAsync(null, cancellationToken);
            }
        }

        #endregion

        #region Search API

        // Bing web search API
        private async Task<Microsoft.Azure.CognitiveServices.Search.WebSearch.Models.SearchResponse> GetBingWebSearchResult(string query, string filter = "", int offset = 0)
        {
            try
            {
                //IList<string> promote = new IList<string>() { "images", "videos" };
                var client = new Microsoft.Azure.CognitiveServices.Search.WebSearch.WebSearchClient(new Microsoft.Azure.CognitiveServices.Search.WebSearch.ApiKeyServiceClientCredentials
                    (_botStateService._bingSettings.BingSubcriptionKey));
                return await client.Web.SearchAsync(query: query, offset: offset, count: _botStateService._bingSettings.BingResultCount
                 //freshness: _botStateService._bingSettings.Freshness
                 );
            }
            catch
            {
                return null;
            }
        }

        // Bing web search API
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

        // Bing news search API
        private async Task<IList<Microsoft.Azure.CognitiveServices.Search.NewsSearch.Models.NewsArticle>> GetBingNewsSearchResult
    (string query, string filter = "", int offset = 0)
        {
            try
            {
                var client = new NewsSearchClient(new Microsoft.Azure.CognitiveServices.Search.WebSearch.ApiKeyServiceClientCredentials
                    (_botStateService._bingSettings.BingSubcriptionKey));
                return client.News.SearchAsync(query: query, offset: offset, count: _botStateService._bingSettings.BingResultCount,
                    market: _botStateService._bingSettings.Market
                    //freshness: _botStateService._bingSettings.Freshness
                    ).Result.Value;


            }
            catch (Exception ex)
            {
                return null;
            }
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



        // Bing video search API
        private async Task<IList<Microsoft.Azure.CognitiveServices.Search.VideoSearch.Models.VideoObject>> GetBingVideosSearchResult
            (string query, string filter = "", int offset = 0)
        {
            try
            {
                var client = new VideoSearchClient(new Microsoft.Azure.CognitiveServices.Search.WebSearch.ApiKeyServiceClientCredentials
                    (_botStateService._bingSettings.BingSubcriptionKey));
                //return client.Videos.GetAsync(query: query, offset: offset, count: _botStateService._bingSettings.BingResultCount,
                //    market: _botStateService._bingSettings.Market, freshness: _botStateService._bingSettings.Freshness).Result.Value;
                return client.Videos.SearchAsync(query: query, offset: offset, count: _botStateService._bingSettings.BingResultCount,
                    market: _botStateService._bingSettings.Market).Result.Value;


            }
            catch (Exception ex)
            {
                return null;
            }
        }

        // QnA Knowledge base search
        private string GetQnaSearchResult(string query)
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
                    var qnaFirstAnswer = qnaSearchList.Answers[0].Answer;
                    var qnaScore = qnaSearchList.Answers[0].Score;
                    if (!qnaFirstAnswer.ToLower().Equals(qnaMakerAnswerNotFound) && qnaScore >= _botStateService._qnaSettings.ScorePercentage)
                        return qnaFirstAnswer;
                }
                return qnaMakerAnswerNotFound;

            }
            catch
            {
                return null;
            }
        }
        #endregion

        #region Methods

        // check if URL contains image
        public static bool IsUrlImage(string url)
        {
            try
            {
                var req = (HttpWebRequest)HttpWebRequest.Create(url);
                req.Method = "HEAD";
                using (var resp = req.GetResponse())
                {
                    return resp.ContentType.ToLower(CultureInfo.InvariantCulture)
                               .StartsWith("image/");
                }
            }
            catch (Exception ex)
            {
                return false;
            }
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

        // Create news card
        private static Attachment CreateNewsHeroCard
            ([Optional] Microsoft.Azure.CognitiveServices.Search.NewsSearch.Models.NewsArticle article)
        {
            ThumbnailCard heroCard;
            if (article != null)
            {
                heroCard = new ThumbnailCard()
                {
                    Title = article.Name,
                    Subtitle = article.DatePublished.ToString(),
                    Text = article.Description,
                    Images = new List<CardImage>
                    {
                        new CardImage(article.Image!= null ? article.Image.Thumbnail.ContentUrl : ""),
                    },
                    Buttons = new List<CardAction>
                    {
                        new CardAction(ActionTypes.OpenUrl, "View Article", value: article.Url),
                    },
                };
            }
            else
            {
                heroCard = new ThumbnailCard()
                {
                    Title = "Would you like to see more details?",
                    Buttons = new List<CardAction>
                    {
                        new CardAction(ActionTypes.ImBack, "Click here"),
                    },
                };
            }
            return heroCard.ToAttachment();
        }


       

        // Create video card
        private static Attachment CreateVideoCard(Microsoft.Azure.CognitiveServices.Search.VideoSearch.Models.VideoObject video)
        {
            try
            {
                Task.Delay(2000);
                var videoCard = new VideoCard
                {
                    // Title = video.Name,
                    //Subtitle = video.Text,

                    //Text = video.Image.Thumbnail.Text,
                    //Image = new ThumbnailUrl
                    //{
                    //    Url = video.Image.Thumbnail.ThumbnailUrl,
                    //},
                    Media = new List<MediaUrl>
                {
                    new MediaUrl()
                    {
                       // Url = video.MotionThumbnailUrl.Replace("https","http") ,
                       Url = video.ContentUrl.Replace("https","http"),
                    },
                },
                    Buttons = new List<CardAction>
                {
                    new CardAction()
                    {
                        Title = "Learn More",
                        Type = ActionTypes.OpenUrl,
                        Value =  "Learn More",

                    },
                },
                };

                return videoCard.ToAttachment();
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        #endregion
    }
}
