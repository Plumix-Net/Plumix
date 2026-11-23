using CoreAnimation;
using CoreGraphics;
using Foundation;
using UIKit;

namespace Plumix.Impeller.iOS;

// C#-only host infrastructure parity sources:
// flutter/engine/src/flutter/shell/platform/darwin/ios/framework/Source/FlutterKeyboardInsetManager.mm and
// flutter/engine/src/flutter/third_party/spring_animation/spring_animation.mm. The keyboard's frame notifications
// become the view's bottom inset; the change is animated frame by frame along the keyboard's own curve: a hidden view
// is animated with the keyboard's duration and, when UIKit gives that a CASpringAnimation, the same spring is evaluated
// at each vsync (else the presentation layer's position is read). The engine's vsync client is a CADisplayLink here.

/// <summary>
/// Reports the keyboard's overlap with the view as the physical bottom inset, animated like the keyboard.
/// </summary>
internal sealed class IosKeyboardInsetManager : IDisposable
{
    private readonly PlumixViewController _viewController;
    private readonly Action<double> _updateInset;
    private readonly Func<double> _currentInset;
    private double _targetViewInsetBottom;
    private double _originalViewInsetBottom;
    private bool _keyboardAnimationIsShowing;
    private double _keyboardAnimationStartTime;
    private UIView? _keyboardAnimationView;
    private SpringAnimation? _keyboardSpringAnimation;
    private CADisplayLink? _vsyncClient;

    public IosKeyboardInsetManager(
        PlumixViewController viewController, Func<double> currentInset, Action<double> updateInset)
    {
        _viewController = viewController;
        _currentInset = currentInset;
        _updateInset = updateInset;
    }

    private enum KeyboardMode
    {
        Hidden,
        Docked,
        Floating,
    }

    public void Dispose()
    {
        InvalidateVsyncClient();
        _keyboardAnimationView?.RemoveFromSuperview();
    }

    /// <summary>handleKeyboardNotification: for will-show, will-change-frame and will-hide.</summary>
    public void HandleKeyboardNotification(NSNotification notification)
    {
        if (ShouldIgnoreKeyboardNotification(notification))
        {
            return;
        }

        NSDictionary? info = notification.UserInfo;
        CGRect beginKeyboardFrame = RectOf(info, UIKeyboard.FrameBeginUserInfoKey);
        CGRect keyboardFrame = RectOf(info, UIKeyboard.FrameEndUserInfoKey);
        KeyboardMode keyboardMode = CalculateKeyboardAttachMode(notification);
        double calculatedInset = CalculateKeyboardInset(keyboardFrame, keyboardMode);
        double duration = (info?[UIKeyboard.AnimationDurationUserInfoKey] as NSNumber)?.DoubleValue ?? 0;

        // The keyboard vanished without animation (e.g. the app went to the background).
        if (keyboardMode == KeyboardMode.Hidden && calculatedInset == 0.0 && duration == 0.0)
        {
            HideKeyboardImmediately();
            return;
        }

        if (_targetViewInsetBottom == calculatedInset)
        {
            return;
        }

        _targetViewInsetBottom = calculatedInset;
        // A show during a show (or a hide during a hide) retargets the running animation.
        bool keyboardWillShow = beginKeyboardFrame.Y > keyboardFrame.Y;
        bool keyboardAnimationIsCompounding =
            _keyboardAnimationIsShowing == keyboardWillShow && _vsyncClient is not null;
        _keyboardAnimationIsShowing = keyboardWillShow;
        if (!keyboardAnimationIsCompounding)
        {
            StartKeyboardAnimation(duration);
        }
        else if (_keyboardSpringAnimation is not null)
        {
            _keyboardSpringAnimation.ToValue = _targetViewInsetBottom;
        }
    }

    private bool ShouldIgnoreKeyboardNotification(NSNotification notification)
    {
        // Hiding always matters: when the keyboard goes from docked to floating its frame stays on screen.
        if (notification.Name == UIKeyboard.WillHideNotification)
        {
            return false;
        }

        CGRect keyboardFrame = RectOf(notification.UserInfo, UIKeyboard.FrameEndUserInfoKey);
        // A zero frame on will-change-frame comes with the shortcuts bar being dragged; will-show handles it.
        if (notification.Name == UIKeyboard.WillChangeFrameNotification && keyboardFrame == CGRect.Empty)
        {
            return true;
        }

        if (keyboardFrame.IsEmpty)
        {
            return false;
        }

        // A keyboard for another app (iPad multitasking) or another view controller is not this view's.
        return notification.UserInfo?[UIKeyboard.IsLocalUserInfoKey] is NSNumber isLocal && !isLocal.BoolValue;
    }

