using System.Text.RegularExpressions;

namespace ClearanceGate.Profiles;

public sealed record ProfileVersionIdentity(
    string Family,
    int Version,
    string CanonicalName)
{
    private static readonly Regex Pattern = new(
        "^(?<family>[a-z0-9]+(?:_[a-z0-9]+)*)_v(?<version>[1-9][0-9]*)$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static ProfileVersionIdentity Parse(string profileName)
    {
        if (!TryParse(profileName, out var identity))
        {
            throw new InvalidOperationException(
                $"Profile '{profileName}' must use canonical name '<family>_v<positive integer>'.");
        }

        return identity!;
    }

    public static bool TryParse(string? profileName, out ProfileVersionIdentity? identity)
    {
        identity = null;
        if (string.IsNullOrWhiteSpace(profileName))
        {
            return false;
        }

        var match = Pattern.Match(profileName);
        if (!match.Success)
        {
            return false;
        }

        if (!int.TryParse(match.Groups["version"].Value, out var version))
        {
            return false;
        }

        identity = new ProfileVersionIdentity(
            match.Groups["family"].Value,
            version,
            profileName);
        return true;
    }
}
