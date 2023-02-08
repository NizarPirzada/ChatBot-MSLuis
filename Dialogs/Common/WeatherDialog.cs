using AriBotV4.Common;
using AriBotV4.Dialogs.Common.Resources;
using AriBotV4.Enums;
using AriBotV4.Models;
using AriBotV4.Models.Common.Weather;
using AriBotV4.Services;
using LuisEntityHelpers;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenWeatherMap;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AriBotV4.Dialogs.Common
{
    public class WeatherDialog : ComponentDialog
    {
        #region Properties and Fields
        private readonly BotStateService _botStateService;
        private LuisModel luisResponse;
        #endregion


        #region WaterFallSteps and Dialogs

        public WeatherDialog(string dialogId, BotStateService botStateService) : base(dialogId)
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
                AskUserCity,
                DisplayWeather,
                FinalAsync
            };

            // Add Named Dialogs
            AddDialog(new WaterfallDialog($"{nameof(WeatherDialog)}.mainFlow", waterfallSteps));
            AddDialog(new TextPrompt($"{nameof(WeatherDialog)}.name"));

            // Set the starting Dialog
            InitialDialogId = $"{nameof(WeatherDialog)}.mainFlow";
        }



        private async Task<DialogTurnResult> CheckUserCity(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            DateTime weatherDate = new DateTime();
            // Get LUIS response from last dialog
            RecognizerResult recognizerResult = (RecognizerResult)stepContext.ActiveDialog.State["options"];
            LuisModel luisResponse = JsonConvert.DeserializeObject<LuisModel>(JsonConvert.SerializeObject(recognizerResult));


            if (luisResponse.Entities.datetime != null)
            {

                // Convert LUIS date expression to DateTime
                if (luisResponse.Entities.datetime[0].Expressions[0] == AriBotV4.Common.Constants.Now)
                    stepContext.Values["weatherDate"] = DateTime.UtcNow;
                else
                {
                    var datetimes = Microsoft.Recognizers.Text.DataTypes.TimexExpression.TimexResolver.Resolve
                    (new[] { luisResponse.Entities.datetime[0].Expressions[0] },
                        System.DateTime.Today);

                    if (datetimes.Values != null && datetimes.Values.Count > 0)
                    {
                        int count = datetimes.Values.Count;

                        if (datetimes.Values[0].Start != null)
                        {
                            stepContext.Values["weatherDate"] = Convert.ToDateTime(datetimes.Values[count - 1].Start);
                        }
                        else if (DateTime.TryParse(datetimes.Values[count - 1].Value, out weatherDate))
                        {
                            stepContext.Values["weatherDate"] = Convert.ToDateTime(datetimes.Values[count - 1].Value);
                        }
                        else
                            stepContext.Values["weatherDate"] = DateTime.Now.AddSeconds(Convert.ToDouble(datetimes.Values[0].Value));
                    }

                }

            }
            else
            {
                stepContext.Values["weatherDate"] = DateTime.UtcNow;
                //weatherDate = DateTime.UtcNow;
            }

            
            // Check with LUIS whether user asked for any city
           if (luisResponse.Entities.geographyV2 != null)
            {
                stepContext.Values["weatherCity"] = luisResponse.Entities.geographyV2[0].Location.ToLower();
            }
           else if(luisResponse.Entities._instance.Places_AbsoluteLocation != null)
            {
                stepContext.Values["weatherCity"] = luisResponse.Entities._instance.Places_AbsoluteLocation[0].Text.ToLower();
                //weatherCity = luisResponse.Entities.geographyV2[0].Location.ToLower();
            }
           else if(!string.IsNullOrEmpty(Convert.ToString(stepContext.Context.Activity.From.Properties[AriBotV4.Common.Constants.TaskSpurTimeZone])) && Convert.ToString(stepContext.Context.Activity.From.Properties[AriBotV4.Common.Constants.TaskSpurTimeZone]).Contains("/"))
           {
                
                stepContext.Values["weatherCity"] = Convert.ToString(stepContext.Context.Activity.From.Properties[AriBotV4.Common.Constants.TaskSpurTimeZone]).Split("/")[1].ToLower();
            }
           

            return await stepContext.NextAsync(null, cancellationToken);


        }

        // If user does not specify, ask user for city name      
        public async Task<DialogTurnResult> AskUserCity(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if(!stepContext.Values.ContainsKey("weatherCity"))
            {
                return await stepContext.PromptAsync($"{nameof(WeatherDialog)}.name",
                     new PromptOptions
                     {
                         Prompt = MessageFactory.Text(SharedStrings.AskCity)
                     }, cancellationToken);
            }
            return await stepContext.NextAsync(null, cancellationToken);


        }

        public async Task<DialogTurnResult> DisplayWeather(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (!stepContext.Values.ContainsKey("weatherCity"))
                stepContext.Values["weatherCity"] = (string)stepContext.Result;

            var client = new OpenWeatherMapClient(_botStateService._weatherSettings.WeatherSubscriptionKey);

            // Get current weather
            try
            {

                WeatherResponse weatherReport =  await _botStateService._taskSpurApiClient.GetWeatherReport(Convert.ToString(stepContext.Values["weatherCity"]),
Convert.ToString(stepContext.Context.Activity.From.Properties[AriBotV4.Common.Constants.TaskSpurTimeZone]), Convert.ToDateTime(stepContext.Values["weatherDate"]));
                var updateWeatherTem = UpdateAdaptivecardAttachment(JsonConvert.SerializeObject(weatherReport));
                              

                await stepContext.Context.SendActivityAsync(MessageFactory.Attachment(updateWeatherTem), cancellationToken);
            }
            catch (Exception ex)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(SharedStrings.Sorry), cancellationToken);
                stepContext.Values.Remove("weatherCity");
                stepContext.ActiveDialog.State["stepIndex"] = (int)stepContext.ActiveDialog.State["stepIndex"] - 1;
                return await AskUserCity(stepContext, cancellationToken);
            }
            return await stepContext.NextAsync(null, cancellationToken);


        }
        private async Task<DialogTurnResult> FinalAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            //weatherCity = string.Empty;
            //weatherDate = new DateTime();
            await stepContext.EndDialogAsync(null, cancellationToken);
            return await stepContext.BeginDialogAsync($"{nameof(AnythingElseDialog)}.AnythingElse", null, cancellationToken);

        }


        #region Methods

        // Path for weather json format
        private readonly string[] _cards =
        {
                    Path.Combine(AriBotV4.Common.Constants.WeatherJsonPath),

        };

        // Weather adaptive card
        private static Attachment CreateAdaptiveCardAttachment(string filePath)
        {
            var adaptiveCardJson = File.ReadAllText(filePath);
            var adaptiveCardAttachment = new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(adaptiveCardJson),
            };
            return adaptiveCardAttachment;
        }

        private static JObject readFileforUpdate_jobj(string filepath)
        {
            var json = File.ReadAllText(filepath);
            var jobj = JsonConvert.DeserializeObject(json);
            JObject Jobj_card = JObject.FromObject(jobj) as JObject;
            return Jobj_card;
        }
        private static Attachment UpdateAdaptivecardAttachment(string updateAttch)
        {

            var adaptiveCardAttch = new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(updateAttch),
            };
            return adaptiveCardAttch;
        }
        public static string ImageToBase64(string filePath)
        {
            Byte[] bytes = File.ReadAllBytes(filePath);
            string base64String = Convert.ToBase64String(bytes);
            return "data:image/jpg;base64," + base64String;
        }
        #endregion

    }



}
#endregion
