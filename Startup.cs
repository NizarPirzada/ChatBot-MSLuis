// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio CoreBot v4.3.0

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.DependencyInjection;

using AriBotV4.Bots;
using AriBotV4.Dialogs;
using AriBotV4.Services;
using Microsoft.Extensions.Configuration;
using AriBotV4.Helpers;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using AriBotV4.Interface.Travel;
using AriBotV4.Service.Travel;
using AriBotV4.AppSettings;
using Microsoft.Bot.Builder.Integration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using Microsoft.Bot.Schema;
using AriBotV4.Interface.TaskSpur;
using AriBotV4.Extensions;
using AriBotV4.Models;
using Polly;
using System.Net.Sockets;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Bot.Builder.Integration.ApplicationInsights.WebApi;
using System.Configuration;
using AriBotV4.Interface.AriQuestion;
using AriBotV4.Services.AriQuestions;
using AriBotV4.AppSettings.MyCarte;
using AriBotV4.AppSettings.Intellego;
using AriBotV4.Services.Common;

namespace AriBotV4
{
    public class Startup
    {
        #region Properties and Fields
        public IConfiguration _config { get; }
        public IHostingEnvironment _env { get; }
        #endregion

        #region Method

        // This is the starting point in application
        public Startup(IConfiguration configuration, IHostingEnvironment env)
        {
            _config = configuration;
            _env = env;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            ConfigureAspWebApi(services);

            // Create the credential provider to be used with the bot framework adapter.
            services.AddSingleton<ICredentialProvider, ConfigurationCredentialProvider>();


            // Create the bot framework adapter.
            services.AddSingleton<IBotFrameworkHttpAdapter, BotFrameworkHttpAdapter>();

            var applicationInsights = _config.GetSection(nameof(ApplicationInsights)).Get<ApplicationInsights>();

            // Add Application Insights services into service collection
            services.AddApplicationInsightsTelemetry
                   (applicationInsights.InstrumentationKey);

            //(applicationInsights.)

            // Create the telemetry client.
            services.AddSingleton<IBotTelemetryClient, BotTelemetryClient>();

            // Add telemetry initializer that will set the correlation context for all telemetry items.
            services.AddSingleton<ITelemetryInitializer, OperationCorrelationTelemetryInitializer>();

            // Add telemetry initializer that sets the user ID and session ID (in addition to other bot-specific properties such as activity ID)
            services.AddSingleton<ITelemetryInitializer, TelemetryBotIdInitializer>();

            // Create the telemetry middleware to initialize telemetry gathering
            services.AddSingleton<TelemetryInitializerMiddleware>();

            // Create the telemetry middleware (used by the telemetry initializer) to track conversation events
            services.AddSingleton<TelemetryLoggerMiddleware>();

            // Configure services
            services.AddSingleton<BotServices>();

            // Configure state
            ConfigureState(services);


            //Configure the dialogs
            ConfigureDialogs(services);



            // Create a global hashset for our ConversationReferences
            services.AddSingleton<ConcurrentDictionary<string, ConversationReference>>();
            services.AddSingleton<ConversationReference>();
            // Create the bot as a transient. In this case the asp controller is expecting an IBot.
            services.AddTransient<IBot, MainBot<RootDialog>>();

        }

        // This method is for configuring root dialog
        private void ConfigureDialogs(IServiceCollection services)
        {
            services.AddSingleton<RootDialog>();
        }

        // This method is for configuring state
        public void ConfigureState(IServiceCollection services)
        {
            // create storage for states , memory is great for testing
            var storage = new MemoryStorage();

            services.AddSingleton<IStorage, MemoryStorage>();

            //create the user state
            services.AddSingleton<UserState>();



            //create the ConversationState
            services.AddSingleton<ConversationState>();



            //create instance of State Service
            services.AddSingleton<BotStateService>();
            //services.AddHttpClient<TClient, TImplementation>()
            //   .AddHttpMessageHandler<PollyContextDelegatingHandler<TClient>>()
            //   .AddTransientHttpErrorPolicy(b => b.GetRetryPolicy())
            //   .AddPolicyHandler(PollyPoliciesStore.GetRefreshAuthPolicy())

            services.AddSingleton<ITravelService, TravelService>();
        }

        // This method is configure external service like email service, logger
        public void ConfigureAspWebApi(IServiceCollection services)
        {

            services.AddHttpsRedirection(options =>
           {
               options.RedirectStatusCode = StatusCodes.Status307TemporaryRedirect;
           });

            services.AddCors();



            services.AddMvc().AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                options.SerializerSettings.ContractResolver = new DefaultContractResolver();
            });
            services.AddMvc(option => option.EnableEndpointRouting = false);


            services.AddTransient<IHttpContextAccessor, HttpContextAccessor>();
            services.AddTransient<ISimpleLogger, SimpleLogger>();


            services.AddSingleton<IEmailConfiguration>(_config.GetSection("EmailConfiguration").Get<EmailConfiguration>());

            // Inject application settings
            services.AddSingleton(_config.GetSection("AppSettings").Get<AppSetting>());
            services.AddSingleton(_config.GetSection("BingSettings").Get<BingSettings>());
            services.AddSingleton(_config.GetSection("QnASettings").Get<QnASettings>());
            services.AddSingleton(_config.GetSection("BotSettings").Get<BotSettings>());
            services.AddSingleton(_config.GetSection("TaskSpurSettings").Get<TaskSpurSettings>());
            services.AddSingleton(_config.GetSection("WeatherSettings").Get<WeatherSettings>());
            services.AddSingleton(_config.GetSection("BlobSettings").Get<BlobSettings>());
            services.AddSingleton(_config.GetSection("ApplicationInsights").Get<ApplicationInsights>());
            services.AddSingleton(_config.GetSection("TaskSpurAriSettings").Get<TaskSpurAriSettings>());
            services.AddSingleton(_config.GetSection("TaskSpurToggleSettings").Get<TaskSpurToggleSettings>());
            services.AddSingleton(_config.GetSection("MyCarteToggleSettings").Get<MyCarteToggleSettings>());
            services.AddSingleton(_config.GetSection("IntellegoToggleSettings").Get<IntellegoToggleSettings>());
            services.AddSingleton(_config.GetSection("TextAnalyticsSettings").Get<TextAnalyticsSettings>());

            // Add taskspur client 
            services.AddTransient<ITaskSpurApiClient, TaskSpurApiClient>();
            services.AddHttpClient<ITaskSpurApiClient, TaskSpurApiClient>();
            services.AddTransient<IAriQuestionsApiClient, AriQuestionsApiClient>();
            services.AddHttpClient<IAriQuestionsApiClient, AriQuestionsApiClient>();
            services.AddTransient<IEmailService, EmailService>();
            services.AddTransient<AzureBlobService>();
            services.AddTransient<BotServices>();



        }

        // This method gets called by the runtime. Use this method to configure the http request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IServiceProvider serviceProvider)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }



            // global cors policy
            //app.UseCors("CorsPolicy");
            app.UseCors(x => x
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader());

            app.UseAuthentication();
            app.UseHttpsRedirection();
            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseMvc();



        }



        #endregion

    }
}
