using AriBotV4.Enums;
using AriBotV4.Helpers;
using AriBotV4.Models;
using AriBotV4.Services;
using AriBotV4.Common;
using AriBotV4.Dialogs.Common;
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
using AriBotV4.Interface.Travel;
using AriBotV4.Models.Travel;
using AriBotV4.Dialogs.Deals.Travel.Resources;

namespace AriBotV4.Dialogs.Deals.Travel
{
    public class TravelDealsDialog : ComponentDialog
    {
        #region Properties and Fields
        private readonly BotStateService _botStateService;
        private readonly BotServices _botServices;
        private readonly ITravelService _mirabileDealService;
        private TravelRequest _travelDealInput;
        private bool _isRepeat;
        private int _debugCount = 1;
        #endregion

        #region Method
        public TravelDealsDialog(string dialogId, BotStateService botStateService, BotServices botServices, ITravelService mirabileDealService) : base(dialogId)
        {
            _botStateService = botStateService ?? throw new System.ArgumentNullException(nameof(botStateService));
            _botServices = botServices ?? throw new System.ArgumentNullException(nameof(botServices));

            _mirabileDealService = mirabileDealService ?? throw new System.ArgumentNullException(nameof(mirabileDealService));
            InitializeWaterfallDialog();

            _isRepeat = false;
        }

        private void InitializeWaterfallDialog()
        {
            // Create Waterfall Steps
            var waterfallSteps = new WaterfallStep[]
            {
               WhatSpecificAsync,
               GetLuisIntentsAsync,
               GetPeopleCount,
               FinalizeDataOrRepeat,
               GetApiResultsThenAskApprovalAsync,
               //we can transfer these 3 card related to a new dialog , reusable for other deals
               AskApprovalOfCardAsync, 
               UseCardAsync,
               EndDialogAsync
            };

            // Add Named Dialogs (these doesn't dictate the flow, waterfall does!)
            AddDialog(new WaterfallDialog($"{nameof(TravelDealsDialog)}.mainFlow", waterfallSteps));
            AddDialog(new TextPrompt($"{nameof(TravelDealsDialog)}.whatSpecificDeal"));
            AddDialog(new TextPrompt($"{nameof(TravelDealsDialog)}.travelDealWhatAirline"));
            AddDialog(new ChoicePrompt($"{nameof(TravelDealsDialog)}.howManyPeople"));
            AddDialog(new ChoicePrompt($"{nameof(TravelDealsDialog)}.dataCorrect"));
            AddDialog(new ChoicePrompt($"{nameof(TravelDealsDialog)}.approveDeal"));
            AddDialog(new ChoicePrompt($"{nameof(TravelDealsDialog)}.approveCard"));

            // Set the starting Dialog
            InitialDialogId = $"{nameof(TravelDealsDialog)}.mainFlow";
        }
              

        private async Task<DialogTurnResult> WhatSpecificAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            //we don't ask again if user has given it before, unless he opted for repeat
            if (_travelDealInput?.Specifics != null && !_isRepeat)
            {                
                return await stepContext.NextAsync(null, cancellationToken);
            }
            else
            {
                return await stepContext.PromptAsync($"{nameof(TravelDealsDialog)}.whatSpecificDeal",
                     new PromptOptions
                     {
                         Prompt = MessageFactory.Text(TravelDeals.AskTravelDetails)
                     }, cancellationToken);
            }            
        }

