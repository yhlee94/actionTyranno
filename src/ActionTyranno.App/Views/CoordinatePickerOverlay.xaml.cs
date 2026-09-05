using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ActionTyranno.Core.Input;

namespace ActionTyranno.App.Views;

/// <summary>
/// Full virtual-screen, always-on-top overlay used to pick a screen coordinate by clicking.
/// Coordinates are reported in physical pixels (the same coordinate space InputSimulator uses),
/// converted from WPF's DPI-independent units via the window's own device transform.
/// </summary>
public partial class CoordinatePickerOverlay : Window
{
    private Matrix _toDip;
    private Matrix _toDevice;
    private VirtualScreenBounds _bounds;

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
        var source = PresentationSource.FromVisual(this)
            ?? throw new InvalidOperationException("PresentationSource not available.");

        _toDip = source.CompositionTarget.TransformFromDevice;
        _toDevice = source.CompositionTarget.TransformToDevice;

        _bounds = InputSimulator.GetVirtualScreenBounds();

        // Position/size the window so it exactly covers the physical virtual-screen bounds,
        // regardless of the DPI scale of the monitor the window happens to be created on.
        Left = _bounds.X * _toDip.M11;
        Top = _bounds.Y * _toDip.M22;
        Width = _bounds.Width * _toDip.M11;
        Height = _bounds.Height * _toDip.M22;
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        var dipPos = e.GetPosition(RootCanvas);

        HorizontalLine.X1 = 0;
        HorizontalLine.X2 = ActualWidth;
        HorizontalLine.Y1 = HorizontalLine.Y2 = dipPos.Y;

        VerticalLine.Y1 = 0;
        VerticalLine.Y2 = ActualHeight;
        VerticalLine.X1 = VerticalLine.X2 = dipPos.X;

        var physical = DipToPhysical(dipPos);
        CoordLabel.Text = $"X: {physical.X}  Y: {physical.Y}";

        var labelX = Math.Min(dipPos.X + 18, ActualWidth - CoordLabelBorder.ActualWidth - 4);
        var labelY = Math.Min(dipPos.Y + 18, ActualHeight - CoordLabelBorder.ActualHeight - 4);
        System.Windows.Controls.Canvas.SetLeft(CoordLabelBorder, labelX);
        System.Windows.Controls.Canvas.SetTop(CoordLabelBorder, labelY);
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        var dipPos = e.GetPosition(RootCanvas);
        Result = DipToPhysical(dipPos);

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

    private (int X, int Y) DipToPhysical(Point dipPosInWindow)
    {
        var device = _toDevice.Transform(dipPosInWindow);
        return (_bounds.X + (int)Math.Round(device.X), _bounds.Y + (int)Math.Round(device.Y));
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
}
