# How AI was used to build FaultDesk

The brief asked for an application that shows how AI fits into a development workflow. This is the honest account.
FaultDesk was built in one session with **Claude Code** (Anthropic's agentic coding tool) driving the editor, shell,
browser and git, with the candidate directing the design and reviewing every increment. Every commit carries a
`Co-Authored-By` trailer for that reason.

## Division of labour

**Candidate (decisions and review)**

- Set the constraints up front: C# / .NET 10 Blazor Server, SQL Server 2025 with vector search, a domain-driven
  project split, xUnit without overkill, an incremental commit history, secrets only via environment/user-secrets.
- Chose the AI shape when asked: Claude (Opus 5) for reasoning with its web search tool, OpenAI for embeddings, both
  behind ports so the provider is configuration; a zero-key mock mode so reviewers can run it; Docker Compose for
  deployment; which extras were worth the time (triage on submit, clarifying questions, workflow with resolution
  notes, seeded history, token/prompt provenance) and which were not (real DVLA lookup).
- Reviewed the plan before any code was written, and the result of each increment as it landed.

**Claude Code (research, code, verification)**

- Researched the moving parts before planning: EF Core 10's native `SqlVector<float>` support and
  `EF.Functions.VectorDistance`, the SQL Server 2025 container image and its Apple Silicon caveat, the official
  Anthropic C# SDK's `IChatClient` adapter and hosted web search support, Microsoft.Extensions.AI structured output
  semantics. Two things it verified turned out to matter: SDK 10 creates `.slnx` solutions by default, and the
  machine's global `dotnet-ef` was a 9.x tool, so the repo pins a local 10.x tool manifest.
- Wrote a plan (kept verbatim in [plan.md](plan.md)), then the code, tests, Dockerfile and docs, in the commit order
  the plan set out.
- Verified each step rather than assuming: `dotnet build` and `dotnet test` before every commit, the generated
  migration script inspected for `vector(1536)`, the app driven through its own browser pane (registration lookup,
  submit, grid, ticket page, streamed investigation, workflow), rows checked with `sqlcmd` inside the container
  (`VECTOR_NORM` of the stored embedding), and the compose stack exercised on port 8080.

## Where the AI was wrong, and how it was caught

Listing these is the point of the exercise: the value of AI in the loop comes with a verification habit.

| Step | What went wrong | How it surfaced | Fix |
|---|---|---|---|
| Scaffold | Tool manifest was created at the repo root, not `.config/` | Listing the tree | Moved it |
| Domain | A single large shell script of heredocs failed to parse | Shell error, nothing written | Switched to writing files directly |
| Persistence | The domain rule "reopen clears the resolution" does not survive EF's detached-aggregate update for an optional owned type | Reasoning through the EF mapping while writing it | Changed the rule: the last resolution is kept as history (better UX anyway); test updated |
| AI adapters | `GetOpenAIResponseClient(model)` no longer exists in OpenAI 2.13; `IConfigurationSection.Get<T>` needed the Binder package | Compiler errors | Read the package XML docs: `GetResponsesClient().AsIChatClient(model)`; added the package |
| Customer portal | An "unhandled error" banner appeared on every page | Browser check | The `display:none` rule for the banner lived in the scoped layout CSS that had been deleted; restored in `app.css` |
| Garage portal | A Razor component named `TicketDetail` shadowed the Application DTO of the same name | Compiler errors | Renamed the page component |
| Docker | The published container served no `_framework/blazor.web.js` (404), so the page was blank | Browser network log, then inspecting the build stage: the framework-assets package was never restored | Restore had run from csproj files alone, before the `.razor` sources were copied, so the SDK did not treat it as a Blazor project; the Dockerfile now restores with the full source present |
| Demo mode | A flashing engine light was filed under Electrical by the mock | Walking the demo flow in the browser | A dedicated engine-management rule |

Nothing in the table was found by a unit test; all of it was found by running the real thing. The unit tests
protect the domain rules, the handlers' best-effort behaviour and the prompt contents.

## Prompts as code

The three prompts live in the Application layer with a version constant that is stored with every output
(`AiCallMetadata.PromptVersion`), and are mirrored in [prompts](prompts). Changing a prompt means bumping the
version, so a report can always be traced to the exact wording that produced it. The structured-output types use
strings for enums on purpose: a model answer that is slightly off degrades to `Unknown` rather than failing.

## What the same workflow would do next

- Run the real providers against a larger sample of descriptions and turn the outputs into an eval set for the
  triage prompt (category and safe-to-drive agreement with a human adviser).
- Parse the provider's citation blocks for the investigation report instead of trusting the model's own
  `## Sources` list.
- Add a small integration test against the SQL Server container for the vector query.
