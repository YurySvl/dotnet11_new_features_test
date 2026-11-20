using Showcase.Language;

namespace Showcase.Tests;

public class LabeledBreakContinueTests
{
    private static readonly int[,] Grid =
    {
        { 1, 2, 3, 4 },
        { 5, -1, 7, 8 },
        { 9, 10, 11, 12 },
    };

    [Theory]
    [InlineData(3)]
    [InlineData(15)]
    [InlineData(21)]
    [InlineData(999)]
    public void Labeled_break_matches_flag_version(int target)
    {
        Assert.Equal(
            LabeledBreakContinueDemo.FindFirstPairOld(Grid, target),
            LabeledBreakContinueDemo.FindFirstPair(Grid, target));
    }

    [Fact]
    public void Labeled_break_finds_first_pair()
    {
        Assert.Equal((2, 1), LabeledBreakContinueDemo.FindFirstPair(Grid, 21));
        Assert.Null(LabeledBreakContinueDemo.FindFirstPair(Grid, 999));
    }

    [Fact]
    public void Labeled_continue_skips_rows_with_negatives()
    {
        Assert.Equal([10, 42], LabeledBreakContinueDemo.RowSumsSkippingNegativeRows(Grid));
        Assert.Equal(
            LabeledBreakContinueDemo.RowSumsSkippingNegativeRowsOld(Grid),
            LabeledBreakContinueDemo.RowSumsSkippingNegativeRows(Grid));
    }
}
