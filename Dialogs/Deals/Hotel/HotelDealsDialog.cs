using AriBotV4.Enums;
using AriBotV4.Helpers;
using AriBotV4.Models;
using AriBotV4.Services;
using AriBotV4.Common;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using AriBotV4.Dialogs.Common;
using AriBotV4.Models.Hotel;
using AriBotV4.Dialogs.Deals.Hotel.Resources;
using AriBotV4.Dialogs.Common.Resources;

namespace AriBotV4.Dialogs.Deals.Hotel
{
    public class HotelDealsDialog : ComponentDialog
    {
        #region properties and Fields
        private readonly BotStateService _botStateService;
        private readonly BotServices _botServices;
        private HotelRequest _hotelDealInput;
        #endregion 

        #region Method
        public HotelDealsDialog(string dialogId, BotStateService botStateService, BotServices botServices) : base(dialogId)
        {
            _botStateService = botStateService ?? throw new System.ArgumentNullException(nameof(botStateService));
            _botServices = botServices ?? throw new System.ArgumentNullException(nameof(botServices));

            InitializeWaterfallDialog();
        }

        private void InitializeWaterfallDialog()
        {
            // Create Waterfall Steps
            var waterfallSteps = new WaterfallStep[]
            {
               WhatSpecificAsync,
               GetLuisIntentsAsync,
               GetApiResultsThenAskApprovalAsync,
               AskApprovalOfCardAsync,
               UseCardAsync,
               EndDialogAsync
            };

            // Add Named Dialogs (these doesn't dictate the flow, waterfall does!)
            AddDialog(new WaterfallDialog($"{nameof(HotelDealsDialog)}.mainFlow", waterfallSteps));
            AddDialog(new TextPrompt($"{nameof(HotelDealsDialog)}.detailsBooking"));
            AddDialog(new TextPrompt($"{nameof(HotelDealsDialog)}.hotelDealWhatPrice"));
            AddDialog(new ChoicePrompt($"{nameof(HotelDealsDialog)}.approveDeal"));
            AddDialog(new ChoicePrompt($"{nameof(HotelDealsDialog)}.approveCard"));

            // Set the starting Dialog
            InitialDialogId = $"{nameof(HotelDealsDialog)}.mainFlow";
        }
               
        private async Task<DialogTurnResult> WhatSpecificAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync($"{nameof(HotelDealsDialog)}.detailsBooking",
                 new PromptOptions
                 {
                     Prompt = MessageFactory.Text(HotelDeal.AskHotelDetails)
                 }, cancellationToken);

        }

        private async Task<DialogTurnResult> GetLuisIntentsAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            //to deserialize LUIS result
            //https://stackoverflow.com/questions/56372143/how-can-i-access-an-entities-score-information-and-or-existence-from-the-luis-ai

            // First, we use the dispatch model to determine which cognitive service (LUIS or Qna) to use
            var recognizerResult = await _botServices.Dispatch.RecognizeAsync(stepContext.Context, cancellationToken);
            var luisResponse = JsonConvert.DeserializeObject<LuisModel>(JsonConvert.SerializeObject(recognizerResult));

            // Top intent tell us which cognitive service to use
            var topIntent = recognizerResult.GetTopScoringIntent();

            if (topIntent.intent == Constants.HotelIntent)
            {
                _hotelDealInput = GetHotelDataFromLuis(luisResponse);

                return await stepContext.PromptAsync($"{nameof(HotelDealsDialog)}.hotelDealWhatPrice",
                     new PromptOptions
                     {
                         Prompt = MessageFactory.Text(HotelDeal.AskPrice)
                     }, cancellationToken);
            }
            else
            {
                //bring back to main dialog here
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(HotelDeal.NoDataFound), cancellationToken);
                // cancel all the proceeding waterfall as we are going to repeat

                return await stepContext.BeginDialogAsync($"{nameof(HotelDealsDialog)}.mainFlow", null, cancellationToken);
            }

        }

        private async Task<DialogTurnResult> GetApiResultsThenAskApprovalAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            _hotelDealInput.PriceString = (string)stepContext.Result;
            //validate if there's a price somewhere 

            //call Deals API Here...
            await stepContext.Context.SendActivityAsync(MessageFactory.Text(string.Format(HotelDeal.BookingDetails, _hotelDealInput.Location, _hotelDealInput.PaxCount, _hotelDealInput.CheckInDate, _hotelDealInput.PriceString)), cancellationToken);
            var message = HotelDeal.SpecialDeal;

            return await PrompterYesNoDialog.PromptForYesOrNo(stepContext, cancellationToken, $"{nameof(HotelDealsDialog)}.approveDeal", $"({ message}. Approve ?", false);

        }

        private async Task<DialogTurnResult> AskApprovalOfCardAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var approval = ((FoundChoice)stepContext.Result).Value; //approval of deal here

            if (approval.Contains("👍"))
            {
                return await PrompterYesNoDialog.PromptForYesOrNo(stepContext, cancellationToken, $"{nameof(HotelDealsDialog)}.approveCard", SharedStrings.AskedStoreCard, false);
            }
            else
            {
                //go back to previous async step
                //tempo only replace this with go back to prev step
                //return await stepContext.NextAsync(null, cancellationToken);
                return await stepContext.ReplaceDialogAsync($"{nameof(HotelDealsDialog)}.mainFlow", null, cancellationToken);
            }

        }

        private async Task<DialogTurnResult> UseCardAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var approval = ((FoundChoice)stepContext.Result).Value;  //approval for usage of card here 

            //do api call to usage of card here

            await stepContext.Context.SendActivityAsync(MessageFactory.Text(HotelDeal.BookingConfirmation), cancellationToken);

            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> EndDialogAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
             await stepContext.EndDialogAsync(null, cancellationToken);
            return await stepContext.BeginDialogAsync($"{nameof(AnythingElseDialog)}.AnythingElse", null, cancellationToken);
        }

        //TO DO! transfer this to injected helper
        private HotelRequest GetHotelDataFromLuis(LuisModel luisResponse)
        {
            var result = new HotelRequest();
            // Locations/Cities: get 1 only
            var geographies = luisResponse.Entities.geographyV2;
            result.Location = geographies?.Length > 0 ? geographies[0].Location : string.Empty;
            // number of people
            var numbers = luisResponse.Entities.number;
            result.PaxCount = numbers?.Length > 0 ? numbers[0] : 0;
            //schedule
            var dates = luisResponse.Entities.datetime;
            result.CheckInDate = dates?.Length > 0 ? dates[0].Expressions[0].ToString() : string.Empty;

            return result;
        }

        #endregion

    }
}