using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source (reference): flutter/packages/flutter/lib/src/material/card_theme.dart (approximate)

public sealed record CardThemeData
{
    public CardThemeData(
        Clip? ClipBehavior = null,
        Color? Color = null,
        Color? ShadowColor = null,
        Color? SurfaceTintColor = null,
        double? Elevation = null,
        Thickness? Margin = null,
        ShapeBorder? Shape = null)
    {
        if (Elevation.HasValue
            && (double.IsNaN(Elevation.Value)
                || double.IsInfinity(Elevation.Value)
                || Elevation.Value < 0))
        {
            throw new ArgumentOutOfRangeException(nameof(Elevation), "Card theme elevation must be non-negative and finite.");
        }

        this.ClipBehavior = ClipBehavior;
        this.Color = Color;
        this.ShadowColor = ShadowColor;
        this.SurfaceTintColor = SurfaceTintColor;
        this.Elevation = Elevation;
        this.Margin = Margin;
        this.Shape = Shape;
    }

    public Clip? ClipBehavior { get; init; }

    public Color? Color { get; init; }

    public Color? ShadowColor { get; init; }

    public Color? SurfaceTintColor { get; init; }

    public double? Elevation { get; init; }

    public Thickness? Margin { get; init; }

    public ShapeBorder? Shape { get; init; }
}

public sealed class CardTheme : InheritedWidget
{
    public CardTheme(
        CardThemeData data,
        Widget child,
        Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public CardThemeData Data { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context)
    {
        return Child;
    }

    protected internal override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((CardTheme)oldWidget).Data, Data);
    }

    public static CardThemeData Of(BuildContext context)
    {
        var localTheme = context.DependOnInherited<CardTheme>();
        if (localTheme is not null)
        {
            return localTheme.Data;
        }

        return Theme.Of(context).CardTheme;
    }
}
