namespace ClearanceGate.Kernel;

public static class WireNames
{
    public static string ToWireName(this ClearanceState state) =>
        state switch
        {
            ClearanceState.Init => "INIT",
            ClearanceState.InfoInsufficient => "INFO_INSUFFICIENT",
            ClearanceState.RiskFlagged => "RISK_FLAGGED",
            ClearanceState.AwaitingAck => "AWAITING_ACK",
            ClearanceState.Authorized => "AUTHORIZED",
            ClearanceState.Blocked => "BLOCKED",
            ClearanceState.Degraded => "DEGRADED",
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown clearance state."),
        };

    public static string ToWireName(this AuthorizationOutcome outcome) =>
        outcome switch
        {
            AuthorizationOutcome.Proceed => KernelOutcomeNames.Proceed,
            AuthorizationOutcome.Block => KernelOutcomeNames.Block,
            AuthorizationOutcome.RequireAck => KernelOutcomeNames.RequireAck,
            AuthorizationOutcome.Degrade => KernelOutcomeNames.Degrade,
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, "Unknown authorization outcome."),
        };
}
