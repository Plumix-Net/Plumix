// Dart parity source: flutter/engine/src/flutter/lib/ui/platform_dispatcher.dart (ViewPadding)

namespace Plumix.UI;

/// <summary>
/// A representation of distances for each of the four edges of a rectangle, used to encode the view
/// insets and padding that applications should place around their user interface, as exposed by
/// <see cref="Widgets.FlutterView.ViewInsets"/> and <see cref="Widgets.FlutterView.Padding"/>.
/// </summary>
/// <remarks>
/// dart:ui's <c>ViewPadding</c>. The values are in physical pixels. Dart's constructor is private to the
/// engine; Plumix's hosts (and the test harness) play the engine's role, so the constructor is public.
/// Convert to logical pixels with <see cref="Rendering.EdgeInsets.FromViewPadding"/>.
/// </remarks>
public readonly record struct ViewPadding(double Left, double Top, double Right, double Bottom)
{
    /// <summary>A view padding that has zeros for each edge.</summary>
    public static ViewPadding Zero => default;

    /// <inheritdoc />
    public override string ToString() =>
        $"ViewPadding(left: {Fmt(Left)}, top: {Fmt(Top)}, right: {Fmt(Right)}, bottom: {Fmt(Bottom)})";

    // Dart's `double.toString`: integral values keep a trailing `.0`.
    private static string Fmt(double value) => Foundation.BindingBase.DartDoubleToString(value);
}
