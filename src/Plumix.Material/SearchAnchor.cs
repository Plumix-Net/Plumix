using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Gestures;
using Plumix.Painting;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/search_anchor.dart

public enum TextInputAction
{
    None,
    Search,
    Done,
    Go,
    Next,
    Send,
}

public delegate Widget SearchAnchorChildBuilder(BuildContext context, SearchController controller);

public delegate ValueTask<IReadOnlyList<Widget>> SuggestionsBuilder(BuildContext context, SearchController controller);

public delegate Widget ViewBuilder(IReadOnlyList<Widget> suggestions);

internal static class SearchViewChoreography
{
    internal static readonly TimeSpan OpenViewDuration = TimeSpan.FromMilliseconds(600);
    internal static readonly TimeSpan AnchorFadeDuration = TimeSpan.FromMilliseconds(150);
    internal static readonly Curve ViewFadeOnInterval = Curves.Interval(0.0, 1.0 / 2.0);
    internal static readonly Curve ViewIconsFadeOnInterval = Curves.Interval(1.0 / 6.0, 2.0 / 6.0);
    internal static readonly Curve ViewDividerFadeOnInterval = Curves.Interval(0.0, 1.0 / 6.0);
    internal static readonly Curve ViewListFadeOnInterval = Curves.Interval(133.0 / 600.0, 233.0 / 600.0);
    internal const double DisableSearchBarOpacity = 0.38;

    internal static double ClampDouble(double value, double min, double max)
    {
        // Mirrors Dart's clampDouble: no min <= max validation, max wins on conflict.
        if (value < min)
        {
            value = min;
        }

        return value > max ? max : value;
    }
}

public sealed class SearchController : TextEditingController
{
    private SearchAnchor.SearchAnchorState? _anchor;

    public SearchController(string text = "") : base(text)
    {
    }

    public bool IsAttached => _anchor is not null;

    public bool IsOpen
    {
        get
        {
            EnsureAttached();
            return _anchor!.ViewIsOpen;
        }
    }

    public void OpenView()
    {
        EnsureAttached();
        _anchor!.OpenView();
    }

    public void CloseView(string? selectedText)
    {
        EnsureAttached();
        _anchor!.CloseView(selectedText);
    }

    internal void Attach(SearchAnchor.SearchAnchorState anchor)
    {
        _anchor = anchor;
    }

    internal void Detach(SearchAnchor.SearchAnchorState anchor)
    {
        if (ReferenceEquals(_anchor, anchor))
        {
            _anchor = null;
        }
    }

    private void EnsureAttached()
    {
        if (_anchor is null)
        {
            throw new InvalidOperationException("SearchController is not attached to a SearchAnchor.");
        }
    }
}

public class SearchAnchor : StatefulWidget
{
    public SearchAnchor(
        SearchAnchorChildBuilder builder,
        SuggestionsBuilder suggestionsBuilder,
        bool? isFullScreen = null,
        SearchController? searchController = null,
        ViewBuilder? viewBuilder = null,
        Widget? viewLeading = null,
        IReadOnlyList<Widget>? viewTrailing = null,
        string? viewHintText = null,
        Color? viewBackgroundColor = null,
        double? viewElevation = null,
        Color? viewSurfaceTintColor = null,
        BorderSide? viewSide = null,
        OutlinedBorder? viewShape = null,
        EdgeInsetsGeometry? viewBarPadding = null,
        double? headerHeight = null,
        TextStyle? headerTextStyle = null,
        TextStyle? headerHintStyle = null,
        Color? dividerColor = null,
        BoxConstraints? viewConstraints = null,
        EdgeInsetsGeometry? viewPadding = null,
        bool? shrinkWrap = null,
        TextCapitalization? textCapitalization = null,
        Action<string>? viewOnChanged = null,
        Action<string>? viewOnSubmitted = null,
        Action? viewOnClose = null,
        Action? viewOnOpen = null,
        TextInputAction? textInputAction = null,
        TextInputType? keyboardType = null,
        bool enabled = true,
        SmartDashesType? smartDashesType = null,
        SmartQuotesType? smartQuotesType = null,
        Key? key = null) : base(key)
    {
        Builder = builder ?? throw new ArgumentNullException(nameof(builder));
        SuggestionsBuilder = suggestionsBuilder ?? throw new ArgumentNullException(nameof(suggestionsBuilder));
        IsFullScreen = isFullScreen;
        SearchController = searchController;
        ViewBuilder = viewBuilder;
        ViewLeading = viewLeading;
        ViewTrailing = viewTrailing;
        ViewHintText = viewHintText;
        ViewBackgroundColor = viewBackgroundColor;
        ViewElevation = viewElevation;
        ViewSurfaceTintColor = viewSurfaceTintColor;
        ViewSide = viewSide;
        ViewShape = viewShape;
        ViewBarPadding = viewBarPadding;
        HeaderHeight = headerHeight;
        HeaderTextStyle = headerTextStyle;
        HeaderHintStyle = headerHintStyle;
        DividerColor = dividerColor;
        ViewConstraints = viewConstraints;
        ViewPadding = viewPadding;
        ShrinkWrap = shrinkWrap;
        TextCapitalization = textCapitalization;
        ViewOnChanged = viewOnChanged;
        ViewOnSubmitted = viewOnSubmitted;
        ViewOnClose = viewOnClose;
        ViewOnOpen = viewOnOpen;
        TextInputAction = textInputAction;
        KeyboardType = keyboardType;
        Enabled = enabled;
        SmartDashesType = smartDashesType;
        SmartQuotesType = smartQuotesType;
    }

    public bool? IsFullScreen { get; }
    public SearchController? SearchController { get; }
    public ViewBuilder? ViewBuilder { get; }
    public Widget? ViewLeading { get; }
    public IReadOnlyList<Widget>? ViewTrailing { get; }
    public string? ViewHintText { get; }
    public Color? ViewBackgroundColor { get; }
    public double? ViewElevation { get; }
    public Color? ViewSurfaceTintColor { get; }
    public BorderSide? ViewSide { get; }
    public OutlinedBorder? ViewShape { get; }
    public EdgeInsetsGeometry? ViewBarPadding { get; }
    public double? HeaderHeight { get; }
    public TextStyle? HeaderTextStyle { get; }
    public TextStyle? HeaderHintStyle { get; }
    public Color? DividerColor { get; }
    public BoxConstraints? ViewConstraints { get; }
    public EdgeInsetsGeometry? ViewPadding { get; }
    public bool? ShrinkWrap { get; }
    public TextCapitalization? TextCapitalization { get; }
    public Action<string>? ViewOnChanged { get; }
    public Action<string>? ViewOnSubmitted { get; }
    public Action? ViewOnClose { get; }
    public Action? ViewOnOpen { get; }
    public SearchAnchorChildBuilder Builder { get; }
    public SuggestionsBuilder SuggestionsBuilder { get; }
    public TextInputAction? TextInputAction { get; }
    public TextInputType? KeyboardType { get; }
    public bool Enabled { get; }
    public SmartDashesType? SmartDashesType { get; }
    public SmartQuotesType? SmartQuotesType { get; }

