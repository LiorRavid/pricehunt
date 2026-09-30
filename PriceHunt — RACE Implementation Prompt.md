# PriceHunt — RACE Implementation Prompt

Sep 30, 2026 · @Lior

## R — Role

You are a principal full-stack engineer delivering a take-home assignment that SHIP4WD's senior engineers will review line by line.

Your expertise:

- .NET and ASP.NET Core minimal APIs: async and concurrent code, cooperative cancellation, `TimeProvider`, EF Core, SQLite.
- Angular (standalone components, signals, RxJS interop, modern control flow, zoneless change detection) and Tailwind CSS.
- Clean Architecture, SOLID and pragmatic domain modelling.
- Real-time HTTP streaming with Server-Sent Events, time-boxed fan-out, failure isolation.
- Test-driven development and automation: xUnit, `WebApplicationFactory`, Vitest, Playwright Test.
- Browser verification and diagnostics with Playwright MCP and Chrome DevTools MCP.

Your stance:

- Work autonomously in a pre-configured environment. Decide, record the reason, keep going. Stop to ask only before an irreversible or destructive action.
- Correctness first, then clarity, then polish. Small focused units, no speculative abstractions, no dead code.
- Claim nothing you haven't run and observed. Never weaken, skip or delete a test to get a green build.

## A — Action

Build **PriceHunt** end to end: a .NET API and an Angular app. Together they stream shipping quotes from seven simulated suppliers, persist every search and response, and serve a filterable, sortable, paged price history. The result must meet every requirement in C1 and the Definition of Done in E2.

Operating rules for every phase:

1. Read all of C and E before Phase 0. Cite requirement IDs from C1 (e.g. `SV3`) in tests, commits and the README.
2. Work through the phases in order. Each ends with a **gate**; start the next phase only when the gate is green. Fix causes, not tests.
3. Tests are part of the work. Write domain, orchestration, streaming, timeout and cancellation logic test-first. One behaviour per test, Arrange-Act-Assert, names that read as behaviour.
4. Keep tests deterministic: `FakeTimeProvider` for time, a seeded source for randomness, `TaskCompletionSource` for supplier completion. No sleeps or fixed waits in any test; use awaitable signals and Playwright's auto-waiting assertions.
5. Verify every UI change in a real browser with both MCP servers (C2) before its gate.
6. Commit at each gate with Conventional Commits (`feat:`, `fix:`, `test:`, `refactor:`, `docs:`, `chore:`), then push directly to `main` on `origin`. Never force-push or rewrite pushed history. Never commit build output, `node_modules`, the database file or secrets.
7. Keep a running decision log (decision, alternatives, reason) and an AI-usage log. Both feed the README in Phase 9.
8. When something is ambiguous, pick the most reasonable reading, add it to the README's assumptions, and continue.
9. Run the API and the dev server in the background, wait for readiness (`/health`, the app URL), and stop them when done, child `dotnet` and `node` processes included. Leave no orphaned processes.

### Phase 0 — Discovery, decisions and repository skeleton

Goal: a committed skeleton, the key decisions written down, and a traceability matrix.

- Inspect the environment: `dotnet --list-sdks`, `node --version`, `npm --version`, `ng version`, `git --version`, and the PowerShell versions available (`pwsh --version`, plus Windows PowerShell 5.1 on Windows). Confirm both MCP servers respond (open `about:blank`, list pages).
- Choose and record versions: the newest installed .NET LTS SDK (≥ 8; expect .NET 10) and the newest installed Angular CLI (≥ 17; expect v22). With older versions, use the fallbacks in C4; install nothing.
- Clone the private repository `https://github.com/LiorRavid/pricehunt.git` and work inside the clone; if the current directory already is that clone, use it. Git access is configured: confirm it with `git ls-remote`, and if authentication fails, stop and report rather than touching credentials.
- Keep any files GitHub created (README, licence, `.gitignore`) and extend them. Create the layout from E1: `pricehunt-backend/`, `pricehunt-frontend/`, `docs/`, plus root `README.md`, `.gitignore`, `.editorconfig` and `.gitattributes` (`* text=auto eol=lf`, and `*.ps1 text eol=crlf` for Windows PowerShell).
- Write short ADRs in `docs/adr/` (context, decision, alternatives, consequences):
  - ADR-001 Streaming transport: SSE over POST vs SignalR, WebSockets, NDJSON, long polling, gRPC-Web.
  - ADR-002 Search lifecycle: deadline, cancellation and finalisation.
  - ADR-003 Persistence model and SQLite type mapping.
  - ADR-004 Frontend state, stale-event guard and flicker-free ordering.
- Create `docs/requirements-traceability.md`: requirement ID → implementation → proving tests → status. Update it in every phase.

**Gate:** skeleton and ADRs committed and pushed; the matrix lists every ID in C1.

### Phase 1 — Backend foundation and quality gates

Goal: an empty but strict .NET solution whose build fails on any style, analyzer or architecture violation.

- Solution in `pricehunt-backend/` with `PriceHunt.Domain`, `PriceHunt.Application`, `PriceHunt.Infrastructure`, `PriceHunt.Api` and the test projects listed in E1 (xUnit v3).
- `Directory.Build.props`: nullable enabled, implicit usings, `TreatWarningsAsErrors`, `AnalysisLevel` set to `latest-recommended`, `EnforceCodeStyleInBuild`. `Directory.Packages.props` for central package versions.
- `.editorconfig` encoding the C# conventions in C3 (naming, `var`, namespaces, braces), so `dotnet format --verify-no-changes` enforces them.
- Architecture tests (ArchUnitNET or NetArchTest) that fail when the dependency rule in C3 breaks.
- API skeleton: `/health`, ProblemDetails (RFC 9457) via `IExceptionHandler`, an OpenAPI document in Development, structured logging.
- Reviewer-friendly hosting: a fixed HTTP port in `launchSettings.json` (e.g. `http://localhost:5080`) and no HTTPS redirection in Development, so reviewers need no dev-certificate step.
- `pricehunt-backend/run.ps1` is the single command that runs the backend (behaviour in E1).