    private KeyboardMode CalculateKeyboardAttachMode(NSNotification notification)
    {
        CGRect keyboardFrame = RectOf(notification.UserInfo, UIKeyboard.FrameEndUserInfoKey);
        if (notification.Name == UIKeyboard.WillHideNotification)
        {
            return KeyboardMode.Hidden;
        }

        // The minimized shortcuts bar reports a zero frame.
        if (keyboardFrame == CGRect.Empty)
        {
            return KeyboardMode.Floating;
        }

        if (keyboardFrame.IsEmpty)
        {
            return KeyboardMode.Hidden;
        }

        CGRect screenRect = Screen.Bounds;
        CGRect adjustedKeyboardFrame = keyboardFrame;
        adjustedKeyboardFrame.Y += (nfloat)CalculateMultitaskingAdjustment(screenRect, keyboardFrame);
        CGRect intersection = CGRect.Intersect(adjustedKeyboardFrame, screenRect);
        if (Math.Round(intersection.Height) > 0 && intersection.Width > 0)
        {
            // A keyboard that ends above the screen's bottom floats; the docked one reaches it.
            return Math.Round(adjustedKeyboardFrame.GetMaxY()) < screenRect.Height
                ? KeyboardMode.Floating
                : KeyboardMode.Docked;
        }

        return KeyboardMode.Hidden;
    }

    // calculateMultitaskingAdjustment: in Slide Over the keyboard frame leaves out the space below the app.
    private double CalculateMultitaskingAdjustment(CGRect screenRect, CGRect keyboardFrame)
    {
        if (_viewController.ViewIfLoaded is not { } view || !IsPadInSlideOverOrStageManagerMode(view))
        {
            return 0;
        }

        if (screenRect.Height == keyboardFrame.GetMaxY())
        {
            return 0;
        }

        double offset = screenRect.Height - ConvertViewRectToScreen(view, view.Bounds).GetMaxY();
        return offset > 0 ? offset : 0;
    }

    private double CalculateKeyboardInset(CGRect keyboardFrame, KeyboardMode keyboardMode)
    {
        if (keyboardMode != KeyboardMode.Docked || _viewController.ViewIfLoaded is not { } view)
        {
            return 0;
        }

        CGRect intersection = CGRect.Intersect(keyboardFrame, ConvertViewRectToScreen(view, view.Bounds));
        return intersection.Height * Screen.Scale;
    }

    private void StartKeyboardAnimation(double duration)
    {
        if (_viewController.ViewIfLoaded is not { } view)
        {
            return;
        }

        _keyboardAnimationView ??= new UIView { Hidden = true };
        if (_keyboardAnimationView.Superview != view)
        {
            view.AddSubview(_keyboardAnimationView);
        }

        _keyboardAnimationView.Layer.RemoveAllAnimations();
        double currentInset = _currentInset();
        _keyboardAnimationView.Frame = new CGRect(0, currentInset, 0, 0);
        _keyboardAnimationStartTime = CAAnimation.CurrentMediaTime();
        _originalViewInsetBottom = currentInset;

        InvalidateVsyncClient();
        CADisplayLink currentVsyncClient = SetUpVsyncClient();
        UIView.Animate(
            duration,
            () =>
            {
                _keyboardAnimationView.Frame = new CGRect(0, _targetViewInsetBottom, 0, 0);
                _keyboardAnimationView.LayoutIfNeeded();
                SetUpKeyboardSpringAnimationIfNeeded(_keyboardAnimationView.Layer.AnimationForKey("position"));
            },
            () =>
            {
                if (_vsyncClient == currentVsyncClient)
                {
                    InvalidateVsyncClient();
                    _keyboardAnimationView?.RemoveFromSuperview();
                    EnsureViewportMetricsIsCorrect();
                }
            });
    }

    private void HideKeyboardImmediately()
    {
        InvalidateVsyncClient();
        if (_keyboardAnimationView is not null)
        {
            _keyboardAnimationView.Layer.RemoveAllAnimations();
            _keyboardAnimationView.RemoveFromSuperview();
            _keyboardAnimationView = null;
        }

        _keyboardSpringAnimation = null;
        _targetViewInsetBottom = 0.0;
        EnsureViewportMetricsIsCorrect();
    }

