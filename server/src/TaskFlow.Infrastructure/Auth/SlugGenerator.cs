using System.Text;
using System.Text.RegularExpressions;

namespace TaskFlow.Infrastructure.Auth;

internal static partial class SlugGenerator
{
    public static string Generate(string name)
    {
        var lowercase = name.Trim().ToLowerInvariant();
        var hyphenated = NonAlphanumericRun().Replace(lowercase, "-").Trim('-');

        return hyphenated.Length > 0 ? hyphenated : "org";
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonAlphanumericRun();
}
