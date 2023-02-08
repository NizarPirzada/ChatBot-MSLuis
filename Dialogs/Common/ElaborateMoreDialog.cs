using AriBotV4.Common;
using AriBotV4.Dialogs.AskAri.Resources;
using AriBotV4.Dialogs.Common.Resources;
using AriBotV4.Enums;
using AriBotV4.Models;
using AriBotV4.Models.Common;
using AriBotV4.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AriBotV4.Dialogs.Common
{
    public class ElaborateMoreDialog : ComponentDialog
    {
        #region Properties and Fields
        private readonly BotStateService _botStateService;
        
        #endregion

        #region WaterFallSteps and Dialogs
        public ElaborateMoreDialog(string dialogId, BotStateService botStateService) : base(dialogId)
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
                ElaborateMore,
                GetConfirmation,
                RecallMainAction
                //FinalAsync
            };

            // Add Named Dialogs
            AddDialog(new WaterfallDialog($"{nameof(ElaborateMoreDialog)}.mainFlow", waterfallSteps));
            
            AddDialog(new ChoicePrompt($"{nameof(ElaborateMoreDialog)}.ElaborateMore"));
            AddDialog(new TextPrompt($"{nameof(ElaborateMoreDialog)}.GetDetailedInformation"));
            //AddDialog(new WeatherDialog($"{nameof(WeatherDialog)}.mainFlow", _botStateService));
            // Set the starting Dialog
            InitialDialogId = $"{nameof(ElaborateMoreDialog)}.mainFlow";
        }

        #endregion
        private async Task<DialogTurnResult> ElaborateMore(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            string query = (string)stepContext.ActiveDialog.State["options"];

            
            // await stepContext.Context.SendActivityAsync(MessageFactory.Text(Utility.GenerateRandomMessages(Constants.SearchAriImprove)), cancellationToken);
            await stepContext.Context.SendActivityAsync(MessageFactory.Text(SharedStrings.SorryNotFound), cancellationToken);

            // get bing web search count
            WebSearchCountResponse response = await _botStateService._taskSpurApiClient.GetBingCounter(DateTime.Now.ToString("MMMM"),
            DateTime.Now.ToString("yyyy"));


            //if (response.data.count < 1000)
            List<string> elaborateMore = new List<string>();
            if (response.data.count < 1000)
                elaborateMore.Add(SearchAri.Searchinternet);
            elaborateMore.Add(SearchAri.ConfirmAskForMoreInfo);
            elaborateMore.Add(SearchAri.ConfirmFeedback);
            elaborateMore.Add(SearchAri.Goodbye);
            //{
            //    response.data.count < 1000 ? SearchAri.Searchinternet : null,
            //            SearchAri.ConfirmAskForMoreInfo,
            //            SearchAri.ConfirmFeedback,
            //            SearchAri.Goodbye
            //    }


            var confirmUser = new PromptOptions
            {
                Prompt = MessageFactory.Text(SharedStrings.proceed),
                Choices = ChoiceFactory.ToChoices(elaborateMore
            )
            };
            return await stepContext.PromptAsync($"{nameof(ElaborateMoreDialog)}.ElaborateMore", confirmUser, cancellationToken);

        }

        private async Task<DialogTurnResult> GetConfirmation(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var selectedChoice = ((FoundChoice)stepContext.Result).Value;
            if (selectedChoice.Contains(SearchAri.ConfirmAskForMoreInfo))
            {

                return await GetDetailedInformation(stepContext, cancellationToken);

            }
            // Ask for feedback
            else if (selectedChoice.Contains(SearchAri.ConfirmFeedback))
            {
               // await stepContext.EndDialogAsync(null, cancellationToken);
                return await stepContext.BeginDialogAsync($"{nameof(FeedbackDialog)}.Feedback", null, cancellationToken);
            }
            // Search in Internet
            else if (selectedChoice.Contains(SearchAri.Searchinternet))
            {
                WebSearchCountRequest webSearchCountRequest = new WebSearchCountRequest();
                webSearchCountRequest.Month = DateTime.Now.ToString("MMMM");
                webSearchCountRequest.Year = DateTime.Now.ToString("yyyy");
                await _botStateService._taskSpurApiClient.UpdateBingCounter(webSearchCountRequest);

                string searchType = string.Empty;
                string query = string.Empty;
                if (stepContext.ActiveDialog.State["options"].ToString().Contains("-"))
                {
                    searchType = stepContext.ActiveDialog.State["options"].ToString().Split('-')[0];
                    query = stepContext.ActiveDialog.State["options"].ToString().Split('-')[1];
                }
                



                if (EnumHelpers.GetEnumDescription(AriBotV4.Enums.AskAri.News)== searchType)
                {


                    return await stepContext.BeginDialogAsync($"{nameof(NewsDialog)}.mainFlow", query, cancellationToken);
                }
                else if (EnumHelpers.GetEnumDescription(AriBotV4.Enums.AskAri.Images) == searchType)
                {


                    return await stepContext.BeginDialogAsync($"{nameof(ImagesDialog)}.mainFlow", query, cancellationToken);
                }
                
                else
                {
                    return await stepContext.BeginDialogAsync($"{nameof(GeneralDialog)}.mainFlow", query, cancellationToken);
                }
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(Utility.GenerateRandomMessages(Constants.GoodByeLibrary)), cancellationToken);

                //await stepContext.Context.SendActivityAsync(MessageFactory.Text(SharedStrings.Goodbye), cancellationToken);
                return await stepContext.EndDialogAsync(null, cancellationToken);
            }
        }
        // Get more information from user when he is not satisfied
        private async Task<DialogTurnResult> GetDetailedInformation(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync($"{nameof(ElaborateMoreDialog)}.GetDetailedInformation",
                   new PromptOptions
                   {
                       Prompt = MessageFactory.Text(Utility.GenerateRandomMessages(Constants.ElaborateMoreDetails))
                   }, cancellationToken);

       

        }

        private async Task<DialogTurnResult> RecallMainAction(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            string searchType = string.Empty;
            string query = string.Empty;
            if (stepContext.ActiveDialog.State["options"].ToString().Contains("-"))
                         searchType = stepContext.ActiveDialog.State["options"].ToString().Split('-')[0];
            else
                searchType = stepContext.ActiveDialog.State["options"].ToString();

            // update the bng counter in DB
            WebSearchCountRequest webSearchCountRequest = new WebSearchCountRequest();
            webSearchCountRequest.Month = DateTime.Now.ToString("MMMM");
            webSearchCountRequest.Year = DateTime.Now.ToString("yyyy");
            await _botStateService._taskSpurApiClient.UpdateBingCounter(webSearchCountRequest);


            if (EnumHelpers.GetEnumDescription(AriBotV4.Enums.AskAri.News) == searchType)
            {
                return await stepContext.BeginDialogAsync($"{nameof(NewsDialog)}.mainFlow", stepContext.Context.Activity.Text, cancellationToken);
            }
            if (EnumHelpers.GetEnumDescription(AriBotV4.Enums.AskAri.Images) == searchType)
            {
                return await stepContext.BeginDialogAsync($"{nameof(ImagesDialog)}.mainFlow", stepContext.Context.Activity.Text, cancellationToken);
            }
            if (EnumHelpers.GetEnumDescription(AriBotV4.Enums.AskAri.General) == searchType)
            {
                return await stepContext.BeginDialogAsync($"{nameof(GeneralDialog)}.mainFlow", stepContext.Context.Activity.Text, cancellationToken);
            }
            else
            {
                await stepContext.EndDialogAsync(null, cancellationToken);
                return await stepContext.BeginDialogAsync($"{nameof(RootDialog)}.contactOptions", stepContext.Context.Activity.Text, cancellationToken);
            }


        }
    }
    
}
