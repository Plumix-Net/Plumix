using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

// Dart parity source: cupertino_ui/lib/src/picker.dart

namespace Plumix.Cupertino;

/// <summary>An iOS-styled fixed-extent wheel picker.</summary>
public sealed class CupertinoPicker : StatefulWidget
{
    internal const double DefaultDiameterRatio = 1.07;
    internal const double DefaultSqueeze = 1.45;
    internal const double OverAndUnderCenterOpacity = 0.447;
    internal static readonly TimeSpan TapToScrollDuration = TimeSpan.FromMilliseconds(300.0);

    /// <summary>Creates a picker from a concrete list of children.</summary>
    public CupertinoPicker(
        double itemExtent,
        Action<int>? onSelectedItemChanged,
        IReadOnlyList<Widget> children,
        double diameterRatio = DefaultDiameterRatio,
        CupertinoDynamicColor? backgroundColor = null,
        double offAxisFraction = 0.0,
        bool useMagnifier = false,
        double magnification = 1.0,
        FixedExtentScrollController? scrollController = null,
        double squeeze = DefaultSqueeze,
        ChangeReportingBehavior changeReportingBehavior = ChangeReportingBehavior.OnScrollUpdate,
        bool looping = false,
        Key? key = null) : this(
        itemExtent,
        onSelectedItemChanged,
        children,
        selectionOverlay: new CupertinoPickerDefaultSelectionOverlay(),
        diameterRatio,
        backgroundColor,
        offAxisFraction,
        useMagnifier,
        magnification,
        scrollController,
        squeeze,
        changeReportingBehavior,
        looping,
        key)
    {
    }

    /// <summary>Creates a picker with an explicitly supplied (or removed) selection overlay.</summary>
    public CupertinoPicker(
        double itemExtent,
        Action<int>? onSelectedItemChanged,
        IReadOnlyList<Widget> children,
        Widget? selectionOverlay,
        double diameterRatio = DefaultDiameterRatio,
        CupertinoDynamicColor? backgroundColor = null,
        double offAxisFraction = 0.0,
        bool useMagnifier = false,
        double magnification = 1.0,
        FixedExtentScrollController? scrollController = null,
        double squeeze = DefaultSqueeze,
        ChangeReportingBehavior changeReportingBehavior = ChangeReportingBehavior.OnScrollUpdate,
        bool looping = false,
        Key? key = null) : this(
        itemExtent,
        onSelectedItemChanged,
        looping
            ? new ListWheelChildLoopingListDelegate(children)
            : new ListWheelChildListDelegate(children),
        selectionOverlay,
        diameterRatio,
        backgroundColor,
        offAxisFraction,
        useMagnifier,
        magnification,
        scrollController,
        squeeze,
        changeReportingBehavior,
        key)
    {
    }

    /// <summary>Creates a picker whose children are built lazily.</summary>
    public static CupertinoPicker Builder(
        double itemExtent,
        Action<int>? onSelectedItemChanged,
        NullableIndexedWidgetBuilder itemBuilder,
        int? childCount = null,
        double diameterRatio = DefaultDiameterRatio,
        CupertinoDynamicColor? backgroundColor = null,
        double offAxisFraction = 0.0,
        bool useMagnifier = false,
        double magnification = 1.0,
        FixedExtentScrollController? scrollController = null,
        double squeeze = DefaultSqueeze,
        ChangeReportingBehavior changeReportingBehavior = ChangeReportingBehavior.OnScrollUpdate,
        Key? key = null)
    {
        return Builder(
            itemExtent,
            onSelectedItemChanged,
            itemBuilder,
            new CupertinoPickerDefaultSelectionOverlay(),
            childCount,
            diameterRatio,
            backgroundColor,
            offAxisFraction,
            useMagnifier,
            magnification,
            scrollController,
            squeeze,
            changeReportingBehavior,
            key);
    }

