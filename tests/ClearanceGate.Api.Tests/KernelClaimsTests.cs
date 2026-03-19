using Xunit;

namespace ClearanceGate.Api.Tests;

public sealed class KernelClaimsTests
{
    // CG1: each known clearance state maps to exactly one defined authorization outcome
    [Fact]
    public void EveryKnownState_MapsToExactlyOneDefinedOutcome()
    {
        var states = Enum.GetValues<ClearanceGate.Kernel.ClearanceState>();
        var allowedOutcomes = Enum.GetValues<ClearanceGate.Kernel.AuthorizationOutcome>()
            .ToHashSet();

        foreach (var state in states)
        {
            var outcome = ClearanceGate.Kernel.ClearanceKernel.MapOutcome(state);
            Assert.Contains(outcome, allowedOutcomes);
        }
    }

    // CG2: degraded or insufficient states must never fail open to PROCEED
    [Fact]
    public void DegradedAndInsufficientStates_NeverProceed()
    {
        Assert.NotEqual(
            ClearanceGate.Kernel.AuthorizationOutcome.Proceed,
            ClearanceGate.Kernel.ClearanceKernel.MapOutcome(ClearanceGate.Kernel.ClearanceState.Degraded));

        Assert.NotEqual(
            ClearanceGate.Kernel.AuthorizationOutcome.Proceed,
            ClearanceGate.Kernel.ClearanceKernel.MapOutcome(ClearanceGate.Kernel.ClearanceState.InfoInsufficient));
    }
}
