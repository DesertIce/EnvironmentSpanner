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

    [Fact]
    public void ReviewDuplicatesCommand_StartsWithFirstGroupAndDefaultRemovals()
    {
        var viewModel = new ListEditorViewModel();
        viewModel.Initialize("PATH", "One;One;Two;Two;Two");

        viewModel.ReviewDuplicatesCommand.Execute(null);

        Assert.True(viewModel.IsReviewingDuplicates);
        Assert.Equal(0, viewModel.CurrentDuplicateGroupIndex);
        Assert.Equal("Group 1 of 2", viewModel.ReviewProgressText);
        Assert.False(viewModel.CurrentDuplicateGroup!.Occurrences[0].IsMarkedForRemoval);
        Assert.True(viewModel.CurrentDuplicateGroup.Occurrences[1].IsMarkedForRemoval);
    }

    [Fact]
    public void DuplicateGroupNavigation_MovesWithoutChangingItems()
    {
        var viewModel = new ListEditorViewModel();
        viewModel.Initialize("PATH", "One;One;Two;Two");
        var originalIds = viewModel.Items.Select(item => item.Id).ToArray();
        viewModel.ReviewDuplicatesCommand.Execute(null);

        viewModel.NextDuplicateGroupCommand.Execute(null);

        Assert.Equal(1, viewModel.CurrentDuplicateGroupIndex);
        Assert.Equal("Group 2 of 2", viewModel.ReviewProgressText);
        Assert.Equal(originalIds, viewModel.Items.Select(item => item.Id));
    }

    [Fact]
    public void KeepAllCurrentGroupCommand_ClearsProposedRemovals()
    {
        var viewModel = new ListEditorViewModel();
        viewModel.Initialize("PATH", "One;One;One");
        viewModel.ReviewDuplicatesCommand.Execute(null);

        viewModel.KeepAllCurrentGroupCommand.Execute(null);

        Assert.All(
            viewModel.CurrentDuplicateGroup!.Occurrences,
            item => Assert.False(item.IsMarkedForRemoval));
    }

    [Fact]
    public void ToggleReviewRemovalCommand_DoesNotAllowRemovingEntireGroup()
    {
        var viewModel = new ListEditorViewModel();
        viewModel.Initialize("PATH", "One;One");
        viewModel.ReviewDuplicatesCommand.Execute(null);
        var retained = viewModel.CurrentDuplicateGroup!.Occurrences[0];

        viewModel.ToggleReviewRemovalCommand.Execute(retained);

        Assert.False(retained.IsMarkedForRemoval);
        Assert.Equal(
            "At least one occurrence must be kept.",
            viewModel.ReviewConstraintMessage);
    }

    [Fact]
    public void CancelDuplicateReviewCommand_DiscardsSelectionsWithoutChangingItems()
    {
        var viewModel = new ListEditorViewModel();
        viewModel.Initialize("PATH", "One;One;Two");
        var originalIds = viewModel.Items.Select(item => item.Id).ToArray();
        viewModel.ReviewDuplicatesCommand.Execute(null);
        viewModel.KeepAllCurrentGroupCommand.Execute(null);

        viewModel.CancelDuplicateReviewCommand.Execute(null);

        Assert.False(viewModel.IsReviewingDuplicates);
        Assert.Equal(originalIds, viewModel.Items.Select(item => item.Id));
    }

    [Fact]
    public void ReviewDuplicatesCommand_WithNoDuplicates_CannotExecute()
    {
        var viewModel = new ListEditorViewModel();
        viewModel.Initialize("PATH", "One;Two");

        Assert.False(viewModel.ReviewDuplicatesCommand.CanExecute(null));
    }

    [Fact]
    public void CleanupPreview_UpdatesAfterKeepAll()
    {
        var viewModel = new ListEditorViewModel();
        viewModel.Initialize("PATH", "One;One;Two;Two;Two");
        viewModel.ReviewDuplicatesCommand.Execute(null);

        viewModel.KeepAllCurrentGroupCommand.Execute(null);

        Assert.Equal(2, viewModel.PendingRemovalCount);
        Assert.Equal(
            "Remove 2 entries from 1 group. The list will contain 3 entries instead of 5.",
            viewModel.CleanupPreviewText);
    }

    [Theory]
    [InlineData("add")]
    [InlineData("update")]
    [InlineData("remove")]
    public void OrdinaryMutation_CancelsActiveDuplicateReview(string mutation)
    {
        var viewModel = new ListEditorViewModel();
        viewModel.Initialize("PATH", "One;One;Two");
        viewModel.ReviewDuplicatesCommand.Execute(null);

        switch (mutation)
        {
            case "add":
                viewModel.AddItemCommand.Execute(null);
                break;
            case "update":
                viewModel.EditText = "Changed";
                viewModel.UpdateItemCommand.Execute(null);
                break;
            case "remove":
                viewModel.RemoveItemCommand.Execute(null);
                break;
        }

        Assert.False(viewModel.IsReviewingDuplicates);
    }

    [Fact]
    public void ApplyDuplicateCleanupCommand_RemovesOnlyMarkedInstancesInOrder()
    {
        var viewModel = new ListEditorViewModel();
        viewModel.Initialize("PATH", "One;Two;One;Three;One");
        viewModel.ReviewDuplicatesCommand.Execute(null);
        var kept = viewModel.Items[0];

        viewModel.ApplyDuplicateCleanupCommand.Execute(null);

        Assert.Equal(["One", "Two", "Three"], viewModel.Items.Select(item => item.Value));
        Assert.Same(kept, viewModel.Items[0]);
        Assert.False(viewModel.IsReviewingDuplicates);
        Assert.True(viewModel.CanUndoDuplicateCleanup);
        Assert.Equal("Removed 2 duplicate entries.", viewModel.CleanupResultMessage);
    }

    [Fact]
    public void ApplyDuplicateCleanupCommand_CanKeepALaterOccurrence()
    {
        var viewModel = new ListEditorViewModel();
        viewModel.Initialize("PATH", "One;One;Two");
        var first = viewModel.Items[0];
        var second = viewModel.Items[1];
        viewModel.ReviewDuplicatesCommand.Execute(null);
        var occurrences = viewModel.CurrentDuplicateGroup!.Occurrences;
        viewModel.ToggleReviewRemovalCommand.Execute(occurrences[1]);
        viewModel.ToggleReviewRemovalCommand.Execute(occurrences[0]);

        viewModel.ApplyDuplicateCleanupCommand.Execute(null);

        Assert.DoesNotContain(viewModel.Items, item => ReferenceEquals(item, first));
        Assert.Same(second, viewModel.Items[0]);
        Assert.Contains(viewModel.SelectedItem!, viewModel.Items);
    }

    [Fact]
    public void UndoDuplicateCleanupCommand_RestoresInstancesAtPriorPositions()
    {
        var viewModel = new ListEditorViewModel();
        viewModel.Initialize("PATH", "One;Two;One;Three;One");
        var original = viewModel.Items.ToArray();
        viewModel.ReviewDuplicatesCommand.Execute(null);
        viewModel.ApplyDuplicateCleanupCommand.Execute(null);

        viewModel.UndoDuplicateCleanupCommand.Execute(null);

        Assert.Equal(original.Select(item => item.Id), viewModel.Items.Select(item => item.Id));
        Assert.Equal([0, 1, 2, 3, 4], viewModel.Items.Select(item => item.Position));
        Assert.False(viewModel.CanUndoDuplicateCleanup);
    }

    [Theory]
    [InlineData("add")]
    [InlineData("update")]
    [InlineData("remove")]
    public void NextOrdinaryMutation_InvalidatesCleanupUndo(string mutation)
    {
        var viewModel = new ListEditorViewModel();
        viewModel.Initialize("PATH", "One;One;Two");
        viewModel.ReviewDuplicatesCommand.Execute(null);
        viewModel.ApplyDuplicateCleanupCommand.Execute(null);

        switch (mutation)
        {
            case "add":
                viewModel.AddItemCommand.Execute(null);
                break;
            case "update":
                viewModel.SelectedItem = viewModel.Items[0];
                viewModel.EditText = "Changed";
                viewModel.UpdateItemCommand.Execute(null);
                break;
            case "remove":
                viewModel.SelectedItem = viewModel.Items[0];
                viewModel.RemoveItemCommand.Execute(null);
                break;
        }

        Assert.False(viewModel.CanUndoDuplicateCleanup);
    }

    [Fact]
    public void Cancel_AfterAppliedCleanup_RestoresOriginalValue()
    {
        var viewModel = new ListEditorViewModel();
        viewModel.Initialize("PATH", "One;One;Two");
        viewModel.ReviewDuplicatesCommand.Execute(null);
        viewModel.ApplyDuplicateCleanupCommand.Execute(null);

        viewModel.Cancel();

        Assert.Equal("One;One;Two", viewModel.GetResultValue());
        Assert.False(viewModel.CanUndoDuplicateCleanup);
        Assert.False(viewModel.IsReviewingDuplicates);
    }

    [Fact]
    public void ApplyDuplicateCleanupCommand_WithKeepAll_CannotExecute()
    {
        var viewModel = new ListEditorViewModel();
        viewModel.Initialize("PATH", "One;One");
        viewModel.ReviewDuplicatesCommand.Execute(null);
        viewModel.KeepAllCurrentGroupCommand.Execute(null);

        Assert.False(viewModel.ApplyDuplicateCleanupCommand.CanExecute(null));
        Assert.Equal("One;One", viewModel.GetResultValue());
    }

    [Fact]
    public void ApplyDuplicateCleanupCommand_RequiresFinalReviewGroup()
    {
        var viewModel = new ListEditorViewModel();
        viewModel.Initialize("PATH", "One;One;Two;Two");
        viewModel.ReviewDuplicatesCommand.Execute(null);

        Assert.False(viewModel.ApplyDuplicateCleanupCommand.CanExecute(null));

        viewModel.NextDuplicateGroupCommand.Execute(null);

        Assert.True(viewModel.ApplyDuplicateCleanupCommand.CanExecute(null));
    }

    [Fact]
    public void Initialize_AfterReview_RefreshesDerivedReviewState()
    {
        var viewModel = new ListEditorViewModel();
        viewModel.Initialize("PATH", "One;One;Two;Two");
        viewModel.ReviewDuplicatesCommand.Execute(null);
        var changedProperties = new List<string?>();
        var applyCanExecuteChanged = 0;
        viewModel.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);
        viewModel.ApplyDuplicateCleanupCommand.CanExecuteChanged +=
            (_, _) => applyCanExecuteChanged++;

        viewModel.Initialize("PATH", "Three;Four");

        Assert.Contains(nameof(viewModel.ReviewProgressText), changedProperties);
        Assert.Contains(nameof(viewModel.PendingRemovalCount), changedProperties);
        Assert.Contains(nameof(viewModel.CleanupPreviewText), changedProperties);
        Assert.True(applyCanExecuteChanged > 0);
    }
}
