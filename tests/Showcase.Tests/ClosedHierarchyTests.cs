using Showcase.Language;

namespace Showcase.Tests;

// This test project is a DIFFERENT assembly from Showcase.Language, so the following does not compile:
//
// public record PaymentDisputed(Guid PaymentId) : PaymentEvent(PaymentId);
// error CS9382: 'PaymentDisputed': cannot use a closed type 'PaymentEvent' from another assembly as a base type.

public class ClosedHierarchyTests
{
    private static readonly Guid Id = Guid.NewGuid();

    public static TheoryData<PaymentEvent, string> Events => new()
    {
        { new PaymentAuthorized(Id, 10m), "authorized 10.00" },
        { new PaymentCaptured(Id, 10m), "captured 10.00" },
        { new PaymentRefunded(Id, 2.5m, "late"), "refunded 2.50 (late)" },
        { new PaymentFailed(Id, "declined"), "failed: declined" },
    };

    [Theory]
    [MemberData(nameof(Events))]
    public void Every_direct_subtype_is_handled(PaymentEvent e, string expected)
    {
        Assert.Equal(expected, ClosedHierarchiesDemo.Describe(e));
    }

    [Fact]
    public void Closed_class_is_implicitly_abstract()
    {
        Assert.True(typeof(PaymentEvent).IsAbstract);
    }
}
