using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;
using PriceHunt.Application.Searches;

namespace PriceHunt.Application.Tests;

public sealed class CompositionTests
{
    [Fact]
    public void Registers_the_use_cases_per_request_and_the_system_clock()
    {
        var services = new ServiceCollection();

        services.AddApplication();

        services.Should().ContainSingle(descriptor => descriptor.ServiceType == typeof(SearchPlanner) && descriptor.Lifetime == ServiceLifetime.Scoped);
        services.Should().ContainSingle(descriptor => descriptor.ServiceType == typeof(SearchOrchestrator) && descriptor.Lifetime == ServiceLifetime.Scoped);
        services.Should().ContainSingle(descriptor => descriptor.ServiceType == typeof(InterruptedSearchRecovery));
        services.Should().ContainSingle(descriptor => descriptor.ServiceType == typeof(TimeProvider))
            .Which.ImplementationInstance.Should().BeSameAs(TimeProvider.System);
    }

    [Fact]
    public void Keeps_a_clock_that_is_already_registered()
    {
        var services = new ServiceCollection();
        var clock = new FixedClock();
        services.AddSingleton<TimeProvider>(clock);

        services.AddApplication();

        services.Should().ContainSingle(descriptor => descriptor.ServiceType == typeof(TimeProvider))
            .Which.ImplementationInstance.Should().BeSameAs(clock);
    }

    [Fact]
    public void Default_search_options_allow_six_seconds()
    {
        var options = new SearchOptions();

        options.MaxDuration.Should().Be(TimeSpan.FromSeconds(6));
        Validator.TryValidateObject(options, new ValidationContext(options), null, validateAllProperties: true).Should().BeTrue();
    }

    [Fact]
    public void A_zero_search_duration_is_invalid()
    {
        var options = new SearchOptions { MaxDuration = TimeSpan.Zero };

        Validator.TryValidateObject(options, new ValidationContext(options), null, validateAllProperties: true).Should().BeFalse();
    }

    private sealed class FixedClock : TimeProvider;
}
