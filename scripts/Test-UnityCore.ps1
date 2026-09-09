param([Parameter(Mandatory)][string]$TaskTag, [switch]$SkipRefresh, [ValidateRange(120, 3600)][int]$TimeoutSec = 900)
$ErrorActionPreference = 'Stop'
if ($TaskTag -notmatch '^[a-zA-Z0-9_-]+$') { throw 'TaskTag must be a plain artifact name.' }
$artifactDirectory = Join-Path $PSScriptRoot '../artifacts/physiology'
New-Item -ItemType Directory -Force $artifactDirectory | Out-Null
if (!$SkipRefresh) { $null = & "$PSScriptRoot/Invoke-UnityMcp.ps1" -Tool assets-refresh }
$run = & "$PSScriptRoot/Invoke-UnityMcp.ps1" -Tool tests-run -Arguments @{
    testMode='EditMode'; testAssembly='Biathlon.Tests'; includePassingTests=$true; includeStacktrace=$true
} -TimeoutSec $TimeoutSec -OutputFile (Join-Path $artifactDirectory "$TaskTag-editmode.json")
$summary = $run.structuredContent.result.Summary
$summary | ConvertTo-Json
if (!$summary -or $summary.Status -ne 'Passed' -or $summary.SkippedTests -ne 0 -or $summary.TotalTests -le 0) {
    $run.structuredContent.result.Results | Where-Object Status -ne Passed | ConvertTo-Json -Depth 8
    throw 'Unity core suite did not pass completely.'
}
$console = & "$PSScriptRoot/Invoke-UnityMcp.ps1" -Tool reflection-method-call -Arguments @{
    filter=@{Namespace='Biathlon.Unity.Editor';TypeName='FoundationDiagnostics';MethodName='ConsoleState';IsStatic=$true;IsPublic=$true;InputParameters=@()}
    inputParameters=@()
} -OutputFile (Join-Path $artifactDirectory "$TaskTag-console.json")
$counts = $console.structuredContent.result.value | ConvertFrom-Json
$counts | ConvertTo-Json
if (!$counts -or $counts.compilation_failed -or $counts.errors -ne 0) { throw 'Inspect Unity compilation/Console errors.' }
