using System.Text;

// Dart parity source: flutter/engine/src/flutter/lib/ui/window.dart

namespace Plumix.UI;

/// <summary>
/// Additional accessibility features that may be enabled by the platform.
/// </summary>
/// <remarks>
/// <para>
/// dart:ui's <c>AccessibilityFeatures</c>: a bitfield the engine reports through
/// <see cref="PlatformDispatcher.UpdateAccessibilityFeatures"/>. It is not intended to be
/// instantiated by the framework; read it from <see cref="PlatformDispatcher.AccessibilityFeatures"/>.
/// </para>
/// <para>
/// Dart's constructor is library-private and flutter_test's <c>FakeAccessibilityFeatures</c>
/// <c>implements</c> the class. C# has no implicit interfaces, so the getters are virtual and a
/// protected constructor lets a test double derive from it instead.
/// </para>
/// </remarks>
public class AccessibilityFeatures : IEquatable<AccessibilityFeatures>
{
    internal const int KAccessibleNavigationIndex = 1 << 0;
    internal const int KInvertColorsIndex = 1 << 1;
    internal const int KDisableAnimationsIndex = 1 << 2;
    internal const int KBoldTextIndex = 1 << 3;
    internal const int KReduceMotionIndex = 1 << 4;
    internal const int KHighContrastIndex = 1 << 5;
    internal const int KOnOffSwitchLabelsIndex = 1 << 6;
    internal const int KNoAnnounceIndex = 1 << 7;
    internal const int KNoAutoPlayAnimatedImagesIndex = 1 << 8;
    internal const int KNoAutoPlayVideosIndex = 1 << 9;
    internal const int KDeterministicCursorIndex = 1 << 10;

    /// <summary>Dart's <c>AccessibilityFeatures._(this._index)</c>.</summary>
    internal AccessibilityFeatures(int index)
    {
        Index = index;
    }

    /// <summary>
    /// For test doubles that override every getter, as flutter_test's <c>FakeAccessibilityFeatures</c>
    /// <c>implements</c> the Dart class.
    /// </summary>
    protected AccessibilityFeatures()
        : this(0)
    {
    }

    // A bitfield which represents each enabled feature.
    internal int Index { get; }

    /// <summary>
    /// Whether there is a running accessibility service which is changing the interaction model of
    /// the device.
    /// </summary>
    /// <remarks>For example, TalkBack on Android and VoiceOver on iOS enable this flag.</remarks>
    public virtual bool AccessibleNavigation => (KAccessibleNavigationIndex & Index) != 0;

    /// <summary>The platform is inverting the colors of the application.</summary>
    public virtual bool InvertColors => (KInvertColorsIndex & Index) != 0;

    /// <summary>The platform is requesting that animations be disabled or simplified.</summary>
    public virtual bool DisableAnimations => (KDisableAnimationsIndex & Index) != 0;

    /// <summary>The platform is requesting that text be rendered at a bold font weight.</summary>
    /// <remarks>Only supported on iOS and Android API 31+.</remarks>
    public virtual bool BoldText => (KBoldTextIndex & Index) != 0;

    /// <summary>
    /// The platform is requesting that certain animations be simplified and parallax effects removed.
    /// </summary>
    /// <remarks>Only supported on iOS.</remarks>
    public virtual bool ReduceMotion => (KReduceMotionIndex & Index) != 0;

    /// <summary>The platform is requesting that UI be rendered with darker colors.</summary>
    /// <remarks>Only supported on iOS and Android API 34+.</remarks>
    public virtual bool HighContrast => (KHighContrastIndex & Index) != 0;

    /// <summary>The platform is requesting to show on/off labels inside switches.</summary>
    /// <remarks>Only supported on iOS.</remarks>
    public virtual bool OnOffSwitchLabels => (KOnOffSwitchLabelsIndex & Index) != 0;

    /// <summary>
    /// Whether the platform supports the accessibility announcement API, i.e.
    /// <c>SemanticsService.SendAnnouncement</c>.
    /// </summary>
    /// <remarks>
    /// Returns <see langword="false"/> on platforms where announcements are deprecated or unsupported
    /// (Android discourages them in favor of live regions), and <see langword="true"/> where they are
    /// generally supported without discouragement (iOS, web, ...).
    /// </remarks>
    // This index check is inverted (== 0 vs != 0); far more platforms support
    // "announce" than discourage it.
    public virtual bool SupportsAnnounce => (KNoAnnounceIndex & Index) == 0;

    /// <summary>Whether the platform allows auto-playing animated images.</summary>
    /// <remarks>Only supported on iOS. Always returns <see langword="true"/> on other platforms.</remarks>
    // This index check is inverted (== 0 vs != 0) since most of the platforms
    // don't have an option to disable animated images auto play.
    public virtual bool AutoPlayAnimatedImages => (KNoAutoPlayAnimatedImagesIndex & Index) == 0;

    /// <summary>Whether the platform allows auto-playing videos.</summary>
    /// <remarks>Only supported on iOS. Always returns <see langword="true"/> on other platforms.</remarks>
    // This index check is inverted (== 0 vs != 0) since most of the platforms
    // don't have an option to disable videos auto play.
    public virtual bool AutoPlayVideos => (KNoAutoPlayVideosIndex & Index) == 0;

    /// <summary>
    /// The platform is requesting to show deterministic (non-blinking) cursor in editable text fields.
    /// </summary>
    /// <remarks>Only supported on iOS.</remarks>
    public virtual bool DeterministicCursor => (KDeterministicCursorIndex & Index) != 0;

    public static bool operator ==(AccessibilityFeatures? left, AccessibilityFeatures? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(AccessibilityFeatures? left, AccessibilityFeatures? right) => !(left == right);

    public override string ToString()
    {
        var features = new List<string>();
        if (AccessibleNavigation)
        {
            features.Add("accessibleNavigation");
        }

        if (InvertColors)
        {
            features.Add("invertColors");
        }

        if (DisableAnimations)
        {
            features.Add("disableAnimations");
        }

        if (BoldText)
        {
            features.Add("boldText");
        }

        if (ReduceMotion)
        {
            features.Add("reduceMotion");
        }

        if (HighContrast)
        {
            features.Add("highContrast");
        }

        if (OnOffSwitchLabels)
        {
            features.Add("onOffSwitchLabels");
        }

        if (SupportsAnnounce)
        {
            features.Add("supportsAnnounce");
        }

        if (AutoPlayAnimatedImages)
        {
            features.Add("autoPlayAnimatedImages");
        }

        if (AutoPlayVideos)
        {
            features.Add("autoPlayVideos");
        }

        if (DeterministicCursor)
        {
            features.Add("deterministicCursor");
        }

        // Dart's `'AccessibilityFeatures$features'` interpolates the list's `toString`: `[a, b]`.
        var builder = new StringBuilder("AccessibilityFeatures[");
        builder.AppendJoin(", ", features);
        builder.Append(']');
        return builder.ToString();
    }

    public virtual bool Equals(AccessibilityFeatures? other)
    {
        if (other is null || other.GetType() != GetType())
        {
            return false;
        }

        return other.Index == Index;
    }

    public override bool Equals(object? obj) => Equals(obj as AccessibilityFeatures);

    public override int GetHashCode() => Index.GetHashCode();
}
