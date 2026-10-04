namespace PriceHunt.Domain.Tests;

public sealed class LocationTests
{
    [Fact]
    public void Trims_surrounding_whitespace()
    {
        bool created = Location.TryCreate("  Haifa  ", out Location? location, out _);

        created.Should().BeTrue();
        location!.Value.Should().Be("Haifa");
    }

    [Fact]
    public void Normalizes_for_case_insensitive_comparison_beyond_ascii()
    {
        var location = Location.Create("zürich");

        location.Normalized.Should().Be("ZÜRICH");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Rejects_a_missing_location(string? value)
    {
        bool created = Location.TryCreate(value, out Location? location, out string? error);

        created.Should().BeFalse();
        location.Should().BeNull();
        error.Should().Be("A location is required.");
    }

    [Fact]
    public void Accepts_the_maximum_length()
    {
        bool created = Location.TryCreate(new string('a', Location.MaxLength), out _, out _);

        created.Should().BeTrue();
    }

    [Fact]
    public void Rejects_a_location_longer_than_the_maximum()
    {
        bool created = Location.TryCreate(new string('a', Location.MaxLength + 1), out _, out string? error);

        created.Should().BeFalse();
        error.Should().Be($"A location can have at most {Location.MaxLength} characters.");
    }

    [Fact]
    public void Create_throws_for_an_invalid_location()
    {
        Action create = () => Location.Create(" ");

        create.Should().Throw<ArgumentException>().WithMessage("A location is required.*");
    }
}

public sealed class RouteTests
{
    [Fact]
    public void Connects_two_different_locations()
    {
        bool created = Route.TryCreate(Location.Create("Haifa"), Location.Create("Rotterdam"), out Route? route, out _);

        created.Should().BeTrue();
        route!.Origin.Value.Should().Be("Haifa");
        route.Destination.Value.Should().Be("Rotterdam");
    }

    [Fact]
    public void Rejects_the_same_location_ignoring_case()
    {
        bool created = Route.TryCreate(Location.Create("Haifa"), Location.Create("HAIFA"), out Route? route, out string? error);

        created.Should().BeFalse();
        route.Should().BeNull();
        error.Should().Be("The destination must differ from the origin.");
    }

    [Fact]
    public void Create_throws_for_the_same_location()
    {
        Action create = () => Route.Create(Location.Create("Haifa"), Location.Create("haifa"));

        create.Should().Throw<ArgumentException>();
    }
}

public sealed class ShippingDateRangeTests
{
    [Fact]
    public void Accepts_a_single_day()
    {
        var day = new DateOnly(2026, 10, 1);

        bool created = ShippingDateRange.TryCreate(day, day, out ShippingDateRange? range, out _);

        created.Should().BeTrue();
        range!.From.Should().Be(day);
        range.To.Should().Be(day);
    }

    [Fact]
    public void Rejects_an_end_before_the_start()
    {
        bool created = ShippingDateRange.TryCreate(new DateOnly(2026, 10, 2), new DateOnly(2026, 10, 1), out _, out string? error);

        created.Should().BeFalse();
        error.Should().Be("The end date can't be before the start date.");
    }

    [Fact]
    public void Create_throws_for_an_inverted_range()
    {
        Action create = () => ShippingDateRange.Create(new DateOnly(2026, 10, 2), new DateOnly(2026, 10, 1));

        create.Should().Throw<ArgumentException>();
    }
}

public sealed class MoneyTests
{
    [Fact]
    public void Holds_an_amount_and_a_currency()
    {
        var money = Money.Create(1234.5m, "USD");

        money.Amount.Should().Be(1234.5m);
        money.Currency.Should().Be("USD");
    }

    [Fact]
    public void Converts_to_and_from_minor_units()
    {
        var money = Money.Create(1234.56m, "USD");

        money.ToMinorUnits().Should().Be(123456);
        Money.FromMinorUnits(123456, "USD").Should().Be(money);
    }

    [Fact]
    public void Accepts_zero()
    {
        Money.Create(0m, "USD").Amount.Should().Be(0m);
    }

    [Fact]
    public void Rejects_a_negative_amount()
    {
        Action create = () => Money.Create(-0.01m, "USD");

        create.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Rejects_more_than_two_decimals()
    {
        Action create = () => Money.Create(1.005m, "USD");

        create.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("usd")]
    [InlineData("US")]
    [InlineData("USDT")]
    [InlineData("")]
    [InlineData("USD\n")]
    public void Rejects_a_currency_that_is_not_an_iso_4217_code(string currency)
    {
        Action create = () => Money.Create(1m, currency);

        create.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Accepts_the_largest_amount_whose_minor_units_fit()
    {
        Money.Create(92_233_720_368_547_758.07m, "USD").ToMinorUnits().Should().Be(long.MaxValue);
    }

    [Fact]
    public void Rejects_an_amount_whose_minor_units_overflow()
    {
        Action create = () => Money.Create(92_233_720_368_547_758.08m, "USD");

        create.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Treats_equal_amounts_with_different_scale_as_equal()
    {
        Money.Create(10.5m, "USD").Should().Be(Money.Create(10.50m, "USD"));
    }
}

public sealed class SupplierIdTests
{
    [Theory]
    [InlineData("aurora-freightways")]
    [InlineData("g7")]
    public void Accepts_a_lowercase_slug(string value)
    {
        bool created = SupplierId.TryCreate(value, out SupplierId? id);

        created.Should().BeTrue();
        id!.Value.Should().Be(value);
        id.ToString().Should().Be(value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Aurora")]
    [InlineData("aurora freight")]
    [InlineData("-aurora")]
    [InlineData("aurora--freight")]
    [InlineData("aurora\n")]
    public void Rejects_anything_that_is_not_a_slug(string? value)
    {
        bool created = SupplierId.TryCreate(value, out SupplierId? id);

        created.Should().BeFalse();
        id.Should().BeNull();
    }

    [Fact]
    public void Rejects_an_overlong_id()
    {
        SupplierId.TryCreate(new string('a', SupplierId.MaxLength + 1), out _).Should().BeFalse();
    }

    [Fact]
    public void Create_throws_for_an_invalid_id()
    {
        Action create = () => SupplierId.Create("Not A Slug");

        create.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Compares_by_value()
    {
        SupplierId.Create("aurora").Should().Be(SupplierId.Create("aurora"));
    }
}
