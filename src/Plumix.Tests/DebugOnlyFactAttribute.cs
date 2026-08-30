using Plumix.Foundation;
using Xunit;

// C#-only test infrastructure: Dart has no counterpart, because `flutter test` only ever runs a
// debug build.

namespace Plumix.Tests;

/// <summary>
/// A <see cref="FactAttribute"/> that skips the test unless the assemblies were compiled in debug
/// mode.
/// </summary>
/// <remarks>
/// Flutter guards diagnostics and contract checks with `assert(...)`, which the Dart VM strips
/// outside a debug build, and `flutter test` always builds in debug — so Flutter's own tests never
/// observe the stripped behavior. Plumix mirrors the stripping through
/// <see cref="Constants.KDebugMode"/> but runs the same suite under `-c Profile` and `-c Release`,
/// where a test that asserts a debug-only message, `DebugFillProperties` output or `AssertionError`
/// has nothing to assert. Marking such a test with this attribute states that its subject is
/// debug-only rather than that it is broken.
///
/// The build-mode contract itself — what diagnostics degrade to in profile and release — is
/// asserted by the `BuildModeGates_*` tests, which run in every configuration.
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public sealed class DebugOnlyFactAttribute : FactAttribute
{
    public DebugOnlyFactAttribute()
    {
        if (!Constants.KDebugMode)
        {
            Skip = "Debug-only: Dart guards this behavior with assert(), so it does not exist in a "
                   + "profile or release build.";
        }
    }
}
