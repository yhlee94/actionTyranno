using System.ComponentModel;
using System.Diagnostics;
using ActionTyranno.Core.Ai;
using ActionTyranno.Core.Input;
using ActionTyranno.Core.Models;
using ActionTyranno.Core.Storage;

namespace ActionTyranno.Core.Execution;

/// <summary>
/// Runs a macro's action sequence via InputSimulator, repeating it Macro.RepeatCount times.
/// Runs entirely on the calling task (intended to be started via Task.Run from the UI)
/// and can be stopped mid-flight through the supplied CancellationToken.
/// Any exception thrown by an action aborts the whole run.
/// </summary>
public class MacroPlayer
{
    // Guard against sending huge files to the AI step.
    private const long MaxReadFileBytes = 512 * 1024;

    private const string DefaultAiResultVariable = "AiResult";

    private readonly InputSimulator _simulator = new();
    private readonly IAiClient _aiClient;
    private readonly PromptTemplateRepository? _promptRepository;

    /// <param name="aiClient">AI backend for AiRequest actions; defaults to the local Claude CLI.</param>
    /// <param name="promptRepository">Template source; when null, prompts.json is re-read on every AiRequest.</param>
    public MacroPlayer(IAiClient? aiClient = null, PromptTemplateRepository? promptRepository = null)
    {
        _aiClient = aiClient ?? new ClaudeCliClient();
        _promptRepository = promptRepository;
    }

