using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Tournament.Api.Data;

namespace Tournament.Api.UnitTests.Integration;

public class AppStartupTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AppStartupTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                // EF Core 8+ stores provider config in IDbContextOptionsConfiguration<T>, not in
                // DbContextOptions<T> directly — remove everything related to TournamentDbContext.
                var toRemove = services
                    .Where(d => d.ServiceType == typeof(TournamentDbContext)
                             || d.ServiceType == typeof(DbContextOptions<TournamentDbContext>)
                             || d.ServiceType == typeof(DbContextOptions)
                             || (d.ServiceType.IsGenericType
                                 && d.ServiceType.GetGenericTypeDefinition() == typeof(IDbContextOptionsConfiguration<>)
                                 && d.ServiceType.GenericTypeArguments[0] == typeof(TournamentDbContext)))
                    .ToList();
                foreach (var d in toRemove) services.Remove(d);

                services.AddDbContext<TournamentDbContext>(options =>
                    options.UseInMemoryDatabase("IntegrationTestDb"));
            }));
    }

    [Fact]
    public async Task App_StartsAndRespondsToHealthCheck()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/tournaments");

        ((int)response.StatusCode).Should().BeOneOf(200, 404);
    }

    [Fact]
    public void App_DependencyInjection_ResolvesAllServices()
    {
        var client = _factory.CreateClient();
        client.Should().NotBeNull();
    }
}
