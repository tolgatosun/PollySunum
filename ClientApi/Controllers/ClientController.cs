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
        private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;
        private readonly AsyncCircuitBreakerPolicy<HttpResponseMessage> _circuitBreakerPolicy;

        public ClientController(IHttpClientFactory httpClientFactory, AsyncRetryPolicy<HttpResponseMessage> retryPolicy, AsyncCircuitBreakerPolicy<HttpResponseMessage> circuitBreakerPolicy)
        {
            _httpClientFactory = httpClientFactory;
            _circuitBreakerPolicy = circuitBreakerPolicy;
            _retryPolicy = retryPolicy;
        }


        [HttpGet()]
        [Route("GetClientRetry")]
        public async Task<string> GetClientRetry()
        {
            var httpClient = _httpClientFactory.CreateClient();

            var response = await _retryPolicy.ExecuteAsync(() => httpClient.GetAsync("https://localhost:7101/Product/GetProductDetails"));

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Service is currently unavailable");
            }

            var responseText = await response.Content.ReadAsStringAsync();
            return responseText;
        }
         


        [HttpGet()]
        [Route("GetClientCircuitBreaker")]
        public async Task<string> GetClientCircuitBreaker()
        {
            if (_circuitBreakerPolicy.CircuitState == CircuitState.Open)
            {
                throw new Exception("Service is currently unavailable: CircuitState.Open");
            }

            if (_circuitBreakerPolicy.CircuitState == CircuitState.HalfOpen)
            {
                _circuitBreakerPolicy.Reset();
            } 

            var httpClient = _httpClientFactory.CreateClient();

            var response = await _circuitBreakerPolicy.ExecuteAsync(() => httpClient.GetAsync("https://localhost:7101/Product/GetProductDetails"));

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Service is currently unavailable");
            }

            var responseText = await response.Content.ReadAsStringAsync();
            return responseText;
        } 
    }
}