using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using EnvironmentSpanner.Models;
using EnvironmentSpanner.Services;

namespace EnvironmentSpanner.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IEnvironmentVariableService _service;
    private readonly ILogger<MainWindowViewModel> _logger;

    [ObservableProperty]
    private ObservableCollection<EnvironmentVariableViewModel> userVariables = new();

    [ObservableProperty]
    private ObservableCollection<EnvironmentVariableViewModel> systemVariables = new();

    [ObservableProperty]
    private bool isElevated;

    [ObservableProperty]
    private bool isSystemReadOnly;

    [ObservableProperty]
    private int selectedTabIndex;

    [ObservableProperty]
    private bool hasPendingChanges;

    [ObservableProperty]
    private bool isBusy;

    private ObservableCollection<EnvironmentVariableViewModel> _originalUserVariables = new();
    private ObservableCollection<EnvironmentVariableViewModel> _originalSystemVariables = new();

    public MainWindowViewModel(IEnvironmentVariableService service, ILogger<MainWindowViewModel> logger)
    {
        try
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogInformation("MainWindowViewModel constructor started");
            _logger.LogInformation("IEnvironmentVariableService injected");
            _logger.LogInformation("ILogger injected");
            
            _logger.LogInformation("Checking elevation status");
            IsElevated = _service.IsElevated();
            _logger.LogInformation("Elevation status: {IsElevated}", IsElevated);
            
            IsSystemReadOnly = !IsElevated;
            _logger.LogInformation("System read-only status: {IsSystemReadOnly}", IsSystemReadOnly);
            
            _logger.LogInformation("Loading environment variables");
            _ = LoadVariablesAsync();
            _logger.LogInformation("MainWindowViewModel constructor completed successfully");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error in MainWindowViewModel constructor");
            throw;
        }
    }

    private async Task LoadVariablesAsync()
    {
        try
        {
            IsBusy = true;
            _logger.LogInformation("LoadVariablesAsync started");
            
            var userVars = await Task.Run(() => 
                _service.GetEnvironmentVariables(EnvironmentVariableTarget.User).ToList());
            var systemVars = await Task.Run(() => 
                _service.GetEnvironmentVariables(EnvironmentVariableTarget.Machine).ToList());
            
            // Update UI on UI thread
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                UserVariables.Clear();
                foreach (var variable in userVars.OrderBy(v => v.Name))
                {
                    var vm = new EnvironmentVariableViewModel(variable, false, OnDeleteUserVariable, OnOpenListEditor);
                    vm.PropertyChanged += (s, e) => OnVariableChanged();
                    UserVariables.Add(vm);
                }
                _logger.LogInformation("Loaded {Count} user environment variables", UserVariables.Count);
                
                SystemVariables.Clear();
                foreach (var variable in systemVars.OrderBy(v => v.Name))
                {
                    var vm = new EnvironmentVariableViewModel(variable, IsSystemReadOnly, OnDeleteSystemVariable, OnOpenListEditor);
                    vm.PropertyChanged += (s, e) => OnVariableChanged();
                    SystemVariables.Add(vm);
                }
                _logger.LogInformation("Loaded {Count} system environment variables", SystemVariables.Count);
                
                SaveOriginalState();
                HasPendingChanges = false;
            });
            
            _logger.LogInformation("LoadVariablesAsync completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in LoadVariablesAsync");
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                MessageBox.Show($"Error loading environment variables: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }
        finally
        {
            IsBusy = false;
        }
    }


    private void SaveOriginalState()
    {
        _originalUserVariables = new ObservableCollection<EnvironmentVariableViewModel>(
            UserVariables.Select(vm => new EnvironmentVariableViewModel(
                new EnvironmentVariable
                {
                    Name = vm.Name,
                    Value = vm.Value,
                    Target = EnvironmentVariableTarget.User
                },
                false)));

        _originalSystemVariables = new ObservableCollection<EnvironmentVariableViewModel>(
            SystemVariables.Select(vm => new EnvironmentVariableViewModel(
                new EnvironmentVariable
                {
                    Name = vm.Name,
                    Value = vm.Value,
                    Target = EnvironmentVariableTarget.Machine
                },
                IsSystemReadOnly)));
    }

    private void OnVariableChanged()
    {
        HasPendingChanges = true;
    }

    private void OnDeleteUserVariable(EnvironmentVariableViewModel vm)
    {
        UserVariables.Remove(vm);
        OnVariableChanged();
    }

    private void OnDeleteSystemVariable(EnvironmentVariableViewModel vm)
    {
        if (!IsSystemReadOnly)
        {
            SystemVariables.Remove(vm);
            OnVariableChanged();
        }
    }

    public event Action<EnvironmentVariableViewModel>? OpenListEditorRequested;

    private void OnOpenListEditor(EnvironmentVariableViewModel vm) => OpenListEditorRequested?.Invoke(vm);

    [RelayCommand]
    private void AddVariable()
    {
        var target = SelectedTabIndex == 0 ? EnvironmentVariableTarget.User : EnvironmentVariableTarget.Machine;
        var isReadOnly = target == EnvironmentVariableTarget.Machine && IsSystemReadOnly;

        var newVariable = new EnvironmentVariable
        {
            Name = "NEW_VARIABLE",
            Value = string.Empty,
            Target = target
        };

        var vm = new EnvironmentVariableViewModel(newVariable, isReadOnly,
            target == EnvironmentVariableTarget.User ? OnDeleteUserVariable : OnDeleteSystemVariable,
            OnOpenListEditor);
        vm.PropertyChanged += (s, e) => OnVariableChanged();

        if (target == EnvironmentVariableTarget.User)
        {
            UserVariables.Add(vm);
        }
        else
        {
            SystemVariables.Add(vm);
        }

        OnVariableChanged();
    }

    [RelayCommand]
    private async Task Save()
    {
        try
        {
            IsBusy = true;
            _logger.LogInformation("Save operation started");
            // Build dictionaries of original state for comparison
            var originalUserVars = _originalUserVariables.ToDictionary(v => v.Name, v => v.Value);
            var originalSystemVars = _originalSystemVariables.ToDictionary(v => v.Name, v => v.Value);

            await Task.Run(() =>
            {
                // Save user variables - only modified entries
                var currentUserVarNames = UserVariables.Select(v => v.Name).ToHashSet();
                
                foreach (var vm in UserVariables)
                {
                    if (originalUserVars.TryGetValue(vm.Name, out var originalValue))
                    {
                        // Variable existed - only save if value changed
                        if (originalValue != vm.Value)
                        {
                            _service.SetEnvironmentVariable(vm.Name, vm.Value, EnvironmentVariableTarget.User);
                            _logger.LogInformation("User variable '{Name}' updated.", vm.Name);
                        }
                    }
                    else
                    {
                        // New variable - save it
                        _service.SetEnvironmentVariable(vm.Name, vm.Value, EnvironmentVariableTarget.User);
                        _logger.LogInformation("User variable '{Name}' added.", vm.Name);
                    }
                }

                // Remove deleted user variables
                foreach (var originalVar in originalUserVars.Keys)
                {
                    if (!currentUserVarNames.Contains(originalVar))
                    {
                        _service.DeleteEnvironmentVariable(originalVar, EnvironmentVariableTarget.User);
                        _logger.LogInformation("User variable '{Name}' deleted.", originalVar);
                    }
                }

                // Save system variables - only if elevated and only modified entries
                if (IsElevated)
                {
                    var currentSystemVarNames = SystemVariables.Select(v => v.Name).ToHashSet();

                    foreach (var vm in SystemVariables)
                    {
                        if (originalSystemVars.TryGetValue(vm.Name, out var originalValue))
                        {
                            // Variable existed - only save if value changed
                            if (originalValue != vm.Value)
                            {
                                _service.SetEnvironmentVariable(vm.Name, vm.Value, EnvironmentVariableTarget.Machine);
                                _logger.LogInformation("System variable '{Name}' updated.", vm.Name);
                            }
                        }
                        else
                        {
                            // New variable - save it
                            _service.SetEnvironmentVariable(vm.Name, vm.Value, EnvironmentVariableTarget.Machine);
                            _logger.LogInformation("System variable '{Name}' added.", vm.Name);
                        }
                    }

                    // Remove deleted system variables
                    foreach (var originalVar in originalSystemVars.Keys)
                    {
                        if (!currentSystemVarNames.Contains(originalVar))
                        {
                            _service.DeleteEnvironmentVariable(originalVar, EnvironmentVariableTarget.Machine);
                            _logger.LogInformation("System variable '{Name}' deleted.", originalVar);
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("Attempted to save system variables without elevation. Operation skipped.");
                }
            });

            await LoadVariablesAsync();
            HasPendingChanges = false;
            _logger.LogInformation("Environment variables saved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save environment variables");
            MessageBox.Show($"Error saving environment variables: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task Cancel()
    {
        await LoadVariablesAsync();
        HasPendingChanges = false;
    }
}
