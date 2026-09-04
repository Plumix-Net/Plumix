using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/toggle_buttons.dart

public sealed class ToggleButtons : StatelessWidget
{
    private const double DefaultBorderWidth = 1.0;

    public ToggleButtons(
        IReadOnlyList<Widget> children,
        IReadOnlyList<bool> isSelected,
        Action<int>? onPressed = null,
        MouseCursor? mouseCursor = null,
        MaterialTapTargetSize? tapTargetSize = null,
        TextStyle? textStyle = null,
        BoxConstraints? constraints = null,
        Color? color = null,
        Color? selectedColor = null,
        Color? disabledColor = null,
        MaterialStateProperty<Color?>? fillColor = null,
        Color? focusColor = null,
        Color? highlightColor = null,
        Color? hoverColor = null,
        Color? splashColor = null,
        IReadOnlyList<FocusNode>? focusNodes = null,
        bool renderBorder = true,
        Color? borderColor = null,
        Color? selectedBorderColor = null,
        Color? disabledBorderColor = null,
        BorderRadius? borderRadius = null,
        double? borderWidth = null,
        Axis direction = Axis.Horizontal,
        VerticalDirection verticalDirection = VerticalDirection.Down,
        Key? key = null) : base(key)
    {
        ArgumentNullException.ThrowIfNull(children);
        ArgumentNullException.ThrowIfNull(isSelected);
        if (children.Count != isSelected.Count)
        {
            throw new ArgumentException(
                $"children has {children.Count} widgets and isSelected has {isSelected.Count} entries.",
                nameof(isSelected));
        }

        Children = children;
        IsSelected = isSelected;
        OnPressed = onPressed;
        MouseCursor = mouseCursor;
        TapTargetSize = tapTargetSize;
        TextStyle = textStyle;
        Constraints = constraints;
        Color = color;
        SelectedColor = selectedColor;
        DisabledColor = disabledColor;
        FillColor = fillColor;
        FocusColor = focusColor;
        HighlightColor = highlightColor;
        HoverColor = hoverColor;
        SplashColor = splashColor;
        FocusNodes = focusNodes;
        RenderBorder = renderBorder;
        BorderColor = borderColor;
        SelectedBorderColor = selectedBorderColor;
        DisabledBorderColor = disabledBorderColor;
        BorderRadius = borderRadius;
        BorderWidth = borderWidth;
        Direction = direction;
        VerticalDirection = verticalDirection;
    }

    public IReadOnlyList<Widget> Children { get; }
    public IReadOnlyList<bool> IsSelected { get; }
    public Action<int>? OnPressed { get; }
    public MouseCursor? MouseCursor { get; }
    public MaterialTapTargetSize? TapTargetSize { get; }
    public TextStyle? TextStyle { get; }
    public BoxConstraints? Constraints { get; }
    public Color? Color { get; }
    public Color? SelectedColor { get; }
    public Color? DisabledColor { get; }
    public MaterialStateProperty<Color?>? FillColor { get; }
    public Color? FocusColor { get; }
    public Color? HighlightColor { get; }
    public Color? HoverColor { get; }
    public Color? SplashColor { get; }
    public IReadOnlyList<FocusNode>? FocusNodes { get; }
    public bool RenderBorder { get; }
    public Color? BorderColor { get; }
    public Color? SelectedBorderColor { get; }
    public Color? DisabledBorderColor { get; }
    public BorderRadius? BorderRadius { get; }
    public double? BorderWidth { get; }
    public Axis Direction { get; }
    public VerticalDirection VerticalDirection { get; }