**Gate:** zero build warnings; `dotnet format --verify-no-changes` clean; architecture tests and a `/health` smoke test pass; `run.ps1` starts the API and `/health` answers.

### Phase 2 — Domain and application core: the search engine

Goal: the complete search behaviour (streaming, deadline, cancellation, isolation) proven by tests, with no infrastructure.

**Domain** (no framework references):

- Value objects: location (trimmed, non-empty, length-limited), route (origin ≠ destination), shipping date range (from ≤ to), money (amount ≥ 0 plus ISO-4217 currency), supplier id.
- `Search` aggregate with lifecycle `Running → Completed | TimedOut | Cancelled`. Terminal states are final and reached once.
- `SupplierResponse` with outcome `Succeeded | Failed | TimedOut | Cancelled`, price (success only), response time, timestamp, and error code and message (failure only).

**Application:**

- Ports: `IShippingSupplier` (the common supplier interface: id, display name, `GetQuoteAsync(request, cancellationToken)`), a supplier catalogue, a search repository (write side) and a price-history query (read side). Use the BCL `TimeProvider`.
- `SearchOptions.MaxDuration` of 6 seconds, bound from configuration and validated at startup.

**The streaming orchestrator** returns `IAsyncEnumerable<SearchEvent>`:

1. Validate criteria and resolve suppliers: a missing or empty selection means all seven; an unknown id is a validation error.
2. Persist the search as `Running`, then emit `SearchStarted` (search id, suppliers, deadline) first.
3. Start all selected supplier calls concurrently. Each is isolated, with its own try/catch and its own timing via `TimeProvider.GetTimestamp()`. An exception becomes a `Failed` result and never affects other calls (SV4).
4. Emit each result the moment it completes, in completion order, and persist it, success or failure.
5. Enforce the budget with a `TimeProvider`-aware `CancellationTokenSource` linked to the caller's token. At the deadline: cancel outstanding calls, record them as `TimedOut`, mark the search `TimedOut`, and emit `SearchCompleted` with that status (SV3).
6. When every selected supplier has finished, mark the search `Completed` and emit `SearchCompleted`.
7. On caller cancellation (disconnect or new search): cancel outstanding calls, record them as `Cancelled`, mark the search `Cancelled`, and emit nothing further (SV5).
8. Emit exactly one terminal event and nothing after it. Finalisation writes use `CancellationToken.None` with a short timeout, because the request token is already cancelled.

**Tests** (Domain and Application test projects), at minimum:

- Value-object invariants and every legal and illegal state transition.
- Results stream in completion order; the first result is observable while other suppliers are still pending.
- A throwing supplier yields a `Failed` event while the others still succeed.
- Advancing fake time to 6 s yields `TimedOut` with exactly the results collected so far; the never-responding fake observes cancellation.
- Cancelling mid-flight cancels every outstanding supplier token, ends `Cancelled`, and still persists the finalisation.
- Only selected suppliers are called; no selection means all; unknown ids are rejected.
- Each outcome is persisted exactly once, and the terminal event is emitted exactly once.

**Gate:** tests green; line coverage ≥ 90 % on Domain and Application; ADR-002 matches the code.

### Phase 3 — Infrastructure: simulated suppliers and persistence

Goal: seven realistic simulated suppliers, and a SQLite database that creates itself and answers every history query on the server.

**Simulated suppliers** (S1–S4): seven implementations of `IShippingSupplier` with invented company names (no real brands), configured in `appsettings.json`.

- Each waits a uniform random 0.5–5 s with `Task.Delay(delay, timeProvider, cancellationToken)`, then returns a plausible random price (per-supplier range, two decimals, USD).
- One fails with probability 0.3 after its delay, throwing a specific supplier exception.
- One never responds: it awaits an infinite delay that still honours cancellation, so nothing leaks.
- Randomness comes from an injectable source, seedable through configuration (e.g. `Simulation__Seed`) for reproducible demos and E2E runs.

**EF Core + SQLite** (DB1–DB3):

