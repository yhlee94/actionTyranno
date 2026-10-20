using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace ActionTyranno.Core.Ai;

/// <summary>
/// Calls the locally installed Claude Code CLI in print mode (`claude -p`), using its existing login.
/// The prompt is sent through stdin (logs are too large for command-line arguments) and
/// all CLI tools are disabled so the model can only answer with text.
/// </summary>
public class ClaudeCliClient : IAiClient
{
    private static readonly UTF8Encoding Utf8NoBom = new(false);

    private readonly string? _executable;
    private readonly string? _model;
    private readonly TimeSpan _timeout;

    public ClaudeCliClient(string? executable = null, string? model = null, TimeSpan? timeout = null)
    {
        _executable = string.IsNullOrWhiteSpace(executable) ? null : executable;
        _model = string.IsNullOrWhiteSpace(model) ? null : model;
        _timeout = timeout ?? TimeSpan.FromMinutes(5);
    }

    public async Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken)
    {
        var systemFile = Path.Combine(Path.GetTempPath(), $"actiontyranno-system-{Guid.NewGuid():N}.txt");
        await File.WriteAllTextAsync(systemFile, systemPrompt, Utf8NoBom, cancellationToken);

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = _executable ?? ResolveFromPath(),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardInputEncoding = Utf8NoBom,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                // Drive root: keeps CLAUDE.md files of user/project folders from leaking into the request.
                WorkingDirectory = Path.GetPathRoot(Path.GetTempPath()) ?? Path.GetTempPath()
            };
            psi.ArgumentList.Add("-p");
            psi.ArgumentList.Add("--system-prompt-file");
            psi.ArgumentList.Add(systemFile);
            psi.ArgumentList.Add("--tools=");
            psi.ArgumentList.Add("--no-session-persistence");
            psi.ArgumentList.Add("--setting-sources");
            psi.ArgumentList.Add("project");
            if (_model != null)
            {
                psi.ArgumentList.Add("--model");
                psi.ArgumentList.Add(_model);
            }

            using var process = new Process { StartInfo = psi };
            try
            {
                process.Start();
            }
            catch (Win32Exception ex)
            {
                throw new InvalidOperationException(
                    $"AI 요청: claude CLI를 실행할 수 없습니다 ('{psi.FileName}'). 설치/로그인 상태를 확인하세요. ({ex.Message})", ex);
            }

            // Start draining output before writing input so a full pipe can never deadlock us.
            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(_timeout);

            try
            {
                try
                {
                    await process.StandardInput.WriteAsync(userPrompt.AsMemory(), timeoutCts.Token);
                    process.StandardInput.Close();
                }
                catch (IOException)
                {
                    // The CLI exited before reading everything; its exit code and stderr explain why below.
                }

                await process.WaitForExitAsync(timeoutCts.Token);
            }
            catch (OperationCanceledException)
            {
                Kill(process);
                if (cancellationToken.IsCancellationRequested)
                    throw;
                throw new TimeoutException($"AI 요청: {_timeout.TotalSeconds:0}초 안에 응답이 없어 중단했습니다.");
            }

            var stdout = (await stdoutTask).Trim();
            var stderr = (await stderrTask).Trim();

            if (process.ExitCode != 0)
                throw new InvalidOperationException(
                    $"AI 요청 실패 (종료 코드 {process.ExitCode}): {FirstNonEmpty(stderr, stdout)}");
            if (stdout.Length == 0)
                throw new InvalidOperationException("AI 요청: 응답이 비어 있습니다.");

            return stdout;
        }
        finally
        {
            try { File.Delete(systemFile); } catch (IOException) { }
        }
    }

    private static string FirstNonEmpty(string a, string b)
    {
        var s = a.Length > 0 ? a : b;
        return s.Length > 500 ? s[..500] + "…" : s;
    }

    private static void Kill(Process process)
    {
        try { process.Kill(entireProcessTree: true); }
        catch (InvalidOperationException) { }
    }

    /// <summary>Finds claude.exe / claude.cmd on PATH (npm global installs use the .cmd shim).</summary>
    private static string ResolveFromPath()
    {
        var dirs = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        foreach (var name in new[] { "claude.exe", "claude.cmd" })
        {
            foreach (var dir in dirs)
            {
                var candidate = Path.Combine(dir.Trim().Trim('"'), name);
                if (File.Exists(candidate))
                    return candidate;
            }
        }
        return "claude";
    }
}
