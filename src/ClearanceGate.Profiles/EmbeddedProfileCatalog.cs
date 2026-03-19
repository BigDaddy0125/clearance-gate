using System.Reflection;
using System.Text.Json;

namespace ClearanceGate.Profiles;

public sealed class EmbeddedProfileCatalog : IProfileCatalog
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static readonly string[] RequiredKernelRoles =
    {
        KernelResponsibilityRoles.DecisionOwner,
        KernelResponsibilityRoles.AcknowledgingAuthority,
        KernelResponsibilityRoles.AuditReviewer,
    };

    private static readonly string[] AllowedConstraintKinds =
    {
        "ack_required",
        "required_field",
    };

    private static readonly string[] AllowedRequiredFields =
    {
        "responsibility.owner",
        "metadata.source_system",
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

        var loadedProfiles = new Dictionary<string, ClearanceProfile>(StringComparer.Ordinal);

        foreach (var resourceName in resourceNames)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Embedded profile resource '{resourceName}' could not be read.");
            var profile = JsonSerializer.Deserialize<ClearanceProfile>(stream, SerializerOptions)
                ?? throw new InvalidOperationException($"Embedded profile resource '{resourceName}' could not be deserialized.");

            ValidateProfile(profile, resourceName);

            if (!loadedProfiles.TryAdd(profile.Profile, profile))
            {
                throw new InvalidOperationException($"Profile '{profile.Profile}' is defined more than once.");
            }
        }

        return loadedProfiles;
    }

    private static void ValidateProfile(ClearanceProfile profile, string resourceName)
    {
        if (string.IsNullOrWhiteSpace(profile.Profile))
        {
            throw new InvalidOperationException($"Profile resource '{resourceName}' is missing a profile identifier.");
        }

        if (profile.Constraints.Count == 0)
        {
            throw new InvalidOperationException($"Profile '{profile.Profile}' must define at least one constraint.");
        }

        var roleSet = new HashSet<string>(profile.ResponsibilityRoles, StringComparer.Ordinal);
        foreach (var requiredRole in RequiredKernelRoles)
        {
            if (!roleSet.Contains(requiredRole))
            {
                throw new InvalidOperationException($"Profile '{profile.Profile}' is missing required role '{requiredRole}'.");
            }
        }

        var constraintIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var constraint in profile.Constraints)
        {
            if (string.IsNullOrWhiteSpace(constraint.Id))
            {
                throw new InvalidOperationException($"Profile '{profile.Profile}' contains a constraint without an id.");
            }

            if (!constraintIds.Add(constraint.Id))
            {
                throw new InvalidOperationException($"Profile '{profile.Profile}' defines duplicate constraint id '{constraint.Id}'.");
            }

            if (!AllowedConstraintKinds.Contains(constraint.Kind, StringComparer.Ordinal))
            {
                throw new InvalidOperationException($"Profile '{profile.Profile}' uses unsupported constraint kind '{constraint.Kind}'.");
            }

            if (string.Equals(constraint.Kind, "required_field", StringComparison.Ordinal) &&
                !AllowedRequiredFields.Contains(constraint.Field, StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Profile '{profile.Profile}' uses unsupported required field '{constraint.Field}'.");
            }

            if (string.Equals(constraint.Kind, "ack_required", StringComparison.Ordinal) &&
                string.IsNullOrWhiteSpace(constraint.WhenRiskFlagPresent))
            {
                throw new InvalidOperationException(
                    $"Profile '{profile.Profile}' must specify whenRiskFlagPresent for ack_required constraint '{constraint.Id}'.");
            }
        }
    }
}
