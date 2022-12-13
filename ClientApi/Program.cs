using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();



#region Policy

#region MyRegion
//HttpResponseMessage result = await Policy
//  .Handle<HttpRequestException>()
//  .OrResult<HttpResponseMessage>(r => httpStatusCodesWorthRetrying.Contains(r.StatusCode));
 
//AsyncRetryPolicy<HttpResponseMessage> retryPolicy =
//             Policy.HandleResult<HttpResponseMessage>(message => (int)message.StatusCode == 429 || (int)message.StatusCode >= 500)
//                   .RetryAsync(2, retryAttempt =>
//                   {
//                       Console.WriteLine($"Retrying because of transient error. Attemp {retryAttempt}");
//                       return TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
//                   });
 
//var httpRetryPolicy = Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
//                    .WaitAndRetryAsync(3, retryAttempt =>
//                    {
//                        Console.WriteLine("Attempt... " + retryAttempt);
//                        var timeToRetry = TimeSpan.FromSeconds(2);
//                        Console.WriteLine($"Waiting {timeToRetry.TotalSeconds} seconds");
//                        return TimeSpan.FromSeconds(30);
//                    }); 
#endregion

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

#endregion


builder.Services.AddHttpClient();
builder.Services.AddSingleton<AsyncRetryPolicy<HttpResponseMessage>>(retryPolicy);
builder.Services.AddSingleton<AsyncCircuitBreakerPolicy<HttpResponseMessage>>(circuitBreakerPolicy);
builder.Services.AddSingleton<AsyncCircuitBreakerPolicy<HttpResponseMessage>>(circuitBreakerPolicy);



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();



void OnHalfOpen()
{
    Console.WriteLine("\t\t\t\t\tConnection half open");
}

void OnReset(Context context)
{
    Console.WriteLine("\t\t\t\t\tConnection reset");
}

void OnBreak(DelegateResult<HttpResponseMessage> delegateResult, TimeSpan timeSpan, Context context)
{
    Console.WriteLine($"\t\t\t\t\tConnection break: {delegateResult.Result.StatusCode}");
}