    /// <summary>Creates a lazy picker with an explicitly supplied (or removed) selection overlay.</summary>
    public static CupertinoPicker Builder(
        double itemExtent,
        Action<int>? onSelectedItemChanged,
        NullableIndexedWidgetBuilder itemBuilder,
        Widget? selectionOverlay,
        int? childCount = null,
        double diameterRatio = DefaultDiameterRatio,
        CupertinoDynamicColor? backgroundColor = null,
        double offAxisFraction = 0.0,
        bool useMagnifier = false,
        double magnification = 1.0,
        FixedExtentScrollController? scrollController = null,
        double squeeze = DefaultSqueeze,
        ChangeReportingBehavior changeReportingBehavior = ChangeReportingBehavior.OnScrollUpdate,
        Key? key = null)
    {
        return new CupertinoPicker(
            itemExtent,
            onSelectedItemChanged,
            new ListWheelChildBuilderDelegate(itemBuilder, childCount),
            selectionOverlay,
            diameterRatio,
            backgroundColor,
            offAxisFraction,
            useMagnifier,
            magnification,
            scrollController,
            squeeze,
            changeReportingBehavior,
            key);
    }

    private CupertinoPicker(
        double itemExtent,
        Action<int>? onSelectedItemChanged,
        ListWheelChildDelegate childDelegate,
        Widget? selectionOverlay,
        double diameterRatio,
        CupertinoDynamicColor? backgroundColor,
        double offAxisFraction,
        bool useMagnifier,
        double magnification,
        FixedExtentScrollController? scrollController,
        double squeeze,
        ChangeReportingBehavior changeReportingBehavior,
        Key? key) : base(key)
    {
        if (!(diameterRatio > 0.0))
        {
            throw new ArgumentOutOfRangeException(
                nameof(diameterRatio),
                RenderListWheelViewport.DiameterRatioZeroMessage);
        }

        if (!(magnification > 0.0))
        {
            throw new ArgumentOutOfRangeException(nameof(magnification));
        }

        if (!(itemExtent > 0.0))
        {
            throw new ArgumentOutOfRangeException(nameof(itemExtent));
        }

        if (!(squeeze > 0.0))
        {
            throw new ArgumentOutOfRangeException(nameof(squeeze));
        }

        DiameterRatio = diameterRatio;
        BackgroundColor = backgroundColor;
        OffAxisFraction = offAxisFraction;
        UseMagnifier = useMagnifier;
        Magnification = magnification;
        ScrollController = scrollController;
        ItemExtent = itemExtent;
        Squeeze = squeeze;
        ChangeReportingBehavior = changeReportingBehavior;
        OnSelectedItemChanged = onSelectedItemChanged;
        ChildDelegate = childDelegate;
        SelectionOverlay = selectionOverlay;
    }

    public double DiameterRatio { get; }

    public CupertinoDynamicColor? BackgroundColor { get; }

    public double OffAxisFraction { get; }

    public bool UseMagnifier { get; }

    public double Magnification { get; }

    public FixedExtentScrollController? ScrollController { get; }

    public double ItemExtent { get; }

    public double Squeeze { get; }

    public ChangeReportingBehavior ChangeReportingBehavior { get; }

    public Action<int>? OnSelectedItemChanged { get; }

    public ListWheelChildDelegate ChildDelegate { get; }

    public Widget? SelectionOverlay { get; }

    public override State CreateState() => new CupertinoPickerState();

    internal sealed class CupertinoPickerState : State
    {
        private int _lastHapticIndex;
        private int? _lastMiddlePosition;
        private FixedExtentScrollController? _controller;
        private bool _enableHapticFeedback = true;

        private CupertinoPicker Current => (CupertinoPicker)StateWidget;

        internal FixedExtentScrollController EffectiveController => Current.ScrollController ?? _controller!;

