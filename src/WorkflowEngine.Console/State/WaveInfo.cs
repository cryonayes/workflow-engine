namespace WorkflowEngine.Console.State;

/// <summary>
/// Information about a wave in the execution plan.
/// </summary>
/// <param name="Index">The wave index.</param>
/// <param name="IsAlways">Whether this is an always() wave.</param>
/// <param name="Status">Current status of the wave.</param>
internal sealed record WaveInfo(int Index, bool IsAlways, WaveStatus Status = WaveStatus.Pending);
