using Microsoft.AspNetCore.Mvc;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Wrap;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace ClientApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ClientController : ControllerBase
    {  
        private readonly IHttpClientFactory _httpClientFactory;
         
        private readonly AsyncCircuitBreakerPolicy<HttpResponseMessage> _asyncCircuitBreakerPolicy;
        
        private static readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy =
             Policy.HandleResult<HttpResponseMessage>(message => (int)message.StatusCode == 429 || (int)message.StatusCode >= 500)
                   .WaitAndRetryAsync(2, retryAttempt =>
                  {
                      Console.WriteLine($"Retrying because of transient error. Attemp {retryAttempt}");
                      return TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                  });


        //private static readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy2 =
        //     Policy.HandleResult<HttpResponseMessage>(message => (int)message.StatusCode == 429 || (int)message.StatusCode >= 500)
        //           .WaitAndRetryForever(2, retryCount =>
        //           {
        //               Console.WriteLine($"Retrying because of transient error. Attemp {retryCount}");
        //               return TimeSpan.FromSeconds(Math.Pow(2, retryCount));
        //           });

        private static readonly AsyncCircuitBreakerPolicy<HttpResponseMessage> _circuitBreakerPolicy =
             Policy.HandleResult<HttpResponseMessage>(message => (int)message.StatusCode == 503)
                   .CircuitBreakerAsync(2, TimeSpan.FromMinutes(1));
        
           
        public ClientController(IHttpClientFactory httpClientFactory, AsyncCircuitBreakerPolicy<HttpResponseMessage> asyncCircuitBreakerPolicy)
        {
            _httpClientFactory = httpClientFactory;
            _asyncCircuitBreakerPolicy = asyncCircuitBreakerPolicy;
        }


        [HttpGet()]
        [Route("GetClientRetry")]
        public async Task<string> GetClientRetry()
        {
            var httpClient = _httpClientFactory.CreateClient();

            var response = await _retryPolicy.ExecuteAsync(() =>
                httpClient.GetAsync("https://localhost:7101/Product/GetProductDetails"));

            if (response.IsSuccessStatusCode)
            {
                throw new Exception("Service is currently unavailable");
            }

            var responseText = await response.Content.ReadAsStringAsync();
            return responseText;
        }
         


        [HttpGet()]
        [Route("GetClientCircuitBreakerHalfOpen")]
        public async Task<string> GetClientCircuitBreakerHalfOpen()
        {
            if (_asyncCircuitBreakerPolicy.CircuitState == CircuitState.HalfOpen)
            {

            } 

            var httpClient = _httpClientFactory.CreateClient();
            var response = await _asyncCircuitBreakerPolicy.ExecuteAsync(() =>
                httpClient.GetAsync("https://localhost:7101/Product/GetProductDetails"));

            if (response.IsSuccessStatusCode)
            {
                throw new Exception("Service is currently unavailable");
            }

            var responseText = await response.Content.ReadAsStringAsync();
            return responseText;
        } 
    }
}