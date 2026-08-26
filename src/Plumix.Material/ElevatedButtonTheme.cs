using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/elevated_button_theme.dart

/// <summary>Dart parity: `ElevatedButtonThemeData`.</summary>
public sealed record ElevatedButtonThemeData : IDiagnosticable
{
    public ElevatedButtonThemeData(ButtonStyle? style = null)
    {
        Style = style;
    }

    public ButtonStyle? Style { get; init; }

    public static ElevatedButtonThemeData? Lerp(ElevatedButtonThemeData? a, ElevatedButtonThemeData? b, double t)
    {
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        return new ElevatedButtonThemeData(ButtonStyle.Lerp(a?.Style, b?.Style, t));
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

/// <summary>Dart parity: `ElevatedButtonTheme`.</summary>
public sealed class ElevatedButtonTheme : InheritedTheme
{
    public ElevatedButtonTheme(
        ElevatedButtonThemeData data,
        Widget child,
        Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public ElevatedButtonThemeData Data { get; }

    public Widget Child { get; }

    public static ElevatedButtonThemeData Of(BuildContext context)
    {
        var localTheme = context.DependOnInherited<ElevatedButtonTheme>();
        return localTheme is not null ? localTheme.Data : Theme.Of(context).ElevatedButtonTheme;
    }

    public override Widget Build(BuildContext context)
    {
        return Child;
    }

    public override Widget Wrap(BuildContext context, Widget child)
    {
        return new ElevatedButtonTheme(Data, child);
    }

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((ElevatedButtonTheme)oldWidget).Data, Data);
    }
}
