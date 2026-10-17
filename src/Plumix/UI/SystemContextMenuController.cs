using System.Runtime.CompilerServices;
using Avalonia;
using Plumix.Foundation;

namespace Plumix.UI;

// Dart parity source: flutter/packages/flutter/lib/src/services/text_input.dart

/// <summary>
/// An interface to receive calls related to the system context menu from the engine.
/// </summary>
/// <remarks>
/// Dart's <c>mixin SystemContextMenuClient</c> (services/binding.dart). Currently this is only
/// supported on iOS 16.0 and later.
/// </remarks>
public interface ISystemContextMenuClient
{
    /// <summary>Handles the system hiding a context menu.</summary>
    /// <remarks>
    /// Called only on the single active instance registered with
    /// <see cref="ServicesBinding.SystemContextMenuClient"/>, and not called when Flutter itself hid
    /// the menu.
    /// </remarks>
    void HandleSystemHide();

    /// <summary>Called when a custom context menu action is triggered by the platform.</summary>
    void HandleCustomContextMenuAction(string actionId);
}

/// <summary>
/// Allows access to the system context menu.
/// </summary>
/// <remarks>
/// <para>
/// Dart's <c>SystemContextMenuController</c>. The context menu is the menu that appears, for example,
/// when doing text selection. Flutter typically draws this menu itself, but this class deals with the
/// platform-rendered context menu instead. Only one instance can be visible at a time; calling
/// <see cref="ShowWithItems"/> while another instance is visible asserts.
/// </para>
/// <para>
/// Currently this system context menu is bound to text input. Using this without an active
/// <see cref="TextInputConnection"/> is a no-op that asserts.
/// </para>
/// </remarks>
public class SystemContextMenuController : ISystemContextMenuClient, IDiagnosticable
{
    private static readonly MethodChannel Channel = SystemChannels.Platform;

    /// <summary>The instance that is currently showing the menu, process-wide.</summary>
    private static SystemContextMenuController? _lastShown;

    private readonly Dictionary<string, Action> _customActionCallbacks = [];

    /// <summary>The target rect that was last given to <see cref="ShowWithItems"/>.</summary>
    private Rect? _lastTargetRect;

    /// <summary>The items that were last given to <see cref="ShowWithItems"/>.</summary>
    private IReadOnlyList<IOSSystemContextMenuItemData>? _lastItems;

    /// <summary>
    /// True when the instance most recently shown has been hidden by the system.
    /// </summary>
    private bool _hiddenBySystem;

    private bool _isDisposed;

    /// <summary>Creates an instance of <see cref="SystemContextMenuController"/>.</summary>
    /// <remarks>
    /// Not shown until <see cref="ShowWithItems"/> is called. Creating a controller registers it as
    /// <see cref="ServicesBinding.SystemContextMenuClient"/>, replacing any client registered before.
    /// </remarks>
    public SystemContextMenuController(Action? onSystemHide = null)
    {
        OnSystemHide = onSystemHide;
        ServicesBinding.SystemContextMenuClient = this;
    }

    /// <summary>Called when the system has hidden the context menu.</summary>
    /// <remarks>
    /// For example, tapping outside of the context menu typically causes the system to hide it. Not
    /// called when <see cref="Hide"/> hides the menu.
    /// </remarks>
    public Action? OnSystemHide { get; }

    /// <summary>Whether this instance is currently visible.</summary>
    /// <remarks>
    /// Dart's <c>@visibleForTesting isVisible</c>: only one instance can be visible at a time, and
    /// this does not account for the system hiding the menu while another instance is shown.
    /// </remarks>
    public bool IsVisible => ReferenceEquals(this, _lastShown) && !_hiddenBySystem;

    /// <inheritdoc />
    public void HandleSystemHide()
    {
        AssertNotDisposed();
        if (Constants.KDebugMode && !IsVisible)
        {
            throw new AssertionError();
        }

        // The system shouldn't be able to hide the menu when it's not visible, but in release mode
        // the instance may already be disposed.
        if (_isDisposed || !IsVisible)
        {
            return;
        }

        if (ReferenceEquals(_lastShown, this))
        {
            _lastShown = null;
        }

        _hiddenBySystem = true;
        _customActionCallbacks.Clear();
        OnSystemHide?.Invoke();
    }

