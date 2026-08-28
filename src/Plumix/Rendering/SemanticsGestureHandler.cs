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
/// <c>_RenderScrollSemantics</c> forms.
/// </remarks>
public class RenderSemanticsGestureHandler : RenderProxyBox
{
    private SemanticsActions? _validActions;
    private Action? _onTap;
    private Action? _onLongPress;
    private Action<DragUpdateDetails>? _onHorizontalDragUpdate;
    private Action<DragUpdateDetails>? _onVerticalDragUpdate;
    private double _scrollFactor = DefaultScrollFactor;

    /// <summary>The fraction of the viewport a single semantic scroll action moves.</summary>
    public const double DefaultScrollFactor = 0.8;

    public RenderSemanticsGestureHandler(RenderBox? child = null)
    {
        Child = child;
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
        set => SetGestureCallback(ref _onTap, value);
    }

    public Action? OnLongPress
    {
        get => _onLongPress;
        set => SetGestureCallback(ref _onLongPress, value);
    }

    public Action<DragUpdateDetails>? OnHorizontalDragUpdate
    {
        get => _onHorizontalDragUpdate;
        set => SetGestureCallback(ref _onHorizontalDragUpdate, value);
    }

    public Action<DragUpdateDetails>? OnVerticalDragUpdate
    {
        get => _onVerticalDragUpdate;
        set => SetGestureCallback(ref _onVerticalDragUpdate, value);
    }

    /// <summary>The fraction of the render object a semantic scroll action moves.</summary>
    public double ScrollFactor
    {
        get => _scrollFactor;
        set
        {
            if (value.Equals(_scrollFactor))
            {
                return;
            }

            _scrollFactor = value;
            MarkNeedsSemanticsUpdate();
        }
    }

    private void SetGestureCallback<T>(ref T? field, T? value) where T : class
    {
        bool hadHandler = field != null;
        field = value;
        if (hadHandler != (value != null))
        {
            MarkNeedsSemanticsUpdate();
        }
    }

    protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
    {
        base.DescribeSemanticsConfiguration(configuration);

        if (_onTap is { } tap && IsValidAction(SemanticsActions.Tap))
        {
            configuration.AddActionHandler(SemanticsActions.Tap, tap);
        }

        if (_onLongPress is { } longPress && IsValidAction(SemanticsActions.LongPress))
        {
            configuration.AddActionHandler(SemanticsActions.LongPress, longPress);
        }

        if (_onHorizontalDragUpdate != null)
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

        if (_onVerticalDragUpdate != null)
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
        return _validActions is not { } valid || (valid & action) != SemanticsActions.None;
    }

    private void PerformSemanticScrollLeft() => PerformHorizontalScroll(Size.Width * -_scrollFactor);

    private void PerformSemanticScrollRight() => PerformHorizontalScroll(Size.Width * _scrollFactor);

    private void PerformSemanticScrollUp() => PerformVerticalScroll(Size.Height * -_scrollFactor);

    private void PerformSemanticScrollDown() => PerformVerticalScroll(Size.Height * _scrollFactor);

    private void PerformHorizontalScroll(double primaryDelta)
    {
        if (_onHorizontalDragUpdate is not { } drag)
        {
            return;
        }

        var localCenter = new Point(Size.Width / 2.0, Size.Height / 2.0);
        drag(new DragUpdateDetails(
            GlobalPosition: LocalToGlobal(localCenter),
            LocalPosition: localCenter,
            Delta: new Point(primaryDelta, 0.0),
            PrimaryDelta: primaryDelta));
    }

    private void PerformVerticalScroll(double primaryDelta)
    {
        if (_onVerticalDragUpdate is not { } drag)
        {
            return;
        }

        var localCenter = new Point(Size.Width / 2.0, Size.Height / 2.0);
        drag(new DragUpdateDetails(
            GlobalPosition: LocalToGlobal(localCenter),
            LocalPosition: localCenter,
            Delta: new Point(0.0, primaryDelta),
            PrimaryDelta: primaryDelta));
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        var gestures = new List<string>();
        if (OnTap is not null)
        {
            gestures.Add("tap");
        }

        if (OnLongPress is not null)
        {
            gestures.Add("long press");
        }

        if (OnHorizontalDragUpdate is not null)
        {
            gestures.Add("horizontal scroll");
        }

        if (OnVerticalDragUpdate is not null)
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
