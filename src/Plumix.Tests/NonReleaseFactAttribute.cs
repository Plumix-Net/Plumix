using Plumix.Foundation;
using Xunit;

// C#-only test infrastructure: Dart has no counterpart, because `flutter test` only ever runs a
// debug build.

namespace Plumix.Tests;

/// <summary>
/// A <see cref="FactAttribute"/> that skips the test in a release build.
/// </summary>
/// <remarks>
/// For behavior Flutter guards with <c>!kReleaseMode</c> rather than <c>assert</c> — timeline events
/// and <c>FlutterTimeline</c> collection exist in debug and profile builds alike — so the test runs
/// under <c>-c Profile</c> too, unlike a <see cref="DebugOnlyFactAttribute"/> test.
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public sealed class NonReleaseFactAttribute : FactAttribute
{
    public NonReleaseFactAttribute()
    {
        if (Constants.KReleaseMode)
        {
            Skip = "Not in release: Dart guards this behavior with !kReleaseMode.";
        }
    }
}
