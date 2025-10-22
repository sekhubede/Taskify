# Taskify

Taskify is a personal productivity tool designed to extend the functionality of Microsoft To Do for M-Files assignments. It allows users to manage their M-Files tasks more efficiently by enabling the creation of subtasks, adding personal comments, and updating assignment statuses directly in M-Files.

## Overview
- **Purpose:** Enhance task management in M-Files by integrating the concept of subtasks and comments similar to MS To Do.
- **Functionality:**
  - Pull assignments and comments from M-Files.
  - Add and manage subtasks for each assignment.
  - Add personal comments and vault comments.
  - Update assignment status in M-Files.

## Architecture
Taskify is structured as a full-stack application with the following components:

### Backend (Taskify.MFiles)
- **Type:** .NET 8 Console Application wrapped in Topshelf.
- **Responsibilities:**
  - Connect to M-Files using COM API.
  - Retrieve assignments and comments.
  - Update assignments with new subtasks, personal comments, and vault comments.
  - Handle assignment completion.

### Frontend
- **Type:** HTML, CSS, JavaScript.
- **Responsibilities:**
  - Display M-Files assignments.
  - Enable adding and managing subtasks.
  - Allow adding personal and vault comments.
  - Tick off completed assignments.

## Project Structure
```
Taskify
├─ backend
│  ├─ src
│  │  ├─ Taskify.Application/
│  │  ├─ Taskify.Domain/
│  │  └─ Taskify.MFiles/
│  └─ tests
│     └─ Taskify.Tests/
├─ client
│  ├─ css/
│  ├─ js/
│  └─ index.html
└─ .github/workflows
```

## Setup & Installation
1. Clone the repository:
   ```bash
   git clone https://github.com/YourUsername/Taskify.git
   ```
2. Navigate to the backend and restore dependencies:
   ```bash
   cd backend
   dotnet restore Taskify.sln
   ```
3. Build the solution:
   ```bash
   dotnet build Taskify.sln
   ```
4. Run tests:
   ```bash
   dotnet test tests/Taskify.Tests/Taskify.Tests.csproj
   ```
5. Start the backend service (local development):
   ```bash
   dotnet run --project Taskify.MFiles/Taskify.MFiles.csproj
   ```
6. Open `index.html` in the `client` folder to view the frontend.

## SonarCloud Integration
- Automated code quality and coverage checks are set up using GitHub Actions.
- Ensure `SONAR_TOKEN` and `GITHUB_TOKEN` secrets are set in the repository settings.

## Roadmap
- MVP: Pull assignments from M-Files and display in frontend.
- Add subtask management and personal/vault comments.
- Enable marking assignments as complete.
- Future: Explore React frontend and M-Files UIX integration for enhanced user experience.

## License
This project is licensed under the MIT License.

