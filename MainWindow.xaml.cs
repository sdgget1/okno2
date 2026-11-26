using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace DrugieOkno;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly DispatcherTimer _clockTimer;
    private readonly ObservableCollection<TodoItem> _tasks = new();
    private readonly ICollectionView _tasksView;
    private bool _isLoadingState;

    public MainWindow()
    {
        InitializeComponent();
        _tasksView = CollectionViewSource.GetDefaultView(_tasks);
        _tasksView.Filter = FilterTasks;
        TasksListBox.ItemsSource = _tasksView;
        _tasks.CollectionChanged += Tasks_CollectionChanged;

        LoadState();
        UpdateTaskSummary();
        UpdateNotesStats();
        Closing += (_, _) => SaveState();

        _clockTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _clockTimer.Tick += (_, _) => UpdateClock();
        _clockTimer.Start();
        UpdateClock();
    }

    private void Tasks_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (TodoItem item in e.NewItems)
            {
                item.PropertyChanged += TaskItem_PropertyChanged;
            }
        }

        if (e.OldItems != null)
        {
            foreach (TodoItem item in e.OldItems)
            {
                item.PropertyChanged -= TaskItem_PropertyChanged;
            }
        }

        UpdateTaskSummary();
        SaveState();
    }

    private void TaskItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TodoItem.IsDone))
        {
            UpdateTaskSummary();
            _tasksView.Refresh();
            SaveState();
        }
    }

    private bool FilterTasks(object obj)
    {
        if (obj is not TodoItem item)
        {
            return false;
        }

        var search = SearchTextBox?.Text?.Trim() ?? string.Empty;
        if (!string.IsNullOrEmpty(search) &&
            !item.Title.Contains(search, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var filter = (FilterComboBox?.SelectedItem as ComboBoxItem)?.Content?.ToString();
        return filter switch
        {
            "Aktywne" => !item.IsDone,
            "Ukończone" => item.IsDone,
            _ => true
        };
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _tasksView?.Refresh();
    }

    private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _tasksView?.Refresh();
    }

    private void UpdateTaskSummary()
    {
        var total = _tasks.Count;
        var done = _tasks.Count(t => t.IsDone);
        var remaining = total - done;
        TasksSummaryTextBlock.Text = $"Zadania: {total} | Ukończone: {done} | Pozostałe: {remaining}";
    }

    private void UpdateClock()
    {
        ClockTextBlock.Text = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
    }

    private void AddTaskButton_Click(object sender, RoutedEventArgs e)
    {
        AddTaskFromInput();
    }

    private void TaskInputTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            AddTaskFromInput();
        }
    }

    private void AddTaskFromInput()
    {
        var title = TaskInputTextBox.Text.Trim();
        if (string.IsNullOrEmpty(title))
        {
            StatusTextBlock.Text = "Wpisz treść zadania przed dodaniem.";
            return;
        }

        var priority = (PriorityComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Normalny";
        _tasks.Add(new TodoItem { Title = title, Priority = priority });
        TaskInputTextBox.Clear();
        TaskInputTextBox.Focus();
        StatusTextBlock.Text = $"Dodano zadanie ({priority}): {title}";
    }

    private void DeleteTaskButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: TodoItem item })
        {
            _tasks.Remove(item);
            StatusTextBlock.Text = $"Usunięto zadanie: {item.Title}";
        }
    }

    private void ClearCompletedButton_Click(object sender, RoutedEventArgs e)
    {
        var completed = _tasks.Where(t => t.IsDone).ToList();
        if (completed.Count == 0)
        {
            StatusTextBlock.Text = "Brak ukończonych zadań do usunięcia.";
            return;
        }

        var result = MessageBox.Show(
            $"Czy na pewno usunąć ukończone zadania ({completed.Count})?",
            "Potwierdzenie",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        foreach (var item in completed)
        {
            _tasks.Remove(item);
        }

        StatusTextBlock.Text = $"Usunięto ukończone zadania: {completed.Count}";
    }

    private void NotesTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateNotesStats();
        SaveState();
    }

    private void LoadState()
    {
        _isLoadingState = true;
        try
        {
            var state = AppState.Load();
            foreach (var item in state.Tasks)
            {
                _tasks.Add(item);
            }

            NotesTextBox.Text = state.Notes;
        }
        finally
        {
            _isLoadingState = false;
        }
    }

    private void SaveState()
    {
        if (_isLoadingState)
        {
            return;
        }

        try
        {
            var state = new AppState
            {
                Tasks = _tasks.ToList(),
                Notes = NotesTextBox.Text ?? string.Empty
            };
            state.Save();
        }
        catch
        {
            StatusTextBlock.Text = "Nie udało się zapisać stanu aplikacji.";
        }
    }

    private void UpdateNotesStats()
    {
        var text = NotesTextBox.Text ?? string.Empty;
        var chars = text.Length;
        var words = string.IsNullOrWhiteSpace(text)
            ? 0
            : text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        NotesStatsTextBlock.Text = $"Znaki: {chars} | Słowa: {words}";
    }
}