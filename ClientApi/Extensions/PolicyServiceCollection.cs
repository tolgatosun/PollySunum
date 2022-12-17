﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using Polly.Wrap;
using System.Net;

namespace ClientApi.Extensions
{
    public static class PolicyServiceCollection
    {
        public static IServiceCollection RegisterPolicyCollection(this IServiceCollection services)
        {

            #region Policy

            HttpStatusCode[] httpStatusCodesWorthRetrying = {
                        HttpStatusCode.RequestTimeout, // 408
                        HttpStatusCode.InternalServerError, // 500
                        HttpStatusCode.BadGateway, // 502
                        HttpStatusCode.ServiceUnavailable, // 503
                        HttpStatusCode.GatewayTimeout // 504
                    };


            var retryPolicy = Policy
                    .HandleResult<HttpResponseMessage>(message => httpStatusCodesWorthRetrying.Contains(message.StatusCode))
                    .WaitAndRetryAsync(3, retryAttempt =>
                    {
                        Console.WriteLine($"Retrying because of transient error. Attemp {retryAttempt}");
                        return TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                    }); 


            var circuitBreakerPolicy = Policy
                                .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                                .CircuitBreakerAsync(3, TimeSpan.FromSeconds(30));
              

            var circuitBreakerAdvancedPolicy = Policy
                                .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                                .AdvancedCircuitBreakerAsync(0.25, TimeSpan.FromSeconds(60), 7, TimeSpan.FromSeconds(30), OnBreak, OnReset, OnHalfOpen);


            var retryAndCircuitBreakerWrapPolicy = Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);

            var timeoutPolicy = Policy.TimeoutAsync(5, Polly.Timeout.TimeoutStrategy.Pessimistic);

            #endregion







            services.AddSingleton<AsyncRetryPolicy<HttpResponseMessage>>(retryPolicy);
            
            services.AddSingleton<AsyncCircuitBreakerPolicy<HttpResponseMessage>>(circuitBreakerPolicy);

            //services.AddSingleton<AsyncCircuitBreakerPolicy<HttpResponseMessage>>(circuitBreakerAdvancedPolicy);

            //services.AddSingleton<AsyncPolicyWrap<HttpResponseMessage>>(retryAndCircuitBreakerPolicy);

            services.AddSingleton<AsyncTimeoutPolicy>(timeoutPolicy);

            return services;
        }

        static void OnHalfOpen()
        {
            Console.WriteLine("\t\t\t\t\tConnection half open");
        }

        static void OnReset(Context context)
        {
            Console.WriteLine("\t\t\t\t\tConnection reset");
        }

        static void OnBreak(DelegateResult<HttpResponseMessage> delegateResult, TimeSpan timeSpan, Context context)
        {
            Console.WriteLine($"\t\t\t\t\tConnection break: {delegateResult.Result.StatusCode}");
        }
    }
}
