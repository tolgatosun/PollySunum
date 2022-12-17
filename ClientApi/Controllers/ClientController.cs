using Microsoft.AspNetCore.Mvc;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using Polly.Wrap;
using System.Net;
using System.Reflection.Metadata.Ecma335;
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
        private readonly AsyncTimeoutPolicy _timeoutPolicy;

        public ClientController(IHttpClientFactory httpClientFactory, AsyncRetryPolicy<HttpResponseMessage> retryPolicy, AsyncCircuitBreakerPolicy<HttpResponseMessage> circuitBreakerPolicy, AsyncTimeoutPolicy timeoutPolicy)
        {
            _httpClientFactory = httpClientFactory;
            _circuitBreakerPolicy = circuitBreakerPolicy;
            _retryPolicy = retryPolicy;
            _timeoutPolicy = timeoutPolicy;
        }


        [HttpGet()]
        [Route("GetClientRetry")]
        public async Task<string> GetClientRetry()
        {
            var httpClient = _httpClientFactory.CreateClient();

            var response = await _retryPolicy.ExecuteAsync(() => httpClient.GetAsync("https://localhost:7101/Product/GetProductDetails"));

            if (!response.IsSuccessStatusCode)
            {
                return ("Service is currently unavailable");
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
                return  ("Service is currently unavailable: CircuitState.Open");
            }

            if (_circuitBreakerPolicy.CircuitState == CircuitState.HalfOpen)
            {
                _circuitBreakerPolicy.Reset();

                #region info
                //_circuitBreakerPolicy.Isolate();// devreyi manual açmak için.

                /*
                CircuitState.Closed - Normal operation. Execution of actions allowed.
                CircuitState.Open - The automated controller has opened the circuit. Execution of actions blocked.
                CircuitState.HalfOpen - Recovering from open state, after the automated break duration has expired. Execution of actions permitted. Success of subsequent action/s controls onward transition to Open or Closed state.
                CircuitState.Isolated - Circuit held manually in an open state. Execution of actions blocked.
                */ 
                #endregion
            }

            var httpClient = _httpClientFactory.CreateClient();

            var response = await _circuitBreakerPolicy.ExecuteAsync(() => httpClient.GetAsync("https://localhost:7101/Product/GetProductDetails"));

            if (!response.IsSuccessStatusCode)
            {
                return ("Service is currently unavailable");
            }

            var responseText = await response.Content.ReadAsStringAsync();
            return responseText;
        }


        [HttpGet()]
        [Route("GetClientFallback")]
        public async Task<string> GetClientFallback()
        {
            var httpClient = _httpClientFactory.CreateClient();

            var result = Policy.HandleResult<HttpResponseMessage>(x => x.StatusCode != HttpStatusCode.OK)
                               .Fallback(await httpClient.GetAsync("https://localhost:7101/Product/GetFallBack2"),
                               (response) => {
                                   var result = response;
                               
                               }).Execute(() => httpClient.GetAsync("https://localhost:7101/Product/GetFallBack1").Result );

            if (result.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return "FallBack";
            }
            else
            {
                return "ErrorFallBack";
            }
        }

        [HttpGet()]
        [Route("GetClientTimeout")]
        public async Task<string> GetClientTimeout()
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();

                var response = await _timeoutPolicy.ExecuteAsync(() => httpClient.GetAsync("https://localhost:7101/Product/GetTimeout"));

                if (!response.IsSuccessStatusCode)
                {
                    return ("Service is currently unavailable");
                }

                var responseText = await response.Content.ReadAsStringAsync();
                return responseText;
            }
            catch (TimeoutRejectedException ex)
            {
                Console.WriteLine($"Timeout {ex}");
                return ("Timeout");
            }
        }

    }
}