using System.Net;

namespace PriceHunt.Api.Tests;

public sealed class HealthAndErrorTests(PriceHuntApiFactory factory) : IClassFixture<PriceHuntApiFactory>
{
    [Fact]
    public async Task Health_endpoint_reports_healthy()
    {
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/health", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.Should().Be("Healthy");
    }

    [Fact]
    public async Task Unknown_route_returns_problem_details()
    {
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/no-such-route", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task OpenApi_document_is_generated_in_development()
    {
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        string document = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        document.Should().Contain("\"openapi\"");
    }
}
