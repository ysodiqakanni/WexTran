using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using WexTran.Api.External;
using WexTran.Api.Repositories;

namespace WexTran.Tests.Integration;

public class CustomWebApplicationFactory : WebApplicationFactory<WexTran.Api.Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    public Mock<IExchangeRateService> ExchangeRateServiceMock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // Replace SQL Server with an isolated in-memory database
            var dbDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<WexTransactionDbContext>));
            if (dbDescriptor != null) services.Remove(dbDescriptor);

            services.AddDbContext<WexTransactionDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            // Replace the real exchange rate service with a controllable mock
            var exchangeDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IExchangeRateService));
            if (exchangeDescriptor != null) services.Remove(exchangeDescriptor);

            services.AddScoped<IExchangeRateService>(_ => ExchangeRateServiceMock.Object);
        });
    }
}
