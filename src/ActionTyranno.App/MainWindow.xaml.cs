using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using ActionTyranno.App.Interop;
using ActionTyranno.App.Views;
using ActionTyranno.Core.Execution;
using ActionTyranno.Core.Models;
using ActionTyranno.Core.Storage;
using Microsoft.Win32;

namespace ActionTyranno.App;

public partial class MainWindow : Window
{
    private readonly MacroRepository _repository = new();
    private readonly ObservableCollection<Macro> _macros = new();
    private readonly ObservableCollection<MacroAction> _currentActions = new();
    private readonly MacroPlayer _player = new();

    private CancellationTokenSource? _playCts;
    private GlobalHotKeyManager? _hotKeys;
    private readonly Stopwatch _playStopwatch = new();
    private readonly DispatcherTimer _elapsedTimer = new() { Interval = TimeSpan.FromMilliseconds(100) };

    private Macro? SelectedMacro => MacroListBox.SelectedItem as Macro;

    public MainWindow()
    {
        InitializeComponent();

        MacroListBox.ItemsSource = _macros;
        ActionListView.ItemsSource = _currentActions;

        foreach (var macro in _repository.GetAll())
            _macros.Add(macro);

        _elapsedTimer.Tick += (_, _) =>
            PlaybackStatusText.Text = $"실행 중... ({_playStopwatch.Elapsed.TotalSeconds:0.0}초)";
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        _hotKeys = new GlobalHotKeyManager(this);
        var f6Ok = _hotKeys.Register(ModifierKeys.None, Key.F6, () => StartPlayback());
        var f7Ok = _hotKeys.Register(ModifierKeys.None, Key.F7, RequestStop);

        if (!f6Ok || !f7Ok)
        {
            PlaybackStatusText.Text = "전역 단축키(F6/F7) 등록 실패 - 다른 프로그램과 충돌";
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _elapsedTimer.Stop();
        _hotKeys?.Dispose();
        base.OnClosed(e);
    }

    private void OnNewMacroClick(object sender, RoutedEventArgs e)
    {
        var macro = _repository.Add("새 매크로");
        _macros.Add(macro);
        MacroListBox.SelectedItem = macro;
    }

    private void OnDeleteMacroClick(object sender, RoutedEventArgs e)
    {
        if (SelectedMacro is not { } macro)
            return;

        var confirm = MessageBox.Show(this, $"'{macro.Name}' 매크로를 삭제할까요?", "매크로 삭제",
            MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes)
            return;

        _repository.Delete(macro.Id);
        _macros.Remove(macro);
    }

    private void OnExportMacroClick(object sender, RoutedEventArgs e)
    {
        if (SelectedMacro is not { } macro)
            return;

        var dialog = new SaveFileDialog
        {
            Title = "매크로 내보내기",
            FileName = SanitizeFileName(macro.Name) + ".json",
            Filter = "JSON 파일 (*.json)|*.json"
        };

        if (dialog.ShowDialog(this) != true)
            return;

        try
        {
            File.WriteAllText(dialog.FileName, MacroRepository.SerializeMacro(macro));
            MessageBox.Show(this, $"'{macro.Name}' 매크로를 내보냈습니다.", "내보내기 완료",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"내보내기에 실패했습니다: {ex.Message}", "오류",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnImportMacroClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "매크로 가져오기",
            Filter = "JSON 파일 (*.json)|*.json"
        };

        if (dialog.ShowDialog(this) != true)
            return;

        List<Macro> imported;
        try
        {
            var json = File.ReadAllText(dialog.FileName);
            imported = MacroRepository.ParseSharedMacros(json);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"가져오기에 실패했습니다: {ex.Message}", "오류",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        Macro? lastImported = null;
        foreach (var source in imported)
            lastImported = _repository.Import(source);

        if (lastImported == null)
            return;

        foreach (var macro in _repository.GetAll())
        {
            if (_macros.All(m => m.Id != macro.Id))
                _macros.Add(macro);
        }

        MacroListBox.SelectedItem = _macros.FirstOrDefault(m => m.Id == lastImported.Id);
        MessageBox.Show(this, $"매크로 {imported.Count}개를 가져왔습니다.", "가져오기 완료",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray()).Trim();
        return sanitized.Length == 0 ? "macro" : sanitized;
    }

    private void OnMacroSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        _currentActions.Clear();

        if (SelectedMacro is { } macro)
        {
            MacroNameTextBox.Text = macro.Name;
            RepeatCountTextBox.Text = macro.RepeatCount.ToString();
            PlaybackStatusText.Text = string.Empty;
            foreach (var action in macro.Actions)
                _currentActions.Add(action);
            EditorPanel.IsEnabled = true;
        }
        else
        {
            MacroNameTextBox.Text = string.Empty;
            RepeatCountTextBox.Text = string.Empty;
            EditorPanel.IsEnabled = false;
        }
    }

    private void OnMacroNameLostFocus(object sender, RoutedEventArgs e)
    {
        if (SelectedMacro is not { } macro)
            return;

        var newName = MacroNameTextBox.Text.Trim();
        if (newName.Length == 0 || newName == macro.Name)
            return;

        macro.Name = newName;
        _repository.Update(macro);
        MacroListBox.Items.Refresh();
    }

    private void OnRepeatCountLostFocus(object sender, RoutedEventArgs e)
    {
        if (SelectedMacro is not { } macro)
            return;

        if (!int.TryParse(RepeatCountTextBox.Text, out var count) || count < 1)
            count = 1;

        RepeatCountTextBox.Text = count.ToString();
        if (count == macro.RepeatCount)
            return;

        macro.RepeatCount = count;
        _repository.Update(macro);
    }

    private void OnAddActionClick(object sender, RoutedEventArgs e)
    {
        if (SelectedMacro is not { } macro)
            return;

        var dialog = new ActionEditWindow { Owner = this };
        if (dialog.ShowDialog() == true && dialog.Result is { } newAction)
        {
            _currentActions.Add(newAction);
            PersistCurrentActions(macro);
        }
    }

    private void OnEditActionClick(object sender, RoutedEventArgs e)
    {
        if (SelectedMacro is not { } macro)
            return;

        var index = ActionListView.SelectedIndex;
        if (index < 0)
            return;

        var dialog = new ActionEditWindow(_currentActions[index]) { Owner = this };
        if (dialog.ShowDialog() == true && dialog.Result is { } edited)
        {
            _currentActions[index] = edited;
            PersistCurrentActions(macro);
        }
    }

    private void OnDeleteActionClick(object sender, RoutedEventArgs e)
    {
        if (SelectedMacro is not { } macro)
            return;

        var index = ActionListView.SelectedIndex;
        if (index < 0)
            return;

        _currentActions.RemoveAt(index);
        PersistCurrentActions(macro);
    }

    private void OnMoveUpClick(object sender, RoutedEventArgs e)
    {
        if (SelectedMacro is not { } macro)
            return;

        var index = ActionListView.SelectedIndex;
        if (index <= 0)
            return;

        _currentActions.Move(index, index - 1);
        PersistCurrentActions(macro);
        ActionListView.SelectedIndex = index - 1;
    }

    private void OnMoveDownClick(object sender, RoutedEventArgs e)
    {
        if (SelectedMacro is not { } macro)
            return;

        var index = ActionListView.SelectedIndex;
        if (index < 0 || index >= _currentActions.Count - 1)
            return;

        _currentActions.Move(index, index + 1);
        PersistCurrentActions(macro);
        ActionListView.SelectedIndex = index + 1;
    }

    private void OnPlayClick(object sender, RoutedEventArgs e) => StartPlayback();

    private void OnStopClick(object sender, RoutedEventArgs e) => RequestStop();

    private async void StartPlayback()
    {
        if (_playCts != null)
            return; // already running (e.g. F6 pressed twice)

        if (SelectedMacro is not { } macro)
            return;

        if (macro.Actions.Count == 0)
        {
            MessageBox.Show(this, "실행할 액션이 없습니다.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // Read straight from the textbox rather than relying on its LostFocus handler having
        // already committed the value - clicking Play right after typing must not race that.
        if (int.TryParse(RepeatCountTextBox.Text, out var repeatCount) && repeatCount >= 1
            && repeatCount != macro.RepeatCount)
        {
            macro.RepeatCount = repeatCount;
            _repository.Update(macro);
        }

        SetPlaybackState(isPlaying: true);
        PlaybackStatusText.Text = "실행 중... (0.0초)";
        _playStopwatch.Restart();
        _elapsedTimer.Start();

        _playCts = new CancellationTokenSource();
        try
        {
            await _player.RunAsync(macro, _playCts.Token);
            _elapsedTimer.Stop();
            PlaybackStatusText.Text = $"완료 (총 {_playStopwatch.Elapsed.TotalSeconds:0.0}초)";
        }
        catch (OperationCanceledException)
        {
            _elapsedTimer.Stop();
            PlaybackStatusText.Text = $"정지됨 ({_playStopwatch.Elapsed.TotalSeconds:0.0}초)";
        }
        finally
        {
            _playStopwatch.Stop();
            _playCts.Dispose();
            _playCts = null;
            SetPlaybackState(isPlaying: false);
        }
    }

    private void RequestStop()
    {
        _playCts?.Cancel();
    }

    private void SetPlaybackState(bool isPlaying)
    {
        PlayButton.IsEnabled = !isPlaying;
        StopButton.IsEnabled = isPlaying;
        RepeatCountTextBox.IsEnabled = !isPlaying;
        MacroListPanel.IsEnabled = !isPlaying;
        NameRow.IsEnabled = !isPlaying;
        ActionButtonsPanel.IsEnabled = !isPlaying;
    }

    private void PersistCurrentActions(Macro macro)
    {
        macro.Actions = _currentActions.ToList();
        _repository.Update(macro);
    }
}
