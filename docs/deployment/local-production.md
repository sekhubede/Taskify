# Local Production Deployment (M-Files)

This guide covers two local deployment modes:

- **Terminal-run mode**: quickest setup, but services stop when terminal closes.
- **Persistent mode**: services auto-start and survive terminal close/reboot.

## Important behavior

If you start backend/frontend directly in a terminal, those processes are attached to that terminal session. Closing the terminal stops the app. This is expected.

Use **Persistent mode** if you need Taskify to stay available after terminal close or machine reboot.

## Prerequisites

- .NET 8 SDK
- Node.js + npm
- M-Files Desktop client installed
- Access to the target vault GUID

## Build artifacts (required for both modes)

From repo root:

```bash
cd backend
dotnet publish "src/Taskify.Api/Taskify.Api.csproj" -c Release -o "publish/api"
```

```bash
cd client
npm install
npm run build
```

## Mode A: Terminal-run (quick test)

### Start backend (Production + MFiles)

```bash
cd backend
ASPNETCORE_ENVIRONMENT=Production ASPNETCORE_URLS=http://0.0.0.0:5000 Taskify__DataSource=MFiles MFiles__VaultGuid="{YOUR-VAULT-GUID}" dotnet "c:/TempRepos/Taskify/backend/publish/api/Taskify.Api.dll"
```

### Start frontend

```bash
cd client
npm run preview -- --host 0.0.0.0 --port 4173
```

Open `http://localhost:4173`.

### Stop

Close the terminals, or run:

```powershell
Get-Process dotnet,node | Stop-Process -Force
```

## Mode B: Persistent local hosting (recommended)

This mode uses startup scripts + Windows Task Scheduler so Taskify auto-starts at login and remains running independently of interactive terminals.

### 1) Create startup scripts

Create `scripts/deployment/start-taskify-api.ps1`:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Production"
$env:ASPNETCORE_URLS = "http://0.0.0.0:5000"
$env:Taskify__DataSource = "MFiles"
$env:MFiles__VaultGuid = "{YOUR-VAULT-GUID}"

Set-Location "C:\TempRepos\Taskify\backend\publish\api"
dotnet "C:\TempRepos\Taskify\backend\publish\api\Taskify.Api.dll"
```

Create `scripts/deployment/start-taskify-web.ps1`:

```powershell
Set-Location "C:\TempRepos\Taskify\client"
npm run preview -- --host 0.0.0.0 --port 4173
```

### 2) Register startup tasks (run PowerShell as Administrator)

```powershell
$apiAction = New-ScheduledTaskAction -Execute "powershell.exe" -Argument "-NoProfile -ExecutionPolicy Bypass -File C:\TempRepos\Taskify\scripts\deployment\start-taskify-api.ps1"
$webAction = New-ScheduledTaskAction -Execute "powershell.exe" -Argument "-NoProfile -ExecutionPolicy Bypass -File C:\TempRepos\Taskify\scripts\deployment\start-taskify-web.ps1"
$trigger = New-ScheduledTaskTrigger -AtLogOn

Register-ScheduledTask -TaskName "Taskify-API" -Action $apiAction -Trigger $trigger -Description "Start Taskify backend API at logon" -Force
Register-ScheduledTask -TaskName "Taskify-Web" -Action $webAction -Trigger $trigger -Description "Start Taskify frontend preview at logon" -Force
```

### 3) Start immediately (without waiting for next login)

```powershell
Start-ScheduledTask -TaskName "Taskify-API"
Start-ScheduledTask -TaskName "Taskify-Web"
```

### 4) Verify and reboot test

Health checks:

```bash
curl http://localhost:5000/api/health
curl http://localhost:4173
```

Reboot machine, sign in, then re-run the checks. If both endpoints respond, persistent startup is working.

### 5) Manage/stop persistent tasks

```powershell
Stop-ScheduledTask -TaskName "Taskify-API"
Stop-ScheduledTask -TaskName "Taskify-Web"

Unregister-ScheduledTask -TaskName "Taskify-API" -Confirm:$false
Unregister-ScheduledTask -TaskName "Taskify-Web" -Confirm:$false
```
