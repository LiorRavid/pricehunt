# ADR-001: Streaming transport — Server-Sent Events over `POST`

- Status: Accepted
- Date: 2026-09-30
- Requirements: SV1, SV2, SV5, CL1, CL5

## Context

A search fans out to up to seven suppliers and must show each quote the moment it arrives (SV1). The stream is one-way, from server to client, and bounded: at most 6 seconds (SV3). The client must be able to stop it, and the server must notice. Starting a new search or leaving the page must stop the supplier calls on the server (SV5, CL5).

A search is a single operation with structured input: two locations, two dates and a list of suppliers. Reviewers run the app through the Angular dev server's proxy over plain HTTP, with no extra setup (SUB1).

## Decision

Stream the search as **Server-Sent Events over `POST /api/searches`**.

- **Server:** the endpoint validates the JSON body *before* streaming, so a bad request is a normal `400` ProblemDetails response. It then returns .NET 10's `TypedResults.ServerSentEvents` with `SseItem<T>` items: an event type, an increasing event id, and a camelCase JSON payload. It passes `HttpContext.RequestAborted` to the search orchestrator, so a disconnect cancels the supplier calls.
- **Client:** `fetch` with a `ReadableStream` reads the body. A small in-house parser, written to the WHATWG event-stream rules, turns it into events, exposed as an RxJS `Observable`. Unsubscribing calls `AbortController.abort()`, which closes the request, which fires `RequestAborted` on the server.
- **One request equals one search.** The connection's lifetime *is* the search's lifetime, so cancellation needs no extra protocol.

Event contract:

| Event | When | Payload (besides `searchId`) |
| --- | --- | --- |
| `search-started` | Always first | suppliers (id, name), `startedAt`, `deadline`, `maxDurationMs` |
| `quote-received` | A supplier returned a price | `supplierId`, `price { amount, currency }`, `responseTimeMs`, `receivedAt` |
| `supplier-failed` | A supplier threw | `supplierId`, `errorCode`, `errorMessage`, `responseTimeMs`, `receivedAt` |
| `search-completed` | Always last, exactly once | `status` (`Completed`, `TimedOut` or `Faulted`), counts, `noResponseSupplierIds` |

A cancelled search emits nothing further, because nobody is listening any more. `Faulted` is an addition to the three statuses the assignment requires, and covers an unexpected server error after the stream has started.

## Alternatives considered

| Option | Why not |
| --- | --- |
| Native `EventSource` (GET) | Criteria would have to go in the query string, and custom headers aren't possible. Its automatic reconnect would silently **re-run a search** after a drop. It can't read a `400` ProblemDetails body. |
| SignalR | A two-way hub with connection state, reconnection, groups and an extra client library. That's all machinery this one-way, six-second stream doesn't need. |
| WebSockets | Needs a custom message protocol, loses HTTP status codes and ProblemDetails for validation, and needs upgrade support in every proxy. |
| NDJSON over `fetch` | Works, but has no standard framing for event names or ids and no standard parser. SSE gets both, plus a BCL parser (`SseParser`) for server-side tests. |
| Long polling | Several requests per search, server-side state between them, and extra latency on every result. |
| gRPC-Web | Needs a proxy (Envoy or middleware), a protobuf toolchain and generated clients. That's a heavy dependency for a take-home. |
| `eventsource-parser` (MIT) instead of an in-house parser | A fine library, but the parser is about 60 lines of pure code that's easy to test exhaustively (chunk splits, CRLF, multi-line data, comments). Owning it removes a dependency. |

## Consequences

- A validation error is a plain `400` response. Once the stream has started the status code can't change, so later failures travel as a terminal `search-completed` with status `Faulted`.
- There is no automatic resume. If the connection drops before the terminal event, the client reports "connection lost" and the user can search again. That's acceptable for a six-second search.
- The browser allows about six HTTP/1.1 connections per origin. The UI keeps at most one search stream open per tab, and the previous one is aborted first.
- The Angular dev-server proxy must stream events one at a time and pass client aborts through. **Verified in Phase 5**, with a `fetch` through `http://localhost:4200/api/searches` run in Chrome via DevTools MCP:
  - Events arrived at 284 ms, 1.94 s, 2.12 s, 2.35 s, 3.56 s, 3.90 s and 4.18 s, and the search completed at 6.10 s.
  - Aborting right after `search-started` made the API log "Search cancelled by the client; cancelled supplier calls: 7" 11 ms later, and the database recorded the search and all 7 responses as `Cancelled`.
  - No CORS fallback is needed.
