using AriBotV4.Common;
using AriBotV4.Enums;
using AriBotV4.Models;
using AriBotV4.Models.TaskSpur.Auth;
using AriBotV4.Models.TaskSpur.Goals;
using AriBotV4.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AriBotV4.Dialogs.TaskSpur
{
    public class CreateGoalDialog : ComponentDialog
    {
        #region Properties and Fields
        private readonly BotStateService _botStateService;
        
        #endregion
        public CreateGoalDialog(string dialogId, BotStateService botStateService) : base(dialogId)
        {
            _botStateService = botStateService ?? throw new System.ArgumentNullException(nameof(botStateService));

            InitializeWaterfallDialog();
        }

        private void InitializeWaterfallDialog()
        {
            // Create Waterfall Steps
            var waterfallSteps = new WaterfallStep[]
            {
                AskGoalName,
                AskGoalCategory,
                GoalCreated

            };

            // Add Named Dialogs
            AddDialog(new WaterfallDialog($"{nameof(CreateGoalDialog)}.mainFlow", waterfallSteps));
            AddDialog(new ChoicePrompt($"{nameof(CreateGoalDialog)}.category"));
            AddDialog(new TextPrompt($"{nameof(CreateGoalDialog)}.name", ValidateGoalName));
            AddDialog(new CommonDialog($"{nameof(CommonDialog)}.mainFlow", _botStateService));

            // Set the starting Dialog
            InitialDialogId = $"{nameof(CreateGoalDialog)}.mainFlow";
        }

        // Ask user goal name
        public async Task<DialogTurnResult> AskGoalName(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            
           if (string.IsNullOrEmpty(Convert.ToString(stepContext.Context.Activity.From.Properties[Constants.TaskSpurToken])))
            {
                return await stepContext.BeginDialogAsync($"{nameof(CommonDialog)}.mainFlow", null, cancellationToken);
            }
            else
            {
                return await stepContext.PromptAsync($"{nameof(CreateGoalDialog)}.name",
                         new PromptOptions
                         {
                             Prompt = MessageFactory.Text(TaskSpur.Resources.TaskSpur.AskGoalName),
                             RetryPrompt = MessageFactory.Text(TaskSpur.Resources.TaskSpur.GoalEmpty)

                         }, cancellationToken);
            }
        }

        // Ask user goal category
        public async Task<DialogTurnResult> AskGoalCategory(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            try
            {
                UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
                
                userProfile.CreateGoal = new Dictionary<string, string>();
                userProfile.CreateGoal.Add(Constants.GoalName, (string)stepContext.Result);
                userProfile.LastMessageReceived = DateTime.UtcNow;
                await _botStateService.UserProfileAccessor.SetAsync(stepContext.Context, userProfile);
            }catch(Exception ex)
            {

            }


            await stepContext.Context.SendActivityAsync(TaskSpur.Resources.TaskSpur.AskGoalCategory);

            var prompt = new PromptOptions
            {

                Choices = ChoiceFactory.ToChoices(new List<string>
                {
                        EnumHelpers.GetEnumDescription(AriBotV4.Enums.TaskSpur.Life),
                        EnumHelpers.GetEnumDescription(AriBotV4.Enums.TaskSpur.Work),
                        EnumHelpers.GetEnumDescription(AriBotV4.Enums.TaskSpur.Finance),
                        EnumHelpers.GetEnumDescription(AriBotV4.Enums.TaskSpur.Health),
                }),
                Style = ListStyle.SuggestedAction,

            };

            return await stepContext.PromptAsync($"{nameof(CreateGoalDialog)}.category", prompt, cancellationToken);
        }


        // Goal created
        public async Task<DialogTurnResult> GoalCreated(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            //AriBotV4.Enums.TaskSpur category = (AriBotV4.Enums.TaskSpur)Enum.Parse
            //    (typeof(AriBotV4.Enums.TaskSpur), ((FoundChoice)stepContext.Result).Value, true);
            UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(stepContext.Context, () => new UserProfile());
            
            CreateGoalRequest createGoalRequest = new CreateGoalRequest();
            createGoalRequest.name = userProfile.CreateGoal[Constants.GoalName];
            createGoalRequest.description = string.Empty;
            createGoalRequest.categoryId = (int)EnumHelpers.GetValueFromDescription<AriBotV4.Enums.TaskSpur>(((FoundChoice)stepContext.Result).Value);
            createGoalRequest.priorityId = 0;
           

            // Call create goal api
            CreateGoalResponse response = await _botStateService._taskSpurApiClient.CreateGoal(createGoalRequest,
                Convert.ToString(stepContext.Context.Activity.From.Properties[Constants.TaskSpurToken]));

            // Check token authrozation
            //if (response.statusCode == 401)
            //{

            //    // Get refersh token
            //    Models.TaskSpur.Auth.TokenResponse refreshTokenResponse = await _botStateService._taskSpurApiClient.GetRefreshToken(stepContext);

            //    // Create goal with refresh token
            //    if (!string.IsNullOrEmpty(refreshTokenResponse.data.token))
            //    {
            //        response = await _botStateService._taskSpurApiClient.CreateGoal(createGoalRequest,
            //              refreshTokenResponse.data.token);
            //        await stepContext.Context.SendActivityAsync(MessageFactory.Text(response.toast.message));
            //    }

            //    // Update users's from property
            //    await _botStateService._taskSpurApiClient.UpdateToken(stepContext, refreshTokenResponse);


            //}
            //else
            //{
                if (response != null &&  response.message != null )
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(response.message));
                else
                {
                    
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(response.toast.message));
                }
                
                   

            //}
            return await stepContext.BeginDialogAsync($"{nameof(AnythingElseDialog)}.AnythingElse", TaskSpur.Resources.TaskSpur.CreateGoals + "|" + response.data.code, cancellationToken);
        }

        private static async Task<bool> ValidateGoalName(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
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


    }
}
