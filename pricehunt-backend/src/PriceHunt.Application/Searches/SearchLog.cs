using Microsoft.Extensions.Logging;

namespace PriceHunt.Application.Searches;

/// <summary>Source-generated log messages for the search lifecycle.</summary>
internal static partial class SearchLog
{
    private static readonly Func<ILogger, Guid, IDisposable?> s_searchScope =
        LoggerMessage.DefineScope<Guid>("SearchId:{SearchId}");

    public static IDisposable? BeginSearchScope(ILogger logger, Guid searchId) => s_searchScope(logger, searchId);

    [LoggerMessage(EventId = 1000, Level = LogLevel.Information, Message = "Search started; suppliers: {SupplierCount}, deadline {Deadline:O}")]
    public static partial void SearchStarted(ILogger logger, int supplierCount, DateTime deadline);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "Quote from {SupplierId}: {Amount} {Currency} after {ElapsedMs} ms")]
    public static partial void QuoteReceived(ILogger logger, string supplierId, decimal amount, string currency, long elapsedMs);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Warning, Message = "Supplier {SupplierId} failed with {ErrorCode} after {ElapsedMs} ms")]
    public static partial void SupplierFailed(ILogger logger, string supplierId, string errorCode, long elapsedMs);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Error, Message = "Supplier {SupplierId} threw an unexpected exception")]
    public static partial void SupplierThrewUnexpectedly(ILogger logger, string supplierId, Exception exception);

    [LoggerMessage(EventId = 1004, Level = LogLevel.Information, Message = "Search completed: every supplier responded")]
    public static partial void SearchCompleted(ILogger logger);

    [LoggerMessage(EventId = 1005, Level = LogLevel.Information, Message = "Search reached its deadline; suppliers without a response: {NoResponseCount}")]
    public static partial void SearchTimedOut(ILogger logger, int noResponseCount);

    [LoggerMessage(EventId = 1006, Level = LogLevel.Information, Message = "Search cancelled by the client; cancelled supplier calls: {PendingCount}")]
    public static partial void SearchCancelled(ILogger logger, int pendingCount);

    [LoggerMessage(EventId = 1007, Level = LogLevel.Error, Message = "Search faulted")]
    public static partial void SearchFaulted(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 1008, Level = LogLevel.Error, Message = "The final state of the search could not be persisted")]
    public static partial void FinalisationFailed(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 1009, Level = LogLevel.Warning, Message = "Closed searches left running by an earlier shutdown as cancelled: {Count}")]
    public static partial void InterruptedSearchesRecovered(ILogger logger, int count);
}
