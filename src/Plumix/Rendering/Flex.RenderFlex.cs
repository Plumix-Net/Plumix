using System.Diagnostics;
using Avalonia;
using Plumix.Foundation;
using Plumix.Painting;
using Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/flex.dart

namespace Plumix.Rendering;

/// <summary>
/// Displays its children in a one-dimensional array.
/// </summary>
public class RenderFlex : RenderBox, IRenderBoxContainerDefaultsMixin<RenderBox, FlexParentData>, IRenderObjectContainer
{
    private readonly RenderBoxContainerDefaultsMixin<RenderBox, FlexParentData> _mixin1;

    public RenderFlex(
        List<RenderBox>? children = null,
        Axis direction = Axis.Horizontal,
        MainAxisSize mainAxisSize = MainAxisSize.Max,
        MainAxisAlignment mainAxisAlignment = MainAxisAlignment.Start,
        CrossAxisAlignment crossAxisAlignment = CrossAxisAlignment.Center,
        TextDirection? textDirection = null,
        VerticalDirection verticalDirection = VerticalDirection.Down,
        TextBaseline? textBaseline = null,
        Clip clipBehavior = Clip.None,
        double spacing = 0.0)
    {
        if (Constants.KDebugMode && !(spacing >= 0.0))
        {
            throw new AssertionError();
        }

        _direction = direction;
        _mainAxisSize = mainAxisSize;
        _mainAxisAlignment = mainAxisAlignment;
        _crossAxisAlignment = crossAxisAlignment;
        _textDirection = textDirection;
        _verticalDirection = verticalDirection;
        _textBaseline = textBaseline;
        _clipBehavior = clipBehavior;
        _spacing = spacing;

        _mixin1 = new RenderBoxContainerDefaultsMixin<RenderBox, FlexParentData>(this);

        if (children != null)
            AddAll(children);
    }

    #region Properties

    private Axis _direction;

    /// The direction to use as the main axis.
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

    private MainAxisAlignment _mainAxisAlignment;

    /// How the children should be placed along the main axis.
    public MainAxisAlignment MainAxisAlignment
    {
        get => _mainAxisAlignment;
        set
        {
            if (_mainAxisAlignment == value)
            {
                return;
            }

            _mainAxisAlignment = value;
            MarkNeedsLayout();
        }
    }

    private MainAxisSize _mainAxisSize;

    /// How much space should be occupied in the main axis.
    public MainAxisSize MainAxisSize
    {
        get => _mainAxisSize;
        set
        {
            if (_mainAxisSize == value)
            {
                return;
            }

            _mainAxisSize = value;
            MarkNeedsLayout();
        }
    }

    private CrossAxisAlignment _crossAxisAlignment;

    /// How the children should be placed along the cross axis.
    public CrossAxisAlignment CrossAxisAlignment
    {
        get => _crossAxisAlignment;
        set
        {
            if (_crossAxisAlignment == value)
            {
                return;
            }

            _crossAxisAlignment = value;
            MarkNeedsLayout();
        }
    }

    private TextDirection? _textDirection;

    /// Determines the order to lay children out horizontally and how to interpret
    /// `start` and `end` in the horizontal direction.
    public TextDirection? TextDirection
    {
        get => _textDirection;
        set
        {
            if (_textDirection != value)
            {
                _textDirection = value;
                MarkNeedsLayout();
            }
        }
    }

    private VerticalDirection _verticalDirection;

    /// Determines the order to lay children out vertically and how to interpret
    /// `start` and `end` in the vertical direction.
    public VerticalDirection VerticalDirection
    {
        get => _verticalDirection;
        set
        {
            if (_verticalDirection != value)
            {
                _verticalDirection = value;
                MarkNeedsLayout();
            }
        }
    }

    private TextBaseline? _textBaseline;

    /// If aligning items according to their baseline, which baseline to use.
    ///
    /// Must not be null if [CrossAxisAlignment] is [CrossAxisAlignment.Baseline].
    public TextBaseline? TextBaseline
    {
        get => _textBaseline;
        set
        {
            if (Constants.KDebugMode && _crossAxisAlignment == CrossAxisAlignment.Baseline && value == null)
            {
                throw new AssertionError();
            }

            if (_textBaseline != value)
            {
                _textBaseline = value;
                MarkNeedsLayout();
            }
        }
    }

    private Clip _clipBehavior = Clip.None;

    /// Defaults to [Clip.None].
    public Clip ClipBehavior
    {
        get => _clipBehavior;
        set
        {
            if (value == _clipBehavior)
            {
                return;
            }

            _clipBehavior = value;
            MarkNeedsPaint();
            MarkNeedsSemanticsUpdate();
        }
    }

    private double _spacing;

