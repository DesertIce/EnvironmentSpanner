using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using EnvironmentSpanner.Models;
using EnvironmentSpanner.Services;
using EnvironmentSpanner.ViewModels;
using Moq;
using Xunit;

namespace EnvironmentSpanner.Tests.ViewModels;

public class MainWindowViewModelTests
{
    private IServiceProvider CreateServiceProvider(Mock<IEnvironmentVariableService> mockService)
    {
        var services = new ServiceCollection();
        services.AddSingleton(mockService.Object);
        services.AddSingleton<ILogger<MainWindowViewModel>>(new Mock<ILogger<MainWindowViewModel>>().Object);
        services.AddTransient<MainWindowViewModel>();
        return services.BuildServiceProvider();
    }

    [Fact]
    public void Constructor_InitializesCollections()
    {
        // Arrange
        var mockService = new Mock<IEnvironmentVariableService>();
        mockService.Setup(s => s.GetEnvironmentVariables(It.IsAny<EnvironmentVariableTarget>()))
            .Returns(Array.Empty<EnvironmentVariable>());
        mockService.Setup(s => s.IsElevated()).Returns(false);
        var serviceProvider = CreateServiceProvider(mockService);

        // Act
        var viewModel = serviceProvider.GetRequiredService<MainWindowViewModel>();

        // Assert
        Assert.NotNull(viewModel.UserVariables);
        Assert.NotNull(viewModel.SystemVariables);
        Assert.False(viewModel.IsElevated);
        Assert.True(viewModel.IsSystemReadOnly);
    }

    [Fact]
    public void IsElevated_True_SetsSystemReadOnlyToFalse()
    {
        // Arrange
        var mockService = new Mock<IEnvironmentVariableService>();
        mockService.Setup(s => s.GetEnvironmentVariables(It.IsAny<EnvironmentVariableTarget>()))
            .Returns(Array.Empty<EnvironmentVariable>());
        mockService.Setup(s => s.IsElevated()).Returns(true);
        var serviceProvider = CreateServiceProvider(mockService);

        // Act
        var viewModel = serviceProvider.GetRequiredService<MainWindowViewModel>();

        // Assert
        Assert.True(viewModel.IsElevated);
        Assert.False(viewModel.IsSystemReadOnly);
    }

    [Fact]
    public void AddVariableCommand_UserTab_AddsToUserVariables()
    {
        // Arrange
        var mockService = new Mock<IEnvironmentVariableService>();
        mockService.Setup(s => s.GetEnvironmentVariables(It.IsAny<EnvironmentVariableTarget>()))
            .Returns(Array.Empty<EnvironmentVariable>());
        mockService.Setup(s => s.IsElevated()).Returns(false);
        var serviceProvider = CreateServiceProvider(mockService);
        var viewModel = serviceProvider.GetRequiredService<MainWindowViewModel>();
        viewModel.SelectedTabIndex = 0; // User tab

        // Act
        viewModel.AddVariableCommand.Execute(null);

        // Assert
        Assert.Single(viewModel.UserVariables);
        Assert.Equal("NEW_VARIABLE", viewModel.UserVariables[0].Name);
    }

    [Fact]
    public void SaveCommand_WithChanges_CallsService()
    {
        // Arrange
        var mockService = new Mock<IEnvironmentVariableService>();
        mockService.Setup(s => s.GetEnvironmentVariables(It.IsAny<EnvironmentVariableTarget>()))
            .Returns(Array.Empty<EnvironmentVariable>());
        mockService.Setup(s => s.IsElevated()).Returns(false);
        var serviceProvider = CreateServiceProvider(mockService);
        var viewModel = serviceProvider.GetRequiredService<MainWindowViewModel>();
        viewModel.SelectedTabIndex = 0;
        viewModel.AddVariableCommand.Execute(null);
        viewModel.UserVariables[0].Name = "TEST_VAR";
        viewModel.UserVariables[0].Value = "TestValue";

        // Act
        viewModel.SaveCommand.Execute(null);

        // Assert
        mockService.Verify(s => s.SetEnvironmentVariable("TEST_VAR", "TestValue", EnvironmentVariableTarget.User), Times.Once);
    }

