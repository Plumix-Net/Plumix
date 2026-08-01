using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source (reference): flutter/packages/flutter/lib/src/material/text_button_theme.dart;
// flutter/packages/flutter/lib/src/material/elevated_button_theme.dart;
// flutter/packages/flutter/lib/src/material/outlined_button_theme.dart;
// flutter/packages/flutter/lib/src/material/filled_button_theme.dart;
// flutter/packages/flutter/lib/src/material/icon_button_theme.dart

public sealed partial record TextButtonThemeData
{
    public TextButtonThemeData(ButtonStyle? style = null)
    {
        Style = style;
    }

    public ButtonStyle? Style { get; init; }
}

public sealed partial record ElevatedButtonThemeData
{
    public ElevatedButtonThemeData(ButtonStyle? style = null)
    {
        Style = style;
    }

    public ButtonStyle? Style { get; init; }
}

public sealed partial record OutlinedButtonThemeData
{
    public OutlinedButtonThemeData(ButtonStyle? style = null)
    {
        Style = style;
    }

    public ButtonStyle? Style { get; init; }
}

public sealed partial record FilledButtonThemeData
{
    public FilledButtonThemeData(ButtonStyle? style = null)
    {
        Style = style;
    }

    public ButtonStyle? Style { get; init; }
}

public sealed record IconButtonThemeData
{
    public IconButtonThemeData(ButtonStyle? style = null)
    {
        Style = style;
    }

    public ButtonStyle? Style { get; init; }

    public IconButtonThemeData CopyWith(ButtonStyle? style = null)
    {
        return new IconButtonThemeData(style ?? Style);
    }

    public static IconButtonThemeData? Lerp(
        IconButtonThemeData? a,
        IconButtonThemeData? b,
        double t)
    {
        if (ReferenceEquals(a, b))
        {
            return a;
        }
        if (a is null && b is null)
        {
            return null;
        }

        return new IconButtonThemeData(ButtonStyle.Lerp(a?.Style, b?.Style, t));
    }
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

    public static IconButtonThemeData Of(BuildContext context)
    {
        var localTheme = context.DependOnInherited<IconButtonTheme>();
        if (localTheme is not null)
        {
            return localTheme.Data;
        }

        return Theme.Of(context).IconButtonTheme;
    }
}