    public static SearchAnchor Bar(
        SuggestionsBuilder suggestionsBuilder,
        Widget? barLeading = null,
        IReadOnlyList<Widget>? barTrailing = null,
        string? barHintText = null,
        Action? onTap = null,
        Action<string>? onSubmitted = null,
        Action<string>? onChanged = null,
        Action? onClose = null,
        Action? onOpen = null,
        MaterialStateProperty<double?>? barElevation = null,
        MaterialStateProperty<Color?>? barBackgroundColor = null,
        MaterialStateProperty<Color?>? barOverlayColor = null,
        MaterialStateProperty<BorderSide?>? barSide = null,
        MaterialStateProperty<OutlinedBorder?>? barShape = null,
        MaterialStateProperty<EdgeInsetsGeometry?>? barPadding = null,
        EdgeInsetsGeometry? viewBarPadding = null,
        MaterialStateProperty<TextStyle?>? barTextStyle = null,
        MaterialStateProperty<TextStyle?>? barHintStyle = null,
        ViewBuilder? viewBuilder = null,
        Widget? viewLeading = null,
        IReadOnlyList<Widget>? viewTrailing = null,
        string? viewHintText = null,
        Color? viewBackgroundColor = null,
        double? viewElevation = null,
        BorderSide? viewSide = null,
        OutlinedBorder? viewShape = null,
        double? viewHeaderHeight = null,
        TextStyle? viewHeaderTextStyle = null,
        TextStyle? viewHeaderHintStyle = null,
        Color? dividerColor = null,
        BoxConstraints? constraints = null,
        BoxConstraints? viewConstraints = null,
        EdgeInsetsGeometry? viewPadding = null,
        bool? shrinkWrap = null,
        bool? isFullScreen = null,
        SearchController? searchController = null,
        TextCapitalization? textCapitalization = null,
        TextInputAction? textInputAction = null,
        TextInputType? keyboardType = null,
        Thickness? scrollPadding = null,
        EditableTextContextMenuBuilder? contextMenuBuilder = null,
        bool enabled = true,
        SmartDashesType? smartDashesType = null,
        SmartQuotesType? smartQuotesType = null,
        Key? key = null)
    {
        return new SearchAnchor(
            builder: (context, controller) => new SearchBar(
                constraints: constraints,
                controller: controller,
                onTap: () =>
                {
                    controller.OpenView();
                    onTap?.Invoke();
                },
                onChanged: _ => controller.OpenView(),
                onSubmitted: onSubmitted,
                hintText: barHintText,
                hintStyle: barHintStyle,
                textStyle: barTextStyle,
                elevation: barElevation,
                backgroundColor: barBackgroundColor,
                overlayColor: barOverlayColor,
                side: barSide,
                shape: barShape,
                padding: barPadding
                         ?? MaterialStateProperty<EdgeInsetsGeometry?>.All(
                             EdgeInsetsGeometry.Symmetric(horizontal: 16.0)),
                leading: barLeading ?? new Icon(Icons.Search),
                trailing: barTrailing,
                textCapitalization: textCapitalization,
                textInputAction: textInputAction,
                keyboardType: keyboardType,
                scrollPadding: scrollPadding,
                contextMenuBuilder: contextMenuBuilder,
                enabled: enabled,
                smartDashesType: smartDashesType,
                smartQuotesType: smartQuotesType),
            suggestionsBuilder: suggestionsBuilder,
            isFullScreen: isFullScreen,
            searchController: searchController,
            viewBuilder: viewBuilder,
            viewLeading: viewLeading,
            viewTrailing: viewTrailing,
            viewHintText: viewHintText ?? barHintText,
            viewBackgroundColor: viewBackgroundColor,
            viewElevation: viewElevation,
            viewSide: viewSide,
            viewShape: viewShape,
            viewBarPadding: viewBarPadding,
            headerHeight: viewHeaderHeight,
            headerTextStyle: viewHeaderTextStyle,
            headerHintStyle: viewHeaderHintStyle,
            dividerColor: dividerColor,
            viewConstraints: viewConstraints,
            viewPadding: viewPadding,
            shrinkWrap: shrinkWrap,
            textCapitalization: textCapitalization,
            viewOnChanged: onChanged,
            viewOnSubmitted: onSubmitted,
            viewOnClose: onClose,
            viewOnOpen: onOpen,
            textInputAction: textInputAction,
            keyboardType: keyboardType,
            enabled: enabled,
            smartDashesType: smartDashesType,
            smartQuotesType: smartQuotesType,
            key: key);
    }

    public override State CreateState()
    {
        return new SearchAnchorState();
    }

    public sealed class SearchAnchorState : State
    {
        private readonly GlobalKey _anchorKey = new GlobalObjectKey<State>(new object());
        private bool _anchorIsVisible = true;
        private SearchController? _internalSearchController;
        private SearchViewRoute? _route;
        private Size? _screenSize;

        private SearchAnchor CurrentWidget => (SearchAnchor)StateWidget;

        internal bool ViewIsOpen => !_anchorIsVisible;

        private SearchController ControllerInstance =>
            CurrentWidget.SearchController ?? (_internalSearchController ??= new SearchController());

        public override void InitState()
        {
            ControllerInstance.Attach(this);
        }

        public override void DidChangeDependencies()
        {
            Size updatedScreenSize = MediaQuery.Of(Context).Size;
            if (_screenSize is not null && _screenSize != updatedScreenSize
                && ViewIsOpen && !GetShowFullScreenView())
            {
                CloseView(null);
            }

            _screenSize = updatedScreenSize;
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var old = (SearchAnchor)oldWidget;
            if (!ReferenceEquals(old.SearchController, CurrentWidget.SearchController))
            {
                old.SearchController?.Detach(this);
                ControllerInstance.Attach(this);
            }
        }

        public override void Dispose()
        {
            CurrentWidget.SearchController?.Detach(this);
            _internalSearchController?.Detach(this);
            bool usingExternalController = CurrentWidget.SearchController is not null;
            if (_route is not null && _route.Navigator is not null)
            {
                _route.Dismiss(disposeController: !usingExternalController);
            }
            else
            {
                _internalSearchController?.Dispose();
            }
        }

        public override Widget Build(BuildContext context)
        {
            return new AnimatedOpacity(
                opacity: GetOpacity(),
                duration: SearchViewChoreography.AnchorFadeDuration,
                key: _anchorKey,
                child: new IgnorePointer(
                    ignoring: !CurrentWidget.Enabled,
                    child: new GestureDetector(
                        excludeFromSemantics: true,
                        onTap: OpenView,
                        child: CurrentWidget.Builder(context, ControllerInstance))));
        }

        internal void OpenView()
        {
            if (ViewIsOpen)
            {
                return;
            }

            NavigatorState navigator = Navigator.Of(Context);
            _route = new SearchViewRoute(
                anchorKey: _anchorKey,
                toggleVisibility: ToggleVisibility,
                searchController: ControllerInstance,
                suggestionsBuilder: CurrentWidget.SuggestionsBuilder,
                viewBuilder: CurrentWidget.ViewBuilder,
                viewLeading: CurrentWidget.ViewLeading,
                viewTrailing: CurrentWidget.ViewTrailing,
                viewHintText: CurrentWidget.ViewHintText,
                viewBackgroundColor: CurrentWidget.ViewBackgroundColor,
                viewElevation: CurrentWidget.ViewElevation,
                viewSurfaceTintColor: CurrentWidget.ViewSurfaceTintColor,
                viewSide: CurrentWidget.ViewSide,
                viewShape: CurrentWidget.ViewShape,
                viewBarPadding: CurrentWidget.ViewBarPadding,
                viewHeaderHeight: CurrentWidget.HeaderHeight,
                viewHeaderTextStyle: CurrentWidget.HeaderTextStyle,
                viewHeaderHintStyle: CurrentWidget.HeaderHintStyle,
                dividerColor: CurrentWidget.DividerColor,
                viewConstraints: CurrentWidget.ViewConstraints,
                viewPadding: CurrentWidget.ViewPadding,
                shrinkWrap: CurrentWidget.ShrinkWrap,
                textCapitalization: CurrentWidget.TextCapitalization,
                viewOnChanged: CurrentWidget.ViewOnChanged,
                viewOnSubmitted: CurrentWidget.ViewOnSubmitted,
                viewOnOpen: CurrentWidget.ViewOnOpen,
                viewOnClose: CurrentWidget.ViewOnClose,
                textInputAction: CurrentWidget.TextInputAction,
                keyboardType: CurrentWidget.KeyboardType,
                smartDashesType: CurrentWidget.SmartDashesType,
                smartQuotesType: CurrentWidget.SmartQuotesType,
                capturedThemes: InheritedTheme.Capture(from: Context, to: navigator.Context),
                textDirection: Directionality.Of(Context),
                showFullScreenView: GetShowFullScreenView());
            navigator.Push(_route);
        }

