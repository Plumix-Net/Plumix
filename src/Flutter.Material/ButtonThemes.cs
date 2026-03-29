using Flutter.Foundation;
using Flutter.Widgets;

namespace Flutter.Material;

// Dart parity source (reference): flutter/packages/flutter/lib/src/material/text_button_theme.dart; flutter/packages/flutter/lib/src/material/elevated_button_theme.dart; flutter/packages/flutter/lib/src/material/outlined_button_theme.dart; flutter/packages/flutter/lib/src/material/filled_button_theme.dart (approximate)

public sealed record TextButtonThemeData
{
    public TextButtonThemeData(ButtonStyle? style = null)
    {
        Style = style;
    }

    public ButtonStyle? Style { get; init; }
}

public sealed record ElevatedButtonThemeData
{
    public ElevatedButtonThemeData(ButtonStyle? style = null)
    {
        Style = style;
    }

    public ButtonStyle? Style { get; init; }
}

public sealed record OutlinedButtonThemeData
{
    public OutlinedButtonThemeData(ButtonStyle? style = null)
    {
        Style = style;
    }

    public ButtonStyle? Style { get; init; }
}

public sealed record FilledButtonThemeData
{
    public FilledButtonThemeData(ButtonStyle? style = null)
    {
        Style = style;
    }

    public ButtonStyle? Style { get; init; }
}

public sealed class TextButtonTheme : InheritedWidget
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

    public override Widget Build(BuildContext context)
    {
        return Child;
    }

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((TextButtonTheme)oldWidget).Data, Data);
    }

    public static TextButtonThemeData Of(BuildContext context)
    {
        var localTheme = context.DependOnInherited<TextButtonTheme>();
        if (localTheme is not null)
        {
            return localTheme.Data;
        }

        return Theme.Of(context).TextButtonTheme;
    }
}

public sealed class ElevatedButtonTheme : InheritedWidget
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

    public override Widget Build(BuildContext context)
    {
        return Child;
    }

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((ElevatedButtonTheme)oldWidget).Data, Data);
    }

    public static ElevatedButtonThemeData Of(BuildContext context)
    {
        var localTheme = context.DependOnInherited<ElevatedButtonTheme>();
        if (localTheme is not null)
        {
            return localTheme.Data;
        }

        return Theme.Of(context).ElevatedButtonTheme;
    }
}

public sealed class OutlinedButtonTheme : InheritedWidget
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

    public override Widget Build(BuildContext context)
    {
        return Child;
    }

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((OutlinedButtonTheme)oldWidget).Data, Data);
    }

    public static OutlinedButtonThemeData Of(BuildContext context)
    {
        var localTheme = context.DependOnInherited<OutlinedButtonTheme>();
        if (localTheme is not null)
        {
            return localTheme.Data;
        }

        return Theme.Of(context).OutlinedButtonTheme;
    }
}

public sealed class FilledButtonTheme : InheritedWidget
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

    public override Widget Build(BuildContext context)
    {
        return Child;
    }

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((FilledButtonTheme)oldWidget).Data, Data);
    }

    public static FilledButtonThemeData Of(BuildContext context)
    {
        var localTheme = context.DependOnInherited<FilledButtonTheme>();
        if (localTheme is not null)
        {
            return localTheme.Data;
        }

        return Theme.Of(context).FilledButtonTheme;
    }
}
