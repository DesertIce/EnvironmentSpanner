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
    private ObservableCollection<string> items = [];

    [ObservableProperty]
    private string variableName = string.Empty;

    [ObservableProperty]
    private string? selectedItem;

    private string _originalValue = string.Empty;

    public ICommand OkCommand { get; set; } = null!;
    public ICommand CancelCommand { get; set; } = null!;

    public void Initialize(string name, string value)
    {
        VariableName = name;
        _originalValue = value;
        Items.Clear();

        if (!string.IsNullOrEmpty(value))
        {
            var parts = value.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                Items.Add(part.Trim());
            }
        }
        
        SelectedItem = Items.FirstOrDefault();
        RemoveItemCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void AddItem()
    {
        Items.Add("New Item");
        SelectedItem = Items.LastOrDefault();
        RemoveItemCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanRemoveItem))]
    private void RemoveItem()
    {
        if (SelectedItem != null && Items.Contains(SelectedItem))
        {
            var index = Items.IndexOf(SelectedItem);
            Items.Remove(SelectedItem);
            
            // Select the next item, or previous if at the end, or null if list is empty
            if (Items.Count > 0)
            {
                if (index < Items.Count)
                {
                    SelectedItem = Items[index];
                }
                else if (index > 0)
                {
                    SelectedItem = Items[index - 1];
                }
                else
                {
                    SelectedItem = Items[0];
                }
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

    partial void OnSelectedItemChanged(string? value)
    {
        EditText = value ?? string.Empty;
        RemoveItemCommand.NotifyCanExecuteChanged();
        UpdateItemCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanUpdateItem))]
    private void UpdateItem()
    {
        if (SelectedItem != null && Items.Contains(SelectedItem) && !string.IsNullOrWhiteSpace(EditText))
        {
            var index = Items.IndexOf(SelectedItem);
            if (index >= 0)
            {
                Items[index] = EditText;
                SelectedItem = EditText;
            }
        }
    }

    private bool CanUpdateItem() => SelectedItem != null && !string.IsNullOrWhiteSpace(EditText);

    public void Cancel() => Initialize(VariableName, _originalValue);

    public string GetResultValue() =>
        string.Join(";", Items.Where(i => !string.IsNullOrWhiteSpace(i)));
}
