using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Input.TextInput;
using Avalonia.Media;
using System.Reflection;
using Flutter.Gestures;
using Flutter.Rendering;
using Flutter.UI;
using Flutter.Widgets;
using FrameworkFocusManager = Flutter.Widgets.FocusManager;
using AvaloniaTextSelection = Avalonia.Input.TextInput.TextSelection;

// Dart parity source (reference): flutter/packages/flutter/lib/src/widgets/binding.dart; flutter/packages/flutter/lib/src/rendering/binding.dart (host integration, adapted)

namespace Flutter;

public class FlutterHost : Control
{
    static FlutterHost()
    {
        TextInputMethodClientRequestedEvent.AddClassHandler<FlutterHost>((host, e) =>
        {
            e.Client = host._textInputClient;
        });
    }

    private readonly RenderView _root = new();
    private readonly PipelineOwner _pipeline;
    private readonly GestureBinding _gestureBinding = GestureBinding.Instance;
    private readonly FlutterTextInputMethodClient _textInputClient;
    private bool _isSubscribedToScheduler;
    private Size _lastArrangedSize;
    private TopLevel? _attachedTopLevel;
    private IInsetsManager? _insetsManager;
    private IInputPane? _inputPane;
    private bool _isSubscribedToSystemUiOverlayStyle;
    private SystemUiOverlayStyle _currentSystemUiOverlayStyle = SystemChrome.CurrentSystemUiOverlayStyle;

    public event Action<SemanticsNode?>? SemanticsUpdated;

    public FlutterHost()
    {
        _pipeline = new PipelineOwner(_root);
        _pipeline.OnNeedVisualUpdate = ScheduleVisualUpdate;
        _pipeline.Attach(_root);
        _textInputClient = new FlutterTextInputMethodClient(this);

        ClipToBounds = true;
        Focusable = true;
        EnsureSchedulerSubscription();
    }

    internal RenderBox? RootChild => _root.Child;

    public SemanticsNode? SemanticsRoot => _pipeline.SemanticsOwner.RootNode;

    public void SetRootChild(RenderBox? child)
    {
        _root.Child = child;
        _pipeline.RequestLayout();
        _pipeline.RequestPaint();
        ScheduleVisualUpdate();
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (!_lastArrangedSize.Equals(finalSize))
        {
            _lastArrangedSize = finalSize;
            OnMetricsChanged();
        }

        _pipeline.RequestLayout();
        return base.ArrangeOverride(finalSize);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Focus();
        e.Pointer.Capture(this);
        DispatchPointerEvent(ToPointerDownEvent(e));
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Handled)
        {
            return;
        }

        if (IsPasteShortcut(e))
        {
            e.Handled = true;
            _ = HandleSystemPasteShortcutAsync();
            return;
        }

        var keyEvent = new KeyEvent(
            key: e.Key.ToString(),
            isDown: true,
            isShiftPressed: e.KeyModifiers.HasFlag(KeyModifiers.Shift),
            isControlPressed: e.KeyModifiers.HasFlag(KeyModifiers.Control),
            isAltPressed: e.KeyModifiers.HasFlag(KeyModifiers.Alt),
            isMetaPressed: e.KeyModifiers.HasFlag(KeyModifiers.Meta));

        if (FrameworkFocusManager.Instance.HandleKeyEvent(keyEvent))
        {
            e.Handled = true;
            if (IsCopyOrCutShortcut(e))
            {
                _ = PushFrameworkClipboardToSystemAsync();
            }

            return;
        }

        var keyName = e.Key.ToString();
        var isBackKey = e.Key == Key.Escape
                        || string.Equals(keyName, "Back", StringComparison.Ordinal)
                        || string.Equals(keyName, "BrowserBack", StringComparison.Ordinal);
        if (!isBackKey)
        {
            return;
        }

