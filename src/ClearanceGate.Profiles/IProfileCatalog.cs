namespace ClearanceGate.Profiles;

public interface IProfileCatalog
{
    ClearanceProfile GetRequiredProfile(string profileName);
}
