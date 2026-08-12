using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/expansion_tile.dart
public sealed class ExpansionTile : StatefulWidget
{
    public ExpansionTile(
        Widget title,
        Widget? leading = null,
        Widget? subtitle = null,
        Action<bool>? onExpansionChanged = null,
        IReadOnlyList<Widget>? children = null,
        Widget? trailing = null,
        bool showTrailingIcon = true,
        bool initiallyExpanded = false,
        bool maintainState = false,
        EdgeInsetsGeometry? tilePadding = null,
        CrossAxisAlignment? expandedCrossAxisAlignment = null,
        AlignmentGeometry? expandedAlignment = null,
        EdgeInsetsGeometry? childrenPadding = null,
        Color? backgroundColor = null,
        Color? collapsedBackgroundColor = null,
        Color? textColor = null,
        Color? collapsedTextColor = null,
        Color? iconColor = null,
        Color? collapsedIconColor = null,
        ShapeBorder? shape = null,
        ShapeBorder? collapsedShape = null,
        Clip? clipBehavior = null,
        ListTileControlAffinity? controlAffinity = null,
        ExpansibleController? controller = null,
        bool? dense = null,
        Color? splashColor = null,
        VisualDensity? visualDensity = null,
        double? minTileHeight = null,
        bool? enableFeedback = true,
        bool enabled = true,
        AnimationStyle? expansionAnimationStyle = null,
        bool internalAddSemanticForOnTap = false,
        WidgetStatesController? statesController = null,
        Key? key = null) : base(key)
    {
        if (expandedCrossAxisAlignment == CrossAxisAlignment.Baseline)
        {
            throw new ArgumentException(
                "CrossAxisAlignment.Baseline is not supported since the expanded children are aligned in a "
                + "column, not a row. Try to use another constant.",
                nameof(expandedCrossAxisAlignment));
        }

        if (minTileHeight.HasValue && (!double.IsFinite(minTileHeight.Value) || minTileHeight.Value < 0.0))
        {
            throw new ArgumentOutOfRangeException(
                nameof(minTileHeight),
                "Minimum tile height must be finite and non-negative.");
        }

        Title = title ?? throw new ArgumentNullException(nameof(title));
        Leading = leading;
        Subtitle = subtitle;
        OnExpansionChanged = onExpansionChanged;
        Children = children ?? [];
        Trailing = trailing;
        ShowTrailingIcon = showTrailingIcon;
        InitiallyExpanded = initiallyExpanded;
        MaintainState = maintainState;
        TilePadding = tilePadding;
        ExpandedCrossAxisAlignment = expandedCrossAxisAlignment;
        ExpandedAlignment = expandedAlignment;
        ChildrenPadding = childrenPadding;
        BackgroundColor = backgroundColor;
        CollapsedBackgroundColor = collapsedBackgroundColor;
        TextColor = textColor;
        CollapsedTextColor = collapsedTextColor;
        IconColor = iconColor;
        CollapsedIconColor = collapsedIconColor;
        Shape = shape;
        CollapsedShape = collapsedShape;
        ClipBehavior = clipBehavior;
        ControlAffinity = controlAffinity;
        Controller = controller;
        Dense = dense;
        SplashColor = splashColor;
        VisualDensity = visualDensity;
        MinTileHeight = minTileHeight;
        EnableFeedback = enableFeedback;
        Enabled = enabled;
        ExpansionAnimationStyle = expansionAnimationStyle;
        InternalAddSemanticForOnTap = internalAddSemanticForOnTap;
        StatesController = statesController;
    }

