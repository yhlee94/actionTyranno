using ActionTyranno.Core.Input;
using ActionTyranno.Core.Models;

namespace ActionTyranno.Core.Execution;

/// <summary>
/// Runs a macro's action sequence via InputSimulator, repeating it Macro.RepeatCount times.
/// Runs entirely on the calling task (intended to be started via Task.Run from the UI)
/// and can be stopped mid-flight through the supplied CancellationToken.
/// </summary>
public class MacroPlayer
{
    private readonly InputSimulator _simulator = new();

    public async Task RunAsync(Macro macro, CancellationToken cancellationToken)
    {
        var repeatCount = Math.Max(1, macro.RepeatCount);

        for (var i = 0; i < repeatCount; i++)
        {
            foreach (var action in macro.Actions)
            {
                cancellationToken.ThrowIfCancellationRequested();

                Execute(action);

                if (action.DelayAfterMs > 0)
                    await Task.Delay(action.DelayAfterMs, cancellationToken);
            }
        }
    }

    private void Execute(MacroAction action)
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
        }
    }
}
