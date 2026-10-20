using System.Windows;
using System.Windows.Controls;
using ActionTyranno.Core.Models;
using ActionTyranno.Core.Storage;

namespace ActionTyranno.App.Views;

public partial class PromptEditorWindow : Window
{
    private readonly PromptTemplateRepository _repository = new();
    private PromptTemplate? _current;
    private bool _loading;

    public PromptEditorWindow()
    {
        InitializeComponent();
        Reload(selectId: null);
    }

    private void Reload(int? selectId)
    {
        var all = _repository.GetAll();
        PromptListBox.ItemsSource = all;
        PromptListBox.SelectedItem = all.FirstOrDefault(t => t.Id == selectId) ?? all.FirstOrDefault();
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _current = PromptListBox.SelectedItem as PromptTemplate;
        EditorGrid.IsEnabled = _current != null;
        DeleteButton.IsEnabled = _current != null;

        _loading = true;
        NameTextBox.Text = _current?.Name ?? string.Empty;
        SystemPromptTextBox.Text = _current?.SystemPrompt ?? string.Empty;
        UserPromptTextBox.Text = _current?.UserPromptTemplate ?? string.Empty;
        _loading = false;
        StatusText.Text = string.Empty;
    }

    private void OnNewClick(object sender, RoutedEventArgs e)
    {
        var created = _repository.Add("새 프롬프트", string.Empty, "{LogText}");
        Reload(created.Id);
        NameTextBox.Focus();
        NameTextBox.SelectAll();
    }

    private void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        if (_current == null)
            return;

        var answer = MessageBox.Show(this, $"'{_current.Name}' 프롬프트를 삭제할까요?\n이 프롬프트를 쓰는 AI 요청 액션은 실행 시 오류가 납니다.",
            "삭제 확인", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (answer != MessageBoxResult.Yes)
            return;

        _repository.Delete(_current.Id);
        Reload(selectId: null);
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        if (_current == null || _loading)
            return;

        var name = NameTextBox.Text.Trim();
        if (name.Length == 0)
        {
            StatusText.Text = "이름을 입력하세요.";
            return;
        }

        _current.Name = name;
        _current.SystemPrompt = SystemPromptTextBox.Text;
        _current.UserPromptTemplate = UserPromptTextBox.Text;
        _repository.Update(_current);

        var id = _current.Id;
        Reload(id);
        StatusText.Text = "저장했습니다.";
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}
