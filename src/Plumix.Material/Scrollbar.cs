using Avalonia;
using Avalonia.Media;
using Plumix.Cupertino;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/scrollbar.dart

/// <summary>A Material Design scrollbar.</summary>
/// <remarks>
/// To add a scrollbar to a <see cref="ScrollView"/>, wrap the scroll view widget in a
/// <see cref="Scrollbar"/> widget. On iOS the widget delegates to <see cref="CupertinoScrollbar"/>.
/// </remarks>
public sealed class Scrollbar : StatelessWidget
{
    internal const double KScrollbarThickness = 8.0;
    internal const double KScrollbarThicknessWithTrack = 12.0;
    internal const double KScrollbarMargin = 2.0;
    internal const double KScrollbarMinLength = 48.0;
    internal const double KScrollbarRadius = 8.0;

    private static readonly TimeSpan KScrollbarFadeDuration = TimeSpan.FromMilliseconds(300);
    private static readonly TimeSpan KScrollbarTimeToFade = TimeSpan.FromMilliseconds(600);

    /// <summary>Creates a Material Design scrollbar that wraps the given <paramref name="child"/>.</summary>
    public Scrollbar(
        Widget child,
        ScrollController? controller = null,
        bool? thumbVisibility = null,
        bool? trackVisibility = null,
        double? thickness = null,
        double? radius = null,
        ScrollNotificationPredicate? notificationPredicate = null,
        bool? interactive = null,
        ScrollbarOrientation? scrollbarOrientation = null,
        Key? key = null) : base(key)
    {
        Child = child ?? throw new ArgumentNullException(nameof(child));
        Controller = controller;
        ThumbVisibility = thumbVisibility;
        TrackVisibility = trackVisibility;
        Thickness = thickness;
        Radius = radius;
        NotificationPredicate = notificationPredicate;
        Interactive = interactive;
        ScrollbarOrientation = scrollbarOrientation;
    }

    public Widget Child { get; }
    public ScrollController? Controller { get; }
    public bool? ThumbVisibility { get; }
    public bool? TrackVisibility { get; }
    public double? Thickness { get; }
    public double? Radius { get; }
    public ScrollNotificationPredicate? NotificationPredicate { get; }
    public bool? Interactive { get; }
    public ScrollbarOrientation? ScrollbarOrientation { get; }

    public override Widget Build(BuildContext context)
    {
        if (Theme.Of(context).Platform == TargetPlatform.IOS)
        {
            return new CupertinoScrollbar(
                thumbVisibility: ThumbVisibility ?? false,
                thickness: Thickness ?? CupertinoScrollbar.DefaultThickness,
                thicknessWhileDragging: Thickness ?? CupertinoScrollbar.DefaultThicknessWhileDragging,
                radius: Radius ?? CupertinoScrollbar.DefaultRadius,
                radiusWhileDragging: Radius ?? CupertinoScrollbar.DefaultRadiusWhileDragging,
                controller: Controller,
                notificationPredicate: NotificationPredicate,
                scrollbarOrientation: ScrollbarOrientation,
                child: Child);
        }

        return new MaterialScrollbar(
            controller: Controller,
            thumbVisibility: ThumbVisibility,
            trackVisibility: TrackVisibility,
            thickness: Thickness,
            radius: Radius,
            notificationPredicate: NotificationPredicate,
            interactive: Interactive,
            scrollbarOrientation: ScrollbarOrientation,
            child: Child);
    }

    internal sealed class MaterialScrollbar : RawScrollbar
    {
        public MaterialScrollbar(
            Widget child,
            ScrollController? controller = null,
            bool? thumbVisibility = null,
            bool? trackVisibility = null,
            double? thickness = null,
            double? radius = null,
            ScrollNotificationPredicate? notificationPredicate = null,
            bool? interactive = null,
            ScrollbarOrientation? scrollbarOrientation = null,
            Key? key = null) : base(
            child: child,
            controller: controller,
            thumbVisibility: thumbVisibility,
            trackVisibility: trackVisibility,
            thickness: thickness,
            radius: radius,
            fadeDuration: KScrollbarFadeDuration,
            timeToFade: KScrollbarTimeToFade,
            pressDuration: TimeSpan.Zero,
            notificationPredicate: notificationPredicate ?? DefaultScrollNotificationPredicate,
            interactive: interactive,
            scrollbarOrientation: scrollbarOrientation,
            key: key)
        {
        }

