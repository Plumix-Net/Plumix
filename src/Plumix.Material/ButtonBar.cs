using Avalonia;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/button_bar.dart

public sealed class ButtonBar : StatelessWidget
{
    public ButtonBar(
        MainAxisAlignment? alignment = null,
        MainAxisSize? mainAxisSize = null,
        ButtonTextTheme? buttonTextTheme = null,
        double? buttonMinWidth = null,
        double? buttonHeight = null,
        EdgeInsetsGeometry? buttonPadding = null,
        bool? buttonAlignedDropdown = null,
        ButtonBarLayoutBehavior? layoutBehavior = null,
        VerticalDirection? overflowDirection = null,
        double? overflowButtonSpacing = null,
        IReadOnlyList<Widget>? children = null,
        Key? key = null) : base(key)
    {
        ValidateNonNegative(nameof(buttonMinWidth), buttonMinWidth);
        ValidateNonNegative(nameof(buttonHeight), buttonHeight);
        ValidateNonNegative(nameof(overflowButtonSpacing), overflowButtonSpacing);
        Alignment = alignment;
        MainAxisSize = mainAxisSize;
        ButtonTextTheme = buttonTextTheme;
        ButtonMinWidth = buttonMinWidth;
        ButtonHeight = buttonHeight;
        ButtonPadding = buttonPadding;
        ButtonAlignedDropdown = buttonAlignedDropdown;
        LayoutBehavior = layoutBehavior;
        OverflowDirection = overflowDirection;
        OverflowButtonSpacing = overflowButtonSpacing;
        Children = children ?? [];
    }

    public MainAxisAlignment? Alignment { get; }

    public MainAxisSize? MainAxisSize { get; }

    public ButtonTextTheme? ButtonTextTheme { get; }

    public double? ButtonMinWidth { get; }

    public double? ButtonHeight { get; }

    public EdgeInsetsGeometry? ButtonPadding { get; }

    public bool? ButtonAlignedDropdown { get; }

    public ButtonBarLayoutBehavior? LayoutBehavior { get; }

    public VerticalDirection? OverflowDirection { get; }

    public double? OverflowButtonSpacing { get; }

    public IReadOnlyList<Widget> Children { get; }

    public override Widget Build(BuildContext context)
    {
        var parentButtonTheme = ButtonTheme.Of(context);
        var barTheme = ButtonBarTheme.Of(context);
        EdgeInsetsGeometry effectivePadding = ButtonPadding
                                               ?? barTheme.ButtonPadding
                                               ?? EdgeInsetsGeometry.Symmetric(horizontal: 8);
        var buttonTheme = parentButtonTheme with
        {
            TextTheme = ButtonTextTheme ?? barTheme.ButtonTextTheme ?? global::Plumix.Material.ButtonTextTheme.Primary,
            MinWidth = ButtonMinWidth ?? barTheme.ButtonMinWidth ?? 64,
            Height = ButtonHeight ?? barTheme.ButtonHeight ?? 36,
            Padding = effectivePadding,
            AlignedDropdown = ButtonAlignedDropdown ?? barTheme.ButtonAlignedDropdown ?? false,
            LayoutBehavior = LayoutBehavior ?? barTheme.LayoutBehavior ?? ButtonBarLayoutBehavior.Padded,
        };
        double paddingUnit = buttonTheme.EffectivePadding.Horizontal / 4.0;
        List<Widget> paddedChildren = Children
            .Select(child => (Widget)new Padding(
                EdgeInsetsGeometry.Symmetric(horizontal: paddingUnit),
                child))
            .ToList();

        Widget child = new ButtonTheme(
            data: buttonTheme,
            child: new ButtonBarRow(
                children: paddedChildren,
                mainAxisAlignment: Alignment ?? barTheme.Alignment ?? MainAxisAlignment.End,
                mainAxisSize: MainAxisSize ?? barTheme.MainAxisSize ?? global::Plumix.Rendering.MainAxisSize.Max,
                overflowDirection: OverflowDirection ?? barTheme.OverflowDirection ?? VerticalDirection.Down,
                overflowButtonSpacing: OverflowButtonSpacing));

        return buttonTheme.LayoutBehavior switch
        {
            ButtonBarLayoutBehavior.Padded => new Padding(
                EdgeInsetsGeometry.Symmetric(
                    vertical: 2.0 * paddingUnit,
                    horizontal: paddingUnit),
                child),
            ButtonBarLayoutBehavior.Constrained => new ConstrainedBox(
                new BoxConstraints(MinHeight: 52),
                new Padding(
                    EdgeInsetsGeometry.Symmetric(horizontal: paddingUnit),
                    new Center(child: child))),
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

    private static void ValidateNonNegative(string name, double? value)
    {
        if (value.HasValue && (double.IsNaN(value.Value) || value.Value < 0))
        {
            throw new ArgumentOutOfRangeException(name);
        }
    }
}

internal sealed class ButtonBarRow : Flex
{
    public ButtonBarRow(
        IReadOnlyList<Widget> children,
        MainAxisSize mainAxisSize,
        MainAxisAlignment mainAxisAlignment,
        VerticalDirection overflowDirection,
        double? overflowButtonSpacing) : base(
        direction: Axis.Horizontal,
        children: children,
        mainAxisSize: mainAxisSize,
        mainAxisAlignment: mainAxisAlignment,
        verticalDirection: overflowDirection)
    {
        OverflowButtonSpacing = overflowButtonSpacing;
    }

    public double? OverflowButtonSpacing { get; }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderButtonBarRow(
            mainAxisSize: MainAxisSize,
            mainAxisAlignment: MainAxisAlignment,
            textDirection: Directionality.Of(context),
            verticalDirection: VerticalDirection,
            overflowButtonSpacing: OverflowButtonSpacing);
    }

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var row = (RenderButtonBarRow)renderObject;
        row.Direction = Direction;
        row.MainAxisAlignment = MainAxisAlignment;
        row.MainAxisSize = MainAxisSize;
        row.CrossAxisAlignment = CrossAxisAlignment;
        row.TextDirection = Directionality.Of(context);
        row.VerticalDirection = VerticalDirection;
        row.TextBaseline = TextBaseline;
        row.OverflowButtonSpacing = OverflowButtonSpacing;
    }
}

