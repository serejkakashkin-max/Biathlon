using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
namespace Biathlon.Unity.Editor
{
    public static class FoundationDiagnostics
    {
        public static string Assemblies() => string.Join("\n", AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name.StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal))
            .Select(a => a.FullName + " | " + a.Location));
        public static string ConsoleState()
        {
            var type = typeof(EditorApplication).Assembly.GetType("UnityEditor.LogEntries", true);
            var method = type.GetMethod("GetCountsByType", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            var counts = new object[] { 0, 0, 0 };
            method.Invoke(null, counts);
            return "{\"errors\":" + counts[0] + ",\"warnings\":" + counts[1] + ",\"logs\":" + counts[2]
                + ",\"compilation_failed\":" + EditorUtility.scriptCompilationFailed.ToString().ToLowerInvariant() + "}";
        }
    }
}