        internal void CloseView(string? selectedText)
        {
            if (selectedText is not null)
            {
                ControllerInstance.Text = selectedText;
            }

            Navigator.Of(Context).Pop();
        }

        internal bool GetShowFullScreenView()
        {
            if (CurrentWidget.IsFullScreen.HasValue)
            {
                return CurrentWidget.IsFullScreen.Value;
            }

            return Theme.Of(Context).Platform
                is TargetPlatform.IOS or TargetPlatform.Android or TargetPlatform.Fuchsia;
        }

        private double GetOpacity()
        {
            if (!CurrentWidget.Enabled)
            {
                return SearchViewChoreography.DisableSearchBarOpacity;
            }

            return _anchorIsVisible ? 1.0 : 0.0;
        }

        private bool ToggleVisibility()
        {
            SetState(() => _anchorIsVisible = !_anchorIsVisible);
            return _anchorIsVisible;
        }
    }

    internal sealed class SearchViewRoute : PopupRoute
    {
        private readonly GlobalKey _anchorKey;
        private readonly Func<bool>? _toggleVisibility;
        private readonly SearchController _searchController;
        private readonly SuggestionsBuilder _suggestionsBuilder;
        private readonly ViewBuilder? _viewBuilder;
        private readonly Widget? _viewLeading;
        private readonly IReadOnlyList<Widget>? _viewTrailing;
        private readonly string? _viewHintText;
        private readonly Color? _viewBackgroundColor;
        private readonly double? _viewElevation;
        private readonly Color? _viewSurfaceTintColor;
        private readonly BorderSide? _viewSide;
        private readonly OutlinedBorder? _viewShape;
        private readonly EdgeInsetsGeometry? _viewBarPadding;
        private readonly double? _viewHeaderHeight;
        private readonly TextStyle? _viewHeaderTextStyle;
        private readonly TextStyle? _viewHeaderHintStyle;
        private readonly Color? _dividerColor;
        private readonly BoxConstraints? _viewConstraints;
        private readonly EdgeInsetsGeometry? _viewPadding;
        private readonly bool? _shrinkWrap;
        private readonly TextCapitalization? _textCapitalization;
        private readonly Action<string>? _viewOnChanged;
        private readonly Action<string>? _viewOnSubmitted;
        private readonly Action? _viewOnOpen;
        private readonly Action? _viewOnClose;
        private readonly TextInputAction? _textInputAction;
        private readonly TextInputType? _keyboardType;
        private readonly SmartDashesType? _smartDashesType;
        private readonly SmartQuotesType? _smartQuotesType;
        private readonly CapturedThemes _capturedThemes;
        private readonly TextDirection? _textDirection;
        private readonly bool _showFullScreenView;
        private readonly RectTween _rectTween = new();
        private SearchViewThemeData? _viewDefaults;
        private SearchViewThemeData? _viewTheme;
        private CurvedAnimation? _curvedAnimation;
        private CurvedAnimation? _viewFadeOnIntervalCurve;
        private bool _willDisposeSearchController;

        public SearchViewRoute(
            GlobalKey anchorKey,
            Func<bool>? toggleVisibility,
            SearchController searchController,
            SuggestionsBuilder suggestionsBuilder,
            ViewBuilder? viewBuilder,
            Widget? viewLeading,
            IReadOnlyList<Widget>? viewTrailing,
            string? viewHintText,
            Color? viewBackgroundColor,
            double? viewElevation,
            Color? viewSurfaceTintColor,
            BorderSide? viewSide,
            OutlinedBorder? viewShape,
            EdgeInsetsGeometry? viewBarPadding,
            double? viewHeaderHeight,
            TextStyle? viewHeaderTextStyle,
            TextStyle? viewHeaderHintStyle,
            Color? dividerColor,
            BoxConstraints? viewConstraints,
            EdgeInsetsGeometry? viewPadding,
            bool? shrinkWrap,
            TextCapitalization? textCapitalization,
            Action<string>? viewOnChanged,
            Action<string>? viewOnSubmitted,
            Action? viewOnOpen,
            Action? viewOnClose,
            TextInputAction? textInputAction,
            TextInputType? keyboardType,
            SmartDashesType? smartDashesType,
            SmartQuotesType? smartQuotesType,
            CapturedThemes capturedThemes,
            TextDirection? textDirection,
            bool showFullScreenView) : base()
        {
            _anchorKey = anchorKey;
            _toggleVisibility = toggleVisibility;
            _searchController = searchController;
            _suggestionsBuilder = suggestionsBuilder;
            _viewBuilder = viewBuilder;
            _viewLeading = viewLeading;
            _viewTrailing = viewTrailing;
            _viewHintText = viewHintText;
            _viewBackgroundColor = viewBackgroundColor;
            _viewElevation = viewElevation;
            _viewSurfaceTintColor = viewSurfaceTintColor;
            _viewSide = viewSide;
            _viewShape = viewShape;
            _viewBarPadding = viewBarPadding;
            _viewHeaderHeight = viewHeaderHeight;
            _viewHeaderTextStyle = viewHeaderTextStyle;
            _viewHeaderHintStyle = viewHeaderHintStyle;
            _dividerColor = dividerColor;
            _viewConstraints = viewConstraints;
            _viewPadding = viewPadding;
            _shrinkWrap = shrinkWrap;
            _textCapitalization = textCapitalization;
            _viewOnChanged = viewOnChanged;
            _viewOnSubmitted = viewOnSubmitted;
            _viewOnOpen = viewOnOpen;
            _viewOnClose = viewOnClose;
            _textInputAction = textInputAction;
            _keyboardType = keyboardType;
            _smartDashesType = smartDashesType;
            _smartQuotesType = smartQuotesType;
            _capturedThemes = capturedThemes;
            _textDirection = textDirection;
            _showFullScreenView = showFullScreenView;
        }

        public override Color? BarrierColor => Colors.Transparent;

        public override bool BarrierDismissible => true;

        public override string? BarrierLabel => "Dismiss";

        public override TimeSpan TransitionDuration => SearchViewChoreography.OpenViewDuration;

        public override void DidPush()
        {
            BuildContext anchorContext = _anchorKey.CurrentContext!.Value;
            UpdateViewConfig(anchorContext);
            UpdateTweens(anchorContext);
            _toggleVisibility?.Invoke();
            _viewOnOpen?.Invoke();
            base.DidPush();
        }

