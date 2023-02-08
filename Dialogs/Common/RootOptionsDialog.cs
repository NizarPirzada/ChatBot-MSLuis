using AriBotV4.Common;
using AriBotV4.Dialogs.AskAri.Resources;
using AriBotV4.Dialogs.Common;
using AriBotV4.Dialogs.Common.Resources;
using AriBotV4.Dialogs.Deals.Others;
using AriBotV4.Dialogs.Deals.Travel;
using AriBotV4.Dialogs.TaskSpur;
using AriBotV4.Enums;
using AriBotV4.Helpers;
using AriBotV4.Interface.Travel;
using AriBotV4.Models;
using AriBotV4.Models.Common;
using AriBotV4.Models.TaskSpur.Tasks;
using AriBotV4.Services;
using Azure;
using Azure.AI.TextAnalytics;
using LuisEntityHelpers;
using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace AriBotV4.Dialogs
{
    public class RootOptionsDialog : ComponentDialog
    {
        #region #Properties and Fields
        private readonly BotStateService _botStateService;
        private readonly BotServices _botServices;
        private readonly ITravelService _travelService;
        private readonly IConfiguration _configuration;
        private static  AzureKeyCredential credentials;
        private static  Uri endpoint;



        #endregion


        #region Method
        public RootOptionsDialog(string dialogId, BotStateService botStateService, BotServices botServices, ITravelService travelService, IConfiguration configuration) : base(dialogId)
        {
            _botStateService = botStateService ?? throw new System.ArgumentNullException(nameof(botStateService));
            _botServices = botServices ?? throw new System.ArgumentNullException(nameof(botServices));
            _travelService = travelService ?? throw new System.ArgumentNullException(nameof(travelService));
            _configuration = configuration ?? throw new System.ArgumentNullException(nameof(configuration));
            credentials = new AzureKeyCredential(botStateService._textAnalyticsSettings.Key);
            endpoint = new Uri(botStateService._textAnalyticsSettings.EndPoint);
            InitializeWaterfallDialog();
        }

        private void InitializeWaterfallDialog()
        {

            // Create Waterfall Steps
            var waterfallSteps = new WaterfallStep[]
            {
                ChoiceStepAsync,
                ChoiceResultStepAsync,
                FinalStepAsync
            };

            // Add Named Dialogs
            AddDialog(new WaterfallDialog($"{nameof(RootOptionsDialog)}.mainFlow", waterfallSteps));
            
            AddDialog(new ChoicePrompt($"{nameof(RootOptionsDialog)}.name"));
            AddDialog(new ContactProfilingDialog($"{nameof(RootOptionsDialog)}.contactProfiling", _botStateService));
            AddDialog(new ChoicePrompt($"{nameof(RootOptionsDialog)}.wasIHelpful"));
            AddDialog(new TextPrompt($"{nameof(RootOptionsDialog)}.details"));
            AddDialog(new OthersDialog($"{nameof(RootOptionsDialog)}.Others", _botStateService));
            AddDialog(new AskAriDialog($"{nameof(AskAriDialog)}.AskAri", _botStateService, _botServices));
            AddDialog(new TaskSpurDialog($"{nameof(TaskSpurDialog)}.TaskSpur", _botStateService, _botServices, _configuration));
            AddDialog(new AskAriDialog($"{nameof(AskAriDialog)}.SearchQueryAsync", _botStateService, _botServices));
            AddDialog(new WasIUsefulDialog($"{nameof(WasIUsefulDialog)}.wasIUseful", _botStateService));
            AddDialog(new AnythingElseDialog($"{nameof(AnythingElseDialog)}.AnythingElse", _botStateService));
            AddDialog(new FeedbackDialog($"{nameof(FeedbackDialog)}.Feedback", _botStateService));
            AddDialog(new SearchInternetDialog($"{nameof(SearchInternetDialog)}.Search", _botStateService));
            AddDialog(new WeatherDialog($"{nameof(WeatherDialog)}.mainFlow", _botStateService));
            AddDialog(new TimeDialog($"{nameof(TimeDialog)}.mainFlow", _botStateService));
            AddDialog(new NewsDialog($"{nameof(NewsDialog)}.mainFlow", _botStateService));
            AddDialog(new ImagesDialog($"{nameof(ImagesDialog)}.mainFlow", _botStateService));
            AddDialog(new DeleteGoalDialog($"{nameof(DeleteGoalDialog)}.mainFlow", _botStateService));
            AddDialog(new CreateGoalDialog($"{nameof(CreateGoalDialog)}.mainFlow", _botStateService));
            AddDialog(new EditGoalDialog($"{nameof(EditGoalDialog)}.mainFlow", _botStateService));
            AddDialog(new CreateTaskDialog($"{nameof(CreateTaskDialog)}.mainFlow", _botStateService, _botServices));
            AddDialog(new DeleteTaskDialog($"{nameof(DeleteTaskDialog)}.mainFlow", _botStateService, _botServices));
            AddDialog(new GetTasksDialog($"{nameof(GetTasksDialog)}.mainFlow", _botStateService, _botServices));
            AddDialog(new GetGoalsDialog($"{nameof(GetGoalsDialog)}.mainFlow", _botStateService, _botServices));
            AddDialog(new EditTaskDialog($"{nameof(EditTaskDialog)}.mainFlow", _botStateService, _botServices));
            AddDialog(new TaskSpurFeatureDialog($"{nameof(TaskSpurFeatureDialog)}.mainFlow", _botStateService, _botServices));
            AddDialog(new ScheduleTaskDialog($"{nameof(ScheduleTaskDialog)}.mainFlow", _botStateService, _botServices));
            AddDialog(new ElaborateMoreDialog($"{nameof(ElaborateMoreDialog)}.mainFlow", _botStateService));

            AddDialog(new GeneralDialog($"{nameof(GeneralDialog)}.mainFlow", _botStateService));
            AddDialog(new QnADialog($"{nameof(QnADialog)}.mainFlow", _botStateService));
            AddDialog(new FindDealsDialog($"{nameof(FindDealsDialog)}.FindDeals", _botStateService, _botServices, _travelService));
            AddDialog(new TextPrompt($"{nameof(RootOptionsDialog)}.userInput", Utility.ValidateUserInput));

            // Set the starting Dialog
            InitialDialogId = $"{nameof(RootOptionsDialog)}.mainFlow";

        }

        // Choose options for ARI
        private async Task<DialogTurnResult> ChoiceStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (_configuration.GetValue<bool>("TaskSpurToggleSettings:AriOptions"))
            {
                UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());


                var prompt = new PromptOptions
                {
                    Prompt = MessageFactory.Text(Utility.GenerateRandomMessages(Constants.ChooseDeals)),
                    Choices = ChoiceFactory.ToChoices(new List<string> {
                    EnumHelpers.GetEnumDescription(MainOptions.AskAri),
                    //EnumHelpers.GetEnumDescription(MainOptions.FindDeals),
                    EnumHelpers.GetEnumDescription(MainOptions.TaskSpur),
                    }),
                    Style = ListStyle.SuggestedAction,

                };

                return await stepContext.PromptAsync($"{nameof(RootOptionsDialog)}.name", prompt, cancellationToken);
            }
            else
            {
                string query = (string)stepContext.ActiveDialog.State["options"];
                if(!string.IsNullOrEmpty(query))
                return await stepContext.NextAsync(stepContext.Context.Activity.Text, cancellationToken);
                else
                return new DialogTurnResult(DialogTurnStatus.Waiting);

                //var prompt = new PromptOptions
                //{
                //    Prompt = MessageFactory.Text(TaskSpur.Resources.TaskSpur.GetTaskInput + Constants.HtmlBr + "Some examples:" + Constants.HtmlBr +
                //          Utility.GenerateRandomMessages(Constants.AskAriSample) + Constants.HtmlBr + Utility.GenerateRandomMessages(Constants.TasksCreateSample) + Constants.HtmlBr + Utility.GenerateRandomMessages(Constants.CalendarTasksSample)),


                //    Style = ListStyle.None

                //};
                //return await stepContext.PromptAsync($"{nameof(RootOptionsDialog)}.userInput", prompt, cancellationToken);
            }


        }

        private async Task<DialogTurnResult> ChoiceResultStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            
            //string getProjectSettings = Utility.GetProjectSettings(Convert.ToInt32(stepContext.Context.Activity.From.Properties["project"]));
            string getProjectSettings = Utility.GetProjectSettings(Convert.ToInt32(_configuration.GetValue<int>("ProjectId")));


            if (_configuration.GetValue<bool>("TaskSpurToggleSettings:AriOptions"))
            {
                var selectedChoice = ((FoundChoice)stepContext.Result).Value;

                //var selectedChoice = (string)stepContext.Result;
                UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
                userProfile.Subject = selectedChoice;
                userProfile.LastMessageReceived = DateTime.UtcNow;
                await _botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);



                if (selectedChoice.Contains("I have an awesome idea"))
                {
                    return await stepContext.BeginDialogAsync($"{nameof(RootOptionsDialog)}.contactProfiling", null, cancellationToken);
                }
                else if (selectedChoice.Contains("Other"))
                {
                    return await stepContext.BeginDialogAsync($"{nameof(RootOptionsDialog)}.Others", null, cancellationToken);
                }
                else if (selectedChoice.Contains(EnumHelpers.GetEnumDescription(MainOptions.AskAri)))
                {
                    return await stepContext.BeginDialogAsync($"{nameof(AskAriDialog)}.AskAri", null, cancellationToken);
                }
                else if (selectedChoice.Contains(EnumHelpers.GetEnumDescription(MainOptions.FindDeals)))
                {
                    return await stepContext.BeginDialogAsync($"{nameof(FindDealsDialog)}.FindDeals", null, cancellationToken);
                }
                else if (selectedChoice.Contains(EnumHelpers.GetEnumDescription(MainOptions.TaskSpur)))
                {

                    return await stepContext.BeginDialogAsync($"{nameof(TaskSpurDialog)}.TaskSpur", null, cancellationToken);
                }
                else
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(SharedStrings.Sorry), cancellationToken);
                    await stepContext.ReplaceDialogAsync(InitialDialogId);
                }
            }
            else
            {
                if (stepContext.Context.Activity.Text != null)
                {

                    RecognizerResult recognizerResult = new RecognizerResult();
                    LuisModel luisResponse = new LuisModel();
                    // First, we use the dispatch model to determine which cognitive service (LUIS or Qna) to use
                    try
                    {
                        recognizerResult = await _botServices.Dispatch.RecognizeAsync(stepContext.Context, cancellationToken);
                        luisResponse = JsonConvert.DeserializeObject<LuisModel>(JsonConvert.SerializeObject(recognizerResult));
                        //Convert
                    }
                    catch (Exception ex)
                    {

                    }


                    stepContext.EndDialogAsync(null, cancellationToken);

                    // Top intent tell us which cognitive service to use
                    LuisModel.Intent topIntent = new LuisModel.Intent();
                    if (luisResponse != null)
                        topIntent = luisResponse.TopIntent().intent;

                    // Get QnA Scoring
                    QnAMaker qnAMaker = Utility.GetQnaSearchResult((string)(stepContext.Result), _botStateService);
                    //SearchType searchType1 = Utility.GetSearchType(luisResponse, qnAMaker);

                    if (qnAMaker != null && qnAMaker.Answers[0].Score >= 95)
                    {
                        return await stepContext.BeginDialogAsync($"{nameof(QnADialog)}.mainFlow", stepContext.Context.Activity.Text, cancellationToken);
                    }
                    else
                    {
                        if (_configuration.GetValue<bool>(getProjectSettings + ":Weather") && luisResponse.Entities._instance.weather != null)
                        {


                            return await stepContext.BeginDialogAsync($"{nameof(WeatherDialog)}.mainFlow", recognizerResult, cancellationToken);
                        }
                        else if (_configuration.GetValue<bool>(getProjectSettings + ":Time") && luisResponse.Entities._instance.time != null)
                        {


                            return await stepContext.BeginDialogAsync($"{nameof(TimeDialog)}.mainFlow", recognizerResult, cancellationToken);
                        }
                        else if (Constants.YesLibrary.Any(str => str.ToLower() == (stepContext.Context.Activity.Text)))
                        {

                            return await stepContext.BeginDialogAsync($"{nameof(RootDialog)}.contactOptions", null, cancellationToken);
                        }
                        else if (_configuration.GetValue<bool>(getProjectSettings + ":Tasks") && luisResponse.Entities._instance.create != null && luisResponse.Entities._instance.goal != null)
                        {

                            return await stepContext.BeginDialogAsync($"{nameof(CreateGoalDialog)}.mainFlow", null, cancellationToken);
                        }
                        else if (_configuration.GetValue<bool>(getProjectSettings + ":Tasks") && topIntent == LuisModel.Intent.Edit_Goal_Intent && luisResponse.Entities._instance.edit != null && luisResponse.Entities._instance.goal != null)
                        {

                            return await stepContext.BeginDialogAsync($"{nameof(EditGoalDialog)}.mainFlow", null, cancellationToken);
                        }
                        else if (_configuration.GetValue<bool>(getProjectSettings + ":Tasks") && topIntent == LuisModel.Intent.Get_Goals_Intent && luisResponse.Entities._instance.goal != null)
                        {
                            return await stepContext.BeginDialogAsync($"{nameof(GetGoalsDialog)}.mainFlow", recognizerResult, cancellationToken);
                        }
                        else if (_configuration.GetValue<bool>(getProjectSettings + ":Tasks") && luisResponse.Entities._instance.delete != null && luisResponse.Entities._instance.goal != null)
                        {

                            return await stepContext.BeginDialogAsync($"{nameof(DeleteGoalDialog)}.mainFlow", null, cancellationToken);
                        }
                        else if (_configuration.GetValue<bool>(getProjectSettings + ":Tasks") && topIntent == LuisModel.Intent.Edit_Task_Intent && luisResponse.Entities._instance.edit != null && luisResponse.Entities._instance.task != null)
                        {

                            return await stepContext.BeginDialogAsync($"{nameof(EditTaskDialog)}.mainFlow", null, cancellationToken);

                        }
                        //else if (_configuration.GetValue<bool>(getProjectSettings + ":Tasks") && luisResponse.Entities._instance.TaskGoalName != null || topIntent == LuisModel.Intent.Create_Task || luisResponse.Entities._instance.Status != null)
                            else if (_configuration.GetValue<bool>(getProjectSettings + ":Tasks") && luisResponse.Entities._instance.TaskGoalName != null || topIntent == LuisModel.Intent.Create_Task)
                                {

                            return await stepContext.BeginDialogAsync($"{nameof(TaskSpurFeatureDialog)}.mainFlow", luisResponse, cancellationToken);
                        }
                        //else if (_configuration.GetValue<bool>(getProjectSettings + ":Tasks") && (topIntent == LuisModel.Intent.Create_Task || luisResponse.Entities._instance.Status != null))
                            else if (_configuration.GetValue<bool>(getProjectSettings + ":Tasks") && (topIntent == LuisModel.Intent.Create_Task))
                                {


                            return await stepContext.BeginDialogAsync($"{nameof(CreateTaskDialog)}.mainFlow", luisResponse, cancellationToken);

                        }

                        //else if (_configuration.GetValue<bool>(getProjectSettings + ":Tasks") && (luisResponse.Entities._instance.schedule != null && luisResponse.Entities._instance.task != null))
                        //{


                        //    return await stepContext.BeginDialogAsync($"{nameof(ScheduleTaskDialog)}.mainFlow", luisResponse, cancellationToken);
                        //}


                        else if (_configuration.GetValue<bool>(getProjectSettings + ":Tasks") && topIntent == LuisModel.Intent.Delete_Task_Intent && luisResponse.Entities._instance.delete != null && luisResponse.Entities._instance.task != null)
                        {

                            return await stepContext.BeginDialogAsync($"{nameof(DeleteTaskDialog)}.mainFlow", null, cancellationToken);
                        }
                        //else if (_configuration.GetValue<bool>(getProjectSettings + ":Tasks") && (luisResponse.Entities._instance.tasks != null || luisResponse.Entities._instance.appointment != null || luisResponse.Entities._instance.meeting != null) && luisResponse.Entities._instance.create == null)
                        else if (_configuration.GetValue<bool>(getProjectSettings + ":Tasks") && (topIntent == LuisModel.Intent.Get_Tasks_Intent))
                        {

                            return await stepContext.BeginDialogAsync($"{nameof(GetTasksDialog)}.mainFlow", null, cancellationToken);
                        }
                        else
                        {
                            // get bing web search count
                            WebSearchCountResponse response = await _botStateService._taskSpurApiClient.GetBingCounter(DateTime.Now.ToString("MMMM"),
                            DateTime.Now.ToString("yyyy"));
                            if(luisResponse.Entities._instance.WebSearch != null && response.data != null && response.data.count < 1000)
                            {
                                // Add or update bing web search count
                                WebSearchCountRequest webSearchCountRequest = new WebSearchCountRequest();
                                webSearchCountRequest.Month = DateTime.Now.ToString("MMMM");
                                webSearchCountRequest.Year = DateTime.Now.ToString("yyyy");
                                await _botStateService._taskSpurApiClient.UpdateBingCounter(webSearchCountRequest);
                                if (_configuration.GetValue<bool>(getProjectSettings + ":AskAri") && luisResponse.Entities._instance.news != null)
                                {


                                    return await stepContext.BeginDialogAsync($"{nameof(NewsDialog)}.mainFlow", stepContext.Context.Activity.Text, cancellationToken);
                                }
                                else if (_configuration.GetValue<bool>(getProjectSettings + ":AskAri") && luisResponse.Entities._instance.images != null)
                                {


                                    return await stepContext.BeginDialogAsync($"{nameof(ImagesDialog)}.mainFlow", stepContext.Context.Activity.Text, cancellationToken);
                                }


                                else
                                {
                                    return await stepContext.BeginDialogAsync($"{nameof(GeneralDialog)}.mainFlow", stepContext.Context.Activity.Text, cancellationToken);
                                }
                            }
                            else if(luisResponse.Entities._instance.WebSearch != null && response.data.count >= 1000)
                            {
                                stepContext.Context.SendActivityAsync(MessageFactory.Text(SharedStrings.WebSearchExpired), cancellationToken);
                                return await stepContext.BeginDialogAsync($"{nameof(AnythingElseDialog)}.AnythingElse", null, cancellationToken);
                            }
                            else
                            {

                                string searchType = string.Empty;
                                if (luisResponse.Entities._instance.images != null)
                                    searchType = EnumHelpers.GetEnumDescription(AriBotV4.Enums.AskAri.Images);
                                else if (luisResponse.Entities._instance.news != null)
                                    searchType = EnumHelpers.GetEnumDescription(AriBotV4.Enums.AskAri.News);
                                else
                                    searchType = EnumHelpers.GetEnumDescription(AriBotV4.Enums.AskAri.General);
                                return await stepContext.BeginDialogAsync($"{nameof(ElaborateMoreDialog)}.mainFlow", searchType + "-" + stepContext.Context.Activity.Text, cancellationToken);
                            }
                           
                        }



                    }
                }
            }
            return await stepContext.NextAsync(null, cancellationToken);

        }




        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.EndDialogAsync(null, cancellationToken);
        }



    

        #endregion

    }
}