    public override Widget Build(BuildContext context)
    {
        if (FocusNodes is not null && FocusNodes.Count != Children.Count)
        {
            throw new ArgumentException(
                $"focusNodes has {FocusNodes.Count} nodes and children has {Children.Count} widgets.",
                nameof(FocusNodes));
        }

        var theme = Theme.Of(context);
        var colorScheme = theme.ColorScheme;
        var toggleTheme = ToggleButtonsTheme.Of(context);
        var effectiveConstraints = Constraints ?? toggleTheme.Constraints;
        Size minimumSize = effectiveConstraints?.Smallest ?? new Size(48.0, 48.0);
        Size? maximumSize = effectiveConstraints?.Biggest;
        var effectiveTextStyle = TextStyle ?? toggleTheme.TextStyle ?? theme.TextTheme.BodyMedium;
        double effectiveBorderWidth = BorderWidth ?? toggleTheme.BorderWidth ?? DefaultBorderWidth;
        var effectiveRadius = BorderRadius ?? toggleTheme.BorderRadius ?? Plumix.Rendering.BorderRadius.Zero;
        TextDirection textDirection = Directionality.Of(context);
        bool enabled = OnPressed is not null;
        Size tapTargetMinimumSize = ResolveTapTargetMinimumSize(
            TapTargetSize ?? theme.MaterialTapTargetSize,
            Direction);

        var buttons = new List<Widget>(Children.Count);
        for (int index = 0; index < Children.Count; index++)
        {
            bool selected = enabled && IsSelected[index];
            Color foreground = ResolveForegroundColor(colorScheme, toggleTheme, selected, enabled);
            var states = enabled
                ? selected ? MaterialState.Selected : MaterialState.None
                : MaterialState.Disabled;
            Color background = ResolveFillColor(colorScheme, toggleTheme, states);
            BorderSide leadingSide = ResolveLeadingBorderSide(
                colorScheme,
                toggleTheme,
                index,
                enabled,
                effectiveBorderWidth);
            BorderSide currentSide = ResolveBorderSide(
                colorScheme,
                toggleTheme,
                IsSelected[index],
                enabled,
                effectiveBorderWidth);
            BorderSide trailingSide = index == Children.Count - 1
                ? currentSide
                : Plumix.Rendering.BorderSide.None;
            if (!RenderBorder)
            {
                leadingSide = Plumix.Rendering.BorderSide.None;
                currentSide = Plumix.Rendering.BorderSide.None;
                trailingSide = Plumix.Rendering.BorderSide.None;
            }

            BorderRadius edgeRadius = ResolveEdgeRadius(
                effectiveRadius,
                index,
                Children.Count,
                textDirection);
            BorderRadius clipRadius = DeflateRadius(edgeRadius, effectiveBorderWidth / 2.0);
            int capturedIndex = index;
            var overlay = MaterialStateProperty<Color?>.ResolveWith(buttonStates =>
                ResolveOverlayColor(colorScheme, toggleTheme, selected, enabled, buttonStates));
            var style = new ButtonStyle(
                ForegroundColor: MaterialStateProperty<Color?>.All(foreground),
                BackgroundColor: MaterialStateProperty<Color?>.All(background),
                OverlayColor: overlay,
                Elevation: MaterialStateProperty<double?>.All(0.0),
                IconColor: MaterialStateProperty<Color?>.All(foreground),
                IconSize: MaterialStateProperty<double?>.All(24.0),
                Padding: MaterialStateProperty<EdgeInsetsGeometry?>.All(default),
                Shape: MaterialStateProperty<OutlinedBorder?>.All(new RoundedRectangleBorder(borderRadius:
                    Plumix.Rendering.BorderRadius.Zero)),
                MinimumSize: MaterialStateProperty<Size?>.All(minimumSize),
                MaximumSize: maximumSize.HasValue
                    ? MaterialStateProperty<Size?>.All(maximumSize.Value)
                    : null,
                Alignment: Alignment.Center,
                TapTargetSize: MaterialTapTargetSize.ShrinkWrap,
                TextStyle: MaterialStateProperty<TextStyle?>.All(
                    effectiveTextStyle.CopyWith(color: foreground)),
                MouseCursor: MouseCursor is null
                    ? null
                    : MaterialStateProperty<MouseCursor?>.All(MouseCursor),
                VisualDensity: VisualDensity.Standard,
                AnimationDuration: ButtonStyleState.ThemeChangeDuration,
                EnableFeedback: true,
                SplashFactory: InkRipple.SplashFactory);

            Widget button = new TextButton(
                focusNode: FocusNodes?[index],
                style: style,
                onPressed: enabled ? () => OnPressed!(capturedIndex) : null,
                child: Children[index]);
            button = new ClipRRect(clipRadius, child: button);
            button = new SelectToggleButton(
                leadingBorderSide: leadingSide,
                borderSide: currentSide,
                trailingBorderSide: trailingSide,
                borderRadius: edgeRadius,
                isFirstButton: index == 0,
                isLastButton: index == Children.Count - 1,
                direction: Direction,
                verticalDirection: VerticalDirection,
                textDirection: textDirection,
                child: button);
            if (effectiveConstraints.HasValue)
            {
                button = new Center(child: button);
            }

            button = new ToggleButtonInputPadding(
                minSize: tapTargetMinimumSize,
                direction: Direction,
                child: button);
            Action? semanticsTap = enabled ? () => OnPressed!(capturedIndex) : null;
            button = new MergeSemantics(
                new Semantics(
                    child: button,
                    container: true,
                    @checked: IsSelected[index],
                    onTap: semanticsTap,
                    flags: enabled ? SemanticsFlags.IsEnabled : SemanticsFlags.None));
            buttons.Add(button);
        }

        return Direction == Axis.Horizontal
            ? new IntrinsicHeight(
                child: new Row(
                    children: buttons,
                    mainAxisSize: MainAxisSize.Min,
                    crossAxisAlignment: CrossAxisAlignment.Stretch,
                    textDirection: textDirection))
            : new IntrinsicWidth(
                child: new Column(
                    children: buttons,
                    mainAxisSize: MainAxisSize.Min,
                    crossAxisAlignment: CrossAxisAlignment.Stretch,
                    textDirection: textDirection,
                    verticalDirection: VerticalDirection));
    }