        public override bool DidPop(object? result)
        {
            BuildContext? anchorContext = _anchorKey.CurrentContext;
            if (anchorContext.HasValue)
            {
                UpdateTweens(anchorContext.Value);
            }

            _toggleVisibility?.Invoke();
            _viewOnClose?.Invoke();
            Scheduler.AddPostFrameCallback(_ =>
            {
                BuildContext? context = _anchorKey.CurrentContext;
                if (context.HasValue)
                {
                    FocusScope.MaybeOf(context.Value)?.Unfocus();
                }
            });
            return base.DidPop(result);
        }

        internal void Dismiss(bool disposeController)
        {
            _willDisposeSearchController = disposeController;
            if (IsActive)
            {
                Navigator?.RemoveRoute(this);
            }
        }

        public override void Dispose()
        {
            _curvedAnimation?.Dispose();
            _viewFadeOnIntervalCurve?.Dispose();
            if (_willDisposeSearchController)
            {
                _searchController.Dispose();
            }

            base.Dispose();
        }

        public override Widget BuildPage(BuildContext context)
        {
            return new Directionality(
                _textDirection ?? TextDirection.Ltr,
                new AnimatedBuilder(
                    animation: Animation,
                    builder: (builderContext, _) =>
                    {
                        _curvedAnimation ??= new CurvedAnimation(
                            Animation,
                            Curves.EaseInOutCubicEmphasized,
                            Curves.Flipped(Curves.EaseInOutCubicEmphasized));
                        Rect viewRect = _rectTween.Evaluate(_curvedAnimation.Value);
                        double topPadding = _showFullScreenView
                            ? LerpDouble(0.0, MediaQuery.PaddingOf(builderContext).Top, _curvedAnimation.Value)
                            : 0.0;
                        _viewFadeOnIntervalCurve ??= new CurvedAnimation(
                            Animation,
                            SearchViewChoreography.ViewFadeOnInterval,
                            Curves.Flipped(SearchViewChoreography.ViewFadeOnInterval));
                        return new FadeTransition(
                            opacity: _viewFadeOnIntervalCurve,
                            child: _capturedThemes.Wrap(new SearchViewContent(
                                animation: _curvedAnimation,
                                topPadding: topPadding,
                                viewMaxWidth: _rectTween.End!.Value.Width,
                                viewRect: viewRect,
                                searchController: _searchController,
                                suggestionsBuilder: _suggestionsBuilder,
                                viewBuilder: _viewBuilder,
                                viewLeading: _viewLeading,
                                viewTrailing: _viewTrailing,
                                viewHintText: _viewHintText,
                                viewBackgroundColor: _viewBackgroundColor,
                                viewElevation: _viewElevation,
                                viewSurfaceTintColor: _viewSurfaceTintColor,
                                viewSide: _viewSide,
                                viewShape: _viewShape,
                                viewBarPadding: _viewBarPadding,
                                viewHeaderHeight: _viewHeaderHeight,
                                viewHeaderTextStyle: _viewHeaderTextStyle,
                                viewHeaderHintStyle: _viewHeaderHintStyle,
                                dividerColor: _dividerColor,
                                viewConstraints: _viewConstraints,
                                viewPadding: _viewPadding,
                                shrinkWrap: _shrinkWrap,
                                textCapitalization: _textCapitalization,
                                viewOnChanged: _viewOnChanged,
                                viewOnSubmitted: _viewOnSubmitted,
                                textInputAction: _textInputAction,
                                keyboardType: _keyboardType,
                                smartDashesType: _smartDashesType,
                                smartQuotesType: _smartQuotesType,
                                showFullScreenView: _showFullScreenView)));
                    }));
        }

        internal Rect? GetRect()
        {
            BuildContext? context = _anchorKey.CurrentContext;
            if (!context.HasValue)
            {
                return null;
            }

            var searchBarBox = (RenderBox)context.Value.FindRenderObject()!;
            RenderObject? navigatorBox = Navigator!.Context.FindRenderObject();
            Point boxLocation = searchBarBox.LocalToGlobal(default, ancestor: navigatorBox);
            return new Rect(boxLocation, searchBarBox.Size);
        }

        private void UpdateViewConfig(BuildContext context)
        {
            _viewDefaults = SearchViewDefaultsM3.Resolve(Theme.Of(context), _showFullScreenView);
            _viewTheme = SearchViewTheme.Of(context);
        }

        private void UpdateTweens(BuildContext context)
        {
            var navigatorBox = (RenderBox)Navigator!.Context.FindRenderObject()!;
            Size screenSize = navigatorBox.Size;
            Rect anchorRect = GetRect() ?? default;
            BoxConstraints effectiveConstraints =
                _viewConstraints ?? _viewTheme!.Constraints ?? _viewDefaults!.Constraints!.Value;
            _rectTween.Begin = anchorRect;

            double viewWidth = SearchViewChoreography.ClampDouble(
                anchorRect.Width, effectiveConstraints.MinWidth, effectiveConstraints.MaxWidth);
            double viewHeight = SearchViewChoreography.ClampDouble(
                screenSize.Height * 2.0 / 3.0, effectiveConstraints.MinHeight, effectiveConstraints.MaxHeight);

            double dx;
            double dy = anchorRect.Top;
            switch (_textDirection ?? TextDirection.Ltr)
            {
                case TextDirection.Ltr:
                    dx = anchorRect.Left;
                    if (screenSize.Width - anchorRect.Left < viewWidth)
                    {
                        dx = screenSize.Width - Math.Min(viewWidth, screenSize.Width);
                    }

                    break;
                default:
                    dx = Math.Max(anchorRect.Right - viewWidth, 0.0);
                    if (anchorRect.Right < viewWidth)
                    {
                        dx = 0.0;
                    }

                    break;
            }

            if (screenSize.Height - anchorRect.Top < viewHeight)
            {
                dy = screenSize.Height - Math.Min(viewHeight, screenSize.Height);
            }

            _rectTween.End = _showFullScreenView
                ? new Rect(default(Point), screenSize)
                : new Rect(new Point(dx, dy), new Size(viewWidth, viewHeight));
        }

        private static double LerpDouble(double a, double b, double t)
        {
            return a + (b - a) * t;
        }
    }
}

internal sealed class SearchViewContent : StatefulWidget
{
    public SearchViewContent(
        Animation<double> animation,
        double topPadding,
        double viewMaxWidth,
        Rect viewRect,
        SearchController searchController,
        SuggestionsBuilder suggestionsBuilder,
        ViewBuilder? viewBuilder,
        Widget? viewLeading,
        IReadOnlyList<Widget>? viewTrailing,
        string? viewHintText,
        Color? viewBackgroundColor,
        double? viewElevation,
        Color? viewSurfaceTintColor,
        BorderSide? viewSide,
        OutlinedBorder? viewShape,
        EdgeInsetsGeometry? viewBarPadding,
        double? viewHeaderHeight,
        TextStyle? viewHeaderTextStyle,
        TextStyle? viewHeaderHintStyle,
        Color? dividerColor,
        BoxConstraints? viewConstraints,
        EdgeInsetsGeometry? viewPadding,
        bool? shrinkWrap,
        TextCapitalization? textCapitalization,
        Action<string>? viewOnChanged,
        Action<string>? viewOnSubmitted,
        TextInputAction? textInputAction,
        TextInputType? keyboardType,
        SmartDashesType? smartDashesType,
        SmartQuotesType? smartQuotesType,
        bool showFullScreenView)
    {
        Animation = animation;
        TopPadding = topPadding;
        ViewMaxWidth = viewMaxWidth;
        ViewRect = viewRect;
        SearchController = searchController;
        SuggestionsBuilder = suggestionsBuilder;
        ViewBuilder = viewBuilder;
        ViewLeading = viewLeading;
        ViewTrailing = viewTrailing;
        ViewHintText = viewHintText;
        ViewBackgroundColor = viewBackgroundColor;
        ViewElevation = viewElevation;
        ViewSurfaceTintColor = viewSurfaceTintColor;
        ViewSide = viewSide;
        ViewShape = viewShape;
        ViewBarPadding = viewBarPadding;
        ViewHeaderHeight = viewHeaderHeight;
        ViewHeaderTextStyle = viewHeaderTextStyle;
        ViewHeaderHintStyle = viewHeaderHintStyle;
        DividerColor = dividerColor;
        ViewConstraints = viewConstraints;
        ViewPadding = viewPadding;
        ShrinkWrap = shrinkWrap;
        TextCapitalization = textCapitalization;
        ViewOnChanged = viewOnChanged;
        ViewOnSubmitted = viewOnSubmitted;
        TextInputAction = textInputAction;
        KeyboardType = keyboardType;
        SmartDashesType = smartDashesType;
        SmartQuotesType = smartQuotesType;
        ShowFullScreenView = showFullScreenView;
    }

