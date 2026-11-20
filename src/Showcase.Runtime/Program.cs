// Runtime async demo: same nested async chain, compiled with and without runtime async.
// This is a quick Stopwatch/allocation comparison, NOT a rigorous benchmark (use BenchmarkDotNet for that).

using System.Diagnostics;
using Classic = Showcase.Runtime.Classic.NestedChain;
using RtAsync = Showcase.Runtime.RuntimeAsync.NestedChain;

Console.WriteLine("== Runtime async ==");
Console.WriteLine($"Classic.NestedChain uses runtime async: {Classic.IsRuntimeAsync}");
Console.WriteLine($"RtAsync.NestedChain uses runtime async: {RtAsync.IsRuntimeAsync}");
Console.WriteLine();

// 1) Live stack traces captured inside three nested async methods.
PrintStack("Classic (compiler state machines)", await Classic.OuterAsync());
PrintStack("Runtime async", await RtAsync.OuterAsync());

// 2) Timing + allocations.
const int Depth = 20;
Console.WriteLine($"Chain depth {Depth}. Median of 5 runs each. Allocations = all threads (GC.GetTotalAllocatedBytes).");
Console.WriteLine($"{"scenario",-38} {"mode",-14} {"time",10} {"alloc/op",12}");

await Compare("synchronous completion (no suspension)", 200_000, yieldAtBottom: false);
await Compare("suspends at bottom (Task.Yield)", 20_000, yieldAtBottom: true);

static async Task Compare(string scenario, int iterations, bool yieldAtBottom)
{
    // Warm up both so tiering/JIT is not measured.
    await Run(Classic.Depth, iterations / 10, yieldAtBottom);
    await Run(RtAsync.Depth, iterations / 10, yieldAtBottom);

    var classic = await Measure(Classic.Depth, iterations, yieldAtBottom);
    var rt = await Measure(RtAsync.Depth, iterations, yieldAtBottom);

    Console.WriteLine($"{scenario,-38} {"classic",-14} {classic.Ms,8:0.0}ms {classic.BytesPerOp,10:0} B");
    Console.WriteLine($"{"",-38} {"runtime async",-14} {rt.Ms,8:0.0}ms {rt.BytesPerOp,10:0} B");
}

static async Task<(double Ms, double BytesPerOp)> Measure(Func<int, bool, Task<int>> chain, int iterations, bool yieldAtBottom)
{
    var times = new List<double>();
    var bytes = new List<double>();
    for (int i = 0; i < 5; i++)
    {
        GC.Collect();
        long before = GC.GetTotalAllocatedBytes(precise: true);
        var sw = Stopwatch.StartNew();
        await Run(chain, iterations, yieldAtBottom);
        sw.Stop();
        long after = GC.GetTotalAllocatedBytes(precise: true);
        times.Add(sw.Elapsed.TotalMilliseconds);
        bytes.Add((after - before) / (double)iterations);
    }
    times.Sort();
    bytes.Sort();
    return (times[2], bytes[2]);
}

static async Task Run(Func<int, bool, Task<int>> chain, int iterations, bool yieldAtBottom)
{
    for (int i = 0; i < iterations; i++)
    {
        await chain(Depth, yieldAtBottom);
    }
}

static void PrintStack(string title, string stack)
{
    var frames = stack.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
    Console.WriteLine($"--- Live stack inside InnerAsync: {title} ({frames.Length} frames)");
    foreach (var frame in frames)
    {
        Console.WriteLine(frame);
    }
    Console.WriteLine();
}
