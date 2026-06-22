param([Parameter(Mandatory)][string]$BaseUrl)

$ErrorActionPreference = "Stop"
$BaseUrl = $BaseUrl.TrimEnd('/')

function Test-Endpoint([string]$Path, [string]$Marker) {
    for ($attempt = 1; $attempt -le 12; $attempt++) {
        try {
            $response = Invoke-WebRequest -UseBasicParsing "$BaseUrl$Path" -TimeoutSec 10
            if ($response.StatusCode -eq 200 -and $response.Content.Contains($Marker)) {
                Write-Output "PASS $Path"
                return
            }
        } catch {
            if ($attempt -eq 12) { throw }
        }
        Start-Sleep -Seconds 5
    }
    throw "FAIL $Path"
}

Test-Endpoint "/" "El1te Spr1nt Athlet1cs"
Test-Endpoint "/admin/login" "Admin sign in"