    public Animation<double> Animation { get; }
    public double TopPadding { get; }
    public double ViewMaxWidth { get; }
    public Rect ViewRect { get; }
    public SearchController SearchController { get; }
    public SuggestionsBuilder SuggestionsBuilder { get; }
    public ViewBuilder? ViewBuilder { get; }
    public Widget? ViewLeading { get; }
    public IReadOnlyList<Widget>? ViewTrailing { get; }
    public string? ViewHintText { get; }
    public Color? ViewBackgroundColor { get; }
    public double? ViewElevation { get; }
    public Color? ViewSurfaceTintColor { get; }
    public BorderSide? ViewSide { get; }
    public OutlinedBorder? ViewShape { get; }
    public EdgeInsetsGeometry? ViewBarPadding { get; }
    public double? ViewHeaderHeight { get; }
    public TextStyle? ViewHeaderTextStyle { get; }
    public TextStyle? ViewHeaderHintStyle { get; }
    public Color? DividerColor { get; }
    public BoxConstraints? ViewConstraints { get; }
    public EdgeInsetsGeometry? ViewPadding { get; }
    public bool? ShrinkWrap { get; }
    public TextCapitalization? TextCapitalization { get; }
    public Action<string>? ViewOnChanged { get; }
    public Action<string>? ViewOnSubmitted { get; }
    public TextInputAction? TextInputAction { get; }
    public TextInputType? KeyboardType { get; }
    public SmartDashesType? SmartDashesType { get; }
    public SmartQuotesType? SmartQuotesType { get; }
    public bool ShowFullScreenView { get; }

    public override State CreateState()
    {
        return new SearchViewContentState();
    }

    private sealed class SearchViewContentState : State
    {
        private Size? _screenSize;
        private Rect _viewRect;
        private string? _searchValue;
        private IReadOnlyList<Widget> _result = [];
        private CurvedAnimation? _viewIconsFade;
        private CurvedAnimation? _viewDividerFade;
        private CurvedAnimation? _viewListFade;
        private int _refreshGeneration;

        private SearchViewContent Current => (SearchViewContent)StateWidget;

        public override void InitState()
        {
            _viewRect = Current.ViewRect;
            Current.SearchController.AddListener(UpdateSuggestions);
            _viewIconsFade = new CurvedAnimation(
                Current.Animation,
                SearchViewChoreography.ViewIconsFadeOnInterval,
                Curves.Flipped(SearchViewChoreography.ViewIconsFadeOnInterval));
            // Upstream quirk: the divider reverses along the whole-view fade interval, not its own.
            _viewDividerFade = new CurvedAnimation(
                Current.Animation,
                SearchViewChoreography.ViewDividerFadeOnInterval,
                Curves.Flipped(SearchViewChoreography.ViewFadeOnInterval));
            _viewListFade = new CurvedAnimation(
                Current.Animation,
                SearchViewChoreography.ViewListFadeOnInterval,
                Curves.Flipped(SearchViewChoreography.ViewListFadeOnInterval));
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var old = (SearchViewContent)oldWidget;
            if (!ReferenceEquals(old.SearchController, Current.SearchController))
            {
                old.SearchController.RemoveListener(UpdateSuggestions);
                Current.SearchController.AddListener(UpdateSuggestions);
            }

            if (old.ViewRect != Current.ViewRect)
            {
                SetState(() => _viewRect = Current.ViewRect);
            }
        }

        public override void DidChangeDependencies()
        {
            Size updatedScreenSize = MediaQuery.Of(Context).Size;
            if (_screenSize != updatedScreenSize)
            {
                _screenSize = updatedScreenSize;
                if (Current.ShowFullScreenView)
                {
                    _viewRect = new Rect(default(Point), updatedScreenSize);
                }
            }

            if (_searchValue != Current.SearchController.Text)
            {
                ScheduleSuggestionsRefresh();
            }
        }

        public override void Dispose()
        {
            Current.SearchController.RemoveListener(UpdateSuggestions);
            _viewIconsFade?.Dispose();
            _viewDividerFade?.Dispose();
            _viewListFade?.Dispose();
            _refreshGeneration++;
        }

