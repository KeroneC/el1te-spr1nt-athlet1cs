param([Parameter(Mandatory)][string]$BaseUrl)

$ErrorActionPreference = "Stop"
$BaseUrl = $BaseUrl.TrimEnd('/')
$MaxAttempts = 40

function Test-Endpoint([string]$Path, [string]$Expected = "") {
    for ($attempt = 1; $attempt -le $MaxAttempts; $attempt++) {
        try {
            $response = Invoke-WebRequest -UseBasicParsing "$BaseUrl$Path" -TimeoutSec 10
            if ($response.StatusCode -eq 200 -and (!$Expected -or $response.Content.Contains($Expected))) {
                Write-Output "PASS $Path"
                return
            }
        } catch {
            if ($attempt -eq $MaxAttempts) { throw }
        }
        Start-Sleep -Seconds 5
    }
    throw "FAIL $Path"
}

Test-Endpoint "/health" '"status":"healthy"'
Test-Endpoint "/health/ready" '"status":"healthy"'
Test-Endpoint "/api/public/site-settings"
Test-Endpoint "/api/public/announcements?page=1&pageSize=1"
