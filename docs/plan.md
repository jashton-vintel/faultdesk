> Planning artefact: this is the implementation plan agreed at the start of the exercise, produced with Claude Code
> (plan mode) after clarifying questions. It is kept verbatim so the evolution of the solution can be compared against it.

# FaultDesk — garage fault-triage assessment (plan)

## Context

Interview assessment: build, in roughly one evening, an app that turns customers' free-text vehicle-fault
descriptions into something useful for a service adviser / workshop, and that shows how AI is used in the
development workflow. Deliverables: working app, git repo with an incremental commit history, build/run docs,
supporting process material. Unfinished features are fine if the docs explain the trade-offs.

Decisions agreed with the user:

| Area | Decision |
|---|---|
| Stack | C#, .NET 10 (SDK 10.0.400 installed), Blazor Web App (Interactive Server, prerender off), EF Core 10, SQL Server 2025 (native `vector` type) |
| AI | Anthropic `claude-opus-5` (official `Anthropic` NuGet 12.x) for triage / clarifying questions / AI Investigate, using Claude's web search server tool; OpenAI `text-embedding-3-small` (1536-dim) for embeddings via `Microsoft.Extensions.AI.OpenAI`. Both behind ports so providers are a config switch. |
| Demo mode | `Ai:ChatProvider=Mock` + `Ai:EmbeddingProvider=Mock` runs everything with zero API keys (canned triage/questions/report, deterministic hashing embeddings). This is the compose default. |
| Deployment | Docker Compose: SQL Server 2025 container + app container; keys from git-ignored `.env`; `dotnet run` path for dev against the compose SQL |
| Architecture | DDD-lite: `FaultDesk.Domain`, `FaultDesk.Application`, `FaultDesk.Infrastructure`, `FaultDesk.Web`, `FaultDesk.Domain.Tests`, `FaultDesk.Application.Tests` (xUnit, light). No MediatR; plain handler classes. |
| In scope | Customer portal (reg lookup mock → editable vehicle form → description → AI clarifying questions → submit), AI triage on submit, garage grid + ticket detail, related tickets (same-vehicle history + vector similarity with resolution notes), streamed AI Investigate persisted per run, status workflow + resolution notes, 20 seeded historical tickets, token usage + prompt versioning, docs/ADRs/AI-workflow notes |
| Out of scope (documented trade-offs) | Authentication, real DVLA lookup, separate deployables per portal, DB integration tests, Aspire |
| Secrets | Never in source. `.env` (git-ignored) for compose, `dotnet user-secrets` for dev, `.env.example` committed |
| Name / location | Solution `FaultDesk` (namespaces `FaultDesk.*`) in `C:\Users\Joshu\Desktop\Klipboard`; `git init` there, branch `main`, no remote (user pushes when ready) |
| Commits | Conventional-commit messages, one per increment (table below), each leaving `dotnet build` + `dotnet test` green; trailer `Co-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>` (fits the "AI in the workflow" story) |

Facts verified during planning (do not re-derive): `dotnet new sln` defaults to `.slnx` in SDK 10 → use `--format sln`;
global `dotnet-ef` is 9.0.10 → local tool manifest pinned to `dotnet-ef` 10.0.x; stable packages: `Anthropic` 12.48,
`Microsoft.EntityFrameworkCore.SqlServer` 10.0.12 (pin `10.0.*`, an 11 preview exists), `Microsoft.Extensions.AI` /
`.OpenAI` 10.10, `Microsoft.AspNetCore.Components.QuickGrid` 10.0.x, `Markdig`; `SqlVector<float>` lives in
`Microsoft.Data.SqlTypes`; `EF.Functions.VectorDistance("cosine", a, b)` returns a *distance* in [0,2] (similarity = 1 − d);
the Anthropic SDK's `AsIChatClient(model)` adapter maps `HostedWebSearchTool`; SQL 2025 image healthcheck uses
`/opt/mssql-tools18/bin/sqlcmd ... -C`. Blazor scaffold: `dotnet new blazor -int Server -ai`.

## Solution layout and key types

