namespace PriceHunt.Domain.Tests;

public sealed class SupplierResponseTests
{
    private static readonly SupplierId s_supplier = SupplierId.Create("aurora");
    private static readonly DateTime s_at = new(2026, 9, 30, 10, 0, 1, DateTimeKind.Utc);

    [Fact]
    public void Restores_a_success_with_its_price()
    {
        var id = Guid.CreateVersion7();

        var response = SupplierResponse.Restore(id, s_supplier, ResponseOutcome.Succeeded, Money.Create(12.5m, "USD"), TimeSpan.FromMilliseconds(1500), s_at, null, null);

        response.Id.Should().Be(id);
        response.Price.Should().Be(Money.Create(12.5m, "USD"));
        response.ResponseTime.Should().Be(TimeSpan.FromMilliseconds(1500));
    }

    [Fact]
    public void Restores_a_failure_with_its_error()
    {
        var response = SupplierResponse.Restore(Guid.CreateVersion7(), s_supplier, ResponseOutcome.Failed, null, TimeSpan.FromSeconds(1), s_at, "boom", "Boom.");

        response.ErrorCode.Should().Be("boom");
        response.ErrorMessage.Should().Be("Boom.");
    }

    [Theory]
    [InlineData(ResponseOutcome.TimedOut)]
    [InlineData(ResponseOutcome.Cancelled)]
    public void Restores_a_missing_response_without_price_or_error(ResponseOutcome outcome)
    {
        var response = SupplierResponse.Restore(Guid.CreateVersion7(), s_supplier, outcome, null, TimeSpan.FromSeconds(6), s_at, null, null);

        response.Outcome.Should().Be(outcome);
    }

    [Fact]
    public void Rejects_a_success_without_a_price()
    {
        Action restore = () => SupplierResponse.Restore(Guid.CreateVersion7(), s_supplier, ResponseOutcome.Succeeded, null, TimeSpan.Zero, s_at, null, null);

        restore.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Rejects_a_failure_with_a_price()
    {
        Action restore = () => SupplierResponse.Restore(Guid.CreateVersion7(), s_supplier, ResponseOutcome.Failed, Money.Create(1m, "USD"), TimeSpan.Zero, s_at, "boom", "Boom.");

        restore.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Rejects_a_timeout_with_an_error()
    {
        Action restore = () => SupplierResponse.Restore(Guid.CreateVersion7(), s_supplier, ResponseOutcome.TimedOut, null, TimeSpan.Zero, s_at, "boom", "Boom.");

        restore.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Rejects_a_local_timestamp()
    {
        Action restore = () => SupplierResponse.Restore(Guid.CreateVersion7(), s_supplier, ResponseOutcome.Cancelled, null, TimeSpan.Zero, DateTime.SpecifyKind(s_at, DateTimeKind.Unspecified), null, null);

        restore.Should().Throw<ArgumentException>();
    }
}
