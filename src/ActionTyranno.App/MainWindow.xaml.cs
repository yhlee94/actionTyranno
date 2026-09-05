using System.Windows;
using ActionTyranno.App.Views;

namespace ActionTyranno.App;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnPickCoordinateClick(object sender, RoutedEventArgs e)
    {
        var picked = CoordinatePickerOverlay.PickCoordinate(this);
        ResultText.Text = picked is { } p
            ? $"선택된 좌표 -> X: {p.X}, Y: {p.Y}"
            : "취소됨 (ESC)";
    }
}