        public override void InitState()
        {
            base.InitState();
            if (Current.ScrollController is null)
            {
                _controller = new FixedExtentScrollController();
            }

            _lastHapticIndex = EffectiveController.InitialItem;
            EffectiveController.AddListener(HandleScroll);
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var oldPicker = (CupertinoPicker)oldWidget;
            if (Current.ScrollController is not null && oldPicker.ScrollController is null)
            {
                _controller?.Dispose();
                _controller = null;
                Current.ScrollController.AddListener(HandleScroll);
            }
            else if (Current.ScrollController is null && oldPicker.ScrollController is not null)
            {
                oldPicker.ScrollController.RemoveListener(HandleScroll);
                _controller = new FixedExtentScrollController();
                _controller.AddListener(HandleScroll);
            }

            base.DidUpdateWidget(oldWidget);
        }

        public override void Dispose()
        {
            _controller?.Dispose();
            Current.ScrollController?.RemoveListener(HandleScroll);
            base.Dispose();
        }

        public override Widget Build(BuildContext context)
        {
            TextStyle textStyle = CupertinoTheme.Of(context).TextTheme.PickerTextStyle;
            Color? backgroundColor = Current.BackgroundColor is null
                ? null
                : CupertinoDynamicColor.Resolve(Current.BackgroundColor, context);
            List<Widget> stackChildren =
            [
                new Positioned(
                    left: 0.0,
                    top: 0.0,
                    right: 0.0,
                    bottom: 0.0,
                    child: new CupertinoPickerSemantics(
                        EffectiveController,
                        new ListWheelScrollView(
                            itemExtent: Current.ItemExtent,
                            childDelegate: new CupertinoPickerListWheelChildDelegateWrapper(
                                Current.ChildDelegate,
                                HandleChildTap),
                            controller: EffectiveController,
                            physics: new FixedExtentScrollPhysics(),
                            diameterRatio: Current.DiameterRatio,
                            offAxisFraction: Current.OffAxisFraction,
                            useMagnifier: Current.UseMagnifier,
                            magnification: Current.Magnification,
                            overAndUnderCenterOpacity: OverAndUnderCenterOpacity,
                            squeeze: Current.Squeeze,
                            onSelectedItemChanged: Current.OnSelectedItemChanged,
                            dragStartBehavior: DragStartBehavior.Down,
                            changeReportingBehavior: Current.ChangeReportingBehavior))),
            ];
            if (Current.SelectionOverlay is not null)
            {
                stackChildren.Add(BuildSelectionOverlay(Current.SelectionOverlay));
            }

            Widget result = new DefaultTextStyle(
                textStyle,
                new Stack(children: stackChildren));

            return new DecoratedBox(
                decoration: new BoxDecoration(Color: backgroundColor),
                child: result);
        }

        private void HandleHapticFeedback(int index)
        {
            if (!_enableHapticFeedback || PlatformDefaults.TargetPlatform != TargetPlatform.IOS)
            {
                return;
            }

            if (index == _lastHapticIndex)
            {
                return;
            }

            _lastHapticIndex = index;
            _ = HapticFeedback.SelectionClick();
            _ = SystemSound.Play(SystemSoundType.Tick);
        }

        private void HandleScroll()
        {
            int index = EffectiveController.SelectedItem;
            double fractionalOffset = EffectiveController.Offset / Current.ItemExtent;
            int currentPosition = (int)Math.Floor(fractionalOffset);
            double currentItemOffset = fractionalOffset - index;
            if (currentPosition != _lastMiddlePosition || Math.Abs(currentItemOffset) <= 0.1)
            {
                HandleHapticFeedback(index);
            }

            _lastMiddlePosition = currentPosition;
        }

