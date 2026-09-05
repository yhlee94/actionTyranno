using System.Globalization;

namespace ActionTyranno.App.Formatting;

/// <summary>
/// UI-facing conversion between the model's millisecond delay and the seconds the user types
/// (e.g. "1", "2.5"). Kept separate from the model so Core stays millisecond-precise internally.
/// </summary>
public static class DelaySecondsFormat
{
    public static string ToDisplayString(int delayMs) =>
        (delayMs / 1000.0).ToString("0.###", CultureInfo.InvariantCulture);

    public static bool TryParseToMs(string text, out int delayMs)
    {
        if (double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds) && seconds >= 0)
        {
            delayMs = (int)Math.Round(seconds * 1000);
            return true;
        }

        delayMs = 0;
        return false;
    }
}
