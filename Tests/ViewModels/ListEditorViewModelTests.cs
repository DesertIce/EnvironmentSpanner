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
        Assert.Equal(["Item1", "Item2", "Item3"], viewModel.Items.Select(item => item.Value));
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
        viewModel.SelectedItem = viewModel.Items[1];

        // Act
        viewModel.RemoveItemCommand.Execute(null);

        // Assert
        Assert.Equal(2, viewModel.Items.Count);
        Assert.DoesNotContain(viewModel.Items, item => item.Value == "Item2");
    }

    [Fact]
    public void RemoveItemCommand_WithSecondEqualEntry_RemovesSelectedInstance()
    {
        var viewModel = new ListEditorViewModel();
        viewModel.Initialize("PATH", @"C:\One;C:\One;C:\Two");
        var first = viewModel.Items[0];
        var second = viewModel.Items[1];
        viewModel.SelectedItem = second;

        viewModel.RemoveItemCommand.Execute(null);

        Assert.Equal(2, viewModel.Items.Count);
        Assert.Same(first, viewModel.Items[0]);
        Assert.DoesNotContain(viewModel.Items, item => ReferenceEquals(item, second));
    }

    [Fact]
    public void UpdateItemCommand_WithSecondEqualEntry_UpdatesSelectedInstance()
    {
        var viewModel = new ListEditorViewModel();
        viewModel.Initialize("PATH", @"C:\One;C:\One;C:\Two");
        var first = viewModel.Items[0];
        var second = viewModel.Items[1];
        viewModel.SelectedItem = second;
        viewModel.EditText = @"C:\Updated";

        viewModel.UpdateItemCommand.Execute(null);

        Assert.Equal(@"C:\One", first.Value);
        Assert.Equal(@"C:\Updated", second.Value);
    }

    [Fact]
    public void EntryPositions_RefreshAfterRemoval()
    {
        var viewModel = new ListEditorViewModel();
        viewModel.Initialize("PATH", "One;Two;Three");
        viewModel.SelectedItem = viewModel.Items[1];

        viewModel.RemoveItemCommand.Execute(null);

        Assert.Equal([0, 1], viewModel.Items.Select(item => item.Position));
    }

    [Theory]
    [InlineData(@"C:\Tools", @"C:\Tools", true)]
    [InlineData(@" C:\Tools ", @"C:\Tools", true)]
    [InlineData(@"C:\Tools", @"c:\tools", false)]
    [InlineData(@"C:\Tools", @"C:/Tools", false)]
    [InlineData(@"C:\Tools", "C:\\Tools\\", false)]
    public void DuplicateAnalysis_UsesTrimmedOrdinalComparison(
        string first,
        string second,
        bool expectedDuplicate)
    {
        var viewModel = new ListEditorViewModel();

        viewModel.Initialize("PATH", $"{first};{second}");

        Assert.Equal(expectedDuplicate, viewModel.HasDuplicates);
        Assert.Equal(expectedDuplicate, viewModel.Items[0].IsDuplicate);
        Assert.Equal(expectedDuplicate, viewModel.Items[1].IsDuplicate);
    }

    [Fact]
    public void DuplicateAnalysis_DoesNotGroupBlankEntries()
    {
        var viewModel = new ListEditorViewModel();

        viewModel.Initialize("TEST", " ; ");

        Assert.False(viewModel.HasDuplicates);
        Assert.Empty(viewModel.DuplicateGroups);
    }

    [Fact]
    public void DuplicateAnalysis_CountsExtraOccurrencesAndGroups()
    {
        var viewModel = new ListEditorViewModel();

        viewModel.Initialize("PATH", "One;One;One;Two;Two;Three");

        Assert.Equal(2, viewModel.DuplicateGroupCount);
        Assert.Equal(3, viewModel.DuplicateEntryCount);
        Assert.Equal("3 duplicate entries found in 2 groups.", viewModel.DuplicateSummary);
    }

    [Fact]
    public void DuplicateSummary_UsesSingularLabels()
    {
        var viewModel = new ListEditorViewModel();

        viewModel.Initialize("PATH", "One;One");

        Assert.Equal("1 duplicate entry found in 1 group.", viewModel.DuplicateSummary);
    }

    [Fact]
    public void AddItemCommand_ReanalyzesDuplicates()
    {
        var viewModel = new ListEditorViewModel();
        viewModel.Initialize("PATH", string.Empty);

        viewModel.AddItemCommand.Execute(null);
        viewModel.AddItemCommand.Execute(null);

        Assert.True(viewModel.HasDuplicates);
    }

    [Fact]
    public void UpdateItemCommand_ReanalyzesDuplicates()
    {
        var viewModel = new ListEditorViewModel();
        viewModel.Initialize("PATH", "One;Two");
        viewModel.SelectedItem = viewModel.Items[1];
        viewModel.EditText = "One";

        viewModel.UpdateItemCommand.Execute(null);

        Assert.True(viewModel.HasDuplicates);
    }

    [Fact]
    public void RemoveItemCommand_ReanalyzesDuplicates()
    {
        var viewModel = new ListEditorViewModel();
        viewModel.Initialize("PATH", "One;One");
        viewModel.SelectedItem = viewModel.Items[1];

        viewModel.RemoveItemCommand.Execute(null);

        Assert.False(viewModel.HasDuplicates);
    }
}
