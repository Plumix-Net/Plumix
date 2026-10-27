// The web engine's platform answers that need the DOM (engine/src/flutter/lib/web_ui/lib/src/engine/
// platform_dispatcher.dart and safe_browser_api.dart): `HapticFeedback.vibrate`, `SystemNavigator.pop` and
// the media queries behind its accessibility features (platform_dispatcher/media_query_manager.dart).

const forcedColors = '(forced-colors: active)';
const reducedMotion = '(prefers-reduced-motion: reduce)';

// safe_browser_api.dart's vibrate.
export function vibrate(durationMs) {
    if ('vibrate' in navigator) {
        navigator.vibrate(durationMs);
    }
}

// BrowserHistory.exit: the host keeps no history entries of its own, so leaving the application is one
// step back.
export function exit() {
    window.history.go(-1);
}

// The high-contrast and reduced-motion listeners: the callback receives both matches now and on every
// change of either.
export function observeAccessibility(callback) {
    if (window.matchMedia === undefined) {
        callback(false, false);
        return;
    }

    const highContrast = window.matchMedia(forcedColors);
    const reduced = window.matchMedia(reducedMotion);
    const report = () => callback(highContrast.matches, reduced.matches);
    highContrast.addEventListener('change', report);
    reduced.addEventListener('change', report);
    report();
}
