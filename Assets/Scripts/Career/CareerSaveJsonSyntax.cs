using Biathlon.Domain;

namespace Biathlon.Career
{
    // Retain the established career boundary while sharing the syntax guard with settings.
    internal static class CareerSaveJsonSyntax
    {
        internal static void Validate(string text)=>JsonDocumentSyntax.Validate(text);
    }
}
