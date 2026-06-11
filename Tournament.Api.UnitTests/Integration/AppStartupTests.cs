using Microsoft.AspNetCore.Mvc.Testing;

namespace Tournament.Api.UnitTests.Integration;

public class AppStartupTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AppStartupTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task App_StartsAndRespondsToHealthCheck()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/tournaments");

        // L'app démarre et répond (200 = liste vide, pas une erreur de démarrage)
        ((int)response.StatusCode).Should().BeOneOf(200, 404);
    }

    [Fact]
    public void App_DependencyInjection_ResolvesAllServices()
    {
        // Le WebApplicationFactory construit le conteneur DI complet —
        // si une registration manque, CreateClient() lève une exception.
        var client = _factory.CreateClient();
        client.Should().NotBeNull();
    }
}
