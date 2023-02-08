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
using AriBotV4.Models.Food;
using AriBotV4.Dialogs.Deals.Food.Resources;
using AriBotV4.Dialogs.Common.Resources;

namespace AriBotV4.Dialogs.Deals.Food
{
    public class FoodDealsDialog : ComponentDialog
    {
        #region properties and Fields
        private readonly BotStateService _botStateService;
        private readonly BotServices _botServices;
        private FoodRequest _foodDealInput;
        #endregion


        #region Methods
        public FoodDealsDialog(string dialogId, BotStateService botStateService, BotServices botServices) : base(dialogId)
        {
            _botStateService = botStateService ?? throw new System.ArgumentNullException(nameof(botStateService));
            _botServices = botServices ?? throw new System.ArgumentNullException(nameof(botServices));

            InitializeWaterfallDialog();

            _foodDealInput = new FoodRequest();
        }

        private void InitializeWaterfallDialog()
        {
            // Create Waterfall Steps
            var waterfallSteps = new WaterfallStep[]
            {
               WhatSpecificFoodAsync,
               AskFoodDetailsAsync,
               GetLuisIntentsAsync,
               AskApprovalOfDealAsync,
               AskApprovalOfCardAsync,
               UseCardAsync,
               EndDialogAsync
            };

            // Add Named Dialogs (these doesn't dictate the flow, waterfall does!)
            AddDialog(new WaterfallDialog($"{nameof(FoodDealsDialog)}.mainFlow", waterfallSteps));
            AddDialog(new ChoicePrompt($"{nameof(FoodDealsDialog)}.whatCuisine"));
            AddDialog(new TextPrompt($"{nameof(FoodDealsDialog)}.foodDetails"));
            AddDialog(new TextPrompt($"{nameof(FoodDealsDialog)}.diningTime"));
            AddDialog(new ChoicePrompt($"{nameof(FoodDealsDialog)}.approveDeal"));
            AddDialog(new ChoicePrompt($"{nameof(FoodDealsDialog)}.approveCard"));

            // Set the starting Dialog
            InitialDialogId = $"{nameof(FoodDealsDialog)}.mainFlow";
        }

        private async Task<DialogTurnResult> WhatSpecificFoodAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userStateLastFood = "";

            //TO DO! we can rotate/randomize the choice here so that options are fresh every time
            var prompt = new PromptOptions
            {
                Prompt = MessageFactory.Text(string.Format(FoodDeals.AskFoodPrefrence, userStateLastFood)),
                Choices = ChoiceFactory.ToChoices(new List<string> {
                    EnumHelpers.GetEnumDescription(Cuisine.Chinese),
                    EnumHelpers.GetEnumDescription(Cuisine.European),
                    EnumHelpers.GetEnumDescription(Cuisine.Indian),
                    EnumHelpers.GetEnumDescription(Cuisine.Japanese),
                    EnumHelpers.GetEnumDescription(Cuisine.Korean),
                    EnumHelpers.GetEnumDescription(Cuisine.Thai)
                    }),
                Style = ListStyle.SuggestedAction,

            };

            return await stepContext.PromptAsync($"{nameof(FoodDealsDialog)}.whatCuisine", prompt, cancellationToken);

        }

        private async Task<DialogTurnResult> AskFoodDetailsAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            _foodDealInput.Cuisine = ((FoundChoice)stepContext.Result).Value;

            return await stepContext.PromptAsync($"{nameof(FoodDealsDialog)}.foodDetails",
                 new PromptOptions
                 {
                     Prompt = MessageFactory.Text(FoodDeals.AskPeople)
                 }, cancellationToken);

        }

        private async Task<DialogTurnResult> GetLuisIntentsAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var recognizerResult = await _botServices.Dispatch.RecognizeAsync(stepContext.Context, cancellationToken);
            var luisResponse = JsonConvert.DeserializeObject<LuisModel>(JsonConvert.SerializeObject(recognizerResult));

            //Dont check for intent anymore? just get Entity
            //var topIntent = recognizerResult.GetTopScoringIntent();

            var data = GetFoodDataFromLuis(luisResponse);
            _foodDealInput.Location = data.Location;
            _foodDealInput.PaxCount = data.PaxCount;

            return await stepContext.PromptAsync($"{nameof(FoodDealsDialog)}.diningTime",
                 new PromptOptions
                 {
                     Prompt = MessageFactory.Text(FoodDeals.AskTime)
                 }, cancellationToken);

        }

        private async Task<DialogTurnResult> AskApprovalOfDealAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            _foodDealInput.PriceString = (string)stepContext.Result;
            //validate if there's a price somewhere 

            //call Deals API Here...

            await stepContext.Context.SendActivityAsync(MessageFactory.Text(string.Format(FoodDeals.ConfirmBooking, _foodDealInput.Cuisine)), cancellationToken);

            return await PromptForYesOrNo(stepContext, cancellationToken, $"{nameof(FoodDealsDialog)}.approveDeal", FoodDeals.Approve);


        }

        private async Task<DialogTurnResult> AskApprovalOfCardAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var approval = ((FoundChoice)stepContext.Result).Value;

            if (approval.Contains("👍"))
            {
                return await PromptForYesOrNo(stepContext, cancellationToken, $"{nameof(FoodDealsDialog)}.approveCard", SharedStrings.AskedStoreCard);
            }
            else
            {
                //go back to previous async step
                //tempo only replace this with go back to prev step
                //return await stepContext.NextAsync(null, cancellationToken);
                return await stepContext.ReplaceDialogAsync($"{nameof(FoodDealsDialog)}.mainFlow", null, cancellationToken);
            }

        }

        private async Task<DialogTurnResult> UseCardAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var approval = ((FoundChoice)stepContext.Result).Value;

            //do api call to usage of card here
            if (approval.Contains("👍"))
            {
            }
            else
            {
            }
            await stepContext.Context.SendActivityAsync(MessageFactory.Text(FoodDeals.BookingConfirmation), cancellationToken);

            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> EndDialogAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await stepContext.EndDialogAsync(null, cancellationToken);
            return await stepContext.BeginDialogAsync($"{nameof(AnythingElseDialog)}.AnythingElse", null, cancellationToken);
        }

        //TO DO! transfer this to injected helper
        private FoodRequest GetFoodDataFromLuis(LuisModel luisResponse)
        {
            var result = new FoodRequest();
            // Locations/Cities: get 1 only
            var geographies = luisResponse.Entities.geographyV2;
            result.Location = geographies?.Length > 0 ? geographies[0].Location : string.Empty;
            // number of people
            var numbers = luisResponse.Entities.number;
            result.PaxCount = numbers?.Length > 0 ? numbers[0] : 0;

            return result;
        }

        //TO DO! transfer this to a static class        
        private async Task<DialogTurnResult> PromptForYesOrNo(WaterfallStepContext stepContext, CancellationToken cancellationToken, string dialogId, string promptText)
        {
            return await stepContext.PromptAsync(dialogId,
               new PromptOptions
               {
                   Style = ListStyle.SuggestedAction,
                   Prompt = MessageFactory.Text(promptText),
                   Choices = ChoiceFactory.ToChoices(new List<string>
                   {
                        "👍",
                        "👎"
                   }),
               }, cancellationToken);
        }

        #endregion
    }

}