    public Widget? Leading { get; }
    public Widget Title { get; }
    public Widget? Subtitle { get; }
    public Action<bool>? OnExpansionChanged { get; }
    public IReadOnlyList<Widget> Children { get; }
    public Widget? Trailing { get; }
    public bool ShowTrailingIcon { get; }
    public bool InitiallyExpanded { get; }
    public bool MaintainState { get; }
    public EdgeInsetsGeometry? TilePadding { get; }
    public CrossAxisAlignment? ExpandedCrossAxisAlignment { get; }
    public AlignmentGeometry? ExpandedAlignment { get; }
    public EdgeInsetsGeometry? ChildrenPadding { get; }
    public Color? BackgroundColor { get; }
    public Color? CollapsedBackgroundColor { get; }
    public Color? TextColor { get; }
    public Color? CollapsedTextColor { get; }
    public Color? IconColor { get; }
    public Color? CollapsedIconColor { get; }
    public ShapeBorder? Shape { get; }
    public ShapeBorder? CollapsedShape { get; }
    public Clip? ClipBehavior { get; }
    public ListTileControlAffinity? ControlAffinity { get; }
    public ExpansibleController? Controller { get; }
    public bool? Dense { get; }
    public Color? SplashColor { get; }
    public VisualDensity? VisualDensity { get; }
    public double? MinTileHeight { get; }
    public bool? EnableFeedback { get; }
    public bool Enabled { get; }
    public AnimationStyle? ExpansionAnimationStyle { get; }
    public bool InternalAddSemanticForOnTap { get; }
    public WidgetStatesController? StatesController { get; }

    public override State CreateState() => new ExpansionTileState();

    private sealed class ExpansionTileState : State
    {
        private ExpansibleController? _controller;
        private bool _ownsController;
        private CancellationTokenSource? _announcementCancellation;

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
            _announcementCancellation?.Cancel();
            _announcementCancellation?.Dispose();
            _announcementCancellation = null;
            DetachController();
        }

        public override Widget Build(BuildContext context)
        {
            ExpansionTileThemeData expansionTheme = ExpansionTileTheme.Of(context);
            AnimationStyle animationStyle = CurrentWidget.ExpansionAnimationStyle
                                            ?? expansionTheme.ExpansionAnimationStyle
                                            ?? new AnimationStyle(
                                                Duration: TimeSpan.FromMilliseconds(200),
                                                Curve: Curves.EaseIn);

            return new Expansible(
                controller: _controller!,
                animationStyle: animationStyle,
                maintainState: CurrentWidget.MaintainState,
                headerBuilder: (buildContext, animation) =>
                    BuildHeader(buildContext, animation, expansionTheme),
                bodyBuilder: (buildContext, animation) => BuildBody(expansionTheme),
                expansibleBuilder: (buildContext, header, body, animation) =>
                    BuildExpansible(header, body, animation, expansionTheme));
        }

