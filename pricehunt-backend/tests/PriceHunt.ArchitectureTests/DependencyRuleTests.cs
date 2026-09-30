using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnitV3;
using PriceHunt.ArchitectureTests.Fixtures;
using static ArchUnitNET.Fluent.ArchRuleDefinition;
using ReflectionAssembly = System.Reflection.Assembly;

namespace PriceHunt.ArchitectureTests;

/// <summary>
/// The dependency rule: Domain ← Application ← Infrastructure, with Api as the composition root
/// and the only layer that knows HTTP.
/// </summary>
public sealed class DependencyRuleTests
{
    private static readonly ReflectionAssembly s_domainAssembly = ReflectionAssembly.Load("PriceHunt.Domain");
    private static readonly ReflectionAssembly s_applicationAssembly = ReflectionAssembly.Load("PriceHunt.Application");
    private static readonly ReflectionAssembly s_infrastructureAssembly = ReflectionAssembly.Load("PriceHunt.Infrastructure");
    private static readonly ReflectionAssembly s_apiAssembly = ReflectionAssembly.Load("PriceHunt.Api");

    private static readonly Architecture s_architecture = new ArchLoader()
        .LoadAssemblies(s_domainAssembly, s_applicationAssembly, s_infrastructureAssembly, s_apiAssembly)
        .Build();

    private static readonly IObjectProvider<IType> s_applicationLayer =
        Types().That().ResideInAssembly(s_applicationAssembly).As("the Application layer");

    private static readonly IObjectProvider<IType> s_infrastructureLayer =
        Types().That().ResideInAssembly(s_infrastructureAssembly).As("the Infrastructure layer");

    private static readonly IObjectProvider<IType> s_apiLayer =
        Types().That().ResideInAssembly(s_apiAssembly).As("the Api layer");

    // External frameworks are only visible to a rule when referenced types are included.
    private static readonly IObjectProvider<IType> s_entityFrameworkCore =
        Types(true).That().ResideInNamespaceMatching(@"^Microsoft\.EntityFrameworkCore(\..+)?$").As("EF Core");

    private static readonly IObjectProvider<IType> s_aspNetCore =
        Types(true).That().ResideInNamespaceMatching(@"^Microsoft\.AspNetCore(\..+)?$").As("ASP.NET Core");

    private static readonly IObjectProvider<IType> s_microsoftExtensions =
        Types(true).That().ResideInNamespaceMatching(@"^Microsoft\.Extensions(\..+)?$").As("Microsoft.Extensions");

    [Fact]
    public void Domain_depends_on_no_other_layer_and_no_framework()
    {
        IArchRule rule = Types().That().ResideInAssembly(s_domainAssembly)
            .Should().NotDependOnAny(s_applicationLayer)
            .AndShould().NotDependOnAny(s_infrastructureLayer)
            .AndShould().NotDependOnAny(s_apiLayer)
            .AndShould().NotDependOnAny(s_entityFrameworkCore)
            .AndShould().NotDependOnAny(s_aspNetCore)
            .AndShould().NotDependOnAny(s_microsoftExtensions)
            .Because("the domain model references nothing");

        rule.Check(s_architecture);
    }

    [Fact]
    public void Application_depends_only_inward_and_knows_neither_ef_core_nor_http()
    {
        IArchRule rule = Types().That().ResideInAssembly(s_applicationAssembly)
            .Should().NotDependOnAny(s_infrastructureLayer)
            .AndShould().NotDependOnAny(s_apiLayer)
            .AndShould().NotDependOnAny(s_entityFrameworkCore)
            .AndShould().NotDependOnAny(s_aspNetCore)
            .Because("Application defines ports that Infrastructure implements");

        rule.Check(s_architecture);
    }

    [Fact]
    public void Infrastructure_depends_neither_on_the_api_nor_on_http()
    {
        IArchRule rule = Types().That().ResideInAssembly(s_infrastructureAssembly)
            .Should().NotDependOnAny(s_apiLayer)
            .AndShould().NotDependOnAny(s_aspNetCore)
            .Because("only the Api layer knows HTTP");

        rule.Check(s_architecture);
    }

    [Fact]
    public void Api_uses_infrastructure_only_from_the_composition_root()
    {
        IArchRule rule = Types().That().ResideInAssembly(s_apiAssembly)
            .And().DoNotHaveFullNameMatching(@"^Program([+/].*)?$")
            .Should().NotDependOnAny(s_infrastructureLayer)
            .Because("endpoints talk to Application ports; Infrastructure is referenced only to register services");

        rule.Check(s_architecture);
    }

    [Fact]
    public void Framework_rules_detect_a_forbidden_dependency()
    {
        Architecture fixtures = new ArchLoader().LoadAssemblies(typeof(HttpAwareFixture).Assembly).Build();
        IArchRule rule = Types().That().Are(typeof(HttpAwareFixture)).Should().NotDependOnAny(s_aspNetCore);

        Action check = () => rule.Check(fixtures);

        check.Should().Throw<FailedArchRuleException>();
    }
}
