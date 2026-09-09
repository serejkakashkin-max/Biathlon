# Automated tests
- Assets/Tests/EditMode: pure NUnit kernel tests, shared with dotnet/Biathlon.Tests.
- Assets/Tests/PlayMode: Unity bootstrap/config loading, ticking and pause integration.
- Run scripts/Test-Foundation.ps1 for the standalone suite.
- Run scripts/Invoke-UnityMcp.ps1 -Tool tests-run -Arguments @{testMode='EditMode';testAssembly='Biathlon.Tests';includePassingTests=$true}.
- Run the same command with testMode='PlayMode' and testAssembly='Biathlon.PlayModeTests'.
Tests require saved open scenes. Result artifacts are written to artifacts/foundation.
