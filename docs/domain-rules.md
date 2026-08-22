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