    /// <inheritdoc />
    public void HandleCustomContextMenuAction(string callbackId)
    {
        ArgumentNullException.ThrowIfNull(callbackId);
        _customActionCallbacks.TryGetValue(callbackId, out Action? callback);
        if (Constants.KDebugMode && callback is null)
        {
            throw new AssertionError(
                $"Custom action callback not found for id: {callbackId}. This may indicate that the "
                + "menu item was not properly registered.");
        }

        callback?.Invoke();
    }

    /// <summary>Shows the system context menu anchored on the given rectangle.</summary>
    /// <remarks>
    /// Dart's deprecated <c>show</c> ("Use showWithItems instead. This feature was deprecated after
    /// v3.29.0-0.3.pre."): the platform shows its default items.
    /// </remarks>
    [Obsolete("Use ShowWithItems instead. This feature was deprecated after v3.29.0-0.3.pre.")]
    public Task Show(Rect targetRect)
    {
        AssertNotDisposed();
        AssertHasConnection();

        // Don't show the same thing that's already being shown.
        if (_lastShown is not null && _lastShown.IsVisible && _lastShown._lastTargetRect == targetRect)
        {
            return Task.CompletedTask;
        }

        AssertNoOtherVisible();
        ServicesBinding.SystemContextMenuClient = this;
        _lastTargetRect = targetRect;
        _lastShown = this;
        _hiddenBySystem = false;
        return Channel.InvokeMethod<object>(
            "ContextMenu.showSystemContextMenu",
            new Dictionary<string, object?> { ["targetRect"] = TargetRectJson(targetRect) });
    }

    /// <summary>
    /// Shows the system context menu anchored on the given rectangle containing the given buttons.
    /// </summary>
    /// <remarks>
    /// The menu is anchored on <paramref name="targetRect"/>, in global coordinates, and positioned
    /// near it by the platform. <paramref name="items"/> must not be empty. Showing the same rect and
    /// items that are already visible does nothing; showing while another instance is visible
    /// asserts. Like Dart's method this is not <c>async</c>, so its assertions throw synchronously.
    /// </remarks>
    public Task ShowWithItems(Rect targetRect, IReadOnlyList<IOSSystemContextMenuItemData> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        AssertNotDisposed();
        if (Constants.KDebugMode && items.Count == 0)
        {
            throw new AssertionError();
        }

        AssertHasConnection();

        // Don't show the same thing that's already being shown.
        if (_lastShown is not null
            && _lastShown.IsVisible
            && _lastShown._lastTargetRect == targetRect
            && ListEquals(_lastShown._lastItems, items))
        {
            return Task.CompletedTask;
        }

        AssertNoOtherVisible();
        ServicesBinding.SystemContextMenuClient = this;

        // Clear and register new callbacks.
        _customActionCallbacks.Clear();
        foreach (IOSSystemContextMenuItemData item in items)
        {
            if (item is IOSSystemContextMenuItemDataCustom custom)
            {
                if (Constants.KDebugMode
                    && _customActionCallbacks.TryGetValue(custom.CallbackId, out Action? existing)
                    && !Equals(existing, custom.OnPressed))
                {
                    throw new AssertionError(
                        $"Duplicate callback ID \"{custom.CallbackId}\" with different callbacks. Each "
                        + "custom menu item must have a unique ID or the same callback.");
                }

                _customActionCallbacks[custom.CallbackId] = custom.OnPressed;
            }
        }

        List<Dictionary<string, object?>> itemsJson = items.Select(item => item.Json).ToList();
        _lastTargetRect = targetRect;
        _lastItems = items;
        _lastShown = this;
        _hiddenBySystem = false;
        return Channel.InvokeMethod<object>(
            "ContextMenu.showSystemContextMenu",
            new Dictionary<string, object?>
            {
                ["targetRect"] = TargetRectJson(targetRect),
                ["items"] = itemsJson,
            });
    }

