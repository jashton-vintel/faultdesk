# FaultDesk

A garage receives free-text descriptions of vehicle faults from customers. FaultDesk turns them into something a
service adviser and the workshop can act on:

- **Customer portal**: enter a registration (looked up, or entered manually), describe the problem in your own words,
  answer a couple of AI-suggested follow-up questions, submit.
- **AI triage on submit**: title, fault category, severity, safe-to-drive flag, symptoms and questions for the adviser.
- **Garage portal**: every ticket in a sortable grid; each ticket shows the vehicle's history and *similar faults the
  garage has seen before*, found with SQL Server 2025 vector search over the descriptions and their resolutions.
- **AI Investigate**: one click builds a prompt from everything known about the ticket, asks Claude to search the web
  for matching faults on that make/model, and streams back a ranked, cited investigation plan for the mechanics.
- **Workflow**: New → In progress → Resolved, with the workshop's resolution notes feeding future suggestions.

Built for a technical assessment in .NET 10 / Blazor Server with a domain-driven project layout, and developed with
Claude Code. See [docs/plan.md](docs/plan.md) for the plan the work followed and [docs](docs) for the ADRs, prompts and
notes on how AI was used during development.

## Quick start (Docker, no API keys needed)

Prerequisites: Docker Desktop (or another Docker engine with Compose v2). On Apple Silicon the SQL Server 2025 image
needs the CU1-or-later tag that `2025-latest` resolves to; if it fails to start, use OrbStack or a recent Docker Desktop.

```bash
cp .env.example .env
docker compose up --build
```

Then open <http://localhost:8080>. The first run pulls the SQL Server 2025 and .NET images (roughly 2 GB), creates the
database, applies the EF Core migrations and seeds 20 historical tickets so the garage portal has history from the start.

Everything works in **demo mode** without keys: triage, follow-up questions and AI Investigate return realistic canned
answers, and similarity search uses deterministic hashed vectors. To use real models, set the providers and keys in
`.env` (see below) and run `docker compose up --build` again.

## Running for development

```bash
cp .env.example .env                 # once
docker compose up sqlserver -d       # SQL Server 2025 on localhost:1433
dotnet run --project src/FaultDesk.Web
```

The app is on <http://localhost:5276>. `appsettings.Development.json` points at the compose SQL Server using the
default password from `.env.example`; change both if you change the password.

Build and test:

```bash
dotnet build FaultDesk.sln
dotnet test FaultDesk.sln
```

EF Core migrations (the local tool manifest pins `dotnet-ef` 10; run `dotnet tool restore` first):

```bash
dotnet dotnet-ef migrations add <Name> -p src/FaultDesk.Infrastructure -o Persistence/Migrations
```

Migrations are applied automatically at start-up.

## Configuring the AI providers

| Setting | Values | Default |
|---|---|---|
| `Ai:ChatProvider` | `Mock`, `Anthropic`, `OpenAI` | `Mock` |
| `Ai:EmbeddingProvider` | `Mock`, `OpenAI` (Anthropic has no embeddings API) | `Mock` |
| `Ai:Anthropic:Model` / `Ai:Anthropic:TriageModel` | any Claude model id | `claude-opus-5` |
| `Ai:OpenAI:ChatModel` / `Ai:OpenAI:EmbeddingModel` | any OpenAI model ids | `gpt-5` / `text-embedding-3-small` |

The recommended real configuration is **Anthropic for reasoning + OpenAI for embeddings**. Keys are never stored in
`appsettings.json`:

- **Docker**: put them in `.env` (git-ignored):
  `AI_CHAT_PROVIDER=Anthropic`, `AI_EMBEDDING_PROVIDER=OpenAI`, `ANTHROPIC_API_KEY=...`, `OPENAI_API_KEY=...`
- **`dotnet run`**: use user-secrets (or the `ANTHROPIC_API_KEY` / `OPENAI_API_KEY` environment variables):

  ```bash
  cd src/FaultDesk.Web
  dotnet user-secrets set "Ai:ChatProvider" "Anthropic"
  dotnet user-secrets set "Ai:EmbeddingProvider" "OpenAI"
  dotnet user-secrets set "Ai:Anthropic:ApiKey" "sk-ant-..."
  dotnet user-secrets set "Ai:OpenAI:ApiKey" "sk-..."
  ```

