namespace PriceHunt.Application;

/// <summary>Exception filters for the few places that turn any failure into an outcome.</summary>
internal static class ExceptionFilters
{
    /// <summary>
    /// Gets a value indicating whether <paramref name="exception"/> means the process itself is in
    /// trouble; such exceptions are never turned into an outcome.
    /// </summary>
    /// <param name="exception">The exception to classify.</param>
    /// <returns><see langword="true"/> for out-of-memory and stack exhaustion.</returns>
    public static bool IsCritical(this Exception exception) =>
        exception is OutOfMemoryException or InsufficientExecutionStackException;
}