        public override Widget Build(BuildContext context)
        {
            var theme = Theme.Of(context);
            SearchViewThemeData viewTheme = SearchViewTheme.Of(context);
            SearchViewThemeData viewDefaults = SearchViewDefaultsM3.Resolve(theme, Current.ShowFullScreenView);

            Color effectiveBackgroundColor =
                Current.ViewBackgroundColor ?? viewTheme.BackgroundColor ?? viewDefaults.BackgroundColor!.Value;
            Color effectiveSurfaceTint =
                Current.ViewSurfaceTintColor ?? viewTheme.SurfaceTintColor ?? viewDefaults.SurfaceTintColor!.Value;
            double effectiveElevation = Current.ViewElevation ?? viewTheme.Elevation ?? viewDefaults.Elevation!.Value;
            BorderSide? effectiveSide = Current.ViewSide ?? viewTheme.Side ?? viewDefaults.Side;
            OutlinedBorder effectiveShape = Current.ViewShape ?? viewTheme.Shape ?? viewDefaults.Shape!;
            if (effectiveSide.HasValue)
            {
                effectiveShape = effectiveShape.CopyWith(effectiveSide);
            }

            Color effectiveDividerColor = Current.DividerColor
                                          ?? viewTheme.DividerColor
                                          ?? DividerTheme.Of(context).Color
                                          ?? viewDefaults.DividerColor!.Value;
            double? effectiveHeaderHeight = Current.ViewHeaderHeight ?? viewTheme.HeaderHeight;
            BoxConstraints? headerConstraints = effectiveHeaderHeight.HasValue
                ? BoxConstraints.TightFor(height: effectiveHeaderHeight)
                : null;
            TextStyle? effectiveTextStyle =
                Current.ViewHeaderTextStyle ?? viewTheme.HeaderTextStyle ?? viewDefaults.HeaderTextStyle;
            TextStyle? effectiveHintStyle = Current.ViewHeaderHintStyle
                                            ?? viewTheme.HeaderHintStyle
                                            ?? Current.ViewHeaderTextStyle
                                            ?? viewTheme.HeaderTextStyle
                                            ?? viewDefaults.HeaderHintStyle;
            EdgeInsetsGeometry? effectivePadding = Current.ViewPadding ?? viewTheme.Padding ?? viewDefaults.Padding;
            EdgeInsetsGeometry? effectiveBarPadding =
                Current.ViewBarPadding ?? viewTheme.BarPadding ?? viewDefaults.BarPadding;
            bool effectiveShrinkWrap = Current.ShrinkWrap ?? viewTheme.ShrinkWrap ?? viewDefaults.ShrinkWrap!.Value;
            BoxConstraints effectiveConstraints =
                Current.ViewConstraints ?? viewTheme.Constraints ?? viewDefaults.Constraints!.Value;
            double minHeight = Math.Min(effectiveConstraints.MinHeight, _viewRect.Height);

            Widget defaultLeading = new BackButton(
                style: new ButtonStyle(TapTargetSize: MaterialTapTargetSize.ShrinkWrap),
                onPressed: () => Navigator.Of(context).Pop());
            IReadOnlyList<Widget> defaultTrailing = string.IsNullOrEmpty(Current.SearchController.Text)
                ? []
                :
                [
                    new IconButton(
                        icon: new Icon(Icons.Close),
                        tooltip: MaterialLocalizations.Of(context).ClearButtonTooltip,
                        onPressed: Current.SearchController.Clear),
                ];
            Widget viewDivider = new DividerTheme(
                DividerTheme.Of(context).CopyWith(color: effectiveDividerColor),
                new Divider(height: 1));

            Widget headerBar = new SearchBar(
                controller: Current.SearchController,
                autoFocus: true,
                constraints: headerConstraints ?? (Current.ShowFullScreenView
                    ? new BoxConstraints(MinHeight: SearchViewDefaultsM3.FullScreenBarHeight)
                    : null),
                padding: MaterialStateProperty<EdgeInsetsGeometry?>.All(effectiveBarPadding),
                leading: Current.ViewLeading ?? defaultLeading,
                trailing: Current.ViewTrailing ?? defaultTrailing,
                hintText: Current.ViewHintText,
                backgroundColor: MaterialStateProperty<Color?>.All(Colors.Transparent),
                overlayColor: MaterialStateProperty<Color?>.All(Colors.Transparent),
                elevation: MaterialStateProperty<double?>.All(0.0),
                textStyle: MaterialStateProperty<TextStyle?>.All(effectiveTextStyle),
                hintStyle: MaterialStateProperty<TextStyle?>.All(effectiveHintStyle),
                onChanged: value =>
                {
                    Current.ViewOnChanged?.Invoke(value);
                    UpdateSuggestions();
                },
                onSubmitted: Current.ViewOnSubmitted,
                textCapitalization: Current.TextCapitalization,
                textInputAction: Current.TextInputAction,
                keyboardType: Current.KeyboardType,
                smartDashesType: Current.SmartDashesType,
                smartQuotesType: Current.SmartQuotesType);

            var columnChildren = new List<Widget>
            {
                new Padding(
                    EdgeInsetsGeometry.Only(top: Current.TopPadding),
                    new SafeArea(top: false, bottom: false, child: headerBar)),
            };
            if (!effectiveShrinkWrap || minHeight > 0 || Current.ShowFullScreenView || _result.Count > 0)
            {
                Widget viewList = Current.ViewBuilder is null
                    ? MediaQuery.RemovePadding(
                        context: context,
                        removeTop: true,
                        child: new ListView(
                            children: _result,
                            padding: new Thickness(0, 0, 0, MediaQuery.ViewInsetsOf(context).Bottom),
                            shrinkWrap: effectiveShrinkWrap))
                    : Current.ViewBuilder(_result);
                columnChildren.Add(new FadeTransition(opacity: _viewDividerFade!, child: viewDivider));
                columnChildren.Add(new Flexible(
                    fit: effectiveShrinkWrap && !Current.ShowFullScreenView ? FlexFit.Loose : FlexFit.Tight,
                    child: new FadeTransition(opacity: _viewListFade!, child: viewList)));
            }

            Widget viewSurface = new Material(
                clipBehavior: Clip.AntiAlias,
                shape: effectiveShape,
                color: effectiveBackgroundColor,
                surfaceTintColor: effectiveSurfaceTint,
                elevation: effectiveElevation,
                child: new OverflowBox(
                    alignment: Alignment.TopLeft,
                    maxWidth: Math.Min(Current.ViewMaxWidth, _screenSize!.Value.Width),
                    minWidth: 0,
                    fit: OverflowBoxFit.DeferToChild,
                    child: new FadeTransition(
                        opacity: _viewIconsFade!,
                        child: new Column(
                            mainAxisSize: MainAxisSize.Min,
                            crossAxisAlignment: CrossAxisAlignment.Stretch,
                            children: columnChildren))));

            return new Align(
                alignment: Alignment.TopLeft,
                child: new Widgets.Transform(
                    Matrix4.TranslationValues(_viewRect.X, _viewRect.Y, 0.0),
                    child: new ConstrainedBox(
                        new BoxConstraints(
                            MinWidth: Math.Min(effectiveConstraints.MinWidth, _viewRect.Width),
                            MaxWidth: _viewRect.Width,
                            MinHeight: minHeight,
                            MaxHeight: _viewRect.Height),
                        new Padding(
                            Current.ShowFullScreenView
                                ? EdgeInsetsGeometry.Zero
                                : effectivePadding ?? EdgeInsetsGeometry.Zero,
                            viewSurface))));
        }

        private async void UpdateSuggestions()
        {
            if (_searchValue == Current.SearchController.Text)
            {
                return;
            }

            _searchValue = Current.SearchController.Text;
            BuildContext context = Context;
            IReadOnlyList<Widget> suggestions = await Current.SuggestionsBuilder(context, Current.SearchController);
            if (Mounted)
            {
                SetState(() => _result = suggestions);
            }
        }

        private void ScheduleSuggestionsRefresh()
        {
            // Dart coalesces bursts of dependency changes through Timer(Duration.zero, ...); the post-frame
            // callback plays that role here, with the generation counter as the cancellation token.
            int generation = ++_refreshGeneration;
            Scheduler.AddPostFrameCallback(_ =>
            {
                if (generation != _refreshGeneration || !Mounted)
                {
                    return;
                }

                RunScheduledRefresh();
            });
        }

        private async void RunScheduledRefresh()
        {
            _searchValue = Current.SearchController.Text;
            BuildContext context = Context;
            IReadOnlyList<Widget> suggestions = await Current.SuggestionsBuilder(context, Current.SearchController);
            _refreshGeneration++;
            if (Mounted)
            {
                SetState(() => _result = suggestions);
            }
        }
    }
}

