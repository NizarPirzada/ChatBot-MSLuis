using AriBotV4.Common;
using AriBotV4.Dialogs.Common.Resources;
using AriBotV4.Enums;
using AriBotV4.Models;
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
using System.Threading;
using System.Threading.Tasks;

namespace AriBotV4.Dialogs.TaskSpur
{
    public class CommonDialog : ComponentDialog
    {
        #region Properties and Fields
        private static BotStateService _botStateService;
        private readonly BotServices _botServices;
        private readonly IConfiguration _configuration;
        private LuisModel luisResponse;
        private BotStateService botStateService;
        private object botServices;

      
        #endregion


        #region WaterFallSteps and Dialogs
        public CommonDialog(string dialogId, BotStateService botStateService) : base(dialogId)
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
               //TaskSpurSignInOptions,
               FinalAsync
            };


            // Add Named Dialogs
            AddDialog(new WaterfallDialog($"{nameof(CommonDialog)}.mainFlow", waterfallSteps));
            AddDialog(new ChoicePrompt($"{nameof(CommonDialog)}.signInOptions"));
            

            // Set the starting Dialog
            InitialDialogId = $"{nameof(CommonDialog)}.mainFlow";

        }

        // Step 1 check user is new or already registered
        private async Task<DialogTurnResult> TaskSpurSignInOptions(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            await stepContext.Context.SendActivityAsync(MessageFactory.SuggestedActions(new CardAction[]
          {
        new CardAction(ActionTypes.OpenUrl,(TaskSpur.Resources.TaskSpur.TaskSpurInfo),value
                            :_botStateService._taskSpurSettings.TaskSpur),
        new CardAction(ActionTypes.OpenUrl,(TaskSpur.Resources.TaskSpur.SignIn),value
                            :_botStateService._taskSpurSettings.LoginURL),
        new CardAction(ActionTypes.OpenUrl,(TaskSpur.Resources.TaskSpur.SignUp),value
                            :_botStateService._taskSpurSettings.SignUpURL),
                 }));



            //return await stepContext.NextAsync(stepContext.Context.Activity.Text, cancellationToken);



            //var chooseDeals = new PromptOptions
            //{
            //    Prompt = new Activity
            //    {

            //        Type = ActivityTypes.Message,
            //        SuggestedActions = new SuggestedActions()
            //        {
            //            Actions = new List<CardAction>()
            //           {

            //                new CardAction(ActionTypes.OpenUrl,(TaskSpur.Resources.TaskSpur.TaskSpurInfo),value
            //                :_botStateService._taskSpurSettings.TaskSpur),

            //                new CardAction(ActionTypes.OpenUrl,(TaskSpur.Resources.TaskSpur.SignIn),value
            //                :_botStateService._taskSpurSettings.LoginURL),


            //                new CardAction(ActionTypes.OpenUrl,(TaskSpur.Resources.TaskSpur.SignUp),value
            //                :_botStateService._taskSpurSettings.SignUpURL),

            //               },
            //        },

            //    }

            //};

            //return await stepContext.PromptAsync($"{nameof(CommonDialog)}.signInOptions", chooseDeals);
        
            return new DialogTurnResult(DialogTurnStatus.Waiting);
           

        }

        private async Task<DialogTurnResult> FinalAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            stepContext.EndDialogAsync(null, cancellationToken);
            return await stepContext.BeginDialogAsync($"{nameof(RootOptionsDialog)}.mainFlow", stepContext.Context.Activity.Text, cancellationToken);


        }
    }

}
#endregion

