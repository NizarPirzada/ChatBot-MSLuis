using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using AriBotV4.Services;
using System.Collections.Generic;
using AriBotV4.Common;
using AriBotV4.Bots.Resources;
using System;
using System.Linq;
using System.Collections.Concurrent;
using AriBotV4.Dialogs;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using AriBotV4.Models;
using AriBotV4.Models.TaskSpur.Tasks;
using AriBotV4.Models.TaskSpur.Tasks.User;
using AriBotV4.Models.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using AriBotV4.Dialogs.Common.Resources;
using AriBotV4.Middleware;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using AriBotV4.Models.MyCarte;

namespace AriBotV4.Bots
{
    public class MainBot<T> : ActivityHandler where T : Dialog
    {

        #region Properties and Fields

        protected readonly Dialog _dialog;
        protected readonly BotStateService _botStateService;
        protected readonly ILogger _logger;
        private readonly IBotFrameworkHttpAdapter _adapter;
        private readonly IConfiguration _configuration;
        private readonly IHostingEnvironment _env;

        private readonly ConcurrentDictionary<string, ConversationReference> _conversationReferences;


        #endregion

        #region Method
        public MainBot(BotStateService botStateService, T dialog, ILogger<MainBot<T>> logger,
            ConcurrentDictionary<string, ConversationReference> conversationReferences, IBotFrameworkHttpAdapter adapter,
            IConfiguration configuration, IHostingEnvironment env)
        {
            _botStateService = botStateService ?? throw new System.ArgumentNullException(nameof(botStateService));
            _dialog = dialog ?? throw new System.ArgumentNullException(nameof(dialog));
            _logger = logger ?? throw new System.ArgumentNullException(nameof(logger));
            _conversationReferences = conversationReferences;
            _adapter = adapter;
            _configuration = configuration;
            _env = env;
        }
        //private void AddConversationReference(Activity activity)
        //{
        //    var conversationReference = activity.GetConversationReference();
        //    _conversationReferences.AddOrUpdate(conversationReference.User.Id, conversationReference, (key, newValue) => conversationReference);
        //}

        //protected override Task OnConversationUpdateActivityAsync(ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        //{
        //    AddConversationReference(turnContext.Activity as Activity);

        //    return base.OnConversationUpdateActivityAsync(turnContext, cancellationToken);
        //}
        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            await base.OnTurnAsync(turnContext, cancellationToken);

            // Save any state changes that might have occured during the turn.
            await _botStateService.UserState.SaveChangesAsync(turnContext, false, cancellationToken);
            await _botStateService.ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
        }


        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            //UserProfile userProfile = await _botStateService.UserProfileAccessor.GetAsync(turnContext, () => new UserProfile());
            foreach (var member in membersAdded)
            {
                // Greet anyone that was not the target (recipient) of this message.
                if (!string.IsNullOrEmpty(member.Name))
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(Convert.ToString(turnContext.Activity.From.Properties[Constants.TaskSpurToken])))
                        {
                            GetUserResponse response = await _botStateService._taskSpurApiClient.GetTaskSpurUser(member.Id,
                                Convert.ToString(turnContext.Activity.From.Properties[Constants.TaskSpurToken]),
                                Convert.ToString(turnContext.Activity.From.Properties[Constants.TaskSpurTimeZone]));


                            if (response.ariUsageCounter == 0)
                            {
                                AddUserRequest addUserRequest = new AddUserRequest();
                                addUserRequest.UserId = member.Id;
                                addUserRequest.AriUsageCounter = 1;
                                AddUserResponse addUserResponse = await _botStateService._taskSpurApiClient.AddTaskSpurUser(addUserRequest,
                                    Convert.ToString(turnContext.Activity.From.Properties[Constants.TaskSpurToken]));

                            }
                            else
                            {
                                AddUserRequest addUserRequest = new AddUserRequest();
                                addUserRequest.UserId = member.Id;
                                addUserRequest.AriUsageCounter = response.ariUsageCounter + 1;
                                await _botStateService._taskSpurApiClient.UpdateTaskSpurUser(addUserRequest,
                                    Convert.ToString(turnContext.Activity.From.Properties[Constants.TaskSpurToken]));

                            }

                        }
                    }
                    catch (Exception ex)
                    {
                        EmailRequest emailRequest = new EmailRequest();
                        emailRequest.Subject = _configuration.GetValue<string>("AriUsageEmailSettings:Name") + _env.EnvironmentName;
                        emailRequest.ToEmail = _configuration.GetValue<string>("AriUsageEmailSettings:Email");
                        emailRequest.Body = _configuration.GetValue<string>("AriUsageEmailSettings:Body" + "Exception : " + ex.Message + " StackTrace : " + ex.StackTrace);
                        await _botStateService._ariQuestionsApiClient.SendEmail(Convert.ToString(turnContext.Activity.From.Properties[Constants.TaskSpurToken]),
                         ex.Message, emailRequest
                            );
                    }

                    break;
                }

            }

            await _dialog.Run(turnContext, _botStateService.DialogStateAccessor, cancellationToken);
        }


        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            //AddConversationReference(turnContext.Activity as Activity);

            RequestResponseLoggingMiddleware requestResponseLoggingMiddleware = new RequestResponseLoggingMiddleware(_env, _configuration, _botStateService);
            requestResponseLoggingMiddleware.SaveConversations(Convert.ToString(turnContext.Activity.From.Properties[AriBotV4.Common.Constants.ConversationId]),
                Convert.ToString(turnContext.Activity.From.Properties[AriBotV4.Common.Constants.ConversationToken]));

            _logger.LogInformation("Running dialog with Message Activity.");

            // Run the Dialog with the new message Activity.
            await _dialog.Run(turnContext, _botStateService.DialogStateAccessor, cancellationToken);
        }
        #endregion
    }

}