    /// <summary>Hides this system context menu.</summary>
    /// <remarks>
    /// Does nothing when this instance is not the one being shown (never shown, already hidden,
    /// hidden by the system, or replaced by another instance). Dart's method is <c>async</c>, so a
    /// failed assertion surfaces through the returned task.
    /// </remarks>
    public Task Hide()
    {
        try
        {
            AssertNotDisposed();
        }
        catch (AssertionError error)
        {
            return Task.FromException(error);
        }

        // This check prevents a given instance from accidentally hiding some other instance, since
        // only one can be visible at a time.
        if (!ReferenceEquals(this, _lastShown))
        {
            return Task.CompletedTask;
        }

        _lastShown = null;
        ServicesBinding.SystemContextMenuClient = null;

        // Clear callbacks when hiding the menu.
        _customActionCallbacks.Clear();

        // This may be called unnecessarily in the case where the user has already hidden the menu
        // (for example by tapping the screen).
        return Channel.InvokeMethod<object>("ContextMenu.hideSystemContextMenu");
    }

    /// <summary>Dart's <c>SystemContextMenuController.debugFillProperties</c>.</summary>
    public void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        properties.Add(new DiagnosticsProperty<bool>("isVisible", IsVisible));
        properties.Add(new FlagProperty(
            "onSystemHide",
            value: OnSystemHide is not null,
            ifTrue: "callback set",
            ifFalse: "callback null",
            showName: true));
        properties.Add(new DiagnosticsProperty<bool>("_hiddenBySystem", _hiddenBySystem));
        properties.Add(new DiagnosticsProperty<bool>("_isDisposed", _isDisposed));
    }

    /// <summary>Dart's <c>Diagnosticable.toString</c>.</summary>
    public override string ToString() => IOSSystemContextMenuItemData.DiagnosticableToString(this);

    /// <summary>Used to release resources when this instance will never be used again.</summary>
    public void Dispose()
    {
        AssertNotDisposed();
        _ = Hide();
        _isDisposed = true;
    }

    /// <summary>Forgets the process-wide shown instance between tests.</summary>
    internal static void ResetForTests() => _lastShown = null;

    private void AssertNotDisposed()
    {
        if (Constants.KDebugMode && _isDisposed)
        {
            throw new AssertionError();
        }
    }

    private static void AssertHasConnection()
    {
        if (Constants.KDebugMode && TextInput.CurrentConnection is null)
        {
            throw new AssertionError(
                "Currently, the system context menu can only be shown for an active text input connection");
        }
    }

    private void AssertNoOtherVisible()
    {
        if (Constants.KDebugMode
            && !(_lastShown is null || ReferenceEquals(_lastShown, this) || !_lastShown.IsVisible))
        {
            throw new AssertionError("Attempted to show while another instance was still visible.");
        }
    }

    private static Dictionary<string, object?> TargetRectJson(Rect targetRect) => new()
    {
        ["x"] = targetRect.Left,
        ["y"] = targetRect.Top,
        ["width"] = targetRect.Width,
        ["height"] = targetRect.Height,
    };

    /// <summary>Dart's <c>listEquals</c> over the item data.</summary>
    private static bool ListEquals(
        IReadOnlyList<IOSSystemContextMenuItemData>? a,
        IReadOnlyList<IOSSystemContextMenuItemData>? b)
    {
        if (a is null)
        {
            return b is null;
        }

        if (b is null || a.Count != b.Count)
        {
            return false;
        }

        if (ReferenceEquals(a, b))
        {
            return true;
        }

        for (int index = 0; index < a.Count; index += 1)
        {
            if (!a[index].Equals(b[index]))
            {
                return false;
            }
        }

        return true;
    }
}

/// <summary>
/// Describes a context menu button that will be rendered in the system context menu.
/// </summary>
/// <remarks>
/// Dart's <c>sealed class IOSSystemContextMenuItemData</c>: the data sent over the platform channel.
/// Only the subclasses in this file exist; C# closes the hierarchy with an internal constructor.
/// </remarks>
public abstract class IOSSystemContextMenuItemData
{
    private protected IOSSystemContextMenuItemData()
    {
    }

    /// <summary>The text to display to the user, or null for a button the platform labels itself.</summary>
    public virtual string? Title => null;

    /// <summary>The type string used when serialized to JSON.</summary>
    private protected abstract string JsonType { get; }

