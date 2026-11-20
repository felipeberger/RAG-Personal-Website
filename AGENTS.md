# AGENTS.md: tools/ingest/

C# console tool that turns the chunk files in `data/` into `data/index.json`: it validates chunks, embeds `title + "\n" + text` with Azure OpenAI, and writes the index.

## Commands

```bash
# From repo root
dotnet run --project tools/ingest -- --data data --dry-run   # validate only, no Azure calls
dotnet run --project tools/ingest -- --data data             # embed and write index.json
dotnet run --project tools/ingest -- --data data --force     # re-embed everything

# Tests
dotnet test tests/ingest.tests/ingest.tests.csproj                    # all tests
dotnet test tests/ingest.tests/ingest.tests.csproj --filter "ValidateTests"  # specific class
```

Options: `--data`, `--out`, `--endpoint`, `--deployment`, `--dry-run`, `--force`, `--help`.

Config comes from `AZURE_OPENAI_ENDPOINT` and `AZURE_OPENAI_EMBEDDING_DEPLOYMENT` (or the matching flags). Authentication is `DefaultAzureCredential` (`az login` locally). The account needs the Cognitive Services OpenAI User role.

## Structure

- `Ingest.cs`: everything: option parsing, loading, validation, incremental embedding, atomic write, and the record types (`RawChunk`, `Chunk`, `IndexedChunk`, `IndexFile`, `Options`).
- `Ingest.csproj`: SDK-style project. Packages: `Azure.AI.OpenAI`, `Azure.Identity`, `Microsoft.Extensions.AI.OpenAI`.
- `tests/ingest.tests/`: xUnit test project, covering options parsing, validation, loading, hashing, and index loading.

## Data

Source chunks live in `data/`:
- `cv-chunks.json`
- `story-chunks.json`
- `consulting-projects-chunks.json`
- `consulting-cv-chunks.json`
- `personal-project-chunks.json`

Output: `data/index.json` (committed; regenerate after editing chunks)

## CI

GitHub Actions workflow at `.github/workflows/ci.yml`:
- `build-and-test`: restore, build, test, dry-run validation
- `validate-ingest`: parallel job for dry-run without Azure creds
- Runs on push/PR to main, uses .NET 10 on ubuntu-24.04

## How it behaves (keep these properties)

- **Validate before spending money.** Errors (duplicate or empty ids, empty text, invalid JSON) stop the run with exit code 1 before any Azure call. Warnings do not stop it.
- **Incremental.** A chunk is re-embedded only when its hash changes. The hash covers the deployment name and the embedded input (`title + text`). Changing `tags`, `related`, `org` or `period` must not trigger re-embedding.
- **Removed chunks disappear** from the index on the next run.
- **Atomic write.** Write to `index.json.tmp`, then move over the real file.
- **Consistent vectors.** All vectors must have the same dimension, otherwise fail and suggest `--force`.
- **Exit codes:** `0` success, `1` validation or config error, `2` Azure authentication or request failure.

## Conventions

- Target the SDK version in the csproj; nullable reference types and implicit usings are on.
- Keep it dependency-light. Do not add a CLI-parsing or DI framework for this tool.
- Never accept or log an API key. Use `DefaultAzureCredential` only.
- Use the same JSON conventions as the API loader: camelCase keys on write, case-insensitive on read.

## Ask first

- Changing what gets embedded (`EmbedInput`): it invalidates every stored vector and changes retrieval quality.
- Changing the `index.json` shape: the API loader has a matching `IndexFile` / `IndexedChunk` model that must change in the same pull request.
- Changing `BatchSize` or retry behaviour, because Azure quotas are involved.

## Before finishing

Run `dotnet build` and a `--dry-run` against `data/`. If you changed the index format, also run the tool for real and confirm the API still loads the result.
If `title`, `text`, `tags`, `related`, `org`, `period` or `type` of any chunk changed, re-run ingest and commit `data/index.json` in the same commit.
