namespace ClearanceGate.Kernel;

public enum ClearanceState
{
    Init = 0,
    InfoInsufficient = 1,
    RiskFlagged = 2,
    AwaitingAck = 3,
    Authorized = 4,
    Blocked = 5,
    Degraded = 6,
}

public enum AuthorizationOutcome
{
    Proceed = 0,
    Block = 1,
    RequireAck = 2,
    Degrade = 3,
}

public static class ClearanceKernel
{
    public static AuthorizationOutcome MapOutcome(ClearanceState state) =>
        state switch
        {
            ClearanceState.Authorized => AuthorizationOutcome.Proceed,
            ClearanceState.AwaitingAck => AuthorizationOutcome.RequireAck,
            ClearanceState.Degraded => AuthorizationOutcome.Degrade,
            ClearanceState.Init => AuthorizationOutcome.Block,
            ClearanceState.InfoInsufficient => AuthorizationOutcome.Block,
            ClearanceState.RiskFlagged => AuthorizationOutcome.Block,
            ClearanceState.Blocked => AuthorizationOutcome.Block,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Unknown clearance state."),
        };
}

public sealed record DecisionEvaluation(
    string DecisionId,
    ClearanceState State);
