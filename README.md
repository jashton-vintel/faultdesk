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

## Trade-offs and what is not done

See the end of this file once the final documentation commit lands; the short version: no authentication (two areas in
one app), a mock registration lookup instead of the DVLA API, no integration tests against SQL Server, and the demo
embeddings are lexical rather than semantic.
