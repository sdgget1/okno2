using System.Collections.ObjectModel;
using System.Windows;
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

        _clockTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _clockTimer.Tick += (_, _) => UpdateClock();
        _clockTimer.Start();
        UpdateClock();
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

        _tasks.Add(new TodoItem { Title = title });
        TaskInputTextBox.Clear();
        TaskInputTextBox.Focus();
        StatusTextBlock.Text = $"Dodano zadanie: {title}";
    }
}