        private async void HandleChildTap(int index)
        {
            _enableHapticFeedback = false;
            await EffectiveController.AnimateToItem(
                index,
                TapToScrollDuration,
                Curves.EaseInOut);

            // Dart's `_CupertinoPickerState._handleTap` continuation is protected by the widget lifetime;
            // C#'s `async void` continuation can outlive the picker, and reading `SelectedItem` after the
            // scroll view detached would throw into an unobserved task.
            if (!Mounted || !EffectiveController.HasClients)
            {
                return;
            }

            _enableHapticFeedback = true;
            _lastHapticIndex = EffectiveController.SelectedItem;
        }

        private Widget BuildSelectionOverlay(Widget selectionOverlay)
        {
            double height = Current.ItemExtent * Current.Magnification;
            return new IgnorePointer(
                child: new Center(
                    child: new ConstrainedBox(
                        BoxConstraints.Expand(height: height),
                        selectionOverlay)));
        }
    }
}

/// <summary>The iOS 14-style gray rounded selection overlay used by <see cref="CupertinoPicker"/>.</summary>
public sealed class CupertinoPickerDefaultSelectionOverlay : StatelessWidget
{
    internal const double DefaultHorizontalMargin = 9.0;
    internal const double DefaultRadius = 8.0;

    public CupertinoPickerDefaultSelectionOverlay(
        bool capStartEdge = true,
        bool capEndEdge = true,
        CupertinoDynamicColor? background = null,
        Key? key = null) : base(key)
    {
        CapStartEdge = capStartEdge;
        CapEndEdge = capEndEdge;
        Background = background ?? CupertinoColors.TertiarySystemFill;
    }

    public bool CapStartEdge { get; }

    public bool CapEndEdge { get; }

    public CupertinoDynamicColor Background { get; }

    public override Widget Build(BuildContext context)
    {
        double startRadius = CapStartEdge ? DefaultRadius : 0.0;
        double endRadius = CapEndEdge ? DefaultRadius : 0.0;
        return new Container(
            margin: EdgeInsetsGeometry.DirectionalOnly(
                start: CapStartEdge ? DefaultHorizontalMargin : 0.0,
                end: CapEndEdge ? DefaultHorizontalMargin : 0.0),
            decoration: new ShapeDecoration(
                Shape: new RoundedSuperellipseBorder(
                    borderRadius: BorderRadiusDirectional.Only(
                        topStart: startRadius,
                        topEnd: endRadius,
                        bottomEnd: endRadius,
                        bottomStart: startRadius)),
                Color: CupertinoDynamicColor.Resolve(Background, context)));
    }
}

internal sealed class CupertinoPickerSemantics : SingleChildRenderObjectWidget
{
    public CupertinoPickerSemantics(
        FixedExtentScrollController scrollController,
        Widget? child = null,
        Key? key = null) : base(child, key)
    {
        ScrollController = scrollController;
    }

    public FixedExtentScrollController ScrollController { get; }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderCupertinoPickerSemantics(
            ScrollController,
            Directionality.Of(context));
    }

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        var semantics = (RenderCupertinoPickerSemantics)renderObject;
        semantics.TextDirection = Directionality.Of(context);
        semantics.Controller = ScrollController;
    }
}

internal sealed class RenderCupertinoPickerSemantics : RenderProxyBox
{
    private FixedExtentScrollController _controller;
    private TextDirection _textDirection;
    private int _currentIndex;

    public RenderCupertinoPickerSemantics(
        FixedExtentScrollController controller,
        TextDirection textDirection)
    {
        _controller = controller;
        _textDirection = textDirection;
        _currentIndex = controller.InitialItem;
    }

    public FixedExtentScrollController Controller
    {
        get => _controller;
        set
        {
            if (ReferenceEquals(value, _controller))
            {
                return;
            }

            if (Attached)
            {
                _controller.RemoveListener(HandleScrollUpdate);
                value.AddListener(HandleScrollUpdate);
            }

            _controller = value;
            _currentIndex = value.InitialItem;
            MarkNeedsSemanticsUpdate();
        }
    }

    public TextDirection TextDirection
    {
        get => _textDirection;
        set
        {
            if (value == _textDirection)
            {
                return;
            }

            _textDirection = value;
            MarkNeedsSemanticsUpdate();
        }
    }

