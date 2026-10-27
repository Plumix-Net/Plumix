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
            EdgeInsets.FromLTRB(
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
    private static readonly EdgeInsets ToolbarButtonPadding = EdgeInsets.FromLTRB(20.0, 0.0, 20.0, 3.0);

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
        return new DesktopTextSelectionToolbarButton(
            onPressed: onPressed,
            child: new Text(
                text,
                overflow: TextOverflow.Ellipsis,
                style: ToolbarButtonFontStyle.CopyWith(color: ForegroundColor(Theme.Of(context)))),
            key: key);
    }

    public override Widget Build(BuildContext context)
    {
        // TODO(hansmuller): Should be colorScheme.onSurface
        Color foregroundColor = ForegroundColor(Theme.Of(context));

        return new SizedBox(
            width: double.PositiveInfinity,
            child: new TextButton(
                style: TextButton.StyleFrom(
                    alignment: Alignment.CenterLeft,
                    enabledMouseCursor: SystemMouseCursors.Basic,
                    disabledMouseCursor: SystemMouseCursors.Basic,
                    foregroundColor: foregroundColor,
                    shape: new RoundedRectangleBorder(),
                    minimumSize: new Size(WidgetConstants.MinInteractiveDimension, 36.0),
                    padding: ToolbarButtonPadding),
                onPressed: OnPressed,
                child: Child));
    }

    private static readonly TextStyle ToolbarButtonFontStyle = new(
        Inherit: false,
        FontSize: 14.0,
        LetterSpacing: -0.15,
        FontWeight: FontWeight.Normal);

    private static Color ForegroundColor(ThemeData theme) =>
        theme.ColorScheme.Brightness == Brightness.Dark ? Colors.White : Colors.Black87;
}