    [Fact]
    public void SaveCommand_SystemVariables_OnlySavesWhenElevated()
    {
        // Arrange - not elevated
        var mockService = new Mock<IEnvironmentVariableService>();
        mockService.Setup(s => s.GetEnvironmentVariables(It.IsAny<EnvironmentVariableTarget>()))
            .Returns(Array.Empty<EnvironmentVariable>());
        mockService.Setup(s => s.IsElevated()).Returns(false);
        var serviceProvider = CreateServiceProvider(mockService);
        var viewModel = serviceProvider.GetRequiredService<MainWindowViewModel>();
        viewModel.SelectedTabIndex = 1; // System tab
        viewModel.AddVariableCommand.Execute(null);
        viewModel.SystemVariables[0].Name = "TEST_SYSTEM_VAR";
        viewModel.SystemVariables[0].Value = "TestValue";

        // Act
        viewModel.SaveCommand.Execute(null);

        // Assert - should not save system variables when not elevated
        mockService.Verify(s => s.SetEnvironmentVariable(It.IsAny<string>(), It.IsAny<string>(), EnvironmentVariableTarget.Machine), Times.Never);
    }

    [Fact]
    public void SaveCommand_SystemVariables_SavesWhenElevated()
    {
        // Arrange - elevated
        var mockService = new Mock<IEnvironmentVariableService>();
        mockService.Setup(s => s.GetEnvironmentVariables(It.IsAny<EnvironmentVariableTarget>()))
            .Returns(Array.Empty<EnvironmentVariable>());
        mockService.Setup(s => s.IsElevated()).Returns(true);
        var serviceProvider = CreateServiceProvider(mockService);
        var viewModel = serviceProvider.GetRequiredService<MainWindowViewModel>();
        viewModel.SelectedTabIndex = 1; // System tab
        viewModel.AddVariableCommand.Execute(null);
        viewModel.SystemVariables[0].Name = "TEST_SYSTEM_VAR";
        viewModel.SystemVariables[0].Value = "TestValue";

        // Act
        viewModel.SaveCommand.Execute(null);

        // Assert - should save system variables when elevated
        mockService.Verify(s => s.SetEnvironmentVariable("TEST_SYSTEM_VAR", "TestValue", EnvironmentVariableTarget.Machine), Times.Once);
    }

    [Fact]
    public void SaveCommand_OnlySavesModifiedEntries()
    {
        // Arrange
        var existingVar = new EnvironmentVariable
        {
            Name = "EXISTING_VAR",
            Value = "OriginalValue",
            Target = EnvironmentVariableTarget.User
        };
        var mockService = new Mock<IEnvironmentVariableService>();
        mockService.Setup(s => s.GetEnvironmentVariables(EnvironmentVariableTarget.User))
            .Returns(new[] { existingVar });
        mockService.Setup(s => s.GetEnvironmentVariables(EnvironmentVariableTarget.Machine))
            .Returns(Array.Empty<EnvironmentVariable>());
        mockService.Setup(s => s.IsElevated()).Returns(false);
        var serviceProvider = CreateServiceProvider(mockService);
        var viewModel = serviceProvider.GetRequiredService<MainWindowViewModel>();
        
        // Find the existing variable and modify it
        var existingVm = viewModel.UserVariables.First(v => v.Name == "EXISTING_VAR");
        existingVm.Value = "ModifiedValue";

        // Act
        viewModel.SaveCommand.Execute(null);

        // Assert - should only save the modified variable, not unchanged ones
        mockService.Verify(s => s.SetEnvironmentVariable("EXISTING_VAR", "ModifiedValue", EnvironmentVariableTarget.User), Times.Once);
        // Verify it doesn't save unchanged variables by checking it's only called once
        mockService.Verify(s => s.SetEnvironmentVariable(It.IsAny<string>(), It.IsAny<string>(), EnvironmentVariableTarget.User), Times.Once);
    }
}