    private static Size ResolveTapTargetMinimumSize(
        MaterialTapTargetSize tapTargetSize,
        Axis direction)
    {
        if (tapTargetSize == MaterialTapTargetSize.ShrinkWrap)
        {
            return new Size();
        }

        return direction == Axis.Horizontal
            ? new Size(0.0, 48.0)
            : new Size(48.0, 0.0);
    }

    private Color ResolveForegroundColor(
        ColorScheme colorScheme,
        ToggleButtonsThemeData toggleTheme,
        bool selected,
        bool enabled)
    {
        if (!enabled)
        {
            return DisabledColor
                   ?? toggleTheme.DisabledColor
                   ?? WithOpacity(colorScheme.OnSurface, 0.38);
        }

        return selected
            ? SelectedColor ?? toggleTheme.SelectedColor ?? colorScheme.Primary
            : Color ?? toggleTheme.Color ?? WithOpacity(colorScheme.OnSurface, 0.87);
    }

    private Color ResolveFillColor(
        ColorScheme colorScheme,
        ToggleButtonsThemeData toggleTheme,
        MaterialState states)
    {
        MaterialStateProperty<Color?>? fill = FillColor ?? toggleTheme.FillColor;
        if (fill is not null)
        {
            Color? resolved = fill is MaterialStatePropertyAll<Color?>
                ? states.HasFlag(MaterialState.Selected) ? fill.Resolve(states) : null
                : fill.Resolve(states);
            if (resolved.HasValue)
            {
                return resolved.Value;
            }
        }

        return states.HasFlag(MaterialState.Selected)
            ? WithOpacity(colorScheme.Primary, 0.12)
            : WithOpacity(colorScheme.Surface, 0.0);
    }

    private BorderSide ResolveLeadingBorderSide(
        ColorScheme colorScheme,
        ToggleButtonsThemeData toggleTheme,
        int index,
        bool enabled,
        double width)
    {
        bool selected = IsSelected[index] || index > 0 && IsSelected[index - 1];
        return ResolveBorderSide(colorScheme, toggleTheme, selected, enabled, width);
    }

    private BorderSide ResolveBorderSide(
        ColorScheme colorScheme,
        ToggleButtonsThemeData toggleTheme,
        bool selected,
        bool enabled,
        double width)
    {
        Color color = !enabled
            ? DisabledBorderColor ?? toggleTheme.DisabledBorderColor ?? WithOpacity(colorScheme.OnSurface, 0.12)
            : selected
                ? SelectedBorderColor
                  ?? toggleTheme.SelectedBorderColor
                  ?? WithOpacity(colorScheme.OnSurface, 0.12)
                : BorderColor ?? toggleTheme.BorderColor ?? WithOpacity(colorScheme.OnSurface, 0.12);
        return new BorderSide(color, width);
    }

    private Color? ResolveOverlayColor(
        ColorScheme colorScheme,
        ToggleButtonsThemeData toggleTheme,
        bool selected,
        bool enabled,
        MaterialState states)
    {
        if (!enabled || states.HasFlag(MaterialState.Disabled))
        {
            return null;
        }

        Color stateColor = selected ? colorScheme.Primary : colorScheme.OnSurface;
        if (states.HasFlag(MaterialState.Pressed))
        {
            return SplashColor
                   ?? toggleTheme.SplashColor
                   ?? (!selected ? HighlightColor ?? toggleTheme.HighlightColor : null)
                   ?? WithOpacity(stateColor, 0.16);
        }

        if (states.HasFlag(MaterialState.Hovered))
        {
            return HoverColor ?? toggleTheme.HoverColor ?? WithOpacity(stateColor, 0.04);
        }

        if (states.HasFlag(MaterialState.Focused))
        {
            return FocusColor ?? toggleTheme.FocusColor ?? WithOpacity(stateColor, 0.12);
        }

        return null;
    }

