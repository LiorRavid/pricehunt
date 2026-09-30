# AI usage log

A factual record of where AI tools were used and how their output was checked. It feeds the README's "AI usage" section. The candidate's own review notes go in the README, not here.

| Phase | Tool | Used for | How the output was verified |
| --- | --- | --- | --- |
| P0 | Claude Code (Claude Opus 5.5) | Reading the assignment and brief; inspecting the environment (SDKs, Node, npm, Git, PowerShell); diagnosing Git's TLS failure (AVG HTTPS scanning, shown by the certificate chain); checking package versions, licences and API stability in the installed Angular and .NET packages; writing the implementation plan, ADRs, traceability matrix and decision log | Each environment fact came from a command whose output was read in the session (for example `dotnet --list-sdks` or the certificate issuer). API stability tags were read from the installed `.d.ts` and reference-assembly XML files. The plan was reviewed and approved by the repository owner. |
| P0 | Playwright MCP | Confirming the server responds (`about:blank`, tab listing) | Tool output: one tab on `about:blank` |
| P0 | Chrome DevTools MCP | Confirming the server responds (`about:blank`, page listing) | Tool output: page list returned |
| P1 | Claude Code (Claude Opus 5.5) | Writing the backend skeleton (solution layout, build props, central packages, C# `.editorconfig` rules, API skeleton, `run.ps1`) and the architecture and API smoke tests; diagnosing the MTP/VSTest error on .NET 10; finding the ArchUnitNET API by reflection | `dotnet build`: 0 warnings. `dotnet format --verify-no-changes`: clean. `dotnet test`: 8/8 passed. `run.ps1` under Windows PowerShell 5.1 served `/health` (`200 Healthy` via `curl.exe`), and failed fast with `dotnet` removed from PATH. |
