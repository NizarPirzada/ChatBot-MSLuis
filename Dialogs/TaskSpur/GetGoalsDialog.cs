using AriBotV4.Common;
using AriBotV4.Enums;
using AriBotV4.Models;
using AriBotV4.Models.TaskSpur.Goals.Get;
using AriBotV4.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AriBotV4.Dialogs.TaskSpur
{
    public class GetGoalsDialog : ComponentDialog
    {
        #region Properties and Fields
        private readonly BotStateService _botStateService;
        private readonly BotServices _botServices;

        #endregion

        public GetGoalsDialog(string dialogId, BotStateService botStateService, BotServices botServices) : base(dialogId)
        {
            _botStateService = botStateService ?? throw new System.ArgumentNullException(nameof(botStateService));
            _botServices = botServices ?? throw new System.ArgumentNullException(nameof(botStateService));
            InitializeWaterfallDialog();
        }

        private void InitializeWaterfallDialog()
        {
            // Create Waterfall Steps
            var waterfallSteps = new WaterfallStep[]
            {
                 GetGoals,
                FinalAsync

            };

            // Add Named Dialogs
            AddDialog(new WaterfallDialog($"{nameof(GetGoalsDialog)}.mainFlow", waterfallSteps));
            AddDialog(new TextPrompt($"{nameof(GetGoalsDialog)}.userInput"));
            AddDialog(new CommonDialog($"{nameof(CommonDialog)}.mainFlow", _botStateService));

            // Set the starting Dialog
            InitialDialogId = $"{nameof(GetGoalsDialog)}.mainFlow";
        }

        private async Task<DialogTurnResult> GetGoals(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

           if (string.IsNullOrEmpty(Convert.ToString(stepContext.Context.Activity.From.Properties[Constants.TaskSpurToken])))
            {
                await stepContext.EndDialogAsync(null, cancellationToken);
                return await stepContext.BeginDialogAsync($"{nameof(CommonDialog)}.mainFlow", null, cancellationToken);
            }
            else
            {
              LuisModel luisResponse;

        RecognizerResult recognizerResult = new RecognizerResult();
                // First, we use the dispatch model to determine which cognitive service (LUIS or Qna) to use
             
                    recognizerResult = await _botServices.Dispatch.RecognizeAsync(stepContext.Context, cancellationToken);
                    luisResponse = JsonConvert.DeserializeObject<LuisModel>(JsonConvert.SerializeObject(recognizerResult));
             
                LuisModel.Intent topIntent = new LuisModel.Intent();
                int categoryId = 0;
                if (luisResponse.Entities._instance.category != null)
                {
                   categoryId = (int)EnumHelpers.GetValueFromDescription<AriBotV4.Enums.GoalCategoryEnum>(luisResponse.Entities._instance.category[0].Text);
                }


                // get goals api 
                GetGoalsResponse goalResponse = await _botStateService._taskSpurApiClient.GetGoals
    (Convert.ToString(stepContext.Context.Activity.From.Properties[AriBotV4.Common.Constants.TaskSpurToken]),"", categoryId, luisResponse.Entities._instance.Active != null ? true : false);


                if (goalResponse!= null && goalResponse.data != null)
                {
                    // Create reply
                    var reply = stepContext.Context.Activity.CreateReply();
                    if (goalResponse.data.data.Count > 0)
                    {
                        for (int i = 0; i <= goalResponse.data.data.Count - 1; i++)
                        {
                            reply.Attachments.Add(GetGoals(goalResponse.data.data[i]));
                        }
                    }
                    if (reply.Attachments.Count == 0)
                    {
                        await stepContext.Context.SendActivityAsync(MessageFactory.Text(goalResponse.toast.message));
                    }
                    else
                    {
                        var entity = new Microsoft.Bot.Schema.Entity();
                        entity.SetAs(new Mention()
                        {

                            Mentioned = new ChannelAccount()
                            {
                                Role = "Tasks"
                            }
                        });
                        reply.Entities.Add(entity);
                        await stepContext.Context.SendActivityAsync(reply, cancellationToken);
                    }
                    //await stepContext.Context.SendActivityAsync(MessageFactory.Text(response.toast.message));

                }
                else
                {
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(TaskSpur.Resources.TaskSpur.CannotFindGoal));
                }

            }

             return await stepContext.NextAsync(stepContext, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            await stepContext.EndDialogAsync(null, cancellationToken);
            return await stepContext.BeginDialogAsync($"{nameof(AnythingElseDialog)}.AnythingElse", null, cancellationToken);
        }

        public static Microsoft.Bot.Schema.Attachment GetGoals(Object data)
        {
            //if (Convert.ToString(data.GetType().GetProperty("system")?.GetValue(data, null)) != "False")
            //{
                var heroCard = new HeroCard()
                {
                    Title = Convert.ToString(data.GetType().GetProperty("name")?.GetValue(data, null)),
                    Subtitle = Convert.ToString(data.GetType().GetProperty("description")?.GetValue(data, null)),
                    Text = Newtonsoft.Json.JsonConvert.SerializeObject(data),

                    //  Text = data.priority

                };

                return heroCard.ToAttachment();
            //}
            //else
            //{
            //    var heroCard = new HeroCard()
            //    {
            //        Title = Convert.ToString(data.GetType().GetProperty("name")?.GetValue(data, null)),
            //        //Subtitle = Convert.ToString(data.GetType().GetProperty("description")?.GetValue(data, null)),
            //        Text = Newtonsoft.Json.JsonConvert.SerializeObject(data),
            //        Buttons = new List<CardAction>
            //        {
            //            new CardAction(ActionTypes.PostBack, TaskSpur.Resources.TaskSpur.TaskSpurChoose, value: Convert.ToString(data.GetType().GetProperty("name")?.GetValue(data, null)) + "|"+ Convert.ToString(data.GetType().GetProperty("id")?.GetValue(data, null))),

            //        },
            //        //  Text = data.priority

            //    };

            //    return heroCard.ToAttachment();
            //}
        }
    }
}
