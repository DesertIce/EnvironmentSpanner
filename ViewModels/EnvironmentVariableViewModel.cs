using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnvironmentSpanner.Models;

namespace EnvironmentSpanner.ViewModels;

public partial class EnvironmentVariableViewModel : ObservableObject
{
    private readonly EnvironmentVariable _model;
    private readonly Action<EnvironmentVariableViewModel>? _onDelete;
    private readonly Action<EnvironmentVariableViewModel>? _onOpenListEditor;

    public EnvironmentVariableViewModel(EnvironmentVariable model, bool isReadOnly, Action<EnvironmentVariableViewModel>? onDelete = null, Action<EnvironmentVariableViewModel>? onOpenListEditor = null)
    {
        _model = model;
        _onDelete = onDelete;
        _onOpenListEditor = onOpenListEditor;
        IsReadOnly = isReadOnly;
        CanEdit = !isReadOnly;
    }

    public string Name
    {
        get => _model.Name;
        set
        {
            if (_model.Name != value)
            {
                _model.Name = value;
                OnPropertyChanged();
            }
        }
    }

    public string Value
    {
        get => _model.Value;
        set
        {
            if (_model.Value != value)
            {
                _model.Value = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsListValue));
            }
        }
    }

    [ObservableProperty]
    private bool isReadOnly;

    [ObservableProperty]
    private bool canEdit;

    public bool IsListValue => _model.IsListValue;

    [RelayCommand]
    private void Delete() => _onDelete?.Invoke(this);

    [RelayCommand(CanExecute = nameof(CanOpenListEditor))]
    private void OpenListEditor() => _onOpenListEditor?.Invoke(this);

    private bool CanOpenListEditor() => IsListValue;

    public EnvironmentVariable GetModel() => _model;
}
