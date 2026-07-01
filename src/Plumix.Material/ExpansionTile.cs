using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source (reference): flutter/packages/flutter/lib/src/material/expansion_tile.dart
public sealed class ExpansionTile : StatefulWidget
{
    public ExpansionTile(
        Widget title,
        Widget? leading = null,
        Widget? subtitle = null,
        Action<bool>? onExpansionChanged = null,
        IReadOnlyList<Widget>? children = null,
        Color? backgroundColor = null,
        Color? collapsedBackgroundColor = null,
        Widget? trailing = null,
        bool showTrailingIcon = true,
        bool initiallyExpanded = false,
        bool maintainState = false,
        Thickness? tilePadding = null,
        Alignment? expandedAlignment = null,
        CrossAxisAlignment? expandedCrossAxisAlignment = null,
        Thickness? childrenPadding = null,
        Color? iconColor = null,
        Color? collapsedIconColor = null,
        Color? textColor = null,
        Color? collapsedTextColor = null,
        BorderRadius? shape = null,
        BorderRadius? collapsedShape = null,
        Clip? clipBehavior = null,
        ListTileControlAffinity? controlAffinity = null,
        ExpansibleController? controller = null,
        bool? dense = null,
        Color? splashColor = null,
        double? minTileHeight = null,
        bool? enableFeedback = true,
        bool enabled = true,
        ExpansionAnimationStyle? expansionAnimationStyle = null,
        Key? key = null) : base(key)
    {
        if (expandedCrossAxisAlignment == CrossAxisAlignment.Baseline)
        {
            throw new ArgumentException(
                "CrossAxisAlignment.Baseline is not supported for ExpansionTile children.",
                nameof(expandedCrossAxisAlignment));
        }

        if (minTileHeight.HasValue && (!double.IsFinite(minTileHeight.Value) || minTileHeight.Value < 0))
        {
            throw new ArgumentOutOfRangeException(nameof(minTileHeight), "Minimum tile height must be finite and non-negative.");
        }

        Title = title ?? throw new ArgumentNullException(nameof(title));
        Leading = leading;
        Subtitle = subtitle;
        OnExpansionChanged = onExpansionChanged;
        Children = children ?? [];
        BackgroundColor = backgroundColor;
        CollapsedBackgroundColor = collapsedBackgroundColor;
        Trailing = trailing;
        ShowTrailingIcon = showTrailingIcon;
        InitiallyExpanded = initiallyExpanded;
        MaintainState = maintainState;
        TilePadding = tilePadding;
        ExpandedAlignment = expandedAlignment;
        ExpandedCrossAxisAlignment = expandedCrossAxisAlignment;
        ChildrenPadding = childrenPadding;
        IconColor = iconColor;
        CollapsedIconColor = collapsedIconColor;
        TextColor = textColor;
        CollapsedTextColor = collapsedTextColor;
        Shape = shape;
        CollapsedShape = collapsedShape;
        ClipBehavior = clipBehavior;
        ControlAffinity = controlAffinity;
        Controller = controller;
        Dense = dense;
        SplashColor = splashColor;
        MinTileHeight = minTileHeight;
        EnableFeedback = enableFeedback;
        Enabled = enabled;
        ExpansionAnimationStyle = expansionAnimationStyle;
    }

    public Widget? Leading { get; }
    public Widget Title { get; }
    public Widget? Subtitle { get; }
    public Action<bool>? OnExpansionChanged { get; }
    public IReadOnlyList<Widget> Children { get; }
    public Color? BackgroundColor { get; }
    public Color? CollapsedBackgroundColor { get; }
    public Widget? Trailing { get; }
    public bool ShowTrailingIcon { get; }
    public bool InitiallyExpanded { get; }
    public bool MaintainState { get; }
    public Thickness? TilePadding { get; }
    public Alignment? ExpandedAlignment { get; }
    public CrossAxisAlignment? ExpandedCrossAxisAlignment { get; }
    public Thickness? ChildrenPadding { get; }
    public Color? IconColor { get; }
    public Color? CollapsedIconColor { get; }
    public Color? TextColor { get; }
    public Color? CollapsedTextColor { get; }
    public BorderRadius? Shape { get; }
    public BorderRadius? CollapsedShape { get; }
    public Clip? ClipBehavior { get; }
    public ListTileControlAffinity? ControlAffinity { get; }
    public ExpansibleController? Controller { get; }
    public bool? Dense { get; }
    public Color? SplashColor { get; }
    public double? MinTileHeight { get; }
    public bool? EnableFeedback { get; }
    public bool Enabled { get; }
    public ExpansionAnimationStyle? ExpansionAnimationStyle { get; }

    public override State CreateState() => new ExpansionTileState();

    private sealed class ExpansionTileState : State
    {
        private ExpansibleController? _controller;
        private bool _ownsController;

