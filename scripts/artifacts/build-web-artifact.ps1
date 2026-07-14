param(
    [string]$WebDirectory = "apps/web",
    [string]$Output = "artifacts/web"
)

$ErrorActionPreference = "Stop"
$server = Join-Path $WebDirectory ".next/standalone/apps/web/server.js"
if (-not (Test-Path -LiteralPath $server)) {
    throw "Standalone server not found. Run the production frontend build first."
}
$buildId = Join-Path $WebDirectory ".next/standalone/apps/web/.next/BUILD_ID"
if (-not (Test-Path -LiteralPath $buildId)) {
    throw "Standalone build metadata not found. Run the production frontend build first."
}

if (Test-Path -LiteralPath $Output) { Remove-Item -LiteralPath $Output -Recurse -Force }
New-Item -ItemType Directory -Force -Path $Output | Out-Null
Copy-Item (Join-Path $WebDirectory ".next/standalone/*") $Output -Recurse -Force

$webOutput = Join-Path $Output "apps/web"
New-Item -ItemType Directory -Force -Path (Join-Path $webOutput ".next") | Out-Null
Copy-Item (Join-Path $WebDirectory ".next/static") (Join-Path $webOutput ".next/static") -Recurse -Force
Copy-Item (Join-Path $WebDirectory "public") (Join-Path $webOutput "public") -Recurse -Force

Write-Output "Web artifact staged at $Output; start with: node apps/web/server.js"
