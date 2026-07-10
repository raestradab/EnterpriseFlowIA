# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

EnterpriseFlow AI — a multi-tenant SaaS platform (project/client/team management with an embedded AI assistant) built as a technical portfolio piece on .NET 8 + Vue 3, following Clean Architecture, tactical DDD, CQRS, and Vertical Slice Architecture.

The project is built incrementally across **Releases**, each fully documented before the next starts. Before assuming something is missing, check `docs/` — the roadmap in `docs/02-roadmap.md` and the backlog in `docs/backlog/epics.md` likely already describe it with a Release assigned (or explicitly deferred). Every non-trivial technical decision has a corresponding ADR in `docs/adr/` (indexed in `docs/adr/README.md`) — read the relevant ADR before revisiting a decision it already justified.

## Commands

### Backend (.NET)

```bash
dotnet build EnterpriseFlow.slnx
dotnet test EnterpriseFlow.slnx

# Single test project
dotnet test tests/EnterpriseFlow.Domain.UnitTests/EnterpriseFlow.Domain.UnitTests.csproj

# Single test / filtered by name
dotnet test EnterpriseFlow.slnx --filter "FullyQualifiedName~ClassName"

# Format check (part of the quality gate — run before considering work done)
dotnet format EnterpriseFlow.slnx --verify-no-changes

# Coverage
dotnet test EnterpriseFlow.slnx --settings coverlet.runsettings --collect:"XPlat Code Coverage" --results-directory ./coverage-results
reportgenerator -reports:"coverage-results/**/coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:"Html;TextSummary"
```

Non-test projects have `TreatWarningsAsErrors=true` (`Directory.Build.props`) and run StyleCop — analyzer violations fail the build, not just warn. Test projects (any csproj whose name ends in `Tests`) are exempt from both. `coverlet.runsettings` excludes EF Core migrations and third-party SDK wrappers that need a real external account/API key to exercise meaningfully (cloud storage providers, SMTP, OpenAI/Anthropic clients) — their pure logic (mappers, translation code) stays covered; only the thin network-calling wrapper is excluded, and only with an inline justification comment.

### Frontend (Vue 3 + Vuetify, `src/EnterpriseFlow.Web/`)

```bash
cd src/EnterpriseFlow.Web
npm install
npm run dev      # dev server at :5173, proxies /api and /hubs (ws) to the API at :5050
npm run build    # vue-tsc -b && vite build — this IS the type-check gate; no separate lint/type-check script, no ESLint config
```

No frontend test framework is configured (no Vitest/Cypress/Playwright) — don't assume one when working here.

### Running the stack

```bash
# Everything via Docker Compose (SQL Server + Api + Frontend, migrations applied automatically on startup)
cp .env.example .env   # set SA_PASSWORD and JWT_SIGNING_KEY (32+ chars)
docker compose up --build

# Or individually, against a local SQL Server / LocalDB:
dotnet run --project src/EnterpriseFlow.Api
cd src/EnterpriseFlow.Web && npm run dev
```

### EF Core migrations

```bash
dotnet ef migrations add <Name> --project src/EnterpriseFlow.Infrastructure --startup-project src/EnterpriseFlow.Api --output-dir Persistence/Migrations
dotnet ef database update --project src/EnterpriseFlow.Infrastructure --startup-project src/EnterpriseFlow.Api
```

## Architecture

### Layering (backend)

```
src/EnterpriseFlow.Domain          # Entities, invariants, domain events. Zero dependencies.
src/EnterpriseFlow.Application     # Use cases (Vertical Slices) via MediatR/CQRS + FluentValidation. Depends only on Domain.
src/EnterpriseFlow.Infrastructure  # EF Core, persistence, external service implementations. Depends on Application.
src/EnterpriseFlow.Api             # ASP.NET Core Minimal APIs, DI composition, Swagger.
```

