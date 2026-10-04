using System;
using System.IO;
using NUnit.Framework;

namespace Biathlon.Tests
{
    internal static class PhysicalApronDraftFixtureFiles
    {
        internal static DirectoryInfo Root(string folder = "candidate", string marker = "apron.json")
        {
            foreach (string startingDirectory in new[] { TestContext.CurrentContext.TestDirectory, Environment.CurrentDirectory })
            {
                var dir = new DirectoryInfo(startingDirectory);
                while (dir != null)
                {
                    if (File.Exists(Path.Combine(dir.FullName, folder, marker))) return dir;
                    string draft = Path.Combine(dir.FullName, "tests/fixtures/venues/shared_apron_contract_v1");
                    if (File.Exists(Path.Combine(draft, folder, marker))) return new DirectoryInfo(draft);
                    dir = dir.Parent;
                }
            }
            throw new FileNotFoundException("Frozen compact apron test fixture was not found: " + folder + "/" + marker);
        }
    }
}
