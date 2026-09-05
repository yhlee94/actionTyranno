using System.Windows;
using ActionTyranno.App.Formatting;
using ActionTyranno.Core.Input;
using ActionTyranno.Core.Models;

namespace ActionTyranno.App.Views;

public partial class ActionEditWindow : Window
{
    public MacroAction? Result { get; private set; }

    public ActionEditWindow(MacroAction? existing = null)
    {
        InitializeComponent();

        ActionTypeComboBox.ItemsSource = Enum.GetValues<ActionType>();
        MouseButtonComboBox.ItemsSource = Enum.GetValues<Core.Models.MouseButton>();

        if (existing != null)
        {
            ActionTypeComboBox.SelectedItem = existing.Type;
            if (existing.X.HasValue) XTextBox.Text = existing.X.Value.ToString();
            if (existing.Y.HasValue) YTextBox.Text = existing.Y.Value.ToString();
            MouseButtonComboBox.SelectedItem = existing.Button ?? Core.Models.MouseButton.Left;
            DoubleClickCheckBox.IsChecked = existing.DoubleClick;
            KeyTextBox.Text = existing.Key ?? string.Empty;
            KeysTextBox.Text = existing.Keys != null ? string.Join(",", existing.Keys) : string.Empty;
            DelayTextBox.Text = DelaySecondsFormat.ToDisplayString(existing.DelayAfterMs);
        }
        else
        {
            ActionTypeComboBox.SelectedItem = ActionType.MouseMove;
            MouseButtonComboBox.SelectedItem = Core.Models.MouseButton.Left;
            DelayTextBox.Text = "0";
        }

        UpdatePanelVisibility();
    }

    private ActionType SelectedType => (ActionType)ActionTypeComboBox.SelectedItem;

    private void OnActionTypeChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        UpdatePanelVisibility();
    }

    private void UpdatePanelVisibility()
    {
        if (ActionTypeComboBox.SelectedItem == null)
            return;

        var type = SelectedType;
        CoordinatePanel.Visibility = type is ActionType.MouseMove or ActionType.MouseClick
            ? Visibility.Visible : Visibility.Collapsed;
        MouseButtonPanel.Visibility = type == ActionType.MouseClick
            ? Visibility.Visible : Visibility.Collapsed;
        KeyPanel.Visibility = type == ActionType.KeyPress
            ? Visibility.Visible : Visibility.Collapsed;
        KeyComboPanel.Visibility = type == ActionType.KeyCombo
            ? Visibility.Visible : Visibility.Collapsed;
        DelayPanel.Visibility = type == ActionType.Delay
            ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnPickCoordinateClick(object sender, RoutedEventArgs e)
    {
        var picked = CoordinatePickerOverlay.PickCoordinate(this);
        if (picked is { } p)
        {
            XTextBox.Text = p.X.ToString();
            YTextBox.Text = p.Y.ToString();
        }
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        ErrorText.Text = string.Empty;

        var type = SelectedType;
        var action = new MacroAction { Type = type };

        switch (type)
        {
            case ActionType.MouseMove:
            case ActionType.MouseClick:
                if (!int.TryParse(XTextBox.Text, out var x) || !int.TryParse(YTextBox.Text, out var y))
                {
                    ErrorText.Text = "'위치 지정' 버튼으로 좌표를 먼저 선택하세요.";
                    return;
                }
                action.X = x;
                action.Y = y;
                if (type == ActionType.MouseClick)
                {
                    action.Button = (Core.Models.MouseButton)MouseButtonComboBox.SelectedItem;
                    action.DoubleClick = DoubleClickCheckBox.IsChecked == true;
                }
                break;

            case ActionType.KeyPress:
                var key = KeyTextBox.Text.Trim();
                if (key.Length == 0 || !KeyNameResolver.TryResolve(key, out _))
                {
                    ErrorText.Text = $"알 수 없는 키 이름입니다: '{key}'";
                    return;
                }
                action.Key = key;
                break;

            case ActionType.KeyCombo:
                var keys = KeysTextBox.Text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .ToList();
                if (keys.Count == 0)
                {
                    ErrorText.Text = "키 조합을 하나 이상 입력하세요 (쉼표로 구분).";
                    return;
                }
                var unknown = keys.FirstOrDefault(k => !KeyNameResolver.TryResolve(k, out _));
                if (unknown != null)
                {
                    ErrorText.Text = $"알 수 없는 키 이름입니다: '{unknown}'";
                    return;
                }
                action.Keys = keys;
                break;

            case ActionType.Delay:
                if (!DelaySecondsFormat.TryParseToMs(DelayTextBox.Text, out var delay))
                {
                    ErrorText.Text = "대기 시간(초)은 0 이상의 숫자여야 합니다. (예: 1, 2.5, 60)";
                    return;
                }
                action.DelayAfterMs = delay;
                break;
        }

        Result = action;
        DialogResult = true;
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        Result = null;
        DialogResult = false;
        Close();
    }
}
