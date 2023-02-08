using AriBotV4.Interface.TaskSpur;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace AriBotV4.Extensions
{
    public class AuthTokenDelegatingHandler<T> : DelegatingHandler where T : ITaskSpurApiClient
    {
       // private readonly IAuthTokenStore authTokenStore;

       

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authTokenStore.GetTokenForApiClient<T>());
            //return base.SendAsync(request, cancellationToken);
            return null;
        }
    }
}
