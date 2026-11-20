// ASP.NET Core 11 natively adds OpenTelemetry semantic-convention tags to its HTTP server Activity
// (source "Microsoft.AspNetCore"). We read them with a plain System.Diagnostics.ActivityListener,
// with NO OpenTelemetry packages at all.

using System.Collections.Concurrent;
using System.Diagnostics;

namespace Showcase.Api.Telemetry;

public sealed record RecordedSpan(
    string OperationName,
    string DisplayName,
    string Kind,
    double DurationMs,
    Dictionary<string, string?> Tags);

public sealed class SpanRecorder : IDisposable
{
    private const int MaxSpans = 20;
    private readonly ConcurrentQueue<RecordedSpan> _spans = new();
    private readonly ActivityListener _listener;

    public SpanRecorder()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Microsoft.AspNetCore",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = OnStopped,
        };
        ActivitySource.AddActivityListener(_listener);
    }

    public IReadOnlyList<RecordedSpan> LastSpans => [.. _spans.Reverse()];

    private void OnStopped(Activity activity)
    {
        var tags = activity.TagObjects.ToDictionary(t => t.Key, t => t.Value?.ToString());
        var span = new RecordedSpan(activity.OperationName, activity.DisplayName, activity.Kind.ToString(),
            activity.Duration.TotalMilliseconds, tags);

        _spans.Enqueue(span);
        while (_spans.Count > MaxSpans && _spans.TryDequeue(out _)) { }

        Console.WriteLine($"[span] {span.DisplayName} ({span.Kind}, {span.DurationMs:0.0} ms)");
        foreach (var (key, value) in tags)
        {
            Console.WriteLine($"         {key} = {value}");
        }
    }

    public void Dispose() => _listener.Dispose();
}