```
Klipboard\
├─ FaultDesk.sln, Directory.Build.props (net10.0, Nullable, ImplicitUsings), .config/dotnet-tools.json (dotnet-ef 10)
├─ .gitignore, .dockerignore, .env.example, Dockerfile, docker-compose.yml, README.md, CLAUDE.md
├─ docs/ plan.md (copy of this), ai-workflow.md, trade-offs in README, prompts/{triage,clarify,investigate}-v1.md,
│        adr/0001-embedding-storage.md, 0002-provider-agnostic-ai.md, 0003-streamed-markdown-report.md
├─ src/
│  ├─ FaultDesk.Domain (no package refs)
│  │  Common/DomainException, Entity<TId>
│  │  Vehicles/VehicleRegistration (VO: Parse/normalise "ab12cde"→"AB12CDE", Display "AB12 CDE", loose UK format check),
│  │           VehicleDetails (record VO: Make, Model, Variant?, Year, EngineSizeCc?, FuelType), FuelType enum
│  │  Tickets/FaultTicket (aggregate root), TicketId (Guid-backed), TicketStatus (New|InProgress|Resolved),
│  │          CustomerContact (VO: Name?, Contact?), TriageSummary (VO), FaultCategory, Severity, SafeToDrive enums,
│  │          Resolution (VO: Notes, ResolvedAt)
│  │  Diagnostics/Investigation (aggregate, refs TicketId), AiCallMetadata (VO: Provider, Model, PromptVersion,
│  │              InputTokens, OutputTokens, Duration, WebSearchUsed)
│  ├─ FaultDesk.Application (refs Domain + Microsoft.Extensions.AI.Abstractions)
│  │  Abstractions/IFaultTicketRepository, IInvestigationRepository, ITicketEmbeddingStore, ISimilarTicketFinder,
│  │               IVehicleLookupService, IFaultTriageService, IClarifyingQuestionService, IDiagnosticsAssistant
│  │  Vehicles/LookupVehicleHandler
│  │  Tickets/SubmitFaultTicket{Command,Handler,Result}, ListTicketsHandler, GetTicketDetailHandler,
│  │          StartTicketHandler, ResolveTicketHandler, ReopenTicketHandler, TicketEmbeddingText, Dtos/
│  │  Diagnostics/InvestigateTicketHandler, InvestigationUpdate, TriageResult, ClarifyingQuestions (LLM output records)
│  │  Prompts/TriagePromptBuilder, ClarifyPromptBuilder, InvestigationPromptBuilder (pure, `Version` constants)
│  │  DependencyInjection
│  ├─ FaultDesk.Infrastructure (refs Application; EF SqlServer 10, Anthropic, Microsoft.Extensions.AI(.OpenAI))
│  │  Persistence/FaultDeskDbContext, Configurations/{FaultTicket,TicketEmbedding,Investigation}Configuration,
│  │              Entities/TicketEmbedding (TicketId PK/FK, Model, SqlVector<float> Vector, CreatedAt),
│  │              Repositories/Ef*, SqlSimilarTicketFinder, DatabaseInitializer (migrate+retry+seed+backfill),
│  │              Seed/SeedTickets (20 realistic UK tickets with resolutions, fixed Guids), Migrations/
│  │  Ai/AiOptions, AiServiceCollectionExtensions (provider switch), ChatTriageService, ChatClarifyingQuestionService,
│  │     ChatDiagnosticsAssistant, Mock/MockChatClient (IChatClient), Mock/HashingEmbeddingGenerator
│  │  Vehicles/MockVehicleLookupService (≈10 known regs, ~300 ms simulated latency, unknown → not found)
│  │  DependencyInjection
│  └─ FaultDesk.Web (refs Application + Infrastructure; QuickGrid, Markdig)
│     Program.cs, appsettings.json (no secrets), Components/App.razor + Routes.razor,
│     Layout/CustomerLayout, GarageLayout; Pages/Home (/ two doors)
│     Customer/ReportFault (/customer/report, stepper), Submitted (/customer/submitted/{id})
│     Garage/Tickets (/garage), TicketDetail (/garage/tickets/{id}), RelatedTickets, TriageCard,
│            InvestigationPanel, StatusBadge, StatusActions
│     Services/MarkdownRenderer (Markdig, DisableHtml)
└─ tests/FaultDesk.Domain.Tests, tests/FaultDesk.Application.Tests (also refs Infrastructure for Mock AI tests; no DB)
```

