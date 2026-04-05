using Avalonia.Media;
using Flutter.Foundation;
using Flutter.Rendering;
using Flutter.Widgets;

namespace Flutter.Material;

// Dart parity source (reference): flutter/packages/flutter/lib/src/material/radio_theme.dart (approximate)

public sealed record RadioThemeData(
    MaterialStateProperty<Color?>? FillColor = null,
    MaterialStateProperty<Color?>? OverlayColor = null,
    MaterialTapTargetSize? MaterialTapTargetSize = null,
    double? SplashRadius = null,
    MaterialStateProperty<Color?>? BackgroundColor = null,
    BorderSide? Side = null,
    MaterialStateProperty<double?>? InnerRadius = null);

public sealed class RadioTheme : InheritedWidget
{
    public RadioTheme(
        RadioThemeData data,
        Widget child,
        Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public RadioThemeData Data { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context)
    {
        return Child;
    }

    protected internal override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((RadioTheme)oldWidget).Data, Data);
    }

    public static RadioThemeData Of(BuildContext context)
    {
        var localTheme = context.DependOnInherited<RadioTheme>();
        if (localTheme is not null)
        {
            return localTheme.Data;
        }

        return Theme.Of(context).RadioTheme;
    }
}
