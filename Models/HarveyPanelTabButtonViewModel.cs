using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HarveyOverhaul.Core.Models;

public sealed class HarveyPanelTabButtonViewModel : INotifyPropertyChanged
{
    private bool _active;

    public string Key { get; set; } = "";

    public string Label { get; set; } = "";

    public bool Active
    {
        get => _active;
        set
        {
            if (SetField(ref _active, value))
            {
                OnPropertyChanged(nameof(TabTextColor));
            }
        }
    }

    public string TabTextColor => Active ? "#3b2a1a" : "#6b5340";

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value))
            return false;

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }

    private void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
