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
using AriBotV4.Dialogs.Deals;
using AriBotV4.Interface.Travel;
using AriBotV4.Dialogs.Deals.Food;
using AriBotV4.Dialogs.Deals.Hotel;
using AriBotV4.Dialogs.Deals.Travel;
using AriBotV4.Dialogs.Deals.Resources;
using AriBotV4.Dialogs.Common.Resources;

namespace AriBotV4.Dialogs
{
    public class FindDealsDialog : ComponentDialog
    {
        #region properties and Fields
        private readonly BotStateService _botStateService;
        private readonly BotServices _botServices;
        private readonly ITravelService _travelService;
        #endregion

        #region Method
        public FindDealsDialog(string dialogId, BotStateService botStateService, BotServices botServices, ITravelService travelService) : base(dialogId)
        {
            _botStateService = botStateService ?? throw new System.ArgumentNullException(nameof(botStateService));
            _botServices = botServices ?? throw new System.ArgumentNullException(nameof(botServices));

            _travelService = travelService ?? throw new System.ArgumentNullException(nameof(travelService));

            InitializeWaterfallDialog();
        }

        private void InitializeWaterfallDialog()
        {
            // Create Waterfall Steps
            var waterfallSteps = new WaterfallStep[]
            {
               WhatDealCategoryAsync,
               GetDealThenTransferDialogAsync,
               EndDialogAsync
            };

            // Add Named Dialogs (these doesn't dictate the flow, waterfall does!)
            AddDialog(new WaterfallDialog($"{nameof(FindDealsDialog)}.mainFlow", waterfallSteps));
            AddDialog(new ChoicePrompt($"{nameof(FindDealsDialog)}.dealName"));
            AddDialog(new TravelDealsDialog($"{nameof(TravelDealsDialog)}.travelDeal", _botStateService, _botServices, _travelService));
            AddDialog(new HotelDealsDialog($"{nameof(HotelDealsDialog)}.hotelDeal", _botStateService, _botServices));
            AddDialog(new FoodDealsDialog($"{nameof(FoodDealsDialog)}.foodDeal", _botStateService, _botServices));

            // Set the starting Dialog
            InitialDialogId = $"{nameof(FindDealsDialog)}.mainFlow";
        }

        private async Task<DialogTurnResult> WhatDealCategoryAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var prompt = new PromptOptions
            {
                Prompt = MessageFactory.Text(FindDeals.AskDeals),
                Choices = ChoiceFactory.ToChoices(new List<string> {
                    EnumHelpers.GetEnumDescription(Deal.Travel),
                    EnumHelpers.GetEnumDescription(Deal.Food),
                    EnumHelpers.GetEnumDescription(Deal.Hotel),
                    EnumHelpers.GetEnumDescription(Deal.Others)
                    }),
                Style = ListStyle.SuggestedAction,

            };

            return await stepContext.PromptAsync($"{nameof(FindDealsDialog)}.dealName", prompt, cancellationToken);
        }

        private async Task<DialogTurnResult> GetDealThenTransferDialogAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var selectedChoice = ((FoundChoice)stepContext.Result).Value;
            //save the main choice to user state . TO DO!! inject a class that handles User State saving
            var userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
            userProfile.DealChosen = selectedChoice;
            await _botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);

            //var prompterForSpecifics = "";
            switch (selectedChoice)
            {
                case "Travel":
                    return await stepContext.BeginDialogAsync($"{nameof(TravelDealsDialog)}.travelDeal", null, cancellationToken);
                case "Food":
                    return await stepContext.BeginDialogAsync($"{nameof(FoodDealsDialog)}.foodDeal", null, cancellationToken);
                case "Hotel":
                    return await stepContext.BeginDialogAsync($"{nameof(HotelDealsDialog)}.hotelDeal", null, cancellationToken);
                case "Others":
                    await stepContext.Context.SendActivityAsync(SharedStrings.StillInProgress);
                    return await stepContext.BeginDialogAsync($"{nameof(AnythingElseDialog)}.AnythingElse", null, cancellationToken);


                    //default:
                    //    prompterForSpecifics = "Tell me what deal you generally want.";
                    //    break;
            }

            //return await stepContext.PromptAsync($"{nameof(FindDealsDialog)}.whatSpecificDeal",
            //     new PromptOptions
            //     {
            //         Prompt = MessageFactory.Text(prompterForSpecifics)
            //     }, cancellationToken);

            return await stepContext.NextAsync(null, cancellationToken);
        }
               

        private async Task<DialogTurnResult> EndDialogAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.EndDialogAsync(null, cancellationToken);
        }

        #endregion
        
        #region Non Waterfall Dialog

        //private async Task<DialogTurnResult> GetAirlineAnswerAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        //{

        //    return await stepContext.NextAsync(null, cancellationToken);
        //}

        #endregion

        #region Non Dialog
        //private TravelDealInput GetLocationFromLuis(AriBotLuisClass luisResponse)
        //{
        //    var result = new TravelDealInput();
        //    // Locations/Cities: we just assume 1st one is from, then 2nd is to
        //    var geographies = luisResponse.Entities.geographyV2;            
        //    result.FromLocation = geographies.Length > 0 ? geographies[0].Location : string.Empty;
        //    result.ToLocation = geographies.Length > 1 ? geographies[1].Location : string.Empty;
        //    // Departure Date and Round Trip Date.. if null, default to Tomorrow
        //    var dates = luisResponse.Entities.datetime;
        //    result.DepartureDate = dates.Length > 0 ? dates[0].Expressions.FirstOrDefault() : DateTime.Now.AddDays(1).ToShortDateString();
        //    result.ReturnDate = dates.Length > 1 ? dates[1].Expressions.FirstOrDefault() : DateTime.Now.AddDays(2).ToShortDateString();

        //    return result;
        //}

        #endregion


    }
}