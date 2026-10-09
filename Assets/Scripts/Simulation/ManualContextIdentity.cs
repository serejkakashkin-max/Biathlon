using System;
using System.Security.Cryptography;
using System.Text;

namespace Biathlon.Simulation
{
    /// <summary>Cold references to already owned immutable context documents.
    /// A fingerprint is an identity, not a replacement for the document needed
    /// to reconstruct a race. It must be resolved against the exact attachment.</summary>
    public static class ManualContextIdentity
    {
        public static string Sha256(string context)
        {
            if(context==null)throw new ArgumentNullException(nameof(context));
            using(var hash=SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(context))).Replace("-","").ToLowerInvariant();
        }
    }
}
