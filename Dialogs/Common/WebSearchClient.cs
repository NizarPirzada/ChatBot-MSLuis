using Microsoft.Azure.CognitiveServices.Search.WebSearch;

namespace AriBotV4.Dialogs.Common
{
    internal class WebSearchClient
    {
        private ApiKeyServiceClientCredentials apiKeyServiceClientCredentials;

        public WebSearchClient(ApiKeyServiceClientCredentials apiKeyServiceClientCredentials)
        {
            this.apiKeyServiceClientCredentials = apiKeyServiceClientCredentials;
        }
    }
}