    public async Task RunAsync(Macro macro, CancellationToken cancellationToken)
    {
        var repeatCount = Math.Max(1, macro.RepeatCount);
        var context = new MacroContext();

        for (var i = 0; i < repeatCount; i++)
        {
            foreach (var action in macro.Actions)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await ExecuteAsync(action, context, cancellationToken);

                var waitMs = IsMouseAction(action.Type) ? macro.MouseActionDelayMs : action.DelayAfterMs;
                if (waitMs > 0)
                    await Task.Delay(waitMs, cancellationToken);
            }
        }
    }

    private async Task ExecuteAsync(MacroAction action, MacroContext context, CancellationToken cancellationToken)
    {
        switch (action.Type)
        {
            case ActionType.MouseMove:
                _simulator.MoveMouseTo(action.X ?? 0, action.Y ?? 0);
                break;

            case ActionType.MouseClick:
                if (action.DoubleClick)
                    _simulator.DoubleClick(action.X ?? 0, action.Y ?? 0, action.Button ?? MouseButton.Left);
                else
                    _simulator.Click(action.X ?? 0, action.Y ?? 0, action.Button ?? MouseButton.Left);
                break;

            case ActionType.MouseScroll:
                _simulator.Scroll(action.X ?? 0, action.Y ?? 0, action.ScrollAmount ?? 0);
                break;

            case ActionType.KeyPress:
                if (!string.IsNullOrEmpty(action.Key))
                    _simulator.KeyPress(action.Key);
                break;

            case ActionType.KeyCombo:
                if (action.Keys is { Count: > 0 })
                    _simulator.KeyCombo(action.Keys);
                break;

            case ActionType.Delay:
                // No-op: the wait itself is handled by DelayAfterMs after this call.
                break;

            case ActionType.ReadFile:
                await ReadFileAsync(action, context, cancellationToken);
                break;

            case ActionType.PasteText:
                await PasteTextAsync(action, context, cancellationToken);
                break;

            case ActionType.AiRequest:
                await AiRequestAsync(action, context, cancellationToken);
                break;

            case ActionType.LaunchUrl:
                await LaunchUrlAsync(action, context, cancellationToken);
                break;
        }
    }

    private static async Task LaunchUrlAsync(MacroAction action, MacroContext context, CancellationToken cancellationToken)
    {
        var url = context.Expand(action.Url).Trim();
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
            throw new InvalidOperationException($"URL 열기: http/https 주소만 열 수 있습니다: '{url}'");

        try
        {
            Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            throw new InvalidOperationException($"URL 열기 실패: {ex.Message}", ex);
        }

        // Give the browser time to come to the front, then maximize it so click coordinates are always the same.
        await Task.Delay(2000, cancellationToken);
        var hwnd = NativeMethods.GetForegroundWindow();
        if (hwnd != IntPtr.Zero)
        {
            NativeMethods.GetWindowThreadProcessId(hwnd, out var pid);
            if (pid != Environment.ProcessId)
                NativeMethods.ShowWindow(hwnd, NativeMethods.SW_MAXIMIZE);
        }
    }

    private async Task AiRequestAsync(MacroAction action, MacroContext context, CancellationToken cancellationToken)
    {
        if (action.PromptTemplateId is not { } templateId)
            throw new InvalidOperationException("AI 요청: 프롬프트 템플릿이 선택되지 않았습니다.");

        var repository = _promptRepository ?? new PromptTemplateRepository();
        var template = repository.GetById(templateId)
            ?? throw new InvalidOperationException($"AI 요청: 프롬프트 템플릿(id {templateId})을 찾을 수 없습니다.");

        var userPrompt = context.Expand(template.UserPromptTemplate);
        var result = await _aiClient.CompleteAsync(template.SystemPrompt, userPrompt, cancellationToken);

        var variable = string.IsNullOrWhiteSpace(action.TargetVariable)
            ? DefaultAiResultVariable
            : action.TargetVariable.Trim();
        context.Set(variable, result);

        var savePath = context.Expand(action.SaveResultPath).Trim();
        if (savePath.Length > 0)
        {
            var dir = Path.GetDirectoryName(savePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            await File.WriteAllTextAsync(savePath, result, cancellationToken);
        }
    }

    private async Task PasteTextAsync(MacroAction action, MacroContext context, CancellationToken cancellationToken)
    {
        var text = context.Expand(action.Text);
        if (text.Length == 0)
            return;

        ClipboardHelper.SetText(text);
        // Give the target app a moment to see the new clipboard content before Ctrl+V.
        await Task.Delay(100, cancellationToken);
        _simulator.KeyCombo(new[] { "Ctrl", "V" });
    }

    private static async Task ReadFileAsync(MacroAction action, MacroContext context, CancellationToken cancellationToken)
    {
        var path = context.Expand(action.FolderPath).Trim();
        if (path.Length == 0)
            throw new InvalidOperationException("파일 읽기: 폴더/파일 경로가 비어 있습니다.");
        if (string.IsNullOrWhiteSpace(action.TargetVariable))
            throw new InvalidOperationException("파일 읽기: 저장할 변수명이 비어 있습니다.");

        string filePath;
        if (File.Exists(path))
        {
            filePath = path;
        }
        else if (Directory.Exists(path))
        {
            var pattern = string.IsNullOrWhiteSpace(action.FilePattern) ? "*" : action.FilePattern.Trim();
            var newest = new DirectoryInfo(path)
                .EnumerateFiles(pattern, SearchOption.TopDirectoryOnly)
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .FirstOrDefault();
            filePath = newest?.FullName
                ?? throw new FileNotFoundException($"파일 읽기: '{path}' 에서 '{pattern}' 에 맞는 파일이 없습니다.");
        }
        else
        {
            throw new FileNotFoundException($"파일 읽기: 경로를 찾을 수 없습니다: {path}");
        }

        var length = new FileInfo(filePath).Length;
        if (length > MaxReadFileBytes)
            throw new InvalidOperationException(
                $"파일 읽기: 파일이 너무 큽니다 ({length / 1024}KB > {MaxReadFileBytes / 1024}KB): {filePath}");

        var text = await File.ReadAllTextAsync(filePath, cancellationToken);
        context.Set(action.TargetVariable.Trim(), text);
    }

    private static bool IsMouseAction(ActionType type) =>
        type is ActionType.MouseMove or ActionType.MouseClick or ActionType.MouseScroll;
}
