using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/filled_button_theme.dart

/// <summary>Dart parity: `FilledButtonThemeData`.</summary>
public sealed record FilledButtonThemeData : IDiagnosticable
{
    public FilledButtonThemeData(ButtonStyle? style = null)
    {
        Style = style;
    }

    public ButtonStyle? Style { get; init; }

    public static FilledButtonThemeData? Lerp(FilledButtonThemeData? a, FilledButtonThemeData? b, double t)
    {
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        return new FilledButtonThemeData(ButtonStyle.Lerp(a?.Style, b?.Style, t));
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

/// <summary>Dart parity: `FilledButtonTheme`.</summary>
public sealed class FilledButtonTheme : InheritedTheme
{
    public FilledButtonTheme(
        FilledButtonThemeData data,
        Widget child,
        Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public FilledButtonThemeData Data { get; }

    public Widget Child { get; }

    public static FilledButtonThemeData Of(BuildContext context)
    {
        var localTheme = context.DependOnInherited<FilledButtonTheme>();
        return localTheme is not null ? localTheme.Data : Theme.Of(context).FilledButtonTheme;
    }

    public override Widget Build(BuildContext context)
    {
        return Child;
    }

    public override Widget Wrap(BuildContext context, Widget child)
    {
        return new FilledButtonTheme(Data, child);
    }

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((FilledButtonTheme)oldWidget).Data, Data);
    }
}