        if (Navigator.TryHandleBackButton())
        {
            e.Handled = true;
        }
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);

        if (e.Handled)
        {
            return;
        }

        var keyEvent = new KeyEvent(
            key: e.Key.ToString(),
            isDown: false,
            isShiftPressed: e.KeyModifiers.HasFlag(KeyModifiers.Shift),
            isControlPressed: e.KeyModifiers.HasFlag(KeyModifiers.Control),
            isAltPressed: e.KeyModifiers.HasFlag(KeyModifiers.Alt),
            isMetaPressed: e.KeyModifiers.HasFlag(KeyModifiers.Meta));

        if (FrameworkFocusManager.Instance.HandleKeyEvent(keyEvent))
        {
            e.Handled = true;
        }
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);

        if (e.Handled || string.IsNullOrEmpty(e.Text))
        {
            return;
        }

        if (FrameworkFocusManager.Instance.HandleTextInput(e.Text))
        {
            e.Handled = true;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        var pointer = e.GetCurrentPoint(this);
        var position = pointer.Position;
        var buttons = ToPointerButtons(pointer.Properties);
        var kind = ToPointerKind(e.Pointer.Type);

        if (buttons == PointerButtons.None)
        {
            DispatchPointerEvent(new PointerHoverEvent(
                pointer: unchecked((int)e.Pointer.Id),
                kind: kind,
                position: position,
                buttons: buttons,
                timestampUtc: DateTime.UtcNow));
            return;
        }

        DispatchPointerEvent(new PointerMoveEvent(
            pointer: unchecked((int)e.Pointer.Id),
            kind: kind,
            position: position,
            buttons: buttons,
            down: true,
            timestampUtc: DateTime.UtcNow));
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        DispatchPointerEvent(new PointerUpEvent(
            pointer: unchecked((int)e.Pointer.Id),
            kind: ToPointerKind(e.Pointer.Type),
            position: e.GetPosition(this),
            buttons: ToPointerButtons(e.GetCurrentPoint(this).Properties),
            timestampUtc: DateTime.UtcNow));

        if (e.Pointer.Captured == this)
        {
            e.Pointer.Capture(null);
        }
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);

        DispatchPointerEvent(new PointerScrollEvent(
            pointer: unchecked((int)e.Pointer.Id),
            kind: ToPointerKind(e.Pointer.Type),
            position: e.GetPosition(this),
            buttons: ToPointerButtons(e.GetCurrentPoint(this).Properties),
            scrollDelta: new Point(e.Delta.X, e.Delta.Y),
            timestampUtc: DateTime.UtcNow));
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);

        DispatchPointerEvent(new PointerCancelEvent(
            pointer: unchecked((int)e.Pointer.Id),
            kind: ToPointerKind(e.Pointer.Type),
            position: default,
            buttons: PointerButtons.None,
            timestampUtc: DateTime.UtcNow));
    }

    public override void Render(DrawingContext context)
    {
        _pipeline.FlushLayout(Bounds.Size);
        _pipeline.FlushCompositingBits();
        _pipeline.FlushPaint();
        _pipeline.CompositeFrame(context);
        FlushSemanticsAndNotify();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        EnsureSchedulerSubscription();
        AttachSystemUiOverlayStyleListener();
        AttachMetricSources();
        OnMetricsChanged();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        DetachMetricSources();
        DetachSystemUiOverlayStyleListener();
        RemoveSchedulerSubscription();
        base.OnDetachedFromVisualTree(e);
    }

    protected virtual void OnDrawFrame(TimeSpan timestamp)
    {
    }

    protected virtual void OnMetricsChanged()
    {
    }

    protected MediaQueryData GetMediaQueryData()
    {
        var viewPadding = _insetsManager?.SafeAreaPadding ?? default;
        var viewInsets = ResolveViewInsets(_inputPane?.OccludedRect ?? default, Bounds.Size);
        var size = Bounds.Size;
        var scale = _attachedTopLevel?.RenderScaling ?? 1.0;
        if (!double.IsFinite(scale) || scale <= 0)
        {
            scale = 1.0;
        }

        return new MediaQueryData(
            Size: size,
            DevicePixelRatio: scale,
            Padding: MediaQueryData.ComputePadding(viewPadding, viewInsets),
            ViewInsets: viewInsets,
            ViewPadding: viewPadding);
    }

    protected void ScheduleVisualUpdate()
    {
        Scheduler.ScheduleFrame();
    }

    private void HandleSchedulerDrawFrame(TimeSpan timestamp)
    {
        if (!IsVisible)
        {
            return;
        }

        OnDrawFrame(timestamp);
        _pipeline.FlushLayout(Bounds.Size);
        _pipeline.FlushCompositingBits();

        if (_pipeline.NeedsPaint)
        {
            InvalidateVisual();
        }
        else
        {
            FlushSemanticsAndNotify();
        }
    }

    public bool PerformSemanticsAction(int nodeId, SemanticsActions action)
    {
        return _pipeline.SemanticsOwner.PerformAction(nodeId, action);
    }

    internal void FlushPipelineForTests(Size? viewport = null)
    {
        _pipeline.FlushLayout(viewport ?? Bounds.Size);
        _pipeline.FlushCompositingBits();
        _pipeline.FlushPaint();
        FlushSemanticsAndNotify();
    }

    private void EnsureSchedulerSubscription()
    {
        if (_isSubscribedToScheduler)
        {
            return;
        }

        Scheduler.AddPersistentFrameCallback(HandleSchedulerDrawFrame);
        _isSubscribedToScheduler = true;
    }

    private void RemoveSchedulerSubscription()
    {
        if (!_isSubscribedToScheduler)
        {
            return;
        }

        Scheduler.RemovePersistentFrameCallback(HandleSchedulerDrawFrame);
        _isSubscribedToScheduler = false;
    }

    private void AttachMetricSources()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (ReferenceEquals(topLevel, _attachedTopLevel))
        {
            return;
        }

        DetachMetricSources();

        _attachedTopLevel = topLevel;
        if (_attachedTopLevel == null)
        {
            return;
        }

        _insetsManager = _attachedTopLevel.InsetsManager;
        if (_insetsManager != null)
        {
            _insetsManager.SafeAreaChanged += HandleSafeAreaChanged;
            ApplySystemUiOverlayStyle();
        }

        _inputPane = _attachedTopLevel.InputPane;
        if (_inputPane != null)
        {
            _inputPane.StateChanged += HandleInputPaneStateChanged;
        }
    }

    private void DetachMetricSources()
    {
        if (_insetsManager != null)
        {
            _insetsManager.SafeAreaChanged -= HandleSafeAreaChanged;
        }

        if (_inputPane != null)
        {
            _inputPane.StateChanged -= HandleInputPaneStateChanged;
        }

        _attachedTopLevel = null;
        _insetsManager = null;
        _inputPane = null;
    }

    private void HandleSafeAreaChanged(object? sender, SafeAreaChangedArgs e)
    {
        OnMetricsChanged();
    }

    private void HandleInputPaneStateChanged(object? sender, InputPaneStateEventArgs e)
    {
        OnMetricsChanged();
    }

    private void AttachSystemUiOverlayStyleListener()
    {
        if (_isSubscribedToSystemUiOverlayStyle)
        {
            return;
        }

        _currentSystemUiOverlayStyle = SystemChrome.CurrentSystemUiOverlayStyle;
        SystemChrome.SystemUiOverlayStyleChanged += HandleSystemUiOverlayStyleChanged;
        _isSubscribedToSystemUiOverlayStyle = true;
    }

    private void DetachSystemUiOverlayStyleListener()
    {
        if (!_isSubscribedToSystemUiOverlayStyle)
        {
            return;
        }

        SystemChrome.SystemUiOverlayStyleChanged -= HandleSystemUiOverlayStyleChanged;
        _isSubscribedToSystemUiOverlayStyle = false;
    }

    private void HandleSystemUiOverlayStyleChanged(SystemUiOverlayStyle style)
    {
        _currentSystemUiOverlayStyle = style;
        ApplySystemUiOverlayStyle();
    }

    private void ApplySystemUiOverlayStyle()
    {
        if (_insetsManager == null)
        {
            return;
        }

        var style = _currentSystemUiOverlayStyle;
        var shouldDisplayEdgeToEdge = ShouldDisplayEdgeToEdge(style);
        if (_insetsManager.DisplayEdgeToEdgePreference != shouldDisplayEdgeToEdge)
        {
            _insetsManager.DisplayEdgeToEdgePreference = shouldDisplayEdgeToEdge;
        }

        var fallbackSystemBarColor = style.StatusBarColor ?? style.NavigationBarColor;
        if (fallbackSystemBarColor.HasValue)
        {
            if (_attachedTopLevel != null)
            {
                TopLevel.SetSystemBarColor(_attachedTopLevel, new SolidColorBrush(fallbackSystemBarColor.Value));
            }

            _insetsManager.SystemBarColor = fallbackSystemBarColor.Value;
        }

        var iconBrightness = style.StatusBarIconBrightness ?? style.NavigationBarIconBrightness;
        if (iconBrightness.HasValue)
        {
            var systemBarTheme = iconBrightness.Value == SystemUiIconBrightness.Dark
                ? SystemBarTheme.Light
                : SystemBarTheme.Dark;
            TrySetInsetsManagerSystemBarTheme(_insetsManager, systemBarTheme);
        }

        TryApplyAndroidSystemBarColors(_insetsManager, style.StatusBarColor, style.NavigationBarColor);
    }

    private static bool ShouldDisplayEdgeToEdge(SystemUiOverlayStyle style)
    {
        static bool IsTransparentOrUnset(Color? color) => !color.HasValue || color.Value.A == 0;

        return IsTransparentOrUnset(style.StatusBarColor)
               && IsTransparentOrUnset(style.NavigationBarColor);
    }

    private static void TrySetInsetsManagerSystemBarTheme(IInsetsManager insetsManager, SystemBarTheme theme)
    {
        var property = insetsManager.GetType().GetProperty(
            "SystemBarTheme",
            BindingFlags.Instance | BindingFlags.Public);

        if (property?.CanWrite != true)
        {
            return;
        }

        if (property.PropertyType == typeof(SystemBarTheme))
        {
            property.SetValue(insetsManager, theme);
            return;
        }

        if (property.PropertyType == typeof(SystemBarTheme?))
        {
            property.SetValue(insetsManager, theme);
        }
    }

    private static void TryApplyAndroidSystemBarColors(
        IInsetsManager insetsManager,
        Color? statusBarColor,
        Color? navigationBarColor)
    {
        if (!statusBarColor.HasValue && !navigationBarColor.HasValue)
        {
            return;
        }

        var activityField = insetsManager.GetType().GetField(
            "_activity",
            BindingFlags.Instance | BindingFlags.NonPublic);
        var activity = activityField?.GetValue(insetsManager);
        if (activity == null)
        {
            return;
        }

        var windowProperty = activity.GetType().GetProperty(
            "Window",
            BindingFlags.Instance | BindingFlags.Public);
        var window = windowProperty?.GetValue(activity);
        if (window == null)
        {
            return;
        }

        if (statusBarColor.HasValue)
        {
            TrySetAndroidWindowColor(window, "SetStatusBarColor", statusBarColor.Value);
        }

        if (navigationBarColor.HasValue)
        {
            TrySetAndroidWindowColor(window, "SetNavigationBarColor", navigationBarColor.Value);
        }
    }

    private static void TrySetAndroidWindowColor(object window, string methodName, Color color)
    {
        var method = window.GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .FirstOrDefault(candidate =>
                string.Equals(candidate.Name, methodName, StringComparison.Ordinal)
                && candidate.GetParameters().Length == 1);
        if (method == null)
        {
            return;
        }

        var parameterType = method.GetParameters()[0].ParameterType;
        var argument = CreateAndroidColorArgument(parameterType, color);
        if (argument == null)
        {
            return;
        }

        method.Invoke(window, [argument]);
    }

    private static object? CreateAndroidColorArgument(Type parameterType, Color color)
    {
        var argb = unchecked((int)ToArgb(color));
        if (parameterType == typeof(int))
        {
            return argb;
        }

        var constructor = parameterType.GetConstructor([typeof(int)]);
        if (constructor != null)
        {
            return constructor.Invoke([argb]);
        }

        return null;
    }

    private static uint ToArgb(Color color)
    {
        return ((uint)color.A << 24)
               | ((uint)color.R << 16)
               | ((uint)color.G << 8)
               | color.B;
    }

    private static Thickness ResolveViewInsets(Rect occludedRect, Size hostSize)
    {
        if (occludedRect.Height <= 0 || hostSize.Height <= 0)
        {
            return default;
        }

        var bottomInset = Math.Clamp(occludedRect.Height, 0.0, hostSize.Height);
        return new Thickness(0, 0, 0, bottomInset);
    }

    private void DispatchPointerEvent(PointerEvent @event)
    {
        _gestureBinding.HandlePointerEvent(_root, @event);
    }

    private void FlushSemanticsAndNotify()
    {
        var hadPendingSemantics = _pipeline.PendingSemanticsNodeCount > 0;
        _pipeline.FlushSemantics();
        if (hadPendingSemantics)
        {
            SemanticsUpdated?.Invoke(_pipeline.SemanticsOwner.RootNode);
        }
    }

    private static bool IsPasteShortcut(KeyEventArgs e)
    {
        return (e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Meta))
               && e.Key == Key.V;
    }

    private static bool IsCopyOrCutShortcut(KeyEventArgs e)
    {
        if (!e.KeyModifiers.HasFlag(KeyModifiers.Control) && !e.KeyModifiers.HasFlag(KeyModifiers.Meta))
        {
            return false;
        }

        return e.Key is Key.C or Key.X;
    }

    private async Task HandleSystemPasteShortcutAsync()
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard != null)
        {
            var systemText = await clipboard.TryGetTextAsync();
            if (!string.IsNullOrEmpty(systemText))
            {
                TextClipboard.SetText(systemText);
            }
        }

        var textToPaste = TextClipboard.GetText() ?? string.Empty;
        if (!string.IsNullOrEmpty(textToPaste))
        {
            _ = FrameworkFocusManager.Instance.HandleTextInput(textToPaste);
        }
    }

    private async Task PushFrameworkClipboardToSystemAsync()
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard == null)
        {
            return;
        }

        await clipboard.SetTextAsync(TextClipboard.CurrentText);
    }

    private PointerDownEvent ToPointerDownEvent(PointerPressedEventArgs e)
    {
        return new PointerDownEvent(
            pointer: unchecked((int)e.Pointer.Id),
            kind: ToPointerKind(e.Pointer.Type),
            position: e.GetPosition(this),
            buttons: ToPointerButtons(e.GetCurrentPoint(this).Properties),
            timestampUtc: DateTime.UtcNow);
    }

    private static PointerButtons ToPointerButtons(PointerPointProperties properties)
    {
        var buttons = PointerButtons.None;

        if (properties.IsLeftButtonPressed)
        {
            buttons |= PointerButtons.Primary;
        }

        if (properties.IsRightButtonPressed)
        {
            buttons |= PointerButtons.Secondary;
        }

        if (properties.IsMiddleButtonPressed)
        {
            buttons |= PointerButtons.Middle;
        }

        return buttons;
    }

    private static PointerDeviceKind ToPointerKind(PointerType type)
    {
        return type switch
        {
            PointerType.Mouse => PointerDeviceKind.Mouse,
            PointerType.Touch => PointerDeviceKind.Touch,
            PointerType.Pen => PointerDeviceKind.Stylus,
            _ => PointerDeviceKind.Unknown
        };
    }

    private sealed class FlutterTextInputMethodClient : TextInputMethodClient
    {
        private readonly FlutterHost _host;
        private AvaloniaTextSelection _selection;

        public FlutterTextInputMethodClient(FlutterHost host)
        {
            _host = host;
        }

        public override Visual TextViewVisual => _host;

        public override bool SupportsPreedit => true;

        public override bool SupportsSurroundingText => ResolveTextInputState().HasValue;

        public override string SurroundingText => ResolveTextInputState()?.SurroundingText ?? string.Empty;

        public override Rect CursorRectangle => ResolveTextInputState()?.CursorRectangle ?? default;

        public override AvaloniaTextSelection Selection
        {
            get
            {
                var state = ResolveTextInputState();
                if (!state.HasValue)
                {
                    return _selection;
                }

                _selection = new AvaloniaTextSelection(state.Value.SelectionStart, state.Value.SelectionEnd);
                return _selection;
            }
            set
            {
                _selection = value;
                _ = FrameworkFocusManager.Instance.HandleTextSelectionChanged(value.Start, value.End);
            }
        }

        public override void SetPreeditText(string? preeditText)
        {
            _ = FrameworkFocusManager.Instance.HandleTextCompositionUpdate(preeditText ?? string.Empty);
        }

        public override void SetPreeditText(string? preeditText, int? cursorPos)
        {
            SetPreeditText(preeditText);
        }

        private static FocusTextInputState? ResolveTextInputState()
        {
            return FrameworkFocusManager.Instance.ResolveTextInputState();
        }
    }
}
