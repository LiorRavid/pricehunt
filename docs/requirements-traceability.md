# Requirements traceability

Every requirement from the assignment ([brief](brief/PriceHunt_Home_Assignment.md)), mapped to where it's implemented and the tests that prove it. The IDs follow C1 of the [implementation brief](brief/PriceHunt%20%E2%80%94%20RACE%20Implementation%20Prompt.md). This file is updated at every phase gate.

Status: **Planned** → **Implemented** (the code exists) → **Verified** (automated tests plus browser evidence where it applies).

| ID | Requirement | Implementation | Proving tests | Status |
| --- | --- | --- | --- | --- |
| S1 | Seven suppliers behind a common interface | `IShippingSupplier` (Application); seven configured simulated suppliers (Infrastructure) | Infrastructure: shipped-configuration test | Planned |
| S2 | Random 0.5–5 s delay and a random price | Simulated supplier's delay and price generator | Infrastructure: delay bounds; completes at the drawn delay on fake time | Planned |
| S3 | One supplier fails about 30 % of the time | Flaky supplier's failure decision | Infrastructure: 30 % ± 2 % over 10 000 seeded draws | Planned |
| S4 | One supplier never responds | Unresponsive supplier (infinite delay that honours cancellation) | Infrastructure: completes only through cancellation | Planned |
| P1 | From/to location, from/to date, supplier list (all by default) | `StartSearchRequest`, `SearchPlanner`, search form | Application: planner tests; Frontend: form tests | Planned |
| SV1 | Stream each response as it arrives | `SearchOrchestrator` + `POST /api/searches` (SSE) | Application: completion order; Api: first quote while another supplier is pending | Planned |
| SV2 | README explains the streaming approach and why | README "Streaming", ADR-001 | Review | Planned |
| SV3 | 6 s maximum, graceful end with results so far | Deadline linked to the caller's token | Application and Api: fake time advanced to 6 s → `TimedOut` | Planned |
| SV4 | A supplier failure never affects the others | Per-supplier wrapper that never throws | Application: throwing supplier alongside successful ones | Planned |
| SV5 | Client cancellation stops server work and reaches supplier calls | `RequestAborted` → linked token → supplier tokens | Application: caller cancel; Api: request abort → `Cancelled` | Planned |
| CL1 | Search form with the P1 fields, all suppliers selected; progressive results | Search feature (form + store) | Frontend: form defaults; E2E scenario 1 | Planned |
| CL2 | List stays sorted cheapest first as results arrive | Pure ordering + reducer | Frontend: reducer ordering; E2E scenario 1; browser check 3 | Planned |
| CL3 | No visible flicker or jumping | Fixed slots, tracked `@for`, transform-only movement | Frontend: DOM identity; browser checks 1–2; performance trace | Planned |
| CL4 | Progress ("X of N responded") and a clear final state | Progress and final-badge components | Frontend: progress and badge tests; E2E scenario 2 | Planned |
| CL5 | New search cancels the old one; no late results from it | `switchMap` + attempt/search-id/terminal guards | Frontend: stale-event tests; E2E scenario 3; browser check 4 | Planned |
| DB1 | Persist every search (parameters, suppliers, timestamp, final status) | `Searches` + `SearchSuppliers` tables | Infrastructure: round-trip; Api: statuses persisted | Planned |
| DB2 | Persist every supplier response, failures included | `SupplierResponses` table | Application: each outcome persisted once; Infrastructure: round-trip | Planned |
| DB3 | EF Core + SQLite, schema created on first run | Migrations applied at startup | Infrastructure: migrations apply to an empty database; clean-clone check | Planned |
| H1 | History endpoint: start date, end date, supplier list | `GET /api/history` | Infrastructure: filter matrix; Api: list binding | Planned |
| H2 | History filterable by from/to location | History query location filters | Infrastructure: location matching; Api: filter tests | Planned |
| HC1 | History screen with date and multi-select supplier filters | History feature (filters) | Frontend: filter/URL tests; E2E scenario 6 | Planned |
| HC2 | Table of Date, Route, Supplier, Price, Response time; sortable; paged | History feature (table, pagination) | Frontend: sort/pagination tests; E2E scenario 6 | Planned |
| SUB1 | Two projects, one command each, no special setup | `pricehunt-backend/run.ps1`, `pricehunt-frontend/run.ps1` | Clean-clone check | Planned |
| SUB2 | README: run, streaming, database, decisions, improvements, AI usage | `README.md` | Review | Planned |
| SUB3 | .NET 8+ and Angular 17+ | .NET 10 (`net10.0`, `pricehunt-backend/Directory.Build.props`); Angular 22 | Backend build: 0 warnings | Implemented (backend) |
