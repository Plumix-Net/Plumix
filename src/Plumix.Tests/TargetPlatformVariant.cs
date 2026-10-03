using Plumix.UI;
using Xunit;

// C#-only test infrastructure: flutter_test's `TargetPlatformVariant` (widget_tester.dart), which runs a
// `testWidgets` body once per platform with `debugDefaultTargetPlatformOverride` set.

namespace Plumix.Tests;

/// <summary>
/// flutter_test's <c>TargetPlatformVariant</c>. Two ways to use it:
/// <list type="bullet">
/// <item>xUnit theory: <c>[Theory, MemberData(nameof(TargetPlatformVariant.DesktopData),
/// MemberType = typeof(TargetPlatformVariant))]</c> and <c>using var _ = TargetPlatformVariant.Override(platform);</c>
/// as the body's first line.</item>
/// <item>Inline loop: <c>TargetPlatformVariant.Desktop().Run(platform =&gt; { using var tester = ...; })</c>.</item>
/// </list>
/// <c>variant: TargetPlatformVariant.only(TargetPlatform.android)</c> is
/// <c>using var _ = TargetPlatformVariant.Override(TargetPlatform.Android);</c>.
/// </summary>
internal sealed class TargetPlatformVariant
{
    private static readonly TargetPlatform[] DesktopPlatforms =
        [TargetPlatform.Linux, TargetPlatform.MacOS, TargetPlatform.Windows];

    private static readonly TargetPlatform[] MobilePlatforms =
        [TargetPlatform.Android, TargetPlatform.IOS, TargetPlatform.Fuchsia];

    public TargetPlatformVariant(IEnumerable<TargetPlatform> values)
    {
        Values = values.Distinct().ToList();
    }

    /// <summary>The platforms the body runs for, in order.</summary>
    public IReadOnlyList<TargetPlatform> Values { get; }

    /// <summary>Dart's <c>TargetPlatformVariant.all(excluding:)</c>.</summary>
    public static TargetPlatformVariant All(params TargetPlatform[] excluding) =>
        new(Enum.GetValues<TargetPlatform>().Except(excluding));

    /// <summary>Dart's <c>TargetPlatformVariant.desktop()</c>: linux, macOS, windows.</summary>
    public static TargetPlatformVariant Desktop() => new(DesktopPlatforms);

    /// <summary>Dart's <c>TargetPlatformVariant.mobile()</c>: android, iOS, fuchsia.</summary>
    public static TargetPlatformVariant Mobile() => new(MobilePlatforms);

    /// <summary>Dart's <c>TargetPlatformVariant.only(platform)</c>.</summary>
    public static TargetPlatformVariant Only(TargetPlatform platform) => new([platform]);

    /// <summary>Every platform, as xUnit member data.</summary>
    public static TheoryData<TargetPlatform> AllData => ToTheoryData(Enum.GetValues<TargetPlatform>());

    /// <summary>The desktop platforms, as xUnit member data.</summary>
    public static TheoryData<TargetPlatform> DesktopData => ToTheoryData(DesktopPlatforms);

    /// <summary>The mobile platforms, as xUnit member data.</summary>
    public static TheoryData<TargetPlatform> MobileData => ToTheoryData(MobilePlatforms);

    /// <summary>
    /// Sets <c>debugDefaultTargetPlatformOverride</c> to <paramref name="platform"/> until the returned
    /// scope is disposed, which restores the previous override (Dart's variant <c>setUp</c>/<c>tearDown</c>).
    /// </summary>
    public static IDisposable Override(TargetPlatform platform) => new OverrideScope(platform);

    /// <summary>Runs <paramref name="body"/> once per platform, each with the override set.</summary>
    public void Run(Action<TargetPlatform> body)
    {
        foreach (TargetPlatform platform in Values)
        {
            using IDisposable scope = Override(platform);
            body(platform);
        }
    }

    private static TheoryData<TargetPlatform> ToTheoryData(IEnumerable<TargetPlatform> platforms)
    {
        var data = new TheoryData<TargetPlatform>();
        foreach (TargetPlatform platform in platforms)
        {
            data.Add(platform);
        }

        return data;
    }

    private sealed class OverrideScope : IDisposable
    {
        private readonly TargetPlatform? _previous;
        private bool _disposed;

        public OverrideScope(TargetPlatform platform)
        {
            _previous = PlatformDefaults.DebugTargetPlatformOverride;
            PlatformDefaults.DebugTargetPlatformOverride = platform;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            PlatformDefaults.DebugTargetPlatformOverride = _previous;
        }
    }
}