    private BorderRadius ResolveEdgeRadius(
        BorderRadius radius,
        int index,
        int count,
        TextDirection textDirection)
    {
        if (count == 1)
        {
            return radius;
        }

        bool first = index == 0;
        bool last = index == count - 1;
        if (Direction == Axis.Horizontal)
        {
            bool left = textDirection == TextDirection.Ltr ? first : last;
            bool right = textDirection == TextDirection.Ltr ? last : first;
            return Plumix.Rendering.BorderRadius.Only(
                topLeft: left ? radius.TopLeftRadius : Plumix.Rendering.Radius.Zero,
                topRight: right ? radius.TopRightRadius : Plumix.Rendering.Radius.Zero,
                bottomRight: right ? radius.BottomRightRadius : Plumix.Rendering.Radius.Zero,
                bottomLeft: left ? radius.BottomLeftRadius : Plumix.Rendering.Radius.Zero);
        }

        bool top = VerticalDirection == VerticalDirection.Down ? first : last;
        bool bottom = VerticalDirection == VerticalDirection.Down ? last : first;
        return Plumix.Rendering.BorderRadius.Only(
            topLeft: top ? radius.TopLeftRadius : Plumix.Rendering.Radius.Zero,
            topRight: top ? radius.TopRightRadius : Plumix.Rendering.Radius.Zero,
            bottomRight: bottom ? radius.BottomRightRadius : Plumix.Rendering.Radius.Zero,
            bottomLeft: bottom ? radius.BottomLeftRadius : Plumix.Rendering.Radius.Zero);
    }

    private static BorderRadius DeflateRadius(BorderRadius radius, double amount)
    {
        return Plumix.Rendering.BorderRadius.Only(
            topLeft: radius.TopLeftRadius.Deflate(amount),
            topRight: radius.TopRightRadius.Deflate(amount),
            bottomRight: radius.BottomRightRadius.Deflate(amount),
            bottomLeft: radius.BottomLeftRadius.Deflate(amount));
    }

    private static Color WithOpacity(Color color, double opacity)
    {
        return NavigationSurfaceUtilities.WithOpacity(color, opacity);
    }
}

internal sealed class SelectToggleButton : SingleChildRenderObjectWidget
{
    public SelectToggleButton(
        BorderSide leadingBorderSide,
        BorderSide borderSide,
        BorderSide trailingBorderSide,
        BorderRadius borderRadius,
        bool isFirstButton,
        bool isLastButton,
        Axis direction,
        VerticalDirection verticalDirection,
        TextDirection textDirection,
        Widget child) : base(child)
    {
        LeadingBorderSide = leadingBorderSide;
        BorderSide = borderSide;
        TrailingBorderSide = trailingBorderSide;
        BorderRadius = borderRadius;
        IsFirstButton = isFirstButton;
        IsLastButton = isLastButton;
        Direction = direction;
        VerticalDirection = verticalDirection;
        TextDirection = textDirection;
    }

    public BorderSide LeadingBorderSide { get; }
    public BorderSide BorderSide { get; }
    public BorderSide TrailingBorderSide { get; }
    public BorderRadius BorderRadius { get; }
    public bool IsFirstButton { get; }
    public bool IsLastButton { get; }
    public Axis Direction { get; }
    public VerticalDirection VerticalDirection { get; }
    public TextDirection TextDirection { get; }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSelectToggleButton(
            leadingBorderSide: LeadingBorderSide,
            borderSide: BorderSide,
            trailingBorderSide: TrailingBorderSide,
            borderRadius: BorderRadius,
            isFirstButton: IsFirstButton,
            isLastButton: IsLastButton,
            direction: Direction,
            verticalDirection: VerticalDirection,
            textDirection: TextDirection);
    }

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var button = (RenderSelectToggleButton)renderObject;
        button.LeadingBorderSide = LeadingBorderSide;
        button.BorderSide = BorderSide;
        button.TrailingBorderSide = TrailingBorderSide;
        button.BorderRadius = BorderRadius;
        button.IsFirstButton = IsFirstButton;
        button.IsLastButton = IsLastButton;
        button.Direction = Direction;
        button.VerticalDirection = VerticalDirection;
        button.TextDirection = TextDirection;
    }
}

internal sealed class RenderSelectToggleButton : RenderProxyBox
{
    private BorderSide _leadingBorderSide;
    private BorderSide _borderSide;
    private BorderSide _trailingBorderSide;
    private BorderRadius _borderRadius;
    private bool _isFirstButton;
    private bool _isLastButton;
    private Axis _direction;
    private VerticalDirection _verticalDirection;
    private TextDirection _textDirection;

    public RenderSelectToggleButton(
        BorderSide leadingBorderSide,
        BorderSide borderSide,
        BorderSide trailingBorderSide,
        BorderRadius borderRadius,
        bool isFirstButton,
        bool isLastButton,
        Axis direction,
        VerticalDirection verticalDirection,
        TextDirection textDirection)
    {
        _leadingBorderSide = leadingBorderSide;
        _borderSide = borderSide;
        _trailingBorderSide = trailingBorderSide;
        _borderRadius = borderRadius;
        _isFirstButton = isFirstButton;
        _isLastButton = isLastButton;
        _direction = direction;
        _verticalDirection = verticalDirection;
        _textDirection = textDirection;
    }

    public BorderSide LeadingBorderSide
    {
        get => _leadingBorderSide;
        set => SetLayoutProperty(ref _leadingBorderSide, value);
    }

    public BorderSide BorderSide
    {
        get => _borderSide;
        set => SetLayoutProperty(ref _borderSide, value);
    }