**Aggregate behaviour.** `FaultTicket.Create(reg, vehicle, description, contact, now)` validates description
(10–4000 chars) → `New`. `ApplyTriage(TriageSummary)`; `Start()` New→InProgress; `Resolve(notes, now)`
New/InProgress→Resolved (notes required, cannot resolve twice); `Reopen()` Resolved→InProgress. Violations throw
`DomainException`. Use .NET `TimeProvider` rather than a custom clock port.

**Key design decisions (become ADRs):**
- ADR-0001 Embedding lives in an Infrastructure-owned `TicketEmbeddings` table (1:1 with ticket), not on the
  aggregate: `SqlVector<float>` is a SqlClient type (Domain must not reference it), a value-converted `float[]` cannot be
  used in `VectorDistance`, the vector is a derived model-specific artefact (store `Model` beside it), it is best-effort
  (failed embedding never fails a submission; backfilled on boot), and ~6 KB/row should not load on every read.
- ADR-0002 AI behind Application ports in domain language (`IFaultTriageService`, `IClarifyingQuestionService`,
  `IDiagnosticsAssistant`, `IEmbeddingGenerator`); Infrastructure adapters use Microsoft.Extensions.AI `IChatClient`
  from `AnthropicClient.AsIChatClient(model)` (OpenAI Responses client as alternative), or `MockChatClient`. Provider is
  config, not code.
- ADR-0003 AI Investigate streams **markdown with fixed headings** (not JSON): streaming and structured output don't
  mix, and fixed headings give a stable UI and easy persistence.
- AI is always best-effort: triage/clarify/embedding failures are logged and skipped; submit never fails because of AI.

## AI interactions

**Config** (`AiOptions`, section `Ai`): `ChatProvider` = Mock|Anthropic|OpenAI, `EmbeddingProvider` = Mock|OpenAI,
`Anthropic:{ApiKey, Model=claude-opus-5, TriageModel=claude-opus-5}`, `OpenAI:{ApiKey, ChatModel=gpt-5,
EmbeddingModel=text-embedding-3-small}`, `EmbeddingDimensions=1536` (validated at startup, fail fast),
`Investigate:MaxOutputTokens=4000`, `TimeoutSeconds=300`. `AnthropicClient`/`OpenAIClient`/`IChatClient`/
`IEmbeddingGenerator` are singletons. Not using Anthropic's beta server-side refusal fallback: we go through the
Microsoft.Extensions.AI adapter; note this in ADR-0002 as a follow-up if the raw SDK path is ever adopted.

**Mock provider:** `MockChatClient` inspects the request (triage vs clarify vs investigate by a marker in the system
prompt) and returns plausible canned JSON / questions / a headed markdown report streamed with ~30 ms per chunk so
streaming is visible. `HashingEmbeddingGenerator` hashes lower-cased tokens into 1536 buckets and L2-normalises, so
shared vocabulary yields real similarity in demo mode.

**(a) Clarifying questions (customer step)** — `IClarifyingQuestionService.SuggestAsync(vehicle, description, ct)
→ ClarifyingQuestions(string[] Questions)` via `chatClient.GetResponseAsync<ClarifyingQuestions>`. Prompt `clarify-v1`:
"ask 2–3 short questions a service adviser would ask to narrow this fault (when it happens, noises, warning lights,
recent work); plain English; no diagnosis". Customer answers are optional; answered ones are appended to the
description as a "Follow-up answers" block before submit. Failure → skip the step.

**(b) Triage on submit** — `IFaultTriageService.TriageAsync(vehicle, description, ct) → TriageResult?` via
`GetResponseAsync<TriageResult>`; string enums mapped to domain enums with `Unknown` fallback:
`Title (≤80)`, `Category` (Engine|Electrical|Brakes|SteeringSuspension|Transmission|ExhaustEmissions|Cooling|Tyres|
Bodywork|Unknown), `Severity` (Low|Medium|High), `SafeToDrive` (Yes|Caution|No), `Symptoms[]`, `LikelySystems[]`,
`AdviserQuestions[]` (3–5), `CustomerSummary`. Prompt `triage-v1`: "assistant to a UK garage service adviser; extract,
do not diagnose; never invent facts; Unknown when unsure; UK terminology (MOT, DPF, AdBlue)". Shown on the Submitted
page (summary + safe-to-drive advice) and in the garage TriageCard.