        private ExpansionTile CurrentWidget => (ExpansionTile)StateWidget;

        public override void InitState()
        {
            AttachController(CurrentWidget.Controller);
            if (CurrentWidget.InitiallyExpanded)
            {
                _controller!.Expand();
            }

            _controller!.AddListener(HandleExpansionChanged);
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var oldTile = (ExpansionTile)oldWidget;
            if (!ReferenceEquals(oldTile.Controller, CurrentWidget.Controller))
            {
                DetachController();
                AttachController(CurrentWidget.Controller);
                _controller!.AddListener(HandleExpansionChanged);
            }
        }

        public override void Dispose()
        {
            DetachController();
        }

        public override Widget Build(BuildContext context)
        {
            var expansionTheme = ExpansionTileTheme.Of(context);
            var animationStyle = CurrentWidget.ExpansionAnimationStyle
                                 ?? expansionTheme.ExpansionAnimationStyle
                                 ?? new ExpansionAnimationStyle();

            return new Expansible(
                controller: _controller!,
                duration: animationStyle.Duration ?? TimeSpan.FromMilliseconds(200),
                curve: animationStyle.Curve ?? Curves.EaseIn,
                reverseCurve: animationStyle.ReverseCurve,
                maintainState: CurrentWidget.MaintainState,
                headerBuilder: (buildContext, animation) => BuildHeader(buildContext, animation, expansionTheme),
                bodyBuilder: (buildContext, animation) => BuildBody(expansionTheme),
                expansibleBuilder: (buildContext, header, body, animation) =>
                    BuildExpansible(header, body, animation, expansionTheme));
        }

        private Widget BuildHeader(
            BuildContext context,
            AnimationController animation,
            ExpansionTileThemeData expansionTheme)
        {
            var theme = Theme.Of(context);
            var progress = Curves.EaseIn(animation.Value);
            var expandedTextColor = CurrentWidget.TextColor
                                    ?? expansionTheme.TextColor
                                    ?? (theme.UseMaterial3 ? theme.OnSurfaceColor : theme.PrimaryColor);
            var collapsedTextColor = CurrentWidget.CollapsedTextColor
                                     ?? expansionTheme.CollapsedTextColor
                                     ?? theme.OnSurfaceColor;
            var expandedIconColor = CurrentWidget.IconColor
                                    ?? expansionTheme.IconColor
                                    ?? theme.PrimaryColor;
            var collapsedIconColor = CurrentWidget.CollapsedIconColor
                                     ?? expansionTheme.CollapsedIconColor
                                     ?? (theme.UseMaterial3
                                         ? theme.OnSurfaceVariantColor
                                         : ApplyOpacity(theme.OnSurfaceColor, 0.54));
            var textColor = LerpColor(collapsedTextColor, expandedTextColor, progress);
            var iconColor = LerpColor(collapsedIconColor, expandedIconColor, progress);
            var affinity = ResolveAffinity(expansionTheme);
            var arrow = BuildArrow(animation, iconColor);
            var leading = affinity == ListTileControlAffinity.Leading
                ? CurrentWidget.Leading ?? arrow
                : CurrentWidget.Leading;
            Widget? trailing = null;
            if (CurrentWidget.ShowTrailingIcon)
            {
                trailing = affinity == ListTileControlAffinity.Trailing
                    ? CurrentWidget.Trailing ?? arrow
                    : CurrentWidget.Trailing;
            }

            var tile = new ListTile(
                enabled: CurrentWidget.Enabled,
                onTap: CurrentWidget.Enabled
                    ? (_controller!.IsExpanded ? _controller.Collapse : _controller.Expand)
                    : null,
                dense: CurrentWidget.Dense ?? expansionTheme.Dense,
                splashColor: CurrentWidget.SplashColor,
                enableFeedback: CurrentWidget.EnableFeedback ?? expansionTheme.EnableFeedback,
                contentPadding: CurrentWidget.TilePadding
                                ?? expansionTheme.TilePadding
                                ?? new Thickness(16, 0),
                leading: leading,
                title: CurrentWidget.Title,
                subtitle: CurrentWidget.Subtitle,
                trailing: trailing,
                minTileHeight: CurrentWidget.MinTileHeight ?? expansionTheme.MinTileHeight,
                textColor: textColor,
                iconColor: iconColor);

            var flags = SemanticsFlags.HasExpandedState;
            if (_controller!.IsExpanded)
            {
                flags |= SemanticsFlags.IsExpanded;
            }

            if (CurrentWidget.Enabled)
            {
                flags |= SemanticsFlags.IsEnabled;
            }

            return new Semantics(
                child: tile,
                flags: flags,
                onTap: CurrentWidget.Enabled
                    ? (_controller.IsExpanded ? _controller.Collapse : _controller.Expand)
                    : null,
                container: true);
        }

