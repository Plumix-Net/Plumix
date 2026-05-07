using Avalonia.Media;
using Plumix;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source (reference): flutter/packages/flutter/lib/src/material/progress_indicator_theme.dart (baseline subset)

public sealed record ProgressIndicatorThemeData(
    Color? Color = null,
    Color? LinearTrackColor = null,
    double? LinearMinHeight = null,
    BorderRadius? BorderRadius = null);

public sealed class ProgressIndicatorTheme : InheritedWidget
{
    public ProgressIndicatorTheme(
        ProgressIndicatorThemeData data,
        Widget child,
        Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public ProgressIndicatorThemeData Data { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context)
    {
        return Child;
    }

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((ProgressIndicatorTheme)oldWidget).Data, Data);
    }

    public static ProgressIndicatorThemeData Of(BuildContext context)
    {
        var localTheme = context.DependOnInherited<ProgressIndicatorTheme>();
        if (localTheme is not null)
        {
            return localTheme.Data;
        }

        return Theme.Of(context).ProgressIndicatorTheme;
    }
}
