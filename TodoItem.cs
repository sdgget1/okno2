using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DrugieOkno;

/// <summary>
/// Reprezentuje pojedyncze zadanie na liście użytkownika.
/// </summary>
public class TodoItem : INotifyPropertyChanged
{
    private string _title = string.Empty;
    private bool _isDone;
    private string _priority = "Normalny";

    public string Title
    {
        get => _title;
        set
        {
            if (_title != value)
            {
                _title = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsDone
    {
        get => _isDone;
        set
        {
            if (_isDone != value)
            {
                _isDone = value;
                OnPropertyChanged();
            }
        }
    }

    public string Priority
    {
        get => _priority;
        set
        {
            if (_priority != value)
            {
                _priority = value;
                OnPropertyChanged();
            }
        }
    }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
