using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/banner.dart

public enum MaterialBannerClosedReason
{
    Dismiss,
    Swipe,
    Hide,
    Remove,
}

public sealed class MaterialBanner : StatefulWidget
{
    public static readonly TimeSpan TransitionDuration = TimeSpan.FromMilliseconds(250);
    public const double MaxContentTextScaleFactor = 1.5;

    public MaterialBanner(
        Widget content,
        IReadOnlyList<Widget> actions,
        TextStyle? contentTextStyle = null,
        double? elevation = null,
        Widget? leading = null,
        Color? backgroundColor = null,
        Color? surfaceTintColor = null,
        Color? shadowColor = null,
        Color? dividerColor = null,
        EdgeInsetsGeometry? padding = null,
        EdgeInsetsGeometry? margin = null,
        EdgeInsetsGeometry? leadingPadding = null,
        bool forceActionsBelow = false,
        OverflowBarAlignment overflowAlignment = OverflowBarAlignment.End,
        Animation<double>? animation = null,
        Action? onVisible = null,
        double minActionBarHeight = 52.0,
        Key? key = null) : base(key)
    {
        Content = content ?? throw new ArgumentNullException(nameof(content));
        Actions = actions ?? throw new ArgumentNullException(nameof(actions));
        if (Actions.Count == 0) throw new ArgumentException("MaterialBanner actions must not be empty.", nameof(actions));
        ValidateNonNegativeFinite(elevation, nameof(elevation));
        ValidateNonNegativeFinite(minActionBarHeight, nameof(minActionBarHeight));
        ValidateInsets(padding, nameof(padding));
        ValidateInsets(margin, nameof(margin));
        ValidateInsets(leadingPadding, nameof(leadingPadding));

        ContentTextStyle = contentTextStyle;
        Elevation = elevation;
        Leading = leading;
        BackgroundColor = backgroundColor;
        SurfaceTintColor = surfaceTintColor;
        ShadowColor = shadowColor;
        DividerColor = dividerColor;
        Padding = padding;
        Margin = margin;
        LeadingPadding = leadingPadding;
        ForceActionsBelow = forceActionsBelow;
        OverflowAlignment = overflowAlignment;
        Animation = animation;
        OnVisible = onVisible;
        MinActionBarHeight = minActionBarHeight;
    }

    public Widget Content { get; }
    public TextStyle? ContentTextStyle { get; }
    public IReadOnlyList<Widget> Actions { get; }
    public double? Elevation { get; }
    public Widget? Leading { get; }
    public double MinActionBarHeight { get; }
    public Color? BackgroundColor { get; }
    public Color? SurfaceTintColor { get; }
    public Color? ShadowColor { get; }
    public Color? DividerColor { get; }
    public EdgeInsetsGeometry? Padding { get; }
    public EdgeInsetsGeometry? Margin { get; }
    public EdgeInsetsGeometry? LeadingPadding { get; }
    public bool ForceActionsBelow { get; }
    public OverflowBarAlignment OverflowAlignment { get; }
    public Animation<double>? Animation { get; }
    public Action? OnVisible { get; }

    public static AnimationController CreateAnimationController() => new(TransitionDuration);

    public MaterialBanner WithAnimation(Animation<double> animation, Key? fallbackKey = null)
    {
        ArgumentNullException.ThrowIfNull(animation);
        return new MaterialBanner(
            content: Content,
            contentTextStyle: ContentTextStyle,
            actions: Actions,
            elevation: Elevation,
            leading: Leading,
            minActionBarHeight: MinActionBarHeight,
            backgroundColor: BackgroundColor,
            surfaceTintColor: SurfaceTintColor,
            shadowColor: ShadowColor,
            dividerColor: DividerColor,
            padding: Padding,
            margin: Margin,
            leadingPadding: LeadingPadding,
            forceActionsBelow: ForceActionsBelow,
            overflowAlignment: OverflowAlignment,
            animation: animation,
            onVisible: OnVisible,
            key: Key ?? fallbackKey);
    }

