# ADR-003: Comment System Migration from Local Storage to Connectors

| Field       | Value                    |
|-------------|--------------------------|
| **Status**  | Accepted                 |
| **Date**    | 2026-02-17               |
| **Issue**   | #30                      |

## Context

The comment system was originally implemented with local file-based storage (`LocalCommentRepository`, `LocalCommentStore`). This meant:

- Comments added in Taskify were not visible in M-Files
- Comments from M-Files were not visible in Taskify
- The comment system was out of sync with the actual assignment data

This was inconsistent with the app's purpose of extending M-Files, not replacing it.

## Decision

Migrate the comment system to use the **connector abstraction** (`ITaskDataSource`), removing the local storage layer entirely.

### Changes made

1. **Added comment methods to `ITaskDataSource`**: `GetCommentsForTaskAsync`, `AddCommentAsync`, `GetCommentCountAsync`
2. **Implemented in `MFilesConnector`**: reads comments from M-Files version history; writes comments by checking out the object, appending to the comment property, and checking back in
3. **Implemented in `MockConnector`**: returns sample comment data for development
4. **Added `CommentDTO`**: normalized comment model used across all connectors
5. **Updated `CommentService`**: now delegates to `ITaskDataSource` instead of local storage
6. **Removed**: `ICommentRepository`, `LocalCommentRepository`, `LocalCommentStore`

### Comment reading strategy (MFilesConnector)

1. Primary: iterate M-Files version history, reading `VersionComment` and `LastModifiedBy` from each version
2. Fallback: parse the multi-line comment property if no version comments are found

## Consequences

### Positive

- **Single source of truth** — comments live in M-Files where they belong
- **Bi-directional visibility** — comments added in Taskify appear in M-Files and vice versa
- **Consistent architecture** — all data flows through `ITaskDataSource`
- **Reduced infrastructure** — no local comment files to manage

### Negative

- **Network dependency** — reading/writing comments now requires an active M-Files connection
- **Performance** — iterating version history for every comment load is slower than local file reads
- **Write constraints** — adding a comment requires a check-out/check-in cycle, which could conflict with other users editing the same object

### Risks

- Version history iteration may be slow for assignments with many versions. Mitigation: consider caching or pagination in the future.
- Check-out conflicts when adding comments. Mitigation: handle `CheckOut` failures gracefully and notify the user.