    /// <summary>
    /// Returns json for use in method channel calls, specifically
    /// <c>ContextMenu.showSystemContextMenu</c>.
    /// </summary>
    /// <remarks>
    /// The title key is left out when <see cref="Title"/> is null, as Dart's <c>?title</c> element.
    /// </remarks>
    internal virtual Dictionary<string, object?> Json
    {
        get
        {
            var json = new Dictionary<string, object?> { ["callbackId"] = GetHashCode() };
            if (Title is { } title)
            {
                json["title"] = title;
            }

            json["type"] = JsonType;
            return json;
        }
    }

    public override int GetHashCode() => Title?.GetHashCode() ?? 0;

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj is null || obj.GetType() != GetType())
        {
            return false;
        }

        return obj is IOSSystemContextMenuItemData other && other.Title == Title;
    }

    /// <summary>Dart's <c>Diagnosticable.toString</c> for the classes that mix it in.</summary>
    internal static string DiagnosticableToString(IDiagnosticable self)
    {
        return Constants.KDebugMode
            ? self.ToDiagnosticsNode(style: DiagnosticsTreeStyle.SingleLine).ToString(null, DiagnosticLevel.Info)
            : self.ToStringShort();
    }

    /// <summary>
    /// Dart's <c>Object.hash(title, onPressed)</c>, consistent with delegate equality: equal
    /// delegates share their method and target.
    /// </summary>
    internal static int HashTitleAndCallback(string title, Action onPressed)
    {
        int target = onPressed.Target is { } instance ? RuntimeHelpers.GetHashCode(instance) : 0;
        return HashCode.Combine(title, onPressed.Method, target);
    }
}

/// <summary>A <see cref="IOSSystemContextMenuItemData"/> for the system's built-in copy button.</summary>
/// <remarks>The title and action are both handled by the platform.</remarks>
public sealed class IOSSystemContextMenuItemDataCopy : IOSSystemContextMenuItemData
{
    private protected override string JsonType => "copy";
}

/// <summary>A <see cref="IOSSystemContextMenuItemData"/> for the system's built-in cut button.</summary>
/// <remarks>The title and action are both handled by the platform.</remarks>
public sealed class IOSSystemContextMenuItemDataCut : IOSSystemContextMenuItemData
{
    private protected override string JsonType => "cut";
}

/// <summary>A <see cref="IOSSystemContextMenuItemData"/> for the system's built-in paste button.</summary>
/// <remarks>The title and action are both handled by the platform.</remarks>
public sealed class IOSSystemContextMenuItemDataPaste : IOSSystemContextMenuItemData
{
    private protected override string JsonType => "paste";
}

/// <summary>A <see cref="IOSSystemContextMenuItemData"/> for the system's built-in select all button.</summary>
/// <remarks>The title and action are both handled by the platform.</remarks>
public sealed class IOSSystemContextMenuItemDataSelectAll : IOSSystemContextMenuItemData
{
    private protected override string JsonType => "selectAll";
}

/// <summary>A <see cref="IOSSystemContextMenuItemData"/> for the system's built-in look up button.</summary>
/// <remarks>Must specify a <see cref="Title"/>, typically <c>WidgetsLocalizations.LookUpButtonLabel</c>;
/// the action is handled by the platform.</remarks>
public sealed class IOSSystemContextMenuItemDataLookUp : IOSSystemContextMenuItemData, IDiagnosticable
{
    /// <summary>Creates an instance of <see cref="IOSSystemContextMenuItemDataLookUp"/>.</summary>
    public IOSSystemContextMenuItemDataLookUp(string title)
    {
        ArgumentNullException.ThrowIfNull(title);
        Title = title;
    }

    /// <inheritdoc />
    public override string Title { get; }

    private protected override string JsonType => "lookUp";

    /// <summary>Dart's <c>debugFillProperties</c>.</summary>
    public void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        properties.Add(new StringProperty("title", Title));
    }

    public override string ToString() => DiagnosticableToString(this);
}

