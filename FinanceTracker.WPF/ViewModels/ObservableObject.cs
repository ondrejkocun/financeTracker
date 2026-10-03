using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Semestralka.WPF.ViewModels;

/// <summary>
/// Base class for view models that notifies the UI about property changes.
/// </summary>
public class ObservableObject : INotifyPropertyChanged
{
    /// <summary>
    /// Raised when a property value changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Raises <see cref="PropertyChanged"/> for the specified property.
    /// </summary>
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// Sets the backing field and raises property changed only when the value is different.
    /// </summary>
    protected void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;
            OnPropertyChanged(name);
        }
    }
}
