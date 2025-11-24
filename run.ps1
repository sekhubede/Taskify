$ErrorActionPreference = "Stop"

function Assert-Command {
    param(
        [Parameter(Mandatory = $true)][string]$Name
    )

    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Missing required command: $Name. Make sure it is installed and on your PATH."
    }
}

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptRoot

Assert-Command -Name "dotnet"
Assert-Command -Name "npm"

$dotnetCmd = (Get-Command dotnet -ErrorAction Stop).Path
$npmCmdInfo = Get-Command npm -ErrorAction Stop
$npmCmdPath = $npmCmdInfo.Path

$backendProject = Join-Path $scriptRoot "backend/src/Taskify.Api/Taskify.Api.csproj"
if (-not (Test-Path $backendProject)) {
    throw "Backend project file not found at $backendProject"
}

Write-Host "Starting Backend (dotnet watch run)..." -ForegroundColor Cyan
$backendProcess = Start-Process -FilePath $dotnetCmd -ArgumentList @("watch", "run", "--project", $backendProject) -WorkingDirectory $scriptRoot -PassThru

$frontendDir = Join-Path $scriptRoot "client"
if (-not (Test-Path $frontendDir)) {
    throw "Frontend directory not found at $frontendDir"
}

Write-Host "Starting Frontend (npm run dev)..." -ForegroundColor Cyan
$frontendLogOut = Join-Path $frontendDir "dev.stdout.log"
$frontendLogErr = Join-Path $frontendDir "dev.stderr.log"
Remove-Item $frontendLogOut, $frontendLogErr -ErrorAction SilentlyContinue
$frontendCommand = if ($npmCmdPath -and ($npmCmdPath.ToLower().EndsWith(".cmd") -or $npmCmdPath.ToLower().EndsWith(".bat") -or $npmCmdPath.ToLower().EndsWith(".exe"))) {
    @($npmCmdPath, "run", "dev")
}
else {
    @("npm", "run", "dev")
}

if ($frontendCommand[0].ToLower().EndsWith(".exe")) {
    $frontendProcess = Start-Process -FilePath $frontendCommand[0] -ArgumentList $frontendCommand[1..($frontendCommand.Length - 1)] -WorkingDirectory $frontendDir -PassThru -RedirectStandardOutput $frontendLogOut -RedirectStandardError $frontendLogErr
}
else {
    $frontendProcess = Start-Process -FilePath $env:ComSpec -ArgumentList @("/c", "`"$($frontendCommand -join ' ')`"") -WorkingDirectory $frontendDir -PassThru -RedirectStandardOutput $frontendLogOut -RedirectStandardError $frontendLogErr
}

Start-Sleep -Seconds 2

if ($backendProcess.HasExited) {
    throw "Backend process exited immediately with code $($backendProcess.ExitCode). Check the console window for details."
}

if ($frontendProcess.HasExited) {
    $frontendOutput = @()
    if (Test-Path $frontendLogOut) {
        $frontendOutput += "stdout:`n$(Get-Content $frontendLogOut -Raw)"
    }
    if (Test-Path $frontendLogErr) {
        $frontendOutput += "stderr:`n$(Get-Content $frontendLogErr -Raw)"
    }
    if (-not $frontendOutput) {
        $frontendOutput = @("<no log output captured>")
    }
    throw "Frontend process exited immediately with code $($frontendProcess.ExitCode).`n$($frontendOutput -join "`n`n")"
}

Write-Host ""
Write-Host "Backend PID: $($backendProcess.Id)" -ForegroundColor Green
Write-Host "Frontend PID: $($frontendProcess.Id)" -ForegroundColor Green
Write-Host ""
Write-Host "Both processes are running in their own windows. Ctrl+C this script to stop monitoring." -ForegroundColor Yellow