        private Widget BuildHeader(
            BuildContext context,
            Animation<double> animation,
            ExpansionTileThemeData expansionTheme)
        {
            ThemeData theme = Theme.Of(context);
            ExpansionTileThemeData defaults = ResolveDefaults(theme);
            double progress = Curves.EaseIn(animation.Value);
            Color expandedTextColor = CurrentWidget.TextColor
                                      ?? expansionTheme.TextColor
                                      ?? defaults.TextColor!.Value;
            Color collapsedTextColor = CurrentWidget.CollapsedTextColor
                                       ?? expansionTheme.CollapsedTextColor
                                       ?? defaults.CollapsedTextColor!.Value;
            Color expandedIconColor = CurrentWidget.IconColor
                                      ?? expansionTheme.IconColor
                                      ?? defaults.IconColor!.Value;
            Color collapsedIconColor = CurrentWidget.CollapsedIconColor
                                       ?? expansionTheme.CollapsedIconColor
                                       ?? defaults.CollapsedIconColor!.Value;
            Color textColor = LerpColor(collapsedTextColor, expandedTextColor, progress);
            Color iconColor = LerpColor(collapsedIconColor, expandedIconColor, progress);
            ListTileControlAffinity affinity = ResolveAffinity();
            Widget arrow = BuildArrow(animation, iconColor);
            Widget? leading = affinity == ListTileControlAffinity.Leading
                ? CurrentWidget.Leading ?? arrow
                : CurrentWidget.Leading;
            Widget? trailing = CurrentWidget.ShowTrailingIcon
                ? affinity == ListTileControlAffinity.Trailing
                    ? CurrentWidget.Trailing ?? arrow
                    : CurrentWidget.Trailing
                : null;

            Widget child = ListTileTheme.Merge(
                iconColor: MaterialStateProperty<Color?>.All(iconColor),
                textColor: MaterialStateProperty<Color?>.All(textColor),
                child: new ListTile(
                    enabled: CurrentWidget.Enabled,
                    onTap: _controller!.IsExpanded ? _controller.Collapse : _controller.Expand,
                    dense: CurrentWidget.Dense,
                    splashColor: CurrentWidget.SplashColor,
                    visualDensity: CurrentWidget.VisualDensity,
                    enableFeedback: CurrentWidget.EnableFeedback,
                    contentPadding: CurrentWidget.TilePadding ?? expansionTheme.TilePadding,
                    leading: leading,
                    title: CurrentWidget.Title,
                    subtitle: CurrentWidget.Subtitle,
                    trailing: trailing,
                    minTileHeight: CurrentWidget.MinTileHeight,
                    internalAddSemanticForOnTap: CurrentWidget.InternalAddSemanticForOnTap,
                    statesController: CurrentWidget.StatesController));

            MaterialLocalizations localizations = MaterialLocalizations.Of(context);
            string onTapHint = _controller.IsExpanded
                ? localizations.ExpansionTileExpandedTapHint
                : localizations.ExpansionTileCollapsedTapHint;
            TargetPlatform platform = PlatformDefaults.TargetPlatform;
            string semanticsHint = platform is TargetPlatform.IOS or TargetPlatform.MacOS
                ? _controller.IsExpanded
                    ? $"{localizations.CollapsedHint}\n {localizations.ExpansionTileExpandedHint}"
                    : $"{localizations.ExpandedHint}\n {localizations.ExpansionTileCollapsedHint}"
                : _controller.IsExpanded
                    ? localizations.CollapsedHint
                    : localizations.ExpandedHint;
            child = new Semantics(
                hint: semanticsHint,
                onTapHint: onTapHint,
                child: child);
            return platform == TargetPlatform.Android
                ? new Semantics(
                    label: semanticsHint,
                    liveRegion: true,
                    child: child)
                : child;
        }

