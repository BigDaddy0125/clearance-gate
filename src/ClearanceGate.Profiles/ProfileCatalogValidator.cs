namespace ClearanceGate.Profiles;

public static class ProfileCatalogValidator
{
    private static readonly string[] RequiredKernelRoles =
    [
        KernelResponsibilityRoles.DecisionOwner,
        KernelResponsibilityRoles.AcknowledgingAuthority,
        KernelResponsibilityRoles.AuditReviewer,
    ];

    public static Dictionary<string, ClearanceProfile> ValidateProfiles(
        IEnumerable<(ClearanceProfile Profile, string SourceName)> profiles)
    {
        var loadedProfiles = new Dictionary<string, ClearanceProfile>(StringComparer.Ordinal);
        var identityIndex = new Dictionary<(string Family, int Version), string>();

        foreach (var (profile, sourceName) in profiles)
        {
            ValidateProfile(profile, sourceName);
            var identity = ProfileVersionIdentity.Parse(profile.Profile);
            var versionKey = (identity.Family, identity.Version);

            if (!identityIndex.TryAdd(versionKey, profile.Profile))
            {
                throw new InvalidOperationException(
                    $"Profile family '{identity.Family}' defines version '{identity.Version}' more than once.");
            }

            if (!loadedProfiles.TryAdd(profile.Profile, profile))
            {
                throw new InvalidOperationException($"Profile '{profile.Profile}' is defined more than once.");
            }
        }

        return loadedProfiles;
    }

    private static void ValidateProfile(ClearanceProfile profile, string sourceName)
    {
        if (string.IsNullOrWhiteSpace(profile.Profile))
        {
            throw new InvalidOperationException($"Profile resource '{sourceName}' is missing a profile identifier.");
        }

        if (string.IsNullOrWhiteSpace(profile.Description))
        {
            throw new InvalidOperationException($"Profile '{profile.Profile}' must define a non-empty description.");
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

            if (!IsAllowedConstraintKind(constraint.Kind))
            {
                throw new InvalidOperationException($"Profile '{profile.Profile}' uses unsupported constraint kind '{constraint.Kind}'.");
            }

            if (string.Equals(constraint.Kind, ProfileConstraintKinds.RequiredField, StringComparison.Ordinal) &&
                !IsAllowedRequiredField(constraint.Field))
            {
                throw new InvalidOperationException(
                    $"Profile '{profile.Profile}' uses unsupported required field '{constraint.Field}'.");
            }

            if (string.Equals(constraint.Kind, ProfileConstraintKinds.AckRequired, StringComparison.Ordinal) &&
                string.IsNullOrWhiteSpace(constraint.WhenRiskFlagPresent))
            {
                throw new InvalidOperationException(
                    $"Profile '{profile.Profile}' must specify whenRiskFlagPresent for ack_required constraint '{constraint.Id}'.");
            }
        }
    }

    private static bool IsAllowedConstraintKind(string? kind) =>
        string.Equals(kind, ProfileConstraintKinds.AckRequired, StringComparison.Ordinal) ||
        string.Equals(kind, ProfileConstraintKinds.RequiredField, StringComparison.Ordinal);

    private static bool IsAllowedRequiredField(string? field) =>
        string.Equals(field, ProfileFieldPaths.ResponsibilityOwner, StringComparison.Ordinal) ||
        string.Equals(field, ProfileFieldPaths.MetadataSourceSystem, StringComparison.Ordinal);
}
