using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace EnvironmentSpanner.ViewModels;

public partial class ListEditorViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<ListEntryViewModel> items = [];

    [ObservableProperty]
    private string variableName = string.Empty;

    [ObservableProperty]
    private ListEntryViewModel? selectedItem;

    private string _originalValue = string.Empty;
    private int _nextEntryId;

    public ICommand OkCommand { get; set; } = null!;
    public ICommand CancelCommand { get; set; } = null!;

    public ObservableCollection<DuplicateReviewGroupViewModel> DuplicateGroups { get; } = [];

    public bool HasDuplicates => DuplicateGroups.Count > 0;
    public int DuplicateGroupCount => DuplicateGroups.Count;
    public int DuplicateEntryCount =>
        DuplicateGroups.Sum(group => group.Occurrences.Count - 1);
    public string DuplicateSummary =>
        $"{DuplicateEntryCount} duplicate " +
        $"{(DuplicateEntryCount == 1 ? "entry" : "entries")} found in {DuplicateGroupCount} " +
        $"{(DuplicateGroupCount == 1 ? "group" : "groups")}.";

    [ObservableProperty]
    private bool isReviewingDuplicates;

    [ObservableProperty]
    private int currentDuplicateGroupIndex = -1;

    [ObservableProperty]
    private DuplicateReviewGroupViewModel? currentDuplicateGroup;

    [ObservableProperty]
    private string reviewConstraintMessage = string.Empty;

    public string ReviewProgressText =>
        CurrentDuplicateGroup is null
            ? string.Empty
            : $"Group {CurrentDuplicateGroupIndex + 1} of {DuplicateGroupCount}";

    public int PendingRemovalCount =>
        DuplicateGroups.Sum(group => group.ProposedRemovalCount);

    public string CleanupPreviewText
    {
        get
        {
            var affectedGroupCount =
                DuplicateGroups.Count(group => group.ProposedRemovalCount > 0);
            return $"Remove {PendingRemovalCount} " +
                   $"{(PendingRemovalCount == 1 ? "entry" : "entries")} from " +
                   $"{affectedGroupCount} " +
                   $"{(affectedGroupCount == 1 ? "group" : "groups")}. " +
                   $"The list will contain {Items.Count - PendingRemovalCount} entries " +
                   $"instead of {Items.Count}.";
        }
    }

    public void Initialize(string name, string value)
    {
        VariableName = name;
        _originalValue = value;
        Items.Clear();
        _nextEntryId = 0;

        if (!string.IsNullOrEmpty(value))
        {
            var parts = value.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                Items.Add(new ListEntryViewModel(_nextEntryId++, Items.Count, part.Trim()));
            }
        }
        
        SelectedItem = Items.FirstOrDefault();
        AnalyzeDuplicates();
        RemoveItemCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void AddItem()
    {
        if (IsReviewingDuplicates)
        {
            CancelDuplicateReview();
        }

        Items.Add(new ListEntryViewModel(_nextEntryId++, Items.Count, "New Item"));
        SelectedItem = Items.LastOrDefault();
        AnalyzeDuplicates();
        RemoveItemCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanRemoveItem))]
    private void RemoveItem()
    {
        if (IsReviewingDuplicates)
        {
            CancelDuplicateReview();
        }

        if (SelectedItem is not null)
        {
            var index = Items.IndexOf(SelectedItem);
            if (index < 0)
            {
                return;
            }

            Items.RemoveAt(index);
            RefreshPositions();
            AnalyzeDuplicates();

            // Select the next item, or previous if at the end, or null if list is empty
            if (Items.Count > 0)
            {
                SelectedItem = Items[Math.Min(index, Items.Count - 1)];
            }
            else
            {
                SelectedItem = null;
                EditText = string.Empty;
            }
        }
    }

    private bool CanRemoveItem() => SelectedItem != null && Items.Count > 0;

    [ObservableProperty]
    private string? editText = string.Empty;

    partial void OnSelectedItemChanged(ListEntryViewModel? value)
    {
        EditText = value?.Value ?? string.Empty;
        RemoveItemCommand.NotifyCanExecuteChanged();
        UpdateItemCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanUpdateItem))]
    private void UpdateItem()
    {
        if (IsReviewingDuplicates)
        {
            CancelDuplicateReview();
        }

        if (SelectedItem is not null && !string.IsNullOrWhiteSpace(EditText))
        {
            SelectedItem.Value = EditText;
            EditText = SelectedItem.Value;
            AnalyzeDuplicates();
        }
    }

    private bool CanUpdateItem() => SelectedItem != null && !string.IsNullOrWhiteSpace(EditText);

    [RelayCommand(CanExecute = nameof(CanReviewDuplicates))]
    private void ReviewDuplicates()
    {
        IsReviewingDuplicates = true;
        CurrentDuplicateGroupIndex = 0;
        CurrentDuplicateGroup = DuplicateGroups[0];
        ReviewConstraintMessage = string.Empty;
        NotifyReviewStateChanged();
    }

    private bool CanReviewDuplicates() => HasDuplicates && !IsReviewingDuplicates;

    [RelayCommand(CanExecute = nameof(CanGoToPreviousDuplicateGroup))]
    private void PreviousDuplicateGroup()
    {
        CurrentDuplicateGroupIndex--;
        CurrentDuplicateGroup = DuplicateGroups[CurrentDuplicateGroupIndex];
        ReviewConstraintMessage = string.Empty;
        NotifyReviewStateChanged();
    }

    private bool CanGoToPreviousDuplicateGroup() =>
        IsReviewingDuplicates && CurrentDuplicateGroupIndex > 0;

    [RelayCommand(CanExecute = nameof(CanGoToNextDuplicateGroup))]
    private void NextDuplicateGroup()
    {
        CurrentDuplicateGroupIndex++;
        CurrentDuplicateGroup = DuplicateGroups[CurrentDuplicateGroupIndex];
        ReviewConstraintMessage = string.Empty;
        NotifyReviewStateChanged();
    }

    private bool CanGoToNextDuplicateGroup() =>
        IsReviewingDuplicates &&
        CurrentDuplicateGroupIndex < DuplicateGroupCount - 1;

    [RelayCommand]
    private void ToggleReviewRemoval(DuplicateReviewItemViewModel item)
    {
        if (!item.IsMarkedForRemoval &&
            CurrentDuplicateGroup is not null &&
            CurrentDuplicateGroup.Occurrences.Count(
                occurrence => !occurrence.IsMarkedForRemoval) == 1)
        {
            ReviewConstraintMessage = "At least one occurrence must be kept.";
            return;
        }

        item.IsMarkedForRemoval = !item.IsMarkedForRemoval;
        ReviewConstraintMessage = string.Empty;
        NotifyReviewStateChanged();
    }

    [RelayCommand]
    private void KeepAllCurrentGroup()
    {
        if (CurrentDuplicateGroup is null)
        {
            return;
        }

        foreach (var item in CurrentDuplicateGroup.Occurrences)
        {
            item.IsMarkedForRemoval = false;
        }

        ReviewConstraintMessage = string.Empty;
        NotifyReviewStateChanged();
    }

    [RelayCommand]
    private void CancelDuplicateReview()
    {
        IsReviewingDuplicates = false;
        CurrentDuplicateGroupIndex = -1;
        CurrentDuplicateGroup = null;
        ReviewConstraintMessage = string.Empty;
        AnalyzeDuplicates();
        NotifyReviewStateChanged();
    }

    public void Cancel() => Initialize(VariableName, _originalValue);

    public string GetResultValue() =>
        string.Join(";", Items.Select(item => item.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value)));

    private void RefreshPositions()
    {
        for (var index = 0; index < Items.Count; index++)
        {
            Items[index].Position = index;
        }
    }

    private void AnalyzeDuplicates()
    {
        DuplicateGroups.Clear();

        foreach (var item in Items)
        {
            item.IsDuplicate = false;
        }

        var groups = Items
            .Where(item => !string.IsNullOrWhiteSpace(item.Value))
            .GroupBy(item => item.Value.Trim(), StringComparer.Ordinal)
            .Where(group => group.Count() > 1);

        foreach (var group in groups)
        {
            var entries = group.OrderBy(item => item.Position).ToArray();
            foreach (var entry in entries)
            {
                entry.IsDuplicate = true;
            }

            DuplicateGroups.Add(new DuplicateReviewGroupViewModel(group.Key, entries));
        }

        OnPropertyChanged(nameof(HasDuplicates));
        OnPropertyChanged(nameof(DuplicateGroupCount));
        OnPropertyChanged(nameof(DuplicateEntryCount));
        OnPropertyChanged(nameof(DuplicateSummary));
        ReviewDuplicatesCommand.NotifyCanExecuteChanged();
    }

    private void NotifyReviewStateChanged()
    {
        OnPropertyChanged(nameof(ReviewProgressText));
        OnPropertyChanged(nameof(PendingRemovalCount));
        OnPropertyChanged(nameof(CleanupPreviewText));
        ReviewDuplicatesCommand.NotifyCanExecuteChanged();
        PreviousDuplicateGroupCommand.NotifyCanExecuteChanged();
        NextDuplicateGroupCommand.NotifyCanExecuteChanged();
    }
}
