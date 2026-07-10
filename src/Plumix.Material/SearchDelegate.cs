using Avalonia.Media;
using Plumix.Foundation;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: flutter/packages/flutter/lib/src/material/search.dart

public abstract class SearchDelegate<T> : IDisposable
{
    private readonly TextEditingController _queryController = new();
    private SearchPageRoute<T>? _route;
    private SearchPageState<T>? _page;

    protected SearchDelegate(
        string? searchFieldLabel = null,
        TextStyle? searchFieldStyle = null,
        InputDecorationThemeData? searchFieldDecorationTheme = null,
        TextInputType? keyboardType = null,
        TextInputAction textInputAction = TextInputAction.Search,
        bool autocorrect = true,
        bool enableSuggestions = true)
    {
        if (searchFieldStyle is not null && searchFieldDecorationTheme is not null)
        {
            throw new ArgumentException(
                "Only one of searchFieldStyle and searchFieldDecorationTheme may be specified.",
                nameof(searchFieldDecorationTheme));
        }

        SearchFieldLabel = searchFieldLabel;
        SearchFieldStyle = searchFieldStyle;
        SearchFieldDecorationTheme = searchFieldDecorationTheme;
        KeyboardType = keyboardType;
        TextInputAction = textInputAction;
        Autocorrect = autocorrect;
        EnableSuggestions = enableSuggestions;
    }

    public string? SearchFieldLabel { get; }

    public TextStyle? SearchFieldStyle { get; }

    public InputDecorationThemeData? SearchFieldDecorationTheme { get; }

    public TextInputType? KeyboardType { get; }

    public TextInputAction TextInputAction { get; }

    public bool Autocorrect { get; }

    public bool EnableSuggestions { get; }

    public string Query
    {
        get => _queryController.Text;
        set
        {
            string next = value ?? string.Empty;
            _queryController.Value = new TextEditingValue(
                text: next,
                selection: TextSelection.Collapsed(next.Length));
        }
    }

    /// <summary>Current search-route fade controller, or <see langword="null"/> while inactive.</summary>
    public AnimationController? TransitionAnimation => _route?.Animation;

    public abstract Widget BuildSuggestions(BuildContext context);

    public abstract Widget BuildResults(BuildContext context);

    public virtual Widget? BuildLeading(BuildContext context) => null;

    public virtual IReadOnlyList<Widget>? BuildActions(BuildContext context) => null;

    public virtual Widget? BuildBottom(BuildContext context) => null;

    public virtual Widget? BuildFlexibleSpace(BuildContext context) => null;

    public virtual bool? AutomaticallyImplyLeading => null;

    public virtual double? LeadingWidth => null;

    public virtual ThemeData AppBarTheme(BuildContext context)
    {
        var theme = Theme.Of(context);
        Color background = theme.Brightness == Brightness.Dark
            ? Color.Parse("#FF212121")
            : Colors.White;
        Color foreground = theme.Brightness == Brightness.Dark ? Colors.White : Colors.Black;
        var decorationTheme = SearchFieldDecorationTheme
                              ?? theme.InputDecorationTheme with
                              {
                                  HintStyle = SearchFieldStyle ?? theme.InputDecorationTheme.HintStyle,
                                  Border = InputBorder.None,
                              };
        return theme with
        {
            AppBarTheme = theme.AppBarTheme with
            {
                BackgroundColor = background,
                ForegroundColor = foreground,
                TitleTextStyle = theme.TextTheme.TitleLarge,
                ToolbarTextStyle = theme.TextTheme.BodyMedium,
            },
            InputDecorationTheme = decorationTheme,
        };
    }

    public void ShowResults(BuildContext context)
    {
        _ = context;
        ShowResults();
    }

    public void ShowResults()
    {
        _page?.ShowResults();
    }

    public void ShowSuggestions(BuildContext context)
    {
        _ = context;
        ShowSuggestions();
    }

    public void ShowSuggestions()
    {
        _page?.ShowSuggestions();
    }

    public void Close(BuildContext context, T? result)
    {
        if (_route is null)
        {
            throw new InvalidOperationException("SearchDelegate is not associated with an active showSearch call.");
        }

        _ = context;
        Close(result);
    }

    public void Close(T? result)
    {
        if (_route is null)
        {
            throw new InvalidOperationException("SearchDelegate is not associated with an active showSearch call.");
        }

        _page?.ClearFocus();
        _page?.Close(result);
    }