    public BorderSide TrailingBorderSide
    {
        get => _trailingBorderSide;
        set => SetLayoutProperty(ref _trailingBorderSide, value);
    }

    public BorderRadius BorderRadius
    {
        get => _borderRadius;
        set => SetLayoutProperty(ref _borderRadius, value);
    }

    public bool IsFirstButton
    {
        get => _isFirstButton;
        set => SetLayoutProperty(ref _isFirstButton, value);
    }

    public bool IsLastButton
    {
        get => _isLastButton;
        set => SetLayoutProperty(ref _isLastButton, value);
    }

    public Axis Direction
    {
        get => _direction;
        set => SetLayoutProperty(ref _direction, value);
    }

    public VerticalDirection VerticalDirection
    {
        get => _verticalDirection;
        set => SetLayoutProperty(ref _verticalDirection, value);
    }

    public TextDirection TextDirection
    {
        get => _textDirection;
        set => SetLayoutProperty(ref _textDirection, value);
    }

    private Thickness ChildPadding => Direction == Axis.Horizontal
        ? TextDirection == TextDirection.Ltr
            ? new Thickness(
                LeadingBorderSide.Width,
                BorderSide.Width,
                TrailingBorderSide.Width,
                BorderSide.Width)
            : new Thickness(
                TrailingBorderSide.Width,
                BorderSide.Width,
                LeadingBorderSide.Width,
                BorderSide.Width)
        : VerticalDirection == VerticalDirection.Down
            ? new Thickness(
                BorderSide.Width,
                LeadingBorderSide.Width,
                BorderSide.Width,
                TrailingBorderSide.Width)
            : new Thickness(
                BorderSide.Width,
                TrailingBorderSide.Width,
                BorderSide.Width,
                LeadingBorderSide.Width);

    protected override void PerformLayout()
    {
        if (Child is null)
        {
            Size = ComputeEmptySize(Constraints);
            return;
        }

        Thickness padding = ChildPadding;
        Child.Layout(Constraints.Deflate(padding), parentUsesSize: true);
        Size = Constraints.Constrain(Inflate(Child.Size, padding));
        ((BoxParentData)Child.parentData!).offset = new Point(padding.Left, padding.Top);
    }

    protected override Size ComputeDryLayout(BoxConstraints constraints)
    {
        if (Child is null)
        {
            return ComputeEmptySize(constraints);
        }

        Thickness padding = ChildPadding;
        Size childSize = Child.GetDryLayout(constraints.Deflate(padding));
        return constraints.Constrain(Inflate(childSize, padding));
    }

    protected override double ComputeMinIntrinsicWidth(double height)
    {
        double childWidth = Child?.GetMinIntrinsicWidth(height) ?? 0.0;
        return Direction == Axis.Horizontal
            ? LeadingBorderSide.Width + childWidth + TrailingBorderSide.Width
            : (BorderSide.Width * 2.0) + childWidth;
    }

    protected override double ComputeMaxIntrinsicWidth(double height)
    {
        double childWidth = Child?.GetMaxIntrinsicWidth(height) ?? 0.0;
        return Direction == Axis.Horizontal
            ? LeadingBorderSide.Width + childWidth + TrailingBorderSide.Width
            : (BorderSide.Width * 2.0) + childWidth;
    }

    protected override double ComputeMinIntrinsicHeight(double width)
    {
        double childHeight = Direction == Axis.Vertical
            ? Child?.GetMaxIntrinsicHeight(width) ?? 0.0
            : Child?.GetMinIntrinsicHeight(width) ?? 0.0;
        return Direction == Axis.Horizontal
            ? (BorderSide.Width * 2.0) + childHeight
            : LeadingBorderSide.Width + childHeight + TrailingBorderSide.Width;
    }

    protected override double ComputeMaxIntrinsicHeight(double width)
    {
        double childHeight = Child?.GetMaxIntrinsicHeight(width) ?? 0.0;
        return Direction == Axis.Horizontal
            ? (BorderSide.Width * 2.0) + childHeight
            : LeadingBorderSide.Width + childHeight + TrailingBorderSide.Width;
    }

    protected override double? ComputeDryBaseline(BoxConstraints constraints, TextBaseline baseline)
    {
        if (Child is null)
        {
            return null;
        }

        double? childBaseline = Child.GetDryBaseline(constraints.Deflate(ChildPadding), baseline);
        return childBaseline + ChildPadding.Top;
    }

    protected override double? ComputeDistanceToActualBaseline(TextBaseline baseline)
    {
        if (Child is null)
        {
            return null;
        }

        double? childBaseline = Child.GetDistanceToBaseline(baseline, onlyReal: true);
        return childBaseline + ChildPadding.Top;
    }

