using AdaptiveCards;
using AriBotV4.AppSettings;
using AriBotV4.Enums;
using AriBotV4.Models;
using AriBotV4.Models.MyCarte;
using AriBotV4.Services;
using Azure.AI.TextAnalytics;
using LuisEntityHelpers;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AriBotV4.Common
{
    public class Utility
    {

            // Generate random messages
            public static string GenerateRandomMessages(string[] messages)
        {
            try
            {
                Random rnd = new Random();
                int r = rnd.Next(messages.Count());
                return ((string)messages[r]);
            }
            catch (Exception ex) { return string.Empty; }
        }

        // Get https,http,www url
        public static string GetUrlFromString(string rawString)
        {
            try
            {
                var linkParser = new Regex(@"\b(?:https?://|www\.)\S+\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                foreach (Match match in linkParser.Matches(rawString))
                {
                    return match.Value;
                    
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                return string.Empty;
            }

        }

        // valid user inout
        public static async Task<bool> ValidateUserInput(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {

            if (string.IsNullOrWhiteSpace(promptContext.Recognized.Value))
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        // Get keyword from sentence
       public static async Task<List<string>> KeyPhraseExtractionExample(TextAnalyticsClient client, string query)
        {
            var response = await client.ExtractKeyPhrasesAsync(query);
            List<string> keywords = new List<string>();
            // Printing key phrases
             foreach (string keyphrase in response.Value)
            {
                keywords.Add(keyphrase);
                
            }
            if(keywords.Count == 0)
            {
                keywords.Add(query);
            }

            return await Task.Run(() => keywords);
        }

        // Get project settings 
        public static string GetProjectSettings(int projectCode)
        {
            string projectSettings = string.Empty;
            if (projectCode == 0)
                projectSettings = "TaskSpurToggleSettings";
            else if (projectCode == 1)
                projectSettings = "MyCarteToggleSettings";
            else if (projectCode == 2)
                projectSettings = "IntellegoToggleSettings";
            return projectSettings;
        }


        // Get QnA results
        public static QnAMaker GetQnaSearchResult(string query, BotStateService _botStateService)
        {
            try
            {
                var qnaMakerHost = _botStateService._qnaSettings.QnAMakerHost;
                var qnaMakerKBId = _botStateService._qnaSettings.QnAMakerId;
                var qnaMakerEndPointKey = _botStateService._qnaSettings.QnAMakerEndPointKey;
                var qnaMakerFormatJson = _botStateService._qnaSettings.QnAMakerFormatJson;
               
                var client = new RestClient(qnaMakerHost + "/knowledgebases/" + qnaMakerKBId + "/generateAnswer");
                var qnaRequest = new RestRequest(Method.POST);
                qnaRequest.AddHeader("authorization", "EndpointKey " + qnaMakerEndPointKey);
                qnaRequest.AddParameter(qnaMakerFormatJson, "{\"question\": \"" + query + "\"}", ParameterType.RequestBody);
                var qnaResponse = client.Execute(qnaRequest);

                var qnaSearchList = JsonConvert.DeserializeObject<QnAMaker>(qnaResponse.Content);


                if (qnaSearchList.Answers.Count > 0)
                {
                    return qnaSearchList;
                    // var qnaFirstAnswer = qnaSearchList.Answers[0].Answer;
                    //// var qnaScore = qnaSearchList.Answers[0].Score;
                    // //if (!qnaFirstAnswer.ToLower().Equals(qnaMakerAnswerNotFound) && qnaScore >= _botStateService._qnaSettings.ScorePercentage)
                    //     return qnaSearchList;
                    // else
                    // {
                    //     qnaSearchList.Answers[0].Answer = qnaMakerAnswerNotFound;
                    //     qnaSearchList.Answers[0].Source = "Editorial";
                    //     return qnaSearchList;
                    // }
                }
                return null;


            }
            catch
            {
                return null;
            }
        }

        // Get Common QnA results
        public static QnAMaker GetCommonQnaSearchResult(string query, QnASettings _qnaSettings)
        {
            try
            {
                var qnaMakerHost = _qnaSettings.QnAMakerHost;
                var qnaMakerKBId = _qnaSettings.QnAMakerId;
                var qnaMakerEndPointKey = _qnaSettings.QnAMakerEndPointKey;
                var qnaMakerFormatJson = _qnaSettings.QnAMakerFormatJson;
               
                var client = new RestClient(qnaMakerHost + "/knowledgebases/" + qnaMakerKBId + "/generateAnswer");
                var qnaRequest = new RestRequest(Method.POST);
                qnaRequest.AddHeader("authorization", "EndpointKey " + qnaMakerEndPointKey);
                qnaRequest.AddParameter(qnaMakerFormatJson, "{\"question\": \"" + query + "\"}", ParameterType.RequestBody);
                var qnaResponse = client.Execute(qnaRequest);

                var qnaSearchList = JsonConvert.DeserializeObject<QnAMaker>(qnaResponse.Content);


                if (qnaSearchList.Answers.Count > 0)
                {
                    return qnaSearchList;
                    // var qnaFirstAnswer = qnaSearchList.Answers[0].Answer;
                    //// var qnaScore = qnaSearchList.Answers[0].Score;
                    // //if (!qnaFirstAnswer.ToLower().Equals(qnaMakerAnswerNotFound) && qnaScore >= _botStateService._qnaSettings.ScorePercentage)
                    //     return qnaSearchList;
                    // else
                    // {
                    //     qnaSearchList.Answers[0].Answer = qnaMakerAnswerNotFound;
                    //     qnaSearchList.Answers[0].Source = "Editorial";
                    //     return qnaSearchList;
                    // }
                }
                return null;


            }
            catch
            {
                return null;
            }
        }


        // Get Common Luis results
        public static async Task<LuisHelper> GetCommonLuisSearchResult(string query, LuisSettings _luisSettings, WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            try
            {
                var luisAppId = _luisSettings.LuisAppId;
                var luisAPIKey = _luisSettings.LuisAPIKey;
                var luisAPIHostName = _luisSettings.LuisAPIHostName;

                var luisRecognizer = new LuisRecognizer(new LuisApplication(
                    luisAppId,
                    luisAPIKey,
                    $"https://{luisAPIHostName}.api.cognitive.microsoft.com"),
                    new LuisPredictionOptions { IncludeAllIntents = true, IncludeInstanceData = true },
                true);


                RecognizerResult recognizerResult = new RecognizerResult();
                LuisHelper luisResponse = new LuisHelper();
                // First, we use the dispatch model to determine which cognitive service (LUIS or Qna) to use
                try
                {
                    recognizerResult = await luisRecognizer.RecognizeAsync(stepContext.Context, cancellationToken);
                    var jsonObj = JsonConvert.SerializeObject(recognizerResult);
                    luisResponse = JsonConvert.DeserializeObject<LuisHelper>(jsonObj);
                    //Convert
                }
                catch (Exception ex)
                {

                }

                //stepContext.EndDialogAsync(null, cancellationToken);

                //// Top intent tell us which cognitive service to use
                //LuisModel.Intent topIntent = new LuisModel.Intent();
                //if (luisResponse != null)
                //    topIntent = luisResponse.TopIntent().intent;


                return luisResponse;


            }
            catch
            {
                return null;
            }
        }

        public static SearchType GetSearchType(LuisModel luisResponse, QnAMaker qnAMaker)
        {
            if ((qnAMaker != null  && qnAMaker.Answers[0].Score == 100) || (qnAMaker != null  && qnAMaker.Answers[0].Score >= 95 && qnAMaker.Answers[0].Score > luisResponse.TopIntent().score))
                return SearchType.QnA;
            else if ((luisResponse != null && luisResponse.TopIntent().intent != LuisModel.Intent.None && luisResponse.TopIntent().score == 100) || (luisResponse != null && luisResponse.TopIntent().intent != LuisModel.Intent.None &&  luisResponse.TopIntent().score >= 95 && qnAMaker.Answers[0].Score < luisResponse.TopIntent().score))
                return SearchType.LUIS;
            else
                return SearchType.General;

        }

        // Validate task name
        public static async Task<bool> ValidateTaskName(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {

            if (string.IsNullOrWhiteSpace(promptContext.Recognized.Value))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        // Validate start time and end time



        public static Attachment GetSelectedCard(string answer)
        {
            int length = answer.Split(';').Length;

            switch (length)
            {
                case 4: return GetHeroCard(answer);
                case 6: return GetVideoCard(answer);
                default: return GetHeroCard(answer);
            }
        }

        public static Attachment GetHeroCard(string answer)
        {
            string[] qnaAnswerData = answer.Split(';');
            string title = qnaAnswerData[0].Trim();
            string description = qnaAnswerData[1].Trim();
            string url = qnaAnswerData[2].Trim();
            string imageUrls = qnaAnswerData[3].Trim();

            string[] imageUrlList;
            var cardImages = new List<CardImage>();

            if (imageUrls.Contains(','))
            {
                imageUrlList = imageUrls.Split(',');

                foreach (var imageUrl in imageUrlList)
                {
                    if (!String.IsNullOrEmpty(imageUrl))
                    {
                        cardImages.Add(new CardImage(url = imageUrl));
                    }
                }
            }
            else
            {
                cardImages.Add(new CardImage(url = imageUrls));
            }

            HeroCard hCard = new HeroCard
            {
                Title = title,
                Subtitle = description,
            };

            hCard.Buttons = new List<CardAction>
            {
                new CardAction(ActionTypes.OpenUrl, "Learn More", value:url)
            };

            hCard.Images = cardImages;

            return hCard.ToAttachment();
        }

        public static Attachment GetVideoCard(string answer)
        {
            string[] qnaAnswerData = answer.Split(';');
            string title = qnaAnswerData[0].Trim();
            string subTitle = qnaAnswerData[1].Trim();
            string description = qnaAnswerData[2].Trim();
            string thumbImageUrl = qnaAnswerData[3].Trim();
            string mediaUrl = qnaAnswerData[4].Trim();
            string url = qnaAnswerData[5].Trim();

            VideoCard vCard = new VideoCard
            {
                Title = title,
                Subtitle = subTitle,
                Text = description,
            };

            vCard.Image = new ThumbnailUrl
            {
                Url = thumbImageUrl
            };

            vCard.Media = new List<MediaUrl>
            {
                new MediaUrl()
                {
                    Url = mediaUrl
                }
            };

            vCard.Buttons = new List<CardAction>
            {
                new CardAction()
                {
                    Title = "Learn More",
                    Type = ActionTypes.OpenUrl,
                    Value = url
                }
            };

            return vCard.ToAttachment();
        }


        public static Attachment CreateAdapativecard()
        {

            AdaptiveCard card = new AdaptiveCard();

            // Specify speech for the card.  
            card.Speak = "I'm AVA bot";
            // Body content  
            card.Body.Add(new AdaptiveImage()
            {
                Url = new Uri("https://media.istockphoto.com/vectors/chat-bot-using-laptop-computer-robot-virtual-assistance-of-website-or-vector-id1177016383?k=20&m=1177016383&s=612x612&w=0&h=BM-W0s-Snd16CrVOkY9V4eccAfZilpx4NZx-e19Wsvg="),
                Size = AdaptiveImageSize.Small,
                Style = AdaptiveImageStyle.Person,
                AltText = "I'm at LIG"

            });

            // Add text to the card.  
            card.Body.Add(new AdaptiveTextBlock()
            {
                Text = ".Net (C#) Developer",
                Size = AdaptiveTextSize.Large,
                Weight = AdaptiveTextWeight.Bolder
            });

            // Add text to the card.  
            card.Body.Add(new AdaptiveTextBlock()
            {
                Text = "ia@lig.com"
            });

            // Add text to the card.  
            card.Body.Add(new AdaptiveTextBlock()
            {
                Text = "923xxxxxx761"
            });

            // Create the attachment with adapative card.  
            Attachment attachment = new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = card
            };
            return attachment;
        }

    }
}
