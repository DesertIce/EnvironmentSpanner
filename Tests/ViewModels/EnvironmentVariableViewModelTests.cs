using EnvironmentSpanner.Models;
using EnvironmentSpanner.ViewModels;
using Xunit;

namespace EnvironmentSpanner.Tests.ViewModels;

public class EnvironmentVariableViewModelTests
{
    [Fact]
    public void Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        var model = new EnvironmentVariable
        {
            Name = "TEST_VAR",
            Value = "TestValue",
            Target = EnvironmentVariableTarget.User
        };

        // Act
        var viewModel = new EnvironmentVariableViewModel(model, isReadOnly: false);

        // Assert
        Assert.Equal("TEST_VAR", viewModel.Name);
        Assert.Equal("TestValue", viewModel.Value);
        Assert.False(viewModel.IsReadOnly);
        Assert.True(viewModel.CanEdit);
    }

    [Fact]
    public void IsListValue_WithSemicolon_ReturnsTrue()
    {
        // Arrange
        var model = new EnvironmentVariable
        {
            Name = "PATH",
            Value = "C:\\Path1;C:\\Path2",
            Target = EnvironmentVariableTarget.User
        };
        var viewModel = new EnvironmentVariableViewModel(model, isReadOnly: false);

        // Act & Assert
        Assert.True(viewModel.IsListValue);
    }

    [Fact]
    public void IsListValue_WithoutSemicolon_ReturnsFalse()
    {
        // Arrange
        var model = new EnvironmentVariable
        {
            Name = "TEST_VAR",
            Value = "SimpleValue",
            Target = EnvironmentVariableTarget.User
        };
        var viewModel = new EnvironmentVariableViewModel(model, isReadOnly: false);

        // Act & Assert
        Assert.False(viewModel.IsListValue);
    }

    [Fact]
    public void Value_Setter_UpdatesModel()
    {
        // Arrange
        var model = new EnvironmentVariable
        {
            Name = "TEST_VAR",
            Value = "OldValue",
            Target = EnvironmentVariableTarget.User
        };
        var viewModel = new EnvironmentVariableViewModel(model, isReadOnly: false);

        // Act
        viewModel.Value = "NewValue";

        // Assert
        Assert.Equal("NewValue", viewModel.Value);
        Assert.Equal("NewValue", model.Value);
    }
}
