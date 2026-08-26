using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/text_button_theme.dart

/// <summary>Dart parity: `TextButtonThemeData`.</summary>
public sealed record TextButtonThemeData : IDiagnosticable
{
    public TextButtonThemeData(ButtonStyle? style = null)
    {
        Style = style;
    }

    public ButtonStyle? Style { get; init; }

    public static TextButtonThemeData? Lerp(TextButtonThemeData? a, TextButtonThemeData? b, double t)
    {
        if (ReferenceEquals(a, b))
        {
            return a;
        }

        return new TextButtonThemeData(ButtonStyle.Lerp(a?.Style, b?.Style, t));
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

/// <summary>Dart parity: `TextButtonTheme`.</summary>
public sealed class TextButtonTheme : InheritedTheme
{
    public TextButtonTheme(
        TextButtonThemeData data,
        Widget child,
        Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public TextButtonThemeData Data { get; }

    public Widget Child { get; }

    public static TextButtonThemeData Of(BuildContext context)
    {
        var localTheme = context.DependOnInherited<TextButtonTheme>();
        return localTheme is not null ? localTheme.Data : Theme.Of(context).TextButtonTheme;
    }

    public override Widget Build(BuildContext context)
    {
        return Child;
    }

    public override Widget Wrap(BuildContext context, Widget child)
    {
        return new TextButtonTheme(Data, child);
    }

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((TextButtonTheme)oldWidget).Data, Data);
    }
}
