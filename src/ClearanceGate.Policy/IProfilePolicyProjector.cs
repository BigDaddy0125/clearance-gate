namespace ClearanceGate.Policy;

public interface IProfilePolicyProjector
{
    ProjectedPolicyProfile Project(ClearanceGate.Profiles.ClearanceProfile profile);
}