Switching embedding provider is safe: on the next start-up every ticket without a vector for the new model is
re-embedded (vectors from different models are never compared).

## How it is put together

```
src/FaultDesk.Domain          aggregates and value objects, no dependencies
src/FaultDesk.Application     use-case handlers, ports, DTOs, versioned prompt builders
src/FaultDesk.Infrastructure  EF Core + SQL Server 2025 (vector column), AI adapters, mock lookup, seeding
src/FaultDesk.Web             Blazor Server UI (customer and garage areas) and the composition root
tests/                        xUnit: domain invariants, handlers with fakes, prompts, mock AI
```

Dependencies point inwards (`Web -> Infrastructure -> Application -> Domain`). The AI capabilities are ports in the
application layer (`IFaultTriageService`, `IClarifyingQuestionService`, `IDiagnosticsAssistant`, `IEmbeddingService`)
implemented once against Microsoft.Extensions.AI's `IChatClient`, so the provider is configuration rather than code.
AI is best-effort everywhere: if triage, questions or embedding fail, the ticket is still stored and the failure is logged.

Key design decisions are recorded as ADRs in [docs/adr](docs/adr).

## Where the AI is used

| Moment | Prompt | Output |
|---|---|---|
| Customer clicks *Continue* after describing the problem | `clarify-v1` | 2-3 follow-up questions, answers appended to the description |
| Customer submits | `triage-v1` | structured triage (JSON schema) stored on the ticket |
| Garage clicks *AI Investigate* | `investigate-v1` + hosted web search | streamed markdown report, persisted with model, prompt version and token usage |
| Every submit and resolution | embedding model | vector for similarity search (`VECTOR_DISTANCE('cosine')` in SQL Server 2025) |

Prompt text lives in `src/FaultDesk.Application/Prompts` with a version constant that is stored alongside every output.

## How AI was used to build it

[docs/ai-workflow.md](docs/ai-workflow.md) describes the split between the candidate's decisions and Claude Code's
research, code and verification, and lists the places the AI got it wrong and how each was caught. The commit history
follows the plan in [docs/plan.md](docs/plan.md) one increment at a time; the ADRs are in [docs/adr](docs/adr) and the
prompts in [docs/prompts](docs/prompts).

## Trade-offs and what is not done

Deliberate scope decisions for a one-evening build, in the order a reviewer is most likely to notice them:

- **No authentication.** Customer and garage areas live in one Blazor app; in production they would be separate
  deployables or at least separate authorisation policies.
- **Mock registration lookup.** Ten known registrations, everything else goes to manual entry. A DVLA Vehicle Enquiry
  Service adapter would sit behind `IVehicleLookupService` without touching anything else.
- **Demo embeddings are lexical.** The zero-key mode hashes words and bigrams, so "similar" means shared vocabulary.
  Real embeddings (`Ai:EmbeddingProvider=OpenAI`) are semantic; switching re-embeds every ticket on the next start.
- **Sources are the model's own list.** The investigation prompt demands a `## Sources` section with URLs; the
  provider's citation blocks are not parsed. Suggestions are a starting point for a technician and the UI says so.
- **No integration tests against SQL Server.** The vector column, `VECTOR_DISTANCE` query and migrations were verified
  by running the app against the container; a Testcontainers-based test is the obvious next addition.
- **Exact vector search.** With a few thousand tickets a full `VECTOR_DISTANCE` scan is fine; a DiskANN vector index
  would be the step after that.
- **No cost controls.** Every submission calls the model twice (questions, triage) and every investigation once with
  web search; investigations are persisted so re-running is a choice, but there is no rate limiting or budget.
- **Single time zone.** Times are shown in the server's local time (UTC inside the container).
- **Anthropic's refusal-fallback beta is not enabled** because requests go through the Microsoft.Extensions.AI adapter
  rather than the raw messages API (see ADR-0002).

Everything in the plan was implemented; the items above are the edges that were consciously left rough.
