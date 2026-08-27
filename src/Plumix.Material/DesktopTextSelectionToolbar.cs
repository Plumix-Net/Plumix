using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity sources:
// - material_ui/lib/src/desktop_text_selection_toolbar.dart
// - material_ui/lib/src/desktop_text_selection_toolbar_button.dart
// - flutter/packages/flutter/lib/src/widgets/desktop_text_selection_toolbar_layout_delegate.dart

/// <summary>A Material-style desktop text selection toolbar.</summary>
public sealed class DesktopTextSelectionToolbar : StatelessWidget
{
    internal const double ToolbarScreenPadding = 8.0;
    internal const double ToolbarWidth = 222.0;

    public DesktopTextSelectionToolbar(
        Point anchor,
        IReadOnlyList<Widget> children,
        Key? key = null) : base(key)
    {
        ArgumentNullException.ThrowIfNull(children);
        if (children.Count == 0)
        {
            throw new ArgumentException("DesktopTextSelectionToolbar children must not be empty.", nameof(children));
        }

        Anchor = anchor;
        Children = children;
    }

    public Point Anchor { get; }

    public IReadOnlyList<Widget> Children { get; }

    public override Widget Build(BuildContext context)
    {
        double paddingAbove = MediaQuery.PaddingOf(context).Top + ToolbarScreenPadding;
        var localAdjustment = new Vector(ToolbarScreenPadding, paddingAbove);
        var localAnchor = Anchor - localAdjustment;

        Widget toolbar = new SizedBox(
            width: ToolbarWidth,
            child: new Material(
                borderRadius: BorderRadius.Circular(7.0),
                clipBehavior: Clip.AntiAlias,
                elevation: 1.0,
                type: MaterialType.Card,
                child: new Column(
                    mainAxisSize: MainAxisSize.Min,
                    children: Children)));

        return new Padding(
            new Thickness(
                ToolbarScreenPadding,
                paddingAbove,
                ToolbarScreenPadding,
                ToolbarScreenPadding),
            new CustomSingleChildLayout(
                new DesktopTextSelectionToolbarLayoutDelegate(localAnchor),
                toolbar));
    }
}

/// <summary>A full-width text button styled for a Material desktop selection toolbar.</summary>
public sealed class DesktopTextSelectionToolbarButton : StatelessWidget
{
    private static readonly Thickness ToolbarButtonPadding = new(20.0, 0.0, 20.0, 3.0);

    public DesktopTextSelectionToolbarButton(
        Action? onPressed,
        Widget child,
        Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
        OnPressed = onPressed;
    }

    public Action? OnPressed { get; }

    public Widget Child { get; }

    public static DesktopTextSelectionToolbarButton Text(
        BuildContext context,
        Action? onPressed,
        string text,
        Key? key = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        Color color = ResolveForegroundColor(Theme.Of(context));
        return new DesktopTextSelectionToolbarButton(
            onPressed: onPressed,
            child: new Text(
                text,
                fontSize: 14.0,
                color: color,
                fontWeight: FontWeight.Normal,
                letterSpacing: -0.15,
                overflow: TextOverflow.Ellipsis),
            key: key);
    }

    public override Widget Build(BuildContext context)
    {
        Color foregroundColor = ResolveForegroundColor(Theme.Of(context));
        ButtonStyle style = TextButton.StyleFrom(
            alignment: Alignment.CenterLeft,
            foregroundColor: foregroundColor,
            shape: new RoundedRectangleBorder(borderRadius: BorderRadius.Zero),
            minimumSize: new Size(48.0, 36.0),
            padding: ToolbarButtonPadding) with
        {
            MouseCursor = MaterialStateProperty<MouseCursor?>.All(SystemMouseCursors.Basic),
        };

        return new SizedBox(
            width: double.PositiveInfinity,
            child: new TextButton(
                child: Child,
                onPressed: OnPressed,
                style: style));
    }

    private static Color ResolveForegroundColor(ThemeData theme)
    {
        Color defaultOnSurface = theme.Brightness == Brightness.Dark
            ? ThemeData.Dark.ColorScheme.OnSurface
            : ThemeData.Light.ColorScheme.OnSurface;
        if (theme.ColorScheme.OnSurface != defaultOnSurface)
        {
            return theme.ColorScheme.OnSurface;
        }

        return theme.Brightness == Brightness.Dark
            ? Colors.White
            : Color.FromArgb(0xDE, 0x00, 0x00, 0x00);
    }
}
