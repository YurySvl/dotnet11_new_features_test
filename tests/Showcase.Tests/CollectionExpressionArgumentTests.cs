using Showcase.Language;

namespace Showcase.Tests;

public class CollectionExpressionArgumentTests
{
    [Fact]
    public void HashSet_uses_the_comparer_passed_with_with()
    {
        var tags = CollectionExpressionArgumentsDemo.CaseInsensitiveTags("Hello", "HELLO", "hello");

        Assert.Single(tags);
        Assert.Contains("hElLo", tags);
        Assert.Same(StringComparer.OrdinalIgnoreCase, tags.Comparer);
    }

    [Fact]
    public void List_capacity_is_passed_to_the_constructor()
    {
        List<int> list = [with(capacity: 64), 1, 2, 3];

        Assert.Equal(64, list.Capacity);
        Assert.Equal([1, 2, 3], list);
    }

    [Fact]
    public void Squares_helper_sets_exact_capacity()
    {
        var squares = CollectionExpressionArgumentsDemo.Squares(4);

        Assert.Equal([1, 4, 9, 16], squares);
        Assert.Equal(4, squares.Capacity);
    }
}
