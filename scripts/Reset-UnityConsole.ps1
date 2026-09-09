$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$artifacts = Join-Path $root 'artifacts/foundation'
New-Item -ItemType Directory -Force $artifacts | Out-Null
$archive = Join-Path $artifacts ("console-before-reset-" + (Get-Date -Format 'yyyyMMdd-HHmmss') + ".json")
$null = & (Join-Path $PSScriptRoot 'Invoke-UnityMcp.ps1') -Tool console-get-logs -Arguments @{maxEntries=10000} -OutputFile $archive
# MCP 0.90.0 deletes its log file on Clear(), which fails when another reader holds it
# without FileShare.Delete on Windows. Dispose/flush, truncate with sharing, then recreate.
$code = @'
using System.IO;
using System.Reflection;
using UnityEditor;
using com.IvanMurzak.Unity.MCP;
public static class ResetFoundationConsole
{
    public static void Run()
    {
        var plugin = UnityMcpPluginEditor.Instance;
        plugin.DisposeLogCollector();
        try
        {
            var file = Path.GetFullPath("Temp/mcp-server/ai-editor-logs.txt");
            using (var stream = new FileStream(file, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
                stream.SetLength(0);
            typeof(EditorApplication).Assembly.GetType("UnityEditor.LogEntries", true)
                .GetMethod("Clear", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
        }
        finally { plugin.AddUnityLogCollectorIfNeeded(() => new BufferedFileLogStorage()); }
    }
}
'@
& (Join-Path $PSScriptRoot 'Invoke-UnityMcp.ps1') -Tool script-execute -Arguments @{
    csharpCode=$code; className='ResetFoundationConsole'; methodName='Run'
}
Write-Host "Previous Console captured in $archive"