    public override State CreateState() => new MaterialBannerState();

    private static void ValidateNonNegativeFinite(double? value, string parameterName)
    {
        if (value.HasValue && (!double.IsFinite(value.Value) || value.Value < 0))
        {
            throw new ArgumentOutOfRangeException(parameterName, "Value must be non-negative and finite.");
        }
    }

    private static void ValidateInsets(EdgeInsetsGeometry? value, string parameterName)
    {
        if (!value.HasValue) return;
        var insets = value.Value;
        if (!double.IsFinite(insets.Left) || !double.IsFinite(insets.Top)
            || !double.IsFinite(insets.Right) || !double.IsFinite(insets.Bottom)
            || !double.IsFinite(insets.Start) || !double.IsFinite(insets.End)
            || insets.Left < 0 || insets.Top < 0 || insets.Right < 0 || insets.Bottom < 0
            || insets.Start < 0 || insets.End < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Insets must be non-negative and finite.");
        }
    }

    private sealed class MaterialBannerState : State
    {
        private bool _wasVisible;

        private MaterialBanner CurrentWidget => (MaterialBanner)Element.Widget;

        public override void InitState()
        {
            base.InitState();
            Subscribe(CurrentWidget.Animation);
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            base.DidUpdateWidget(oldWidget);
            var oldBanner = (MaterialBanner)oldWidget;
            if (ReferenceEquals(oldBanner.Animation, CurrentWidget.Animation)) return;
            Unsubscribe(oldBanner.Animation);
            Subscribe(CurrentWidget.Animation);
        }

        public override void Dispose()
        {
            Unsubscribe(CurrentWidget.Animation);
            base.Dispose();
        }

