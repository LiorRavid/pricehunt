using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PriceHunt.Application.History;

namespace PriceHunt.Api.Tests;

public sealed class UnexpectedErrorTests
{
    [Fact]
    public async Task An_unexpected_failure_becomes_a_500_problem_without_internal_details()
    {
        await using var factory = new FailingHistoryFactory();
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/api/history", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var problem = JsonDocument.Parse(body);
        problem.RootElement.GetProperty("title").GetString().Should().Be("An unexpected error occurred.");
        body.Should().NotContain("database is on fire", "internal details must not leak");
    }

    private sealed class FailingHistoryFactory : PriceHuntApiFactory
    {
        protected override void ConfigureTestServices(IServiceCollection services)
        {
            services.RemoveAll<IPriceHistoryQuery>();
            services.AddSingleton<IPriceHistoryQuery, FailingHistoryQuery>();
        }
    }

    private sealed class FailingHistoryQuery : IPriceHistoryQuery
    {
        public Task<PagedResult<PriceHistoryItem>> GetAsync(PriceHistoryFilter filter, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("The database is on fire.");
    }
}
