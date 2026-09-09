param(
    [string]$Tool,
    [hashtable]$Arguments = @{},
    [switch]$ListTools,
    [string]$OutputFile,
    [int]$TimeoutSec = 120
)
$ErrorActionPreference = 'Stop'
$endpoint = 'http://localhost:29577/mcp'
$headers = @{ Accept = 'application/json, text/event-stream' }
function Invoke-Rpc($payload) {
    $response = Invoke-WebRequest $endpoint -Method Post -Headers $headers -ContentType 'application/json' -Body ($payload | ConvertTo-Json -Depth 50 -Compress) -TimeoutSec $TimeoutSec
    if ($response.Headers['Mcp-Session-Id']) { $headers['Mcp-Session-Id'] = [string]$response.Headers['Mcp-Session-Id'][0] }
    $body = [string]$response.Content
    if (!$body.Trim()) { return }
    if ($body.TrimStart().StartsWith('{')) { return $body | ConvertFrom-Json -Depth 100 }
    foreach ($line in ($body -split "`n")) {
        if ($line.StartsWith('data: ')) {
            $item = $line.Substring(6) | ConvertFrom-Json -Depth 100
            if ($null -ne $item.id) { return $item }
        }
    }
}
$init = Invoke-Rpc @{ jsonrpc='2.0'; id=1; method='initialize'; params=@{ protocolVersion='2025-03-26'; capabilities=@{}; clientInfo=@{ name='codex-biathlon'; version='1.0' } } }
if ($init.error) { throw ($init.error | ConvertTo-Json) }
$null = Invoke-Rpc @{ jsonrpc='2.0'; method='notifications/initialized' }
$method = if ($ListTools) { 'tools/list' } else { 'tools/call' }
$parameters = if ($ListTools) { @{} } else { @{ name=$Tool; arguments=$Arguments } }
$result = Invoke-Rpc @{ jsonrpc='2.0'; id=2; method=$method; params=$parameters }
if ($result.error) { throw ($result.error | ConvertTo-Json -Depth 10) }
if ($OutputFile) { $result | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath $OutputFile -Encoding utf8 }
if ($result.result.isError) { throw ($result.result | ConvertTo-Json -Depth 20) }
$result.result
