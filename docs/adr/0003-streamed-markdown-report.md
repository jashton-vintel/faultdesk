# ADR-0003: AI Investigate streams markdown with fixed headings rather than returning JSON

**Status:** accepted (commit 8)

## Context

The investigation report is the longest model output in the app (up to a few thousand tokens) and it uses web
search, so it takes tens of seconds. Triage uses structured JSON output; the same could be done for the report.

## Decision

The report is requested as markdown with six mandatory headings (`Summary`, `Probable causes (ranked)`,
`Diagnostic steps`, `Parts likely needed`, `Safety warnings`, `Sources`) and streamed into the page as it is
generated. The finished text is persisted as-is with its provenance (provider, model, prompt version, token usage,
duration, web-search flag).

## Consequences

- Streaming JSON cannot be shown to the user until it is complete; streaming markdown can, so the mechanic sees the
  summary and first causes within seconds.
- The fixed headings give the UI a stable shape and give the prompt tests something to assert on, without parsing.
- The report is rendered with Markdig with raw HTML disabled, because the model has read arbitrary web pages.
- Sources are whatever the model lists under `## Sources`; citation blocks from the provider are not parsed.
  This is the main thing a raw-API implementation would improve.
