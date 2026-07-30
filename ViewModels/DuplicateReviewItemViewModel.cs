using CommunityToolkit.Mvvm.ComponentModel;

namespace EnvironmentSpanner.ViewModels;

public sealed partial class DuplicateReviewItemViewModel : ObservableObject
{
    public DuplicateReviewItemViewModel(
        ListEntryViewModel entry,
        bool isMarkedForRemoval)
    {
        Entry = entry;
        this.isMarkedForRemoval = isMarkedForRemoval;
    }

    public ListEntryViewModel Entry { get; }

    [ObservableProperty]
    private bool isMarkedForRemoval;
}
