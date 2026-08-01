using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/divider_theme.dart

public sealed partial record DividerThemeData(
    Color? Color = null,
    double? Space = null,
    double? Thickness = null,
    double? Indent = null,
    double? EndIndent = null,
    BorderRadiusGeometry? Radius = null)
{
    public DividerThemeData CopyWith(
        Color? color = null,
        double? space = null,
        double? thickness = null,
        double? indent = null,
        double? endIndent = null,
        BorderRadiusGeometry? radius = null)
    {
        return new DividerThemeData(
            Color: color ?? Color,
            Space: space ?? Space,
            Thickness: thickness ?? Thickness,
            Indent: indent ?? Indent,
            EndIndent: endIndent ?? EndIndent,
            Radius: radius ?? Radius);
    }
}

public sealed class DividerTheme : InheritedTheme
{
    public DividerTheme(
        DividerThemeData data,
        Widget child,
        Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public DividerThemeData Data { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context)
    {
        return Child;
    }

    public override Widget Wrap(BuildContext context, Widget child)
    {
        return new DividerTheme(Data, child);
    }

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((DividerTheme)oldWidget).Data, Data);
    }

    public static DividerThemeData Of(BuildContext context)
    {
        var localTheme = context.DependOnInherited<DividerTheme>();
        if (localTheme is not null)
        {
            return localTheme.Data;
        }

        return Theme.Of(context).DividerTheme;
    }
}
