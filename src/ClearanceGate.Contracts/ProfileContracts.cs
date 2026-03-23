namespace ClearanceGate.Contracts;

public sealed record ProfileCatalogResponse(
    IReadOnlyList<ProfileCatalogItemResponse> Profiles);

public sealed record ProfileCatalogItemResponse(
    string Profile,
    string Family,
    int Version,
    string Description,
    bool IsLatest);

public sealed record LatestProfileResponse(
    string Family,
    ProfileCatalogItemResponse Profile);
