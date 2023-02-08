using AriBotV4.Models;
using Microsoft.ApplicationInsights;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using System.Net.Http;
using System.Net.Http.Headers;
using AriBotV4.Common;
using AriBotV4.Models.Activity;
using Newtonsoft.Json;
using System.Text;
using AriBotV4.AppSettings;
using AriBotV4.Services;

namespace AriBotV4.Middleware
{
    public class RequestResponseLoggingMiddleware
    {

        private readonly IHostingEnvironment _env;
        

        //// EndpointUri for the Azure Cosmos account.
        //private string _endpointUri = "";
        //// The primary key for the Azure Cosmos account.
        //private string _primaryKey = "";

        //// The Cosmos client instance
        //private CosmosClient cosmosClient;

        //// The database we will create
        //private Database database;

        //// The container we will create.
        //private Container container;

        //// The name of the database and container we will create
        //private string _databaseId = "";
        //private string _containerId = "";
        private readonly IConfiguration _config;
        private readonly BotStateService _botStateService;



        public RequestResponseLoggingMiddleware(IHostingEnvironment env, IConfiguration config, BotStateService botStateService)
        {
            
            _env = env;
            _config = config;
            _botStateService = botStateService;
            //_endpointUri = _config["AzureCosmoDB:Uri"].ToString();
            //_primaryKey = _config["AzureCosmoDB:PrimaryKey"].ToString();
            //_databaseId = _config["AzureCosmoDB:DatabaseId"].ToString();
            //_containerId = _config["AzureCosmoDB:ContainerId"].ToString();
            //this.cosmosClient = new CosmosClient(_endpointUri, _primaryKey);
            //this.database = this.cosmosClient.GetDatabase(_databaseId);
            //this.container = this.database.GetContainer(_containerId);
        }

        public async Task SaveConversations(string conversationId, string token)
        {
            StoreConverasations storeConverasations = new StoreConverasations();
            // get all the activities from current conversation
             HttpClient client = new HttpClient();
            client.BaseAddress = new Uri("https://directline.botframework.com//v3/directline/conversations/" + conversationId + "/activities");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
                        
            var response = await HttpClientExtensions.ReadAsJsonAsync<AriActivities>(client, "");

            if (response.activities != null)
            {
                storeConverasations.Activities = response;
              

                try
                {
                    var temp = JsonConvert.SerializeObject(storeConverasations);

                    var plainTextBytes = Encoding.UTF8.GetBytes(temp);
                    var tempString = Convert.ToBase64String(plainTextBytes);
                    await _botStateService._azureBlobService.Insert(tempString, conversationId);

                }
                catch (Exception ex)
                {
                    var telemetry = new TelemetryClient();

                    telemetry.TrackException(ex);
                }
            }

        }

    }


}
