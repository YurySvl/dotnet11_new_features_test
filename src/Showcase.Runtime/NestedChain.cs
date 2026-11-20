// A deeply nested async call chain. This ONE file is compiled twice:
//  - into Showcase.Runtime          with <Features>runtime-async=on</Features>  (namespace ...RuntimeAsync)
//  - into Showcase.Runtime.Classic  without it, i.e. classic compiler state machines (namespace ...Classic)
// so Program.cs can run the identical code both ways in the same process.

using System.Diagnostics;
using System.Reflection;

#if CLASSIC_ASYNC
namespace Showcase.Runtime.Classic;
#else
namespace Showcase.Runtime.RuntimeAsync;
#endif

public static class NestedChain
{
    // MethodImplAttributes.Async (0x2000) marks a method the runtime executes as async itself.
    private const MethodImplAttributes AsyncFlag = (MethodImplAttributes)0x2000;

    public static bool IsRuntimeAsync =>
        (typeof(NestedChain).GetMethod(nameof(Depth))!.MethodImplementationFlags & AsyncFlag) != 0;

    // Recursive chain used for timing. `yieldAtBottom` forces a real suspension at the deepest level.
    public static async Task<int> Depth(int depth, bool yieldAtBottom)
    {
        if (depth == 0)
        {
            if (yieldAtBottom)
            {
                await Task.Yield();
            }
            return 0;
        }
        return await Depth(depth - 1, yieldAtBottom) + 1;
    }

    // Three named levels used to show what a live stack trace looks like.
    public static async Task<string> OuterAsync() => await MiddleAsync();

    private static async Task<string> MiddleAsync() => await InnerAsync();

    private static async Task<string> InnerAsync()
    {
        await Task.CompletedTask; // completes synchronously; we are still on the original call stack
        return new StackTrace().ToString();
    }
}
