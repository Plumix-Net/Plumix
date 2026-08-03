using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/search_anchor.dart

public enum TextCapitalization
{
    None,
    Characters,
    Words,
    Sentences,
}

public enum TextInputAction
{
    None,
    Search,
    Done,
    Go,
    Next,
    Send,
}

public enum TextInputType
{
    Text,
    Multiline,
    Number,
    Phone,
    Datetime,
    EmailAddress,
    Url,
}

public enum SmartDashesType
{
    Disabled,
    Enabled,
}

public enum SmartQuotesType
{
    Disabled,
    Enabled,
}

public delegate Widget SearchAnchorChildBuilder(BuildContext context, SearchController controller);

public delegate IReadOnlyList<Widget> SuggestionsBuilder(BuildContext context, SearchController controller);

public delegate Widget SearchViewBuilder(IReadOnlyList<Widget> suggestions);

public sealed class SearchController : TextEditingController
{
    private SearchAnchor.SearchAnchorState? _anchor;

    public SearchController(string text = "") : base(text)
    {
    }

    public bool IsAttached => _anchor is not null;

    public bool IsOpen => _anchor?.ViewIsOpen ?? false;

    public void OpenView()
    {
        if (_anchor is null)
        {
            throw new InvalidOperationException("SearchController is not attached to a SearchAnchor.");
        }

        _anchor.OpenView();
    }

    public void CloseView(string? selectedText = null)
    {
        if (_anchor is null)
        {
            throw new InvalidOperationException("SearchController is not attached to a SearchAnchor.");
        }

        _anchor.CloseView(selectedText);
    }

    internal void Attach(SearchAnchor.SearchAnchorState anchor)
    {
        if (_anchor is not null && !ReferenceEquals(_anchor, anchor))
        {
            throw new InvalidOperationException("A SearchController cannot be attached to more than one SearchAnchor.");
        }

        _anchor = anchor;
    }

    internal void Detach(SearchAnchor.SearchAnchorState anchor)
    {
        if (ReferenceEquals(_anchor, anchor))
        {
            _anchor = null;
        }
    }
}

public sealed class SearchAnchor : StatefulWidget
{
    private const double DisabledOpacity = 0.38;
    private static readonly TimeSpan AnchorFadeDuration = TimeSpan.FromMilliseconds(150);

