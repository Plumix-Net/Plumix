using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/text_selection_theme.dart

public sealed record TextSelectionThemeData(
    Color? CursorColor = null,
    Color? SelectionColor = null,
    Color? SelectionHandleColor = null);

public sealed class TextSelectionTheme : InheritedWidget
{
    public TextSelectionTheme(TextSelectionThemeData data, Widget child, Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public TextSelectionThemeData Data { get; }
    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((TextSelectionTheme)oldWidget).Data, Data);
    }

    public static TextSelectionThemeData Of(BuildContext context)
    {
        return context.DependOnInherited<TextSelectionTheme>()?.Data ?? Theme.Of(context).TextSelectionTheme;
    }
}
