# ADR-001: Connector Abstraction Pattern (ITaskDataSource)

| Field       | Value                    |
|-------------|--------------------------|
| **Status**  | Accepted                 |
| **Date**    | 2026-02-17               |
| **Issue**   | N/A (foundational design) |

## Context

Taskify was originally built to work exclusively with M-Files via the COM API. This created tight coupling between the application logic and the M-Files infrastructure, making it difficult to:

- Develop and test the frontend without an active M-Files vault connection
- Add support for other data sources in the future
- Run automated tests without COM API dependencies

## Decision

Introduce a **connector abstraction** via the `ITaskDataSource` interface. All data access (tasks, comments, user info) flows through this interface. Concrete implementations are swapped via configuration (`appsettings.json`):

- **`MFilesConnector`** — Production connector using M-Files COM API. All M-Files knowledge lives in this class and its helpers.
- **`MockConnector`** — Development/testing connector that returns realistic sample data with no external dependencies.

The active connector is selected by the `Taskify:DataSource` config value (`"MFiles"` or `"Mock"`).

### Interface contract

```csharp
public interface ITaskDataSource
{
    Task<IReadOnlyList<TaskDTO>> GetAllTasksAsync();
    Task<TaskDTO?> GetTaskByIdAsync(string taskId);
    Task<IReadOnlyList<TaskDTO>> GetTasksByAssigneeAsync(string assigneeId);
    Task<bool> UpdateTaskStatusAsync(string taskId, TaskItemStatus newStatus);
    Task<bool> IsAvailableAsync();
    Task<string> GetCurrentUserNameAsync();
    Task<IReadOnlyList<CommentDTO>> GetCommentsForTaskAsync(string taskId);
    Task<CommentDTO> AddCommentAsync(string taskId, string content);
    Task<int> GetCommentCountAsync(string taskId);
}
```

## Consequences

### Positive

- **Frontend development is unblocked** — use `"Mock"` mode and develop UI features without needing M-Files installed or a vault connection.
- **Testability** — unit tests mock `ITaskDataSource` directly; no COM interop needed in CI.
- **Single responsibility** — M-Files knowledge is isolated in `Taskify.Connectors/MFiles/`. When M-Files goes away, you delete that folder and nothing else changes.
- **Future-proof** — adding another data source (e.g., Odoo, Jira) means implementing one interface.

### Negative

- **Lowest common denominator** — the interface must accommodate all connectors, so M-Files-specific features (e.g., version history) need to be generalized or handled internally.
- **Mock drift** — the mock connector's behavior may diverge from the real connector over time if not maintained.

### Risks

- Mock connector might mask real integration issues. Mitigation: always do a final verification against a real M-Files vault before merging features that touch the connector layer.
