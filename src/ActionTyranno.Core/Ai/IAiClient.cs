namespace ActionTyranno.Core.Ai;

/// <summary>
/// Sends a prompt to an AI backend and returns its text answer.
/// Implementations must throw on any failure so the macro aborts instead of continuing with an empty result.
/// </summary>
public interface IAiClient
{
    Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken);
}
