// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio CoreBot v4.3.0

using System;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Bot.Builder.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Bot.Builder.Integration.ApplicationInsights.WebApi;

namespace AriBotV4.Adapters
{
    public class AdapterWithErrorHandler : BotFrameworkHttpAdapter
    {
        #region Method
        public AdapterWithErrorHandler(ICredentialProvider credentialProvider, ILogger<BotFrameworkHttpAdapter> logger,
            TelemetryInitializerMiddleware telemetryInitializerMiddleware,
            ConversationState conversationState = null)
            : base(credentialProvider)
        {
                Use(telemetryInitializerMiddleware);

            //OnTurnError = async (turnContext, exception) =>
            //{
            //    // Log any leaked exception from the application.
            //    logger.LogError($"Exception caught : {exception.Message}");

            //    // Send a catch-all apology to the user.
            //    await turnContext.SendActivityAsync("Sorry, it looks like something went wrong.");

            //    if (conversationState != null)
            //    {
            //        try
            //        {
            //            // Delete the conversationState for the current conversation to prevent the
            //            // bot from getting stuck in a error-loop caused by being in a bad state.
            //            // ConversationState should be thought of as similar to "cookie-state" in a Web pages.
            //            await conversationState.DeleteAsync(turnContext);
            //        }
            //        catch (Exception e)
            //        {
            //            logger.LogError($"Exception caught on attempting to Delete ConversationState : {e.Message}");
            //        }
            //    }
            //};
        }
        #endregion
    }
}