public sealed class SearchBar : StatefulWidget
{
    public SearchBar(
        TextEditingController? controller = null,
        FocusNode? focusNode = null,
        string? hintText = null,
        Widget? leading = null,
        IReadOnlyList<Widget>? trailing = null,
        Action? onTap = null,
        Action<PointerDownEvent>? onTapOutside = null,
        Action<string>? onChanged = null,
        Action<string>? onSubmitted = null,
        BoxConstraints? constraints = null,
        MaterialStateProperty<double?>? elevation = null,
        MaterialStateProperty<Color?>? backgroundColor = null,
        MaterialStateProperty<Color?>? shadowColor = null,
        MaterialStateProperty<Color?>? surfaceTintColor = null,
        MaterialStateProperty<Color?>? overlayColor = null,
        MaterialStateProperty<BorderSide?>? side = null,
        MaterialStateProperty<OutlinedBorder?>? shape = null,
        MaterialStateProperty<EdgeInsetsGeometry?>? padding = null,
        MaterialStateProperty<TextStyle?>? textStyle = null,
        MaterialStateProperty<TextStyle?>? hintStyle = null,
        TextCapitalization? textCapitalization = null,
        bool enabled = true,
        bool autoFocus = false,
        TextInputAction? textInputAction = null,
        TextInputType? keyboardType = null,
        Thickness? scrollPadding = null,
        EditableTextContextMenuBuilder? contextMenuBuilder = null,
        bool readOnly = false,
        SmartDashesType? smartDashesType = null,
        SmartQuotesType? smartQuotesType = null,
        Key? key = null) : base(key)
    {
        Controller = controller;
        FocusNode = focusNode;
        HintText = hintText;
        Leading = leading;
        Trailing = trailing;
        OnTap = onTap;
        OnTapOutside = onTapOutside;
        OnChanged = onChanged;
        OnSubmitted = onSubmitted;
        Constraints = constraints;
        Elevation = elevation;
        BackgroundColor = backgroundColor;
        ShadowColor = shadowColor;
        SurfaceTintColor = surfaceTintColor;
        OverlayColor = overlayColor;
        Side = side;
        Shape = shape;
        Padding = padding;
        TextStyle = textStyle;
        HintStyle = hintStyle;
        TextCapitalization = textCapitalization;
        Enabled = enabled;
        AutoFocus = autoFocus;
        TextInputAction = textInputAction;
        KeyboardType = keyboardType;
        ScrollPadding = scrollPadding ?? new Thickness(20);
        ContextMenuBuilder = contextMenuBuilder ?? TextField.DefaultContextMenuBuilder;
        ReadOnly = readOnly;
        SmartDashesType = smartDashesType;
        SmartQuotesType = smartQuotesType;
    }

    public TextEditingController? Controller { get; }
    public FocusNode? FocusNode { get; }
    public string? HintText { get; }
    public Widget? Leading { get; }
    public IReadOnlyList<Widget>? Trailing { get; }
    public Action? OnTap { get; }
    public Action<PointerDownEvent>? OnTapOutside { get; }
    public Action<string>? OnChanged { get; }
    public Action<string>? OnSubmitted { get; }
    public BoxConstraints? Constraints { get; }
    public MaterialStateProperty<double?>? Elevation { get; }
    public MaterialStateProperty<Color?>? BackgroundColor { get; }
    public MaterialStateProperty<Color?>? ShadowColor { get; }
    public MaterialStateProperty<Color?>? SurfaceTintColor { get; }
    public MaterialStateProperty<Color?>? OverlayColor { get; }
    public MaterialStateProperty<BorderSide?>? Side { get; }
    public MaterialStateProperty<OutlinedBorder?>? Shape { get; }
    public MaterialStateProperty<EdgeInsetsGeometry?>? Padding { get; }
    public MaterialStateProperty<TextStyle?>? TextStyle { get; }
    public MaterialStateProperty<TextStyle?>? HintStyle { get; }
    public TextCapitalization? TextCapitalization { get; }
    public bool Enabled { get; }
    public bool AutoFocus { get; }
    public TextInputAction? TextInputAction { get; }
    public TextInputType? KeyboardType { get; }
    public Thickness ScrollPadding { get; }
    public EditableTextContextMenuBuilder ContextMenuBuilder { get; }
    public bool ReadOnly { get; }
    public SmartDashesType? SmartDashesType { get; }
    public SmartQuotesType? SmartQuotesType { get; }

    public override State CreateState()
    {
        return new SearchBarState();
    }

    private sealed class SearchBarState : State
    {
        private readonly MaterialStatesController _internalStatesController = new();
        private FocusNode? _internalFocusNode;
        private FocusNode? _attachedFocusNode;

        private SearchBar CurrentWidget => (SearchBar)StateWidget;

        private FocusNode FocusNodeInstance => CurrentWidget.FocusNode ?? (_internalFocusNode ??= new FocusNode());

        public override void InitState()
        {
            _internalStatesController.AddListener(HandleStatesChanged);
            AttachFocusListener();
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var old = (SearchBar)oldWidget;
            if (!ReferenceEquals(old.FocusNode, CurrentWidget.FocusNode))
            {
                AttachFocusListener();
            }
        }

        public override void Dispose()
        {
            _attachedFocusNode?.RemoveListener(HandleFocusChanged);
            _attachedFocusNode = null;
            _internalFocusNode?.Dispose();
            _internalStatesController.RemoveListener(HandleStatesChanged);
            _internalStatesController.Dispose();
        }

