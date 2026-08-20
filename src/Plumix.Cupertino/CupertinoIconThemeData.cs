using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity source: cupertino_ui/lib/src/icon_theme_data.dart

/// <summary>An icon theme that resolves a Cupertino dynamic color at the consumer's context.</summary>
public sealed class CupertinoIconThemeData : IconThemeData
{
    private readonly CupertinoDynamicColor? _dynamicColor;

    public CupertinoIconThemeData(
        CupertinoDynamicColor? Color = null,
        double? Size = null,
        double? Opacity = null,
        double? Fill = null,
        double? Weight = null,
        double? Grade = null,
        double? OpticalSize = null,
        IReadOnlyList<Shadow>? Shadows = null,
        bool? ApplyTextScaling = null)
        : base(
            Color: Color?.Value,
            Size: Size,
            Opacity: Opacity,
            Fill: Fill,
            Weight: Weight,
            Grade: Grade,
            OpticalSize: OpticalSize,
            Shadows: Shadows,
            ApplyTextScaling: ApplyTextScaling)
    {
        _dynamicColor = Color;
    }

    public override IconThemeData Resolve(BuildContext context)
    {
        if (_dynamicColor is null)
        {
            return this;
        }

        Color resolvedColor = CupertinoDynamicColor.Resolve(_dynamicColor, context);
        return CopyWith(color: resolvedColor);
    }

    public override CupertinoIconThemeData CopyWith(
        Color? color = null,
        double? size = null,
        double? opacity = null,
        double? fill = null,
        double? weight = null,
        double? grade = null,
        double? opticalSize = null,
        IReadOnlyList<Shadow>? shadows = null,
        bool? applyTextScaling = null)
    {
        CupertinoDynamicColor? effectiveColor = color.HasValue ? color.Value : _dynamicColor;
        return new CupertinoIconThemeData(
            Color: effectiveColor,
            Size: size ?? Size,
            Opacity: opacity ?? Opacity,
            Fill: fill ?? Fill,
            Weight: weight ?? Weight,
            Grade: grade ?? Grade,
            OpticalSize: opticalSize ?? OpticalSize,
            Shadows: shadows ?? Shadows,
            ApplyTextScaling: applyTextScaling ?? ApplyTextScaling);
    }

    public override bool Equals(IconThemeData? other)
    {
        return base.Equals(other)
               && other is CupertinoIconThemeData cupertino
               && Equals(cupertino._dynamicColor, _dynamicColor);
    }

    public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), _dynamicColor);

    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new DiagnosticsProperty<CupertinoDynamicColor?>(
            "color",
            _dynamicColor,
            defaultValue: DiagnosticsDefaults.NullValue));
    }
}