        private async Task<DialogTurnResult> GetLuisIntentsAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (_travelDealInput?.Specifics != null && !_isRepeat)
            {
                //specifics have been given before
                return await stepContext.NextAsync(null, cancellationToken);
            }
            else
            {
                _travelDealInput = new TravelRequest { Specifics = (string)stepContext.Result };

                //to deserialize LUIS result
                //https://stackoverflow.com/questions/56372143/how-can-i-access-an-entities-score-information-and-or-existence-from-the-luis-ai

                // First, we use the dispatch model to determine which cognitive service (LUIS or Qna) to use
                var recognizerResult = await _botServices.Dispatch.RecognizeAsync(stepContext.Context, cancellationToken);
                var luisResponse = JsonConvert.DeserializeObject<LuisModel>(JsonConvert.SerializeObject(recognizerResult));

                // Top intent tell us which cognitive service to use
                var topIntent = recognizerResult.GetTopScoringIntent();

                //to continue, must be Travel INtent and at least 1 location
                if (topIntent.intent != Constants.TravelIntent)
                {
                    _travelDealInput.Specifics = null;

                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(TravelDeals.NoDeal), cancellationToken);

                    //return to main deal choices
                    return await stepContext.ReplaceDialogAsync($"{nameof(TravelDealsDialog)}.mainFlow", null, cancellationToken);                    
                }
                else
                {
                    _travelDealInput = _mirabileDealService.GetTravelDataFromLuis(luisResponse, _travelDealInput);
                    if (string.IsNullOrEmpty(_travelDealInput.ToLocation))
                    {
                        _travelDealInput.Specifics = null;

                        await stepContext.Context.SendActivityAsync(MessageFactory.Text(TravelDeals.DestinationMissing), cancellationToken);

                        //return to main deal choices
                        return await stepContext.ReplaceDialogAsync($"{nameof(TravelDealsDialog)}.mainFlow", null, cancellationToken);
                    }
                    else
                    {
                        return await stepContext.PromptAsync($"{nameof(TravelDealsDialog)}.travelDealWhatAirline",
                        new PromptOptions
                        {
                            Prompt = MessageFactory.Text(TravelDeals.AskAirline)
                        }, cancellationToken);
                    }

                }
                
            }

        }

        private async Task<DialogTurnResult> GetPeopleCount(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            
            if ( _travelDealInput?.PeopleCount != null && !_isRepeat  )
            {
                //specifics and peoplecount have been given have been given before
                return await stepContext.NextAsync(null, cancellationToken);
            }
            else
            {
                _travelDealInput.AirlineChoice = _travelDealInput.AirlineChoice == null ? (string)stepContext.Result : _travelDealInput.AirlineChoice;
                return await PrompterCountDialog.PromptForHowMany(stepContext, cancellationToken, $"{nameof(TravelDealsDialog)}.howManyPeople", $"How many people are travelling?");
            }

        }

        private async Task<DialogTurnResult> FinalizeDataOrRepeat(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (_travelDealInput?.PeopleCount == null)
            {
                _travelDealInput.PeopleCount = Convert.ToInt32(((FoundChoice)stepContext.Result).Value);
            }

            //This is now your data           
            var message = $"Details: Flying:{_travelDealInput.AirlineChoice}  To:{_travelDealInput.ToLocation} On:{_travelDealInput.DepartureDate} How Many: {_travelDealInput.PeopleCount} .{Environment.NewLine}Is this correct?";
            
            //ask if final or repeat
            return await PrompterYesNoDialog.PromptForYesOrNo(stepContext, cancellationToken, $"{nameof(TravelDealsDialog)}.dataCorrect", $"{message}", false);
        }

        private async Task<DialogTurnResult> GetApiResultsThenAskApprovalAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var isDataCorrect = ((FoundChoice)stepContext.Result).Value;

            if (isDataCorrect.Contains("👍"))
            {
                //call Mirabile
                var dealOptions = _mirabileDealService.GetTravelOptions(_travelDealInput);

                //TO DO! call API again for another deal? or call once, then get all deals possible? iterate on it...
                var message = _debugCount == 1? dealOptions?.FirstOrDefault().Description : dealOptions[1].Description;
                _debugCount = _debugCount == 1 ? 2 : 1;

                return await PrompterYesNoDialog.PromptForYesOrNo(stepContext, cancellationToken, $"{nameof(TravelDealsDialog)}.approveDeal", $"{message}. Approve?", true);
            }
            else
            {
                _isRepeat = true;
                return await stepContext.ReplaceDialogAsync($"{nameof(TravelDealsDialog)}.mainFlow", null, cancellationToken);
            }

           

        }

        private async Task<DialogTurnResult> AskApprovalOfCardAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var approval = ((FoundChoice)stepContext.Result).Value; //approval of deal here

            if (approval.Contains("👍"))
            {
                //TO do booking here!
                return await PrompterYesNoDialog.PromptForYesOrNo(stepContext, cancellationToken, $"{nameof(TravelDealsDialog)}.approveCard", "Certainly, can I use your stored Card?", false);
            }
            else if (approval.Contains("Repeat"))
            {
                _isRepeat = true;
                return await stepContext.ReplaceDialogAsync($"{nameof(TravelDealsDialog)}.mainFlow", null, cancellationToken);
            }
            else
            {                
                _isRepeat = false;
                return await stepContext.ReplaceDialogAsync($"{nameof(TravelDealsDialog)}.mainFlow", null, cancellationToken);
            }

        }

        private async Task<DialogTurnResult> UseCardAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var approval = ((FoundChoice)stepContext.Result).Value;

            //do api call to usage of card here
            if (approval.Contains("👍"))
            {
                

                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"The booking is made. Have a nice trip!"), cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"The booking is pending. Please pay this booking reference number."), cancellationToken);
            }

            

            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> EndDialogAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
             await stepContext.EndDialogAsync(null, cancellationToken);
            return await stepContext.BeginDialogAsync($"{nameof(AnythingElseDialog)}.AnythingElse", null, cancellationToken);

        }

        #endregion
    }
}