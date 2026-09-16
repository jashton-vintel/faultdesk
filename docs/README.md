# Documentation index

| Document | Read it for |
|---|---|
| [plan.md](plan.md) | The plan agreed before coding started: decisions, architecture, commit sequence, verification. Kept verbatim so the history can be compared against it. |
| [ai-workflow.md](ai-workflow.md) | How AI (Claude Code) was used to build this, the split between human decisions and AI work, and the table of things the AI got wrong and how each was caught. |
| [adr/0001-embedding-storage.md](adr/0001-embedding-storage.md) | Why the vector lives in its own infrastructure-owned table, not on the ticket aggregate. |
| [adr/0002-provider-agnostic-ai.md](adr/0002-provider-agnostic-ai.md) | Why AI capabilities are application ports over Microsoft.Extensions.AI, and why a zero-key mock mode exists. |
| [adr/0003-streamed-markdown-report.md](adr/0003-streamed-markdown-report.md) | Why the investigation report is streamed markdown with fixed headings rather than JSON. |
| [prompts/triage-v1.md](prompts/triage-v1.md) | The triage prompt and its output schema. |
| [prompts/clarify-v1.md](prompts/clarify-v1.md) | The follow-up questions prompt. |
| [prompts/investigate-v1.md](prompts/investigate-v1.md) | The investigation prompt, required headings and how related tickets are folded in. |
| [screenshots/](screenshots) | Home, ticket grid, ticket page (desktop and tablet), report form, confirmation. |

The prompt files mirror the code in `src/FaultDesk.Application/Prompts`; the version id in each file name is stored
with every AI output the app produces.
