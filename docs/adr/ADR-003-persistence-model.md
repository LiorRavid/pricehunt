# ADR-003: Persistence model and SQLite type mapping

- Status: Accepted
- Date: 2026-09-30
- Requirements: DB1, DB2, DB3, H1, H2, HC2

## Context

Every search and every supplier response, including failures, must be stored (DB1, DB2). The storage must use EF Core with a database that needs no installation and creates its schema on first run (DB3).

The history screen filters by a quote-timestamp range, suppliers and locations (H1, H2). It sorts by every column, and pages (HC2). All of this must happen in the database, not in memory.

EF Core's SQLite provider **can't translate comparisons or `ORDER BY` on `decimal`, `DateTimeOffset`, `TimeSpan` or `ulong`**. It also reads `DateTime` values back with `DateTimeKind.Unspecified`, which makes JSON drop the `Z` so browsers shift the time.

## Decision

**Tables.** These follow the brief's shape, with three improvements marked ★.

| Table | Key columns | Notes |
| --- | --- | --- |
| `Suppliers` ★ | `Id` (PK), `Name` | Upserted from configuration at startup. Gives supplier ids referential integrity and lets history sort by supplier *name*. |
| `Searches` | `Id` (Guid v7, PK), `Origin`, `Destination`, `OriginNormalized` ★, `DestinationNormalized` ★, `ShipDateFrom`, `ShipDateTo`, `CreatedAt`, `CompletedAt?`, `Status` | Status is `Running`, `Completed`, `TimedOut`, `Cancelled` or `Faulted`. |
| `SearchSuppliers` | (`SearchId`, `SupplierId`) PK | The suppliers selected for each search. FKs to both tables. |
| `SupplierResponses` | `Id` (Guid v7, PK), `SearchId`, `SupplierId`, `Outcome`, `PriceMinorUnits?`, `Currency?`, `ResponseTimeMs`, `ReceivedAt`, `ErrorCode?`, `ErrorMessage?` | ★ A unique index on (`SearchId`, `SupplierId`) enforces one outcome per supplier per search. |

**Type mapping.**

- **Timestamps:** UTC `DateTime`. A global value converter marks values read back as `DateTimeKind.Utc`. EF stores them as fixed-format ISO text, so text order equals time order.
- **Money:** a `long` count of minor units (cents) plus an ISO-4217 `Currency` code, stored as an INTEGER that can be compared and sorted. The simulation only quotes USD, and minor units assume a currency with two decimals.
- **Response time:** integer milliseconds.
- **Shipping dates:** `DateOnly`, stored as `yyyy-MM-dd` text, which sorts correctly.
- **Enums** (`Status`, `Outcome`) are stored as strings so the database stays readable.
- **Ids:** Guid v7, stored as text. The first characters encode the creation time, so ids work as a deterministic, time-ordered tie-breaker for sorting.
- **Case-insensitive matching:** origin and destination are also stored upper-cased with `ToUpperInvariant`. Filtering and sorting use those columns because SQLite's `NOCASE` only folds ASCII. For example, "zürich" must match "Zürich".

**Indexes** cover every history filter and sort column:

- `SupplierResponses`: `ReceivedAt`, (`SupplierId`, `ReceivedAt`), (`Outcome`, `ReceivedAt`), `PriceMinorUnits` and `ResponseTimeMs`.
- `Searches`: `OriginNormalized`, `DestinationNormalized`, and `Status`, which the startup recovery uses to find searches left `Running`.
- `Suppliers`: `Name`.
- `SearchSuppliers`: `SupplierId`, for the foreign key.

**Mapping style.**
- The Domain has no EF attributes. Infrastructure has its own persistence entities, each with an explicit `IEntityTypeConfiguration`, and maps to and from the domain explicitly (`PersistenceMapping`).
- History reads use `AsNoTracking` projections straight to DTOs.
- The `DbContext` is `internal`, so the API can only reach the data through Application ports.
- A design-time factory lets `dotnet ef migrations add` run from the Infrastructure project alone.

**Lifecycle.**

- **Migrations** are committed and applied with `Database.MigrateAsync()` at startup, and the applied migrations are logged. Reviewers never need the `dotnet ef` tool.
- **Journal mode:** WAL.
- **Database path:** `Database:Path` = `App_Data/pricehunt.db`, resolved against the API's content root, so it doesn't depend on the shell's working directory. The file is git-ignored, and `run.ps1 -ResetDatabase` deletes it.
- **Startup order:** migrate → WAL → sync suppliers → close searches left `Running` as `Cancelled`.

## Alternatives considered

- **`decimal` price and `DateTimeOffset` timestamps.** These are natural in C#, but SQLite can't sort or compare them in the database, so history would have to sort in memory.
- **No `Suppliers` table.** The table would match the brief exactly, but sorting by supplier would sort by id, not by the name users see, and supplier ids would have no referential integrity.
- **Copying the supplier's name onto each response.** This keeps a historical record of the name, but duplicates data and adds no integrity. Supplier names are stable configuration here.
- **`COLLATE NOCASE`.** It only folds ASCII letters.
- **The EF InMemory provider for tests.** It isn't relational: no SQL translation, no constraints, no migrations. Tests use real SQLite files instead.

## Consequences

- Every history filter, sort and page runs in SQL against an index, with a deterministic order.
- Mixing currencies would make price sorting meaningless. This is a known limitation, since the simulation only quotes USD, and it's listed in the README.
- The `Suppliers` table mirrors the configured catalogue. Removed suppliers keep their rows so older history still resolves.
