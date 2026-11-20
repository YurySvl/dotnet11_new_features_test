// C# 15 closed hierarchies.
//
// `closed` on a class means: every DIRECT subclass must live in the same assembly (and module).
// Because the compiler can see the full list of subclasses, a switch that handles all of them
// is exhaustive without a `_` discard arm. A closed class is implicitly abstract.
//
// How this differs from unions:
// - A closed hierarchy is ordinary inheritance: cases share a base type, base members and
//   virtual methods. The cases MUST derive from the base.
// - A union combines EXISTING, unrelated types (even int, string or types you don't own) without
//   changing them. The union is a separate wrapper struct whose Value is one of the cases.
// - Closed: "these are my subclasses". Union: "a value is one of these types".

namespace Showcase.Language;

public closed record PaymentEvent(Guid PaymentId);
public record PaymentAuthorized(Guid PaymentId, decimal Amount) : PaymentEvent(PaymentId);
public record PaymentCaptured(Guid PaymentId, decimal Amount) : PaymentEvent(PaymentId);
public record PaymentRefunded(Guid PaymentId, decimal Amount, string Reason) : PaymentEvent(PaymentId);
public record PaymentFailed(Guid PaymentId, string Error) : PaymentEvent(PaymentId);

// The compiler only allows direct subclasses inside this assembly. In ANOTHER assembly
// (e.g. the Showcase.Tests project), this line fails to compile:
//
//   public record PaymentDisputed(Guid PaymentId) : PaymentEvent(PaymentId);
//   // error CS9382: 'PaymentDisputed': cannot use a closed type 'PaymentEvent'
//   //                from another assembly as a base type.
//
// (Verified on SDK 11.0.100. See the commented-out attempt in
// tests/Showcase.Tests/ClosedHierarchyTests.cs.)

public static class ClosedHierarchiesDemo
{
    public static string Describe(PaymentEvent e) => e switch
    {
        PaymentAuthorized a => $"authorized {a.Amount:0.00}",
        PaymentCaptured c => $"captured {c.Amount:0.00}",
        PaymentRefunded r => $"refunded {r.Amount:0.00} ({r.Reason})",
        PaymentFailed f => $"failed: {f.Error}",
        // No `_` arm and no warning: every direct subtype is handled.
    };

    public static void Run()
    {
        Console.WriteLine("== Closed hierarchies ==");
        var id = Guid.Parse("11111111-2222-3333-4444-555555555555");
        PaymentEvent[] events =
        [
            new PaymentAuthorized(id, 19.99m),
            new PaymentCaptured(id, 19.99m),
            new PaymentRefunded(id, 5m, "damaged item"),
            new PaymentFailed(id, "card expired"),
        ];
        foreach (var e in events)
        {
            Console.WriteLine($"  {e.GetType().Name,-18} -> {Describe(e)}");
        }
        Console.WriteLine($"  typeof(PaymentEvent).IsAbstract = {typeof(PaymentEvent).IsAbstract}");
        Console.WriteLine();
    }
}
