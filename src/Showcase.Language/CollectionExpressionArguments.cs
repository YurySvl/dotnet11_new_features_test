// C# 15 collection expression arguments.
//
// `with(...)` as the FIRST element of a collection expression passes those arguments to the
// target collection's constructor (or CollectionBuilder method). Before C# 15 you had to fall back
// to `new HashSet<string>(comparer) { ... }` if you needed a comparer or a capacity.
// Not allowed for arrays or spans.

namespace Showcase.Language;

public static class CollectionExpressionArgumentsDemo
{
    public static HashSet<string> CaseInsensitiveTags(params string[] tags) =>
        [with(StringComparer.OrdinalIgnoreCase), .. tags];

    public static List<int> Squares(int count) =>
        [with(capacity: count), .. Enumerable.Range(1, count).Select(i => i * i)];

    public static void Run()
    {
        Console.WriteLine("== Collection expression arguments ==");

        string[] values = ["one", "two", "three"];
        List<string> names = [with(capacity: values.Length * 2), .. values];
        Console.WriteLine($"  List: Count={names.Count}, Capacity={names.Capacity}");

        HashSet<string> tags = CaseInsensitiveTags("Hello", "HELLO", "hello", "World");
        Console.WriteLine($"  HashSet (OrdinalIgnoreCase): Count={tags.Count} -> [{string.Join(", ", tags)}]");
        Console.WriteLine($"  tags.Contains(\"WORLD\") = {tags.Contains("WORLD")}");

        HashSet<string> plain = ["Hello", "HELLO", "hello"];
        Console.WriteLine($"  HashSet (default comparer): Count={plain.Count}");

        Dictionary<string, int> ages = [with(StringComparer.OrdinalIgnoreCase)];
        ages["Ada"] = 36;
        Console.WriteLine($"  Dictionary with comparer: ages[\"ADA\"] = {ages["ADA"]}");

        var squares = Squares(5);
        Console.WriteLine($"  Squares: [{string.Join(", ", squares)}], Capacity={squares.Capacity}");
        Console.WriteLine();
    }
}