- A `DbContext` with one `IEntityTypeConfiguration` per entity; the Domain carries no EF attributes.
- Tables for searches, the suppliers selected per search, and supplier responses (shape in E1). Enums stored as strings, foreign keys, and indexes on every history filter and sort column.
- SQLite-safe types: UTC `DateTime` read back as `DateTimeKind.Utc`, money as integer minor units plus a currency code, response time as integer milliseconds (C4 explains why).
- Committed migrations applied with `Database.MigrateAsync()` at startup, creating the file and schema on first run. WAL journal mode. A deterministic database path (not relative to the shell's working directory), git-ignored.
- At startup, searches left `Running` by a crash are closed as `Cancelled`; the README says so.

**History query** (read side, `AsNoTracking` projections):

- Filters: quote-timestamp range, supplier set, origin and destination (case-insensitive), and an optional outcome filter (successful quotes by default).
- Whitelisted sorting on every column with a deterministic tie-breaker; offset paging that returns items, page, page size and total count.

**Tests** against real SQLite (an open in-memory connection or a temp file), never the EF InMemory provider:

- The delay generator always stays within \[0.5 s, 5 s\]; under fake time, a supplier call completes exactly at its drawn delay.
- The flaky supplier's failure decision yields 30 % ± 2 % over 10 000 seeded draws. Test the decision directly, not 10 000 delayed calls.
- The unresponsive supplier completes only through cancellation, and cancellation mid-delay stops any supplier promptly.
- Migrations apply to an empty database, and `HasPendingModelChanges()` is false.
- Round-trips keep UTC kind, money and enums intact.
- Filters at range boundaries (half-open), one, several and all suppliers, location matching, every sort column in both directions, and paging edges (first, last, beyond last, size cap).

**Gate:** tests green; starting the API with `run.ps1` on a fresh checkout creates the database automatically and logs the applied migrations.

### Phase 4 — API: streaming search, suppliers and history endpoints

Goal: three endpoints with a stable contract, proven by integration tests that read the stream incrementally.

- `GET /api/suppliers` returns the catalogue (id, name), so the UI never hard-codes suppliers.
- `POST /api/searches` returns `text/event-stream` (SV1):
  - Validate the body **before** streaming and return `400` ProblemDetails with field errors (built-in minimal-API validation on .NET 10, or FluentValidation). Once the stream starts, the status code can't change.
  - Events are JSON (camelCase, enums as strings), each with the `searchId` and an increasing SSE `id`: `search-started`, `quote-received`, `supplier-failed`, and the terminal `search-completed` (final status, counts, and the ids of suppliers that didn't respond).
  - An unexpected fault mid-stream ends with a terminal event of status `Faulted`, logged and persisted. Document it as an addition to the three required statuses.
  - Use [`TypedResults.ServerSentEvents`](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/responses?view=aspnetcore-10.0) with `SseItem<T>` on .NET 10; on older SDKs, a small tested writer that flushes per event. No buffering or compression on this endpoint.
  - Pass `HttpContext.RequestAborted` to the orchestrator so a disconnect cancels the supplier calls (SV5).
- `GET /api/history` (H1, H2) returns a paged result; invalid input returns `400`. Parameters:
  - `startDate`, `endDate`: ISO-8601 instants, a half-open interval on the quote timestamp.
  - `suppliers`: repeatable; empty means all.
  - `origin`, `destination`: case-insensitive.
  - `sortBy` (`date`, `route`, `supplier`, `price`, `responseTime`) and `sortDirection`.
  - `page`, a capped `pageSize`, and `includeFailures`.
- Logging scopes carry the search id. Supplier outcomes, deadlines and cancellations use source-generated `LoggerMessage` methods.

**Tests** (`WebApplicationFactory` with fake suppliers, `FakeTimeProvider`, and an isolated SQLite database per test):

- The response is `text/event-stream`; `search-started` comes first and exactly one terminal event comes last.
- True streaming: reading incrementally, the first `quote-received` arrives while another fake supplier is still pending.
- Advancing fake time to 6 s produces a terminal `TimedOut`, closes the stream, and leaves `TimedOut` and no-response rows in the database.
- Aborting the request mid-stream cancels the fake suppliers and leaves the search `Cancelled`. If `TestServer` doesn't surface the abort faithfully, run this test on real Kestrel.
- Validation returns `400` ProblemDetails. History tests cover list binding (`?suppliers=a&suppliers=b`), filters, sorting, paging and invalid parameters.

Manual check: start the API with `run.ps1` and watch events arrive one at a time with `curl -N -X POST …` (`curl.exe` in Windows PowerShell).

**Gate:** all backend suites green; the OpenAPI document generates; the live stream has been observed.

### Phase 5 — Frontend foundation

Goal: a strict Angular workspace with Tailwind, enforced layering and a tested SSE client.

- Create the workspace in `pricehunt-frontend/` with the installed CLI: standalone, strict TypeScript, routing, no SSR, CSS styles. Keep the CLI defaults (zoneless change detection, Vitest) unless an ADR says otherwise.
- Add Tailwind CSS v4 with `ng add tailwindcss`, or the manual `.postcssrc.json` + `@import 'tailwindcss';` setup from the [Angular Tailwind guide](https://angular.dev/guide/tailwind). Put design tokens in `@theme`.
- ESLint (angular-eslint) and Prettier with `prettier-plugin-tailwindcss`; import-boundary lint rules for the layering in C3.
- Feature-first structure (C3): `core/` (shell, error handling, API base, SSE client), `shared/ui/`, `features/search/`, `features/history/`. Lazy routes `/search` (default) and `/history` behind a top navigation bar.
- `proxy.conf.json`, wired in `angular.json`, forwards `/api` to the backend port, so no CORS or environment settings are needed.
- `pricehunt-frontend/run.ps1` is the single command that runs the frontend, installing dependencies on a fresh checkout (behaviour in E1). `engines` and `.nvmrc` pin the Node version.
- **SSE client** in `core/`: `fetch` + `ReadableStream` + a small pure SSE parser (or a maintained MIT parser such as `eventsource-parser`, justified in ADR-001), exposed as an `Observable`. Unsubscribing calls `AbortController.abort()`. Non-2xx responses surface ProblemDetails; a stream that ends without a terminal event surfaces a "connection lost" error.
- Typed DTOs and mappers live in each feature's data-access layer. Components never touch `HttpClient` or `fetch`.

**Tests** (Vitest):

- The parser handles events split across chunks at any position, CRLF and LF, multi-line `data`, comments, `id` and `event` fields, and a trailing partial event.
- The client aborts on unsubscribe, maps HTTP errors, and detects a premature end.
- API services are covered with `HttpTestingController`.

**Gate:** `npm run lint`, `npm test` and `npm run build` green; on a fresh checkout, `run.ps1` installs dependencies and serves the shell with a clean console, checked through MCP.

### Phase 6 — Live search screen

Goal: results stream in, stay sorted without flicker, show clear progress, and never mix with an older search.

**Form** (CL1):

- From location, to location, from date, to date, and a supplier checklist loaded from `/api/suppliers`. All suppliers start checked, with select all and select none.
- Use Signal Forms when the installed Angular marks them stable (v22+); otherwise typed Reactive Forms.
- Validation mirrors the server: required and trimmed, origin ≠ destination, to date ≥ from date, at least one supplier. Accessible labels, inline errors, submit disabled while invalid.

**Store** (signals, feature-scoped): a state machine `idle → searching → completed | timedOut | cancelled | error`, driven by a pure reducer over stream events. The reducer is the main unit-test target.

**Cancellation and stale results** (CL5):

- Each search runs through `switchMap`, so a new search unsubscribes from and aborts the previous stream.
- Defence in depth: the reducer drops any event whose `searchId` isn't the active search, and anything after a terminal event.
- A Cancel button stops the search in flight.

**Results list** (CL2, CL3):

- Ascending by price, with deterministic tie-breaks: price, then response time, then supplier name.
- `@for` tracked by supplier id, so DOM nodes are reused and never re-created.
- One row slot per selected supplier from the start, with a skeleton while pending, so the list height never changes mid-search.
- Re-ordering animates `transform` only: FLIP, or rows placed with `translateY(rank × rowHeight)` and a CSS transition. Respect `prefers-reduced-motion`.
- Failed and no-response suppliers render as distinct muted rows below all priced rows.
- Prices through `Intl.NumberFormat` or `CurrencyPipe`; response time in seconds with one decimal.

**Progress** (CL4):

- "X of N suppliers responded" (failures count as responded, with their own count), chips for pending suppliers, and a subtle bar toward the 6-second deadline.
- A clear final badge (Completed, Timed out naming the silent suppliers, Cancelled or Error), announced through an `aria-live="polite"` region.
- With the default selection, the never-responding supplier makes "Timed out" the expected ending. That is correct behaviour, not a bug.

**Tests** (Vitest):

- Reducer: sorted insertion, ties, counts, stale-event rejection, events after the terminal event ignored, each terminal transition.
- Store: a new search aborts the previous stream; late events from the old search never reach state.
- Components: form defaults and validation, rendering order, progress text, final badges, DOM node identity kept across re-sorts.

**Browser verification:** run mandatory checks 1–6 from C2 and attach the evidence to the phase report.

**Gate:** unit tests green; browser checks passed with evidence.

### Phase 7 — History screen

Goal: a fast, shareable history view where the server does all filtering, sorting and paging.

**Filters** (HC1, H2):

- Start date and end date (default: the last 7 days), and a supplier multi-select (all by default) built on Angular Aria (stable in v22) or the Angular CDK for keyboard and screen-reader support.
- Origin and destination text filters (debounced), because the API supports them.
- An "Include failures" toggle, off by default. When on, failed, timed-out and cancelled rows appear with an outcome badge and "—" for price.

**Table** (HC2):

- Columns: Date (local time, converted from UTC), Route (origin → destination, with the searched shipping dates as a secondary line), Supplier, Price, Response time.
- Every header is a sort button with `aria-sort`, toggling ascending and descending.
- Pagination: first, previous, next, last, a page-size selector, and "Showing 21–40 of 312".
- A loading skeleton, an empty state, and an error state with retry.

**Behaviour:**

- Filter, sort and page state lives in the URL query string: links are shareable, and back, forward and reload restore the view.
- Local-day date filters convert to UTC instants before the API call.
- Load data with an RxJS `switchMap` pipeline, or with `httpResource` if the installed version marks the resource APIs stable.

**Tests** (Vitest): query-parameter mapping both ways, URL sync, sort toggling, pagination boundaries, date-boundary conversion, and each rendering state.

**Browser verification:** run a few searches, then filter by one, several and all suppliers, sort every column, page through, and reload with filters in the URL. Confirm each network call carries the expected query parameters.

**Gate:** unit tests green; browser checks passed with evidence.

### Phase 8 — End-to-end tests, browser verification and hardening

Goal: proof that the whole system works for a real user, under stress, every time.

**Playwright Test suite** in `pricehunt-frontend/e2e/`:

- `webServer` entries start the real API (seeded simulator via an environment variable) and the dev server, using the same underlying commands as the `run.ps1` scripts so the suite doesn't depend on a shell. They wait on `/health` and the app URL.
- Prefer the installed Chrome channel so no browser download is needed; document the fallback.
- Role-based locators only (derive or check them with Playwright MCP). Assert invariants, never exact prices.

Scenarios:

1. Results appear progressively and are ascending at every observation.
2. The progress counter only increases. The default selection ends "Timed out" at about 6 s; deselecting the silent supplier ends "Completed".
3. Rapid re-search: start search A, then search B at once. No row from A ever appears, the network log shows A aborted, and with "Include failures" on, history lists A's pending suppliers as Cancelled.
4. The Cancel button ends in the Cancelled state.
5. Validation errors block submission.
6. History filters, every sort column, pagination and URL restoration.
7. An API failure (simulated with request interception) shows a friendly error and no unhandled console errors.
8. `@axe-core/playwright` finds no serious or critical violations on either screen, and the keyboard-only flow works.

Run the suite with `--repeat-each=3`; it must pass every time.

**Hardening with MCP:**

- CPU and network throttling during a live search.
- A performance trace showing no layout shifts or long tasks caused by re-sorting.
- A Lighthouse audit (accessibility and best practices) on both screens.
- A phone-width check, and a clean console everywhere.

**Gate:** every suite green three runs in a row; findings fixed or listed as known limitations.

### Phase 9 — Documentation and final verification

Goal: a submission a reviewer can clone, run with two commands, and understand from the README alone.

- Write `README.md` with every section in E1, including Mermaid diagrams (architecture, the streaming sequence with deadline and cancellation, the ER diagram) and 2–3 screenshots captured through MCP.
- Finalise the ADRs and `docs/requirements-traceability.md`: every ID implemented, with evidence.
- Push everything, then run the clean-clone check: clone `https://github.com/LiorRavid/pricehunt.git` into a temporary directory and run only the two `run.ps1` scripts. Confirm the database appears on its own and a search completes end to end, then run every test suite from that clone.
- Where both are available, run the scripts under Windows PowerShell 5.1 and PowerShell 7; otherwise record which one you verified.

**Gate:** E2 fully satisfied; then write the final hand-off (E3).

## C — Context

Use this reference throughout: C1 defines what to build, C2 the tools, C3 the standards, and C4 the traps to avoid.

### C1. Requirements and traceability IDs

The assignment file, `PriceHunt_Home_Assignment.md`, is the source of truth when present; these IDs restate it for traceability. If this prompt and the assignment ever disagree, follow the assignment and note the conflict.

**Scenario:** a user searches shipping prices between two locations for a date range. The server queries the selected suppliers (all seven by default), and prices appear cheapest first while suppliers are still responding. Every search and price is persisted and browsable later by date and supplier.

**Suppliers** (simulated in-process, no real network calls)

- **S1** Seven suppliers behind a common interface.
- **S2** Each responds after a random 0.5–5 s delay with a random price.
- **S3** One supplier fails about 30 % of the time.
- **S4** One supplier never responds.

**Search parameters**

- **P1** From location, to location, from date, to date, and a supplier list (all by default).

**Server**

- **SV1** The search endpoint queries the selected suppliers and streams each response as it arrives, never waiting for all.
- **SV2** The README explains the streaming approach and the reasoning.
- **SV3** A search lasts at most 6 s. At the limit it ends gracefully with the results so far and tells the client it has ended.
- **SV4** A supplier failure never affects other suppliers.
- **SV5** Client cancellation (disconnect or new search) stops server work, and cancellation reaches the supplier calls.

**Client (Angular)**

- **CL1** A search form with the P1 fields, all suppliers selected by default; results appear progressively.
- **CL2** The list stays sorted cheapest to most expensive as results arrive.
- **CL3** No visible flicker or jumping while re-ordering.
- **CL4** Progress such as "5 of 7 suppliers responded", with a clear final state (Completed or Timed out).
- **CL5** A new search cancels the previous one; a late result from an old search never appears in the new one.

**Persistence**

- **DB1** Every search: from and to location, from and to date, selected suppliers, timestamp, and final status (completed, timed out or cancelled).
- **DB2** Every supplier response, linked to its search: supplier, price, response time, timestamp. Failures are recorded too.
- **DB3** EF Core with a zero-install database (SQLite); the schema is created automatically on first run.

**History API**

- **H1** A history endpoint taking a start date, an end date and a supplier list (one, several or all).
- **H2** History is also filterable by from and to location.

**History screen (Angular)**

- **HC1** A separate screen with filters for start date, end date and multi-select suppliers.
- **HC2** A table of Date, Route, Supplier, Price and Response Time, sortable by every column, with pagination.

**Submission**

- **SUB1** A Git repository with two projects. One command runs each, with no special setup, installation or configuration.
- **SUB2** A README covering how to run, how and why results are streamed, the database and table structure, design decisions and trade-offs, what you would do differently, and which AI tools were used where.
- **SUB3** .NET 8 or later and Angular 17 or later.

**Interpretations to apply** (record them in the README):

- History dates filter on the quote timestamp, not the shipping dates: a half-open interval of UTC instants derived from the user's local days.
- A supplier still pending at the deadline is recorded as `TimedOut`; one pending at a client cancel is recorded as `Cancelled`. Both count as recorded failures under DB2.
- "Responded" counts successes and failures; suppliers with no response are listed separately.
- "Two projects" are `pricehunt-backend` (the .NET solution with its layer projects) and `pricehunt-frontend` (the Angular app). "One command" is each folder's `run.ps1`.

### C2. Environment and browser tooling

The machine is already configured: .NET SDK, Node.js and npm, Angular CLI, PowerShell, Git, Google Chrome, and both MCP servers are installed and connected. Git can already reach the private repository `https://github.com/LiorRavid/pricehunt.git`.

- Don't install global tools or change system settings. Project-local NuGet and npm packages, and a local `dotnet-tools.json` manifest (e.g. for `dotnet-ef`), are fine.
- Everything you ship runs on Windows, macOS and Linux. The two `run.ps1` scripts are the only shell scripts and run on Windows PowerShell 5.1 and PowerShell 7+; any other tooling is written in Node or .NET, not Bash.

**[Playwright MCP](https://github.com/microsoft/playwright-mcp): user-level functional checks, driven by accessibility snapshots**

- Drive flows with `browser_navigate`, `browser_snapshot`, `browser_fill_form`, `browser_type`, `browser_click`, `browser_select_option`, `browser_press_key` and `browser_wait_for`.
- Check outcomes with `browser_snapshot` or `browser_find`, `browser_console_messages`, `browser_network_requests` and `browser_network_request`. Capture README images with `browser_take_screenshot`.
- Instrument the page with `browser_evaluate`.
- If the `testing` capability is enabled, derive stable locators with `browser_generate_locator` and the `browser_verify_*` tools. If the `network` capability is enabled, `browser_network_state_set` simulates going offline.

**[Chrome DevTools MCP](https://github.com/ChromeDevTools/chrome-devtools-mcp): diagnostics, network truth and performance**

- `list_network_requests` and `get_network_request`: the search request returns `text/event-stream` and stays open while events arrive; starting a new search cancels the previous request.
- `list_console_messages` and `get_console_message`: zero errors and warnings on both screens.
- `performance_start_trace`, `performance_stop_trace` and `performance_analyze_insight` during a live search: look for layout shifts, long tasks and forced reflows caused by re-sorting.
- `emulate` for CPU and network throttling, `resize_page` for phone width, `lighthouse_audit` for accessibility and best practices, `evaluate_script` for in-page probes, and `take_screenshot` and `take_snapshot` for evidence.

**Mandatory browser checks** (Phase 6 onward, and after every UI fix):

1. **Progressive arrival:** inject a `MutationObserver` that timestamps each result row as it appears. Rows must arrive at different times spread over more than a second, not in one batch.
2. **No flicker:** mark each row element early in a search (e.g. a `data-probe` attribute). After later arrivals and re-sorts, the same element objects are still connected, and movement uses `transform` transitions.
3. **Always sorted:** sample the rendered prices several times during a search; every sample is ascending.
4. **Cancellation:** start a search, then another about a second later. The network panel shows the first request cancelled, no row from the first search appears, and the API log shows it cancelled.
5. **Deadline:** the default selection ends "Timed out" at about 6 s; with the silent supplier deselected, it ends "Completed".
6. **Hygiene:** no console errors or warnings, no duplicate or unexpected requests, and one open stream per tab.

Record each check's evidence (a one-line tool result, a screenshot path) in the phase report. If an MCP server is unavailable, say so and use the other server or the E2E suite. Never report a check as passed without evidence.

### C3. Engineering standards

**Clean Architecture (backend)**

- Dependencies point inward. Domain references nothing; Application references only Domain; Infrastructure implements Application's ports; Api is the composition root and the only layer that knows HTTP. Api references Infrastructure only to register services.
- Architecture tests enforce this: no EF Core or ASP.NET Core types in Domain or Application, and no Api references from Infrastructure.
- Plain use-case classes and explicit mapping. No MediatR 13+ or AutoMapper 15+, which moved to [commercial licensing](https://luckypennysoftware.com/faq) and add nothing here. No FluentAssertions 8+ ([commercial licence](https://www.nuget.org/packages/FluentAssertions)); use AwesomeAssertions or Shouldly, with NSubstitute or hand-written fakes.
- Options pattern with startup validation; `TimeProvider` instead of `DateTime.UtcNow`; a cancellation token on every async API; no `async void`, `.Result` or `.Wait()`.

**C# conventions**, from Microsoft's [coding conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions) and [identifier names](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/identifier-names), enforced through `.editorconfig` and analyzers:

- Modern language features; keywords (`string`, `int`) over runtime type names; `int` over unsigned types.
- `var` only when the right-hand side makes the type obvious (`new`, a cast, a literal); explicit types in `foreach`; `var` for LINQ query results.
- String interpolation; raw string literals instead of escapes; `StringBuilder` in loops.
- Collection expressions; `required` properties to force initialisation; primary-constructor parameters in camelCase on classes and PascalCase on records.
- Catch only exceptions you can handle, never bare `Exception` without a filter; specific exception types with clear messages; `using` declarations.
- File-scoped namespaces, `using` directives outside the namespace, Allman braces, four-space indentation, one statement and one declaration per line, line breaks before binary operators.
- Naming: PascalCase for types, namespaces, public members, methods and constants; camelCase for parameters and locals; `_camelCase` for private and internal instance fields; `s_` for private and internal static fields; `I` for interfaces; `T` for type parameters; the `Async` suffix on async methods; no cryptic abbreviations.
- XML doc comments on public APIs; `//` comments on their own line, starting uppercase and ending with a period.

**[Angular style guide](https://angular.dev/style-guide)**

- Hyphenated file names matching the identifier inside (`search-form.ts` holds `SearchForm`). A component's `.ts`, `.html` and `.css` share one base name, `.spec.ts` sits beside the code, and there is no `utils.ts`, `helpers.ts` or `common.ts`.
- UI code under `src`, bootstrapped in `src/main.ts`. Organise by feature, never by type (no `components/` or `services/` folders); one concept per file; split crowded folders.
- `inject()` instead of constructor injection.
- An application-specific selector prefix for components and directives, and camelCase attribute selectors for directives.
- Angular members (injections, inputs, outputs, queries) grouped before methods; `protected` for template-only members; `readonly` on `input()`, `model()`, `output()` and queries.
- `[class]` and `[style]` bindings instead of `NgClass` and `NgStyle`; handlers named for what they do (`startSearch()`, not `onClick()`); lifecycle hooks that call well-named methods; lifecycle interfaces implemented.
- Components focus on presentation: complex template logic moves into `computed()`, and transformation and validation into plain functions or services.
- Beyond the guide: `OnPush`, signal APIs, native control flow (`@if`, `@for` with `track`, `@switch`), lazy routes, no `any`, stable APIs only, and no legacy `@angular/animations` (use CSS, the Web Animations API, or native `animate.enter` and `animate.leave`).

**Frontend layering** (Clean Architecture applied to Angular)

- Per feature: `data-access/` (HTTP and SSE clients, DTOs, mappers) → `state/` (signal store, pure reducer) → `ui/` (presentational components, inputs and outputs only) → the page component that wires state to UI.
- Pure domain types and functions import nothing from Angular. Components never call `HttpClient` or `fetch`, features never import each other, and shared code lives in `core/` or `shared/`. ESLint import-boundary rules enforce this.

**Tailwind CSS**

- Tailwind v4 with CSS-first configuration: `@import 'tailwindcss';` in `src/styles.css`, and tokens (brand colours, radii, fonts) in `@theme`. No `tailwind.config.js` unless a plugin requires one.
- Utilities in templates; repeated patterns become Angular components, not `@apply` chains. A component stylesheet that needs `@apply` references the global stylesheet with `@reference`.
- Never assemble class names dynamically (`'bg-' + colour`); map each state to a complete class string so Tailwind detects it.
- Mobile-first layout, visible `focus-visible` rings, `sr-only` labels where needed, `motion-safe:` and `motion-reduce:` variants, and WCAG AA contrast.
- A calm, professional look: a neutral palette, one accent colour, one spacing scale.

### C4. Technical guidance and known pitfalls

- **Transport: SSE over `POST /api/searches`, read with `fetch` and a `ReadableStream`.** One request equals one search, so a disconnect maps straight onto `RequestAborted`. POST carries the criteria as JSON, `AbortController` gives explicit cancellation, and plain HTTP passes through proxies and dev servers.
- **Why not the alternatives:** native `EventSource` is GET-only and reconnects automatically, which would silently re-run a search. SignalR and WebSockets add two-way machinery this one-way stream doesn't need. Record the comparison in ADR-001 and the README.
- **Iterator constraint:** C# can't `yield return` inside a `try` block that has a `catch`. Design around it: a producer writing outcomes to a `Channel<T>` with a consumer loop that yields, or `Task.WhenEach` (.NET 9+) over tasks that never throw.
- **Two cancellations look alike:** the deadline and the caller both raise `OperationCanceledException`. Decide the outcome by which token fired, not by the exception.
- **Finalising after cancellation:** the request token is already cancelled when you persist `Cancelled`, so that write uses `CancellationToken.None` with a short timeout.
- **Scope lifetime:** background work must not outlive the request's DI scope, which disposes a scoped `DbContext`. Finalise before the response completes, or use `IDbContextFactory<T>` for short-lived contexts. Never share one `DbContext` across concurrent tasks.
- **SQLite with EF Core:** the provider can't translate comparisons or ORDER BY on `decimal`, `DateTimeOffset`, `TimeSpan` or `ulong` ([EF Core docs](https://learn.microsoft.com/en-us/ef/core/providers/sqlite/limitations)). That would break server-side price and date sorting, hence UTC `DateTime`, integer minor units and integer milliseconds.
- **UTC kind:** `DateTime` values read from SQLite come back `Unspecified`. Restore `Utc` with a value converter, or the API emits timestamps without `Z` and browsers shift them to the wrong zone.
- **Migrations:** commit them and apply them at startup, so reviewers never need the `dotnet ef` tool.
- **Reviewer experience:** the two `run.ps1` scripts are the only entry points; HTTP only in Development, fixed ports, the dev-server proxy for `/api`, automatic database creation, no user secrets.
- **Proxy check:** confirm the Angular dev-server proxy streams events incrementally and passes client aborts on to the API. If it doesn't, call the API directly with a narrowly scoped CORS policy and record why.
- **Wire formats:** prices as `{ amount, currency }` and timestamps as ISO-8601 UTC strings; format them only in the UI.
- **PowerShell compatibility:** write `run.ps1` for both Windows PowerShell 5.1 and PowerShell 7+, so no `&&`, `||`, `??` or ternary operators. Resolve paths from `$PSScriptRoot`, set `$ErrorActionPreference = 'Stop'`, and check `$LASTEXITCODE` after each native command.
- **PowerShell traps:** keep scripts ASCII-only, because Windows PowerShell 5.1 reads BOM-less files in the system code page. In Windows PowerShell, `curl` is an alias for `Invoke-WebRequest`, so docs and scripts use `curl.exe`.
- **Execution policy:** Windows PowerShell's default client policy blocks scripts, and a downloaded archive carries the mark of the web. Document `powershell -ExecutionPolicy Bypass -File .\pricehunt-backend\run.ps1` as the form that always works.
- **Version fallbacks:** on .NET 8 or 9, write SSE frames by hand (`text/event-stream`; `event:`, `id:` and `data:` lines; flush per event), and on .NET 8 use `Channel<T>` instead of `Task.WhenEach`. On Angular versions before stable Signal Forms, use typed Reactive Forms.

## E — Expectation

The work is finished when the repository matches E1, passes every check in E2, and is reported as in E3.

### E1. Deliverables

The finished repository has this shape:

```text
pricehunt/                          clone of github.com/LiorRavid/pricehunt
├─ README.md
├─ .editorconfig  .gitattributes  .gitignore
├─ docs/
│  ├─ adr/                          ADR-001 … ADR-004, plus any new ones
│  ├─ requirements-traceability.md
│  └─ screenshots/
├─ pricehunt-backend/
│  ├─ run.ps1                       the single command that runs the API
│  ├─ PriceHunt.slnx (or .sln)  Directory.Build.props  Directory.Packages.props
│  ├─ src/
│  │  ├─ PriceHunt.Domain/
│  │  ├─ PriceHunt.Application/
│  │  ├─ PriceHunt.Infrastructure/
│  │  └─ PriceHunt.Api/
│  └─ tests/
│     ├─ PriceHunt.Domain.Tests/
│     ├─ PriceHunt.Application.Tests/
│     ├─ PriceHunt.Infrastructure.Tests/
│     ├─ PriceHunt.Api.Tests/
│     └─ PriceHunt.ArchitectureTests/
└─ pricehunt-frontend/
   ├─ run.ps1                       the single command that runs the app
   ├─ package.json  angular.json  proxy.conf.json  .postcssrc.json  playwright.config.ts
   ├─ src/app/
   │  ├─ core/
   │  ├─ shared/
   │  └─ features/
   │     ├─ search/
   │     └─ history/
   └─ e2e/
```

**Run commands**: exactly one per project, each the `run.ps1` in its folder. Start the backend first, in its own terminal.

- Backend: `.\pricehunt-backend\run.ps1`
- Frontend: `.\pricehunt-frontend\run.ps1`, then open `http://localhost:4200`
- When script execution is blocked (Windows' default policy, or a downloaded archive): `powershell -ExecutionPolicy Bypass -File .\pricehunt-backend\run.ps1`, and likewise for the frontend. On macOS or Linux: `pwsh ./pricehunt-backend/run.ps1`.

**What each `run.ps1` must do:**

- Resolve every path from `$PSScriptRoot`, so it works from any current directory.
- Run unchanged on Windows PowerShell 5.1 and PowerShell 7+, with no admin rights, global installs or prompts.
- Check prerequisites first, failing fast with a message that names the missing tool and the required version.
- Print the URLs to open, and propagate the exit code of the process it runs.
- Backend: run the API, e.g. `dotnet run --project src/PriceHunt.Api --launch-profile http`. Restore and build happen automatically, and the database appears on first run. An optional `-ResetDatabase` switch deletes the SQLite file first.
- Frontend: run `npm ci` only when `node_modules` is missing or older than `package-lock.json`, then start the dev server with the `/api` proxy. Warn, without failing, when the API's `/health` isn't reachable yet.

**Database shape** (improve it if you can, and say why):

| Table | Columns | Notes |
| --- | --- | --- |
| `Searches` | id, origin, destination, ship date from, ship date to, created at (UTC), completed at (UTC, nullable), status | Time-ordered GUID key; status is `Running`, `Completed`, `TimedOut`, `Cancelled` or `Faulted` |
| `SearchSuppliers` | search id, supplier id | The suppliers selected for each search |
| `SupplierResponses` | id, search id, supplier id, outcome, price minor units (nullable), currency (nullable), response time ms, received at (UTC), error code and message (nullable) | Indexed for every history filter and sort |

**README sections** (SUB2, plus what reviewers look for):

1. Overview with screenshots.
2. Prerequisites (exact .NET SDK, Node and PowerShell versions), the two `run.ps1` commands with the execution-policy fallback, where the database lives, and how to reset it.
3. How to run each test suite, with coverage commands.
4. Architecture: layers, the dependency rule and the frontend structure, with a Mermaid diagram.
5. Streaming, how and why: the SSE choice, alternatives considered, the event contract, and the deadline and cancellation flow as a Mermaid sequence diagram.
6. Database and table structure: an ER diagram, and a table per entity with types, nullability, keys, indexes and the SQLite type choices.
7. API reference with example requests, including `curl -N` for the stream (`curl.exe` in Windows PowerShell).
8. Design decisions and trade-offs, linking the ADRs.
9. Assumptions and interpretations (from C1).
10. What I would do differently, e.g. real supplier adapters with resilience policies, OpenTelemetry, PostgreSQL, keyset paging, rate limiting, auth, containers, CI.
11. AI usage: the tools used (this coding agent, Playwright MCP, Chrome DevTools MCP), what each was used for, and how the output was verified. Report only what happened, and leave a clearly marked placeholder for the candidate's own review notes.

**Out of scope** unless everything else is done: authentication, Docker, CI pipelines, deployment, internationalisation. Mention them under next steps instead.

### E2. Definition of Done

Done means every item below is checked, with evidence:

- [ ] Every C1 requirement is implemented and linked to at least one automated test in `docs/requirements-traceability.md`.
- [ ] Backend: zero build warnings, `dotnet format --verify-no-changes` clean, all suites green, architecture tests enforcing the dependency rule, and line coverage ≥ 90 % on Domain and Application and ≥ 80 % overall.
- [ ] Frontend: lint and format clean, strict TypeScript without `any`, unit tests green with ≥ 80 % coverage on state, data-access and pure functions, and a production build with no budget warnings.
- [ ] E2E: green on three consecutive runs, with no fixed sleeps and only role-based locators.
- [ ] Every mandatory browser check in C2 performed with evidence; no console errors or warnings; no serious or critical axe violations.
- [ ] A fresh clone of `https://github.com/LiorRavid/pricehunt.git` runs with only the two `run.ps1` scripts, and the database and schema appear automatically.
- [ ] Everything is committed and pushed to `origin`, and the working tree is clean.
- [ ] No TODOs, commented-out code, debug logging, unused dependencies or secrets; only permissively licensed dependencies.
- [ ] README complete per E1; ADRs and the traceability matrix current.

### E3. Reporting

Report briefly at every gate, and once in full at the end.

**At each gate:**

- Phase and status: passed or blocked.
- What was built, in 3–6 bullets.
- Evidence: commands run with pass and fail counts, and each MCP check with a one-line result.
- Decisions and deviations, each with a one-line reason.
- The next phase.

**Final hand-off:**

1. A summary in two or three sentences.
2. The two `run.ps1` commands and the URLs.
3. A test results table: suite, tests, passed, coverage, duration.
4. Requirement traceability: ID, status, evidence (file or test name).
5. Assumptions, known limitations, and anything deliberately left out.
6. The last commit pushed to `origin` (hash and message).
7. What's left for the candidate: review the code on GitHub and complete the personal notes in the README's AI-usage section.

Never report a requirement as met without evidence. If something is unfinished, say so plainly.
