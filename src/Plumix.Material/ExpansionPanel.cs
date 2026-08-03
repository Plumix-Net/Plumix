using Avalonia;
using Avalonia.Media;
using Plumix;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/expansion_panel.dart
internal sealed record ExpansionPanelSaltedKey(BuildContext Salt, int Value) : LocalKey;

public delegate void ExpansionPanelCallback(int panelIndex, bool isExpanded);

public delegate Widget ExpansionPanelHeaderBuilder(BuildContext context, bool isExpanded);

public class ExpansionPanel
{
    public ExpansionPanel(
        ExpansionPanelHeaderBuilder headerBuilder,
        Widget body,
        bool isExpanded = false,
        bool canTapOnHeader = false,
        Color? backgroundColor = null,
        Color? splashColor = null,
        Color? highlightColor = null)
    {
        HeaderBuilder = headerBuilder ?? throw new ArgumentNullException(nameof(headerBuilder));
        Body = body ?? throw new ArgumentNullException(nameof(body));
        IsExpanded = isExpanded;
        SplashColor = splashColor;
        HighlightColor = highlightColor;
        CanTapOnHeader = canTapOnHeader;
        BackgroundColor = backgroundColor;
    }

    public ExpansionPanelHeaderBuilder HeaderBuilder { get; }

    public Widget Body { get; }

    public bool IsExpanded { get; }

    public Color? SplashColor { get; }

    public Color? HighlightColor { get; }

    public bool CanTapOnHeader { get; }

    public Color? BackgroundColor { get; }
}

public sealed class ExpansionPanelRadio : ExpansionPanel
{
    public ExpansionPanelRadio(
        object value,
        ExpansionPanelHeaderBuilder headerBuilder,
        Widget body,
        bool canTapOnHeader = false,
        Color? backgroundColor = null,
        Color? splashColor = null,
        Color? highlightColor = null) : base(
        headerBuilder: headerBuilder,
        body: body,
        splashColor: splashColor,
        highlightColor: highlightColor,
        canTapOnHeader: canTapOnHeader,
        backgroundColor: backgroundColor)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    public object Value { get; }
}

public sealed class ExpansionPanelList : StatefulWidget
{
    private static readonly TimeSpan DefaultAnimationDuration = TimeSpan.FromMilliseconds(200);
    private static readonly Thickness DefaultExpandedHeaderPadding = new(0, 16);

    public ExpansionPanelList(
        IReadOnlyList<ExpansionPanel>? children = null,
        ExpansionPanelCallback? expansionCallback = null,
        TimeSpan? animationDuration = null,
        Thickness? expandedHeaderPadding = null,
        Color? dividerColor = null,
        double elevation = 2,
        Color? expandIconColor = null,
        double materialGapSize = 16,
        Key? key = null) : this(
        children: children,
        expansionCallback: expansionCallback,
        animationDuration: animationDuration,
        expandedHeaderPadding: expandedHeaderPadding,
        dividerColor: dividerColor,
        elevation: elevation,
        expandIconColor: expandIconColor,
        materialGapSize: materialGapSize,
        allowOnlyOnePanel: false,
        initialOpenPanelValue: null,
        key: key)
    {
    }

    private ExpansionPanelList(
        IReadOnlyList<ExpansionPanel>? children,
        ExpansionPanelCallback? expansionCallback,
        TimeSpan? animationDuration,
        Thickness? expandedHeaderPadding,
        Color? dividerColor,
        double elevation,
        Color? expandIconColor,
        double materialGapSize,
        bool allowOnlyOnePanel,
        object? initialOpenPanelValue,
        Key? key) : base(key)
    {
        var effectiveDuration = animationDuration ?? DefaultAnimationDuration;
        if (effectiveDuration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(animationDuration), "Animation duration must be non-negative.");
        }

        if (!double.IsFinite(elevation) || !MaterialElevation.HasDefinedShadow(elevation))
        {
            throw new ArgumentOutOfRangeException(
                nameof(elevation),
                "Elevation must be one of 0, 1, 2, 3, 4, 6, 8, 9, 12, 16, or 24.");
        }

