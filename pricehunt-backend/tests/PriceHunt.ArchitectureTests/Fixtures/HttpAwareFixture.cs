using Microsoft.AspNetCore.Http;

namespace PriceHunt.ArchitectureTests.Fixtures;

/// <summary>
/// A deliberate rule violation (a dependency on ASP.NET Core), used to prove the framework
/// rules really see external dependencies instead of passing vacuously.
/// </summary>
internal sealed class HttpAwareFixture
{
    public HttpContext? Context { get; init; }
}
