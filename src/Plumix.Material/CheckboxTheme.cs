using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source (reference): flutter/packages/flutter/lib/src/material/checkbox_theme.dart (approximate)

public sealed record CheckboxThemeData(
    MaterialStateProperty<Color?>? FillColor = null,
    MaterialStateProperty<Color?>? CheckColor = null,
    MaterialStateProperty<Color?>? OverlayColor = null,
    MaterialStateProperty<BorderSide?>? Side = null,
    MaterialTapTargetSize? MaterialTapTargetSize = null,
    BorderRadius? Shape = null,
    double? SplashRadius = null);

public sealed class CheckboxTheme : InheritedWidget
{
    public CheckboxTheme(
        CheckboxThemeData data,
        Widget child,
        Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public CheckboxThemeData Data { get; }

    public Widget Child { get; }

    public override Widget Build(BuildContext context)
    {
        return Child;
    }

    protected internal override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((CheckboxTheme)oldWidget).Data, Data);
    }

    public static CheckboxThemeData Of(BuildContext context)
    {
        var localTheme = context.DependOnInherited<CheckboxTheme>();
        if (localTheme is not null)
        {
            return localTheme.Data;
        }

        return Theme.Of(context).CheckboxTheme;
    }
}
