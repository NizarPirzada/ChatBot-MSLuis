using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using AriBotV4.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using AriBotV4.Interface.Travel;
using AriBotV4.Models;
using Microsoft.Extensions.Configuration;
using AriBotV4.Dialogs.MyCarte;

namespace AriBotV4.Dialogs
{
    public class RootDialog : ComponentDialog
    {
        #region Properties and Fields
        private readonly BotStateService _botStateService;
        private readonly BotServices _botServices;
        private readonly ITravelService _travelService;
        private readonly IBotTelemetryClient _telemetryClient;
        private readonly IConfiguration _configuration;
        #endregion

        #region Method
        public RootDialog(BotStateService botStateService, BotServices botServices, ITravelService travelService,
            IBotTelemetryClient telemetryClient, IConfiguration configuration) : base(nameof(RootDialog))
        {
            _botStateService = botStateService ?? throw new System.ArgumentNullException(nameof(botStateService));
            _botServices = botServices ?? throw new System.ArgumentNullException(nameof(botServices));
            _travelService = travelService ?? throw new System.ArgumentNullException(nameof(travelService));
            _travelService = travelService ?? throw new System.ArgumentNullException(nameof(travelService));
            _configuration = configuration ?? throw new System.ArgumentNullException(nameof(configuration));
            _telemetryClient = telemetryClient;
            InitializeWaterfallDialog();
        }

        private void InitializeWaterfallDialog()
        {
            // Create Waterfall Steps
            var waterfallSteps = new WaterfallStep[]
            {

                InitialStepAsync,
                FinalStepAsync
            };

            // Add Named Dialogs
            AddDialog(new RootOptionsDialog($"{nameof(RootDialog)}.contactOptions", _botStateService, _botServices, _travelService, _configuration));
            AddDialog(new GreetingDialog($"{nameof(RootDialog)}.greeting", _botStateService));
            AddDialog(new WaterfallDialog($"{nameof(RootDialog)}.mainFlow", waterfallSteps));
            AddDialog(new MyCarteRootDialog($"{nameof(MyCarteRootDialog)}.mainFlow", _botStateService, _botServices, _configuration));

            // Set the starting Dialog
            InitialDialogId = $"{nameof(RootDialog)}.mainFlow";
        }


        private async Task<DialogTurnResult> InitialStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            //if (Convert.ToInt32(stepContext.Context.Activity.From.Properties["project"]) == 0)
            if (Convert.ToInt32(_configuration.GetValue<int>("ProjectId")) == 0)//mycode
                return await stepContext.BeginDialogAsync($"{nameof(RootDialog)}.contactOptions", null, cancellationToken);
            else
                return await stepContext.BeginDialogAsync($"{nameof(MyCarteRootDialog)}.mainFlow", null, cancellationToken);

        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            return await stepContext.EndDialogAsync(null, cancellationToken);
        }

        #endregion
    }
}
