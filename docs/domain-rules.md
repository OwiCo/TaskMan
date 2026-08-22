# Domain rules

This file is the specification. The unit tests encode it, and the code implements it. If any of the three
disagree, this file wins and the other two are wrong.

This file grows one entity at a time, alongside the code — not written up front for entities that don't
exist yet.

---

## Project

A `Project` is the top-level container everything else belongs to (a board/workspace, e.g. "Engineering").
Every `WorkItem` belongs to exactly one `Project`. It serves two purposes: grouping related work, and
generating the human-readable ticket identifier (`Key` + a per-project counter, e.g. `ENG-14`).

### Invariants

- `Key` is 2–10 characters, **uppercase letters only** (`A`–`Z`, no digits, no hyphens, no lowercase).
- `Name` is non-blank, max 200 characters.
- `NextItemNumber` starts at 1 on creation. It is never a constructor argument — the entity always
  initializes it itself. It increments once per `WorkItem` created under the project (enforced when
  `WorkItem` creation is built — not yet, since `WorkItem` doesn't exist yet).
- `Key` must be unique **across all projects** — this cannot be enforced by the entity alone (an entity
  can't see other rows), so it will be a database unique constraint once persistence exists, with an
  application-level pre-check for a friendly error message. Not yet built.

### Enforcement

All four invariants above are guard clauses in the constructor. There is no public setter for any of
`Project`'s fields — once constructed, a `Project`'s `Key` and `Name` cannot change (no rename is
supported in this scope), and `NextItemNumber` will only ever change through a dedicated method once
`WorkItem` creation exists.

---

## WorkItem

A `WorkItem` is a single ticket: an Epic, Story, Task, Bug, or Sub-task. It always belongs to exactly one
`Project`, always has a `Reporter`, and carries the two real rules this codebase is built to defend.

### Invariants

- Always belongs to a project (`ProjectId` cannot be empty).
- Always has a non-blank `Title`, max 200 characters.
- Always has a `Number`, assigned at creation and never changed afterward. Combined with the project's
  `Key` it forms the human identifier (e.g. `ENG-14`). The `WorkItem` does not generate this number
  itself — it's supplied by the caller, which is expected to come from `Project`'s own counter once that
  method exists (not yet built).
- Always has a `ReporterId` (cannot be empty). `AssigneeId` is optional.
- Always has a `Status`; every `WorkItem` starts at `Todo`.

### Rule 1 — the status state machine

| From | To | Why |
|---|---|---|
| `Todo` | `InProgress` | Work starts. |
| `InProgress` | `InReview` | Submitted for review. |
| `InProgress` | `Todo` | Stopped, returned to the backlog. |
| `InReview` | `Done` | Review passed. |
| `InReview` | `InProgress` | Review failed; rework needed. |
| `Done` | — | Terminal. Reopening means creating a new `WorkItem`, not un-terminating this one — keeps the record of what was actually completed intact. |

Any transition not in this table is illegal, including every self-transition (`InProgress → InProgress`,
etc.) — a no-op that silently "succeeds" would hide a client bug rather than surface it. `Todo → InReview`
and `Todo → Done` are illegal for the same reason: work can't be reviewed or closed before it starts.

**Enforcement:** `WorkItem.Transition(WorkItemStatus to, DateTimeOffset now)`. `Status`'s setter is
private — there is no other code path that can change it, so the rule can't be bypassed by a future caller
who forgets it exists. An illegal transition throws `InvalidTransitionException`.

### Rule 2 — hierarchy (three fixed tiers, not an open tree)

`ParentId` is self-referencing, constrained by `IssueType` into exactly three tiers:

- **Epic** — must never have a parent.
- **Story / Task / Bug** — parent must be null, or an Epic.
- **Sub-task** — parent must be a Story, Task, or Bug (never null, never an Epic, never another Sub-task).

Because the tiers are strictly ordered like this, a cycle is structurally impossible — it would require an
edge somewhere in the loop that the table already forbids. That means no ancestor-chain walk, no database
query: the whole rule is two known values (`this.IssueType`, `parent?.IssueType`) checked against a small
fixed table, so — like the status machine — it lives entirely on the entity with zero dependencies.

**Enforcement:** checked in the constructor (a `WorkItem` cannot be created with an illegal parent in the
first place). An illegal combination throws `InvalidHierarchyException`. Re-parenting an existing
`WorkItem` after creation is not built yet — only construction-time validation exists so far.

**Deliberately not built yet, on purpose:**
- **No status roll-up.** An Epic's `Status` is independent of its children's — set directly, never
  computed.
- **No cascading delete story here yet** — see the delete-blocking design in the private decision log;
  not implemented in code until the persistence layer exists to enforce it at the database level.
