using System.Text.Json;
using ActionTyranno.Core.Models;

namespace ActionTyranno.Core.Storage;

/// <summary>
/// Persists all prompt templates as a single JSON array file (prompts.json), same pattern as MacroRepository.
/// </summary>
public class PromptTemplateRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        // Keep Korean text readable in the file instead of \uXXXX escapes.
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly string _filePath;
    private readonly object _lock = new();
    private List<PromptTemplate> _templates = new();

    public PromptTemplateRepository(string? filePath = null)
    {
        _filePath = filePath ?? GetDefaultFilePath();
        Load();
    }

    public static string GetDefaultFilePath()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ActionTyranno");
        return Path.Combine(dir, "prompts.json");
    }

    public IReadOnlyList<PromptTemplate> GetAll()
    {
        lock (_lock)
        {
            return _templates.OrderBy(t => t.Id).ToList();
        }
    }

    public PromptTemplate? GetById(int id)
    {
        lock (_lock)
        {
            return _templates.FirstOrDefault(t => t.Id == id);
        }
    }

    public PromptTemplate Add(string name, string systemPrompt, string userPromptTemplate)
    {
        lock (_lock)
        {
            var template = new PromptTemplate
            {
                Id = _templates.Count == 0 ? 1 : _templates.Max(t => t.Id) + 1,
                Name = name,
                SystemPrompt = systemPrompt,
                UserPromptTemplate = userPromptTemplate
            };
            _templates.Add(template);
            Save();
            return template;
        }
    }

    public void Update(PromptTemplate template)
    {
        lock (_lock)
        {
            var index = _templates.FindIndex(t => t.Id == template.Id);
            if (index < 0)
                throw new InvalidOperationException($"Prompt template with id {template.Id} not found.");

            _templates[index] = template;
            Save();
        }
    }

    public bool Delete(int id)
    {
        lock (_lock)
        {
            var removed = _templates.RemoveAll(t => t.Id == id) > 0;
            if (removed)
                Save();
            return removed;
        }
    }

    public void Load()
    {
        lock (_lock)
        {
            if (!File.Exists(_filePath))
            {
                _templates = new List<PromptTemplate>();
                return;
            }

            var json = File.ReadAllText(_filePath);
            _templates = string.IsNullOrWhiteSpace(json)
                ? new List<PromptTemplate>()
                : JsonSerializer.Deserialize<List<PromptTemplate>>(json, JsonOptions) ?? new List<PromptTemplate>();
        }
    }

    public void Save()
    {
        lock (_lock)
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(_templates, JsonOptions);
            File.WriteAllText(_filePath, json);
        }
    }
}
