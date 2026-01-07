using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using EnvironmentSpanner.ViewModels;
using EnvironmentSpanner.Views;

namespace EnvironmentSpanner;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;

    public MainWindow()
    {
        try
        {
            Log.Information("MainWindow constructor started");
            Log.Information("Initializing component");
            InitializeComponent();
            Log.Information("Component initialized successfully");
            
            Log.Information("Resolving MainWindowViewModel from service provider");
            _viewModel = App.ServiceProvider.GetRequiredService<MainWindowViewModel>();
            Log.Information("MainWindowViewModel resolved successfully");
            
            Log.Information("Subscribing to OpenListEditorRequested event");
            _viewModel.OpenListEditorRequested += OnOpenListEditor;
            Log.Information("Event subscription completed");
            
            Log.Information("Setting DataContext");
            DataContext = _viewModel;
            Log.Information("DataContext set successfully");
            
            Log.Information("MainWindow constructor completed successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in MainWindow constructor");
            throw;
        }
    }

    private void OnOpenListEditor(EnvironmentVariableViewModel vm)
    {
        try
        {
            Log.Information("Opening list editor for variable: {VariableName}", vm.Name);
            var dialog = new ListEditorDialog(vm.Name, vm.Value);
            Log.Information("ListEditorDialog created");
            
            var result = dialog.ShowDialog();
            Log.Information("ListEditorDialog result: {Result}", result);
            
            if (result == true && dialog.ResultValue != null)
            {
                Log.Information("Updating variable value for: {VariableName}", vm.Name);
                vm.Value = dialog.ResultValue;
                Log.Information("Variable value updated successfully");
            }
            else
            {
                Log.Information("List editor dialog cancelled or no result value");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error opening list editor for variable: {VariableName}", vm.Name);
            throw;
        }
    }
}