    private void SetUpKeyboardSpringAnimationIfNeeded(CAAnimation? keyboardAnimation)
    {
        _keyboardSpringAnimation = keyboardAnimation is CASpringAnimation spring
            ? new SpringAnimation(
                spring.Stiffness, spring.Damping, spring.Mass, spring.InitialVelocity, _originalViewInsetBottom,
                _targetViewInsetBottom)
            : null;
    }

    // handleKeyboardAnimationCallbackWithTargetTime: the inset at the frame's projected target time.
    private void HandleKeyboardAnimationCallback(double targetTime)
    {
        if (_viewController.ViewIfLoaded is not { } view || _keyboardAnimationView is null || _vsyncClient is null)
        {
            return;
        }

        if (_keyboardAnimationView.Superview != view)
        {
            view.AddSubview(_keyboardAnimationView);
        }

        double currentInset = _keyboardSpringAnimation is { } spring
            ? spring.CurveFunction(targetTime - _keyboardAnimationStartTime)
            : _keyboardAnimationView.Layer.PresentationLayer?.Frame.Y ?? 0;
        _updateInset(currentInset);
    }

    private CADisplayLink SetUpVsyncClient()
    {
        CADisplayLink displayLink = CADisplayLink.Create(() =>
        {
            if (_vsyncClient is not { } link)
            {
                return;
            }

            // The engine projects one frame ahead: the inset is for the frame after the one being prepared.
            double frameInterval = link.TargetTimestamp - link.Timestamp;
            HandleKeyboardAnimationCallback(link.TargetTimestamp + frameInterval);
        });
        displayLink.AddToRunLoop(NSRunLoop.Main, NSRunLoopMode.Common);
        _vsyncClient = displayLink;
        return displayLink;
    }

    private void InvalidateVsyncClient()
    {
        _vsyncClient?.Invalidate();
        _vsyncClient = null;
    }

    private void EnsureViewportMetricsIsCorrect() => _updateInset(_targetViewInsetBottom);

    private UIScreen Screen => _viewController.ViewIfLoaded?.Window?.WindowScene?.Screen ?? UIScreen.MainScreen;

    private static bool IsPadInSlideOverOrStageManagerMode(UIView view) =>
        view.TraitCollection.UserInterfaceIdiom == UIUserInterfaceIdiom.Pad
        && view.TraitCollection.HorizontalSizeClass == UIUserInterfaceSizeClass.Compact
        && view.TraitCollection.VerticalSizeClass == UIUserInterfaceSizeClass.Regular;

    private CGRect ConvertViewRectToScreen(UIView view, CGRect rect) =>
        view.ConvertRectToCoordinateSpace(rect, Screen.CoordinateSpace);

    private static CGRect RectOf(NSDictionary? info, NSString key) =>
        (info?[key] as NSValue)?.CGRectValue ?? CGRect.Empty;

    /// <summary>spring_animation.mm: the closed-form damped spring CASpringAnimation follows.</summary>
    private sealed class SpringAnimation
    {
        private readonly double _zeta;
        private readonly double _omega0;
        private readonly double _omega1;
        private readonly double _v0;
        private readonly double _fromValue;

        public SpringAnimation(
            double stiffness, double damping, double mass, double initialVelocity, double fromValue, double toValue)
        {
            _fromValue = fromValue;
            ToValue = toValue;
            _zeta = damping / (2 * Math.Sqrt(stiffness * mass));
            _omega0 = Math.Sqrt(stiffness / mass);
            _omega1 = _omega0 * Math.Sqrt(1.0 - (_zeta * _zeta));
            _v0 = -initialVelocity;
        }

        public double ToValue { get; set; }

        public double CurveFunction(double t)
        {
            double x0 = ToValue - _fromValue;
            if (_zeta < 1)
            {
                double envelope = Math.Exp(-_zeta * _omega0 * t);
                return ToValue - (envelope * ((((_v0 + (_zeta * _omega0 * x0)) / _omega1) * Math.Sin(_omega1 * t))
                                              + (x0 * Math.Cos(_omega1 * t))));
            }

            double criticalEnvelope = Math.Exp(-_omega0 * t);
            return ToValue - (criticalEnvelope * (x0 + ((_v0 + (_omega0 * x0)) * t)));
        }
    }
}
