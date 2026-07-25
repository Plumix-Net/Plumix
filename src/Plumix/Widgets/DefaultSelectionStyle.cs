using Avalonia.Media;
using Plumix.Foundation;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/default_selection_style.dart

public sealed class DefaultSelectionStyle : InheritedTheme
{
    public DefaultSelectionStyle(
        Widget child,
        Color? cursorColor = null,
        Color? selectionColor = null,
        MouseCursor? mouseCursor = null,
        Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
        CursorColor = cursorColor;
        SelectionColor = selectionColor;
        MouseCursor = mouseCursor;
    }

    private DefaultSelectionStyle(Key? key = null) : base(key)
    {
        Child = new FallbackNullWidget();
    }

    public static DefaultSelectionStyle Fallback(Key? key = null)
    {
        return new DefaultSelectionStyle(key);
    }

    public static Color DefaultColor { get; } = Color.FromArgb(0x80, 0x80, 0x80, 0x80);

    public Widget Child { get; }

    public Color? CursorColor { get; }

    public Color? SelectionColor { get; }

    public MouseCursor? MouseCursor { get; }

    public static Widget Merge(
        Widget child,
        Color? cursorColor = null,
        Color? selectionColor = null,
        MouseCursor? mouseCursor = null,
        Key? key = null)
    {
        ArgumentNullException.ThrowIfNull(child);
        return new Builder(context =>
        {
            DefaultSelectionStyle parent = Of(context);
            return new DefaultSelectionStyle(
                child: child,
                cursorColor: cursorColor ?? parent.CursorColor,
                selectionColor: selectionColor ?? parent.SelectionColor,
                mouseCursor: mouseCursor ?? parent.MouseCursor,
                key: key);
        });
    }

    public static DefaultSelectionStyle Of(BuildContext context)
    {
        return context.DependOnInherited<DefaultSelectionStyle>() ?? Fallback();
    }

    public override Widget Build(BuildContext context)
    {
        return Child;
    }

    public override Widget Wrap(BuildContext context, Widget child)
    {
        return new DefaultSelectionStyle(
            child: child,
            cursorColor: CursorColor,
            selectionColor: SelectionColor,
            mouseCursor: MouseCursor);
    }

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        var oldStyle = (DefaultSelectionStyle)oldWidget;
        return oldStyle.CursorColor != CursorColor
               || oldStyle.SelectionColor != SelectionColor
               || !Equals(oldStyle.MouseCursor, MouseCursor);
    }

    private sealed class FallbackNullWidget : StatelessWidget
    {
        public override Widget Build(BuildContext context)
        {
            throw new InvalidOperationException(
                "A fallback DefaultSelectionStyle cannot be incorporated into the widget tree.");
        }
    }
}