**(c) AI Investigate** — `IDiagnosticsAssistant.InvestigateAsync(prompt, ct) → IAsyncEnumerable<InvestigationUpdate
(string? TextDelta, AiCallMetadata? Completed)>` via `GetStreamingResponseAsync(messages, new ChatOptions { Tools =
[new HostedWebSearchTool()], MaxOutputTokens })`; `UsageContent` gives token counts. If the provider rejects hosted
tools, retry once without tools and record `WebSearchUsed=false`. Raw-SDK fallback if the adapter misbehaves:
`client.Messages.CreateStreaming(new MessageCreateParams { Model="claude-opus-5", MaxTokens=4000, Tools=[new
ToolUnion(new WebSearchTool20260209())], ... })` reading `TryPickContentBlockDelta` → `Delta.TryPickText`.
Prompt `investigate-v1` (built by `InvestigationPromptBuilder.Build(ticket, sameVehicleHistory, similarTickets)`):
system = "senior diagnostic technician writing for workshop mechanics; search the web for reported faults matching this
make/model/engine and symptoms; rank causes by likelihood; be specific about checks, tools and expected readings; safety
first; cite every web claim with a URL; do not invent TSBs or recalls"; user = vehicle block, customer description,
triage summary (if any), up to 5 related tickets with resolution notes, and the required skeleton:
`## Summary`, `## Probable causes (ranked)` (cause — likelihood — evidence), `## Diagnostic steps`, `## Parts likely
needed`, `## Safety warnings`, `## Sources` (title + url). Persisted as `Investigation { Id, TicketId, ReportMarkdown,
AiCallMetadata, CreatedAt }` after the stream completes; latest shown on reload; re-run creates a new row.

**Streaming in Blazor Server** (`InvestigationPanel.razor`, `@implements IDisposable`): button starts a
`CancellationTokenSource(4 min)`; `await foreach` appends deltas to a `StringBuilder`; re-render via
`InvokeAsync(StateHasChanged)` throttled to ~100 ms; `OperationCanceledException` swallowed; `Dispose` cancels;
button disabled while running with elapsed time. Markdown rendered with Markdig `DisableHtml()` as `MarkupString`.
Never start AI work from `OnInitializedAsync`.

## Data and persistence

Tables: `FaultTickets` (owned VOs for registration/vehicle/contact/triage/resolution; `Registration` normalised +
indexed), `TicketEmbeddings` (PK/FK TicketId, `Model`, `Vector vector(1536)` via `.HasColumnType("vector(1536)")`,
CreatedAt), `Investigations`. `AddDbContextFactory<FaultDeskDbContext>` (long-lived circuits; a context per operation).

`SqlSimilarTicketFinder.FindSimilarAsync(ticketId, take)`: load the ticket's vector as a `SqlVector<float>` local, then
`TicketEmbeddings.Where(e => e.TicketId != id && e.Model == current).OrderBy(e => EF.Functions.VectorDistance("cosine",
e.Vector, q)).Take(take)` projecting ticket summary + distance. `GetTicketDetailHandler` merges same-registration
history (repository query) with similar tickets, de-duplicated, similarity displayed as `1 − distance`.

`DatabaseInitializer` (hosted service before Kestrel serves): `MigrateAsync` with up to 10 retries / 3 s on
`SqlException`; seed 20 tickets if the fixed seed Guids are absent; embed seeds in one batched `GenerateAsync`; backfill
any ticket whose embedding is missing or whose `Model` ≠ current embedder (≤50 per boot, failures logged not thrown) —
this is what makes switching Mock→OpenAI embeddings "just work".

## Web UI

- `/` landing with two doors (Customer / Garage). Bootstrap from the template; minimal custom CSS.
- `/customer/report` stepper: 1) registration lookup (known mock regs listed as hints; "enter manually" always
  available) → 2) editable vehicle form + optional name/contact → 3) description textarea → "Continue" → 4) clarifying
  questions (optional answers; skip allowed) → Submit → `/customer/submitted/{id}` with reference, "what we understood"
  (triage) and safe-to-drive advice.
- `/garage` QuickGrid over an in-memory list (ref, created, registration, vehicle, title/category, severity, status),
  sortable, filter by status; row click → detail.
