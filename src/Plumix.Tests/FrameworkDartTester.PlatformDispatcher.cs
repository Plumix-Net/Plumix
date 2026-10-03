using Plumix.UI;

// C#-only test infrastructure: flutter_test's `TestPlatformDispatcher` and `FakeAccessibilityFeatures`
// (flutter/packages/flutter_test/lib/src/window.dart), reduced to the accessibility features.

namespace Plumix.Tests;

internal sealed partial class FrameworkDartTester
{
    /// <summary>Dart's <c>tester.platformDispatcher</c>.</summary>
    public TestPlatformDispatcher PlatformDispatcher => TestPlatformDispatcher.Instance;

    /// <summary>
    /// The test values a test left behind go away with it, as <c>TestWidgetsFlutterBinding</c> resets
    /// its dispatcher between tests.
    /// </summary>
    private static void ResetPlatformDispatcherTestValues()
    {
        if (UI.PlatformDispatcher.Instance.HasAccessibilityFeaturesTestValue)
        {
            TestPlatformDispatcher.Instance.ClearAccessibilityFeaturesTestValue();
        }
    }
}

/// <summary>
/// flutter_test's <c>TestPlatformDispatcher</c>: the real <see cref="PlatformDispatcher"/> with
/// properties a test can fake.
/// </summary>
/// <remarks>
/// Dart wraps the dispatcher and makes every test view report the wrapper as its
/// <c>platformDispatcher</c>. Plumix's views always report <see cref="PlatformDispatcher.Instance"/>, so
/// the wrapper writes its test values into that dispatcher, where the framework reads them.
/// </remarks>
internal sealed class TestPlatformDispatcher
{
    private TestPlatformDispatcher()
    {
    }

    public static TestPlatformDispatcher Instance { get; } = new();

    private static PlatformDispatcher Dispatcher => PlatformDispatcher.Instance;

    /// <summary>
    /// Dart's <c>accessibilityFeatures</c>: the test value if one is set, otherwise the real
    /// dispatcher's, whose default (no flag reported) has <c>supportsAnnounce</c> on.
    /// </summary>
    public AccessibilityFeatures AccessibilityFeatures => Dispatcher.AccessibilityFeatures;

    /// <summary>
    /// Dart's <c>accessibilityFeaturesTestValue</c> setter: hides the real
    /// <see cref="AccessibilityFeatures"/> and invokes <see cref="OnAccessibilityFeaturesChanged"/>.
    /// Consider <see cref="FakeAccessibilityFeatures"/> to provide specific values.
    /// </summary>
    public AccessibilityFeatures AccessibilityFeaturesTestValue
    {
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            Dispatcher.SetAccessibilityFeaturesTestValue(value);
        }
    }

    /// <summary>
    /// Dart's <c>clearAccessibilityFeaturesTestValue</c>: the real value shows again and
    /// <see cref="OnAccessibilityFeaturesChanged"/> is invoked.
    /// </summary>
    public void ClearAccessibilityFeaturesTestValue() => Dispatcher.SetAccessibilityFeaturesTestValue(null);

    /// <summary>Dart's <c>onAccessibilityFeaturesChanged</c>, forwarded to the real dispatcher.</summary>
    public Action? OnAccessibilityFeaturesChanged
    {
        get => Dispatcher.OnAccessibilityFeaturesChanged;
        set => Dispatcher.OnAccessibilityFeaturesChanged = value;
    }

    /// <summary>Dart's <c>clearAllTestValues</c>, for the values this harness can fake.</summary>
    public void ClearAllTestValues() => ClearAccessibilityFeaturesTestValue();
}

/// <summary>
/// flutter_test's <c>FakeAccessibilityFeatures</c>: a test version of <see cref="AccessibilityFeatures"/>
/// in which specific features may be enabled. By default, all features are disabled; for an instance
/// where all of them are enabled, see <see cref="AllOn"/>.
/// </summary>
/// <remarks>
/// Dart's class <c>implements AccessibilityFeatures</c>; C# derives from it and overrides every
/// getter. Unlike the real class, <see cref="SupportsAnnounce"/> defaults to <see langword="false"/>.
/// </remarks>
internal sealed class FakeAccessibilityFeatures : AccessibilityFeatures
{
    /// <summary>An instance of <see cref="AccessibilityFeatures"/> where all the features are enabled.</summary>
    public static readonly FakeAccessibilityFeatures AllOn = new(
        accessibleNavigation: true,
        invertColors: true,
        disableAnimations: true,
        boldText: true,
        reduceMotion: true,
        highContrast: true,
        onOffSwitchLabels: true,
        supportsAnnounce: true,
        autoPlayAnimatedImages: true,
        autoPlayVideos: true,
        deterministicCursor: true);

    /// <summary>Creates a test instance of <see cref="AccessibilityFeatures"/>; all features disabled by default.</summary>
    public FakeAccessibilityFeatures(
        bool accessibleNavigation = false,
        bool invertColors = false,
        bool disableAnimations = false,
        bool boldText = false,
        bool reduceMotion = false,
        bool highContrast = false,
        bool onOffSwitchLabels = false,
        bool supportsAnnounce = false,
        bool autoPlayAnimatedImages = false,
        bool autoPlayVideos = false,
        bool deterministicCursor = false)
    {
        AccessibleNavigation = accessibleNavigation;
        InvertColors = invertColors;
        DisableAnimations = disableAnimations;
        BoldText = boldText;
        ReduceMotion = reduceMotion;
        HighContrast = highContrast;
        OnOffSwitchLabels = onOffSwitchLabels;
        SupportsAnnounce = supportsAnnounce;
        AutoPlayAnimatedImages = autoPlayAnimatedImages;
        AutoPlayVideos = autoPlayVideos;
        DeterministicCursor = deterministicCursor;
    }

    public override bool AccessibleNavigation { get; }

    public override bool InvertColors { get; }

    public override bool DisableAnimations { get; }

    public override bool BoldText { get; }

    public override bool ReduceMotion { get; }

    public override bool HighContrast { get; }

    public override bool OnOffSwitchLabels { get; }

    public override bool SupportsAnnounce { get; }

    public override bool AutoPlayAnimatedImages { get; }

    public override bool AutoPlayVideos { get; }

    public override bool DeterministicCursor { get; }

    public override bool Equals(AccessibilityFeatures? other)
    {
        if (other is null || other.GetType() != GetType())
        {
            return false;
        }

        return other is FakeAccessibilityFeatures fake
               && fake.AccessibleNavigation == AccessibleNavigation
               && fake.InvertColors == InvertColors
               && fake.DisableAnimations == DisableAnimations
               && fake.BoldText == BoldText
               && fake.ReduceMotion == ReduceMotion
               && fake.HighContrast == HighContrast
               && fake.OnOffSwitchLabels == OnOffSwitchLabels
               && fake.SupportsAnnounce == SupportsAnnounce
               && fake.AutoPlayAnimatedImages == AutoPlayAnimatedImages
               && fake.AutoPlayVideos == AutoPlayVideos
               && fake.DeterministicCursor == DeterministicCursor;
    }

    public override bool Equals(object? obj) => Equals(obj as AccessibilityFeatures);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(AccessibleNavigation);
        hash.Add(InvertColors);
        hash.Add(DisableAnimations);
        hash.Add(BoldText);
        hash.Add(ReduceMotion);
        hash.Add(HighContrast);
        hash.Add(OnOffSwitchLabels);
        hash.Add(SupportsAnnounce);
        hash.Add(AutoPlayAnimatedImages);
        hash.Add(AutoPlayVideos);
        hash.Add(DeterministicCursor);
        return hash.ToHashCode();
    }
}
