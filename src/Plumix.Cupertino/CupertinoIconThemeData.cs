using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity source: cupertino_ui/lib/src/icon_theme_data.dart

/// <summary>
/// An <see cref="IconThemeData"/> subclass that automatically resolves its <see cref="IconThemeData.Color"/>
/// when retrieved using <see cref="IconTheme.Of"/>.
/// </summary>
public sealed class CupertinoIconThemeData : IconThemeData
{
    /// <summary>Creates a <see cref="CupertinoIconThemeData"/>.</summary>
    public CupertinoIconThemeData(
        Color? Color = null,
        double? Size = null,
        double? Opacity = null,
        double? Fill = null,
        double? Weight = null,
        double? Grade = null,
        double? OpticalSize = null,
        IReadOnlyList<Shadow>? Shadows = null,
        bool? ApplyTextScaling = null)
        : base(
            Color: Color,
            Size: Size,
            Opacity: Opacity,
            Fill: Fill,
            Weight: Weight,
            Grade: Grade,
            OpticalSize: OpticalSize,
            Shadows: Shadows,
            ApplyTextScaling: ApplyTextScaling)
    {
    }

    /// <summary>
    /// Called by <see cref="IconTheme.Of"/> to resolve <see cref="IconThemeData.Color"/> against
    /// the given <see cref="BuildContext"/>.
    /// </summary>
    public override IconThemeData Resolve(BuildContext context)
    {
        Color? resolvedColor = CupertinoDynamicColor.MaybeResolve(Color, context);
        return resolvedColor == Color ? this : CopyWith(color: resolvedColor);
    }

    /// <summary>Creates a copy of this icon theme but with the given fields replaced with the new values.</summary>
    public override CupertinoIconThemeData CopyWith(
        Color? color = null,
        double? size = null,
        double? opacity = null,
        double? fill = null,
        double? weight = null,
        double? grade = null,
        double? opticalSize = null,
        IReadOnlyList<Shadow>? shadows = null,
        bool? applyTextScaling = null) =>
        new(
            Size: size ?? Size,
            Fill: fill ?? Fill,
            Weight: weight ?? Weight,
            Grade: grade ?? Grade,
            OpticalSize: opticalSize ?? OpticalSize,
            Color: color ?? Color,
            Opacity: opacity ?? Opacity,
            Shadows: shadows ?? Shadows,
            ApplyTextScaling: applyTextScaling ?? ApplyTextScaling);

    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(
            CupertinoColors.CreateCupertinoColorProperty("color", Color, defaultValue: DiagnosticsDefaults.NullValue));
    }
}