/// <summary>A <see cref="IOSSystemContextMenuItemData"/> for the system's built-in search web button.</summary>
/// <remarks>Must specify a <see cref="Title"/>, typically <c>WidgetsLocalizations.SearchWebButtonLabel</c>;
/// the action is handled by the platform.</remarks>
public sealed class IOSSystemContextMenuItemDataSearchWeb : IOSSystemContextMenuItemData, IDiagnosticable
{
    /// <summary>Creates an instance of <see cref="IOSSystemContextMenuItemDataSearchWeb"/>.</summary>
    public IOSSystemContextMenuItemDataSearchWeb(string title)
    {
        ArgumentNullException.ThrowIfNull(title);
        Title = title;
    }

    /// <inheritdoc />
    public override string Title { get; }

    private protected override string JsonType => "searchWeb";

    /// <summary>Dart's <c>debugFillProperties</c>.</summary>
    public void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        properties.Add(new StringProperty("title", Title));
    }

    public override string ToString() => DiagnosticableToString(this);
}

/// <summary>A <see cref="IOSSystemContextMenuItemData"/> for the system's built-in share button.</summary>
/// <remarks>Must specify a <see cref="Title"/>, typically <c>WidgetsLocalizations.ShareButtonLabel</c>;
/// the action is handled by the platform.</remarks>
public sealed class IOSSystemContextMenuItemDataShare : IOSSystemContextMenuItemData, IDiagnosticable
{
    /// <summary>Creates an instance of <see cref="IOSSystemContextMenuItemDataShare"/>.</summary>
    public IOSSystemContextMenuItemDataShare(string title)
    {
        ArgumentNullException.ThrowIfNull(title);
        Title = title;
    }

    /// <inheritdoc />
    public override string Title { get; }

    private protected override string JsonType => "share";

    /// <summary>Dart's <c>debugFillProperties</c>.</summary>
    public void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        properties.Add(new StringProperty("title", Title));
    }

    public override string ToString() => DiagnosticableToString(this);
}

/// <summary>
/// A <see cref="IOSSystemContextMenuItemData"/> for the system's built-in Live Text (OCR) button.
/// </summary>
/// <remarks>The title and action are both handled by the platform.</remarks>
public sealed class IOSSystemContextMenuItemDataLiveText : IOSSystemContextMenuItemData
{
    private protected override string JsonType => "captureTextFromCamera";
}

/// <summary>
/// A <see cref="IOSSystemContextMenuItemData"/> for custom menu items with a title and callback.
/// </summary>
/// <remarks>
/// The platform sends <see cref="CallbackId"/> back in <c>ContextMenu.onPerformCustomAction</c> when
/// the item is tapped.
/// </remarks>
public sealed class IOSSystemContextMenuItemDataCustom : IOSSystemContextMenuItemData, IDiagnosticable
{
    /// <summary>Creates an instance of <see cref="IOSSystemContextMenuItemDataCustom"/>.</summary>
    public IOSSystemContextMenuItemDataCustom(string title, Action onPressed)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(onPressed);
        Title = title;
        OnPressed = onPressed;
    }

    /// <summary>The text to display to the user.</summary>
    public override string Title { get; }

    /// <summary>The callback to be called when the menu item is pressed.</summary>
    public Action OnPressed { get; }

    /// <summary>The unique identifier for this menu item.</summary>
    public string CallbackId => GetHashCode().ToString(System.Globalization.CultureInfo.InvariantCulture);

    private protected override string JsonType => "custom";

    internal override Dictionary<string, object?> Json => new()
    {
        ["id"] = CallbackId,
        ["title"] = Title,
        ["type"] = JsonType,
    };

    /// <summary>Dart's <c>debugFillProperties</c>.</summary>
    public void DebugFillProperties(DiagnosticPropertiesBuilder properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        properties.Add(new StringProperty("title", Title));
        properties.Add(new StringProperty("callbackId", CallbackId));
        properties.Add(new DiagnosticsProperty<Action>("onPressed", OnPressed));
    }

    public override int GetHashCode() => HashTitleAndCallback(Title, OnPressed);

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj)
               || (obj is IOSSystemContextMenuItemDataCustom other
                   && other.Title == Title
                   && Equals(other.OnPressed, OnPressed));
    }

    public override string ToString() => DiagnosticableToString(this);
}
