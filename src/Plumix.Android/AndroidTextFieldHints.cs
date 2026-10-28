using Android.OS;
using Android.Views;
using Android.Views.Accessibility;
using AndroidX.Core.View;
using AndroidX.Core.View.Accessibility;

// C#-only host infrastructure parity source:
// flutter/engine/src/flutter/shell/platform/android/io/flutter/view/BaseRoleConfigurator.java
// (configureLabelAndValue: a text field's node text is its value, its hint text is "label, hint").

namespace Plumix.Android;

/// <summary>
/// Wraps the accessibility delegate Avalonia installs on its <c>AvaloniaView</c> so that every text field node
/// carries its name as hint text, as the engine's Android bridge sets it.
/// </summary>
/// <remarks>
/// Avalonia's <c>ValueNodeInfoProvider</c> makes the value a text field's node text, and its access helper then
/// skips the peer name; the semantics peer therefore hands the engine's text field hint over as help text, which
/// Avalonia writes to the content description. Read as a content description it would replace the value for
/// TalkBack, so the wrapped provider moves it to the hint text of every <c>EditText</c> node it creates.
/// </remarks>
internal sealed class AndroidTextFieldHints : AccessibilityDelegateCompat
{
    private const string EditTextClassName = "android.widget.EditText";

    private readonly AccessibilityDelegateCompat _inner;
    private NodeProvider? _provider;
    private AccessibilityNodeProviderCompat? _innerProvider;

    private AndroidTextFieldHints(AccessibilityDelegateCompat inner) => _inner = inner;

    /// <summary>Wraps the accessibility delegate of <paramref name="view"/>, once.</summary>
    public static void Install(View view)
    {
        AccessibilityDelegateCompat? current = ViewCompat.GetAccessibilityDelegate(view);
        if (current is null or AndroidTextFieldHints)
        {
            return;
        }

        ViewCompat.SetAccessibilityDelegate(view, new AndroidTextFieldHints(current));
    }

    internal static void MoveDescriptionToHint(AccessibilityNodeInfoCompat? info)
    {
        if (info is null || info.ClassName != EditTextClassName || string.IsNullOrEmpty(info.ContentDescription))
        {
            return;
        }

        info.HintText = info.ContentDescription;
        info.ContentDescription = null;
    }

    public override AccessibilityNodeProviderCompat? GetAccessibilityNodeProvider(View? host)
    {
        AccessibilityNodeProviderCompat? inner = _inner.GetAccessibilityNodeProvider(host);
        if (inner is null)
        {
            return null;
        }

        if (!ReferenceEquals(inner, _innerProvider))
        {
            _innerProvider = inner;
            _provider = new NodeProvider(inner);
        }

        return _provider;
    }

    public override bool DispatchPopulateAccessibilityEvent(View? host, AccessibilityEvent? e) =>
        _inner.DispatchPopulateAccessibilityEvent(host, e);

    public override void OnInitializeAccessibilityEvent(View? host, AccessibilityEvent? e) =>
        _inner.OnInitializeAccessibilityEvent(host, e);

    public override void OnInitializeAccessibilityNodeInfo(View? host, AccessibilityNodeInfoCompat? info) =>
        _inner.OnInitializeAccessibilityNodeInfo(host, info);

    public override void OnPopulateAccessibilityEvent(View? host, AccessibilityEvent? e) =>
        _inner.OnPopulateAccessibilityEvent(host, e);

    public override bool OnRequestSendAccessibilityEvent(ViewGroup? host, View? child, AccessibilityEvent? e) =>
        _inner.OnRequestSendAccessibilityEvent(host, child, e);

    public override bool PerformAccessibilityAction(View? host, int action, Bundle? args) =>
        _inner.PerformAccessibilityAction(host, action, args);

    public override void SendAccessibilityEvent(View? host, int eventType) =>
        _inner.SendAccessibilityEvent(host, eventType);

    public override void SendAccessibilityEventUnchecked(View? host, AccessibilityEvent? e) =>
        _inner.SendAccessibilityEventUnchecked(host, e);

    private sealed class NodeProvider(AccessibilityNodeProviderCompat inner) : AccessibilityNodeProviderCompat
    {
        public override AccessibilityNodeInfoCompat? CreateAccessibilityNodeInfo(int virtualViewId)
        {
            AccessibilityNodeInfoCompat? info = inner.CreateAccessibilityNodeInfo(virtualViewId);
            MoveDescriptionToHint(info);
            return info;
        }

        public override AccessibilityNodeInfoCompat? FindFocus(int focus)
        {
            AccessibilityNodeInfoCompat? info = inner.FindFocus(focus);
            MoveDescriptionToHint(info);
            return info;
        }

        public override IList<AccessibilityNodeInfoCompat>? FindAccessibilityNodeInfosByText(
            string? text,
            int virtualViewId) => inner.FindAccessibilityNodeInfosByText(text, virtualViewId);

        public override bool PerformAction(int virtualViewId, int action, Bundle? arguments) =>
            inner.PerformAction(virtualViewId, action, arguments);

        public override void AddExtraDataToAccessibilityNodeInfo(
            int virtualViewId,
            AccessibilityNodeInfoCompat? info,
            string? extraDataKey,
            Bundle? arguments) =>
            inner.AddExtraDataToAccessibilityNodeInfo(virtualViewId, info, extraDataKey, arguments);
    }
}
