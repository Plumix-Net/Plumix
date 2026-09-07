using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/text_selection_theme.dart

public sealed partial record TextSelectionThemeData(
    Color? CursorColor = null,
    Color? SelectionColor = null,
    Color? SelectionHandleColor = null) : IDiagnosticable
{
    public void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        object nullDefault = DiagnosticsDefaults.NullValue;
        properties.Add(new ColorProperty("cursorColor", CursorColor, defaultValue: nullDefault));
        properties.Add(new ColorProperty("selectionColor", SelectionColor, defaultValue: nullDefault));
        properties.Add(new ColorProperty(
            "selectionHandleColor",
            SelectionHandleColor,
            defaultValue: nullDefault));
    }

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
    public TextSelectionTheme(TextSelectionThemeData data, Widget child, Key? key = null) : base(child, key)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        _child = child ?? throw new ArgumentNullException(nameof(child));
    }

    private readonly Widget _child;

    public TextSelectionThemeData Data { get; }

    // Dart overrides the `child` getter to insert `DefaultSelectionStyle` into the subtree without
    // breaking the public API. It relies on an implementation detail of `ProxyWidget`, and is only
    // done here because `TextSelectionTheme` is const in Dart.
    public override Widget Child => new DefaultSelectionStyle(
        child: _child,
        cursorColor: Data.CursorColor,
        selectionColor: Data.SelectionColor);

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
