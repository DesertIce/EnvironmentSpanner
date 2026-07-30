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
        Items.Add(new ListEntryViewModel(_nextEntryId++, Items.Count, "New Item"));
        SelectedItem = Items.LastOrDefault();
        AnalyzeDuplicates();
        RemoveItemCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanRemoveItem))]
    private void RemoveItem()
    {
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
        if (SelectedItem is not null && !string.IsNullOrWhiteSpace(EditText))
        {
            SelectedItem.Value = EditText;
            EditText = SelectedItem.Value;
            AnalyzeDuplicates();
        }
    }

    private bool CanUpdateItem() => SelectedItem != null && !string.IsNullOrWhiteSpace(EditText);

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
    }
}
