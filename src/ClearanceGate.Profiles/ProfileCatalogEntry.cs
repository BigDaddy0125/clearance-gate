namespace ClearanceGate.Profiles;

public sealed record ProfileCatalogEntry(
    string Profile,
    string Family,
    int Version,
    string Description,
    bool IsLatest);
