namespace ActionTyranno.Core.Input;

/// <summary>
/// Bounding box of the full virtual screen spanning all monitors.
/// X/Y can be negative when a monitor is placed left of or above the primary monitor.
/// </summary>
public readonly record struct VirtualScreenBounds(int X, int Y, int Width, int Height);
