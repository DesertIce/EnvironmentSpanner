using CommunityToolkit.Mvvm.ComponentModel;

namespace EnvironmentSpanner.ViewModels;

public sealed partial class ListEntryViewModel : ObservableObject
{
    public ListEntryViewModel(int id, int position, string value)
    {
        Id = id;
        this.position = position;
        this.value = value;
    }

    public int Id { get; }

    [ObservableProperty]
    private int position;

    public int DisplayPosition => Position + 1;

    partial void OnPositionChanged(int value) =>
        OnPropertyChanged(nameof(DisplayPosition));

    [ObservableProperty]
    private string value;

    [ObservableProperty]
    private bool isDuplicate;
}
