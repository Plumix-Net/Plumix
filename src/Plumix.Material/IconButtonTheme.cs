using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/icon_button_theme.dart

/// <summary>Dart parity: `IconButtonThemeData`.</summary>
    /// <remarks>
    /// `IconButton` reads this theme only when `ThemeData.useMaterial3` is true; under Material 2
    /// it takes its colours from the ambient `IconTheme` instead.
    /// </remarks>

public sealed record IconButtonThemeData : IDiagnosticable
{
    public IconButtonThemeData(ButtonStyle? style = null)
    {
        Style = style;
    }

    public ButtonStyle? Style { get; init; }

    public static IconButtonThemeData? Lerp(IconButtonThemeData? a, IconButtonThemeData? b, double t)
    {
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        return new IconButtonThemeData(ButtonStyle.Lerp(a?.Style, b?.Style, t));
    }

    public void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        properties.Add(new DiagnosticsProperty<ButtonStyle?>(
            "style",
            Style,
            defaultValue: DiagnosticsDefaults.NullValue));
    }
}

/// <summary>Dart parity: `IconButtonTheme`.</summary>
public sealed class IconButtonTheme : InheritedTheme
{
    public IconButtonTheme(
        IconButtonThemeData data,
        Widget child,
        Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public IconButtonThemeData Data { get; }

    public Widget Child { get; }

    public static IconButtonThemeData Of(BuildContext context)
    {
        var localTheme = context.DependOnInherited<IconButtonTheme>();
        return localTheme is not null ? localTheme.Data : Theme.Of(context).IconButtonTheme;
    }

    public override Widget Build(BuildContext context)
    {
        return Child;
    }

    public override Widget Wrap(BuildContext context, Widget child)
    {
        return new IconButtonTheme(Data, child);
    }

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((IconButtonTheme)oldWidget).Data, Data);
    }
}
