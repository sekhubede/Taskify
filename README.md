# Taskify

Taskify is a personal productivity tool that extends M-Files assignment management. It pulls assignments from M-Files and layers on subtasks, personal notes, comment management, and a focused "Hot Zone" view — features M-Files doesn't provide natively.

## Overview

- **Purpose:** Enhance task management for M-Files assignments by adding subtasks, personal notes, and streamlined comment interaction.
- **Key features:**
  - Pull assignments and comments from M-Files
  - Add, manage, reorder, and delete subtasks per assignment
  - Add vault comments (synced to M-Files) and personal notes (local only)
  - Pin assignments to a "Hot Zone" section for focused work
  - Mark assignments as complete directly from the app
  - Collapse/expand assignment cards for a clean overview

## Architecture

Taskify is a full-stack application with a .NET 8 backend and React 19 frontend.

### Connector Abstraction

All external data access flows through the `ITaskDataSource` interface. This decouples the app from M-Files and enables:

- **`MFilesConnector`** — Production connector using M-Files COM API
- **`MockConnector`** — Development connector with realistic sample data, no external dependencies

The active connector is selected via `Taskify:DataSource` in `appsettings.json` (`"MFiles"` or `"Mock"`).

> See [ADR-001](docs/adr/001-connector-abstraction-pattern.md) for the full design rationale.

### Backend (.NET 8)

| Project | Purpose |
|---------|---------|
| **Taskify.Api** | ASP.NET Core Web API — RESTful endpoints for assignments, comments, subtasks, and user info |
| **Taskify.Application** | Application services layer — business logic and orchestration |
| **Taskify.Domain** | Domain entities (Assignment, Comment, Subtask, User) and business rules |
| **Taskify.Infrastructure** | Local storage for subtasks and personal notes |
| **Taskify.Connectors** | `ITaskDataSource` interface, `MFilesConnector`, `MockConnector`, DTOs |
| **Taskify.MFiles** | Console app for manual M-Files integration testing |

### Frontend (React 19 + Vite)

Single-page application that displays assignments from the API and provides interactive management of subtasks, comments, notes, and task status.

### Data storage

| Data | Storage | Shared? |
|------|---------|---------|
| Assignments, comments | M-Files vault (via connector) | Yes — visible in M-Files |
| Subtasks | Browser localStorage | No — machine-specific |
| Personal notes | Browser localStorage | No — machine-specific |
| Comment flags | Browser localStorage | No — machine-specific |
| Working-on flags | Backend API (local file) | No — machine-specific |

> See [ADR-002](docs/adr/002-local-storage-for-subtasks-and-notes.md) and [ADR-003](docs/adr/003-comment-system-migration-to-connectors.md) for storage decisions.

## Project Structure

```
Taskify
├── backend
│   ├── src
│   │   ├── Taskify.Api/              # ASP.NET Core Web API
│   │   ├── Taskify.Application/      # Application services layer
│   │   ├── Taskify.Domain/           # Domain entities and interfaces
│   │   ├── Taskify.Infrastructure/   # Local storage for subtasks and notes
│   │   ├── Taskify.Connectors/       # ITaskDataSource, MFiles, Mock connectors
│   │   └── Taskify.MFiles/           # Console testing application
│   └── tests
│       └── Taskify.Tests/            # Unit tests (70+ tests)
├── client
│   ├── src/
│   │   ├── App.jsx                   # Main React component
│   │   └── main.jsx                  # React entry point
│   ├── public/                       # Static assets
│   ├── package.json                  # Node.js dependencies
│   └── vite.config.js               # Vite configuration
├── docs
│   └── adr/                          # Architecture Decision Records
└── .github/workflows/               # CI/CD workflows
```

## Setup & Installation

### Prerequisites

- .NET 8 SDK
- Node.js 18+ and npm
- **For M-Files mode:** M-Files Desktop Client installed (provides COM API)
- **For Mock mode:** No additional dependencies

### Backend Setup

1. Clone the repository:
   ```bash
   git clone https://github.com/sekhubede/Taskify.git
   cd Taskify
   ```

2. Restore dependencies:
   ```bash
   cd backend
   dotnet restore Taskify.sln
   ```

3. Configure the data source:

   **Mock mode** (no M-Files needed — great for frontend development):
   ```json
   // backend/src/Taskify.Api/appsettings.json
   {
     "Taskify": {
       "DataSource": "Mock"
     }
   }
   ```

   **M-Files mode** (requires vault access):
   ```json
   // backend/src/Taskify.Api/appsettings.json
   {
     "Taskify": {
       "DataSource": "MFiles"
     }
   }
   ```
   Then create `appsettings.Development.json` (see `appsettings.Development.json.example`):
   ```json
   {
     "MFiles": {
       "VaultGuid": "{YOUR-VAULT-GUID-HERE}"
     },
     "Urls": "http://localhost:5000"
   }
   ```

4. Build and test:
   ```bash
   dotnet build Taskify.sln
   dotnet test tests/Taskify.Tests/Taskify.Tests.csproj
   ```

5. Run the API:
   ```bash
   cd src/Taskify.Api
   dotnet run
   ```
   The API will be available at `http://localhost:5000`

### Frontend Setup

1. Navigate to the client directory:
   ```bash
   cd client
   ```

2. Install dependencies and start:
   ```bash
   npm install
   npm run dev
   ```
   The frontend will be available at `http://localhost:5173`

3. Build for production:
   ```bash
   npm run build
   ```

### Quick Start (Both Together)

A PowerShell script is available to start both backend and frontend:
```powershell
.\run.ps1
```

## Development Workflow

This project follows a structured workflow:

1. **No code without an issue** — every change starts with a GitHub issue
2. **Branch per issue** — branch naming: `{issue-number}-{short-description}`
3. **No PR without documentation** — feature changes update the README; architectural changes add/update an ADR
4. **Conventional commits** — `feat(scope):`, `fix(scope):`, `docs(scope):`, `chore:`

### Architecture Decision Records

ADRs are stored in [`docs/adr/`](docs/adr/) and document significant architectural choices. See the [ADR template](docs/adr/000-template.md) for the format.

## SonarCloud Integration

Automated code quality and coverage checks via GitHub Actions. Requires `SONAR_TOKEN` and `GITHUB_TOKEN` secrets in repository settings.

## Roadmap

- ✅ Pull assignments from M-Files and display in React frontend
- ✅ RESTful API with ASP.NET Core
- ✅ React frontend with Vite
- ✅ Subtask management (add, delete, reorder via drag-and-drop)
- ✅ Vault comments (synced to M-Files) and personal notes
- ✅ Mark assignments as complete
- ✅ "Hot Zone" pinning and assignment board
- ✅ Connector abstraction (Mock + M-Files)
- 🔜 Component refactoring (extract from monolithic App.jsx)
- 🔜 New comment indicators
- 🔜 Subtask editing and enhanced ordering
- 🔜 Assignment attachments
- 🔜 Team oversight view (all user assignments)
- 📋 Cloud-based subtasks, time tracking, AI features (future)

## License

This project is licensed under the MIT License.
