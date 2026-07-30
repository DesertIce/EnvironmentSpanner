using System.Collections.ObjectModel;

namespace EnvironmentSpanner.ViewModels;

public sealed class DuplicateReviewGroupViewModel
{
    public DuplicateReviewGroupViewModel(
        string comparisonKey,
        IEnumerable<ListEntryViewModel> entries)
    {
        ComparisonKey = comparisonKey;
        Occurrences = new ObservableCollection<DuplicateReviewItemViewModel>(
            entries.Select((entry, index) =>
                new DuplicateReviewItemViewModel(entry, index > 0)));
    }

    public string ComparisonKey { get; }

    public ObservableCollection<DuplicateReviewItemViewModel> Occurrences { get; }

    public int ProposedRemovalCount =>
        Occurrences.Count(item => item.IsMarkedForRemoval);
}