        public override State CreateState() => new MaterialScrollbarState();
    }

    private sealed class MaterialScrollbarState : RawScrollbarState<MaterialScrollbar>
    {
        private AnimationController _hoverAnimationController = null!;
        private bool _dragIsActive;
        private bool _hoverIsActive;
        private ColorScheme _colorScheme = null!;
        private ScrollbarThemeData _scrollbarTheme = null!;

        // On Android, scrollbars should match native appearance.
        private bool _useAndroidScrollbar;

        protected override bool ShowScrollbar =>
            CurrentWidget.ThumbVisibility ?? _scrollbarTheme.ThumbVisibility?.Resolve(States) ?? false;

        protected override bool EnableGestures =>
            CurrentWidget.Interactive ?? _scrollbarTheme.Interactive ?? !_useAndroidScrollbar;

        private WidgetStateProperty<bool> TrackVisibility =>
            WidgetStateProperty<bool>.ResolveWith(states =>
                CurrentWidget.TrackVisibility ?? _scrollbarTheme.TrackVisibility?.Resolve(states) ?? false);

        private IReadOnlySet<WidgetState> States
        {
            get
            {
                var states = new HashSet<WidgetState>();
                if (_dragIsActive) states.Add(WidgetState.Dragged);
                if (_hoverIsActive) states.Add(WidgetState.Hovered);
                return states;
            }
        }

        private WidgetStateProperty<Color> ThumbColor
        {
            get
            {
                Color onSurface = _colorScheme.OnSurface;
                Brightness brightness = _colorScheme.Brightness;
                Color dragColor;
                Color hoverColor;
                Color idleColor;
                if (brightness == Brightness.Light)
                {
                    dragColor = WithOpacity(onSurface, 0.6);
                    hoverColor = WithOpacity(onSurface, 0.5);
                    idleColor = _useAndroidScrollbar
                        ? WithOpacity(Theme.Of(Context).HighlightColor, 1.0)
                        : WithOpacity(onSurface, 0.1);
                }
                else
                {
                    dragColor = WithOpacity(onSurface, 0.75);
                    hoverColor = WithOpacity(onSurface, 0.65);
                    idleColor = _useAndroidScrollbar
                        ? WithOpacity(Theme.Of(Context).HighlightColor, 1.0)
                        : WithOpacity(onSurface, 0.3);
                }

                return WidgetStateProperty<Color>.ResolveWith(states =>
                {
                    if (states.Contains(WidgetState.Dragged))
                    {
                        return _scrollbarTheme.ThumbColor?.Resolve(states) ?? dragColor;
                    }

                    // If the track is visible, the thumb color hover animation is ignored and changes
                    // immediately.
                    if (TrackVisibility.Resolve(states))
                    {
                        return _scrollbarTheme.ThumbColor?.Resolve(states) ?? hoverColor;
                    }

                    return new ColorTween().Evaluate(
                        _hoverAnimationController.Value,
                        _scrollbarTheme.ThumbColor?.Resolve(states) ?? idleColor,
                        _scrollbarTheme.ThumbColor?.Resolve(states) ?? hoverColor);
                });
            }
        }

        private WidgetStateProperty<Color> TrackColor
        {
            get
            {
                Color onSurface = _colorScheme.OnSurface;
                Brightness brightness = _colorScheme.Brightness;
                return WidgetStateProperty<Color>.ResolveWith(states =>
                {
                    if (ShowScrollbar && TrackVisibility.Resolve(states))
                    {
                        return _scrollbarTheme.TrackColor?.Resolve(states)
                               ?? WithOpacity(onSurface, brightness == Brightness.Light ? 0.03 : 0.05);
                    }

                    return Transparent;
                });
            }
        }

        private WidgetStateProperty<Color> TrackBorderColor
        {
            get
            {
                Color onSurface = _colorScheme.OnSurface;
                Brightness brightness = _colorScheme.Brightness;
                return WidgetStateProperty<Color>.ResolveWith(states =>
                {
                    if (ShowScrollbar && TrackVisibility.Resolve(states))
                    {
                        return _scrollbarTheme.TrackBorderColor?.Resolve(states)
                               ?? WithOpacity(onSurface, brightness == Brightness.Light ? 0.1 : 0.25);
                    }

                    return Transparent;
                });
            }
        }

