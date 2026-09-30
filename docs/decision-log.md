# Decision log

A running record of decisions made during the build: what was decided, what else was considered, and why. The significant ones have their own [ADRs](adr/); this log also records the smaller calls. It feeds the README's "Design decisions and trade-offs" section.

| # | Phase | Decision | Alternatives | Reason |
| --- | --- | --- | --- | --- |
| 1 | P0 | .NET SDK **10.0.401** (runtime 10.0.12) and Angular CLI **22.2.0** | .NET 8 or 9 (installed); older Angular | The newest installed LTS SDK and the newest CLI, as the brief requires. .NET 10 brings `TypedResults.ServerSentEvents`, `SseItem<T>.EventId` and built-in minimal-API validation. |
| 2 | P0 | Environment recorded: Node 24.21.0, npm 11.19.0, Git 2.56.0, Windows PowerShell 5.1.26100, Chrome 153 | — | The baseline for the README's prerequisites. PowerShell 7 isn't installed, so the scripts are verified on 5.1 only. |
| 3 | P0 | Build in the existing repository (commit `997bbef` holds the starter) rather than a fresh clone | Clone into a new folder and copy the starter across | It already is the clone of `origin`. Local `main` tracks `origin/main`, so pushes are fast-forwards. |
| 4 | P0 | Move the assignment and the implementation brief into `docs/brief/` | Keep them at the root; stop tracking them | Both were already in the pushed history. Moving them keeps the root in the brief's layout, and the README can link to them. |
| 5 | P0 | Commit locally at each gate, and push once HTTPS access to GitHub works | Change Git's TLS backend (`schannel`) | AVG's HTTPS scanning re-signs GitHub's certificate, which Git's OpenSSL bundle rejects. The repository owner is fixing that themselves. |
| 6 | P0 | Conventional Commits that cite requirement IDs, with no AI co-author trailer | A `Co-Authored-By` trailer | The owner's choice. AI usage is disclosed in the README instead. |
| 7 | P0 | Line endings: `* text=auto eol=lf`, `*.ps1 eol=crlf`, one root `.editorconfig` | Per-project `.editorconfig` files | One source of truth. Windows PowerShell 5.1 expects CRLF, and the global `core.autocrlf=true` must not leak CRLF into the repository. |
| 8 | P0 | Streaming over SSE with `POST` + `fetch` (ADR-001) | SignalR, WebSockets, NDJSON, long polling, gRPC-Web, `EventSource` | One request per search maps a disconnect straight onto cancellation, and validation stays a normal `400`. |
| 9 | P0 | API on fixed HTTP port **5080**, dev server on **4200** with a `/api` proxy | HTTPS dev certificate; CORS | Reviewers need no certificate step and no CORS configuration. |
| 10 | P0 | Dependencies limited to permissively licensed packages; `@axe-core/playwright` (MPL-2.0) is the one exception | Skip automated accessibility checks | The brief requires axe checks. It's a dev-only test dependency and isn't shipped. |