internal sealed class RenderButtonBarRow : RenderFlex
{
    private bool _hasCheckedLayoutWidth;
    private double? _overflowButtonSpacing;

    public RenderButtonBarRow(
        MainAxisSize mainAxisSize,
        MainAxisAlignment mainAxisAlignment,
        TextDirection textDirection,
        VerticalDirection verticalDirection,
        double? overflowButtonSpacing) : base(
        children: null,
        direction: Axis.Horizontal,
        mainAxisSize: mainAxisSize,
        mainAxisAlignment: mainAxisAlignment,
        crossAxisAlignment: CrossAxisAlignment.Center,
        textDirection: textDirection,
        verticalDirection: verticalDirection)
    {
        _overflowButtonSpacing = ValidateSpacing(overflowButtonSpacing);
    }

    public override BoxConstraints Constraints
    {
        get
        {
            BoxConstraints constraints = base.Constraints;
            return _hasCheckedLayoutWidth
                ? constraints
                : constraints with { MaxWidth = double.PositiveInfinity };
        }
    }

    public double? OverflowButtonSpacing
    {
        get => _overflowButtonSpacing;
        set
        {
            double? validated = ValidateSpacing(value);
            if (_overflowButtonSpacing == validated)
            {
                return;
            }

            _overflowButtonSpacing = validated;
            MarkNeedsLayout();
        }
    }

    protected override Size ComputeDryLayout(BoxConstraints constraints)
    {
        Size size = base.ComputeDryLayout(constraints with { MaxWidth = double.PositiveInfinity });
        if (size.Width <= constraints.MaxWidth)
        {
            return base.ComputeDryLayout(constraints);
        }

        double currentHeight = 0;
        for (RenderBox? child = FirstChild; child is not null; child = ChildAfter(child))
        {
            Size childSize = child.GetDryLayout(constraints with { MinWidth = 0 });
            currentHeight += childSize.Height;
            if (OverflowButtonSpacing.HasValue && ChildAfter(child) is not null)
            {
                currentHeight += OverflowButtonSpacing.Value;
            }
        }

        return constraints.Constrain(new Size(constraints.MaxWidth, currentHeight));
    }

    protected override void PerformLayout()
    {
        _hasCheckedLayoutWidth = false;
        base.PerformLayout();
        _hasCheckedLayoutWidth = true;

        if (Size.Width <= Constraints.MaxWidth)
        {
            base.PerformLayout();
            return;
        }

        BoxConstraints childConstraints = Constraints with { MinWidth = 0 };
        double currentHeight = 0;
        RenderBox? child = VerticalDirection == VerticalDirection.Down ? FirstChild : LastChild;
        while (child is not null)
        {
            var childParentData = (FlexParentData)child.parentData!;
            child.Layout(childConstraints, parentUsesSize: true);
            childParentData.offset = new Point(ResolveOverflowX(child.Size.Width), currentHeight);
            currentHeight += child.Size.Height;
            child = VerticalDirection == VerticalDirection.Down
                ? ChildAfter(child)
                : ChildBefore(child);
            if (OverflowButtonSpacing.HasValue && child is not null)
            {
                currentHeight += OverflowButtonSpacing.Value;
            }
        }

        Size = Constraints.Constrain(new Size(Constraints.MaxWidth, currentHeight));
    }

    private double ResolveOverflowX(double childWidth)
    {
        if (MainAxisAlignment == MainAxisAlignment.Center)
        {
            return (Constraints.MaxWidth - childWidth) / 2.0;
        }

        bool alignToRight = TextDirection switch
        {
            UI.TextDirection.Ltr => MainAxisAlignment == MainAxisAlignment.End,
            UI.TextDirection.Rtl => MainAxisAlignment != MainAxisAlignment.End,
            _ => throw new InvalidOperationException("ButtonBar requires a text direction."),
        };
        return alignToRight ? Constraints.MaxWidth - childWidth : 0;
    }

    private static double? ValidateSpacing(double? value)
    {
        if (value.HasValue && (double.IsNaN(value.Value) || value.Value < 0))
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        return value;
    }
}
