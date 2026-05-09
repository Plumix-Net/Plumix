using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source (reference): flutter/packages/flutter/lib/src/material/slider_theme.dart (approximate)

public sealed record SliderThemeData(
    Color? ActiveTrackColor = null,
    Color? InactiveTrackColor = null,
    Color? DisabledActiveTrackColor = null,
    Color? DisabledInactiveTrackColor = null,
    Color? ThumbColor = null,
    Color? DisabledThumbColor = null,
    MaterialStateProperty<Color?>? OverlayColor = null,
    double? TrackHeight = null,
    double? ThumbRadius = null,
    MaterialTapTargetSize? MaterialTapTargetSize = null);

public sealed class SliderTheme : InheritedWidget
{
    public SliderTheme(
        SliderThemeData data,
        Widget child,
        Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public SliderThemeData Data { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context)
    {
        return Child;
    }

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((SliderTheme)oldWidget).Data, Data);
    }

    public static SliderThemeData Of(BuildContext context)
    {
        var localTheme = context.DependOnInherited<SliderTheme>();
        if (localTheme is not null)
        {
            return localTheme.Data;
        }

        return Theme.Of(context).SliderTheme;
    }
}
