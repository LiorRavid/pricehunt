using PriceHunt.Application.Suppliers;

namespace PriceHunt.Application.Tests;

public sealed class SupplierExceptionTests
{
    [Fact]
    public void Carries_a_code_and_a_user_facing_message()
    {
        var inner = new TimeoutException();

        var exception = new SupplierException("supplier_unavailable", "Try again later.", inner);

        exception.ErrorCode.Should().Be("supplier_unavailable");
        exception.Message.Should().Be("Try again later.");
        exception.InnerException.Should().BeSameAs(inner);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Requires_an_error_code(string errorCode)
    {
        Action create = () => _ = new SupplierException(errorCode, "Message.");

        create.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Requires_a_user_facing_message(string blank)
    {
        Action create = () => _ = new SupplierException("supplier_unavailable", blank);

        create.Should().Throw<ArgumentException>().WithParameterName("message");
    }
}