    public override void Paint(PaintingContext context, Point offset)
    {
        base.Paint(context, offset);
        PaintBorder(context, offset);
    }

    private Size ComputeEmptySize(BoxConstraints constraints)
    {
        var horizontal = new Size(
            LeadingBorderSide.Width + TrailingBorderSide.Width,
            BorderSide.Width * 2.0);
        return constraints.Constrain(
            Direction == Axis.Horizontal
                ? horizontal
                : new Size(horizontal.Height, horizontal.Width));
    }

    private void PaintBorder(PaintingContext context, Point offset)
    {
        if (Size.Width <= 0.0 || Size.Height <= 0.0)
        {
            return;
        }

        if (IsFirstButton && IsLastButton)
        {
            PaintRoundedRectangle(context, offset, LeadingBorderSide);
            return;
        }

        bool leadingAtStart = Direction == Axis.Horizontal
            ? TextDirection == TextDirection.Ltr
            : VerticalDirection == VerticalDirection.Down;
        if (Direction == Axis.Horizontal)
        {
            bool physicalLeftEdge = IsFirstButton == leadingAtStart;
            bool physicalRightEdge = IsLastButton == leadingAtStart;
            PaintHorizontalBorder(context, offset, physicalLeftEdge, physicalRightEdge);
        }
        else
        {
            bool physicalTopEdge = IsFirstButton == leadingAtStart;
            bool physicalBottomEdge = IsLastButton == leadingAtStart;
            PaintVerticalBorder(context, offset, physicalTopEdge, physicalBottomEdge);
        }
    }

    private void PaintRoundedRectangle(PaintingContext context, Point offset, BorderSide side)
    {
        if (!ShouldPaint(side))
        {
            return;
        }

        double half = side.Width / 2.0;
        var rect = new Rect(
            offset + new Vector(half, half),
            new Size(Math.Max(0.0, Size.Width - side.Width), Math.Max(0.0, Size.Height - side.Width)));
        context.Canvas.DrawRectangle(
            new SolidColorBrush(Colors.Transparent),
            PenFor(side),
            rect,
            NormalizeRadius(BorderRadius));
    }

    private void PaintHorizontalBorder(
        PaintingContext context,
        Point offset,
        bool physicalLeftEdge,
        bool physicalRightEdge)
    {
        BorderSide horizontalSide = physicalLeftEdge
            ? LeadingBorderSide
            : physicalRightEdge ? TrailingBorderSide : BorderSide;
        Radius topStart = physicalLeftEdge ? ClampRadius(BorderRadius.TopLeftRadius) : Radius.Zero;
        Radius topEnd = physicalRightEdge ? ClampRadius(BorderRadius.TopRightRadius) : Radius.Zero;
        Radius bottomStart = physicalLeftEdge ? ClampRadius(BorderRadius.BottomLeftRadius) : Radius.Zero;
        Radius bottomEnd = physicalRightEdge ? ClampRadius(BorderRadius.BottomRightRadius) : Radius.Zero;
        PaintHorizontalLine(context, offset, horizontalSide, top: true, topStart.X, topEnd.X);
        PaintHorizontalLine(context, offset, horizontalSide, top: false, bottomStart.X, bottomEnd.X);

        if (physicalLeftEdge)
        {
            PaintVerticalLine(
                context,
                offset,
                LeadingBorderSide,
                left: true,
                topStart.Y,
                bottomStart.Y);
            PaintCorner(context, offset, LeadingBorderSide, Corner.TopLeft, topStart);
            PaintCorner(context, offset, LeadingBorderSide, Corner.BottomLeft, bottomStart);
        }
        else
        {
            bool separatorOnLeft = TextDirection == TextDirection.Ltr;
            PaintVerticalLine(context, offset, LeadingBorderSide, separatorOnLeft);
        }

        if (physicalRightEdge)
        {
            PaintVerticalLine(
                context,
                offset,
                TrailingBorderSide,
                left: false,
                topEnd.Y,
                bottomEnd.Y);
            PaintCorner(context, offset, TrailingBorderSide, Corner.TopRight, topEnd);
            PaintCorner(context, offset, TrailingBorderSide, Corner.BottomRight, bottomEnd);
        }
    }

