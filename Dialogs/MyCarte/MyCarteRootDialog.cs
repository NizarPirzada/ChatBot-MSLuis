using AriBotV4.Common;
using AriBotV4.Enums.MyCarteEnums;
using AriBotV4.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AriBotV4.Models.MyCarte;
using AutoMapper;
using AdaptiveCards;
using Microsoft.Bot.Schema.Teams;
using System.Text;
using Newtonsoft.Json.Linq;
using AriBotV4.Models;

namespace AriBotV4.Dialogs.MyCarte
{
    public class MyCarteRootDialog : ComponentDialog
    {
        #region #Properties and Fields

        private readonly BotStateService _botStateService;
        private readonly BotServices _botServices;
        private readonly IConfiguration _configuration;

        #endregion


        public MyCarteRootDialog(string dialogId, BotStateService botStateService, BotServices botServices, IConfiguration configuration) : base(dialogId)
        {
            _botStateService = botStateService ?? throw new System.ArgumentNullException(nameof(botStateService));
            _botServices = botServices ?? throw new System.ArgumentNullException(nameof(botServices));
            _configuration = configuration ?? throw new System.ArgumentNullException(nameof(configuration));
            InitializeWaterfallDialog();
        }

        private void InitializeWaterfallDialog()
        {
            // Create Waterfall Steps
            var waterfallSteps = new WaterfallStep[]
            {
                StepProductSearching,
                StepAddToCart,
                //FinalStepAsync
            };

            // Add Named Dialogs
            AddDialog(new WaterfallDialog($"{nameof(MyCarteRootDialog)}.mainFlow", waterfallSteps));
            AddDialog(new ProductSearchingDialog($"{nameof(ProductSearchingDialog)}.mainFlow", _botStateService, _botServices));
            AddDialog(new AddToCartDialog($"{nameof(AddToCartDialog)}.mainFlow", _botStateService, _botServices));

            // Set the starting Dialog
            InitialDialogId = $"{nameof(MyCarteRootDialog)}.mainFlow";
        }

        private async Task<DialogTurnResult> StepProductSearching(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.BeginDialogAsync($"{nameof(ProductSearchingDialog)}.mainFlow", null, cancellationToken);
        }

        private async Task<DialogTurnResult> StepAddToCart(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await stepContext.BeginDialogAsync($"{nameof(AddToCartDialog)}.mainFlow", stepContext.Result, cancellationToken);
            return await stepContext.EndDialogAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.EndDialogAsync(null, cancellationToken);
        }
    }
}