        public override Widget Build(BuildContext context)
        {
            var widget = CurrentWidget;
            var mediaQuery = MediaQuery.Of(context);
            var theme = Theme.Of(context);
            var bannerTheme = MaterialBannerTheme.Of(context);
            var direction = Directionality.Of(context);
            bool isSingleRow = widget.Actions.Count == 1 && !widget.ForceActionsBelow;

            EdgeInsetsGeometry contentPaddingGeometry = widget.Padding
                                                         ?? bannerTheme.Padding
                                                         ?? (isSingleRow
                                                             ? EdgeInsetsDirectional.Only(
                                                                 start: 16,
                                                                 top: 2)
                                                             : EdgeInsetsDirectional.Only(
                                                                 start: 16,
                                                                 top: 24,
                                                                 end: 16,
                                                                 bottom: 4));
            EdgeInsetsGeometry leadingPaddingGeometry = widget.LeadingPadding
                                                         ?? bannerTheme.LeadingPadding
                                                         ?? EdgeInsetsDirectional.Only(end: 16);
            ValidateInsets(contentPaddingGeometry, nameof(MaterialBannerThemeData.Padding));
            ValidateInsets(leadingPaddingGeometry, nameof(MaterialBannerThemeData.LeadingPadding));
            Thickness contentPadding = contentPaddingGeometry.Resolve(direction);
            Thickness leadingPadding = leadingPaddingGeometry.Resolve(direction);

            Widget actionsBar = new ConstrainedBox(
                constraints: new BoxConstraints(MinHeight: widget.MinActionBarHeight),
                child: new Padding(
                    insets: new Thickness(8, 0),
                    child: new Align(
                        alignment: direction == TextDirection.Ltr ? Alignment.CenterRight : Alignment.CenterLeft,
                        child: new OverflowBar(
                            overflowAlignment: widget.OverflowAlignment,
                            spacing: 8,
                            textDirection: direction,
                            children: widget.Actions))));

            double elevation = widget.Elevation ?? bannerTheme.Elevation ?? 0;
            EdgeInsetsGeometry marginGeometry = widget.Margin
                                                 ?? EdgeInsets.Only(bottom: elevation > 0 ? 10 : 0);
            Thickness margin = marginGeometry.Resolve(direction);
            var backgroundColor = widget.BackgroundColor
                                  ?? bannerTheme.BackgroundColor
                                  ?? (theme.UseMaterial3 ? theme.SurfaceContainerLowColor : theme.SurfaceColor);
            var surfaceTintColor = widget.SurfaceTintColor
                                   ?? bannerTheme.SurfaceTintColor
                                   ?? (theme.UseMaterial3 ? Colors.Transparent : null);
            var shadowColor = widget.ShadowColor ?? bannerTheme.ShadowColor;
            var dividerColor = widget.DividerColor
                               ?? bannerTheme.DividerColor
                               ?? (theme.UseMaterial3 ? theme.OutlineVariantColor : null);
            var textStyle = widget.ContentTextStyle ?? bannerTheme.ContentTextStyle ?? theme.TextTheme.BodyMedium;

            Widget content = new Expanded(
                new DefaultTextStyle(textStyle, widget.Content));
            content = MediaQuery.WithClampedTextScaling(
                context,
                content,
                maxScaleFactor: MaxContentTextScaleFactor);

            var rowChildren = new List<Widget>();
            if (widget.Leading is not null)
            {
                rowChildren.Add(new Padding(leadingPadding, widget.Leading));
            }
            rowChildren.Add(content);
            if (isSingleRow)
            {
                rowChildren.Add(MediaQuery.WithClampedTextScaling(
                    context,
                    actionsBar,
                    maxScaleFactor: MaxContentTextScaleFactor));
            }

            var columnChildren = new List<Widget>
            {
                new Padding(
                    contentPadding,
                    new Row(
                        children: rowChildren,
                        textDirection: direction)),
            };
            if (!isSingleRow) columnChildren.Add(actionsBar);
            if (elevation == 0) columnChildren.Add(new Divider(height: 0, color: dividerColor));

            Widget materialBanner = new Padding(
                margin,
                new Material(
                    elevation: elevation,
                    color: backgroundColor,
                    surfaceTintColor: surfaceTintColor,
                    shadowColor: shadowColor,
                    child: new Column(
                        mainAxisSize: MainAxisSize.Min,
                        children: columnChildren)));

            if (widget.Animation is null) return materialBanner;

            materialBanner = new SafeArea(materialBanner);
            if (!mediaQuery.AccessibleNavigation)
            {
                double slideValue = widget.Animation.Value <= 0 ? -1.0 : 0.0;
                materialBanner = new FractionalTranslation(
                    translation: new Vector(0, slideValue),
                    child: materialBanner);
            }

            materialBanner = new Semantics(
                container: true,
                liveRegion: true,
                onDismiss: HandleDismiss,
                child: materialBanner);

            if (!mediaQuery.AccessibleNavigation)
            {
                materialBanner = new Align(
                    alignment: direction == TextDirection.Ltr ? Alignment.BottomLeft : Alignment.BottomRight,
                    heightFactor: Curves.FastOutSlowIn(widget.Animation.Value),
                    child: materialBanner);
            }

            return new Hero(
                tag: $"<MaterialBanner Hero tag - {widget.Content}>",
                child: new ClipRect(child: materialBanner));
        }

        private void HandleDismiss()
        {
            ScaffoldMessenger.Of(Context).RemoveCurrentMaterialBanner(MaterialBannerClosedReason.Dismiss);
        }

        private void Subscribe(Animation<double>? animation)
        {
            if (animation is null) return;
            animation.AddListener(HandleAnimationChanged);
            animation.AddStatusListener(HandleAnimationStatusChanged);
        }

        private void Unsubscribe(Animation<double>? animation)
        {
            if (animation is null) return;
            animation.RemoveListener(HandleAnimationChanged);
            animation.RemoveStatusListener(HandleAnimationStatusChanged);
        }

        private void HandleAnimationChanged() => SetState(() => { });

        private void HandleAnimationStatusChanged(AnimationStatus status)
        {
            if (status != AnimationStatus.Completed || _wasVisible) return;
            _wasVisible = true;
            CurrentWidget.OnVisible?.Invoke();
        }
    }
}
