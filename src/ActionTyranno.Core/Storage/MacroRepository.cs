using System.Text.Json;
using System.Text.Json.Serialization;
using ActionTyranno.Core.Models;

namespace ActionTyranno.Core.Storage;

/// <summary>
/// Persists all macros as a single JSON array file and keeps an in-memory cache in sync with it.
/// </summary>
public class MacroRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _filePath;
    private readonly object _lock = new();
    private List<Macro> _macros = new();

    public MacroRepository(string? filePath = null)
    {
        _filePath = filePath ?? GetDefaultFilePath();
        Load();
    }

    public static string GetDefaultFilePath()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ActionTyranno");
        return Path.Combine(dir, "macros.json");
    }

    public IReadOnlyList<Macro> GetAll()
    {
        lock (_lock)
        {
            return _macros.OrderBy(m => m.Id).ToList();
        }
    }

    public Macro? GetById(int id)
    {
        lock (_lock)
        {
            return _macros.FirstOrDefault(m => m.Id == id);
        }
    }

    public Macro Add(string name, List<MacroAction>? actions = null)
    {
        lock (_lock)
        {
            var nextId = _macros.Count == 0 ? 1 : _macros.Max(m => m.Id) + 1;
            var macro = new Macro
            {
                Id = nextId,
                Name = name,
                Actions = actions ?? new List<MacroAction>()
            };
            _macros.Add(macro);
            Save();
            return macro;
        }
    }

    public void Update(Macro macro)
    {
        lock (_lock)
        {
            var index = _macros.FindIndex(m => m.Id == macro.Id);
            if (index < 0)
                throw new InvalidOperationException($"Macro with id {macro.Id} not found.");

            _macros[index] = macro;
            Save();
        }
    }

    public bool Delete(int id)
    {
        lock (_lock)
        {
            var removed = _macros.RemoveAll(m => m.Id == id) > 0;
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
                _macros = new List<Macro>();
                return;
            }

            var json = File.ReadAllText(_filePath);
            _macros = string.IsNullOrWhiteSpace(json)
                ? new List<Macro>()
                : JsonSerializer.Deserialize<List<Macro>>(json, JsonOptions) ?? new List<Macro>();
        }
    }

    public void Save()
    {
        lock (_lock)
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(_macros, JsonOptions);
            File.WriteAllText(_filePath, json);
        }
    }
}
