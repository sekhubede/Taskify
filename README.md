# Taskify

Taskify is a productivity tool that extends M-Files assignment management. It pulls assignments from M-Files and layers on subtasks, personal notes, comment management, and a focused "Hot Zone" view.

> MVP note: assignment lists currently focus on active work items (completed assignments are excluded from standard views for responsiveness.)

## Purpose

Enhance task management for M-Files assignments by adding subtasks, personal notes, streamlined comment interaction, and optional team-level visibility.

- **Key features:**
    - Pull assignments and comments from M-Files
    - Add, manage, reorder, and delete subtasks per assignment
    - Add vault comments (synced to M-Files) and personal notes (local only)
    - Create local quick tasks with local comments and checklist items
    - Pin assignments to a "Hot Zone" section for focused work
    - Mark assignments as complete directly from the app
    - Collapse/expand assignment cards for a clean overview

## Architecture

- **Runtime**: React

## Project Structure

```bash
src/
    features/
        assignment/
            components/
            hooks/
            services/
            types/
    shared/
        components/
        hooks/
        utils/
        types/
    pages/
    app/
```

## Run the App

```bash
npm run dev
```

## Workflow Rules

- No code without an issue.
- One issue = one branch = one PR.
- All feature work targets `staging` first.
- `main` remains releasable/stable.

## Definition of Done (Issue Level)

- Build pass locally
- PR includes summary, and linked issue.