        private WidgetStateProperty<double> Thickness =>
            WidgetStateProperty<double>.ResolveWith(states =>
            {
                if (states.Contains(WidgetState.Hovered) && TrackVisibility.Resolve(states))
                {
                    return CurrentWidget.Thickness
                           ?? _scrollbarTheme.Thickness?.Resolve(states)
                           ?? KScrollbarThicknessWithTrack;
                }

                // The default scrollbar thickness is smaller on mobile.
                return CurrentWidget.Thickness
                       ?? _scrollbarTheme.Thickness?.Resolve(states)
                       ?? KScrollbarThickness / (_useAndroidScrollbar ? 2 : 1);
            });

        public override void InitState()
        {
            base.InitState();
            _hoverAnimationController = new AnimationController(
                duration: TimeSpan.FromMilliseconds(200),
                vsync: this);
            _hoverAnimationController.Changed += HandleHoverAnimationTick;
        }

        public override void DidChangeDependencies()
        {
            ThemeData theme = Theme.Of(Context);
            _colorScheme = theme.ColorScheme;
            _scrollbarTheme = ScrollbarTheme.Of(Context);
            _useAndroidScrollbar = theme.Platform == TargetPlatform.Android;
            base.DidChangeDependencies();
        }

        protected override void UpdateScrollbarPainter()
        {
            IReadOnlySet<WidgetState> states = States;
            ScrollbarPainter.Color = ThumbColor.Resolve(states);
            ScrollbarPainter.TrackColor = TrackColor.Resolve(states);
            ScrollbarPainter.TrackBorderColor = TrackBorderColor.Resolve(states);
            ScrollbarPainter.TextDirection = Directionality.Of(Context);
            ScrollbarPainter.Thickness = Thickness.Resolve(states);
            ScrollbarPainter.Radius = CurrentWidget.Radius
                                      ?? _scrollbarTheme.Radius
                                      ?? (_useAndroidScrollbar ? null : KScrollbarRadius);
            ScrollbarPainter.CrossAxisMargin = _scrollbarTheme.CrossAxisMargin
                                               ?? (_useAndroidScrollbar ? 0.0 : KScrollbarMargin);
            ScrollbarPainter.MainAxisMargin = _scrollbarTheme.MainAxisMargin ?? 0.0;
            ScrollbarPainter.MinLength = _scrollbarTheme.MinThumbLength ?? KScrollbarMinLength;
            ScrollbarPainter.Padding = MediaQuery.MaybePaddingOf(Context) ?? default;
            ScrollbarPainter.ScrollbarOrientation = CurrentWidget.ScrollbarOrientation;
            ScrollbarPainter.IgnorePointer = !EnableGestures;
        }

        protected override void HandleThumbPressStart(Point localPosition)
        {
            base.HandleThumbPressStart(localPosition);
            SetState(() => _dragIsActive = true);
        }

        protected override void HandleThumbPressEnd(Point localPosition, Velocity velocity)
        {
            base.HandleThumbPressEnd(localPosition, velocity);
            SetState(() => _dragIsActive = false);
        }

        protected override void HandleHover(PointerHoverEvent @event)
        {
            base.HandleHover(@event);

            // Check if the position of the pointer falls over the painted scrollbar.
            if (IsPointerOverScrollbar(@event.Position, @event.Kind, forHover: true))
            {
                // Pointer is hovering over the scrollbar.
                SetState(() => _hoverIsActive = true);
                _hoverAnimationController.Forward();
            }
            else if (_hoverIsActive)
            {
                // Pointer was, but is no longer over the painted scrollbar.
                SetState(() => _hoverIsActive = false);
                _hoverAnimationController.Reverse();
            }
        }

        protected override void HandleHoverExit(PointerExitEvent @event)
        {
            base.HandleHoverExit(@event);
            SetState(() => _hoverIsActive = false);
            _hoverAnimationController.Reverse();
        }

        public override void Dispose()
        {
            _hoverAnimationController.Changed -= HandleHoverAnimationTick;
            _hoverAnimationController.Dispose();
            base.Dispose();
        }

        private void HandleHoverAnimationTick()
        {
            if (Mounted)
            {
                UpdateScrollbarPainter();
            }
        }

        private static Color Transparent => Color.FromArgb(0x00, 0x00, 0x00, 0x00);
    }

    // Dart's `Color.withOpacity` replaces the alpha channel outright rather than scaling it.
    private static Color WithOpacity(Color color, double opacity) => Color.FromArgb(
        (byte)Math.Round(Math.Clamp(opacity, 0, 1) * 255),
        color.R,
        color.G,
        color.B);
}
