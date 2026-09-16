# ADR-0001: Ticket embeddings live in an infrastructure-owned table, not on the aggregate

**Status:** accepted (commit 3)

## Context

Similar-ticket search needs a vector per ticket, stored in SQL Server 2025's native `vector(1536)` column and
queried with `VECTOR_DISTANCE`. EF Core 10 maps that column to `SqlVector<float>` from `Microsoft.Data.SqlClient`.
The obvious place for the vector is a property on `FaultTicket`.

## Decision

The vector is stored in a separate `TicketEmbeddings` table (one row per ticket, keyed by the ticket id, with the
embedding model's id beside it), owned entirely by the Infrastructure project. The domain never sees it. The
Application layer talks to it through two ports: `ITicketEmbeddingStore` (write) and `ISimilarTicketFinder` (query).

## Consequences

- `SqlVector<float>` is a database-driver type; keeping it out of the Domain project keeps the domain free of package
  references. A value-converted `float[]` on the aggregate would not work either, because `EF.Functions.VectorDistance`
  needs the real vector type.
- The vector is a derived, model-specific artefact. Storing the model id with it means vectors from different
  embedding models are never compared, and switching provider simply re-embeds on the next start-up
  (`EmbeddingBackfiller`).
- Embedding is best-effort: a failed call never fails a customer submission and is repaired later.
- Reading a ticket does not drag 6 KB of floats along with it.
- Cost: one extra table and one join-free subquery in the similarity finder.
