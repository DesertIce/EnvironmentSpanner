using EnvironmentSpanner.ViewModels;
using Xunit;

namespace EnvironmentSpanner.Tests.ViewModels;

public class ListEditorViewModelTests
{
    [Fact]
    public void Initialize_WithSemicolonDelimitedValue_ParsesCorrectly()
    {
        // Arrange
        var viewModel = new ListEditorViewModel();
        var value = "Item1;Item2;Item3";

        // Act
        viewModel.Initialize("TEST_VAR", value);

        // Assert
        Assert.Equal(3, viewModel.Items.Count);
        Assert.Contains("Item1", viewModel.Items);
        Assert.Contains("Item2", viewModel.Items);
        Assert.Contains("Item3", viewModel.Items);
    }

    [Fact]
    public void Initialize_WithEmptyValue_CreatesEmptyList()
    {
        // Arrange
        var viewModel = new ListEditorViewModel();

        // Act
        viewModel.Initialize("TEST_VAR", string.Empty);

        // Assert
        Assert.Empty(viewModel.Items);
    }

    [Fact]
    public void GetResultValue_WithItems_ReturnsSemicolonDelimitedString()
    {
        // Arrange
        var viewModel = new ListEditorViewModel();
        viewModel.Initialize("TEST_VAR", "Item1;Item2;Item3");

        // Act
        var result = viewModel.GetResultValue();

        // Assert
        Assert.Equal("Item1;Item2;Item3", result);
    }

    [Fact]
    public void AddItemCommand_AddsNewItem()
    {
        // Arrange
        var viewModel = new ListEditorViewModel();
        viewModel.Initialize("TEST_VAR", "Item1");

        // Act
        viewModel.AddItemCommand.Execute(null);

        // Assert
        Assert.Equal(2, viewModel.Items.Count);
    }

    [Fact]
    public void RemoveItemCommand_WithSelectedItem_RemovesItem()
    {
        // Arrange
        var viewModel = new ListEditorViewModel();
        viewModel.Initialize("TEST_VAR", "Item1;Item2;Item3");
        viewModel.SelectedItem = "Item2";

        // Act
        viewModel.RemoveItemCommand.Execute(null);

        // Assert
        Assert.Equal(2, viewModel.Items.Count);
        Assert.DoesNotContain("Item2", viewModel.Items);
    }
}
