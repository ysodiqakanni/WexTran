
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;
using System;
using WexTran.Api.External;
using WexTran.Api.Repositories;
using WexTran.Api.Services;

namespace WexTran.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddDbContext<WexTransactionDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.Configure<TreasuryApiOptions>(builder.Configuration.GetSection("TreasuryApi"));

            builder.Services.AddHttpClient<TreasuryExchangeRateService>(client =>
            {
                var baseUrl = builder.Configuration["TreasuryApi:BaseUrl"]
                    ?? throw new InvalidOperationException("TreasuryApi:BaseUrl is not configured.");
                client.BaseAddress = new Uri(baseUrl);
            })
            .AddResilienceHandler("treasury-api", (pipeline, context) =>
            {
                var options = context.ServiceProvider
                    .GetRequiredService<IOptions<TreasuryApiOptions>>().Value;

                pipeline.AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = options.MaxRetryAttempts,
                    Delay = TimeSpan.FromSeconds(1),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true
                });

                pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
                {
                    // Open the circuit after 50% of requests fail within the sampling window
                    FailureRatio = 0.5,
                    // Minimum requests before the failure ratio is evaluated
                    MinimumThroughput = 5,
                    // Window over which failures are tracked
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    // How long the circuit stays open before allowing a test request
                    BreakDuration = TimeSpan.FromSeconds(15)
                });

                pipeline.AddTimeout(new HttpTimeoutStrategyOptions
                {
                    // Per-request timeout (applies to each attempt, including retries)
                    Timeout = TimeSpan.FromSeconds(10)
                });
            });

            builder.Services.AddMemoryCache();

            builder.Services.AddScoped<IExchangeRateService>(sp =>
                new CachedExchangeRateService(
                    sp.GetRequiredService<TreasuryExchangeRateService>(),
                    sp.GetRequiredService<IMemoryCache>()));

            builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
            builder.Services.AddScoped<ITransactionService, TransactionService>();

            builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
            builder.Services.AddProblemDetails();
            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseExceptionHandler();
            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
