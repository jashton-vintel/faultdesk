# FaultDesk — project notes for Claude Code

Garage fault-triage assessment. Customers describe vehicle faults in free text; the app turns them into structured,
searchable tickets with AI triage, similar-ticket lookup (SQL Server 2025 vector search) and a streamed
"AI Investigate" report for the workshop.

## Commands
- Build: `dotnet build FaultDesk.sln`
- Test: `dotnet test FaultDesk.sln`
- Run (dev): `docker compose up sqlserver -d` then `dotnet run --project src/FaultDesk.Web`
- Full stack: `docker compose up --build` (see README)
- EF migrations: `dotnet dotnet-ef migrations add <Name> -p src/FaultDesk.Infrastructure -s src/FaultDesk.Web`

## Layering (dependencies point inwards)
`Domain <- Application <- Infrastructure <- Web`
- **Domain**: aggregates, value objects, enums, invariants. No package references.
- **Application**: use-case handlers, ports (interfaces), DTOs, versioned prompt builders. Only `Microsoft.Extensions.AI.Abstractions`.
- **Infrastructure**: EF Core persistence (SQL Server 2025 `vector`), AI adapters (Anthropic / OpenAI / Mock), mock vehicle lookup, seeding.
- **Web**: Blazor Server UI and the composition root only. No business logic in components.

## Conventions
- AI is best-effort: triage, clarifying questions and embedding failures are logged and skipped. Submitting a ticket never fails because of AI.
- Prompts live in `Application/Prompts` with a `Version` constant. Bump it when the prompt changes and mirror the text in `docs/prompts`.
- Never commit secrets. Keys come from `dotnet user-secrets` (dev) or the git-ignored `.env` (compose). `appsettings.json` holds no secrets.
- Conventional commits, one increment per commit, build and tests green before committing.
- Tests: xUnit with hand-written fakes (no mocking library), no database in tests.
