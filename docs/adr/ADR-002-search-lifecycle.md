# ADR-002: Search lifecycle — deadline, cancellation and finalisation

- Status: Accepted
- Date: 2026-09-30
- Requirements: SV1, SV3, SV4, SV5, DB1, DB2

## Context

Each search calls the selected suppliers at the same time and must meet these rules:

- Stream each result as it arrives (SV1).
- Stop after 6 seconds with the results so far, telling the client it has ended (SV3).
- Keep one supplier's failure from affecting the others (SV4).
- Stop all work when the client goes away (SV5).
- Persist the search, every response and the final status (DB1, DB2).

Four implementation traps shape the design:

1. C# can't `yield return` inside a `try` block that has a `catch`.
2. The deadline and the caller's cancellation both raise `OperationCanceledException`, so the exception type alone can't tell them apart.
3. When the client disconnects, the request token is already cancelled, so persistence that uses it would fail.
4. A scoped `DbContext` must never be shared across concurrent tasks.

## Decision

`SearchOrchestrator.RunAsync(plan, cancellationToken)` returns `IAsyncEnumerable<SearchEvent>`. It's a thin iterator over an internal `SearchRun` object. `SearchRun`'s methods are ordinary `async` methods, so each can use `try`/`catch` freely.

1. **Start.** Create the `Search` aggregate in the `Running` state with a time-ordered id (Guid v7 from `TimeProvider`), and persist it.
2. **Arm the deadline before anything is emitted.** Create `new CancellationTokenSource(MaxDuration, timeProvider)` and link it to the caller's token. Because it exists before the first event, tests that use a fake clock can advance time as soon as they see `search-started`.
3. **Fan out.** Start every selected supplier call concurrently. Each call runs inside a wrapper that never throws:
   - The wrapper times the call with `TimeProvider.GetTimestamp()`.
   - It awaits `GetQuoteAsync(request, linkedToken).WaitAsync(linkedToken)`, so the deadline holds even if a supplier ignores its token.
   - It maps the result to an outcome. Our own cancellation → *interrupted*. A `SupplierException` → *failed* with that exception's error code. Any other exception → *failed* with `unexpected_error`, and it is logged. One supplier's exception can therefore never reach the others (SV4).
4. **Emit `search-started`** with the suppliers, the start time and the deadline.
5. **Stream in completion order.** `Task.WhenEach` yields calls as they finish. For each success or failure:
   - Record it in the aggregate, which accepts only selected suppliers and each supplier only once.
   - Persist it.
   - Then emit `quote-received` or `supplier-failed`. Persisting first means anything the client saw is in the history.
   - Interrupted calls produce no event.
6. **Finalise once.** When every call has settled, the status depends on **which token fired**, not on the exception:
   - Caller's token cancelled → `Cancelled`, and nothing more is emitted, because nobody is listening.
   - Deadline fired → `TimedOut`.
   - Otherwise → `Completed`.

   Pending suppliers are recorded as `TimedOut` or `Cancelled` responses. The status, completion time and those responses are saved in one transaction. The terminal `search-completed` event is emitted **exactly once**, unless the search was cancelled.
7. **Faults.** An unexpected error mid-stream, such as a database failure, is logged. `Faulted` is persisted on a best-effort basis, and a terminal `search-completed` event with status `Faulted` is emitted.
8. **Early stop.** If the consumer stops enumerating, `SearchRun.DisposeAsync` cancels outstanding calls. It then finalises the search as `Cancelled` if that hasn't already happened. This covers a disconnect while a result is being written.

Persistence after the initial insert **doesn't use the request token**. Each write gets its own short timeout (`Search:PersistenceTimeout`, 5 s). That way a response that already arrived, and the final status, are recorded even when the client has gone.

The repository uses `IDbContextFactory` with one short-lived context per write. Only the single consumer loop touches the database; the supplier tasks never do.

A `SearchId` logging scope is opened inside every `SearchRun` call, because a scope opened inside an async iterator doesn't reliably survive a `yield`. Supplier outcomes, deadlines and cancellations are logged through source-generated `LoggerMessage` methods.

At startup, any search left `Running` by a crash is closed as `Cancelled`, using the same domain rule.

## Alternatives considered

- **`Channel<T>` with producer tasks.** This also avoids the `yield`-in-`catch` restriction, but it adds a second concurrency primitive. On .NET 9 and later, `Task.WhenEach` gives completion order directly.
- **`Task.WhenAll` and then emitting everything.** This breaks SV1, because it waits for every supplier.
- **Deciding the outcome by exception type.** The deadline and the caller raise the same exception type, so the outcome would be wrong half the time.
- **Persisting with the request token.** A disconnect in the middle of a write would lose a response that had already arrived, and the final status with it.
- **One scoped `DbContext` for the whole stream.** It works because only one loop writes, but the change tracker would grow with every write, and its lifetime would depend on the response pipeline. Short-lived contexts from the factory avoid both.

## Consequences

- The search never runs past `MaxDuration`, even with a supplier that ignores cancellation. Such a supplier's task finishes in the background and is never observed.
- Every selected supplier ends up with exactly one recorded outcome. The domain enforces this, and so does a unique index on (search id, supplier id).
- Tests are deterministic, using `FakeTimeProvider` for time and `TaskCompletionSource` fakes that let each test decide when every supplier completes.
