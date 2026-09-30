using PriceHunt.Domain;

namespace PriceHunt.Api.Contracts;

/// <summary>A price on the wire: <c>{ "amount": 1234.56, "currency": "USD" }</c>.</summary>
internal sealed record MoneyDto(decimal Amount, string Currency)
{
    public static MoneyDto From(Money money) => new(money.Amount, money.Currency);
}
