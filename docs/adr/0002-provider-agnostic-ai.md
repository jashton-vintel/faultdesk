# ADR-0002: AI capabilities are application ports, implemented once over Microsoft.Extensions.AI

**Status:** accepted (commits 2 and 4)

## Context

The app uses a language model for three things (triage on submit, clarifying questions, the investigation report)
and an embedding model for similarity search. Reviewers will not all have the same API keys, and the assessment should
run with none at all.

## Decision

- The Application layer defines the capabilities in domain language: `IFaultTriageService`,
  `IClarifyingQuestionService`, `IDiagnosticsAssistant`, `IEmbeddingService`. It has no dependency on any AI SDK.
- Infrastructure implements each port once against Microsoft.Extensions.AI's `IChatClient` /
  `IEmbeddingGenerator`. Which client sits behind the abstraction is chosen from configuration
  (`Ai:ChatProvider`, `Ai:EmbeddingProvider`): the official Anthropic SDK's `AsIChatClient`, the OpenAI Responses
  client, or a `MockChatClient`.
- `Mock` is the default. It answers every FaultDesk prompt with keyword-driven canned content in exactly the shape the
  real model returns, streamed in chunks, and a hashing embedder produces deterministic vectors, so the whole
  application (including vector search) works with zero keys.
- Structured outputs use the provider's JSON-schema mode first and fall back to describing the schema in the prompt.
- AI is best-effort everywhere: failures are logged and the user flow continues.

## Consequences

- Adding a provider is a few lines in `AiServiceCollectionExtensions`, not a new implementation of each port.
- Prompts are provider-neutral and live in the Application layer with a version constant that is persisted with
  every output.
- Anthropic's server-side refusal-fallback beta is not used because the request goes through the
  Microsoft.Extensions.AI adapter rather than the raw messages API. If a raw-API path is ever added for
  investigations (for citation blocks, say), enable it there.
- The mock's embeddings are lexical, not semantic; the README says so.