        if (!double.IsFinite(materialGapSize) || materialGapSize < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(materialGapSize), "Material gap size must be finite and non-negative.");
        }

        Children = children ?? [];
        ExpansionCallback = expansionCallback;
        AnimationDuration = effectiveDuration;
        ExpandedHeaderPadding = expandedHeaderPadding ?? DefaultExpandedHeaderPadding;
        DividerColor = dividerColor;
        Elevation = elevation;
        ExpandIconColor = expandIconColor;
        MaterialGapSize = materialGapSize;
        AllowOnlyOnePanel = allowOnlyOnePanel;
        InitialOpenPanelValue = initialOpenPanelValue;
        ValidateRadioChildren();
    }

    public IReadOnlyList<ExpansionPanel> Children { get; }

    public ExpansionPanelCallback? ExpansionCallback { get; }

    public TimeSpan AnimationDuration { get; }

    public Thickness ExpandedHeaderPadding { get; }

    public Color? DividerColor { get; }

    public double Elevation { get; }

    public Color? ExpandIconColor { get; }

    public double MaterialGapSize { get; }

    public object? InitialOpenPanelValue { get; }

    internal bool AllowOnlyOnePanel { get; }

    public static ExpansionPanelList Radio(
        IReadOnlyList<ExpansionPanelRadio>? children = null,
        ExpansionPanelCallback? expansionCallback = null,
        TimeSpan? animationDuration = null,
        object? initialOpenPanelValue = null,
        Thickness? expandedHeaderPadding = null,
        Color? dividerColor = null,
        double elevation = 2,
        Color? expandIconColor = null,
        double materialGapSize = 16,
        Key? key = null)
    {
        return new ExpansionPanelList(
            children: children?.Cast<ExpansionPanel>().ToArray(),
            expansionCallback: expansionCallback,
            animationDuration: animationDuration,
            expandedHeaderPadding: expandedHeaderPadding,
            dividerColor: dividerColor,
            elevation: elevation,
            expandIconColor: expandIconColor,
            materialGapSize: materialGapSize,
            allowOnlyOnePanel: true,
            initialOpenPanelValue: initialOpenPanelValue,
            key: key);
    }

    public override State CreateState() => new ExpansionPanelListState();

    private void ValidateRadioChildren()
    {
        if (!AllowOnlyOnePanel)
        {
            return;
        }

        var values = new HashSet<object>();
        foreach (var panel in Children)
        {
            if (panel is not ExpansionPanelRadio radio)
            {
                throw new ArgumentException(
                    "ExpansionPanelList.Radio children must all be ExpansionPanelRadio instances.",
                    nameof(Children));
            }

            if (!values.Add(radio.Value))
            {
                throw new ArgumentException(
                    "ExpansionPanelRadio values must be unique within a radio list.",
                    nameof(Children));
            }
        }
    }

    private sealed class ExpansionPanelListState : State
    {
        private ExpansionPanelRadio? _currentOpenPanel;

        private ExpansionPanelList CurrentWidget => (ExpansionPanelList)StateWidget;

        public override void InitState()
        {
            if (CurrentWidget.AllowOnlyOnePanel)
            {
                _currentOpenPanel = SearchPanelByValue(CurrentWidget.InitialOpenPanelValue);
            }
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var oldList = (ExpansionPanelList)oldWidget;
            CurrentWidget.ValidateRadioChildren();
            if (CurrentWidget.AllowOnlyOnePanel && !oldList.AllowOnlyOnePanel)
            {
                _currentOpenPanel = SearchPanelByValue(CurrentWidget.InitialOpenPanelValue);
            }
            else if (!CurrentWidget.AllowOnlyOnePanel)
            {
                _currentOpenPanel = null;
            }
        }

        public override Widget Build(BuildContext context)
        {
            var items = new List<MergeableMaterialItem>();
            for (int index = 0; index < CurrentWidget.Children.Count; index++)
            {
                if (IsChildExpanded(index)
                    && index != 0
                    && !IsChildExpanded(index - 1))
                {
                    items.Add(new MaterialGap(
                        key: new ExpansionPanelSaltedKey(context, (index * 2) - 1),
                        size: CurrentWidget.MaterialGapSize));
                }

                ExpansionPanel panel = CurrentWidget.Children[index];
                bool expanded = IsChildExpanded(index);
                int capturedIndex = index;
                Widget headerWidget = panel.HeaderBuilder(context, expanded);
                Widget expandIconPadded = new Padding(
                    insets: EdgeInsetsDirectional.Only(end: 8.0),
                    child: new IgnorePointer(
                        ignoring: panel.CanTapOnHeader,
                        child: new ExpandIcon(
                            color: CurrentWidget.ExpandIconColor,
                            isExpanded: expanded,
                            padding: new Thickness(12.0),
                            splashColor: panel.SplashColor,
                            highlightColor: panel.HighlightColor,
                            onPressed: isExpanded => HandlePressed(capturedIndex, isExpanded))));

                if (!panel.CanTapOnHeader)
                {
                    var localizations = MaterialLocalizations.Of(context);
                    expandIconPadded = new Semantics(
                        label: expanded
                            ? localizations.ExpandedIconTapHint
                            : localizations.CollapsedIconTapHint,
                        container: true,
                        child: expandIconPadded);
                }

                Widget header = new Row(
                    children:
                    [
                        new Expanded(
                            new AnimatedContainer(
                                duration: CurrentWidget.AnimationDuration,
                                curve: Curves.FastOutSlowIn,
                                margin: expanded ? CurrentWidget.ExpandedHeaderPadding : default(Thickness),
                                child: new ConstrainedBox(
                                    constraints: new BoxConstraints(MinHeight: 48.0),
                                    child: headerWidget))),
                        expandIconPadded,
                    ]);

                if (panel.CanTapOnHeader)
                {
                    header = new MergeSemantics(
                        child: new InkWell(
                            splashColor: panel.SplashColor,
                            highlightColor: panel.HighlightColor,
                            onTap: () => HandlePressed(capturedIndex, IsChildExpanded(capturedIndex)),
                            child: header));
                }

                items.Add(new MaterialSlice(
                    key: new ExpansionPanelSaltedKey(context, index * 2),
                    color: panel.BackgroundColor,
                    child: new Column(
                        children:
                        [
                            header,
                            new AnimatedCrossFade(
                                firstChild: new LimitedBox(
                                    maxWidth: 0.0,
                                    child: new SizedBox(width: double.PositiveInfinity, height: 0.0)),
                                secondChild: panel.Body,
                                firstCurve: Curves.Interval(0.0, 0.6, Curves.FastOutSlowIn),
                                secondCurve: Curves.Interval(0.4, 1.0, Curves.FastOutSlowIn),
                                sizeCurve: Curves.FastOutSlowIn,
                                crossFadeState: expanded ? CrossFadeState.ShowSecond : CrossFadeState.ShowFirst,
                                duration: CurrentWidget.AnimationDuration),
                        ])));

                if (expanded && index != CurrentWidget.Children.Count - 1)
                {
                    items.Add(new MaterialGap(
                        key: new ExpansionPanelSaltedKey(context, (index * 2) + 1),
                        size: CurrentWidget.MaterialGapSize));
                }
            }

            return new MergeableMaterial(
                children: items,
                elevation: CurrentWidget.Elevation,
                hasDividers: true,
                dividerColor: CurrentWidget.DividerColor);
        }

        private void HandlePressed(int index, bool isExpanded)
        {
            if (!CurrentWidget.AllowOnlyOnePanel)
            {
                CurrentWidget.ExpansionCallback?.Invoke(index, !isExpanded);
                return;
            }

            var pressedChild = (ExpansionPanelRadio)CurrentWidget.Children[index];
            for (int childIndex = 0; childIndex < CurrentWidget.Children.Count; childIndex++)
            {
                var child = (ExpansionPanelRadio)CurrentWidget.Children[childIndex];
                if (CurrentWidget.ExpansionCallback is not null
                    && childIndex != index
                    && Equals(child.Value, _currentOpenPanel?.Value))
                {
                    CurrentWidget.ExpansionCallback(childIndex, false);
                }
            }

            SetState(() =>
            {
                _currentOpenPanel = isExpanded ? null : pressedChild;
            });
            CurrentWidget.ExpansionCallback?.Invoke(index, !isExpanded);
        }

        private bool IsChildExpanded(int index)
        {
            var child = CurrentWidget.Children[index];
            return CurrentWidget.AllowOnlyOnePanel
                ? child is ExpansionPanelRadio radio && Equals(radio.Value, _currentOpenPanel?.Value)
                : child.IsExpanded;
        }

        private ExpansionPanelRadio? SearchPanelByValue(object? value)
        {
            return CurrentWidget.Children
                .OfType<ExpansionPanelRadio>()
                .FirstOrDefault(panel => Equals(panel.Value, value));
        }
    }
}
