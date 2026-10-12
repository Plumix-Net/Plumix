using Plumix.Foundation;
using Plumix.Painting;

// Dart parity source: flutter/packages/flutter/lib/src/rendering/object.dart

namespace Plumix.Rendering;

/// <summary>Flutter's font-change mixin, composed by render objects through their lifecycle hooks.</summary>
public sealed class RelayoutWhenSystemFontsChangeMixin(RenderObject owner, Action? systemFontsDidChange = null)
{
    private bool _hasPendingSystemFontsDidChangeCallback;

    /// <summary>The default hook; overrides call this before invalidating their font-dependent caches.</summary>
    public void SystemFontsDidChange() => owner.MarkNeedsLayout();

    private void ScheduleSystemFontsUpdate()
    {
        if (_hasPendingSystemFontsDidChangeCallback)
        {
            return;
        }

        _hasPendingSystemFontsDidChangeCallback = true;
        Scheduler.ScheduleFrameCallback(_ =>
        {
            DebugAssert(_hasPendingSystemFontsDidChangeCallback, "A system-font callback must be pending.");
            _hasPendingSystemFontsDidChangeCallback = false;
            DebugAssert(owner.Attached || owner.DebugDisposed,
                $"{owner} is detached during {Scheduler.Phase} but is not disposed.");
            if (owner.Attached)
            {
                if (systemFontsDidChange is null)
                {
                    SystemFontsDidChange();
                }
                else
                {
                    systemFontsDidChange();
                }
            }
        });
    }

    /// <summary>Call after the render object's base attach hook.</summary>
    public void OnAttach()
    {
        DebugAssert(!_hasPendingSystemFontsDidChangeCallback, "A system-font callback must not be pending.");
        PaintingBinding.Instance.SystemFonts.AddListener(ScheduleSystemFontsUpdate);
    }

    /// <summary>Call before the render object's base detach hook.</summary>
    public void OnDetach()
    {
        DebugAssert(!_hasPendingSystemFontsDidChangeCallback, "A system-font callback must not be pending.");
        PaintingBinding.Instance.SystemFonts.RemoveListener(ScheduleSystemFontsUpdate);
    }

    private static void DebugAssert(bool condition, string message)
    {
        if (Constants.KDebugMode && !condition)
        {
            throw new AssertionError(message);
        }
    }
}
