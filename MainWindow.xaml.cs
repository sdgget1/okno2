using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
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

    public MainWindow()
    {
        InitializeComponent();
        TasksListBox.ItemsSource = _tasks;
        _tasks.CollectionChanged += Tasks_CollectionChanged;
        UpdateTaskSummary();

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
    }

    private void TaskItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TodoItem.IsDone))
        {
            UpdateTaskSummary();
        }
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
}