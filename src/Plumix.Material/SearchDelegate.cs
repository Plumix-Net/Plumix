using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source: material_ui/lib/src/search.dart

public abstract class SearchDelegate<T> : IDisposable
{
    private readonly TextEditingController _queryController = new();
    private readonly ProxyAnimation _proxyAnimation = new();
    private readonly ValueNotifier<SearchBody?> _currentBodyNotifier = new(null);
    private SearchPageRoute<T>? _route;
    private SearchPageState<T>? _page;
    private FocusNode? _focusNode;

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

    public Animation<double> TransitionAnimation => _proxyAnimation;

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
        Color background = theme.ColorScheme.Brightness == Brightness.Dark
            ? Color.Parse("#FF212121")
            : Colors.White;
        var decorationTheme = SearchFieldDecorationTheme
                              ?? new InputDecorationThemeData(
                                  HintStyle: SearchFieldStyle ?? theme.InputDecorationTheme.HintStyle,
                                  Border: InputBorder.None);
        return theme with
        {
            AppBarTheme = theme.AppBarTheme with
            {
                SystemOverlayStyle = theme.ColorScheme.Brightness == Brightness.Dark
                    ? SystemUiOverlayStyle.Light
                    : SystemUiOverlayStyle.Dark,
                BackgroundColor = background,
                IconTheme = theme.PrimaryIconTheme.CopyWith(color: Colors.Gray),
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
        _focusNode?.Unfocus();
        CurrentBody = SearchBody.Results;
    }

    public void ShowSuggestions(BuildContext context)
    {
        _ = context;
        ShowSuggestions();
    }

    public void ShowSuggestions()
    {
        if (_focusNode is null)
        {
            throw new InvalidOperationException(
                "SearchDelegate must be associated with an active search before showing suggestions.");
        }

        _focusNode.RequestFocus();
        CurrentBody = SearchBody.Suggestions;
    }

    public void Close(BuildContext context, T? result)
    {
        if (_route is null)
        {
            throw new InvalidOperationException("SearchDelegate is not associated with an active showSearch call.");
        }

        CurrentBody = null;
        _focusNode?.Unfocus();
        NavigatorState navigator = Navigator.Of(context);
        navigator.PopUntil(route => ReferenceEquals(route, _route));
        navigator.Pop(result);
    }

    public void Close(T? result)
    {
        if (_route is null)
        {
            throw new InvalidOperationException("SearchDelegate is not associated with an active showSearch call.");
        }

        _page?.Close(result);
    }

    internal TextEditingController QueryController => _queryController;

    internal ValueNotifier<SearchBody?> CurrentBodyNotifier => _currentBodyNotifier;

    internal SearchBody? CurrentBody
    {
        get => _currentBodyNotifier.Value;
        set => _currentBodyNotifier.Value = value;
    }

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

    internal void AttachTransitionAnimation(Animation<double> animation)
    {
        _proxyAnimation.Parent = animation;
    }

    internal void DetachRoute(SearchPageRoute<T> route)
    {
        if (ReferenceEquals(_route, route))
        {
            _route = null;
            _page = null;
        }
    }

    internal void AttachPage(SearchPageState<T> page, FocusNode focusNode)
    {
        _page = page;
        _focusNode = focusNode;
    }

    internal void DetachPage(SearchPageState<T> page)
    {
        if (ReferenceEquals(_page, page))
        {
            _page = null;
            _focusNode = null;
        }
    }

    public virtual void Dispose()
    {
        _currentBodyNotifier.Dispose();
        _focusNode?.Dispose();
        _queryController.Dispose();
        _proxyAnimation.Parent = null;
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

        searchDelegate.CurrentBody = SearchBody.Suggestions;

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
    private readonly TaskCompletionSource<T?> _completed = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public SearchPageRoute(BuildContext context, SearchDelegate<T> searchDelegate, bool maintainState)
    {
        _delegate = searchDelegate ?? throw new ArgumentNullException(nameof(searchDelegate));
        _ = context;
        MaintainState = maintainState;
        _delegate.AttachRoute(this);
    }

    public bool MaintainState { get; }

    public override TimeSpan TransitionDuration => TimeSpan.FromMilliseconds(300);

    public Task<T?> Completed => _completed.Task;

    protected override void Install()
    {
        base.Install();
        _delegate.AttachTransitionAnimation(Animation);
    }

    public override void DidComplete(object? result)
    {
        base.DidComplete(result);
        _delegate.DetachRoute(this);
        _delegate.CurrentBody = null;
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
        _ = context;
        return new SearchPage<T>(_delegate, Animation);
    }

    public override Widget BuildTransitions(
        BuildContext context,
        Animation<double> animation,
        Animation<double> secondaryAnimation,
        Widget child)
    {
        _ = context;
        _ = secondaryAnimation;
        return new FadeTransition(opacity: animation, child: child);
    }

    public override void Dispose()
    {
        _delegate.DetachRoute(this);
        if (!_completed.Task.IsCompleted)
        {
            _completed.TrySetResult(default);
        }

        base.Dispose();
    }
}

internal sealed class SearchPage<T> : StatefulWidget
{
    public SearchPage(SearchDelegate<T> searchDelegate, Animation<double> animation) : base(key: null)
    {
        SearchDelegate = searchDelegate;
        Animation = animation;
    }

    public SearchDelegate<T> SearchDelegate { get; }

    public Animation<double> Animation { get; }

    public override State CreateState() => new SearchPageState<T>();
}

internal sealed class SearchPageState<T> : State
{
    private readonly FocusNode _focusNode = new();

    private SearchPage<T> Current => (SearchPage<T>)StateWidget;

    public override void InitState()
    {
        Current.SearchDelegate.QueryController.AddListener(HandleQueryChanged);
        Current.SearchDelegate.CurrentBodyNotifier.AddListener(HandleSearchBodyChanged);
        Current.Animation.AddStatusListener(HandleAnimationStatusChanged);
        _focusNode.AddListener(HandleFocusChanged);
        Current.SearchDelegate.AttachPage(this, _focusNode);
        if (Current.Animation.Status == AnimationStatus.Completed)
        {
            HandleAnimationStatusChanged(AnimationStatus.Completed);
        }
    }

    public override void Dispose()
    {
        Current.SearchDelegate.QueryController.RemoveListener(HandleQueryChanged);
        Current.SearchDelegate.CurrentBodyNotifier.RemoveListener(HandleSearchBodyChanged);
        Current.Animation.RemoveStatusListener(HandleAnimationStatusChanged);
        Current.SearchDelegate.DetachPage(this);
        _focusNode.RemoveListener(HandleFocusChanged);
        _focusNode.Dispose();
    }

    public override Widget Build(BuildContext context)
    {
        var searchDelegate = Current.SearchDelegate;
        var theme = searchDelegate.AppBarTheme(context);
        string fieldLabel = searchDelegate.SearchFieldLabel ?? MaterialLocalizations.Of(context).SearchFieldLabel;
        Widget? body = searchDelegate.CurrentBody switch
        {
            SearchBody.Suggestions => new KeyedSubtree(
                key: new ValueKey<SearchBody>(SearchBody.Suggestions),
                child: searchDelegate.BuildSuggestions(context)),
            SearchBody.Results => new KeyedSubtree(
                key: new ValueKey<SearchBody>(SearchBody.Results),
                child: searchDelegate.BuildResults(context)),
            _ => null,
        };
        string routeLabel = theme.Platform is TargetPlatform.IOS or TargetPlatform.MacOS ? string.Empty : fieldLabel;
        var fieldStyle = searchDelegate.SearchFieldStyle ?? theme.TextTheme.TitleLarge;
        Widget title = new TextField(
            controller: searchDelegate.QueryController,
            focusNode: _focusNode,
            style: fieldStyle,
            textInputAction: searchDelegate.TextInputAction,
            autocorrect: searchDelegate.Autocorrect,
            enableSuggestions: searchDelegate.EnableSuggestions,
            keyboardType: searchDelegate.KeyboardType,
            decoration: new InputDecoration(hintText: fieldLabel),
            onSubmitted: _ => ShowResults(),
            onKeyEvent: HandleKeyEvent);
        title = new Semantics(
            inputType: SemanticsInputType.Search,
            child: title);
        Widget page = new Scaffold(
            appBar: new AppBar(
                leading: searchDelegate.BuildLeading(context),
                automaticallyImplyLeading: searchDelegate.AutomaticallyImplyLeading ?? true,
                leadingWidth: searchDelegate.LeadingWidth,
                title: title,
                actions: searchDelegate.BuildActions(context),
                bottom: searchDelegate.BuildBottom(context),
                flexibleSpace: searchDelegate.BuildFlexibleSpace(context)),
            body: new AnimatedSwitcher(
                duration: TimeSpan.FromMilliseconds(300),
                child: body));
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
        Current.SearchDelegate.ShowResults(Context);
    }

    internal void ShowSuggestions()
    {
        Current.SearchDelegate.ShowSuggestions(Context);
    }

    internal void Close(T? result) => Current.SearchDelegate.Close(Context, result);

    private void HandleQueryChanged() => SetState(static () => { });

    private void HandleSearchBodyChanged() => SetState(static () => { });

    private void HandleAnimationStatusChanged(AnimationStatus status)
    {
        if (status != AnimationStatus.Completed)
        {
            return;
        }

        Current.Animation.RemoveStatusListener(HandleAnimationStatusChanged);
        if (Current.SearchDelegate.CurrentBody == SearchBody.Suggestions)
        {
            _focusNode.RequestFocus();
        }
    }

    private void HandleFocusChanged()
    {
        if (_focusNode.HasFocus && Current.SearchDelegate.CurrentBody != SearchBody.Suggestions)
        {
            Current.SearchDelegate.ShowSuggestions(Context);
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
