Write-Host "o_o Setting up Taskify solution..." -ForegroundColor Cyan

# Define solution file path
$solutionPath = "Taskify.sln"

# Remove all existing projects from solution (optional safety reset)
$existingProjects = dotnet sln $solutionPath list
if ($existingProjects) {
    Write-Host "Removing existing projects..." -ForegroundColor Yellow
    foreach ($proj in $existingProjects) {
        dotnet sln $solutionPath remove $proj | Out-Null
    }
}

# Add all .csproj files recursively
Write-Host "Adding all .csproj files to the solution..." -ForegroundColor Cyan
Get-ChildItem -Recurse -Filter *.csproj | ForEach-Object {
    dotnet sln $solutionPath add $_.FullName | Out-Null
    Write-Host "-_- Added $($_.FullName)"
}

Write-Host "--------------------------------------------"
Write-Host "o_o Solution setup complete!" -ForegroundColor Green
Write-Host "--------------------------------------------"
dotnet sln $solutionPath list
