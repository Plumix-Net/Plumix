using Avalonia;
using Avalonia.Media;
using Plumix;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source (reference): flutter/packages/flutter/lib/src/material/expansion_panel.dart
public delegate void ExpansionPanelCallback(int panelIndex, bool isExpanded);

public delegate Widget ExpansionPanelHeaderBuilder(BuildContext context, bool isExpanded);

public class ExpansionPanel
{
    public ExpansionPanel(
        ExpansionPanelHeaderBuilder headerBuilder,
        Widget body,
        bool isExpanded = false,
        Color? splashColor = null,
        Color? highlightColor = null,
        bool canTapOnHeader = false,
        Color? backgroundColor = null)
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
        Color? splashColor = null,
        Color? highlightColor = null,
        bool canTapOnHeader = false,
        Color? backgroundColor = null) : base(
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
        private readonly Dictionary<PanelIdentity, ExpansibleController> _controllers = [];
        private object? _currentOpenPanelValue;

        private ExpansionPanelList CurrentWidget => (ExpansionPanelList)StateWidget;

        public override void InitState()
        {
            if (CurrentWidget.AllowOnlyOnePanel)
            {
                _currentOpenPanelValue = FindRadioValue(CurrentWidget.InitialOpenPanelValue);
            }

            SynchronizeControllers();
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var oldList = (ExpansionPanelList)oldWidget;
            CurrentWidget.ValidateRadioChildren();
            if (CurrentWidget.AllowOnlyOnePanel && !oldList.AllowOnlyOnePanel)
            {
                _currentOpenPanelValue = FindRadioValue(CurrentWidget.InitialOpenPanelValue);
            }
            else if (!CurrentWidget.AllowOnlyOnePanel)
            {
                _currentOpenPanelValue = null;
            }

            SynchronizeControllers();
        }

        public override void Dispose()
        {
            foreach (var controller in _controllers.Values)
            {
                controller.Dispose();
            }

            _controllers.Clear();
        }

        public override Widget Build(BuildContext context)
        {
            var items = new List<MergeableMaterialItem>();
            for (var index = 0; index < CurrentWidget.Children.Count; index++)
            {
                var expanded = IsChildExpanded(index);
                if (expanded
                    && index != 0
                    && !IsChildExpanded(index - 1))
                {
                    items.Add(new MaterialGap(
                        key: new ValueKey<string>($"expansion-panel-gap-{index * 2 - 1}"),
                        size: CurrentWidget.MaterialGapSize));
                }

                var panel = CurrentWidget.Children[index];
                var capturedIndex = index;
                var controller = _controllers[IdentityFor(index, panel)];
                items.Add(new MaterialSlice(
                    key: new ValueKey<string>($"expansion-panel-slice-{index * 2}"),
                    color: panel.BackgroundColor,
                    child: new Expansible(
                        key: new ValueKey<string>($"expansion-panel-expansible-{IdentityFor(index, panel)}"),
                        controller: controller,
                        duration: CurrentWidget.AnimationDuration,
                        curve: Curves.EaseInOut,
                        maintainState: false,
                        headerBuilder: (buildContext, animation) =>
                            BuildHeader(buildContext, panel, capturedIndex, expanded, animation),
                        bodyBuilder: (_, animation) => BuildBody(panel, animation))));

                if (expanded && index != CurrentWidget.Children.Count - 1)
                {
                    items.Add(new MaterialGap(
                        key: new ValueKey<string>($"expansion-panel-gap-{index * 2 + 1}"),
                        size: CurrentWidget.MaterialGapSize));
                }
            }

            return new MergeableMaterial(
                children: items,
                elevation: CurrentWidget.Elevation,
                hasDividers: true,
                dividerColor: CurrentWidget.DividerColor);
        }

        private Widget BuildHeader(
            BuildContext context,
            ExpansionPanel panel,
            int index,
            bool expanded,
            AnimationController animation)
        {
            var progress = Curves.EaseInOut(animation.Value);
            var padding = LerpThickness(default, CurrentWidget.ExpandedHeaderPadding, progress);
            var header = new Padding(
                padding,
                new ConstrainedBox(
                    constraints: new BoxConstraints(MinHeight: 48),
                    child: panel.HeaderBuilder(context, expanded)));
            var iconColor = CurrentWidget.ExpandIconColor ?? Theme.Of(context).OnSurfaceVariantColor;
            Widget arrow = new ExpandIcon(
                isExpanded: expanded,
                onPressed: panel.CanTapOnHeader ? null : _ => HandlePressed(index, expanded),
                color: iconColor,
                disabledColor: iconColor,
                expandedColor: iconColor,
                splashColor: panel.SplashColor,
                highlightColor: panel.HighlightColor,
                padding: panel.CanTapOnHeader ? default : new Thickness(12));

            if (panel.CanTapOnHeader)
            {
                arrow = new SizedBox(
                    width: 48,
                    height: 48,
                    child: new Center(child: arrow));
            }
            else
            {
                arrow = new SizedBox(width: 48, height: 48, child: arrow);
            }

            var flags = SemanticsFlags.HasExpandedState | SemanticsFlags.IsEnabled;
            if (expanded)
            {
                flags |= SemanticsFlags.IsExpanded;
            }

            if (!panel.CanTapOnHeader)
            {
                var localizations = MaterialLocalizations.Of(context);
                arrow = new Semantics(
                    child: arrow,
                    label: expanded
                        ? localizations.ExpandedIconTapHint
                        : localizations.CollapsedIconTapHint,
                    flags: flags,
                    onTap: () => HandlePressed(index, expanded),
                    container: true);
            }

            Widget row = new Row(
                children:
                [
                    new Expanded(header),
                    new Padding(new Thickness(0, 0, 8, 0), arrow)
                ]);
            if (panel.CanTapOnHeader)
            {
                row = new MaterialButtonCore(
                    child: row,
                    onPressed: () => HandlePressed(index, expanded),
                    style: BuildHeaderButtonStyle(panel, Theme.Of(context)));
            }

            return panel.CanTapOnHeader
                ? new Semantics(
                    child: row,
                    flags: flags,
                    onTap: () => HandlePressed(index, expanded),
                    container: true)
                : row;
        }

        private static Widget BuildBody(ExpansionPanel panel, AnimationController animation)
        {
            var progress = animation.Evaluate();
            var opacity = progress <= 0.4
                ? 0
                : Curves.EaseInOut((progress - 0.4) / 0.6);
            return new Opacity(opacity, panel.Body);
        }

        private void HandlePressed(int index, bool isExpanded)
        {
            if (!CurrentWidget.AllowOnlyOnePanel)
            {
                CurrentWidget.ExpansionCallback?.Invoke(index, !isExpanded);
                return;
            }

            if (_currentOpenPanelValue is not null && !isExpanded)
            {
                var previousIndex = FindRadioIndex(_currentOpenPanelValue);
                if (previousIndex >= 0 && previousIndex != index)
                {
                    CurrentWidget.ExpansionCallback?.Invoke(previousIndex, false);
                }
            }

            SetState(() =>
            {
                var pressed = (ExpansionPanelRadio)CurrentWidget.Children[index];
                _currentOpenPanelValue = isExpanded ? null : pressed.Value;
                SynchronizeControllers();
            });
            CurrentWidget.ExpansionCallback?.Invoke(index, !isExpanded);
        }

        private bool IsChildExpanded(int index)
        {
            var child = CurrentWidget.Children[index];
            return CurrentWidget.AllowOnlyOnePanel
                ? child is ExpansionPanelRadio radio && Equals(radio.Value, _currentOpenPanelValue)
                : child.IsExpanded;
        }

        private void SynchronizeControllers()
        {
            var desired = new HashSet<PanelIdentity>();
            for (var index = 0; index < CurrentWidget.Children.Count; index++)
            {
                var identity = IdentityFor(index, CurrentWidget.Children[index]);
                desired.Add(identity);
                if (!_controllers.TryGetValue(identity, out var controller))
                {
                    controller = new ExpansibleController();
                    _controllers.Add(identity, controller);
                }

                if (IsChildExpanded(index))
                {
                    controller.Expand();
                }
                else
                {
                    controller.Collapse();
                }
            }

            foreach (var identity in _controllers.Keys.Where(key => !desired.Contains(key)).ToArray())
            {
                _controllers[identity].Dispose();
                _controllers.Remove(identity);
            }
        }

        private PanelIdentity IdentityFor(int index, ExpansionPanel panel)
        {
            return CurrentWidget.AllowOnlyOnePanel && panel is ExpansionPanelRadio radio
                ? new PanelIdentity(true, radio.Value)
                : new PanelIdentity(false, index);
        }

        private object? FindRadioValue(object? value)
        {
            return CurrentWidget.Children
                .OfType<ExpansionPanelRadio>()
                .FirstOrDefault(panel => Equals(panel.Value, value))
                ?.Value;
        }

        private int FindRadioIndex(object value)
        {
            for (var index = 0; index < CurrentWidget.Children.Count; index++)
            {
                if (CurrentWidget.Children[index] is ExpansionPanelRadio radio
                    && Equals(radio.Value, value))
                {
                    return index;
                }
            }

            return -1;
        }

        private static ButtonStyle BuildHeaderButtonStyle(ExpansionPanel panel, ThemeData theme)
        {
            var overlay = MaterialStateProperty<Color?>.ResolveWith(states =>
            {
                if (states.HasFlag(MaterialState.Pressed))
                {
                    return panel.HighlightColor ?? MaterialButtonCore.ApplyOpacity(theme.OnSurfaceColor, 0.12);
                }

                if (states.HasFlag(MaterialState.Focused) || states.HasFlag(MaterialState.Hovered))
                {
                    return MaterialButtonCore.ApplyOpacity(theme.OnSurfaceColor, 0.08);
                }

                return null;
            });
            return new ButtonStyle(
                BackgroundColor: MaterialStateProperty<Color?>.All(Colors.Transparent),
                OverlayColor: overlay,
                SplashColor: MaterialStateProperty<Color?>.All(
                    panel.SplashColor ?? MaterialButtonCore.ApplyOpacity(theme.OnSurfaceColor, 0.12)),
                Elevation: MaterialStateProperty<double?>.All(0),
                Padding: MaterialStateProperty<Thickness?>.All(default),
                Shape: MaterialStateProperty<BorderRadius?>.All(BorderRadius.Zero),
                MinimumSize: MaterialStateProperty<Size?>.All(new Size(0, 48)),
                MaximumSize: MaterialStateProperty<Size?>.All(new Size(double.PositiveInfinity, double.PositiveInfinity)),
                Alignment: Alignment.CenterLeft,
                TapTargetSize: MaterialTapTargetSize.ShrinkWrap);
        }

        private static Thickness LerpThickness(Thickness from, Thickness to, double progress)
        {
            var t = Math.Clamp(progress, 0, 1);
            return new Thickness(
                from.Left + ((to.Left - from.Left) * t),
                from.Top + ((to.Top - from.Top) * t),
                from.Right + ((to.Right - from.Right) * t),
                from.Bottom + ((to.Bottom - from.Bottom) * t));
        }

        private sealed record PanelIdentity(bool Radio, object Value);
    }
}
