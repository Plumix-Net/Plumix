using Avalonia;
using Plumix.Gestures;
using Plumix.Foundation;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/proxy_box.dart

namespace Plumix.Rendering;

/// <summary>
/// Listens for the semantic counterparts of the gestures a <c>RawGestureDetector</c> recognizes and
/// exposes them as semantics actions.
/// </summary>
/// <remarks>
/// Flutter's <c>RenderSemanticsGestureHandler</c>. The configuration is not a semantics boundary, so
/// these actions merge into the nearest node-forming ancestor — for a scrollable, the node
/// <c>_RenderScrollSemantics</c> forms. Dart's <c>Set&lt;SemanticsAction&gt;? validActions</c> is the
/// <see cref="SemanticsActions"/> flags enum, the repository's only action-set type.
/// </remarks>
public class RenderSemanticsGestureHandler : RenderProxyBoxWithHitTestBehavior
{
    private SemanticsActions? _validActions;
    private Action? _onTap;
    private Action? _onLongPress;
    private Action<DragUpdateDetails>? _onHorizontalDragUpdate;
    private Action<DragUpdateDetails>? _onVerticalDragUpdate;

    public RenderSemanticsGestureHandler(
        RenderBox? child = null,
        Action? onTap = null,
        Action? onLongPress = null,
        Action<DragUpdateDetails>? onHorizontalDragUpdate = null,
        Action<DragUpdateDetails>? onVerticalDragUpdate = null,
        double scrollFactor = 0.8,
        HitTestBehavior behavior = HitTestBehavior.DeferToChild) : base(behavior: behavior, child: child)
    {
        _onTap = onTap;
        _onLongPress = onLongPress;
        _onHorizontalDragUpdate = onHorizontalDragUpdate;
        _onVerticalDragUpdate = onVerticalDragUpdate;
        ScrollFactor = scrollFactor;
    }

    /// <summary>
    /// The subset of the scroll and tap actions this handler is allowed to expose, or <c>null</c> to
    /// expose every action whose callback is set.
    /// </summary>
    /// <remarks>Flutter's <c>RenderSemanticsGestureHandler.validActions</c>.</remarks>
    public SemanticsActions? ValidActions
    {
        get => _validActions;
        set
        {
            if (value == _validActions)
            {
                return;
            }

            _validActions = value;
            MarkNeedsSemanticsUpdate();
        }
    }

    public Action? OnTap
    {
        get => _onTap;
        set
        {
            if (_onTap == value)
            {
                return;
            }

            bool hadHandler = _onTap != null;
            _onTap = value;
            if ((value != null) != hadHandler)
            {
                MarkNeedsSemanticsUpdate();
            }
        }
    }

    public Action? OnLongPress
    {
        get => _onLongPress;
        set
        {
            if (_onLongPress == value)
            {
                return;
            }

            bool hadHandler = _onLongPress != null;
            _onLongPress = value;
            if ((value != null) != hadHandler)
            {
                MarkNeedsSemanticsUpdate();
            }
        }
    }

    public Action<DragUpdateDetails>? OnHorizontalDragUpdate
    {
        get => _onHorizontalDragUpdate;
        set
        {
            if (_onHorizontalDragUpdate == value)
            {
                return;
            }

            bool hadHandler = _onHorizontalDragUpdate != null;
            _onHorizontalDragUpdate = value;
            if ((value != null) != hadHandler)
            {
                MarkNeedsSemanticsUpdate();
            }
        }
    }

    public Action<DragUpdateDetails>? OnVerticalDragUpdate
    {
        get => _onVerticalDragUpdate;
        set
        {
            if (_onVerticalDragUpdate == value)
            {
                return;
            }

            bool hadHandler = _onVerticalDragUpdate != null;
            _onVerticalDragUpdate = value;
            if ((value != null) != hadHandler)
            {
                MarkNeedsSemanticsUpdate();
            }
        }
    }

