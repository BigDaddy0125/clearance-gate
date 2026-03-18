namespace ClearanceGate.Policy;

public sealed record PolicyEvaluationResult(
    ClearanceGate.Kernel.ClearanceState State,
    string Summary,
    IReadOnlyList<string> ConstraintsTriggered);
