using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using ActionTyranno.Core.Input;

namespace ActionTyranno.App.Views;

/// <summary>
/// Full virtual-screen, always-on-top overlay used to pick a screen coordinate by clicking.
/// The app is per-monitor DPI aware, so every coordinate here is a physical pixel (the same space
/// InputSimulator uses). The picked point is read from the real cursor position rather than converted
/// from WPF's DPI-independent units, and the window is placed with Win32 in physical pixels so it covers
/// monitors with different scale factors exactly.
/// </summary>
public partial class CoordinatePickerOverlay : Window
{
    private readonly InputSimulator _input = new();
    private VirtualScreenBounds _bounds;
    private bool _applyingBounds;

    public (int X, int Y)? Result { get; private set; }

    public CoordinatePickerOverlay()
    {
        InitializeComponent();
        SourceInitialized += OnSourceInitialized;
        Loaded += (_, _) => Focus();
        MouseMove += OnMouseMove;
        PreviewMouseLeftButtonDown += OnMouseDown;
        PreviewKeyDown += OnKeyDown;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        _bounds = InputSimulator.GetVirtualScreenBounds();
        ApplyBounds();
    }

    // The window's own DPI can change while it is moved across monitors; WPF then resizes it, so re-apply.
    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
    {
        base.OnDpiChanged(oldDpi, newDpi);
        ApplyBounds();
    }

    private void ApplyBounds()
    {
        if (_applyingBounds)
            return;

        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero)
            return;

        _applyingBounds = true;
        try
        {
            SetWindowPos(hwnd, HWND_TOPMOST, _bounds.X, _bounds.Y, _bounds.Width, _bounds.Height, SWP_NOACTIVATE);
        }
        finally
        {
            _applyingBounds = false;
        }
    }

    /// <summary>Physical screen pixel -> position inside the window in WPF units, for drawing.</summary>
    private Point PhysicalToDip((int X, int Y) physical)
    {
        var dpi = VisualTreeHelper.GetDpi(this);
        return new Point((physical.X - _bounds.X) / dpi.DpiScaleX, (physical.Y - _bounds.Y) / dpi.DpiScaleY);
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        var physical = _input.GetCursorPosition();
        var dipPos = PhysicalToDip(physical);

        HorizontalLine.X1 = 0;
        HorizontalLine.X2 = ActualWidth;
        HorizontalLine.Y1 = HorizontalLine.Y2 = dipPos.Y;

        VerticalLine.Y1 = 0;
        VerticalLine.Y2 = ActualHeight;
        VerticalLine.X1 = VerticalLine.X2 = dipPos.X;

        CoordLabel.Text = $"X: {physical.X}  Y: {physical.Y}";

        var labelX = Math.Min(dipPos.X + 18, ActualWidth - CoordLabelBorder.ActualWidth - 4);
        var labelY = Math.Min(dipPos.Y + 18, ActualHeight - CoordLabelBorder.ActualHeight - 4);
        System.Windows.Controls.Canvas.SetLeft(CoordLabelBorder, labelX);
        System.Windows.Controls.Canvas.SetTop(CoordLabelBorder, labelY);
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        var physical = _input.GetCursorPosition();
        Result = physical;
        var dipPos = PhysicalToDip(physical);

        MouseMove -= OnMouseMove;
        PreviewMouseLeftButtonDown -= OnMouseDown;

        Marker.Visibility = Visibility.Visible;
        System.Windows.Controls.Canvas.SetLeft(Marker, dipPos.X - Marker.Width / 2);
        System.Windows.Controls.Canvas.SetTop(Marker, dipPos.Y - Marker.Height / 2);

        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            DialogResult = true;
            Close();
        };
        timer.Start();
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
            return;

        Result = null;
        DialogResult = false;
        Close();
    }

    /// <summary>
    /// Shows the overlay modally and returns the picked physical-pixel coordinate,
    /// or null if the user cancelled with ESC.
    /// </summary>
    public static (int X, int Y)? PickCoordinate(Window? owner = null)
    {
        var overlay = new CoordinatePickerOverlay();
        if (owner != null)
            overlay.Owner = owner;

        overlay.ShowDialog();
        return overlay.Result;
    }

    private static readonly IntPtr HWND_TOPMOST = new(-1);
    private const uint SWP_NOACTIVATE = 0x0010;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint flags);
}
