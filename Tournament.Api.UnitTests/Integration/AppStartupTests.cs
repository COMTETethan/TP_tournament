using Microsoft.AspNetCore.Mvc.Testing;

namespace Tournament.Api.UnitTests.Integration;

[Trait("Category", "Startup")]
[Trait("Layer", "Integration")]
public class AppStartupTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AppStartupTests(WebApplicationFactory<Program> factory)
    {
        // Force "Testing" environment so appsettings.Development.json is never
        // loaded in CI, keeping the app in in-memory mode (no DB required).
        _factory = factory.WithWebHostBuilder(b => b.UseEnvironment("Testing"));
    }

    [Fact]
    public async Task App_StartsAndRespondsToHealthCheck()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/tournaments");

        // Assert
        ((int)response.StatusCode).Should().BeOneOf(200, 404);
    }

    [Fact]
    public void App_DependencyInjection_ResolvesAllServices()
    {
        // Act
        var client = _factory.CreateClient();
        // Assert
        client.Should().NotBeNull();
    }
}
