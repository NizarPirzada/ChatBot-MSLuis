using AriBotV4.Common;
using AriBotV4.Dialogs.AskAri.Resources;
using AriBotV4.Models;
using AriBotV4.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Newtonsoft.Json;
using NodaTime.TimeZones;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TimeZoneConverter;

namespace AriBotV4.Dialogs.Common
{
    public class TimeDialog : ComponentDialog
    {
        #region Properties and Fields
        private readonly BotStateService _botStateService;

        private LuisModel luisResponse;
        #endregion


        #region WaterFallSteps and Dialogs

        public TimeDialog(string dialogId, BotStateService botStateService) : base(dialogId)
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
                CheckUserCity,
                DisplayTime,
                FinalAsync
            };

            // Add Named Dialogs
            AddDialog(new WaterfallDialog($"{nameof(TimeDialog)}.mainFlow", waterfallSteps));
            AddDialog(new TextPrompt($"{nameof(TimeDialog)}.name"));

            // Set the starting Dialog
            InitialDialogId = $"{nameof(TimeDialog)}.mainFlow";
        }

        private async Task<DialogTurnResult> CheckUserCity(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
                        
            RecognizerResult recognizerResult = (RecognizerResult)stepContext.ActiveDialog.State["options"];
            LuisModel luisResponse = JsonConvert.DeserializeObject<LuisModel>(JsonConvert.SerializeObject(recognizerResult));

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
                    return await stepContext.NextAsync(null, cancellationToken);
                    //return await stepContext.PromptAsync($"{nameof(TimeDialog)}.name",
                    //new PromptOptions
                    //{
                    //    Prompt = MessageFactory.Text(SearchAri.AskCountry)
                    //}, cancellationToken);
                }
                else
                {
                    stepContext.Values["TimeCity"] = luisResponse.Entities.geographyV2[0].Location.ToLower();
                    return await stepContext.NextAsync(null, cancellationToken);
                }
            }
            else
            {
                return await stepContext.NextAsync(null, cancellationToken);
            }


            


        }

        public async Task<DialogTurnResult> DisplayTime(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            
            if (!stepContext.Values.ContainsKey("TimeCity"))
                stepContext.Values["TimeCity"] = (string)stepContext.Result;
            

            try
            {

                // Check whether user is asking for specific country time
                if (stepContext.Values["TimeCity"] != null)
                {
                    var getZoneId = TzdbDateTimeZoneSource.Default.ZoneLocations.Where(x => x.CountryName.ToLower()
                         == (Convert.ToString(stepContext.Values["TimeCity"]).ToLower())).AsQueryable();

                    if (getZoneId.Count() != 1)
                        getZoneId = TzdbDateTimeZoneSource.Default.ZoneLocations.Where(x => x.ZoneId.ToLower()
                       .Contains(Convert.ToString(stepContext.Values["TimeCity"]).ToLower())).AsQueryable();

                    if (getZoneId.Count() != 1)
                        getZoneId = TzdbDateTimeZoneSource.Default.ZoneLocations.Where(x => x.ZoneId.ToLower()
                       .Contains(Convert.ToString(stepContext.Values["TimeCity"]).ToLower())).AsQueryable();

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
                
                return await stepContext.NextAsync(null, cancellationToken);
            }
            catch (Exception ex)
            {
                
                return await stepContext.NextAsync(null, cancellationToken);
            }

        }

        private async Task<DialogTurnResult> FinalAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            //weatherCity = string.Empty;
            //weatherDate = new DateTime();
            await stepContext.EndDialogAsync(null, cancellationToken);
            return await stepContext.BeginDialogAsync($"{nameof(AnythingElseDialog)}.AnythingElse", null, cancellationToken);

        }

        #endregion
    }
}
