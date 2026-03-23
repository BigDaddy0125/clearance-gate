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
    private readonly IReadOnlyList<ProfileCatalogEntry> catalogEntries;
    private readonly Dictionary<string, ProfileCatalogEntry> latestProfilesByFamily;

    public EmbeddedProfileCatalog()
    {
        var catalog = LoadProfiles();
        profiles = catalog.Profiles;
        catalogEntries = catalog.Entries;
        latestProfilesByFamily = catalog.LatestProfilesByFamily;
    }

    public ClearanceProfile GetRequiredProfile(string profileName)
    {
        if (profiles.TryGetValue(profileName, out var profile))
        {
            return profile;
        }

        throw new KeyNotFoundException($"Profile '{profileName}' is not registered.");
    }

    public IReadOnlyList<ProfileCatalogEntry> ListProfiles() => catalogEntries;

    public ProfileCatalogEntry GetLatestProfile(string family)
    {
        if (latestProfilesByFamily.TryGetValue(family, out var entry))
        {
            return entry;
        }

        throw new KeyNotFoundException($"Profile family '{family}' is not registered.");
    }

    private static LoadedProfileCatalog LoadProfiles()
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

        var profiles = ProfileCatalogValidator.ValidateProfiles(loadedProfiles);
        var identities = profiles.Values
            .Select(profile => new
            {
                Profile = profile,
                Identity = ProfileVersionIdentity.Parse(profile.Profile),
            })
            .OrderBy(item => item.Identity.Family, StringComparer.Ordinal)
            .ThenBy(item => item.Identity.Version)
            .ToArray();

        var latestByFamily = identities
            .GroupBy(item => item.Identity.Family, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    var latest = group.OrderByDescending(item => item.Identity.Version).First();
                    return new ProfileCatalogEntry(
                        latest.Profile.Profile,
                        latest.Identity.Family,
                        latest.Identity.Version,
                        latest.Profile.Description,
                        true);
                },
                StringComparer.Ordinal);

        var entries = identities
            .Select(item =>
            {
                var latest = latestByFamily[item.Identity.Family];
                return new ProfileCatalogEntry(
                    item.Profile.Profile,
                    item.Identity.Family,
                    item.Identity.Version,
                    item.Profile.Description,
                    latest.Profile == item.Profile.Profile);
            })
            .ToArray();

        return new LoadedProfileCatalog(profiles, entries, latestByFamily);
    }

    private sealed record LoadedProfileCatalog(
        Dictionary<string, ClearanceProfile> Profiles,
        IReadOnlyList<ProfileCatalogEntry> Entries,
        Dictionary<string, ProfileCatalogEntry> LatestProfilesByFamily);
}