    private void PaintVerticalBorder(
        PaintingContext context,
        Point offset,
        bool physicalTopEdge,
        bool physicalBottomEdge)
    {
        BorderSide verticalSide = physicalTopEdge
            ? LeadingBorderSide
            : physicalBottomEdge ? TrailingBorderSide : BorderSide;
        Radius leftStart = physicalTopEdge ? ClampRadius(BorderRadius.TopLeftRadius) : Radius.Zero;
        Radius leftEnd = physicalBottomEdge ? ClampRadius(BorderRadius.BottomLeftRadius) : Radius.Zero;
        Radius rightStart = physicalTopEdge ? ClampRadius(BorderRadius.TopRightRadius) : Radius.Zero;
        Radius rightEnd = physicalBottomEdge ? ClampRadius(BorderRadius.BottomRightRadius) : Radius.Zero;
        PaintVerticalLine(context, offset, verticalSide, left: true, leftStart.Y, leftEnd.Y);
        PaintVerticalLine(context, offset, verticalSide, left: false, rightStart.Y, rightEnd.Y);

        if (physicalTopEdge)
        {
            PaintHorizontalLine(
                context,
                offset,
                LeadingBorderSide,
                top: true,
                leftStart.X,
                rightStart.X);
            PaintCorner(context, offset, LeadingBorderSide, Corner.TopLeft, leftStart);
            PaintCorner(context, offset, LeadingBorderSide, Corner.TopRight, rightStart);
        }
        else
        {
            bool separatorOnTop = VerticalDirection == VerticalDirection.Down;
            PaintHorizontalLine(context, offset, LeadingBorderSide, separatorOnTop);
        }

        if (physicalBottomEdge)
        {
            PaintHorizontalLine(
                context,
                offset,
                TrailingBorderSide,
                top: false,
                leftEnd.X,
                rightEnd.X);
            PaintCorner(context, offset, TrailingBorderSide, Corner.BottomLeft, leftEnd);
            PaintCorner(context, offset, TrailingBorderSide, Corner.BottomRight, rightEnd);
        }
    }

    private void PaintHorizontalLine(
        PaintingContext context,
        Point offset,
        BorderSide side,
        bool top,
        double startInset = 0.0,
        double endInset = 0.0)
    {
        if (!ShouldPaint(side))
        {
            return;
        }

        double half = side.Width / 2.0;
        double y = offset.Y + (top ? half : Size.Height - half);
        context.Canvas.DrawLine(
            PenFor(side),
            new Point(offset.X + half + startInset, y),
            new Point(
                offset.X + Math.Max(half + startInset, Size.Width - half - endInset),
                y));
    }

    private void PaintVerticalLine(
        PaintingContext context,
        Point offset,
        BorderSide side,
        bool left,
        double startInset = 0.0,
        double endInset = 0.0)
    {
        if (!ShouldPaint(side))
        {
            return;
        }

        double half = side.Width / 2.0;
        double x = offset.X + (left ? half : Size.Width - half);
        context.Canvas.DrawLine(
            PenFor(side),
            new Point(x, offset.Y + half + startInset),
            new Point(
                x,
                offset.Y + Math.Max(half + startInset, Size.Height - half - endInset)));
    }

    private void PaintCorner(
        PaintingContext context,
        Point offset,
        BorderSide side,
        Corner corner,
        Radius radius)
    {
        if (!ShouldPaint(side) || radius.X <= 0.0 || radius.Y <= 0.0)
        {
            return;
        }

        double half = side.Width / 2.0;
        Point position = corner switch
        {
            Corner.TopLeft => offset + new Vector(half, half),
            Corner.TopRight => offset + new Vector(Size.Width - half - (radius.X * 2.0), half),
            Corner.BottomRight => offset + new Vector(
                Size.Width - half - (radius.X * 2.0),
                Size.Height - half - (radius.Y * 2.0)),
            Corner.BottomLeft => offset + new Vector(half, Size.Height - half - (radius.Y * 2.0)),
            _ => throw new ArgumentOutOfRangeException(nameof(corner), corner, null),
        };
        double startAngle = corner switch
        {
            Corner.TopLeft => Math.PI,
            Corner.TopRight => Math.PI * 1.5,
            Corner.BottomRight => 0.0,
            Corner.BottomLeft => Math.PI * 0.5,
            _ => throw new ArgumentOutOfRangeException(nameof(corner), corner, null),
        };
        context.Canvas.DrawArc(
            PenFor(side),
            new Rect(position, new Size(radius.X * 2.0, radius.Y * 2.0)),
            startAngle,
            Math.PI / 2.0);
    }

    private Radius ClampRadius(Radius radius)
    {
        if (radius.X * radius.Y == 0.0)
        {
            return Radius.Zero;
        }

        return Radius.Elliptical(
            Math.Min(radius.X, Size.Width / 2.0),
            Math.Min(radius.Y, Size.Height / 2.0));
    }

    private static BorderRadius NormalizeRadius(BorderRadius radius)
    {
        return new BorderRadius(
            NormalizeCorner(radius.TopLeftRadius),
            NormalizeCorner(radius.TopRightRadius),
            NormalizeCorner(radius.BottomRightRadius),
            NormalizeCorner(radius.BottomLeftRadius));
    }

    private static Radius NormalizeCorner(Radius radius)
    {
        return radius.X * radius.Y == 0.0 ? Radius.Zero : radius;
    }

    private static bool ShouldPaint(BorderSide side)
    {
        return side.Style != BorderStyle.None && side.Width > 0.0 && side.Color.A > 0;
    }

