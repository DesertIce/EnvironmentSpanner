using CommunityToolkit.Mvvm.ComponentModel;

namespace EnvironmentSpanner.Models;

public partial class EnvironmentVariable : ObservableObject
{
    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string value = string.Empty;

    [ObservableProperty]
    private EnvironmentVariableTarget target;

    public bool IsListValue => !string.IsNullOrEmpty(Value) && Value.Contains(';', StringComparison.Ordinal);

    partial void OnValueChanged(string value) => OnPropertyChanged(nameof(IsListValue));
}
