using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/outlined_button_theme.dart

/// <summary>Dart parity: `OutlinedButtonThemeData`.</summary>
public sealed record OutlinedButtonThemeData : IDiagnosticable
{
    public OutlinedButtonThemeData(ButtonStyle? style = null)
    {
        Style = style;
    }

    public ButtonStyle? Style { get; init; }

    public static OutlinedButtonThemeData? Lerp(OutlinedButtonThemeData? a, OutlinedButtonThemeData? b, double t)
    {
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        return new OutlinedButtonThemeData(ButtonStyle.Lerp(a?.Style, b?.Style, t));
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

/// <summary>Dart parity: `OutlinedButtonTheme`.</summary>
public sealed class OutlinedButtonTheme : InheritedTheme
{
    public OutlinedButtonTheme(
        OutlinedButtonThemeData data,
        Widget child,
        Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public OutlinedButtonThemeData Data { get; }

    public Widget Child { get; }

    public static OutlinedButtonThemeData Of(BuildContext context)
    {
        var localTheme = context.DependOnInherited<OutlinedButtonTheme>();
        return localTheme is not null ? localTheme.Data : Theme.Of(context).OutlinedButtonTheme;
    }

    public override Widget Build(BuildContext context)
    {
        return Child;
    }

    public override Widget Wrap(BuildContext context, Widget child)
    {
        return new OutlinedButtonTheme(Data, child);
    }

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((OutlinedButtonTheme)oldWidget).Data, Data);
    }
}
