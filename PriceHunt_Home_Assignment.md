**SHIP4WD**

# PriceHunt | Home Assignment

**Stack:** .NET 8 or later and Angular 17 or later. AI tools are allowed. Please document in the README which tools you used and where.

> **The scenario**
>
> A user searches for shipping prices between two locations for a date range. The server queries the selected suppliers (all seven by default), and prices appear on screen from cheapest to most expensive while suppliers are still responding. Every search and every price is persisted so the user can return later and filter the history by date and supplier.

---

## Part 1: Live Search Across Seven Suppliers

### Suppliers | simulated inside the project

- Seven suppliers behind a common interface. No real network calls are required. Simulate them.
- Each supplier responds after a random delay of 0.5 to 5 seconds, with a random price.
- One supplier fails approximately 30% of the time. One supplier never responds.

### Search parameters

- From location and to location.
- From date and to date.
- A list of suppliers to query. By default, all suppliers are selected.

### Server side

- Provide a search endpoint that accepts the search parameters, queries the selected suppliers and streams each response to the client as soon as it arrives. Do not wait for all suppliers to finish.
- The implementation approach is up to you. Explain the approach and the reasoning in the README.
- Each search has a maximum duration of 6 seconds. When the limit is reached, finish gracefully with the results collected so far and tell the client that the search has ended.
- A supplier failure must not affect the other suppliers.
- If the client cancels, either by disconnecting or starting a new search, the server must stop the work in progress. Cancellation must propagate to the supplier calls.

### Client side | Angular

- Provide a search form with from location, to location, from date, to date, and a supplier list with all suppliers selected by default. Results appear progressively.
- Keep the list sorted from cheapest to most expensive as new results arrive.
- The list should not visibly flicker or jump while it is being re-ordered.
- Show progress, for example: "5 of 7 suppliers responded", with a clear final state such as "Completed" or "Timed out".
- Starting a new search cancels the previous one. A result belonging to an old search must never appear in the new search, even if it arrives late.

---

## Part 2: Persistence and Price History

### Database persistence

- Persist every search: search parameters (from / to location, from / to date, selected suppliers), timestamp, and final status: completed, timed out, or cancelled.
- Persist every supplier response and associate it with its search: supplier, price, response time, and timestamp. Supplier failures must also be recorded.
- Use Entity Framework Core. Choose a database that is easy to run without installation, such as SQLite, and ensure the schema is created automatically on first run.

### Server side

- Provide a history endpoint accepting a start date, an end date, and a list of suppliers. The list may contain one, several, or all suppliers.
- The history can also be filtered by from / to location.

### Client side | Angular

- Create a separate history screen with filters for start date, end date, and multi-select supplier selection.
- Display a table with Date, Route, Supplier, Price, and Response Time. The user should be able to sort by every column and navigate between pages.

---

## Submission Requirements

- A Git repository, either as a link or archive, containing two projects. Running the solution should be as easy as possible: one command to run each project, with no special setup, installation, or configuration.
- README covering how to run the projects, how and why you chose to stream results to the client, database and table structure, design decisions and trade-offs, what you would do differently, and where AI was used.

**Good luck!**