    internal TextEditingController QueryController => _queryController;

    public bool IsActive => _route is not null;

    internal void AttachRoute(SearchPageRoute<T> route)
    {
        if (_route is not null && !ReferenceEquals(_route, route))
        {
            throw new InvalidOperationException(
                "A SearchDelegate can only be associated with one active showSearch call.");
        }

        _route = route;
    }

    internal void DetachRoute(SearchPageRoute<T> route)
    {
        if (ReferenceEquals(_route, route))
        {
            _route = null;
            _page = null;
        }
    }

    internal void AttachPage(SearchPageState<T> page) => _page = page;

    internal void DetachPage(SearchPageState<T> page)
    {
        if (ReferenceEquals(_page, page))
        {
            _page = null;
        }
    }

    public virtual void Dispose()
    {
        _queryController.Dispose();
    }
}

public static class MaterialSearch
{
    public static Task<T?> ShowSearch<T>(
        BuildContext context,
        SearchDelegate<T> searchDelegate,
        string? query = "",
        bool useRootNavigator = false,
        bool maintainState = false)
    {
        ArgumentNullException.ThrowIfNull(searchDelegate);
        if (searchDelegate.IsActive)
        {
            throw new InvalidOperationException(
                "A SearchDelegate can only be associated with one active showSearch call.");
        }

        if (query is not null)
        {
            searchDelegate.Query = query;
        }

        var route = new SearchPageRoute<T>(context, searchDelegate, maintainState);
        Navigator.Of(context, rootNavigator: useRootNavigator).Push(route);
        return route.Completed;
    }
}

internal enum SearchBody
{
    Suggestions,
    Results,
}

internal sealed class SearchPageRoute<T> : PageRoute
{
    private readonly SearchDelegate<T> _delegate;
    private readonly ThemeData _theme;
    private readonly MediaQueryData _mediaQuery;
    private readonly TextDirection _textDirection;
    private readonly MaterialLocalizations _localizations;
    private readonly TaskCompletionSource<T?> _completed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private object? _pendingResult;
    private bool _isExiting;

    public SearchPageRoute(BuildContext context, SearchDelegate<T> searchDelegate, bool maintainState)
    {
        _delegate = searchDelegate ?? throw new ArgumentNullException(nameof(searchDelegate));
        _theme = Theme.Of(context);
        _mediaQuery = MediaQuery.Of(context);
        _textDirection = Directionality.Of(context);
        _localizations = MaterialLocalizations.Of(context);
        MaintainState = maintainState;
        Animation = new AnimationController(TimeSpan.FromMilliseconds(300)) { Curve = Curves.EaseOut };
        Animation.Changed += HandleAnimationChanged;
        Animation.Dismissed += HandleAnimationDismissed;
        _delegate.AttachRoute(this);
    }

    public bool MaintainState { get; }

    public AnimationController Animation { get; }

    public Task<T?> Completed => _completed.Task;

    protected override void OnAttach() => Animation.Forward(from: 0);

    public override bool WillPop(object? result)
    {
        if (_isExiting || Animation.Value <= 0)
        {
            return base.WillPop(result);
        }

        _pendingResult = result;
        _isExiting = true;
        Animation.Reverse(from: Animation.Value);
        return false;
    }

    public override void DidComplete(object? result)
    {
        _delegate.DetachRoute(this);
        if (result is null)
        {
            _completed.TrySetResult(default);
        }
        else if (result is T typed)
        {
            _completed.TrySetResult(typed);
        }
        else
        {
            _completed.TrySetException(new InvalidCastException(
                $"Search result of type {result.GetType().Name} cannot be converted to {typeof(T).Name}."));
        }
    }

    public override Widget BuildPage(BuildContext context)
    {
        Widget page = new SearchPage<T>(_delegate, _localizations);
        page = new Opacity(Math.Clamp(Animation.Value, 0, 1), page);
        page = new Theme(_theme, page);
        page = new MediaQuery(_mediaQuery, page);
        return new Directionality(_textDirection, page);
    }

    public override void Dispose()
    {
        Animation.Changed -= HandleAnimationChanged;
        Animation.Dismissed -= HandleAnimationDismissed;
        Animation.Dispose();
        _delegate.DetachRoute(this);
        if (!_completed.Task.IsCompleted)
        {
            _completed.TrySetResult(default);
        }

        base.Dispose();
    }