    private static Pen PenFor(BorderSide side)
    {
        return new Pen(new SolidColorBrush(side.Color), side.Width);
    }

    private enum Corner
    {
        TopLeft,
        TopRight,
        BottomRight,
        BottomLeft,
    }

    private static Size Inflate(Size size, Thickness padding)
    {
        return new Size(
            size.Width + padding.Left + padding.Right,
            size.Height + padding.Top + padding.Bottom);
    }

    private void SetLayoutProperty<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        MarkNeedsLayout();
    }
}

internal sealed class ToggleButtonInputPadding : SingleChildRenderObjectWidget
{
    public ToggleButtonInputPadding(Size minSize, Axis direction, Widget child) : base(child)
    {
        MinSize = minSize;
        Direction = direction;
    }

    public Size MinSize { get; }
    public Axis Direction { get; }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderToggleButtonInputPadding(MinSize, Direction);
    }

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var padding = (RenderToggleButtonInputPadding)renderObject;
        padding.MinSize = MinSize;
        padding.Direction = Direction;
    }
}

internal sealed class RenderToggleButtonInputPadding : RenderProxyBox
{
    private Size _minSize;
    private Axis _direction;

    public RenderToggleButtonInputPadding(Size minSize, Axis direction)
    {
        _minSize = minSize;
        _direction = direction;
    }

    public Size MinSize
    {
        get => _minSize;
        set
        {
            if (_minSize == value)
            {
                return;
            }

            _minSize = value;
            MarkNeedsLayout();
        }
    }

    public Axis Direction
    {
        get => _direction;
        set
        {
            if (_direction == value)
            {
                return;
            }

            _direction = value;
            MarkNeedsLayout();
        }
    }

    protected override void PerformLayout()
    {
        if (Child is null)
        {
            Size = new Size();
            return;
        }

        Child.Layout(Constraints, parentUsesSize: true);
        Size = Constraints.Constrain(new Size(
            Math.Max(Child.Size.Width, MinSize.Width),
            Math.Max(Child.Size.Height, MinSize.Height)));
        ((BoxParentData)Child.parentData!).offset = new Point(
            (Size.Width - Child.Size.Width) / 2.0,
            (Size.Height - Child.Size.Height) / 2.0);
    }

    protected override Size ComputeDryLayout(BoxConstraints constraints)
    {
        if (Child is null)
        {
            return new Size();
        }

        Size childSize = Child.GetDryLayout(constraints);
        return constraints.Constrain(new Size(
            Math.Max(childSize.Width, MinSize.Width),
            Math.Max(childSize.Height, MinSize.Height)));
    }

    protected override double ComputeMinIntrinsicWidth(double height)
    {
        return Child is null ? 0.0 : Math.Max(Child.GetMinIntrinsicWidth(height), MinSize.Width);
    }

    protected override double ComputeMaxIntrinsicWidth(double height)
    {
        return Child is null ? 0.0 : Math.Max(Child.GetMaxIntrinsicWidth(height), MinSize.Width);
    }

    protected override double ComputeMinIntrinsicHeight(double width)
    {
        return Child is null ? 0.0 : Math.Max(Child.GetMinIntrinsicHeight(width), MinSize.Height);
    }

    protected override double ComputeMaxIntrinsicHeight(double width)
    {
        return Child is null ? 0.0 : Math.Max(Child.GetMaxIntrinsicHeight(width), MinSize.Height);
    }

    protected override double? ComputeDryBaseline(BoxConstraints constraints, TextBaseline baseline)
    {
        if (Child is null)
        {
            return null;
        }

        double? childBaseline = Child.GetDryBaseline(constraints, baseline);
        if (!childBaseline.HasValue)
        {
            return null;
        }

        Size outerSize = ComputeDryLayout(constraints);
        Size childSize = Child.GetDryLayout(constraints);
        return childBaseline.Value + ((outerSize.Height - childSize.Height) / 2.0);
    }

    public override bool HitTest(BoxHitTestResult result, Point position)
    {
        // The base HitTest also checks HitTestChildren. We don't want that in this case because
        // we've padded around the children per tapTargetSize.
        if (position.X < 0.0 || position.Y < 0.0 || position.X >= Size.Width || position.Y >= Size.Height)
        {
            return false;
        }

        if (Child is null)
        {
            return false;
        }

        RenderBox child = Child;

        // Only adjust one axis to ensure the correct button is tapped.
        Point center = Direction == Axis.Horizontal
            ? new Point(position.X, child.Size.Height / 2.0)
            : new Point(child.Size.Width / 2.0, position.Y);
        return result.AddWithRawTransform(
            MatrixUtils.ForceToPoint(center),
            center,
            (hitResult, _) => child.HitTest(hitResult, center));
    }
}
