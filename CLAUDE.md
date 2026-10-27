# CLAUDE.md

> This file comes from a project template. Values in `<angle brackets>` are placeholders. If a placeholder you need is still unfilled, do not guess: ask me, or offer to fill it in from what is actually in the repo. Once confirmed, replace it in this file.

## 1. Think Before Coding
**Don't assume. Don't hide confusion. Surface tradeoffs.**

Before implementing:
- State your assumptions explicitly. If uncertain, ask.
- If multiple interpretations exist, present them - don't pick silently.
- If a simpler approach exists, say so. Push back when warranted.
- If something is unclear, stop. Name what's confusing. Ask.
- Look for similar implementations in the codebase before writing new code.

## 2. Simplicity First
**Minimum code that solves the problem. Nothing speculative.**

- No features beyond what was asked.
- No abstractions for single-use code.
- No "flexibility" or "configurability" that wasn't requested.
- No error handling for impossible scenarios.
- If you write 200 lines and it could be 50, rewrite it.
- Reuse existing code and shared packages - do not reinvent what already exists.
- Never add a new dependency (NuGet, npm, or any other package) without asking me first.

Ask yourself: "Would a senior engineer say this is overcomplicated?" If yes, simplify.

## 3. Surgical Changes
**Touch only what you must. Clean up only your own mess.**

- Don't "improve" adjacent code, comments, or formatting.
- Don't refactor things that aren't broken.
- Match existing style, even if you'd do it differently.
- If you notice unrelated dead code, mention it - don't delete it.
- Remove imports/variables/functions that YOUR changes made unused. Leave pre-existing dead code alone unless asked.

The test: every changed line should trace directly to the request.

