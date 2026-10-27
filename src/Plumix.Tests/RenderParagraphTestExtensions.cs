using Avalonia.Media;
using Plumix.Painting;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Tests;

// C#-only test infrastructure: no Dart parity source.
//
// Like Dart's `RenderParagraph`, Plumix's exposes the styled text only through `Text` (an
// `InlineSpan`). Flutter's tests read `paragraph.text.style!.<field>` or
// `paragraph.text.toPlainText()`; these test-only extension members spell the same reads once, so
// the assertions keep comparing the root span's style (with the framework's text fallbacks applied).
internal static class RenderParagraphTestExtensions
{
    extension(RenderParagraph paragraph)
    {
        /// The root span's style, or the framework's text fallback when the root span has none.
        public TextStyle RootStyle => paragraph.Text.Style ?? TextStyle.Fallback;

        /// Dart's `TextPainter.plainText`: the span tree's text without semantics labels.
        public string PlainText => paragraph.Text.ToPlainText(includeSemanticsLabels: false);

        public FontFamily FontFamily => paragraph.RootStyle.FontFamily ?? FontFamily.Default;

        public FontStyle FontStyle => paragraph.RootStyle.FontStyle ?? FontStyle.Normal;

        public FontWeight FontWeight => paragraph.RootStyle.FontWeight ?? FontWeight.Normal;

        public double FontSize => paragraph.RootStyle.FontSize ?? TextDefaults.DefaultFontSize;

        public IBrush Foreground => new SolidColorBrush(paragraph.RootStyle.Color ?? new Color(0xFF000000));

        public double? Height => paragraph.RootStyle.Height;

        public double LetterSpacing => paragraph.RootStyle.LetterSpacing ?? 0;
    }
}
