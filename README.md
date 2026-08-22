# TaskFlow

A lightweight work-tracking API — a very small Jira. Backend only, built as a take-home exercise.

- **.NET 8**, C# 12, ASP.NET Core Web API (controllers)
- **PostgreSQL 16** via EF Core
- Four entities: `Project`, `WorkItem` (Epic/Story/Task/Bug/Sub-task), `Comment`, `User`
- Two real business rules, enforced end to end: the work-item status state machine, and a three-tier
  work-item hierarchy (Epic → Story/Task/Bug → Sub-task)

## Quickstart

```bash
docker compose up -d
dotnet run --project TaskFlow.Api
```

Swagger UI is at `https://localhost:<port>/swagger`. Migrations apply automatically on startup in
development, so cloning and running is a two-command setup.

```bash
dotnet build   # warning-free
dotnet test    # unit tests always; integration tests need Docker running (Testcontainers, real Postgres)
```

## API surface

| | |
|---|---|
| `POST /api/v1/projects` | Create a project (`key`, `name`) |
| `GET /api/v1/projects` | List projects |
| `POST /api/v1/workitems` | Create a work item (`projectId`, `issueType`, `title`, `reporterId`, optional `parentId`) |
| `GET /api/v1/workitems` | List work items |
| `POST /api/v1/workitems/{id}/transitions` | Move a work item's status (`to`) |
| `POST /api/v1/comments` | Add a comment (`workItemId`, `authorId`, `body`) |
| `GET /api/v1/comments` | List comments |
| `POST /api/v1/users` | Create a user (`name`, `email`) — no login, just identity for reporting/assigning |
| `GET /api/v1/users` | List users |

A minimal end-to-end flow: create a `User`, create a `Project`, create a `WorkItem` in it (reported by
that user), optionally create child work items under it, then transition it through its lifecycle.

## Where things are

```
TaskFlow.Api/
  Domain/            Entities, business rules, domain exceptions — no framework dependencies
  Application/        Services: load, mutate via a domain method, save
  Infrastructure/      EF Core: DbContext, entity configurations, migrations
  Contracts/           Request/response DTOs + mapping
  Controllers/          Thin HTTP endpoints
  ExceptionHandling/    Single place mapping exceptions to HTTP responses
tests/
  TaskFlow.UnitTests/         Domain rules — no database
  TaskFlow.IntegrationTests/  Full HTTP pipeline against real Postgres (Testcontainers)
docs/
  domain-rules.md      The specification the code and tests answer to
```

Single project, not a multi-project split — see `CLAUDE.md` and the private decision log for why.

## The two real rules

**Status transitions** (`WorkItem.Transition()`): `Todo → InProgress → InReview → Done`, plus `InProgress
→ Todo` and `InReview → InProgress` as rework paths. `Done` is terminal. Every other transition,
including any self-transition, is illegal and returns `409`.

**Hierarchy** (`WorkItem`'s constructor): exactly three tiers — `Epic` (never has a parent),
`Story`/`Task`/`Bug` (parent is null or an `Epic`), `Sub-task` (parent must be a `Story`/`Task`/`Bug`).
Tying tiers to `IssueType` like this makes a cycle structurally impossible, so there's no ancestor-chain
check needed anywhere.

Both are enforced entirely on the entity, with an exhaustive test matrix for each.

## How AI was used

This project was built with AI assistance under an explicit process: the AI drafted scaffolding,
boilerplate, and first-pass implementations; the two rules above were drafted collaboratively and then
actively rewritten and owned by hand, not accepted as-is. Every non-obvious decision — including a few
real bugs the process caught along the way — is written up in a private decision log kept out of this
public repo on purpose, available on request.

## License

[MIT](LICENSE)
