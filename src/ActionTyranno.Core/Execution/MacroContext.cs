using System.Text.RegularExpressions;

namespace ActionTyranno.Core.Execution;

/// <summary>
/// Per-run variable store. Text fields may reference variables as {Name}.
/// Unknown placeholders are left untouched.
/// </summary>
public class MacroContext
{
    private static readonly Regex Placeholder = new(@"\{(\w+)\}", RegexOptions.Compiled);

    private readonly Dictionary<string, string> _variables = new(StringComparer.OrdinalIgnoreCase);

    public void Set(string name, string value) => _variables[name] = value;

    public bool TryGet(string name, out string value) => _variables.TryGetValue(name, out value!);

    public string Expand(string? template)
    {
        if (string.IsNullOrEmpty(template))
            return string.Empty;

        return Placeholder.Replace(template, m =>
            _variables.TryGetValue(m.Groups[1].Value, out var v) ? v : m.Value);
    }
}