        private Widget BuildBody(ExpansionTileThemeData expansionTheme)
        {
            Widget body = new Column(
                crossAxisAlignment: CurrentWidget.ExpandedCrossAxisAlignment
                                    ?? CrossAxisAlignment.Center,
                children: CurrentWidget.Children);
            body = new Padding(
                CurrentWidget.ChildrenPadding
                ?? expansionTheme.ChildrenPadding
                ?? EdgeInsetsGeometry.Zero,
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
            Animation<double> animation,
            ExpansionTileThemeData expansionTheme)
        {
            ThemeData theme = Theme.Of(Context);
            double progress = Curves.EaseOut(animation.Value);
            Color? background = MaterialThemeLerp.Color(
                CurrentWidget.CollapsedBackgroundColor ?? expansionTheme.CollapsedBackgroundColor,
                CurrentWidget.BackgroundColor ?? expansionTheme.BackgroundColor,
                progress);
            Color backgroundColor = background ?? expansionTheme.BackgroundColor ?? Colors.Transparent;
            ShapeBorder collapsedShape = CurrentWidget.CollapsedShape
                                         ?? expansionTheme.CollapsedShape
                                         ?? new Plumix.Rendering.Border(
                                             top: new BorderSide(Colors.Transparent),
                                             bottom: new BorderSide(Colors.Transparent));
            ShapeBorder expandedShape = CurrentWidget.Shape
                                        ?? expansionTheme.Shape
                                        ?? new Plumix.Rendering.Border(
                                            top: new BorderSide(theme.DividerColor),
                                            bottom: new BorderSide(theme.DividerColor));
            ShapeBorder expansionTileBorder = MaterialThemeLerp.Shape(collapsedShape, expandedShape, progress)!;
            Clip clipBehavior = CurrentWidget.ClipBehavior
                                ?? expansionTheme.ClipBehavior
                                ?? Clip.AntiAlias;
            Widget tile = new Padding(
                expansionTileBorder.Dimensions.Resolve(TextDirection.Ltr),
                new Column(
                    mainAxisSize: MainAxisSize.Min,
                    children: [header, body]));
            bool isShapeProvided = CurrentWidget.Shape is not null
                                   || expansionTheme.Shape is not null
                                   || CurrentWidget.CollapsedShape is not null
                                   || expansionTheme.CollapsedShape is not null;
            if (isShapeProvided)
            {
                return new Material(
                    clipBehavior: clipBehavior,
                    color: backgroundColor,
                    shape: expansionTileBorder,
                    child: tile);
            }

            if (backgroundColor.A > 0)
            {
                tile = new Material(type: MaterialType.Transparency, child: tile);
            }

            return new DecoratedBox(
                decoration: new ShapeDecoration(expansionTileBorder, backgroundColor),
                child: tile);
        }

        private static ExpansionTileThemeData ResolveDefaults(ThemeData theme)
        {
            return theme.UseMaterial3
                ? new ExpansionTileThemeData(
                    TextColor: theme.ColorScheme.OnSurface,
                    IconColor: theme.ColorScheme.Primary,
                    CollapsedTextColor: theme.ColorScheme.OnSurface,
                    CollapsedIconColor: theme.ColorScheme.OnSurfaceVariant)
                : new ExpansionTileThemeData(
                    TextColor: theme.ColorScheme.Primary,
                    IconColor: theme.ColorScheme.Primary,
                    CollapsedTextColor: theme.TextTheme.TitleMedium.Color,
                    CollapsedIconColor: theme.UnselectedWidgetColor);
        }

        private Widget BuildArrow(Animation<double> animation, Color color)
        {
            double turns = Curves.EaseIn(animation.Value) * 0.5;
            return new RotationTransition(
                turns: new ConstantAnimation<double>(turns, animation.Status),
                child: new Icon(Icons.ExpandMore, color: color));
        }

        private ListTileControlAffinity ResolveAffinity()
        {
            ListTileControlAffinity affinity = CurrentWidget.ControlAffinity
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
            AnnounceExpansionState();
            CurrentWidget.OnExpansionChanged?.Invoke(_controller!.IsExpanded);
        }

        private void AnnounceExpansionState()
        {
            TargetPlatform platform = PlatformDefaults.TargetPlatform;
            if (platform == TargetPlatform.Android)
            {
                return;
            }

            MaterialLocalizations localizations = MaterialLocalizations.Of(Context);
            string stateHint = _controller!.IsExpanded
                ? localizations.CollapsedHint
                : localizations.ExpandedHint;
            TextDirection direction = Localizations.MaybeOf<WidgetsLocalizations>(Context)?.TextDirection
                                      ?? Directionality.Of(Context);
            int viewId = MediaQuery.MaybeOf(Context)?.ViewId ?? 0;
            if (platform == TargetPlatform.IOS)
            {
                _announcementCancellation?.Cancel();
                _announcementCancellation?.Dispose();
                _announcementCancellation = new CancellationTokenSource();
                _ = SendDelayedAnnouncement(viewId, stateHint, direction, _announcementCancellation.Token);
                return;
            }

            _ = SemanticsService.SendAnnouncement(viewId, stateHint, direction);
        }

        private static async Task SendDelayedAnnouncement(
            int viewId,
            string stateHint,
            TextDirection direction,
            CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
                await SemanticsService.SendAnnouncement(viewId, stateHint, direction).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        private static Color LerpColor(Color from, Color to, double progress)
        {
            return new ColorTween().Evaluate(Math.Clamp(progress, 0.0, 1.0), from, to);
        }
    }
}
