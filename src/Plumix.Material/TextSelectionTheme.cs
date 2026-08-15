using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/text_selection_theme.dart

public sealed partial record TextSelectionThemeData(
    Color? CursorColor = null,
    Color? SelectionColor = null,
    Color? SelectionHandleColor = null)
{
    public TextSelectionThemeData CopyWith(
        Color? cursorColor = null,
        Color? selectionColor = null,
        Color? selectionHandleColor = null)
    {
        return new TextSelectionThemeData(
            CursorColor: cursorColor ?? CursorColor,
            SelectionColor: selectionColor ?? SelectionColor,
            SelectionHandleColor: selectionHandleColor ?? SelectionHandleColor);
    }
}

public sealed class TextSelectionTheme : InheritedTheme
{
    public TextSelectionTheme(TextSelectionThemeData data, Widget child, Key? key = null) : base(key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public TextSelectionThemeData Data { get; }
    public Widget Child { get; }

    public override Widget Build(BuildContext context)
    {
        return new DefaultSelectionStyle(
            child: Child,
            cursorColor: Data.CursorColor,
            selectionColor: Data.SelectionColor);
    }

    public override Widget Wrap(BuildContext context, Widget child)
    {
        return new TextSelectionTheme(Data, child);
    }

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(((TextSelectionTheme)oldWidget).Data, Data);
    }

    public static TextSelectionThemeData Of(BuildContext context)
    {
        return context.DependOnInherited<TextSelectionTheme>()?.Data ?? Theme.Of(context).TextSelectionTheme;
    }
}
