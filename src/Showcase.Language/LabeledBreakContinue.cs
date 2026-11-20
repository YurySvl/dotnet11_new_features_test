// C# 15 labeled break / continue.
//
// Put a label on a loop (`outer: for (...)`) and then `break outer;` or `continue outer;` from
// a nested loop. This replaces the classic "bool found = false" flag variable or a `goto`.

namespace Showcase.Language;

public static class LabeledBreakContinueDemo
{
    // Old style: a flag variable to escape two loops.
    public static (int Row, int Col)? FindFirstPairOld(int[,] grid, int target)
    {
        (int, int)? result = null;
        bool found = false;
        for (int r = 0; r < grid.GetLength(0); r++)
        {
            for (int c = 0; c < grid.GetLength(1) - 1; c++)
            {
                if (grid[r, c] + grid[r, c + 1] == target)
                {
                    result = (r, c);
                    found = true;
                    break;
                }
            }
            if (found)
            {
                break;
            }
        }
        return result;
    }

    // C# 15: `break outer;` leaves both loops.
    public static (int Row, int Col)? FindFirstPair(int[,] grid, int target)
    {
        (int, int)? result = null;
        outer: for (int r = 0; r < grid.GetLength(0); r++)
        {
            for (int c = 0; c < grid.GetLength(1) - 1; c++)
            {
                if (grid[r, c] + grid[r, c + 1] == target)
                {
                    result = (r, c);
                    break outer;
                }
            }
        }
        return result;
    }

    // Old style: skip a row once a negative number is seen.
    public static List<int> RowSumsSkippingNegativeRowsOld(int[,] grid)
    {
        List<int> sums = [];
        for (int r = 0; r < grid.GetLength(0); r++)
        {
            int sum = 0;
            bool skip = false;
            for (int c = 0; c < grid.GetLength(1); c++)
            {
                if (grid[r, c] < 0)
                {
                    skip = true;
                    break;
                }
                sum += grid[r, c];
            }
            if (skip)
            {
                continue;
            }
            sums.Add(sum);
        }
        return sums;
    }

    // C# 15: `continue rows;` jumps straight to the next iteration of the outer loop.
    public static List<int> RowSumsSkippingNegativeRows(int[,] grid)
    {
        List<int> sums = [];
        rows: for (int r = 0; r < grid.GetLength(0); r++)
        {
            int sum = 0;
            for (int c = 0; c < grid.GetLength(1); c++)
            {
                if (grid[r, c] < 0)
                {
                    continue rows;
                }
                sum += grid[r, c];
            }
            sums.Add(sum);
        }
        return sums;
    }

    public static void Run()
    {
        Console.WriteLine("== Labeled break / continue ==");
        int[,] grid =
        {
            { 1, 2, 3, 4 },
            { 5, -1, 7, 8 },
            { 9, 10, 11, 12 },
        };

        Console.WriteLine($"  First adjacent pair summing to 21 (flag):  {FindFirstPairOld(grid, 21)}");
        Console.WriteLine($"  First adjacent pair summing to 21 (label): {FindFirstPair(grid, 21)}");
        Console.WriteLine($"  Pair summing to 100 (label):               {FindFirstPair(grid, 100)?.ToString() ?? "none"}");
        Console.WriteLine($"  Row sums, skip rows with negatives (flag):  [{string.Join(", ", RowSumsSkippingNegativeRowsOld(grid))}]");
        Console.WriteLine($"  Row sums, skip rows with negatives (label): [{string.Join(", ", RowSumsSkippingNegativeRows(grid))}]");
        Console.WriteLine();
    }
}
