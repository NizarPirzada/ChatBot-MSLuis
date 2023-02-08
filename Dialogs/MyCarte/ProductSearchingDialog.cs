using AriBotV4.Common;
using AriBotV4.Dialogs.Common.Resources;
using AriBotV4.Models;
using AriBotV4.Models.MyCarte;
using AriBotV4.Services;
using AutoMapper.Configuration;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AriBotV4.Dialogs.MyCarte
{
    public class ProductSearchingDialog : ComponentDialog
    {

        #region #Properties and Fields
        private readonly BotStateService _botStateService;
        private readonly BotServices _botServices;


        #endregion
        public ProductSearchingDialog(string dialogId, BotStateService botStateService, BotServices botServices) : base(dialogId)
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
                ChoiceStepAsync,
                ChoiceResponseStepAsync
            };

            // Add Named Dialogs
            AddDialog(new WaterfallDialog($"{nameof(ProductSearchingDialog)}.mainFlow", waterfallSteps));
            AddDialog(new ChoicePrompt($"{nameof(ProductSearchingDialog)}.suggestion"));
            //AddDialog(new TextPrompt($"{nameof(ProductSearchingDialog)}.suggestion"));
            AddDialog(new TextPrompt($"{nameof(ProductSearchingDialog)}.userInput"));

            // Set the starting Dialog
            InitialDialogId = $"{nameof(ProductSearchingDialog)}.mainFlow";
        }

        private async Task<DialogTurnResult> ChoiceStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var prompt = new PromptOptions
            {
                Prompt = MessageFactory.Text(Resources.MyCarte.GetMyCarteInput),
                //Choices = ChoiceFactory.ToChoices(new List<string> { "I am looking for light", "I am looking for shirt", "I am looking for shampoo" }),
                Choices = new List<Choice>
                            {
                                new Choice
                                {
                                    Value = "light",
                                    Synonyms = new List<string>
                                    {
                                        "looking for light",
                                        "I am looking for light"
                                    },
                                },
                                  new Choice
                                {
                                    Value = "shirt",
                                    Synonyms = new List<string>
                                    {
                                        "looking for shirt",
                                        "I am looking for shirt"
                                    },
                                },  new Choice
                                {
                                    Value = "shampoo",
                                    Synonyms = new List<string>
                                    {
                                        "looking for shampoo",
                                        "I am looking for shampoo"
                                    },
                                }
                            },
                Style = ListStyle.HeroCard
            };

            return await stepContext.PromptAsync($"{nameof(ProductSearchingDialog)}.suggestion", prompt, cancellationToken);

            //var prompt = new PromptOptions
            //{
            //    Prompt = new Activity
            //    {
            //        Type = ActivityTypes.Message,
            //        Text = Resources.MyCarte.GetMyCarteInput,
            //        SuggestedActions = new SuggestedActions()
            //        {
            //            Actions = new List<CardAction>()
            //            {
            //                new CardAction() { Title = "I am looking for light", Type = ActionTypes.ImBack, Value = "I am looking for light" },
            //                new CardAction() { Title = "I am looking for shirt", Type = ActionTypes.ImBack, Value = "I am looking for shirt" },
            //                new CardAction() { Title = "I am looking for shampoo", Type = ActionTypes.ImBack, Value = "I am looking for shampoo" },
            //            },
            //        },
            //    }
            //};


            //return await stepContext.PromptAsync($"{nameof(ProductSearchingDialog)}.suggestion", prompt, cancellationToken);
        }

        private async Task<DialogTurnResult> ChoiceResponseStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var userInput = String.Empty;
            try
            {
                var result = (FoundChoice)stepContext.Result;
                userInput = result.Value;
            }
            catch (Exception ex)
            {
                userInput = stepContext.Context.Activity.Text;
            }

            UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
            userProfile.Subject = userInput;
            userProfile.LastMessageReceived = DateTime.UtcNow;
            await _botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);

            //var luisResult = await Utility.GetCommonLuisSearchResult((result.Value), _botStateService._myCartToggleSettings.LuisSettings, stepContext, cancellationToken);

            //var qnaResult = Utility.GetCommonQnaSearchResult((result.Value), _botStateService._myCartToggleSettings.QnASettings);

            //var answer = qnaResult.Answers[0]?.Answer.ToString();
            //Activity qnaReply = ((Activity)stepContext.Context.Activity).CreateReply();

            //try
            //{
            //    string[] qnaAnswerData1 = answer.Split(';');
            //    int dataLength = qnaAnswerData1.Length;

            //    //image and video card
            //    if (dataLength > 1 && dataLength <= 6)
            //    {
            //        var attachment = Utility.GetSelectedCard(answer);
            //        qnaReply.Attachments.Add(attachment);
            //    }

            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine(ex.Message);
            //}

            //get products from mycarte
            var myCartUrl = String.Empty;

            var httpClient = new HttpClient();

            //var mdiResponse = await httpClient.GetAsync("http://192.168.18.46:8000/nlp_analysis/?query=" + result?.Value);
            //var mdiJsonResponse = await mdiResponse.Content.ReadAsStringAsync();
            //var nlpAnalysis = JsonConvert.DeserializeObject<NlpAnalysis>(mdiJsonResponse);

            if (!String.IsNullOrEmpty("nlpAnalysis?.entity?.noun"))
            {
                myCartUrl = _botStateService._myCartToggleSettings.ApiSettings.CusomerProductsUrl + userInput /*nlpAnalysis?.entity?.noun*/;
            }
            else
            {
                myCartUrl = _botStateService._myCartToggleSettings.ApiSettings.CusomerProductsUrl + userInput;
            }

            var myCarteResponse = await httpClient.GetAsync(myCartUrl);

            var attachments = new List<Attachment>();
            //attachments.AddRange(qnaReply.Attachments); // QnA attachments
            var reply = MessageFactory.Attachment(attachments);
            reply.AttachmentLayout = AttachmentLayoutTypes.Carousel;

            if (myCarteResponse.IsSuccessStatusCode)
            {
                try
                {
                    var JsonDataResponse = await myCarteResponse.Content.ReadAsStringAsync();
                    var myCarteData = JsonConvert.DeserializeObject<ProductsResponse>(JsonDataResponse);

                    if (myCarteData?.data?.count > 0)
                    {
                        myCarteData?.data?.data?.ForEach(dataObj =>
                        {
                            dataObj?.prices?.ForEach(price =>
                            {
                                var dataStr = price.priceName + " (" + price.priceId + ")";

                                ThumbnailCard thumbCard = new ThumbnailCard
                                {
                                    Title = price.images[0].description,
                                    Subtitle = dataObj.categories[0].name,
                                    Text = "$" + price.regularPrice + "\t$" + price.discountPrice + "\t" + price.percentageDiscount + "% OFF",
                                };

                                thumbCard.Buttons = new List<CardAction>
                                    {
                                        new CardAction(ActionTypes.OpenUrl, "View Details", value: _botStateService._myCartToggleSettings.ApiSettings.ProductsUrl + dataObj.id),
                                        new CardAction(ActionTypes.ImBack, title: "Add To Cart",  value: dataStr + " Add To Cart")
                                    };

                                thumbCard.Images = new List<CardImage>() { new CardImage() { Url = price.images[0].url } };

                                reply.Attachments.Add(thumbCard.ToAttachment());

                                #region Adaptie Card
                                //AdaptiveCard card = new AdaptiveCard();

                                //card.Speak = "I'm AVA bot";
                                //card.Body.Add(new AdaptiveColumnSet()
                                //{
                                //    Columns = new List<AdaptiveColumn>
                                //    {
                                //        new AdaptiveColumn()
                                //        {
                                //            Width = 1.ToString(),
                                //            Items = new List<AdaptiveElement>(){
                                //                new AdaptiveImage()
                                //                {
                                //                    Url = new Uri(img.url),
                                //                    Size = AdaptiveImageSize.Large,
                                //                    Style = AdaptiveImageStyle.Normal,
                                //                    AltText = ""
                                //                }
                                //            }
                                //        },
                                //        new AdaptiveColumn()
                                //        {
                                //            Width = 2.ToString(),
                                //            Items = new List<AdaptiveElement>() {
                                //                new AdaptiveTextBlock()
                                //                {
                                //                    Text = dataObj.name,
                                //                    Wrap = true,
                                //                    Size = AdaptiveTextSize.Medium,
                                //                    Weight = AdaptiveTextWeight.Bolder
                                //                },
                                //                new AdaptiveTextBlock()
                                //                {
                                //                    Text = dataObj.categories[0].name
                                //                },
                                //                new AdaptiveTextBlock()
                                //                {
                                //                    Text = dataObj.prices[0].priceName
                                //                },
                                //                new AdaptiveTextBlock()
                                //                {
                                //                    Text = "$" + dataObj.prices[0].regularPrice.ToString() + " - $" + dataObj.prices[0].discountPrice.ToString() + " - " + dataObj.prices[0].percentageDiscount.ToString() + "% OFF",
                                //                    Weight = AdaptiveTextWeight.Bolder
                                //                }
                                //            }
                                //        },
                                //    }
                                //});

                                //var cardActions = new List<AdaptiveAction>();
                                //cardActions.Add(new AdaptiveOpenUrlAction()
                                //{
                                //    Type = AdaptiveOpenUrlAction.TypeName,
                                //    Title = "View Details",
                                //    Url = new Uri(_botStateService._myCartToggleSettings.ApiSettings.ProductsUrl + dataObj.id)
                                //});


                                //cardActions.Add(new AdaptiveSubmitAction()
                                //{
                                //    Type = AdaptiveSubmitAction.TypeName,
                                //    Title = "Add To Cart",
                                //    DataJson = jsonObj
                                //});


                                //card.Actions = cardActions;

                                //Attachment attachment = new Attachment()
                                //{
                                //    ContentType = AdaptiveCard.ContentType,
                                //    Content = card
                                //};

                                //reply.Attachments.Add(attachment);
                                #endregion

                            });
                        });
                    }
                }
                catch (Exception ex)
                {

                }
            }

            //await stepContext.Context.SendActivityAsync(MessageFactory.Text("Do you have any specific brand in mind."), cancellationToken);

            if (reply.Attachments.Count > 0)
            {
                await stepContext.Context.SendActivityAsync(reply, cancellationToken);
                //await stepContext.Context.SendActivityAsync(MessageFactory.Text("This is what I found from My Carte!"), cancellationToken);
                //return await stepContext.NextAsync(//$"{nameof(MyCarteRootDialog)}.suggestionResponse",
                //                                   //    new PromptOptions
                //                                   //    {
                //                                   //        Prompt = MessageFactory.Text("This is what I found from My Carte!"),
                //                                   //    }, 
                //    cancellationToken);

                var prompt = new PromptOptions
                {
                    Prompt = MessageFactory.Text("This is what I found from My Carte!"),
                    Style = ListStyle.None
                };

                return await stepContext.PromptAsync($"{nameof(ProductSearchingDialog)}.userInput", prompt, cancellationToken);
            }
            else
            {
                var prompt = new PromptOptions
                {
                    Prompt = MessageFactory.Text("Nothing is found from My Carte. Is there anything else I can do for you?"),
                    Style = ListStyle.None
                };

                return await stepContext.PromptAsync($"{nameof(ProductSearchingDialog)}.userInput", prompt, cancellationToken);
            }
        }


        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.EndDialogAsync(null, cancellationToken);
        }




        private List<Choice> GetChoices()
        {
            //var jsonStr = _configuration.GetValue("MyCarteToggleSettings:Suggestion");
            //var choices = JsonConvert.DeserializeObject<List<Choice>>(jsonStr);

            var choices = new List<Choice>();

            var action1 = new CardAction();
            action1.Title = "I am looking for light";
            action1.Type = ActionTypes.ImBack;
            action1.Value = "I am looking for light";
            choices.Add(new Choice() { Value = "I am looking for light", Action = action1 });

            var action2 = new CardAction();
            action2.Title = "I am looking for men's shirts";
            action2.Type = ActionTypes.ImBack;
            action2.Value = "I am looking for men's shirts";
            choices.Add(new Choice() { Value = "I am looking for men's shirts", Action = action2 });

            var action3 = new CardAction();
            action3.Title = "I am looking for shampoo";
            action3.Type = ActionTypes.ImBack;
            action3.Value = "I am looking for shampoo";
            choices.Add(new Choice() { Value = "I am looking for shampoo", Action = action3 });

            if (!String.IsNullOrEmpty(_botStateService._myCartToggleSettings.ApiSettings.BasketId))
            {
                var action4 = new CardAction();
                action4.Title = "View Cart";
                action4.Type = ActionTypes.OpenUrl;
                //action4.Value = _botStateService._myCartToggleSettings.ApiSettings.AddToCart + "/" + _botStateService._myCartToggleSettings.ApiSettings.BasketId;
                action4.Value = "https://mycarte-web-app-dev.azurewebsites.net/view-cart";

                choices.Add(new Choice() { Value = "View Cart", Action = action4 });
            }

            return choices;
        }

    }

}
