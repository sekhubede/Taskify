# Taskify

Taskify is a personal productivity tool designed to extend the functionality of Microsoft To Do for M-Files assignments. It allows users to manage their M-Files tasks more efficiently by enabling the creation of subtasks, adding personal comments, and updating assignment statuses directly in M-Files.

## Overview
- **Purpose:** Enhance task management in M-Files by integrating the concept of subtasks and comments similar to MS To Do.
- **Functionality:**
  - Pull assignments and comments from M-Files.
  - Add, manage, and delete subtasks for each assignment.
  - Add personal comments and vault comments.
  - Update assignment status in M-Files.

## Architecture
Taskify is structured as a full-stack application with the following components:

### Backend
The backend is built using .NET 8 with a clean architecture approach:

#### Taskify.Api
- **Type:** ASP.NET Core Web API
- **Responsibilities:**
  - RESTful API endpoints for assignments, comments, and subtasks
  - Connect to M-Files using COM API
  - Handle CORS for frontend integration
  - M-Files connection management via hosted service

#### Taskify.Application
- **Type:** Application services layer
- **Responsibilities:**
  - Business logic for assignments, comments, and subtasks
  - Orchestrates domain operations
  - Provides service interfaces for API consumption

#### Taskify.Domain
- **Type:** Domain layer
- **Responsibilities:**
  - Core entities (Assignment, Comment, Subtask, User, Vault)
  - Repository interfaces
  - Domain business rules

#### Taskify.Infrastructure
- **Type:** Infrastructure layer
- **Responsibilities:**
  - M-Files COM API integration
  - Repository implementations
  - Local storage for subtasks and personal notes
  - Data mapping between M-Files and domain entities

#### Taskify.MFiles
- **Type:** .NET 8 Console Application
- **Responsibilities:**
  - Manual testing and validation tool
  - Console-based testing of M-Files integration

### Frontend
- **Type:** React 19 + Vite
- **Responsibilities:**
  - Display M-Files assignments from the API
  - Enable adding, managing, and deleting subtasks
  - Allow adding personal and vault comments
  - Mark assignments as complete
  - Real-time updates via API calls

## Project Structure
```
Taskify
├─ backend
│  ├─ src
│  │  ├─ Taskify.Api/              # ASP.NET Core Web API
│  │  ├─ Taskify.Application/       # Application services layer
│  │  ├─ Taskify.Domain/            # Domain entities and interfaces
│  │  ├─ Taskify.Infrastructure/    # M-Files integration and storage
│  │  └─ Taskify.MFiles/            # Console testing application
│  └─ tests
│     └─ Taskify.Tests/             # Unit tests
├─ client
│  ├─ src/                          # React source files
│  │  ├─ App.jsx                    # Main React component
│  │  └─ main.jsx                   # React entry point
│  ├─ public/                       # Static assets
│  ├─ package.json                  # Node.js dependencies
│  └─ vite.config.js               # Vite configuration
└─ .github/workflows                # CI/CD workflows
```

## Setup & Installation

### Prerequisites
- .NET 8 SDK
- Node.js 18+ and npm
- M-Files COM API (Interop.MFilesAPI.dll)
- Access to an M-Files vault

### Backend Setup

1. Clone the repository:
   ```bash
   git clone https://github.com/YourUsername/Taskify.git
   cd Taskify
   ```

2. Navigate to the backend and restore dependencies:
   ```bash
   cd backend
   dotnet restore Taskify.sln
   ```

3. Configure M-Files settings:
   - Edit `backend/src/Taskify.Api/appsettings.json`
   - Update the `VaultGuid` with your M-Files vault GUID

4. Build the solution:
   ```bash
   dotnet build Taskify.sln
   ```

5. Run tests:
   ```bash
   dotnet test tests/Taskify.Tests/Taskify.Tests.csproj
   ```

6. Start the API server (local development):
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

2. Install dependencies:
   ```bash
   npm install
   ```

3. Start the development server:
   ```bash
   npm run dev
   ```
   The frontend will be available at `http://localhost:5173` (or the port shown in the terminal)

4. Build for production:
   ```bash
   npm run build
   ```

### Optional: Testing Console Application

The `Taskify.MFiles` console application can be used for manual testing:
```bash
cd backend/src/Taskify.MFiles
dotnet run
```

## SonarCloud Integration
- Automated code quality and coverage checks are set up using GitHub Actions.
- Ensure `SONAR_TOKEN` and `GITHUB_TOKEN` secrets are set in the repository settings.

## Roadmap
- ✅ MVP: Pull assignments from M-Files and display in React frontend
- ✅ RESTful API with ASP.NET Core
- ✅ React frontend with Vite
- ✅ Subtask management (add, update, delete, reorder) and personal/vault comments
- ✅ Mark assignments as complete
- Future: 
  - Enhanced UI/UX improvements
  - M-Files UIX integration
  - Real-time updates via SignalR
  - User authentication and authorization

## License
This project is licensed under the MIT License.

