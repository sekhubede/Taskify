# ADR-002: Local Storage for Subtasks and Personal Notes

| Field       | Value                    |
|-------------|--------------------------|
| **Status**  | Accepted                 |
| **Date**    | 2026-02-17               |
| **Issue**   | N/A (foundational design) |

## Context

Taskify extends M-Files assignments with features that don't exist natively in M-Files: subtasks and personal notes. We needed to decide where to store this data.

Options considered:
1. **Store in M-Files** — write subtasks/notes as custom properties or comments on the assignment object
2. **Store locally** — persist to the browser's localStorage on the client machine
3. **Store in a database** — introduce a backend database for Taskify-specific data

## Decision

Use **browser localStorage** for subtasks and personal notes.

Subtasks and personal notes are stored in the browser's localStorage, keyed by assignment ID. This data is personal to the user and machine — it is not shared with the M-Files vault or other users.

### Storage keys

- Subtasks: `subtasks_{assignmentId}`
- Personal notes (assignment): `assignmentNotes_{assignmentId}`
- Personal notes (subtask): `subtaskNotes_{subtaskId}`
- Comment flags: `commentFlags_{commentId}`
- Working-on flags: stored via backend API (not localStorage)

## Consequences

### Positive

- **Zero infrastructure** — no database to set up, manage, or migrate.
- **Instant and offline-capable** — reads/writes are synchronous and don't depend on network.
- **Privacy** — personal notes never leave the machine; no risk of accidentally sharing sensitive thoughts.
- **Simplicity** — straightforward to implement, easy to debug.

### Negative

- **Machine-specific** — subtasks and notes are tied to the specific browser on the specific machine. Accessing from another machine shows a blank slate.
- **No backup** — clearing browser data deletes all subtasks and notes.
- **Storage limits** — localStorage is limited to ~5-10MB per origin, which is sufficient for text data but could become an issue at scale.

### Risks

- Users who work from multiple machines will lose context. This is a known limitation and is tracked as a future issue (#40 — cloud-based subtasks).
- No migration path currently exists. If we move to cloud storage later, we'll need to build an import/export mechanism.
