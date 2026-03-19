using System.Reflection;
using System.Text.Json;

namespace ClearanceGate.Profiles;

public sealed class EmbeddedProfileCatalog : IProfileCatalog
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly Dictionary<string, ClearanceProfile> profiles;

    public EmbeddedProfileCatalog()
    {
        profiles = LoadProfiles();
    }

    public ClearanceProfile GetRequiredProfile(string profileName)
    {
        if (profiles.TryGetValue(profileName, out var profile))
        {
            return profile;
        }

        throw new KeyNotFoundException($"Profile '{profileName}' is not registered.");
    }

    private static Dictionary<string, ClearanceProfile> LoadProfiles()
    {
        var assembly = typeof(EmbeddedProfileCatalog).Assembly;
        var resourceNames = assembly.GetManifestResourceNames()
            .Where(name => name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        if (resourceNames.Length == 0)
        {
            throw new InvalidOperationException("No embedded profile definitions were found.");
        }

        var loadedProfiles = new List<(ClearanceProfile Profile, string SourceName)>();

        foreach (var resourceName in resourceNames)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Embedded profile resource '{resourceName}' could not be read.");
            var profile = JsonSerializer.Deserialize<ClearanceProfile>(stream, SerializerOptions)
                ?? throw new InvalidOperationException($"Embedded profile resource '{resourceName}' could not be deserialized.");

            loadedProfiles.Add((profile, resourceName));
        }

        return ProfileCatalogValidator.ValidateProfiles(loadedProfiles);
    }
}