- `/garage/tickets/{id}`: vehicle + customer description, TriageCard, StatusActions (Start / Resolve with notes /
  Reopen), RelatedTickets (history + similar with similarity %, resolution notes), InvestigationPanel (button, stream,
  previous investigations with model / prompt version / tokens / duration / web-search flag).

## Docker and secrets

- `Dockerfile` multi-stage: `mcr.microsoft.com/dotnet/sdk:10.0` publish `src/FaultDesk.Web` → `aspnet:10.0`, port 8080.
  `.dockerignore`: bin/obj/.git/.env.
- `docker-compose.yml`: `sqlserver` = `mcr.microsoft.com/mssql/server:2025-latest` (ACCEPT_EULA, MSSQL_SA_PASSWORD from
  `.env`, MSSQL_PID=Developer, named volume, healthcheck `/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P
  "$$MSSQL_SA_PASSWORD" -C -b -Q "SELECT 1"`, interval 10 s, retries 12, start_period 40 s); `app` builds from `.`,
  `depends_on: service_healthy`, env `ConnectionStrings__FaultDesk` (`TrustServerCertificate=True`),
  `Ai__ChatProvider=${AI_CHAT_PROVIDER:-Mock}`, `Ai__EmbeddingProvider=${AI_EMBEDDING_PROVIDER:-Mock}`,
  `Ai__Anthropic__ApiKey=${ANTHROPIC_API_KEY:-}`, `Ai__OpenAI__ApiKey=${OPENAI_API_KEY:-}`.
- `.env.example` with a compliant SA password, Mock providers, empty keys. `.env` git-ignored.
- Dev: `docker compose up sqlserver -d` then `dotnet run --project src/FaultDesk.Web`; keys via
  `dotnet user-secrets set "Ai:Anthropic:ApiKey" ...` on the Web project (same keys as compose).
- README notes: Apple Silicon needs a CU1+ 2025 image (or OrbStack); first `docker compose up` pulls ~2 GB.

## Commit sequence (each builds and tests green)

| # | Commit | Contents |
|---|---|---|
| 0 | `chore: scaffold solution and project structure` | `git init`; sln (`--format sln`), 6 projects + refs, Directory.Build.props, tool manifest, .gitignore, CLAUDE.md, README stub, docs/plan.md |
| 1 | `feat(domain): vehicle value objects and FaultTicket aggregate` | Vehicles/, Tickets/ (status, resolution, triage VO), Diagnostics/, Domain tests |
| 2 | `feat(application): ports, ticket use cases and versioned prompt builders` | ports, handlers, `TicketEmbeddingText`, prompt builders, `TriageResult`/`ClarifyingQuestions`, Application tests with hand-written fakes |
| 3 | `feat(infrastructure): EF Core persistence with SQL Server 2025 vector search` | DbContext, configurations, migration `Initial`, repositories, `SqlSimilarTicketFinder`, `DatabaseInitializer`, mock vehicle lookup, compose with `sqlserver` only |
| 4 | `feat(ai): provider-switchable chat and embeddings with zero-key mock mode` | AiOptions, DI switch, MockChatClient, HashingEmbeddingGenerator, user-secrets id, mock tests |
| 5 | `feat(web): customer portal for reporting a fault` | landing, ReportFault stepper (without clarify step), Submitted page |
| 6 | `feat(web): garage portal with ticket grid, history and similar tickets` | QuickGrid, TicketDetail, RelatedTickets |
| 7 | `feat(seed): 20 historical tickets with resolutions` | seeder + backfill wired into DatabaseInitializer |
| 8 | `feat(diagnostics): streamed AI Investigate with web search, persisted` | ChatDiagnosticsAssistant, InvestigateTicketHandler, InvestigationPanel, `Investigations` migration |
| 9 | `feat(deploy): Dockerfile, compose app service and run docs` | **MVP cut line** — README build/run/config sections |
| 10 | `feat(triage): AI triage on submit shown to customer and garage` | ChatTriageService in submit, TriageCard, Submitted page summary |
| 11 | `feat(tickets): status workflow with resolution notes` | Start/Resolve/Reopen handlers + StatusActions |
| 12 | `feat(customer): AI clarifying questions before submit` | ChatClarifyingQuestionService + stepper step |
| 13 | `feat(ai): record token usage and prompt version per AI call` | AiCallMetadata surfaced in InvestigationPanel |
| 14 | `docs: ADRs, AI workflow notes, prompts and trade-offs` | docs/adr, docs/ai-workflow.md (per-commit log of how Claude Code was used, incl. where it was wrong), docs/prompts, README trade-offs/unfinished section, screenshots if cheap |

