using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.Widgets;

namespace Plumix.Cupertino;

// Dart parity source: cupertino_ui/lib/src/desktop_text_selection_toolbar.dart

/// <summary>A macOS-style desktop text-selection toolbar.</summary>
public sealed class CupertinoDesktopTextSelectionToolbar : StatelessWidget
{
    public const double ToolbarScreenPadding = 8.0;
    public const double ToolbarWidth = 222.0;

    public CupertinoDesktopTextSelectionToolbar(
        Point anchor,
        IReadOnlyList<Widget> children,
        Key? key = null) : base(key)
    {
        ArgumentNullException.ThrowIfNull(children);
        if (children.Count == 0)
        {
            throw new ArgumentException("Toolbar children must not be empty.", nameof(children));
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
        bool dark = CupertinoTheme.BrightnessOf(context) == PlatformBrightness.Dark;
        Color background = dark
            ? Color.Parse("#B2303030")
            : Color.Parse("#B2FFFFFF");
        Color borderColor = dark
            ? Color.Parse("#FF5B5B5B")
            : Color.Parse("#FFB8B8B8");
        var shadow = new Plumix.Rendering.BoxShadow(
            color: Color.FromArgb(60, 0, 0, 0),
            offset: new Point(0.0, 4.0),
            blurRadius: 10.0,
            spreadRadius: 0.5);
        var shape = new ContinuousRectangleBorder(
            borderRadius: BorderRadius.Circular(8.0));
        var borderedShape = new ContinuousRectangleBorder(
            side: new BorderSide(borderColor),
            borderRadius: BorderRadius.Circular(8.0));

        Widget contents = new DecoratedBox(
            decoration: new ShapeDecoration(
                Shape: borderedShape,
                Color: background),
            child: new Padding(
                new Thickness(6.0),
                new Column(
                    mainAxisSize: MainAxisSize.Min,
                    children: Children)));
        contents = new BackdropFilter(
            filter: new ImageFilter.Compose(
                new ImageFilter.ColorMatrix(SaturationMatrix(3.0)),
                new ImageFilter.Blur(20.0, 20.0)),
            child: contents);
        contents = new ClipRRect(
            borderRadius: BorderRadius.Circular(8.0),
            child: contents);
        Widget toolbar = new Container(
            width: ToolbarWidth,
            decoration: new ShapeDecoration(
                Shape: shape,
                Shadows: [shadow]),
            child: contents);

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

    internal static IReadOnlyList<double> SaturationMatrix(double saturation)
    {
        double r = 0.213 * (1.0 - saturation);
        double g = 0.715 * (1.0 - saturation);
        double b = 0.072 * (1.0 - saturation);
        return
        [
            r + saturation, g, b, 0.0, 0.0,
            r, g + saturation, b, 0.0, 0.0,
            r, g, b + saturation, 0.0, 0.0,
            0.0, 0.0, 0.0, 1.0, 0.0,
        ];
    }
}
