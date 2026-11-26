using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;

namespace DrugieOkno;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly DispatcherTimer _clockTimer;
    private readonly DispatcherTimer _pomodoroTimer;
    private readonly ObservableCollection<TodoItem> _tasks = new();
    private readonly ICollectionView _tasksView;
    private bool _isLoadingState;
    private bool _isDarkTheme;
    private TimeSpan _pomodoroRemaining = TimeSpan.FromMinutes(25);
    private bool _isPomodoroRunning;

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

        _pomodoroTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _pomodoroTimer.Tick += PomodoroTimer_Tick;
        UpdatePomodoroDisplay();
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

    private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
    {
        _isDarkTheme = !_isDarkTheme;
        ApplyTheme(_isDarkTheme);
        StatusTextBlock.Text = _isDarkTheme ? "Włączono motyw ciemny." : "Włączono motyw jasny.";
    }

    private void ApplyTheme(bool isDark)
    {
        var windowBg = BrushFromHex(isDark ? "#0F172A" : "#F4F6F9");
        var cardBg = BrushFromHex(isDark ? "#1E293B" : "#FFFFFF");
        var borderBrush = BrushFromHex(isDark ? "#334155" : "#CBD5E1");
        var inputBg = BrushFromHex(isDark ? "#0F172A" : "#FFFFFF");
        var primaryText = BrushFromHex(isDark ? "#F1F5F9" : "#0F172A");
        var secondaryText = BrushFromHex(isDark ? "#94A3B8" : "#475569");
        var statusBg = BrushFromHex(isDark ? "#1E293B" : "#E2E8F0");
        var statusFg = BrushFromHex(isDark ? "#CBD5E1" : "#334155");

        RootGrid.Background = windowBg;
        TasksCard.Background = cardBg;
        TasksCard.BorderBrush = borderBrush;
        NotesCard.Background = cardBg;
        NotesCard.BorderBrush = borderBrush;
        PomodoroCard.Background = cardBg;
        PomodoroCard.BorderBrush = borderBrush;

        TasksTitleTextBlock.Foreground = primaryText;
        NotesTitleTextBlock.Foreground = primaryText;
        PomodoroTitleTextBlock.Foreground = primaryText;
        SearchLabelTextBlock.Foreground = secondaryText;
        NotesStatsTextBlock.Foreground = secondaryText;

        TaskInputTextBox.Background = inputBg;
        TaskInputTextBox.Foreground = primaryText;
        TaskInputTextBox.BorderBrush = borderBrush;

        SearchTextBox.Background = inputBg;
        SearchTextBox.Foreground = primaryText;
        SearchTextBox.BorderBrush = borderBrush;

        TasksListBox.Background = inputBg;
        TasksListBox.Foreground = primaryText;
        TasksListBox.BorderBrush = borderBrush;

        NotesTextBox.Background = inputBg;
        NotesTextBox.Foreground = primaryText;
        NotesTextBox.BorderBrush = borderBrush;

        StatusBorder.Background = statusBg;
        StatusBorder.BorderBrush = borderBrush;
        StatusTextBlock.Foreground = statusFg;
        TasksSummaryTextBlock.Foreground = statusFg;
        ClockTextBlock.Foreground = statusFg;

        ThemeToggleButton.Content = isDark ? "Motyw jasny" : "Motyw ciemny";
    }

    private static SolidColorBrush BrushFromHex(string hex) =>
        new((Color)ColorConverter.ConvertFromString(hex));

    private void PomodoroToggleButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isPomodoroRunning)
        {
            _pomodoroTimer.Stop();
            _isPomodoroRunning = false;
            PomodoroToggleButton.Content = "Wznów";
            StatusTextBlock.Text = "Wstrzymano minutnik skupienia.";
        }
        else
        {
            _pomodoroTimer.Start();
            _isPomodoroRunning = true;
            PomodoroToggleButton.Content = "Pauza";
            StatusTextBlock.Text = "Uruchomiono minutnik skupienia (25 min).";
        }
    }

    private void PomodoroResetButton_Click(object sender, RoutedEventArgs e)
    {
        _pomodoroTimer.Stop();
        _isPomodoroRunning = false;
        _pomodoroRemaining = TimeSpan.FromMinutes(25);
        PomodoroToggleButton.Content = "Start";
        UpdatePomodoroDisplay();
        StatusTextBlock.Text = "Zresetowano minutnik skupienia.";
    }

    private void PomodoroTimer_Tick(object? sender, EventArgs e)
    {
        if (_pomodoroRemaining <= TimeSpan.FromSeconds(1))
        {
            _pomodoroTimer.Stop();
            _isPomodoroRunning = false;
            _pomodoroRemaining = TimeSpan.Zero;
            PomodoroToggleButton.Content = "Start";
            UpdatePomodoroDisplay();
            StatusTextBlock.Text = "Sesja skupienia zakończona!";
            MessageBox.Show("Czas skupienia minął! Zrób krótką przerwę.", "Minutnik skupienia", MessageBoxButton.OK, MessageBoxImage.Information);
            _pomodoroRemaining = TimeSpan.FromMinutes(25);
            UpdatePomodoroDisplay();
            return;
        }

        _pomodoroRemaining -= TimeSpan.FromSeconds(1);
        UpdatePomodoroDisplay();
    }

    private void UpdatePomodoroDisplay()
    {
        PomodoroTimeTextBlock.Text = _pomodoroRemaining.ToString(@"mm\:ss");
    }

    private void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        ExportToMarkdown();
    }

    private void ExportToMarkdown()
    {
        var dialog = new SaveFileDialog
        {
            Title = "Eksportuj zadania i notatki",
            Filter = "Pliki Markdown (*.md)|*.md|Pliki tekstowe (*.txt)|*.txt",
            FileName = $"drugie-okno-{DateTime.Now:yyyy-MM-dd}.md"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"# Raport — Drugie Okno ({DateTime.Now:dd.MM.yyyy HH:mm})");
        sb.AppendLine();
        sb.AppendLine("## Lista zadań");
        if (_tasks.Count == 0)
        {
            sb.AppendLine("_Brak zadań._");
        }
        else
        {
            foreach (var task in _tasks)
            {
                var check = task.IsDone ? "x" : " ";
                sb.AppendLine($"- [{check}] **{task.Title}** *(Priorytet: {task.Priority})*");
            }
        }

        sb.AppendLine();
        sb.AppendLine("## Szybkie notatki");
        var notes = NotesTextBox.Text?.Trim();
        sb.AppendLine(string.IsNullOrEmpty(notes) ? "_Brak notatek._" : notes);

        File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
        StatusTextBlock.Text = $"Wyeksportowano raport do: {Path.GetFileName(dialog.FileName)}";
    }
}