If time runs short: drop the OpenAI chat provider from #4 (keep OpenAI embeddings), make #8 non-streaming first, and
skip #12/#13; README documents whatever is left out.

## Tests (xUnit, no DB)

- Domain: `VehicleRegistrationTests` (normalises case/space/hyphen, Display spacing, rejects empty/garbage, accepts
  current/prefix/suffix formats, equality); `VehicleDetailsTests` (year range, engine size > 0); `FaultTicketTests`
  (Create → New; short description rejected; ApplyTriage; Start; Resolve requires notes; Resolve twice throws; Reopen).
- Application: `SubmitFaultTicketHandlerTests` (normalised reg persisted; triage applied; triage failure doesn't fail
  submit; embedding generated from `TicketEmbeddingText` and stored; embedding failure doesn't fail submit);
  `TicketEmbeddingTextTests`; `TriagePromptBuilderTests`; `InvestigationPromptBuilderTests` (includes triage when
  present, caps related tickets at 5, includes resolution notes and citation instruction, version constant);
  `GetTicketDetailHandlerTests` (merge, de-dupe, excludes self); `HashingEmbeddingGeneratorTests` (deterministic,
  1536 dims, unit norm, related texts closer); `MockChatClientTests` (triage JSON parses; report has all headings).

## Gotchas to pre-empt

- Migration must emit `vector(1536)`: inspect `dotnet ef migrations script`; design-time host must build with Mock AI and
  no keys. `dotnet ef migrations add Initial -p src/FaultDesk.Infrastructure -s src/FaultDesk.Web`.
- `VectorDistance` errors on dimension mismatch → filter by `Model`, validate dims at startup.
- Prerender double execution → global `InteractiveServerRenderMode(prerender: false)`; AI only from click handlers.
- Never bind an EF `IQueryable` to QuickGrid; bind a list. DbContext via factory per operation.
- LLM markdown → Markdig `DisableHtml()`.
- Connection string needs `TrustServerCertificate=True`; compose healthcheck needs `$$` and `-C`.
- Structured-output enums as strings with `Unknown` fallback; `GetResponseAsync<T>` needs public settable/positional record members.
- Long calls: 5-min client timeout, 4-min UI cap, button disabled while running.
- Seeding: one batched embed call; guarded by fixed Guids so it runs once; backfill bounded.
- Secrets: only `.env.example` / user-secrets; never log `AiOptions`.

## Verification

1. After each commit: `dotnet build FaultDesk.sln` and `dotnet test FaultDesk.sln` green; `git log --oneline` shows the increment.
2. After #3: `docker compose up sqlserver -d`, `dotnet run --project src/FaultDesk.Web` → migrations apply; confirm the
   `TicketEmbeddings.Vector` column type is `vector(1536)` in the migration script.
3. After #6/#7 (Mock mode): in the built-in browser, submit a ticket for a known reg and a manual vehicle; open `/garage`,
   confirm grid, detail, same-vehicle history and similar tickets (seeded, with similarity %).
4. After #8 (Mock): press AI Investigate, see streamed headed report, reload and see it persisted with metadata.
5. After #9: `docker compose up --build` from a clean state → app on http://localhost:8080 works end-to-end in Mock mode.
6. Real providers: set `Ai:Anthropic:ApiKey` and `Ai:OpenAI:ApiKey` via user-secrets (user supplies keys; they are never
   committed), `Ai:ChatProvider=Anthropic`, `Ai:EmbeddingProvider=OpenAI`; verify boot backfill re-embeds seeds, triage
   populates, clarifying questions appear, Investigate streams with real web sources and token counts.
7. `git grep -i "sk-ant\|sk-proj"` returns nothing; `.env` absent from `git status`.

## Open items for the user (not blocking)

- Provide the two API keys locally (user-secrets) when we reach real-provider verification; never paste them into chat/files that get committed.
- Whether to create a GitHub remote and push (reviewers) — user's call after the history exists.
- `Ai:Anthropic:TriageModel=claude-sonnet-5` is a one-line config change if submit latency with Opus 5 feels slow.
