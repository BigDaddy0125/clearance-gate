namespace ClearanceGate.Profiles;

public interface IProfileCatalog
{
    ClearanceProfile GetRequiredProfile(string profileName);

    IReadOnlyList<ProfileCatalogEntry> ListProfiles();

    ProfileCatalogEntry GetLatestProfile(string family);
}
