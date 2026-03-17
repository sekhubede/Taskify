# Local Production Deployment (M-Files)

This guide runs Taskify locally in a production-style setup:

- backend published build
- frontend built assets served with `vite preview`
- connector set to `MFiles`

## Prerequisites

- .NET 8 SDK
- Node.js + npm
- M-Files Desktop client installed
- Access to the target vault GUID

## 1) Build and publish backend

From repo root:

```bash
cd backend
dotnet publish "src/Taskify.Api/Taskify.Api.csproj" -c Release -o "publish/api"
```

## 2) Build frontend

From repo root:

```bash
cd client
npm install
npm run build
```

## 3) Start backend (Production + MFiles)

From repo root:

```bash
cd backend
ASPNETCORE_ENVIRONMENT=Production ASPNETCORE_URLS=http://0.0.0.0:5000 Taskify__DataSource=MFiles MFiles__VaultGuid="{YOUR-VAULT-GUID}" dotnet "c:/TempRepos/Taskify/backend/publish/api/Taskify.Api.dll"
```

Backend health check:

```bash
curl http://localhost:5000/api/health
```

## 4) Start frontend preview

From repo root:

```bash
cd client
npm run preview -- --host 0.0.0.0 --port 4173
```

Open:

- `http://localhost:4173`

## 5) Stop services

- Stop frontend preview process.
- Stop backend `dotnet` process.

On Windows PowerShell if needed:

```powershell
Get-Process dotnet,node | Stop-Process -Force
```