        private Widget BuildBody(ExpansionTileThemeData expansionTheme)
        {
            Widget body = new Column(
                crossAxisAlignment: CurrentWidget.ExpandedCrossAxisAlignment
                                    ?? expansionTheme.ExpandedCrossAxisAlignment
                                    ?? CrossAxisAlignment.Center,
                children: CurrentWidget.Children);
            body = new Padding(
                CurrentWidget.ChildrenPadding
                ?? expansionTheme.ChildrenPadding
                ?? default,
                body);
            return new Align(
                alignment: CurrentWidget.ExpandedAlignment
                           ?? expansionTheme.ExpandedAlignment
                           ?? Alignment.Center,
                child: body);
        }

        private Widget BuildExpansible(
            Widget header,
            Widget body,
            AnimationController animation,
            ExpansionTileThemeData expansionTheme)
        {
            var backgroundProgress = Curves.EaseOut(animation.Value);
            var collapsedBackground = CurrentWidget.CollapsedBackgroundColor
                                      ?? expansionTheme.CollapsedBackgroundColor
                                      ?? Colors.Transparent;
            var expandedBackground = CurrentWidget.BackgroundColor
                                     ?? expansionTheme.BackgroundColor
                                     ?? Colors.Transparent;
            var background = LerpColor(collapsedBackground, expandedBackground, backgroundProgress);
            var collapsedShape = CurrentWidget.CollapsedShape
                                 ?? expansionTheme.CollapsedShape
                                 ?? BorderRadius.Zero;
            var expandedShape = CurrentWidget.Shape
                                ?? expansionTheme.Shape
                                ?? BorderRadius.Zero;
            var shape = LerpBorderRadius(collapsedShape, expandedShape, backgroundProgress);

            Widget result = new Column(
                mainAxisSize: MainAxisSize.Min,
                children: [header, body]);
            result = new DecoratedBox(
                decoration: new BoxDecoration(
                    Color: background,
                    BorderRadius: shape),
                child: result);

            var clip = CurrentWidget.ClipBehavior
                       ?? expansionTheme.ClipBehavior
                       ?? Clip.AntiAlias;
            if (clip != Clip.None && shape != BorderRadius.Zero)
            {
                result = new ClipRRect(shape, result);
            }

            return result;
        }

        private Widget BuildArrow(AnimationController animation, Color color)
        {
            const double iconSize = 24;
            var center = iconSize / 2;
            var angle = Math.PI * Curves.EaseIn(animation.Value);
            var cos = Math.Cos(angle);
            var sin = Math.Sin(angle);
            var rotation = new Matrix(cos, sin, -sin, cos, 0, 0);
            return new Plumix.Widgets.Transform(
                transform: Matrix.CreateTranslation(center, center)
                           * rotation
                           * Matrix.CreateTranslation(-center, -center),
                child: new Icon(Icons.ExpandMore, size: iconSize, color: color));
        }

        private ListTileControlAffinity ResolveAffinity(ExpansionTileThemeData expansionTheme)
        {
            var affinity = CurrentWidget.ControlAffinity
                           ?? expansionTheme.ControlAffinity
                           ?? ListTileTheme.Of(Context).ControlAffinity
                           ?? ListTileControlAffinity.Trailing;
            return affinity == ListTileControlAffinity.Leading
                ? ListTileControlAffinity.Leading
                : ListTileControlAffinity.Trailing;
        }

        private void AttachController(ExpansibleController? externalController)
        {
            _controller = externalController ?? new ExpansibleController();
            _ownsController = externalController is null;
        }

        private void DetachController()
        {
            if (_controller is null)
            {
                return;
            }

            _controller.RemoveListener(HandleExpansionChanged);
            if (_ownsController)
            {
                _controller.Dispose();
            }

            _controller = null;
            _ownsController = false;
        }

        private void HandleExpansionChanged()
        {
            CurrentWidget.OnExpansionChanged?.Invoke(_controller!.IsExpanded);
            SetState(() => { });
        }

        private static Color LerpColor(Color from, Color to, double progress)
        {
            return new ColorTween().Evaluate(Math.Clamp(progress, 0, 1), from, to);
        }

        private static BorderRadius LerpBorderRadius(BorderRadius from, BorderRadius to, double progress)
        {
            var t = Math.Clamp(progress, 0, 1);
            return BorderRadius.Circular(from.Radius + ((to.Radius - from.Radius) * t));
        }

        private static Color ApplyOpacity(Color color, double opacity)
        {
            var alpha = (byte)Math.Round(color.A * Math.Clamp(opacity, 0, 1));
            return Color.FromArgb(alpha, color.R, color.G, color.B);
        }
    }
}
