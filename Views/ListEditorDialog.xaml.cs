using System.Windows;
using CommunityToolkit.Mvvm.Input;
using EnvironmentSpanner.ViewModels;

namespace EnvironmentSpanner.Views;

public partial class ListEditorDialog : Window
{
    public ListEditorViewModel ViewModel { get; }

    public string? ResultValue { get; private set; }

    public ListEditorDialog(string variableName, string value)
    {
        InitializeComponent();
        ViewModel = new ListEditorViewModel();
        ViewModel.Initialize(variableName, value);
        ViewModel.OkCommand = new RelayCommand(() =>
        {
            ResultValue = ViewModel.GetResultValue();
            DialogResult = true;
            Close();
        });
        ViewModel.CancelCommand = new RelayCommand(() =>
        {
            ViewModel.Cancel();
            DialogResult = false;
            Close();
        });
        DataContext = ViewModel;
    }
}