        public override Widget Build(BuildContext context)
        {
            var theme = Theme.Of(context);
            SearchBarThemeData searchBarTheme = SearchBarTheme.Of(context);
            SearchBarThemeData defaults = SearchBarDefaultsM3.Resolve(theme);
            MaterialState states = _internalStatesController.Value;

            TextStyle? effectiveTextStyle = Resolve(
                CurrentWidget.TextStyle, searchBarTheme.TextStyle, defaults.TextStyle, states);
            TextStyle? effectiveHintStyle = ResolveProperty(CurrentWidget.HintStyle, states)
                                            ?? ResolveProperty(searchBarTheme.HintStyle, states)
                                            ?? ResolveProperty(CurrentWidget.TextStyle, states)
                                            ?? ResolveProperty(searchBarTheme.TextStyle, states)
                                            ?? ResolveProperty(defaults.HintStyle, states);
            double effectiveElevation = Resolve(
                CurrentWidget.Elevation, searchBarTheme.Elevation, defaults.Elevation, states)!.Value;
            Color effectiveBackgroundColor = Resolve(
                CurrentWidget.BackgroundColor, searchBarTheme.BackgroundColor, defaults.BackgroundColor, states)!.Value;
            Color effectiveShadowColor = Resolve(
                CurrentWidget.ShadowColor, searchBarTheme.ShadowColor, defaults.ShadowColor, states)!.Value;
            Color effectiveSurfaceTintColor = Resolve(
                CurrentWidget.SurfaceTintColor,
                searchBarTheme.SurfaceTintColor,
                defaults.SurfaceTintColor,
                states)!.Value;
            MaterialStateProperty<Color?>? effectiveOverlayColor =
                CurrentWidget.OverlayColor ?? searchBarTheme.OverlayColor ?? defaults.OverlayColor;
            BorderSide? effectiveSide = Resolve(CurrentWidget.Side, searchBarTheme.Side, defaults.Side, states);
            OutlinedBorder? effectiveShape = Resolve(
                CurrentWidget.Shape, searchBarTheme.Shape, defaults.Shape, states);
            if (effectiveSide.HasValue)
            {
                effectiveShape = effectiveShape?.CopyWith(effectiveSide);
            }

            EdgeInsetsGeometry? effectivePadding = Resolve(
                CurrentWidget.Padding, searchBarTheme.Padding, defaults.Padding, states);
            TextCapitalization effectiveTextCapitalization = CurrentWidget.TextCapitalization
                                                             ?? searchBarTheme.TextCapitalization
                                                             ?? defaults.TextCapitalization!.Value;
            BoxConstraints effectiveConstraints =
                CurrentWidget.Constraints ?? searchBarTheme.Constraints ?? defaults.Constraints!.Value;

            bool isDark = theme.Brightness == Brightness.Dark;
            Color defaultIconColor = isDark ? TabStyle.DefaultIconLightColor : TabStyle.DefaultIconDarkColor;
            IconThemeData ambientIconTheme = IconTheme.Of(context);
            IconThemeData? customIconTheme = ambientIconTheme.Color != defaultIconColor ? ambientIconTheme : null;

            var children = new List<Widget>();
            if (CurrentWidget.Leading is not null)
            {
                children.Add(IconTheme.Merge(
                    data: customIconTheme ?? new IconThemeData(Color: theme.OnSurfaceColor),
                    child: CurrentWidget.Leading));
            }

            Widget textField = new TextField(
                controller: CurrentWidget.Controller,
                focusNode: FocusNodeInstance,
                readOnly: CurrentWidget.ReadOnly,
                autofocus: CurrentWidget.AutoFocus,
                onTap: CurrentWidget.OnTap,
                onTapAlwaysCalled: true,
                onTapOutside: CurrentWidget.OnTapOutside,
                onChanged: CurrentWidget.OnChanged,
                onSubmitted: CurrentWidget.OnSubmitted,
                style: effectiveTextStyle,
                enabled: CurrentWidget.Enabled,
                decoration: new InputDecoration(hintText: CurrentWidget.HintText).ApplyDefaults(
                    new InputDecorationThemeData(
                        hintStyle: effectiveHintStyle,
                        enabledBorder: InputBorder.None,
                        border: InputBorder.None,
                        focusedBorder: InputBorder.None,
                        contentPadding: EdgeInsetsGeometry.Zero,
                        isDense: true)),
                textCapitalization: effectiveTextCapitalization,
                textInputAction: CurrentWidget.TextInputAction,
                keyboardType: CurrentWidget.KeyboardType,
                scrollPadding: CurrentWidget.ScrollPadding,
                contextMenuBuilder: CurrentWidget.ContextMenuBuilder,
                smartDashesType: CurrentWidget.SmartDashesType,
                smartQuotesType: CurrentWidget.SmartQuotesType);
            children.Add(new Expanded(
                new Padding(
                    effectivePadding!.Value,
                    new Semantics(inputType: SemanticsInputType.Search, child: textField))));

            if (CurrentWidget.Trailing is not null)
            {
                foreach (Widget trailing in CurrentWidget.Trailing)
                {
                    children.Add(IconTheme.Merge(
                        data: customIconTheme ?? new IconThemeData(Color: theme.OnSurfaceVariantColor),
                        child: trailing));
                }
            }

            Widget content = new Row(children: children);
            content = new Padding(effectivePadding.Value, content);
            content = new InkWell(
                onTap: () =>
                {
                    CurrentWidget.OnTap?.Invoke();
                    if (!FocusNodeInstance.HasFocus)
                    {
                        FocusNodeInstance.RequestFocus();
                    }
                },
                overlayColor: effectiveOverlayColor,
                customBorder: effectiveShape,
                statesController: _internalStatesController,
                child: content);
            content = new IgnorePointer(ignoring: !CurrentWidget.Enabled, child: content);
            content = new Material(
                elevation: effectiveElevation,
                shadowColor: effectiveShadowColor,
                color: effectiveBackgroundColor,
                surfaceTintColor: effectiveSurfaceTintColor,
                shape: effectiveShape,
                child: content);
            content = new Opacity(
                CurrentWidget.Enabled ? 1.0 : SearchViewChoreography.DisableSearchBarOpacity,
                content);
            return new ConstrainedBox(effectiveConstraints, content);
        }

        private static T? Resolve<T>(
            MaterialStateProperty<T?>? widgetValue,
            MaterialStateProperty<T?>? themeValue,
            MaterialStateProperty<T?>? defaultValue,
            MaterialState states)
        {
            return ResolveProperty(widgetValue, states)
                   ?? ResolveProperty(themeValue, states)
                   ?? ResolveProperty(defaultValue, states);
        }

        private static T? ResolveProperty<T>(MaterialStateProperty<T?>? property, MaterialState states)
        {
            return property is null ? default : property.Resolve(states);
        }

        private void AttachFocusListener()
        {
            _attachedFocusNode?.RemoveListener(HandleFocusChanged);
            _attachedFocusNode = FocusNodeInstance;
            _attachedFocusNode.AddListener(HandleFocusChanged);
            _internalStatesController.Update(MaterialState.Focused, _attachedFocusNode.HasFocus);
        }

        private void HandleFocusChanged()
        {
            _internalStatesController.Update(MaterialState.Focused, _attachedFocusNode?.HasFocus == true);
        }

        private void HandleStatesChanged()
        {
            if (Mounted)
            {
                SetState(() => { });
            }
        }
    }
}

internal static class SearchBarDefaultsM3
{
    public static SearchBarThemeData Resolve(ThemeData theme)
    {
        return new SearchBarThemeData(
            Elevation: MaterialStateProperty<double?>.All(6.0),
            BackgroundColor: MaterialStateProperty<Color?>.All(theme.SurfaceContainerHighColor),
            ShadowColor: MaterialStateProperty<Color?>.All(theme.ShadowColor),
            SurfaceTintColor: MaterialStateProperty<Color?>.All(Colors.Transparent),
            OverlayColor: MaterialStateProperty<Color?>.ResolveWith(states =>
            {
                if (states.HasFlag(MaterialState.Pressed))
                {
                    return theme.OnSurfaceColor.WithOpacity(0.1);
                }

                if (states.HasFlag(MaterialState.Hovered))
                {
                    return theme.OnSurfaceColor.WithOpacity(0.08);
                }

                if (states.HasFlag(MaterialState.Focused))
                {
                    return Colors.Transparent;
                }

                return Colors.Transparent;
            }),
            Shape: MaterialStateProperty<OutlinedBorder?>.All(new StadiumBorder()),
            Padding: MaterialStateProperty<EdgeInsetsGeometry?>.All(EdgeInsetsGeometry.Symmetric(horizontal: 8.0)),
            TextStyle: MaterialStateProperty<TextStyle?>.All(
                theme.TextTheme.BodyLarge.CopyWith(color: theme.OnSurfaceColor)),
            HintStyle: MaterialStateProperty<TextStyle?>.All(
                theme.TextTheme.BodyLarge.CopyWith(color: theme.OnSurfaceVariantColor)),
            Constraints: new BoxConstraints(MinWidth: 360.0, MaxWidth: 800.0, MinHeight: 56.0),
            TextCapitalization: TextCapitalization.None);
    }
}

internal static class SearchViewDefaultsM3
{
    public const double FullScreenBarHeight = 72.0;

    public static SearchViewThemeData Resolve(ThemeData theme, bool isFullScreen)
    {
        return new SearchViewThemeData(
            BackgroundColor: theme.SurfaceContainerHighColor,
            Elevation: 6.0,
            SurfaceTintColor: Colors.Transparent,
            Shape: isFullScreen
                ? new RoundedRectangleBorder()
                : new RoundedRectangleBorder(borderRadius: BorderRadius.Circular(28.0)),
            HeaderTextStyle: theme.TextTheme.BodyLarge.CopyWith(color: theme.OnSurfaceColor),
            HeaderHintStyle: theme.TextTheme.BodyLarge.CopyWith(color: theme.OnSurfaceVariantColor),
            Constraints: new BoxConstraints(MinWidth: 360.0, MinHeight: 240.0),
            BarPadding: EdgeInsetsGeometry.Symmetric(horizontal: 8.0),
            ShrinkWrap: false,
            DividerColor: theme.OutlineColor);
    }
}
