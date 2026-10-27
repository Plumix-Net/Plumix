// A port of the web engine's typography observer (engine/src/flutter/lib/web_ui/lib/src/engine/
// platform_dispatcher.dart, `_addTypographySettingsObserver`): an off-screen paragraph whose text spacing
// a user style sheet or browser extension may override, measured whenever it resizes.

const spacingDefault = 100.0;
const defaultRootFontSize = 16.0;
const typographyPrecisionErrorTolerance = 1e-4;

let measurementElement = null;
let observer = null;
let defaultLineHeightFactor = 0;

// safe_browser_api.dart's parseFloat: JavaScript's parseFloat, with NaN as null.
function parseFloatOrNull(source) {
    const result = parseFloat(source);
    return Number.isNaN(result) ? null : result;
}

// safe_browser_api.dart's parseFontSize.
function parseFontSize(element) {
    let fontSize = null;
    if ('computedStyleMap' in element) {
        const value = element.computedStyleMap().get('font-size')?.value;
        fontSize = typeof value === 'number' ? value : null;
    }
    if (fontSize == null) {
        fontSize = parseFloatOrNull(window.getComputedStyle(element).getPropertyValue('font-size'));
    }
    return fontSize;
}

// safe_browser_api.dart's parseNumericStyleProperty.
function parseNumericStyleProperty(element, attributeName) {
    return parseFloatOrNull(window.getComputedStyle(element).getPropertyValue(attributeName));
}

// platform_dispatcher.dart's findBrowserTextScaleFactor.
function findBrowserTextScaleFactor() {
    const fontSize = parseFontSize(document.documentElement) ?? defaultRootFontSize;
    return fontSize / defaultRootFontSize;
}

export function observe(callback) {
    if (observer !== null) {
        return;
    }

    measurementElement = document.createElement('p');
    measurementElement.textContent = 'flutter typography measurement';
    // The element should be hidden from screen readers.
    measurementElement.setAttribute('aria-hidden', 'true');
    const style = measurementElement.style;
    // The element should be positioned off-screen above the window and not visible.
    style.position = 'fixed';
    style.bottom = '100%';
    style.visibility = 'hidden';
    style.opacity = '0';
    style.pointerEvents = 'none';
    // The element should be sensitive to letter-spacing, word-spacing, and line-height changes.
    style.width = 'auto';
    style.height = 'auto';
    style.whiteSpace = 'nowrap';
    // Set text spacing properties defaults.
    style.lineHeight = `${spacingDefault}px`;
    style.letterSpacing = `${spacingDefault}px`;
    style.wordSpacing = `${spacingDefault}px`;
    style.margin = `0px 0px ${spacingDefault}px 0px`;
    document.body.append(measurementElement);

    // Measure baseline font size to calculate the default line-height factor.
    const measurementFontSize = parseFontSize(measurementElement) ?? defaultRootFontSize;
    // The factor that we expect for line-height if no override is present. This factor is constant
    // regardless of browser zoom because both lineHeight and fontSize scale proportionally.
    defaultLineHeightFactor = spacingDefault / (measurementFontSize / findBrowserTextScaleFactor());

    observer = new ResizeObserver(() => callback());
    observer.observe(measurementElement);
}

// The measurement of one observer callback: [textScaleFactor, lineHeightScaleFactorOverride,
// letterSpacingOverride, wordSpacingOverride, paragraphSpacingOverride], with NaN for a null override.
export function read() {
    const computedTextScaleFactor = findBrowserTextScaleFactor();
    const lineHeight = parseNumericStyleProperty(measurementElement, 'line-height');
    const fontSize = parseFontSize(measurementElement);

    // A property is considered "default" (not overridden) if it matches either the specified value or
    // the zoomed specified value.
    const isDefault = (value, defaultValue) => value == null
        || Math.abs(value - defaultValue) < typographyPrecisionErrorTolerance
        || Math.abs(value - defaultValue * computedTextScaleFactor) < typographyPrecisionErrorTolerance;

    // We only consider it an override if the line-height factor deviates from our baseline.
    const computedLineHeightScaleFactor = fontSize != null && !isDefault(lineHeight, spacingDefault)
        ? lineHeight / fontSize
        : null;
    const computedWordSpacing = parseNumericStyleProperty(measurementElement, 'word-spacing');
    const computedLetterSpacing = parseNumericStyleProperty(measurementElement, 'letter-spacing');
    // There is no direct CSS property for paragraph spacing, so on the web this feature is usually
    // implemented by extension authors by leveraging `margin-bottom` on the `p` element.
    const computedParagraphSpacing = parseNumericStyleProperty(measurementElement, 'margin-bottom');

    const lineHeightOverride = computedLineHeightScaleFactor != null
        && Math.abs(computedLineHeightScaleFactor - defaultLineHeightFactor) < typographyPrecisionErrorTolerance
        ? null
        : computedLineHeightScaleFactor;
    const orNaN = value => value ?? NaN;
    return [
        computedTextScaleFactor,
        orNaN(lineHeightOverride),
        orNaN(isDefault(computedLetterSpacing, spacingDefault) ? null : computedLetterSpacing),
        orNaN(isDefault(computedWordSpacing, spacingDefault) ? null : computedWordSpacing),
        orNaN(isDefault(computedParagraphSpacing, spacingDefault) ? null : computedParagraphSpacing),
    ];
}
