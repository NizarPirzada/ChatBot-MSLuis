using AriBotV4.Common;
using AriBotV4.Models.MyCarte;
using AriBotV4.Services;
using AutoMapper.Configuration;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AriBotV4.Dialogs.MyCarte
{
    public class AddToCartDialog : ComponentDialog
    {

        #region #Properties and Fields
        private readonly BotStateService _botStateService;
        private readonly BotServices _botServices;


        #endregion
        public AddToCartDialog(string dialogId, BotStateService botStateService, BotServices botServices) : base(dialogId)
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
                AddToCartStepAsync
            };

            // Add Named Dialogs
            AddDialog(new WaterfallDialog($"{nameof(AddToCartDialog)}.mainFlow", waterfallSteps));
            AddDialog(new TextPrompt($"{nameof(AddToCartDialog)}.userInput"));

            // Set the starting Dialog
            InitialDialogId = $"{nameof(AddToCartDialog)}.mainFlow";
        }

        private async Task<DialogTurnResult> AddToCartStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            try
            {
                // call add to cart method
                if (stepContext.Context.Activity.Text.Contains("Add To Cart"))
                {
                    using (var client = new HttpClient())
                    {
                        var matches = Regex.Matches(stepContext.Context.Activity.Text, @"(\d+)");

                        var priceId = int.Parse(matches.Last().Value);

                        var dataStr = "{id: \"" + GetBasketId() + "\", items: [{quantity: 1, priceId: " + priceId + "}]}";
                        var content = new StringContent(dataStr, Encoding.UTF8, "application/json");

                        var getBasket = await client.GetAsync(_botStateService._myCartToggleSettings.ApiSettings.AddToCart + "/" + GetBasketId());

                        var result = new HttpResponseMessage();

                        if (getBasket.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            string resultContent = await getBasket.Content.ReadAsStringAsync();
                            var addToCartData = JsonConvert.DeserializeObject<AddToCartResponse>(resultContent);

                            var data = JsonConvert.DeserializeObject<AddToCartRequest>(stepContext.Context.Activity.Text);
                            var itemList = data.items;
                            addToCartData.data.business.ForEach(b => {
                                b.items.ForEach(item => {
                                    itemList.Add(new Item() { quantity = item.quantity, priceId = item.priceId });
                                });

                                data.items = itemList;
                            });

                            var strObj = JsonConvert.SerializeObject(data);
                            var postData = new StringContent(strObj, Encoding.UTF8, "application/json");
                            result = await client.PutAsync(_botStateService._myCartToggleSettings.ApiSettings.AddToCart, postData);
                        }
                        else
                        {
                            result = await client.PostAsync(_botStateService._myCartToggleSettings.ApiSettings.AddToCart, content);
                        }

                        if (result.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            string resultContent = await result.Content.ReadAsStringAsync();

                            var addToCartData = JsonConvert.DeserializeObject<AddToCartResponse>(resultContent);

                            var message = "Item " + addToCartData.data.business[0].items[0].productName + " with discount price $ " + addToCartData.data.business[0].items[0].discountPrice + " added to Cart Id: " + addToCartData.data.id + " successfully!";

                            await stepContext.Context.SendActivityAsync(MessageFactory.Text(message), cancellationToken);
                        }
                    }
                }

            }
            catch (Exception ex)
            {

            }

            var prompt = new PromptOptions
            {
                Prompt = MessageFactory.Text("What else I can do for you"),
                Style = ListStyle.None
            };

            return await stepContext.PromptAsync($"{nameof(AddToCartDialog)}.userInput", prompt, cancellationToken);
        }


        private string GetBasketId()
        {
            var basketId = _botStateService._myCartToggleSettings.ApiSettings.BasketId;

            if (String.IsNullOrEmpty(basketId))
            {
                basketId = Guid.NewGuid().ToString();
                _botStateService._myCartToggleSettings.ApiSettings.BasketId = basketId;
            }

            return basketId;
        }

    }
}
