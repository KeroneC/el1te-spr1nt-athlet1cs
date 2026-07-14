param([Parameter(Mandatory)][string]$BaseUrl)

$ErrorActionPreference = "Stop"
$BaseUrl = $BaseUrl.TrimEnd('/')
$MaxAttempts = 40

function Test-Endpoint([string]$Path, [string]$Marker) {
    for ($attempt = 1; $attempt -le $MaxAttempts; $attempt++) {
        try {
            $response = Invoke-WebRequest -UseBasicParsing "$BaseUrl$Path" -TimeoutSec 10
            if ($response.StatusCode -eq 200 -and $response.Content.Contains($Marker)) {
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

Test-Endpoint "/" "El1te Spr1nt Athlet1cs"
Test-Endpoint "/news" "Club announcements"
Test-Endpoint "/events" "Schedule"
Test-Endpoint "/gallery" "Gallery"
Test-Endpoint "/registration" "Registration information"
Test-Endpoint "/admin/login" "Admin sign in"
