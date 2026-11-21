[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root 'CLDV_Final/CLDV_Final.csproj'
$contentRoot = Join-Path $root 'CLDV_Final'
$previousDataDirectory = $env:ABCRETAIL_DEMO_DATA_DIR
$env:ABCRETAIL_DEMO_DATA_DIR = Join-Path $contentRoot 'App_Data/PersonalDemo'
Push-Location $root
try {
    Write-Host 'Starting ABC Retail personal demo: http://127.0.0.1:5097' -ForegroundColor Cyan
    Write-Host 'Use fictional details and sample files. No Azure account or passwords are required.'
    Write-Host 'Wait for the listening message. Press Ctrl+C to stop.'
    & dotnet run --project $project --configuration Release --no-build --no-launch-profile -- '--environment' 'Development' '--contentRoot' $contentRoot '--urls' 'http://127.0.0.1:5097'
    if ($LASTEXITCODE -ne 0) { throw 'The website stopped with an error. Paste the output here.' }
}
finally { $env:ABCRETAIL_DEMO_DATA_DIR = $previousDataDirectory; Pop-Location }
