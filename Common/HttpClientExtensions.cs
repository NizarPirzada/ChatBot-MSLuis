using AriBotV4.Models.TaskSpur.Goals;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace AriBotV4.Common
{
    public static class HttpClientExtensions
    {

        public static Task<HttpResponseMessage> PostAsJsonAsync<T>(this HttpClient httpClient, string url, T data)
        {
            var dataAsString = JsonConvert.SerializeObject(data);
            var content = new StringContent(dataAsString);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var v = new Uri(httpClient.BaseAddress.ToString() + url, UriKind.Absolute);
            return httpClient.PostAsync(v , content);
        }

        public static Task<HttpResponseMessage> PuttAsJsonAsync<T>(this HttpClient httpClient, string url, T data)
        {
            var dataAsString = JsonConvert.SerializeObject(data);
            var content = new StringContent(dataAsString);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            return httpClient.PutAsync(new Uri(httpClient.BaseAddress.ToString() + url, UriKind.Absolute), content);
        }

        //public static async Task<T> ReadAsJsonAsync<T>(this HttpContent content)
        //{
        //    var dataAsString = await content.ReadAsStringAsync();
        //    return JsonConvert.DeserializeObject<T>(dataAsString);
        //}

        public static async Task<T> ReadAsJsonAsync<T>(this HttpClient httpClient, string url)
        {
            var response = await httpClient.GetAsync(new Uri(httpClient.BaseAddress.ToString() + url, UriKind.Absolute));
           
            return  JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync());
        }

        public static async Task<T> DeleteAsJsonAsync<T>(this HttpClient httpClient, string url)
        {
            var response = await httpClient.DeleteAsync(new Uri(httpClient.BaseAddress.ToString() + url, UriKind.Absolute));

            return JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync());
        }
        public static Task<HttpResponseMessage> PatchAsync<T>(this HttpClient httpClient, string url, T data)
        {
            var dataAsString = JsonConvert.SerializeObject(data);
            var content = new StringContent(dataAsString);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            return httpClient.PatchAsync(new Uri(httpClient.BaseAddress.ToString() + url, UriKind.Absolute), content);
        }
    }

}