    /// <summary>The fraction of the render object a semantic scroll action moves.</summary>
    /// <remarks>
    /// Flutter's <c>scrollFactor</c> is a plain mutable field: assigning it schedules no semantics
    /// update, because the value is only read when an action fires.
    /// </remarks>
    public double ScrollFactor { get; set; }

    protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
    {
        base.DescribeSemanticsConfiguration(configuration);

        if (OnTap is { } onTap && IsValidAction(SemanticsActions.Tap))
        {
            configuration.OnTap = onTap;
        }

        if (OnLongPress is { } onLongPress && IsValidAction(SemanticsActions.LongPress))
        {
            configuration.OnLongPress = onLongPress;
        }

        if (OnHorizontalDragUpdate != null)
        {
            if (IsValidAction(SemanticsActions.ScrollRight))
            {
                configuration.OnScrollRight = PerformSemanticScrollRight;
            }

            if (IsValidAction(SemanticsActions.ScrollLeft))
            {
                configuration.OnScrollLeft = PerformSemanticScrollLeft;
            }
        }

        if (OnVerticalDragUpdate != null)
        {
            if (IsValidAction(SemanticsActions.ScrollUp))
            {
                configuration.OnScrollUp = PerformSemanticScrollUp;
            }

            if (IsValidAction(SemanticsActions.ScrollDown))
            {
                configuration.OnScrollDown = PerformSemanticScrollDown;
            }
        }
    }

    private bool IsValidAction(SemanticsActions action)
    {
        return ValidActions is not { } valid || (valid & action) != SemanticsActions.None;
    }

    private void PerformSemanticScrollLeft()
    {
        if (OnHorizontalDragUpdate != null)
        {
            double primaryDelta = Size.Width * -ScrollFactor;
            OnHorizontalDragUpdate(CreateScrollDetails(new Point(primaryDelta, 0.0), primaryDelta));
        }
    }

    private void PerformSemanticScrollRight()
    {
        if (OnHorizontalDragUpdate != null)
        {
            double primaryDelta = Size.Width * ScrollFactor;
            OnHorizontalDragUpdate(CreateScrollDetails(new Point(primaryDelta, 0.0), primaryDelta));
        }
    }

    private void PerformSemanticScrollUp()
    {
        if (OnVerticalDragUpdate != null)
        {
            double primaryDelta = Size.Height * -ScrollFactor;
            OnVerticalDragUpdate(CreateScrollDetails(new Point(0.0, primaryDelta), primaryDelta));
        }
    }

    private void PerformSemanticScrollDown()
    {
        if (OnVerticalDragUpdate != null)
        {
            double primaryDelta = Size.Height * ScrollFactor;
            OnVerticalDragUpdate(CreateScrollDetails(new Point(0.0, primaryDelta), primaryDelta));
        }
    }

    /// <remarks>
    /// Dart's <c>DragUpdateDetails(delta:, primaryDelta:, globalPosition: localToGlobal(size.center(Offset.zero)))</c>;
    /// its <c>localPosition</c> defaults to <c>globalPosition</c>.
    /// </remarks>
    private DragUpdateDetails CreateScrollDetails(Point delta, double primaryDelta)
    {
        Point globalPosition = LocalToGlobal(new Point(Size.Width / 2.0, Size.Height / 2.0));
        return new DragUpdateDetails(
            GlobalPosition: globalPosition,
            LocalPosition: globalPosition,
            Delta: delta,
            PrimaryDelta: primaryDelta);
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        var gestures = new List<string>();
        if (OnTap != null)
        {
            gestures.Add("tap");
        }

        if (OnLongPress != null)
        {
            gestures.Add("long press");
        }

        if (OnHorizontalDragUpdate != null)
        {
            gestures.Add("horizontal scroll");
        }

        if (OnVerticalDragUpdate != null)
        {
            gestures.Add("vertical scroll");
        }

        if (gestures.Count == 0)
        {
            gestures.Add("<none>");
        }

        properties.Add(new IterableProperty<string>("gestures", gestures));
    }
}
