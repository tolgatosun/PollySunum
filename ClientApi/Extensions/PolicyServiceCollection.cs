using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using System.Net;

namespace ClientApi.Extensions
{
    public static class PolicyServiceCollection
    {
        public static IServiceCollection RegisterPolicyCollection(this IServiceCollection services)
        {
            HttpStatusCode[] httpStatusCodesWorthRetrying = {
                        HttpStatusCode.RequestTimeout, // 408
                        HttpStatusCode.InternalServerError, // 500
                        HttpStatusCode.BadGateway, // 502
                        HttpStatusCode.ServiceUnavailable, // 503
                        HttpStatusCode.GatewayTimeout // 504
                    };

            var retryPolicy = Policy
                    .HandleResult<HttpResponseMessage>(message => httpStatusCodesWorthRetrying.Contains(message.StatusCode))
                    .WaitAndRetryAsync(2, retryAttempt =>
                    {
                        Console.WriteLine($"Retrying because of transient error. Attemp {retryAttempt}");
                        return TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                    });

            var circuitBreakerPolicy = Policy
                                .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                                .CircuitBreakerAsync(2, TimeSpan.FromSeconds(30));

            var circuitBreakerPolicyHalfOpen = Policy
                                .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                                .CircuitBreakerAsync(2, TimeSpan.FromSeconds(30));

            var advancedCircuitBreakerPolicy = Policy
                                .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                                .AdvancedCircuitBreakerAsync(0.25, TimeSpan.FromSeconds(60), 7, TimeSpan.FromSeconds(30), OnBreak, OnReset, OnHalfOpen);

            services.AddSingleton<AsyncRetryPolicy<HttpResponseMessage>>(retryPolicy);
            services.AddSingleton<AsyncCircuitBreakerPolicy<HttpResponseMessage>>(circuitBreakerPolicy);
            services.AddSingleton<AsyncCircuitBreakerPolicy<HttpResponseMessage>>(advancedCircuitBreakerPolicy);


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