**Comments - keep them minimal:**
- At most one short doc comment at the top of a class or interface (C#: `/// <summary>`, TS: `/** ... */`) saying what it is for.
- No doc comments on every method/property, no comments that repeat what the code says, no commented-out code.
- An inline comment only when the *why* isn't obvious from the code.

**Modifiers - only what is needed:**
- Don't add modifiers or keywords "just in case" to classes, interfaces, records, methods, properties or anything else - e.g. `sealed`, `partial`, `virtual`, `abstract`, `static`, `required`, `new`.
- Use one only when the code actually needs it (it doesn't compile or behave correctly without it, or there is a concrete reason you can name). Match what the surrounding code already does.

**Language:** everything written into the project is in English by default - code, identifiers, comments, UI text, error messages, docs, commit messages and PR texts - unless Section 5 says otherwise for this project.

## 4. Goal-Driven Execution
**Define success criteria. Loop until verified.**

- "Add validation" -> "Write tests for invalid inputs, then make them pass"
- "Fix the bug" -> "Write a test that reproduces it, then make it pass"
- "Refactor X" -> "Ensure tests pass before and after"

For multi-step tasks, state a brief plan:
```
1. [Step] -> verify: [check]
2. [Step] -> verify: [check]
```
Run the relevant build/test/lint commands (Section 6) before saying a step is done.

## 5. Project
<!-- Fill in per project. Delete what doesn't apply. -->
- **Name:** notification-hub
- **Purpose:** Central notification service for other apps. Consumes notification commands from RabbitMQ, renders them from templates (RazorLight), delivers them through provider integrations (currently email via Brevo) and records delivery status per client service.
- **Layout:**
  - Repo root - C# .NET solution (`notification-hub.sln`) with `Domain`, `Contracts`, `Application`, `DTO`, `DataAccess`, `External`, `Web` (API + MVC) and `Tests` projects
- **Specs:** if an `openspec/` folder exists, follow the active change proposal and do not implement anything outside it.

## 6. Stack & Commands
<!-- Keep only the layers this project has. -->

### Backend (.NET)
- .NET 10, built on the `alaasmagi.Base.*` packages (Section 7). ASP.NET Core MVC + API in one host, EF Core with PostgreSQL, Keycloak, Redis, RabbitMQ, GlitchTip.
- Records for DTOs/value objects, classes for domain entities.
- Async all the way - never `.Result` / `.Wait()`.
- Build: `dotnet build notification-hub.sln`
- Run: `dotnet run --project Web/Web.csproj`
- Test: `dotnet test notification-hub.sln`
- Add a migration (allowed): `dotnet ef migrations add <Name> --project DataAccess/DataAccess.csproj --startup-project Web/Web.csproj --context AppDbContext`
- Never run `dotnet ef database update` - I apply migrations (Section 9).
- **Cleanup after building (macOS) - HARD rule:** builds on macOS can create files/folders whose name contains a literal backslash (e.g. one folder named `bin\Debug` instead of `bin/Debug`). Before finishing any task where you ran a build or tests, remove them:
  ```
  find . -path ./.git -prune -o -name '*\\*' -prune -print -exec rm -rf {} +
  ```
  Mention in the change summary if anything was removed.

### UI (Frontend, Mobile & backend MVC views) - HARD rules
These rules apply to every UI, including Razor Pages views in the backend Web/MVC layer.
- Use only my design system (based on Figma components): its components and tokens (colors, typography, spacing, radii, etc.). No other UI libraries, no hardcoded colors/sizes, no ad-hoc styles outside the tokens.
- Exception: the accent color is project-specific. It is overridden in exactly one place, for both light and dark mode. Everything else uses the accent token - never hardcode the accent value in components.
- The design for a feature is made in Figma first and imported into `/Users/alaasmagi/Projects/design-system`. Before implementing any UI, find the matching design there and build exactly from it.
- If the design is missing, incomplete, or doesn't cover a state you need (empty, loading, error, mobile view, etc.) - stop and ask me. Never invent the design yourself.
- If the design needs a component the design system doesn't have yet - stop and ask. Don't create a one-off replacement.

## 7. Backend Architecture (layered)
The layer boundaries below are strict. Never add a project reference that breaks them. If a task seems to need one, stop and ask.

### Layers and dependencies
| Layer | Contains | May depend on |
|---|---|---|
| **Domain** | Core entities with their validation rules; enums used across the app | `alaasmagi.Base.*` only |
| **Contracts** | Interfaces only, in three folders: `Application/`, `DataAccess/`, `External/` | Domain |
| **Application** | Business logic; implements `Contracts/Application` | Domain, Contracts |
| **DTO** | `DataAccess/` (`...Entity` + `Mappers/`), `Web/` (`...Dto` + `Mappers/`) | Domain |
| **DataAccess** | `AppDbContext`, `AppDbContextFactory`, UoW, CRUD/repositories; implements `Contracts/DataAccess` | Domain, Contracts, DTO (`DataAccess/` only) |
| **External** | Concrete third-party implementations, one folder per concern (`Cache/`, `RabbitMQ/`, ...); implements `Contracts/External` | Domain, Contracts |
| **Web** | `API/` (REST endpoints), `MVC/` (Razor Pages built on Web DTOs) | Domain, Contracts, DTO (`Web/` only). DataAccess/External only for DI registration in `Configuration/` |
| **Tests** | All unit tests | Any layer |

### Naming
- Domain entity: no suffix - `AppUser`
- Database entity: `...Entity` - `AppUserEntity`
- Web/presentation DTO: `...Dto` - `AppUserDto`

### Rules
- **Domain entities are the only currency between layers.** Application methods take and return domain entities only. Repositories take and return domain entities only.
- **Mapping happens only at the edges:** DataAccess maps `...Entity` <-> domain, Web maps `...Dto` <-> domain. Map before calling Application, map after it returns.
- **Every mapper implements `IMapper<...>` from `Base.Contracts.DTO`** (exact generic signature depends on the Base version in use). No ad-hoc mapping inside controllers, pages, services or repositories.
- **Web returns only `...Dto` data** - in API and MVC alike. Never expose domain entities or `...Entity` types from Web.
- **Contracts are provider-agnostic.** No third-party types in signatures, generic names only: `ICacheService.SaveAsync`, not `IRedisService.SaveToRedisAsync`.
- **Provider-specific names and types (Redis, RabbitMQ, ...) exist only in External.**
- **Business validation rules live on domain entities**, not in DTOs, controllers or pages.
- **Persistence constraints** (`[Required]`, `[MaxLength(...)]`) are data annotations on the `...Entity` class. `AppDbContext` holds only schema, table, index and relationship configuration.
- **DI registration lives in Web's `Configuration/` folder** as extension methods. `Program.cs` only calls them and stays as small as possible.
- Layer interfaces in Contracts are required even when there is only one implementation - Section 2's "no abstractions for single-use code" does not apply to layer boundaries.

### Base package (`alaasmagi.Base.*`)
Use what the package already provides before writing anything new:
- **Domain entities** inherit from a `Base.Domain` class (`BaseEntity...`, `...WithMeta`, `...Concurrency`, `...SoftDelete`, `BaseEntityUser...` variants). Identity users/roles inherit from `Base.Domain.Identity`.
- **Repositories** inherit from `BaseRepository<...>` / `BaseRepositorySoftDelete<...>`; the UoW inherits `BaseUow<AppDbContext>`. **Services** inherit from `BaseService<...>` / `BaseServiceSoftDelete<...>`.
- **Repositories never save.** They stage changes; the service commits through the UoW.
- **Metadata** (`CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`) is set by the repository base classes - never set it manually.
- **Soft delete** goes through the `SoftDelete` variants - never implement your own `IsDeleted` handling.
- **User scoping** is automatic for entities implementing `IBaseEntityUserId<TKey>` - never add manual user filters on top.
- **Expected failures return `IMethodResponse<T>`** (`MethodResponse`, `Error`, `ErrorDefaults` from `Base.DTO`) - no custom result/error types. Exceptions (`BaseException` / `HttpException`) only for unexpected failures.
- **Cache:** callers use `IAppCache` (incl. `GetOrSetAsync` for cache-aside); the Redis store in `External/Cache` inherits `BaseCache`.
- **Messaging:** publish with `BaseEventEnvelope<TContent>`, consume with `RabbitMqConsumerBase<TContent>`; RabbitMQ specifics only in External/`Configuration/`.
- **Keycloak:** auth, role claims and admin API go through `Base.Keycloak` helpers - never hand-write JWT/OIDC setup.
- All `alaasmagi.Base.*` packages are kept on the same version. Updating one means updating all.
- If the base package is missing something that would be reusable across projects, tell me instead of building it locally - it may belong in the package.
- Naming in this file wins over the package README examples (e.g. `TodoEntity`, not `TodoDbEntity`).

### Adding a new feature
`AppUser` is the reference implementation - copy its pattern across all layers:
1. Domain model - `Domain/`
2. Persistence entity + mapper - `DTO/DataAccess/`, `DTO/DataAccess/Mappers/`
3. Web DTO + mapper - `DTO/Web/`, `DTO/Web/Mappers/`
4. Repository + service interfaces - `Contracts/DataAccess/`, `Contracts/Application/`
5. Repository + service implementations - `DataAccess/`, `Application/`
6. API controller - `Web/API/Controllers/`
7. DI wiring - `Web/Configuration/ServiceConfiguration.cs`
8. Migration (if persistence changed) + unit tests

Write custom behavior only where the feature differs from generic CRUD or messaging.

### API
- Routes are URL-versioned and kebab-case plural: `/api/v1/<resources>`.
- Every API controller uses the existing named rate-limit policy.
- `PUT` and `DELETE` use optimistic concurrency: current `ConcurrencyToken` in the `If-Match` header.

### Tests
- Anything new that contains logic (domain validation, application services, mappers, repositories, external implementations) gets unit tests in the Tests project in the same task.
- A task is not done until the new tests exist and `dotnet test` passes.

## 8. Platform Integration
<!-- Only if the project uses the shared hubs. -->
- Auth goes through Keycloak (realm: `notification-hub`). Never implement custom auth.
- Async communication goes through RabbitMQ using the `Base.Message` envelope. Do not define a new envelope or change exchange/queue topology in code - topology is set up manually.
- **Errors:** no new exception hierarchies unless asked (see Section 7). Errors are reported to error-hub (GlitchTip), never only to the console. No `try/catch` unless the code can actually handle the error - never swallow exceptions.
- Email is sent via email-hub commands (`<app-slug>.<action>`), never directly from the app.
- Config comes from environment variables (`APP_SLUG` etc.) - see Section 10. Never hardcode secrets; never commit `.env`.

## 9. Version Control - HARD Rules
- Never run `git commit`, `git push`, `git merge`, `git pull`, `git rebase` or `git reset`.
- Creating a new branch (`git switch -c`) is allowed when starting a task on `main`/`develop`. Name: `<feature|fix|chore>/<2-4-keywords>`, lowercase, hyphen-separated. If it already exists, stop and ask.
- Never run SQL or migrations against a real database. Generate migrations; I apply them.
- Never put `[DEPLOY]` in a suggested commit message - it triggers a production deploy. I add it myself.
- Commit messages and PR texts go in the chat only, never into files.

## 10. Environment Variables - HARD Rules
- Environment variables live only in `.env`. Never put config values anywhere else (no hardcoded defaults in code, no inline values in compose files or workflows).
- Only exception: `appsettings*.json` may contain non-secret framework settings like `Logging` (log levels). Anything secret or environment-specific (connection strings, URLs, credentials, keys) goes in `.env`.
- `.env.example` is a copy of `.env` with the same keys in the same order and all values empty.
- `.env` and `.env.example` must ALWAYS be in sync. Every added, renamed or removed variable is changed in both files in the same step.
- Format is exactly `UPPER_SNAKE_CASE=value` - no spaces around `=`, no other naming style. In `.env.example`: `UPPER_SNAKE_CASE=`
- Never invent secret values. A new secret goes into `.env` with an empty value and is listed under Manual steps for me to fill in.
- Every env change must also be applied everywhere the variable is referenced, ALWAYS including:
  - `.github/workflows/*` (e.g. `.env` generation from GitHub secrets)
  - Docker files: `Dockerfile*`, `docker-compose*.yml` / `compose*.yml`, and any other Docker-related folders or files
- Before finishing an env change, search the whole repo for the variable name to verify no reference was missed.

## 11. Change Summary (every task that changes files)
At the end of the response:
- **Branch** (if created)
- **Files changed** - path + 1 line on what changed
- **⚠️ Env variables changed** - if any variable was added, renamed or removed: list each variable, what happened to it, and every file where it was changed or is referenced (`.env`, `.env.example`, workflows, Docker files, code). If a GitHub secret must be added or renamed, say so explicitly.
- **Manual steps** - migrations, env vars, config changes I must do by hand
- **Suggested commit message** - in a code block, one per logical change, past tense, plain language