    private void HandleAnimationChanged() => NotifyRouteChanged();

    private void HandleAnimationDismissed()
    {
        if (_isExiting)
        {
            Navigator?.MaybePop(_pendingResult);
        }
    }
}

internal sealed class SearchPage<T> : StatefulWidget
{
    public SearchPage(SearchDelegate<T> searchDelegate, MaterialLocalizations localizations) : base(key: null)
    {
        SearchDelegate = searchDelegate;
        Localizations = localizations;
    }

    public SearchDelegate<T> SearchDelegate { get; }

    public MaterialLocalizations Localizations { get; }

    public override State CreateState() => new SearchPageState<T>();
}

internal sealed class SearchPageState<T> : State
{
    private readonly FocusNode _focusNode = new();
    private SearchBody _body = SearchBody.Suggestions;

    private SearchPage<T> Current => (SearchPage<T>)StateWidget;

    public override void InitState()
    {
        Current.SearchDelegate.QueryController.AddListener(HandleQueryChanged);
        _focusNode.AddListener(HandleFocusChanged);
        Current.SearchDelegate.AttachPage(this);
    }

    public override void Dispose()
    {
        Current.SearchDelegate.QueryController.RemoveListener(HandleQueryChanged);
        Current.SearchDelegate.DetachPage(this);
        _focusNode.RemoveListener(HandleFocusChanged);
        _focusNode.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        var searchDelegate = Current.SearchDelegate;
        var theme = searchDelegate.AppBarTheme(context);
        string fieldLabel = searchDelegate.SearchFieldLabel ?? Current.Localizations.SearchFieldLabel;
        Widget body = _body == SearchBody.Suggestions
            ? searchDelegate.BuildSuggestions(context)
            : searchDelegate.BuildResults(context);
        string routeLabel = theme.Platform is TargetPlatform.IOS or TargetPlatform.MacOS ? string.Empty : fieldLabel;
        var fieldStyle = searchDelegate.SearchFieldStyle ?? theme.TextTheme.TitleLarge;
        Widget title = new TextField(
            controller: searchDelegate.QueryController,
            focusNode: _focusNode,
            style: fieldStyle,
            decoration: new InputDecoration(hintText: fieldLabel, border: InputBorder.None),
            onSubmitted: _ => ShowResults(),
            onKeyEvent: HandleKeyEvent);
        Widget page = new Scaffold(
            appBar: new AppBar(
                leading: searchDelegate.BuildLeading(context),
                automaticallyImplyLeading: searchDelegate.AutomaticallyImplyLeading ?? true,
                leadingWidth: searchDelegate.LeadingWidth,
                title: title,
                actions: searchDelegate.BuildActions(context),
                bottom: searchDelegate.BuildBottom(context),
                flexibleSpace: searchDelegate.BuildFlexibleSpace(context)),
            body: body);
        page = new Theme(theme, page);
        return new Semantics(
            label: routeLabel,
            scopesRoute: true,
            namesRoute: true,
            explicitChildNodes: true,
            child: page);
    }

    internal void ShowResults()
    {
        _focusNode.Unfocus();
        if (_body != SearchBody.Results)
        {
            SetState(() => _body = SearchBody.Results);
        }
    }

    internal void ShowSuggestions()
    {
        _focusNode.RequestFocus();
        if (_body != SearchBody.Suggestions)
        {
            SetState(() => _body = SearchBody.Suggestions);
        }
    }

    internal void ClearFocus() => _focusNode.Unfocus();

    internal void Close(T? result) => Navigator.MaybePop(Context, result);

    private void HandleQueryChanged() => SetState(static () => { });

    private void HandleFocusChanged()
    {
        if (_focusNode.HasFocus && _body != SearchBody.Suggestions)
        {
            SetState(() => _body = SearchBody.Suggestions);
        }
    }

    private KeyEventResult HandleKeyEvent(FocusNode node, KeyEvent @event)
    {
        _ = node;
        if (@event.IsDown && string.Equals(@event.Key, "Escape", StringComparison.OrdinalIgnoreCase))
        {
            Navigator.MaybePop(Context);
            return KeyEventResult.Handled;
        }

        return KeyEventResult.Ignored;
    }
}
