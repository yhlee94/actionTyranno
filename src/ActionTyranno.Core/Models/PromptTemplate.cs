namespace ActionTyranno.Core.Models;

public class PromptTemplate
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    // Rules the AI must follow (e.g. the report format guide)
    public string SystemPrompt { get; set; } = string.Empty;

    // Per-run request text; may contain {Variable} placeholders such as {LogText}
    public string UserPromptTemplate { get; set; } = string.Empty;
}