    protected override void OnAttach()
    {
        base.OnAttach();
        _controller.AddListener(HandleScrollUpdate);
    }

    protected override void OnDetach()
    {
        _controller.RemoveListener(HandleScrollUpdate);
        base.OnDetach();
    }

    protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
    {
        base.DescribeSemanticsConfiguration(configuration);
        configuration.IsSemanticBoundary = true;
        configuration.ExplicitChildNodes = true;
        configuration.TextDirection = _textDirection;
    }

    protected override void AssembleSemanticsNode(
        SemanticsNode node,
        SemanticsConfiguration config,
        IReadOnlyList<SemanticsNode> children)
    {
        var indexedChildren = new Dictionary<int, SemanticsNode>();
        foreach (SemanticsNode child in children)
        {
            CollectIndexedChildren(child, indexedChildren);
        }

        if (!indexedChildren.TryGetValue(_currentIndex, out SemanticsNode? current))
        {
            node.UpdateWith(config);
            return;
        }

        string currentLabel = current.Label ?? string.Empty;
        if (currentLabel.Length == 0)
        {
            node.UpdateWith(config);
            return;
        }

        config.Value = currentLabel;
        if (indexedChildren.TryGetValue(_currentIndex + 1, out SemanticsNode? next)
            && !string.IsNullOrEmpty(next.Label))
        {
            config.IncreasedValue = next.Label;
            config.AddActionHandler(SemanticsActions.Increase, HandleIncrease);
        }

        if (indexedChildren.TryGetValue(_currentIndex - 1, out SemanticsNode? previous)
            && !string.IsNullOrEmpty(previous.Label))
        {
            config.DecreasedValue = previous.Label;
            config.AddActionHandler(SemanticsActions.Decrease, HandleDecrease);
        }

        node.UpdateWith(config);
    }

    private static void CollectIndexedChildren(
        SemanticsNode node,
        Dictionary<int, SemanticsNode> indexedChildren)
    {
        if (node.IndexInParent is int index)
        {
            indexedChildren[index] = node;
        }

        foreach (SemanticsNode child in node.Children)
        {
            CollectIndexedChildren(child, indexedChildren);
        }
    }

    private void HandleIncrease() => _controller.JumpToItem(_currentIndex + 1);

    private void HandleDecrease() => _controller.JumpToItem(_currentIndex - 1);

    private void HandleScrollUpdate()
    {
        if (!_controller.HasClients || _controller.SelectedItem == _currentIndex)
        {
            return;
        }

        _currentIndex = _controller.SelectedItem;
        MarkNeedsSemanticsUpdate();
    }
}

internal sealed class CupertinoPickerListWheelChildDelegateWrapper : ListWheelChildDelegate
{
    private readonly ListWheelChildDelegate _wrapped;
    private readonly Action<int> _onTappedChild;

    public CupertinoPickerListWheelChildDelegateWrapper(
        ListWheelChildDelegate wrapped,
        Action<int> onTappedChild)
    {
        _wrapped = wrapped;
        _onTappedChild = onTappedChild;
    }

    public override int? EstimatedChildCount => _wrapped.EstimatedChildCount;

    public override Widget? Build(BuildContext context, int index)
    {
        Widget? child = _wrapped.Build(context, index);
        return child is null
            ? null
            : new GestureDetector(
                behavior: HitTestBehavior.Translucent,
                excludeFromSemantics: true,
                onTap: () => _onTappedChild(index),
                child: child);
    }

    public override bool ShouldRebuild(ListWheelChildDelegate oldDelegate)
    {
        return _wrapped.ShouldRebuild(
            ((CupertinoPickerListWheelChildDelegateWrapper)oldDelegate)._wrapped);
    }

    public override int TrueIndexOf(int index) => _wrapped.TrueIndexOf(index);
}
