using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace PriceHunt.Infrastructure.Persistence;

/// <summary>
/// Stores UTC and reads values back as <see cref="DateTimeKind.Utc"/>. SQLite returns
/// <see cref="DateTimeKind.Unspecified"/>, which would serialize without a "Z" and make browsers
/// shift the time.
/// </summary>
internal sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    value => value.ToUniversalTime(),
    value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
