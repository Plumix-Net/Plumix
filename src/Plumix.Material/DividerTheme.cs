using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source (reference): flutter/packages/flutter/lib/src/material/divider_theme.dart (approximate)

public sealed record DividerThemeData(
    Color? Color = null,
    double? Space = null,
    double? Thickness = null,
    double? Indent = null,
    double? EndIndent = null,
    BorderRadius? Radius = null);

public sealed class DividerTheme : InheritedWidget
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
