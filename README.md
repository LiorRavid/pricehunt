# PriceHunt

PriceHunt searches shipping prices across seven simulated suppliers.

- A .NET 10 API streams each quote to the browser over Server-Sent Events as soon as a supplier responds. A search stops after 6 seconds, and cancellation reaches every supplier call.
- An Angular 22 app shows quotes cheapest first while they arrive, without flicker.
- Every search and quote is stored in SQLite and can be browsed on a history screen.

> **Work in progress.** The full README (how to run, the streaming design, the database, decisions and trade-offs, AI usage) comes with the final phase. Until then:
>
> - [Assignment](docs/brief/PriceHunt_Home_Assignment.md) and [implementation brief](docs/brief/PriceHunt%20%E2%80%94%20RACE%20Implementation%20Prompt.md)
> - [Architecture decision records](docs/adr/)
> - [Requirements traceability](docs/requirements-traceability.md)
> - [Decision log](docs/decision-log.md) and [AI usage log](docs/ai-usage-log.md)
