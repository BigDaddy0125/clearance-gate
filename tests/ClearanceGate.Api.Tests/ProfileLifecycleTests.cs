using Xunit;

namespace ClearanceGate.Api.Tests;

public sealed class ProfileLifecycleTests
{
    [Theory]
    [InlineData("itops_deployment_v1", "itops_deployment", 1)]
    [InlineData("kernel_boundary_v12", "kernel_boundary", 12)]
    public void ProfileVersionIdentity_Parse_AcceptsCanonicalNames(
        string profileName,
        string expectedFamily,
        int expectedVersion)
    {
        var identity = ClearanceGate.Profiles.ProfileVersionIdentity.Parse(profileName);

        Assert.Equal(expectedFamily, identity.Family);
        Assert.Equal(expectedVersion, identity.Version);
        Assert.Equal(profileName, identity.CanonicalName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("itops_deployment")]
    [InlineData("itops_deployment_v0")]
    [InlineData("ItOps_Deployment_v1")]
    [InlineData("itops-deployment-v1")]
    public void ProfileVersionIdentity_Parse_RejectsNonCanonicalNames(string profileName)
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ClearanceGate.Profiles.ProfileVersionIdentity.Parse(profileName));

        Assert.Contains("must use canonical name", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ProfileCatalogValidator_RejectsDuplicateFamilyVersion()
    {
        var profiles = new[]
        {
            (CreateProfile("itops_deployment_v1"), "profile-a.json"),
            (CreateProfile("itops_deployment_v1"), "profile-b.json"),
        };

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ClearanceGate.Profiles.ProfileCatalogValidator.ValidateProfiles(profiles));

        Assert.Contains("defines version '1' more than once", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ProfileCatalogValidator_RejectsMissingDescription()
    {
        var profiles = new[]
        {
            (CreateProfile("itops_deployment_v1") with { Description = "" }, "profile-a.json"),
        };

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ClearanceGate.Profiles.ProfileCatalogValidator.ValidateProfiles(profiles));

        Assert.Contains("non-empty description", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ProfileCatalogValidator_AllowsDistinctVersionsWithinSameFamily()
    {
        var profiles = new[]
        {
            (CreateProfile("itops_deployment_v1"), "profile-a.json"),
            (CreateProfile("itops_deployment_v2"), "profile-b.json"),
        };

        var catalog = ClearanceGate.Profiles.ProfileCatalogValidator.ValidateProfiles(profiles);

        Assert.Equal(2, catalog.Count);
        Assert.Contains("itops_deployment_v1", catalog.Keys);
        Assert.Contains("itops_deployment_v2", catalog.Keys);
    }

    [Fact]
    public void ProfileVersionIdentity_TryParse_ReturnsFalseForInvalidName()
    {
        var parsed = ClearanceGate.Profiles.ProfileVersionIdentity.TryParse("bad-profile-name", out var identity);

        Assert.False(parsed);
        Assert.Null(identity);
    }

    private static ClearanceGate.Profiles.ClearanceProfile CreateProfile(string profileName) =>
        new(
            profileName,
            "Profile description",
            new[]
            {
                ClearanceGate.Profiles.KernelResponsibilityRoles.DecisionOwner,
                ClearanceGate.Profiles.KernelResponsibilityRoles.AcknowledgingAuthority,
                ClearanceGate.Profiles.KernelResponsibilityRoles.AuditReviewer,
            },
            new ClearanceGate.Profiles.ClearanceProfileConstraint[]
            {
                new("RISK_ACK_REQUIRED", ClearanceGate.Profiles.ProfileConstraintKinds.AckRequired, null, "HIGH_IMPACT"),
            });
}