Dependency direction (Domain ← Application ← Infrastructure/Api) is enforced by `tests/EnterpriseFlow.Architecture.Tests` (NetArchTest) — a build-breaking rule, not a convention. Application references only EF Core's provider-agnostic `Microsoft.EntityFrameworkCore` package (`DbSet<T>`/`IQueryable`), never `Microsoft.EntityFrameworkCore.SqlServer` — the concrete provider is Infrastructure's concern. Rationale for combining Clean Architecture with Vertical Slices: ADR-0002.

Application features live under `Features/<Area>/<UseCase>/` — one folder per Command/Query, containing the request record, its handler, and (for commands) a FluentValidation validator. There is no separate repository layer; handlers depend on `IAppDbContext` (an interface Application owns, implemented by Infrastructure's `AppDbContext`) directly.

### MediatR pipeline behaviors (in order)

`AuthorizationBehavior → ValidationBehavior → CachingBehavior → CacheInvalidationBehavior`. Requests needing permission checks implement `IRequirePermission` (a `RequiredPermission` string checked against the caller's JWT-derived permissions); requests with no such requirement — because they're inherently scoped to "my own data" (e.g. `GetMyNotificationsQuery`, `GetMyCalendarQuery`) — simply don't implement it. Cacheable queries implement `ICacheableQuery`; mutations that need to invalidate cached entries implement `IInvalidatesCache` (ADR-0012).

### Multi-tenancy

Every tenant-scoped entity implements `ITenantScoped` (a `TenantId` property). `AppDbContext` applies a global EF Core query filter to all of them via reflection (not hand-written per entity) — see `AppDbContext`'s model-building code. `ICurrentTenantService`/`ICurrentUserService` (Infrastructure/Identity) resolve tenant/user/permissions from validated JWT claims. ADR-0003 covers the strategy; ADR-0006 covers the one deliberate exception (auth endpoints bypass the tenant filter, since a login request has no tenant context yet).

### Domain events

Entities raise events via `protected void Raise(IDomainEvent)` (`BaseEntity`). `IDomainEvent` has no MediatR dependency (Domain must stay framework-free) — events are wrapped in `DomainEventNotification<TDomainEvent>` (`Application/Common`) and published via `IPublisher` after a successful `SaveChangesAsync` (see `AuditableEntitySaveChangesInterceptor`). Handlers subscribe as `INotificationHandler<DomainEventNotification<TEvent>>`. This is the mechanism for cross-feature reactions without direct coupling (e.g., a workflow transition triggering a notification) — never add a second messaging mechanism for this; extend this one.

### Cross-aggregate invariants ("hecho inyectado" pattern, ADR-0005)

When one aggregate's operation depends on a fact that lives in another aggregate (e.g., "is this workflow transition allowed"), Application resolves that fact first (by querying the other aggregate) and passes it into the entity method as a plain value/bool — the entity method itself has no knowledge of the other aggregate. Cross-aggregate references are plain `Guid` fields with **no physical FK** (documented per-case in `docs/06-base-de-datos.md`) — referential integrity is enforced in Application, not the database, because the referenced aggregate is independent.

### Provider abstraction pattern

Used whenever a capability has multiple real, swappable implementations (cloud document storage, AI chat/embeddings): an interface in `Application/Abstractions`, concrete implementations in `Infrastructure`, selected via configuration in `Infrastructure/DependencyInjection.cs` (never a runtime `if` in Application code). Each has a graceful-degradation `Null*` fallback (`NullEmailQueue`, `NullAiChatClient`, `NullEmbeddingClient`) registered when nothing is configured, so the app never crashes for lack of external credentials — it degrades visibly instead (e.g. `NullAiChatClient` returns an explanatory message rather than throwing). Exactly one implementation of a given interface is ever registered per running instance.

### AI assistant / RAG (ADR-0013, ADR-0014)

The assistant's "tools" (`Application/Features/Assistant/AssistantToolCatalog.cs`) are thin wrappers over *existing* Application Queries — never raw SQL, never a new code path bypassing the normal `AuthorizationBehavior`/tenant-filter pipeline. This is the load-bearing security property: the model can never see data the calling user couldn't already see through the regular UI. Adding a new tool means adding a Query-wrapping case in `SendAssistantMessageCommandHandler.InvokeToolAsync`, not a new access path. RAG has no dedicated vector store — embeddings are a `byte[]` column on `DocumentChunk`, and similarity search is plain cosine similarity computed in Application code over the tenant's own rows (query-filtered first, compared second) — see ADR-0014 for why a dedicated vector DB was rejected at this scale.

### Testing conventions

- `EnterpriseFlow.Api.IntegrationTests` uses **SQLite** (via `CustomWebApplicationFactory`), not EF Core's InMemory provider — SQLite goes through real SQL translation, which InMemory doesn't guarantee, and this has caught real bugs (e.g. SQLite rejects `ORDER BY` over a `DateTimeOffset` column; always materialize with `ToListAsync()` before sorting by a `DateTimeOffset` field, never sort inside the LINQ-to-Entities query).
- Domain-event side effects (realtime push, email, AI chat/embeddings calls) are verified against recording `Fake*` test doubles registered in `CustomWebApplicationFactory` (`FakeRealtimeNotifier`, `FakeEmailQueue`, `FakeAiChatClient`, `FakeEmbeddingClient`) — asserting the side effect actually fired, not just that a code path compiled. `NullAiWebApplicationFactory` (a subclass) swaps these back to the real `Null*` fallbacks specifically to test the no-provider-configured degradation path end to end.
- Reading a tenant-scoped entity directly from a manually-created DB scope (no `HttpContext`, so no tenant claims) requires `.IgnoreQueryFilters()` — the global tenant filter otherwise resolves to no tenant and silently returns nothing.
- `EnterpriseFlow.Infrastructure.UnitTests` exists specifically for pure, dependency-free logic (protocol mappers, text extraction, chunking) that doesn't need a database or HTTP pipeline to test — don't route this kind of test through `Api.IntegrationTests` just because that's where most Infrastructure coverage lives.
- Migrations are verified by actually applying them to a real LocalDB/SQL Server instance (both incrementally and as a full fresh-database chain), not just by confirming the model builds — the test suite's SQLite `EnsureCreated()` path doesn't exercise migrations at all.
- `EnterpriseFlow.Infrastructure.SqlServerTests` (Release 4) covers SQL-Server-only features SQLite can't emulate (Temporal Tables/`TemporalAsOf`, ADR-0015) against a real LocalDB instance. Tagged `[Trait("Category", "RequiresSqlServer")]` and excluded from `ci.yml`'s default `dotnet test` run via `--filter "Category!=RequiresSqlServer"` (LocalDB is Windows-only; CI runs on `ubuntu-latest`) — run it explicitly with `dotnet test tests/EnterpriseFlow.Infrastructure.SqlServerTests/...` on a machine with LocalDB.

### Frontend structure (`src/EnterpriseFlow.Web/src/`)

Organized by type, not by feature: `api/` (one hand-written module per backend resource, e.g. `api/catalogs.ts`, all going through the shared `api/client.ts` axios instance whose interceptor injects the JWT and handles silent token refresh on 401), `views/<feature>/` (page components), `stores/` (Pinia, `defineStore` composition style — only used for state genuinely shared across components like auth identity or the toast queue; most feature views manage their own local `ref` state instead), `router/index.ts` (single file, all routes, lazy-loaded components, `meta.permission` gates access via a global guard), `locales/{en,es}.ts` (vue-i18n, flat nested objects), `types/index.ts` (hand-written interfaces mirroring backend DTOs; enums are `as const` objects, not TS `enum`, because `erasableSyntaxOnly` is set — numeric values must match the backend's underlying int values exactly since System.Text.Json serializes enums as numbers). No OpenAPI/TypeScript client generation — deliberate, revisit only once the API surface stabilizes.

Vuetify text inputs can be unreliable targets for synthetic DOM automation (`form_input`-style direct value-setting doesn't always propagate through Vue's reactivity) — prefer real keystroke simulation, and if a browser-automation tab seems stuck/unresponsive, verify against a fresh tab before concluding the component itself is broken.