    public SearchAnchor(
        SearchAnchorChildBuilder builder,
        SuggestionsBuilder suggestionsBuilder,
        bool? isFullScreen = null,
        SearchController? searchController = null,
        SearchViewBuilder? viewBuilder = null,
        Widget? viewLeading = null,
        IReadOnlyList<Widget>? viewTrailing = null,
        string? viewHintText = null,
        Color? viewBackgroundColor = null,
        double? viewElevation = null,
        Color? viewSurfaceTintColor = null,
        BorderSide? viewSide = null,
        ShapeBorder? viewShape = null,
        Thickness? viewBarPadding = null,
        double? headerHeight = null,
        TextStyle? headerTextStyle = null,
        TextStyle? headerHintStyle = null,
        Color? dividerColor = null,
        BoxConstraints? viewConstraints = null,
        Thickness? viewPadding = null,
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
        if (viewElevation.HasValue && (!double.IsFinite(viewElevation.Value) || viewElevation.Value < 0))
        {
            throw new ArgumentOutOfRangeException(nameof(viewElevation), "Search view elevation must be non-negative and finite.");
        }

        if (headerHeight.HasValue && (!double.IsFinite(headerHeight.Value) || headerHeight.Value <= 0))
        {
            throw new ArgumentOutOfRangeException(nameof(headerHeight), "Search view header height must be positive and finite.");
        }

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
    public SearchViewBuilder? ViewBuilder { get; }
    public Widget? ViewLeading { get; }
    public IReadOnlyList<Widget>? ViewTrailing { get; }
    public string? ViewHintText { get; }
    public Color? ViewBackgroundColor { get; }
    public double? ViewElevation { get; }
    public Color? ViewSurfaceTintColor { get; }
    public BorderSide? ViewSide { get; }
    public ShapeBorder? ViewShape { get; }
    public Thickness? ViewBarPadding { get; }
    public double? HeaderHeight { get; }
    public TextStyle? HeaderTextStyle { get; }
    public TextStyle? HeaderHintStyle { get; }
    public Color? DividerColor { get; }
    public BoxConstraints? ViewConstraints { get; }
    public Thickness? ViewPadding { get; }
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
        MaterialStateProperty<ShapeBorder?>? barShape = null,
        MaterialStateProperty<Thickness?>? barPadding = null,
        Thickness? viewBarPadding = null,
        MaterialStateProperty<TextStyle?>? barTextStyle = null,
        MaterialStateProperty<TextStyle?>? barHintStyle = null,
        SearchViewBuilder? viewBuilder = null,
        Widget? viewLeading = null,
        IReadOnlyList<Widget>? viewTrailing = null,
        string? viewHintText = null,
        Color? viewBackgroundColor = null,
        double? viewElevation = null,
        BorderSide? viewSide = null,
        ShapeBorder? viewShape = null,
        double? viewHeaderHeight = null,
        TextStyle? viewHeaderTextStyle = null,
        TextStyle? viewHeaderHintStyle = null,
        Color? dividerColor = null,
        BoxConstraints? constraints = null,
        BoxConstraints? viewConstraints = null,
        Thickness? viewPadding = null,
        bool? shrinkWrap = null,
        bool? isFullScreen = null,
        SearchController? searchController = null,
        TextCapitalization? textCapitalization = null,
        TextInputAction? textInputAction = null,
        TextInputType? keyboardType = null,
        bool enabled = true,
        SmartDashesType? smartDashesType = null,
        SmartQuotesType? smartQuotesType = null,
        Key? key = null)
    {
        return new SearchAnchor(
            builder: (context, controller) => new SearchBar(
                controller: controller,
                constraints: constraints,
                onTap: () =>
                {
                    controller.OpenView();
                    onTap?.Invoke();
                },
                onChanged: _ =>
                {
                    if (!controller.IsOpen)
                    {
                        controller.OpenView();
                    }
                },
                onSubmitted: onSubmitted,
                hintText: barHintText,
                hintStyle: barHintStyle,
                textStyle: barTextStyle,
                elevation: barElevation,
                backgroundColor: barBackgroundColor,
                overlayColor: barOverlayColor,
                side: barSide,
                shape: barShape,
                padding: barPadding ?? MaterialStateProperty<Thickness?>.All(new Thickness(16, 0)),
                leading: barLeading ?? new Icon(Icons.Search),
                trailing: barTrailing,
                textCapitalization: textCapitalization,
                enabled: enabled,
                textInputAction: textInputAction,
                keyboardType: keyboardType,
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
        private SearchController? _controller;
        private bool _ownsController;
        private bool _viewIsOpen;
        private SearchViewRoute? _route;

        private SearchAnchor CurrentWidget => (SearchAnchor)StateWidget;

        internal bool ViewIsOpen => _viewIsOpen;

        public override void InitState()
        {
            AttachController(CurrentWidget.SearchController);
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var old = (SearchAnchor)oldWidget;
            if (!ReferenceEquals(old.SearchController, CurrentWidget.SearchController))
            {
                DetachController();
                AttachController(CurrentWidget.SearchController);
            }
        }

        public override Widget Build(BuildContext context)
        {
            double opacity = CurrentWidget.Enabled
                ? _viewIsOpen ? 0.0 : 1.0
                : DisabledOpacity;
            Widget child = CurrentWidget.Builder(context, _controller!);
            child = new Opacity(opacity, child);
            if (CurrentWidget.Enabled)
            {
                child = new GestureDetector(
                    onTap: OpenView,
                    behavior: HitTestBehavior.Translucent,
                    child: child);
            }

            return child;
        }

        public override void Dispose()
        {
            if (_route is not null)
            {
                _route.SuppressCloseCallback();
                _route.Navigator?.MaybePop();
                _route = null;
            }

            DetachController();
        }

        internal void OpenView()
        {
            if (_viewIsOpen || !CurrentWidget.Enabled)
            {
                return;
            }

            var navigator = Navigator.MaybeOf(Context);
            if (navigator is null)
            {
                return;
            }

            bool showFullScreen = ResolveFullScreen(Theme.Of(Context), CurrentWidget.IsFullScreen);
            _route = new SearchViewRoute(
                anchor: this,
                searchController: _controller!,
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
                headerHeight: CurrentWidget.HeaderHeight,
                headerTextStyle: CurrentWidget.HeaderTextStyle,
                headerHintStyle: CurrentWidget.HeaderHintStyle,
                dividerColor: CurrentWidget.DividerColor,
                viewConstraints: CurrentWidget.ViewConstraints,
                viewPadding: CurrentWidget.ViewPadding,
                shrinkWrap: CurrentWidget.ShrinkWrap,
                textCapitalization: CurrentWidget.TextCapitalization,
                viewOnChanged: CurrentWidget.ViewOnChanged,
                viewOnSubmitted: CurrentWidget.ViewOnSubmitted,
                textInputAction: CurrentWidget.TextInputAction,
                keyboardType: CurrentWidget.KeyboardType,
                smartDashesType: CurrentWidget.SmartDashesType,
                smartQuotesType: CurrentWidget.SmartQuotesType,
                showFullScreenView: showFullScreen,
                theme: Theme.Of(Context),
                searchBarTheme: SearchBarTheme.Of(Context),
                searchViewTheme: SearchViewTheme.Of(Context),
                dividerTheme: DividerTheme.Of(Context),
                mediaQuery: MediaQuery.Of(Context),
                textDirection: Directionality.Of(Context),
                localizations: MaterialLocalizations.Of(Context));
            _viewIsOpen = true;
            CurrentWidget.ViewOnOpen?.Invoke();
            SetState(() => { });
            navigator.Push(_route);
        }

        internal void CloseView(string? selectedText)
        {
            if (!_viewIsOpen)
            {
                return;
            }

            if (selectedText is not null)
            {
                _controller!.Text = selectedText;
            }

            _route?.Navigator?.MaybePop();
        }

        internal void NotifyRouteClosed(SearchViewRoute route)
        {
            if (!ReferenceEquals(_route, route))
            {
                return;
            }

            _route = null;
            _viewIsOpen = false;
            CurrentWidget.ViewOnClose?.Invoke();
            if (Mounted)
            {
                SetState(() => { });
            }
        }

        private void AttachController(SearchController? external)
        {
            _controller = external ?? new SearchController();
            _ownsController = external is null;
            _controller.Attach(this);
        }

        private void DetachController()
        {
            if (_controller is null)
            {
                return;
            }

            _controller.Detach(this);
            if (_ownsController)
            {
                _controller.Dispose();
            }

            _controller = null;
            _ownsController = false;
        }

        private static bool ResolveFullScreen(ThemeData theme, bool? overrideValue)
        {
            if (overrideValue.HasValue)
            {
                return overrideValue.Value;
            }

            return theme.Platform is TargetPlatform.Android or TargetPlatform.Fuchsia or TargetPlatform.IOS;
        }
    }

    internal sealed class SearchViewRoute : PageRoute
    {
        private readonly SearchAnchorState _anchor;
        private readonly SearchController _searchController;
        private readonly SuggestionsBuilder _suggestionsBuilder;
        private readonly SearchViewBuilder? _viewBuilder;
        private readonly Widget? _viewLeading;
        private readonly IReadOnlyList<Widget>? _viewTrailing;
        private readonly string? _viewHintText;
        private readonly Color? _viewBackgroundColor;
        private readonly double? _viewElevation;
        private readonly Color? _viewSurfaceTintColor;
        private readonly BorderSide? _viewSide;
        private readonly ShapeBorder? _viewShape;
        private readonly Thickness? _viewBarPadding;
        private readonly double? _headerHeight;
        private readonly TextStyle? _headerTextStyle;
        private readonly TextStyle? _headerHintStyle;
        private readonly Color? _dividerColor;
        private readonly BoxConstraints? _viewConstraints;
        private readonly Thickness? _viewPadding;
        private readonly bool? _shrinkWrap;
        private readonly TextCapitalization? _textCapitalization;
        private readonly Action<string>? _viewOnChanged;
        private readonly Action<string>? _viewOnSubmitted;
        private readonly TextInputAction? _textInputAction;
        private readonly TextInputType? _keyboardType;
        private readonly SmartDashesType? _smartDashesType;
        private readonly SmartQuotesType? _smartQuotesType;
        private readonly bool _showFullScreenView;
        private readonly ThemeData _theme;
        private readonly SearchBarThemeData _searchBarTheme;
        private readonly SearchViewThemeData _searchViewTheme;
        private readonly DividerThemeData _dividerTheme;
        private readonly MediaQueryData _mediaQuery;
        private readonly TextDirection _textDirection;
        private readonly MaterialLocalizations _localizations;
        private bool _suppressCloseCallback;

        public SearchViewRoute(
            SearchAnchorState anchor,
            SearchController searchController,
            SuggestionsBuilder suggestionsBuilder,
            SearchViewBuilder? viewBuilder,
            Widget? viewLeading,
            IReadOnlyList<Widget>? viewTrailing,
            string? viewHintText,
            Color? viewBackgroundColor,
            double? viewElevation,
            Color? viewSurfaceTintColor,
            BorderSide? viewSide,
            ShapeBorder? viewShape,
            Thickness? viewBarPadding,
            double? headerHeight,
            TextStyle? headerTextStyle,
            TextStyle? headerHintStyle,
            Color? dividerColor,
            BoxConstraints? viewConstraints,
            Thickness? viewPadding,
            bool? shrinkWrap,
            TextCapitalization? textCapitalization,
            Action<string>? viewOnChanged,
            Action<string>? viewOnSubmitted,
            TextInputAction? textInputAction,
            TextInputType? keyboardType,
            SmartDashesType? smartDashesType,
            SmartQuotesType? smartQuotesType,
            bool showFullScreenView,
            ThemeData theme,
            SearchBarThemeData searchBarTheme,
            SearchViewThemeData searchViewTheme,
            DividerThemeData dividerTheme,
            MediaQueryData mediaQuery,
            TextDirection textDirection,
            MaterialLocalizations localizations) : base()
        {
            _anchor = anchor;
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
            _headerHeight = headerHeight;
            _headerTextStyle = headerTextStyle;
            _headerHintStyle = headerHintStyle;
            _dividerColor = dividerColor;
            _viewConstraints = viewConstraints;
            _viewPadding = viewPadding;
            _shrinkWrap = shrinkWrap;
            _textCapitalization = textCapitalization;
            _viewOnChanged = viewOnChanged;
            _viewOnSubmitted = viewOnSubmitted;
            _textInputAction = textInputAction;
            _keyboardType = keyboardType;
            _smartDashesType = smartDashesType;
            _smartQuotesType = smartQuotesType;
            _showFullScreenView = showFullScreenView;
            _theme = theme;
            _searchBarTheme = searchBarTheme;
            _searchViewTheme = searchViewTheme;
            _dividerTheme = dividerTheme;
            _mediaQuery = mediaQuery;
            _textDirection = textDirection;
            _localizations = localizations;
        }

        public override bool Opaque => false;

        internal void SuppressCloseCallback()
        {
            _suppressCloseCallback = true;
        }

        public override void DidComplete(object? result)
        {
            if (!_suppressCloseCallback)
            {
                _anchor.NotifyRouteClosed(this);
            }
        }

        public override Widget BuildPage(BuildContext context)
        {
            Widget barrier = new Semantics(
                label: _localizations.ModalBarrierDismissLabel,
                onTap: () => Navigator?.MaybePop(),
                child: new GestureDetector(
                    behavior: HitTestBehavior.Opaque,
                    onTap: () => Navigator?.MaybePop(),
                    child: new SizedBox()));

            Widget content = new SearchViewContent(
                route: this,
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
                headerHeight: _headerHeight,
                headerTextStyle: _headerTextStyle,
                headerHintStyle: _headerHintStyle,
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
                showFullScreenView: _showFullScreenView,
                theme: _theme,
                searchViewTheme: _searchViewTheme,
                dividerTheme: _dividerTheme,
                mediaQuery: _mediaQuery,
                localizations: _localizations);

            Widget page = new Stack(
                fit: StackFit.Expand,
                children:
                [
                    new Positioned(left: 0, top: 0, right: 0, bottom: 0, child: barrier),
                    new Positioned(left: 0, top: 0, right: 0, bottom: 0, child: content),
                ]);
            page = new SearchBarTheme(_searchBarTheme, page);
            page = new SearchViewTheme(_searchViewTheme, page);
            page = new Theme(_theme, page);
            page = new MediaQuery(_mediaQuery, page);
            page = new Directionality(_textDirection, page);
            return page;
        }
    }
}

internal sealed class SearchViewContent : StatefulWidget
{
    public SearchViewContent(
        SearchAnchor.SearchViewRoute route,
        SearchController searchController,
        SuggestionsBuilder suggestionsBuilder,
        SearchViewBuilder? viewBuilder,
        Widget? viewLeading,
        IReadOnlyList<Widget>? viewTrailing,
        string? viewHintText,
        Color? viewBackgroundColor,
        double? viewElevation,
        Color? viewSurfaceTintColor,
        BorderSide? viewSide,
        ShapeBorder? viewShape,
        Thickness? viewBarPadding,
        double? headerHeight,
        TextStyle? headerTextStyle,
        TextStyle? headerHintStyle,
        Color? dividerColor,
        BoxConstraints? viewConstraints,
        Thickness? viewPadding,
        bool? shrinkWrap,
        TextCapitalization? textCapitalization,
        Action<string>? viewOnChanged,
        Action<string>? viewOnSubmitted,
        TextInputAction? textInputAction,
        TextInputType? keyboardType,
        SmartDashesType? smartDashesType,
        SmartQuotesType? smartQuotesType,
        bool showFullScreenView,
        ThemeData theme,
        SearchViewThemeData searchViewTheme,
        DividerThemeData dividerTheme,
        MediaQueryData mediaQuery,
        MaterialLocalizations localizations)
    {
        Route = route;
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
        TextInputAction = textInputAction;
        KeyboardType = keyboardType;
        SmartDashesType = smartDashesType;
        SmartQuotesType = smartQuotesType;
        ShowFullScreenView = showFullScreenView;
        Theme = theme;
        SearchViewTheme = searchViewTheme;
        DividerTheme = dividerTheme;
        MediaQuery = mediaQuery;
        Localizations = localizations;
    }

    public SearchAnchor.SearchViewRoute Route { get; }
    public SearchController SearchController { get; }
    public SuggestionsBuilder SuggestionsBuilder { get; }
    public SearchViewBuilder? ViewBuilder { get; }
    public Widget? ViewLeading { get; }
    public IReadOnlyList<Widget>? ViewTrailing { get; }
    public string? ViewHintText { get; }
    public Color? ViewBackgroundColor { get; }
    public double? ViewElevation { get; }
    public Color? ViewSurfaceTintColor { get; }
    public BorderSide? ViewSide { get; }
    public ShapeBorder? ViewShape { get; }
    public Thickness? ViewBarPadding { get; }
    public double? HeaderHeight { get; }
    public TextStyle? HeaderTextStyle { get; }
    public TextStyle? HeaderHintStyle { get; }
    public Color? DividerColor { get; }
    public BoxConstraints? ViewConstraints { get; }
    public Thickness? ViewPadding { get; }
    public bool? ShrinkWrap { get; }
    public TextCapitalization? TextCapitalization { get; }
    public Action<string>? ViewOnChanged { get; }
    public Action<string>? ViewOnSubmitted { get; }
    public TextInputAction? TextInputAction { get; }
    public TextInputType? KeyboardType { get; }
    public SmartDashesType? SmartDashesType { get; }
    public SmartQuotesType? SmartQuotesType { get; }
    public bool ShowFullScreenView { get; }
    public ThemeData Theme { get; }
    public SearchViewThemeData SearchViewTheme { get; }
    public DividerThemeData DividerTheme { get; }
    public MediaQueryData MediaQuery { get; }
    public MaterialLocalizations Localizations { get; }

    public override State CreateState()
    {
        return new SearchViewContentState();
    }

    private sealed class SearchViewContentState : State
    {
        private SearchViewContent Current => (SearchViewContent)StateWidget;

        public override void InitState()
        {
            Current.SearchController.AddListener(HandleControllerChanged);
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var old = (SearchViewContent)oldWidget;
            if (!ReferenceEquals(old.SearchController, Current.SearchController))
            {
                old.SearchController.RemoveListener(HandleControllerChanged);
                Current.SearchController.AddListener(HandleControllerChanged);
            }
        }

        public override void Dispose()
        {
            Current.SearchController.RemoveListener(HandleControllerChanged);
        }

        public override Widget Build(BuildContext context)
        {
            var theme = Current.Theme;
            var viewTheme = Current.SearchViewTheme;
            var defaults = SearchViewDefaults.Resolve(theme, Current.ShowFullScreenView);
            var shape = Current.ViewShape ?? viewTheme.Shape ?? defaults.Shape!;
            var side = Current.ViewSide ?? viewTheme.Side ?? defaults.Side;
            if (side.HasValue)
            {
                shape = shape with { Side = side };
            }

            double elevation = Current.ViewElevation ?? viewTheme.Elevation ?? defaults.Elevation ?? 6.0;
            var background = Current.ViewBackgroundColor ?? viewTheme.BackgroundColor ?? defaults.BackgroundColor ?? theme.SurfaceContainerHighColor;
            var surfaceTint = Current.ViewSurfaceTintColor ?? viewTheme.SurfaceTintColor ?? defaults.SurfaceTintColor ?? Colors.Transparent;
            if (theme.UseMaterial3 && surfaceTint.A > 0)
            {
                background = NavigationSurfaceUtilities.ApplySurfaceTint(background, surfaceTint, elevation);
            }

            BoxConstraints constraints = Current.ViewConstraints ?? viewTheme.Constraints ?? defaults.Constraints!.Value;
            double maxWidth = Current.ShowFullScreenView
                ? Current.MediaQuery.Size.Width
                : Math.Min(Current.MediaQuery.Size.Width, double.IsInfinity(constraints.MaxWidth) ? Current.MediaQuery.Size.Width : constraints.MaxWidth);
            double maxHeight = Current.ShowFullScreenView
                ? Current.MediaQuery.Size.Height
                : Math.Min(Current.MediaQuery.Size.Height, double.IsInfinity(constraints.MaxHeight) ? Current.MediaQuery.Size.Height * 2 / 3 : constraints.MaxHeight);
            double minWidth = Current.ShowFullScreenView ? maxWidth : Math.Min(constraints.MinWidth, maxWidth);
            double minHeight = Current.ShowFullScreenView ? maxHeight : Math.Min(constraints.MinHeight, maxHeight);
            bool shrinkWrap = Current.ShrinkWrap ?? viewTheme.ShrinkWrap ?? defaults.ShrinkWrap ?? false;

            var suggestions = Current.SuggestionsBuilder(context, Current.SearchController) ?? [];
            Widget suggestionList = Current.ViewBuilder?.Invoke(suggestions)
                                    ?? new ListView(children: suggestions, padding: new Thickness(0));
            if (shrinkWrap && !Current.ShowFullScreenView)
            {
                suggestionList = new SingleChildScrollView(
                    child: new ListBody(children: suggestions),
                    padding: new Thickness(0));
            }

            var contentChildren = new List<Widget>
            {
                BuildHeader(context, viewTheme, defaults),
            };

            if (!shrinkWrap || minHeight > 0 || Current.ShowFullScreenView || suggestions.Count > 0)
            {
                var dividerColor = Current.DividerColor
                                   ?? viewTheme.DividerColor
                                   ?? Current.DividerTheme.Color
                                   ?? defaults.DividerColor
                                   ?? theme.OutlineColor;
                contentChildren.Add(new Divider(height: 1, thickness: 1, color: dividerColor));
                contentChildren.Add(new Flexible(
                    child: suggestionList,
                    fit: shrinkWrap && !Current.ShowFullScreenView ? FlexFit.Loose : FlexFit.Tight));
            }

            Widget surface = new Column(
                mainAxisSize: Current.ShowFullScreenView || !shrinkWrap ? MainAxisSize.Max : MainAxisSize.Min,
                crossAxisAlignment: CrossAxisAlignment.Stretch,
                children: contentChildren);
            surface = new DecoratedBox(
                new BoxDecoration(
                    Color: background,
                    Border: shape.Side,
                    BorderRadius: shape.BorderRadius,
                    BoxShadows: BuildBoxShadows(theme.ShadowColor, elevation)),
                surface);
            surface = new ClipRRect(shape.BorderRadius, surface);
            surface = new ConstrainedBox(
                new BoxConstraints(
                    MinWidth: minWidth,
                    MaxWidth: maxWidth,
                    MinHeight: minHeight,
                    MaxHeight: maxHeight),
                surface);

            if (!Current.ShowFullScreenView)
            {
                surface = new Padding(Current.ViewPadding ?? viewTheme.Padding ?? default, surface);
            }

            return new Align(
                alignment: Current.ShowFullScreenView ? Alignment.TopLeft : Alignment.TopCenter,
                child: surface);
        }

        private Widget BuildHeader(BuildContext context, SearchViewThemeData viewTheme, SearchViewThemeData defaults)
        {
            double? height = Current.HeaderHeight ?? viewTheme.HeaderHeight;
            var headerTextStyle = Current.HeaderTextStyle ?? viewTheme.HeaderTextStyle ?? defaults.HeaderTextStyle;
            var headerHintStyle = Current.HeaderHintStyle
                                  ?? viewTheme.HeaderHintStyle
                                  ?? Current.HeaderTextStyle
                                  ?? viewTheme.HeaderTextStyle
                                  ?? defaults.HeaderHintStyle;
            var barPadding = Current.ViewBarPadding ?? viewTheme.BarPadding ?? defaults.BarPadding;
            var trailing = Current.ViewTrailing ?? BuildDefaultTrailing();
            Widget leading = Current.ViewLeading ?? new BackButton(
                style: new ButtonStyle(TapTargetSize: MaterialTapTargetSize.ShrinkWrap),
                onPressed: () => Current.Route.Navigator?.MaybePop());

            Widget searchBar = new SearchBar(
                controller: Current.SearchController,
                autoFocus: true,
                constraints: height.HasValue
                    ? BoxConstraints.TightFor(height: height)
                    : Current.ShowFullScreenView
                        ? new BoxConstraints(MinHeight: SearchViewDefaults.FullScreenBarHeight)
                        : null,
                padding: MaterialStateProperty<Thickness?>.All(barPadding),
                leading: leading,
                trailing: trailing,
                hintText: Current.ViewHintText,
                backgroundColor: MaterialStateProperty<Color?>.All(Colors.Transparent),
                overlayColor: MaterialStateProperty<Color?>.All(Colors.Transparent),
                elevation: MaterialStateProperty<double?>.All(0.0),
                textStyle: MaterialStateProperty<TextStyle?>.All(headerTextStyle),
                hintStyle: MaterialStateProperty<TextStyle?>.All(headerHintStyle),
                onChanged: value =>
                {
                    Current.ViewOnChanged?.Invoke(value);
                    SetState(() => { });
                },
                onSubmitted: Current.ViewOnSubmitted,
                textCapitalization: Current.TextCapitalization,
                textInputAction: Current.TextInputAction,
                keyboardType: Current.KeyboardType,
                smartDashesType: Current.SmartDashesType,
                smartQuotesType: Current.SmartQuotesType);

            return Current.ShowFullScreenView
                ? new Padding(new Thickness(0, Current.MediaQuery.Padding.Top, 0, 0), searchBar)
                : searchBar;
        }

        private IReadOnlyList<Widget> BuildDefaultTrailing()
        {
            if (string.IsNullOrEmpty(Current.SearchController.Text))
            {
                return [];
            }

            return
            [
                new IconButton(
                    icon: new Icon(Icons.Clear),
                    onPressed: () =>
                    {
                        Current.SearchController.Clear();
                        SetState(() => { });
                    },
                    padding: new Thickness(8),
                    constraints: BoxConstraints.TightFor(width: 48, height: 48))
            ];
        }

        private void HandleControllerChanged()
        {
            if (Mounted)
            {
                SetState(() => { });
            }
        }

        private static BoxShadows? BuildBoxShadows(Color color, double elevation)
        {
            if (color.A == 0 || elevation <= 0)
            {
                return null;
            }

            return new BoxShadows(new BoxShadow
            {
                OffsetY = Math.Max(1, elevation * 0.5),
                Blur = Math.Max(2, elevation * 2.4),
                Color = ApplyOpacity(color, 0.20),
            });
        }

        private static Color ApplyOpacity(Color color, double opacity)
        {
            return Color.FromArgb(
                (byte)Math.Round(color.A * Math.Clamp(opacity, 0, 1)),
                color.R,
                color.G,
                color.B);
        }
    }
}

public sealed class SearchBar : StatefulWidget
{
    private const double DisabledOpacity = 0.38;

    public SearchBar(
        TextEditingController? controller = null,
        FocusNode? focusNode = null,
        string? hintText = null,
        Widget? leading = null,
        IReadOnlyList<Widget>? trailing = null,
        Action? onTap = null,
        Action? onTapOutside = null,
        Action<string>? onChanged = null,
        Action<string>? onSubmitted = null,
        BoxConstraints? constraints = null,
        MaterialStateProperty<double?>? elevation = null,
        MaterialStateProperty<Color?>? backgroundColor = null,
        MaterialStateProperty<Color?>? shadowColor = null,
        MaterialStateProperty<Color?>? surfaceTintColor = null,
        MaterialStateProperty<Color?>? overlayColor = null,
        MaterialStateProperty<BorderSide?>? side = null,
        MaterialStateProperty<ShapeBorder?>? shape = null,
        MaterialStateProperty<Thickness?>? padding = null,
        MaterialStateProperty<TextStyle?>? textStyle = null,
        MaterialStateProperty<TextStyle?>? hintStyle = null,
        TextCapitalization? textCapitalization = null,
        bool enabled = true,
        bool autoFocus = false,
        TextInputAction? textInputAction = null,
        TextInputType? keyboardType = null,
        Thickness? scrollPadding = null,
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
    public Action? OnTapOutside { get; }
    public Action<string>? OnChanged { get; }
    public Action<string>? OnSubmitted { get; }
    public BoxConstraints? Constraints { get; }
    public MaterialStateProperty<double?>? Elevation { get; }
    public MaterialStateProperty<Color?>? BackgroundColor { get; }
    public MaterialStateProperty<Color?>? ShadowColor { get; }
    public MaterialStateProperty<Color?>? SurfaceTintColor { get; }
    public MaterialStateProperty<Color?>? OverlayColor { get; }
    public MaterialStateProperty<BorderSide?>? Side { get; }
    public MaterialStateProperty<ShapeBorder?>? Shape { get; }
    public MaterialStateProperty<Thickness?>? Padding { get; }
    public MaterialStateProperty<TextStyle?>? TextStyle { get; }
    public MaterialStateProperty<TextStyle?>? HintStyle { get; }
    public TextCapitalization? TextCapitalization { get; }
    public bool Enabled { get; }
    public bool AutoFocus { get; }
    public TextInputAction? TextInputAction { get; }
    public TextInputType? KeyboardType { get; }
    public Thickness ScrollPadding { get; }
    public bool ReadOnly { get; }
    public SmartDashesType? SmartDashesType { get; }
    public SmartQuotesType? SmartQuotesType { get; }

    public override State CreateState()
    {
        return new SearchBarState();
    }

    private sealed class SearchBarState : State
    {
        private MaterialStatesController? _statesController;
        private FocusNode? _focusNode;
        private bool _ownsFocusNode;

        private SearchBar CurrentWidget => (SearchBar)StateWidget;

        public override void InitState()
        {
            _statesController = new MaterialStatesController();
            _statesController.AddListener(HandleStatesChanged);
            AttachFocusNode(CurrentWidget.FocusNode);
            SyncDisabledState();
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var old = (SearchBar)oldWidget;
            if (!ReferenceEquals(old.FocusNode, CurrentWidget.FocusNode))
            {
                DetachFocusNode();
                AttachFocusNode(CurrentWidget.FocusNode);
            }

            SyncDisabledState();
        }

        public override void Dispose()
        {
            DetachFocusNode();
            if (_statesController is not null)
            {
                _statesController.RemoveListener(HandleStatesChanged);
                _statesController.Dispose();
                _statesController = null;
            }
        }

        public override Widget Build(BuildContext context)
        {
            var theme = Theme.Of(context);
            var searchBarTheme = SearchBarTheme.Of(context);
            var defaults = SearchBarDefaults.Resolve(theme);
            MaterialState states = _statesController?.Value ?? MaterialState.None;
            if (!CurrentWidget.Enabled)
            {
                states |= MaterialState.Disabled;
            }

            var effectiveTextStyle = Resolve(CurrentWidget.TextStyle, searchBarTheme.TextStyle, defaults.TextStyle, states);
            var effectiveHintStyle = Resolve(CurrentWidget.HintStyle, searchBarTheme.HintStyle, CurrentWidget.TextStyle, states)
                                     ?? Resolve(searchBarTheme.TextStyle, defaults.HintStyle, null, states);
            double elevation = Resolve(CurrentWidget.Elevation, searchBarTheme.Elevation, defaults.Elevation, states) ?? 6.0;
            var background = Resolve(CurrentWidget.BackgroundColor, searchBarTheme.BackgroundColor, defaults.BackgroundColor, states)
                             ?? theme.SurfaceContainerHighColor;
            var shadowColor = Resolve(CurrentWidget.ShadowColor, searchBarTheme.ShadowColor, defaults.ShadowColor, states)
                              ?? theme.ShadowColor;
            var surfaceTint = Resolve(CurrentWidget.SurfaceTintColor, searchBarTheme.SurfaceTintColor, defaults.SurfaceTintColor, states)
                              ?? Colors.Transparent;
            var shape = Resolve(CurrentWidget.Shape, searchBarTheme.Shape, defaults.Shape, states)
                        ?? ShapeBorder.Stadium();
            var side = Resolve(CurrentWidget.Side, searchBarTheme.Side, defaults.Side, states);
            if (side.HasValue)
            {
                shape = shape with { Side = side };
            }
            var padding = Resolve(CurrentWidget.Padding, searchBarTheme.Padding, defaults.Padding, states) ?? new Thickness(8, 0);
            var constraints = CurrentWidget.Constraints
                              ?? searchBarTheme.Constraints
                              ?? defaults.Constraints
                              ?? new BoxConstraints(MinWidth: 360, MaxWidth: 800, MinHeight: 56);
            if (theme.UseMaterial3 && surfaceTint.A > 0)
            {
                background = NavigationSurfaceUtilities.ApplySurfaceTint(background, surfaceTint, elevation);
            }

            var children = new List<Widget>();
            if (CurrentWidget.Leading is not null)
            {
                children.Add(new IconTheme(
                    new IconThemeData(Color: theme.OnSurfaceColor, Size: 24),
                    CurrentWidget.Leading));
            }

            Widget textField = new TextField(
                controller: CurrentWidget.Controller,
                focusNode: _focusNode,
                decoration: InputDecoration.Collapsed(
                    hintText: CurrentWidget.HintText,
                    hintStyle: effectiveHintStyle,
                    enabled: CurrentWidget.Enabled),
                style: effectiveTextStyle,
                keyboardType: CurrentWidget.KeyboardType,
                textInputAction: CurrentWidget.TextInputAction,
                autofocus: CurrentWidget.AutoFocus,
                readOnly: CurrentWidget.ReadOnly,
                enabled: CurrentWidget.Enabled,
                onChanged: CurrentWidget.OnChanged,
                onSubmitted: CurrentWidget.OnSubmitted,
                canRequestFocus: CurrentWidget.Enabled);
            children.Add(new Expanded(
                new Padding(padding, textField)));

            if (CurrentWidget.Trailing is not null)
            {
                foreach (var trailing in CurrentWidget.Trailing)
                {
                    children.Add(new IconTheme(
                        new IconThemeData(Color: theme.OnSurfaceVariantColor, Size: 24),
                        trailing));
                }
            }

            Widget content = new Row(
                textDirection: Directionality.Of(context),
                children: children);
            content = new Padding(padding, content);
            content = new InkWell(
                onTap: CurrentWidget.Enabled
                    ? () =>
                    {
                        CurrentWidget.OnTap?.Invoke();
                        _focusNode?.RequestFocus();
                    }
                    : null,
                overlayColor: CurrentWidget.OverlayColor ?? searchBarTheme.OverlayColor ?? defaults.OverlayColor,
                customBorder: shape,
                statesController: _statesController,
                focusNode: _focusNode,
                canRequestFocus: CurrentWidget.Enabled,
                borderRadius: shape.BorderRadius,
                child: content);
            content = new DecoratedBox(
                new BoxDecoration(
                    Color: background,
                    Border: shape.Side,
                    BorderRadius: shape.BorderRadius,
                    BoxShadows: BuildBoxShadows(shadowColor, elevation)),
                content);
            content = new ClipRRect(shape.BorderRadius, content);
            content = new Opacity(CurrentWidget.Enabled ? 1.0 : DisabledOpacity, content);
            return new ConstrainedBox(constraints, content);
        }

        private static T? Resolve<T>(
            MaterialStateProperty<T?>? widgetValue,
            MaterialStateProperty<T?>? themeValue,
            MaterialStateProperty<T?>? defaultValue,
            MaterialState states)
        {
            T? value = widgetValue is null ? default : widgetValue.Resolve(states);
            if (value is not null)
            {
                return value;
            }

            value = themeValue is null ? default : themeValue.Resolve(states);
            if (value is not null)
            {
                return value;
            }

            return defaultValue is null ? default : defaultValue.Resolve(states);
        }

        private void AttachFocusNode(FocusNode? external)
        {
            _focusNode = external ?? new FocusNode();
            _ownsFocusNode = external is null;
            _focusNode.AddListener(HandleFocusChanged);
            _statesController?.Update(MaterialState.Focused, _focusNode.HasFocus);
        }

        private void DetachFocusNode()
        {
            if (_focusNode is null)
            {
                return;
            }

            _focusNode.RemoveListener(HandleFocusChanged);
            if (_ownsFocusNode)
            {
                _focusNode.Dispose();
            }

            _focusNode = null;
            _ownsFocusNode = false;
        }

        private void HandleFocusChanged()
        {
            _statesController?.Update(MaterialState.Focused, _focusNode?.HasFocus == true);
        }

        private void HandleStatesChanged()
        {
            if (Mounted)
            {
                SetState(() => { });
            }
        }

        private void SyncDisabledState()
        {
            _statesController?.Update(MaterialState.Disabled, !CurrentWidget.Enabled);
        }

        private static BoxShadows? BuildBoxShadows(Color color, double elevation)
        {
            if (color.A == 0 || elevation <= 0)
            {
                return null;
            }

            return new BoxShadows(new BoxShadow
            {
                OffsetY = Math.Max(1, elevation * 0.5),
                Blur = Math.Max(2, elevation * 2.4),
                Color = ApplyOpacity(color, 0.20),
            });
        }

        private static Color ApplyOpacity(Color color, double opacity)
        {
            return Color.FromArgb(
                (byte)Math.Round(color.A * Math.Clamp(opacity, 0, 1)),
                color.R,
                color.G,
                color.B);
        }
    }
}

internal static class SearchBarDefaults
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
                    return MaterialButtonCore.ApplyOpacity(theme.OnSurfaceColor, 0.10);
                }

                if (states.HasFlag(MaterialState.Hovered))
                {
                    return MaterialButtonCore.ApplyOpacity(theme.OnSurfaceColor, 0.08);
                }

                return Colors.Transparent;
            }),
            Shape: MaterialStateProperty<ShapeBorder?>.All(ShapeBorder.Stadium()),
            Padding: MaterialStateProperty<Thickness?>.All(new Thickness(8, 0)),
            TextStyle: MaterialStateProperty<TextStyle?>.All(
                theme.TextTheme.BodyLarge.CopyWith(color: theme.OnSurfaceColor)),
            HintStyle: MaterialStateProperty<TextStyle?>.All(
                theme.TextTheme.BodyLarge.CopyWith(color: theme.OnSurfaceVariantColor)),
            Constraints: new BoxConstraints(MinWidth: 360, MaxWidth: 800, MinHeight: 56),
            TextCapitalization: TextCapitalization.None);
    }
}

internal static class SearchViewDefaults
{
    public const double FullScreenBarHeight = 72.0;

    public static SearchViewThemeData Resolve(ThemeData theme, bool isFullScreen)
    {
        return new SearchViewThemeData(
            BackgroundColor: theme.SurfaceContainerHighColor,
            Elevation: 6.0,
            SurfaceTintColor: Colors.Transparent,
            Shape: isFullScreen
                ? ShapeBorder.RoundedRectangle(0)
                : ShapeBorder.RoundedRectangle(28),
            HeaderTextStyle: theme.TextTheme.BodyLarge.CopyWith(color: theme.OnSurfaceColor),
            HeaderHintStyle: theme.TextTheme.BodyLarge.CopyWith(color: theme.OnSurfaceVariantColor),
            Constraints: new BoxConstraints(MinWidth: 360, MinHeight: 240),
            BarPadding: new Thickness(8, 0),
            ShrinkWrap: false,
            DividerColor: theme.OutlineColor);
    }
}