    /// How much space to place between children in the main axis.
    public double Spacing
    {
        get => _spacing;
        set
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (_spacing == value)
                return;

            _spacing = value;

            MarkNeedsLayout();
        }
    }

    // Set during layout if overflow occurred on the main axis.
    private double _overflow;

    // Dart mixes `DebugOverflowIndicatorMixin` in; C# has no mixins, so its state lives here.
    private readonly DebugOverflowIndicator _debugOverflowIndicator = new();

    // Check whether any meaningful overflow is present. Values below an epsilon
    // are treated as not overflowing.
    public bool _hasOverflow => _overflow > Constants.PrecisionErrorTolerance;

    private bool IsBaselineAligned => CrossAxisAlignment switch
    {
        CrossAxisAlignment.Baseline => Direction switch
        {
            Axis.Horizontal => true,
            Axis.Vertical => false,

            _ => throw new ArgumentOutOfRangeException()
        },

        CrossAxisAlignment.Start => false,
        CrossAxisAlignment.End => false,
        CrossAxisAlignment.Center => false,
        CrossAxisAlignment.Stretch => false,

        _ => throw new ArgumentOutOfRangeException()
    };

    #endregion

    /// Dart's `_debugHasNecessaryDirections`. Reports the resolution failures that
    /// Flutter raises through `assert`s before laying children out.
    private void DebugCheckNecessaryDirections()
    {
        if (!Constants.KDebugMode || DebugCheckingIntrinsics)
        {
            return;
        }

        if (FirstChild != null && !ReferenceEquals(LastChild, FirstChild))
        {
            // i.e. there's more than one child
            if (Direction == Axis.Horizontal && TextDirection == null)
            {
                throw new FlutterError(
                    $"Horizontal {GetType().Name} with multiple children has a null textDirection, "
                    + "so the layout order is undefined.");
            }
        }

        if (MainAxisAlignment is MainAxisAlignment.Start or MainAxisAlignment.End)
        {
            if (Direction == Axis.Horizontal && TextDirection == null)
            {
                throw new FlutterError(
                    $"Horizontal {GetType().Name} with {MainAxisAlignment} has a null textDirection, "
                    + "so the alignment cannot be resolved.");
            }
        }

        if (CrossAxisAlignment is CrossAxisAlignment.Start or CrossAxisAlignment.End)
        {
            if (Direction == Axis.Vertical && TextDirection == null)
            {
                throw new FlutterError(
                    $"Vertical {GetType().Name} with {CrossAxisAlignment} has a null textDirection, "
                    + "so the alignment cannot be resolved.");
            }
        }
    }

    public override void SetupParentData(RenderObject child)
    {
        if (child.parentData is not FlexParentData)
        {
            child.parentData = new FlexParentData();
        }
    }

    private static int _getFlex(RenderBox child)
    {
        var childParentData = (FlexParentData)child.parentData!;

        return childParentData.flex ?? 0;
    }

    private static FlexFit _getFit(RenderBox child)
    {
        var childParentData = (FlexParentData)child.parentData!;

        return childParentData.fit ?? FlexFit.Tight;
    }

    private double _getCrossSize(Size size) => _direction switch
    {
        Axis.Horizontal => size.Height,
        Axis.Vertical => size.Width,

        _ => throw new ArgumentOutOfRangeException()
    };

    private double _getMainSize(Size size) => _direction switch
    {
        Axis.Horizontal => size.Width,
        Axis.Vertical => size.Height,

        _ => throw new ArgumentOutOfRangeException()
    };

    // flipMainAxis is used to decide whether to lay out
    // left-to-right/top-to-bottom (false), or right-to-left/bottom-to-top
    // (true). Returns false in cases when the layout direction does not matter
    // (for instance, there is no child).
    private bool _flipMainAxis =>
        FirstChild != null &&
        Direction switch
        {
            Axis.Horizontal => TextDirection switch
            {
                null => false,
                UI.TextDirection.Ltr => false,
                UI.TextDirection.Rtl => true,

                _ => throw new ArgumentOutOfRangeException()
            },
            Axis.Vertical => VerticalDirection switch
            {
                VerticalDirection.Down => false,
                VerticalDirection.Up => true,

                _ => throw new ArgumentOutOfRangeException()
            },
            _ => throw new ArgumentOutOfRangeException()
        };

    private bool _flipCrossAxis =>
        FirstChild != null &&
        Direction switch
        {
            Axis.Vertical => TextDirection switch
            {
                null => false,
                UI.TextDirection.Ltr => false,
                UI.TextDirection.Rtl => true,

                _ => throw new ArgumentOutOfRangeException()
            },
            Axis.Horizontal => VerticalDirection switch
            {
                VerticalDirection.Down => false,
                VerticalDirection.Up => true,

                _ => throw new ArgumentOutOfRangeException()
            },
            _ => throw new ArgumentOutOfRangeException()
        };

    private BoxConstraints _constraintsForNonFlexChild(BoxConstraints constraints)
    {
        bool fillCrossAxis = CrossAxisAlignment switch
        {
            CrossAxisAlignment.Stretch => true,
            CrossAxisAlignment.Start => false,
            CrossAxisAlignment.Center => false,
            CrossAxisAlignment.End => false,
            CrossAxisAlignment.Baseline => false,

            _ => throw new ArgumentOutOfRangeException()
        };

        return _direction switch
        {
            Axis.Horizontal =>
                fillCrossAxis
                    ? BoxConstraints.TightFor(height: constraints.MaxHeight)
                    : new BoxConstraints(MaxHeight: constraints.MaxHeight),
            Axis.Vertical =>
                fillCrossAxis
                    ? BoxConstraints.TightFor(width: constraints.MaxWidth)
                    : new BoxConstraints(MaxWidth: constraints.MaxWidth),

            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private BoxConstraints _constraintsForFlexChild(
        RenderBox child,
        BoxConstraints constraints,
        double maxChildExtent)
    {
        Debug.Assert(_getFlex(child) > 0.0);
        Debug.Assert(maxChildExtent >= 0.0);

        double minChildExtent = _getFit(child) switch
        {
            FlexFit.Tight => maxChildExtent,
            FlexFit.Loose => 0.0,

            _ => throw new ArgumentOutOfRangeException()
        };

        bool fillCrossAxis = CrossAxisAlignment switch
        {
            CrossAxisAlignment.Stretch => true,
            CrossAxisAlignment.Start => false,
            CrossAxisAlignment.Center => false,
            CrossAxisAlignment.End => false,
            CrossAxisAlignment.Baseline => false,

            _ => throw new ArgumentOutOfRangeException()
        };

        return _direction switch
        {
            Axis.Horizontal => new BoxConstraints(
                MinWidth: minChildExtent,
                MaxWidth: maxChildExtent,
                MinHeight: fillCrossAxis ? constraints.MaxHeight : 0.0,
                MaxHeight: constraints.MaxHeight),
            Axis.Vertical => new BoxConstraints(
                MinWidth: fillCrossAxis ? constraints.MaxWidth : 0.0,
                MaxWidth: constraints.MaxWidth,
                MinHeight: minChildExtent,
                MaxHeight: maxChildExtent),

            _ => throw new ArgumentOutOfRangeException()
        };
    }

    protected override double ComputeMinIntrinsicWidth(double height)
    {
        return GetIntrinsicSize(
            Axis.Horizontal,
            height,
            static (child, extent) => child.GetMinIntrinsicWidth(extent));
    }

    protected override double ComputeMaxIntrinsicWidth(double height)
    {
        return GetIntrinsicSize(
            Axis.Horizontal,
            height,
            static (child, extent) => child.GetMaxIntrinsicWidth(extent));
    }

    protected override double ComputeMinIntrinsicHeight(double width)
    {
        return GetIntrinsicSize(
            Axis.Vertical,
            width,
            static (child, extent) => child.GetMinIntrinsicHeight(extent));
    }

    protected override double ComputeMaxIntrinsicHeight(double width)
    {
        return GetIntrinsicSize(
            Axis.Vertical,
            width,
            static (child, extent) => child.GetMaxIntrinsicHeight(extent));
    }

    private double GetIntrinsicSize(
        Axis sizingDirection,
        double extent,
        Func<RenderBox, double, double> childSize)
    {
        if (Direction == sizingDirection)
        {
            // INTRINSIC MAIN SIZE
            // Intrinsic main size is the smallest size the flex container can take
            // while maintaining the min/max-content contributions of its flex items.
            double totalFlex = 0.0;
            double inflexibleSpace = Spacing * (ChildCount - 1);
            double maxFlexFractionSoFar = 0.0;
            foreach (RenderBox child in EnumerateChildren())
            {
                int flex = _getFlex(child);
                totalFlex += flex;
                if (flex > 0)
                {
                    double flexFraction = childSize(child, extent) / flex;
                    maxFlexFractionSoFar = Math.Max(maxFlexFractionSoFar, flexFraction);
                }
                else
                {
                    inflexibleSpace += childSize(child, extent);
                }
            }

            return maxFlexFractionSoFar * totalFlex + inflexibleSpace;
        }

        // INTRINSIC CROSS SIZE
        // Intrinsic cross size is the max of the intrinsic cross sizes of the
        // children, after the flexible children are fit into the main axis extent.
        bool isHorizontal = Direction == Axis.Horizontal;

        Size LayoutChild(RenderBox child, BoxConstraints childConstraints)
        {
            double mainAxisSizeFromConstraints = isHorizontal
                ? childConstraints.MaxWidth
                : childConstraints.MaxHeight;

            // A infinite mainAxisSizeFromConstraints means the child is flexible
            // (or the given `extent` is infinite).
            double maxMainAxisSize = double.IsFinite(mainAxisSizeFromConstraints)
                ? mainAxisSizeFromConstraints
                : isHorizontal
                    ? child.GetMaxIntrinsicWidth(double.PositiveInfinity)
                    : child.GetMaxIntrinsicHeight(double.PositiveInfinity);

            return isHorizontal
                ? new Size(maxMainAxisSize, childSize(child, maxMainAxisSize))
                : new Size(childSize(child, maxMainAxisSize), maxMainAxisSize);
        }

        BoxConstraints constraints = isHorizontal
            ? new BoxConstraints(MaxWidth: extent)
            : new BoxConstraints(MaxHeight: extent);

        return _computeSizes(
            constraints,
            LayoutChild,
            ChildLayoutHelper.GetDryBaseline).axisSize.crossAxisExtent;
    }

    protected override double? ComputeDistanceToActualBaseline(TextBaseline baseline) => _direction switch
    {
        Axis.Horizontal => _mixin1.DefaultComputeDistanceToHighestActualBaseline(baseline),
        Axis.Vertical => _mixin1.DefaultComputeDistanceToFirstActualBaseline(baseline),

        _ => throw new ArgumentOutOfRangeException()
    };

    protected override Size ComputeDryLayout(BoxConstraints constraints)
    {
        FlutterError? constraintsError =
            DebugCheckConstraints(constraints, reportParentConstraints: false);
        if (constraintsError != null)
        {
            DebugCannotComputeDryLayout(constraintsError.Message);
            return new Size();
        }

        return _computeSizes(
            constraints,
            ChildLayoutHelper.DryLayoutChild,
            ChildLayoutHelper.GetDryBaseline).axisSize.ToSize(Direction);
    }

    protected override double? ComputeDryBaseline(BoxConstraints constraints, TextBaseline baseline)
    {
        _LayoutSizes sizes = _computeSizes(
            constraints,
            ChildLayoutHelper.DryLayoutChild,
            ChildLayoutHelper.GetDryBaseline);

        if (IsBaselineAligned)
        {
            return sizes.baselineOffset;
        }

        return Direction switch
        {
            Axis.Horizontal => ComputeDryDistanceToHighestBaseline(constraints, baseline, sizes),
            Axis.Vertical => ComputeDryDistanceToFirstBaseline(constraints, baseline, sizes),

            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private double? ComputeDryDistanceToHighestBaseline(
        BoxConstraints constraints,
        TextBaseline baseline,
        _LayoutSizes sizes)
    {
        BoxConstraints nonFlexConstraints = _constraintsForNonFlexChild(constraints);
        bool flipCrossAxis = _flipCrossAxis;
        double? minBaseline = null;

        foreach (RenderBox child in EnumerateChildren())
        {
            BoxConstraints childConstraints = GetDryChildConstraints(
                child,
                constraints,
                sizes,
                nonFlexConstraints);
            double? childBaseline = child.GetDryBaseline(childConstraints, baseline);
            if (childBaseline == null)
            {
                continue;
            }

            Size childSize = child.GetDryLayout(childConstraints);
            double childCrossPosition = _getChildCrossAxisOffset(
                CrossAxisAlignment,
                sizes.axisSize.crossAxisExtent - _getCrossSize(childSize),
                flipCrossAxis);
            double candidate = childBaseline.Value + childCrossPosition;
            minBaseline = minBaseline == null ? candidate : Math.Min(minBaseline.Value, candidate);
        }

        return minBaseline;
    }

    private double? ComputeDryDistanceToFirstBaseline(
        BoxConstraints constraints,
        TextBaseline baseline,
        _LayoutSizes sizes)
    {
        BoxConstraints nonFlexConstraints = _constraintsForNonFlexChild(constraints);
        double remainingSpace = Math.Max(0.0, sizes.mainAxisFreeSpace);
        bool flipMainAxis = _flipMainAxis;
        (double leadingSpace, double betweenSpace) = _distributeSpace(
            MainAxisAlignment,
            remainingSpace,
            ChildCount,
            flipMainAxis,
            Spacing);

        var mainPositions = new Dictionary<RenderBox, double>();
        RenderBox? startChild = flipMainAxis ? LastChild : FirstChild;
        Func<RenderBox, RenderBox?> nextChildPaintOrder = flipMainAxis ? ChildBefore : ChildAfter;
        double position = leadingSpace;
        for (RenderBox? child = startChild; child != null; child = nextChildPaintOrder(child))
        {
            mainPositions[child] = position;
            Size childSize = child.GetDryLayout(
                GetDryChildConstraints(child, constraints, sizes, nonFlexConstraints));
            position += _getMainSize(childSize) + betweenSpace;
        }

        foreach (RenderBox child in EnumerateChildren())
        {
            BoxConstraints childConstraints = GetDryChildConstraints(
                child,
                constraints,
                sizes,
                nonFlexConstraints);
            double? childBaseline = child.GetDryBaseline(childConstraints, baseline);
            if (childBaseline != null)
            {
                return childBaseline.Value
                       + (mainPositions.TryGetValue(child, out double mainPosition) ? mainPosition : leadingSpace);
            }
        }

        return null;
    }

    private BoxConstraints GetDryChildConstraints(
        RenderBox child,
        BoxConstraints constraints,
        _LayoutSizes sizes,
        BoxConstraints nonFlexConstraints)
    {
        int flex = _getFlex(child);
        return sizes.spacePerFlex.HasValue && flex > 0
            ? _constraintsForFlexChild(child, constraints, sizes.spacePerFlex.Value * flex)
            : nonFlexConstraints;
    }

    private IEnumerable<RenderBox> EnumerateChildren()
    {
        for (RenderBox? child = FirstChild; child != null; child = ChildAfter(child))
        {
            yield return child;
        }
    }

    private _LayoutSizes _computeSizes(
        BoxConstraints constraints,
        Func<RenderBox, BoxConstraints, Size> layoutChild,
        Func<RenderBox, BoxConstraints, TextBaseline, double?> getBaseline)
    {
        DebugCheckNecessaryDirections();

        // Determine used flex factor, size inflexible items, calculate free space.
        double maxMainSize = _getMainSize(constraints.Biggest);
        bool canFlex = double.IsFinite(maxMainSize);

        BoxConstraints nonFlexChildConstraints = _constraintsForNonFlexChild(constraints);

        // Null indicates the children are not baseline aligned.
        TextBaseline? textBaseline = IsBaselineAligned
            ? TextBaseline ?? throw new FlutterError(
                "To use CrossAxisAlignment.baseline, you must also specify which baseline to use "
                + "using the \"textBaseline\" argument.")
            : null;

        // The first pass lays out non-flex children and computes total flex.
        int totalFlex = 0;
        RenderBox? firstFlexChild = null;

        _AscentDescent accumulatedAscentDescent = _AscentDescent.None;

        // Initially, accumulatedSize is the sum of the spaces between children in the main axis.
        _AxisSize accumulatedSize = new(new Size(Spacing * (ChildCount - 1), 0.0));

        for (RenderBox? child = FirstChild; child != null; child = ChildAfter(child))
        {
            int flex;

            if (canFlex && (flex = _getFlex(child)) > 0)
            {
                totalFlex += flex;
                firstFlexChild ??= child;
            }
            else
            {
                var childSize = _AxisSize.FromSize(
                    size: layoutChild(child, nonFlexChildConstraints),
                    direction: Direction);

                accumulatedSize += childSize;

                // Baseline-aligned children contributes to the cross axis extent separately.
                double? baselineOffset = textBaseline == null
                    ? null
                    : getBaseline(child, nonFlexChildConstraints, textBaseline.Value);

                accumulatedAscentDescent += _AscentDescent.Create(
                    baselineOffset: baselineOffset,
                    crossSize: childSize.crossAxisExtent);
            }
        }

        Debug.Assert((totalFlex == 0) == (firstFlexChild == null));

        // If we are given infinite space there's no need for this extra step.
        Debug.Assert(firstFlexChild == null || canFlex);

        // The second pass distributes free space to flexible children.
        double flexSpace = Math.Max(0.0, maxMainSize - accumulatedSize.mainAxisExtent);
        double spacePerFlex = flexSpace / totalFlex;
        for (
            RenderBox? child = firstFlexChild;
            child != null && totalFlex > 0;
            child = ChildAfter(child))
        {
            int flex = _getFlex(child);
            if (flex == 0)
            {
                continue;
            }

            totalFlex -= flex;
            Debug.Assert(double.IsFinite(spacePerFlex));

            double maxChildExtent = spacePerFlex * flex;

            Debug.Assert(_getFit(child) == FlexFit.Loose || maxChildExtent < double.PositiveInfinity);

            BoxConstraints childConstraints = _constraintsForFlexChild(
                child,
                constraints,
                maxChildExtent);

            var childSize = _AxisSize.FromSize(
                size: layoutChild(child, childConstraints),
                direction: Direction);

            accumulatedSize += childSize;
            double? baselineOffset = textBaseline == null
                ? null
                : getBaseline(child, childConstraints, textBaseline.Value);

            accumulatedAscentDescent += _AscentDescent.Create(
                baselineOffset: baselineOffset,
                crossSize: childSize.crossAxisExtent);
        }

        Debug.Assert(totalFlex == 0);

        // The overall height of baseline-aligned children contributes to the cross axis extent.
        accumulatedSize += accumulatedAscentDescent.AscentDescent switch
        {
            null => _AxisSize.Empty,
            var (ascent, descent) => _AxisSize.Create(
                mainAxisExtent: 0,
                crossAxisExtent: ascent + descent)
        };

        double idealMainSize = MainAxisSize switch
        {
            MainAxisSize.Max when double.IsFinite(maxMainSize) => maxMainSize,
            MainAxisSize.Max or MainAxisSize.Min => accumulatedSize.mainAxisExtent,

            _ => throw new ArgumentOutOfRangeException()
        };

        var constrainedSize = _AxisSize.Create(
            mainAxisExtent: idealMainSize,
            crossAxisExtent: accumulatedSize.crossAxisExtent).ApplyConstraints(constraints, Direction);

        return new _LayoutSizes(
            axisSize: constrainedSize,
            mainAxisFreeSpace: constrainedSize.mainAxisExtent - accumulatedSize.mainAxisExtent,
            baselineOffset: accumulatedAscentDescent.BaselineOffset,
            spacePerFlex: firstFlexChild == null ? null : spacePerFlex);
    }

    protected override void PerformLayout()
    {
        BoxConstraints constraints = Constraints;

        FlutterError? constraintsError =
            DebugCheckConstraints(constraints, reportParentConstraints: true);
        if (constraintsError != null)
        {
            throw constraintsError;
        }

        _LayoutSizes sizes = _computeSizes(
            constraints: constraints,
            layoutChild: ChildLayoutHelper.LayoutChild,
            getBaseline: ChildLayoutHelper.GetBaseline);

        double crossAxisExtent = sizes.axisSize.crossAxisExtent;

        Size = sizes.axisSize.ToSize(Direction);

        _overflow = Math.Max(0.0, -sizes.mainAxisFreeSpace);

        double remainingSpace = Math.Max(0.0, sizes.mainAxisFreeSpace);
        bool flipMainAxis = _flipMainAxis;
        bool flipCrossAxis = _flipCrossAxis;

        (double leadingSpace, double betweenSpace) = _distributeSpace(
            MainAxisAlignment,
            remainingSpace,
            ChildCount,
            flipMainAxis,
            Spacing);

        Func<RenderBox, RenderBox?> nextChild = flipMainAxis ? ChildBefore : ChildAfter;
        RenderBox? topLeftChild = flipMainAxis ? LastChild : FirstChild;

        double? baselineOffset = sizes.baselineOffset;

        Debug.Assert(
            baselineOffset == null ||
            (CrossAxisAlignment == CrossAxisAlignment.Baseline && Direction == Axis.Horizontal));

        // Position all children in visual order: starting from the top-left child and
        // work towards the child that's farthest away from the origin.
        double childMainPosition = leadingSpace;

        for (RenderBox? child = topLeftChild; child != null; child = nextChild(child))
        {
            double? childBaselineOffset = baselineOffset == null
                ? null
                : child.GetDistanceToBaseline(TextBaseline!.Value, onlyReal: true);
            bool baselineAlign = baselineOffset != null && childBaselineOffset != null;

            double childCrossPosition;
            if (baselineAlign)
            {
                childCrossPosition = baselineOffset!.Value - childBaselineOffset!.Value;
            }
            else if (CrossAxisAlignment == CrossAxisAlignment.Baseline && Direction == Axis.Horizontal)
            {
                // Children who report no baseline are top-aligned, regardless of
                // `VerticalDirection`: `flipCrossAxis` is intentionally ignored here.
                childCrossPosition = _getChildCrossAxisOffset(
                    CrossAxisAlignment.Start,
                    crossAxisExtent - _getCrossSize(child.Size),
                    flipped: false);
            }
            else
            {
                childCrossPosition = _getChildCrossAxisOffset(
                    CrossAxisAlignment,
                    crossAxisExtent - _getCrossSize(child.Size),
                    flipCrossAxis);
            }

            var childParentData = (FlexParentData)child.parentData!;

            childParentData.offset = Direction switch
            {
                Axis.Horizontal => new Point(childMainPosition, childCrossPosition),
                Axis.Vertical => new Point(childCrossPosition, childMainPosition),

                _ => throw new ArgumentOutOfRangeException()
            };

            childMainPosition += _getMainSize(child.Size) + betweenSpace;
        }
    }

    /// Dart's `_debugCheckConstraints`: returns the error a flex child with a
    /// non-zero flex factor causes under unbounded main-axis constraints, or null.
    private FlutterError? DebugCheckConstraints(
        BoxConstraints constraints,
        bool reportParentConstraints)
    {
        if (!Constants.KDebugMode)
        {
            return null;
        }

        double maxMainSize = _direction == Axis.Horizontal ? constraints.MaxWidth : constraints.MaxHeight;
        bool canFlex = double.IsFinite(maxMainSize);
        foreach (RenderBox child in EnumerateChildren())
        {
            int flex = _getFlex(child);
            if (flex <= 0)
            {
                continue;
            }

            if (canFlex || (MainAxisSize != MainAxisSize.Max && _getFit(child) != FlexFit.Tight))
            {
                continue;
            }

            string identity = _direction == Axis.Horizontal ? "row" : "column";
            string axis = _direction == Axis.Horizontal ? "horizontal" : "vertical";
            string dimension = _direction == Axis.Horizontal ? "width" : "height";

            var message = new System.Text.StringBuilder();
            message.Append($"RenderFlex children have non-zero flex but incoming {dimension} ");
            message.AppendLine("constraints are unbounded.");
            message.AppendLine(
                $"When a {identity} is in a parent that does not provide a finite {dimension} constraint, "
                + $"for example if it is in a {axis} scrollable, it will try to shrink-wrap its children "
                + $"along the {axis} axis. Setting a flex on a child (e.g. using Expanded) indicates that "
                + $"the child is to expand to fill the remaining space in the {axis} direction.");
            message.AppendLine(
                "These two directives are mutually exclusive. If a parent is to shrink-wrap its child, "
                + "the child cannot simultaneously expand to fit its parent.");
            message.AppendLine(
                "Consider setting mainAxisSize to MainAxisSize.Min and using FlexFit.Loose fits for the "
                + "flexible children (using Flexible rather than Expanded). This will allow the flexible "
                + "children to size themselves to less than the infinite remaining space they would "
                + "otherwise be forced to take, and then will cause the RenderFlex to shrink-wrap the "
                + "children rather than expanding to fit the maximum constraints provided by the parent.");
            message.AppendLine($"The affected RenderFlex is: {GetType().Name}");
            message.Append(DescribeUnboundedAncestor(reportParentConstraints));
            message.AppendLine("See also: https://flutter.dev/unbounded-constraints");

            return new FlutterError(message.ToString());
        }

        return null;
    }

    private string DescribeUnboundedAncestor(bool reportParentConstraints)
    {
        if (!reportParentConstraints)
        {
            return string.Empty;
        }

        RenderBox? node = this;
        while (!HasBoundedMainAxis(node) && node.Parent is RenderBox parentBox)
        {
            node = parentBox;
        }

        if (!HasBoundedMainAxis(node))
        {
            return string.Empty;
        }

        return "The nearest ancestor providing an unbounded width constraint is: "
               + $"{node.GetType().Name}{Environment.NewLine}";
    }

    private bool HasBoundedMainAxis(RenderBox node)
    {
        if (!node.HasBoxConstraints)
        {
            return false;
        }

        BoxConstraints nodeConstraints = node.CurrentBoxConstraints;
        return _direction == Axis.Horizontal
            ? nodeConstraints.HasBoundedWidth
            : nodeConstraints.HasBoundedHeight;
    }

    public override void Paint(PaintingContext ctx, Point offset)
    {
        if (!_hasOverflow)
        {
            DefaultPaint(ctx, offset);
            return;
        }

        // There's no point in drawing the children if we're empty.
        if (Size.IsEmpty)
        {
            return;
        }

        ctx.PushClipRect(
            new Rect(offset, Size),
            clippedContext => DefaultPaint(clippedContext, offset),
            ClipBehavior);

        Rect overflowChildRect = Direction switch
        {
            Axis.Horizontal => new Rect(0.0, 0.0, Size.Width + _overflow, 0.0),
            Axis.Vertical => new Rect(0.0, 0.0, 0.0, Size.Height + _overflow),

            _ => throw new ArgumentOutOfRangeException()
        };

        if (Constants.KDebugMode)
        {
            List<DiagnosticsNode> debugOverflowHints =
            [
                new ErrorDescription(
                    $"The overflowing {Diagnostics.ObjectRuntimeType(this)} has an orientation of {_direction}."),
                new ErrorDescription(
                    $"The edge of the {Diagnostics.ObjectRuntimeType(this)} that is overflowing has been marked "
                    + "in the rendering with a yellow and black striped pattern. This is "
                    + $"usually caused by the contents being too big for the {Diagnostics.ObjectRuntimeType(this)}."),
                new ErrorHint(
                    "Consider applying a flex factor (e.g. using an Expanded widget) to "
                    + $"force the children of the {Diagnostics.ObjectRuntimeType(this)} to fit within the available "
                    + "space instead of being sized to their natural size."),
                new ErrorHint(
                    "This is considered an error condition because it indicates that there "
                    + "is content that cannot be seen. If the content is legitimately bigger "
                    + "than the available space, consider clipping it with a ClipRect widget "
                    + "before putting it in the flex, or using a scrollable container rather "
                    + "than a Flex, like a ListView."),
            ];

            _debugOverflowIndicator.PaintOverflowIndicator(
                this,
                ctx,
                offset,
                new Rect(new Point(), Size),
                overflowChildRect,
                overflowHints: debugOverflowHints);
        }
    }

    protected override Rect? DescribeApproximatePaintClip(RenderObject? child) => ClipBehavior switch
    {
        Clip.None => null,
        Clip.HardEdge or Clip.AntiAlias or Clip.AntiAliasWithSaveLayer =>
            _hasOverflow ? new Rect(new Point(), Size) : null,

        _ => throw new ArgumentOutOfRangeException()
    };

    public override void VisitChildren(Action<RenderObject> visitor)
    {
        for (RenderBox? child = FirstChild; child != null; child = ChildAfter(child))
        {
            visitor(child);
        }
    }

    protected override bool HitTestChildren(BoxHitTestResult result, Point position)
    {
        return DefaultHitTestChildren(result, position);
    }

    private static (double leadingSpace, double betweenSpace) _distributeSpace(
        MainAxisAlignment mainAxisAlignment,
        double freeSpace,
        int itemCount,
        bool flipped,
        double spacing)
    {
        Debug.Assert(itemCount >= 0);

        return mainAxisAlignment switch
        {
            MainAxisAlignment.Start => flipped ? (freeSpace, spacing) : (0.0, spacing),

            MainAxisAlignment.End => _distributeSpace(
                MainAxisAlignment.Start,
                freeSpace,
                itemCount,
                !flipped,
                spacing),
            MainAxisAlignment.SpaceBetween when itemCount < 2 => _distributeSpace(
                MainAxisAlignment.Start,
                freeSpace,
                itemCount,
                flipped,
                spacing),
            MainAxisAlignment.SpaceAround when itemCount == 0 => _distributeSpace(
                MainAxisAlignment.Start,
                freeSpace,
                itemCount,
                flipped,
                spacing),

            MainAxisAlignment.Center => (freeSpace / 2.0, spacing),
            MainAxisAlignment.SpaceBetween => (0.0, freeSpace / (itemCount - 1) + spacing),
            MainAxisAlignment.SpaceAround => (freeSpace / itemCount / 2, freeSpace / itemCount + spacing),
            MainAxisAlignment.SpaceEvenly => (
                freeSpace / (itemCount + 1),
                freeSpace / (itemCount + 1) + spacing),

            _ => throw new ArgumentOutOfRangeException(nameof(mainAxisAlignment), mainAxisAlignment, null)
        };
    }

    private static double _getChildCrossAxisOffset(
        CrossAxisAlignment crossAxisAlignment,
        double freeSpace,
        bool flipped)
    {
        // This method should not be used to position baseline-aligned children.
        return crossAxisAlignment switch
        {
            CrossAxisAlignment.Stretch => 0.0,
            CrossAxisAlignment.Baseline => 0.0,
            CrossAxisAlignment.Start => flipped ? freeSpace : 0.0,
            CrossAxisAlignment.Center => freeSpace / 2,
            CrossAxisAlignment.End => _getChildCrossAxisOffset(
                CrossAxisAlignment.Start,
                freeSpace,
                !flipped),

            _ => throw new ArgumentOutOfRangeException(nameof(crossAxisAlignment), crossAxisAlignment, null)
        };
    }

    private readonly struct _AxisSize(Size size)
    {
        private readonly Size _size = size;

        public static readonly _AxisSize Empty = new(new Size());

        public double mainAxisExtent => _size.Width;

        public double crossAxisExtent => _size.Height;

        public static _AxisSize Create(double mainAxisExtent, double crossAxisExtent)
        {
            return new _AxisSize(new Size(mainAxisExtent, crossAxisExtent));
        }

        public static _AxisSize FromSize(Size size, Axis direction)
        {
            return new _AxisSize(_convert(size, direction));
        }

        public Size ToSize(Axis direction) => _convert(_size, direction);

        public _AxisSize ApplyConstraints(BoxConstraints constraints, Axis direction)
        {
            BoxConstraints effectiveConstraints = direction switch
            {
                Axis.Horizontal => constraints,
                Axis.Vertical => constraints.Flipped,

                _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
            };

            return new _AxisSize(effectiveConstraints.Constrain(_size));
        }

        private static Size _convert(Size size, Axis direction)
        {
            return direction switch
            {
                Axis.Horizontal => size,
                Axis.Vertical => size.Flipped,

                _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
            };
        }

        public static _AxisSize operator +(_AxisSize a, _AxisSize b)
        {
            return new _AxisSize(
                new Size(
                    a._size.Width + b._size.Width,
                    Math.Max(a._size.Height, b._size.Height)));
        }
    }

    private readonly struct _AscentDescent
    {
        private _AscentDescent((double ascent, double descent)? ascentDescent)
        {
            AscentDescent = ascentDescent;
        }

        public double? BaselineOffset => AscentDescent?.ascent;

        public (double ascent, double descent)? AscentDescent { get; }

        public static readonly _AscentDescent None = new(null);

        public static _AscentDescent Create(double? baselineOffset, double crossSize)
        {
            return !baselineOffset.HasValue
                ? None
                : new _AscentDescent((baselineOffset.Value, crossSize - baselineOffset.Value));
        }

        public static _AscentDescent operator +(_AscentDescent a, _AscentDescent b)
        {
            if (a.AscentDescent is null)
            {
                return b;
            }

            if (b.AscentDescent is null)
            {
                return a;
            }

            return new _AscentDescent((
                Math.Max(a.AscentDescent.Value.ascent, b.AscentDescent.Value.ascent),
                Math.Max(a.AscentDescent.Value.descent, b.AscentDescent.Value.descent)));
        }
    }

    private readonly struct _LayoutSizes
    {
        public _LayoutSizes(
            _AxisSize axisSize,
            double mainAxisFreeSpace,
            double? baselineOffset,
            double? spacePerFlex)
        {
            this.axisSize = axisSize;
            this.mainAxisFreeSpace = mainAxisFreeSpace;
            this.baselineOffset = baselineOffset;
            this.spacePerFlex = spacePerFlex;

            Debug.Assert(!spacePerFlex.HasValue || double.IsFinite(spacePerFlex.Value));
        }

        // The constrained _AxisSize of the RenderFlex.
        public readonly _AxisSize axisSize;

        // The free space along the main axis. If the value is positive, the free space
        // will be distributed according to the [MainAxisAlignment] specified. A
        // negative value indicates the RenderFlex overflows along the main axis.
        public readonly double mainAxisFreeSpace;

        // Null if the RenderFlex is not baseline aligned, or none of its children has
        // a valid baseline of the given [TextBaseline] type.
        public readonly double? baselineOffset;

        // The allocated space for flex children.
        public readonly double? spacePerFlex;
    }

    #region Mixins

    public int ChildCount => _mixin1.ChildCount;

    public RenderBox? FirstChild => _mixin1.FirstChild;

    public RenderBox? LastChild => _mixin1.LastChild;

    public void Insert(RenderBox child, RenderBox? after = null) => _mixin1.Insert(child, after);

    public void Move(RenderBox child, RenderBox? after = null) => _mixin1.Move(child, after);

    public void Remove(RenderBox child) => _mixin1.Remove(child);

    void IRenderObjectContainer.Insert(RenderObject child, RenderObject? after)
    {
        Insert((RenderBox)child, (RenderBox?)after);
    }

    void IRenderObjectContainer.Move(RenderObject child, RenderObject? after)
    {
        Move((RenderBox)child, (RenderBox?)after);
    }

    void IRenderObjectContainer.Remove(RenderObject child)
    {
        Remove((RenderBox)child);
    }

    public void AddAll(List<RenderBox> children) => _mixin1.AddAll(children);

    public RenderBox? ChildBefore(RenderBox child) => _mixin1.ChildBefore(child);

    public RenderBox? ChildAfter(RenderBox child) => _mixin1.ChildAfter(child);

    public void DefaultPaint(PaintingContext ctx, Point offset) => _mixin1.DefaultPaint(ctx, offset);

    public bool DefaultHitTestChildren(BoxHitTestResult result, Point position) =>
        _mixin1.DefaultHitTestChildren(result, position);

    #endregion

    /// <inheritdoc />
    public override string ToStringShort()
    {
        string header = base.ToStringShort();
        if (Constants.KDebugMode && _hasOverflow)
        {
            header += " OVERFLOWING";
        }

        return header;
    }

    /// <inheritdoc />
    public override void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        base.DebugFillProperties(properties);
        properties.Add(new EnumProperty<Axis>("direction", Direction));
        properties.Add(new EnumProperty<MainAxisAlignment>("mainAxisAlignment", MainAxisAlignment));
        properties.Add(new EnumProperty<MainAxisSize>("mainAxisSize", MainAxisSize));
        properties.Add(new EnumProperty<CrossAxisAlignment>("crossAxisAlignment", CrossAxisAlignment));
        properties.Add(new EnumProperty<TextDirection>(
            "textDirection",
            TextDirection,
            defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new EnumProperty<VerticalDirection>(
            "verticalDirection",
            VerticalDirection,
            defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new EnumProperty<TextBaseline>(
            "textBaseline",
            TextBaseline,
            defaultValue: DiagnosticsDefaults.NullValue));
        properties.Add(new DoubleProperty("spacing", Spacing, defaultValue: DiagnosticsDefaults.NullValue));
    }

    /// <inheritdoc />
    public override List<DiagnosticsNode> DebugDescribeChildren() => _mixin1.DebugDescribeChildren();
}
