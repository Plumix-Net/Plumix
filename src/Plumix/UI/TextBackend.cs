// C#-only infrastructure; no Dart parity source. The engine lays out paragraphs with its own text stack
// (SkParagraph over the platform's font collection) and loads fonts into that collection
// (`loadFontFromList`); Plumix asks the host's text engine through this seam, so the framework names no
// host type (docs/product avalonia-free-core.md, stage 3). Plumix.Avalonia installs the Avalonia text
// engine (AvaloniaTextBackend). Unlike the other seams, the default is not a stub that throws: without a
// host, paragraphs are HeadlessParagraph (the `FlutterTest` font's metrics, drawn as typed canvas data)
// and loaded fonts wait for the host's text engine.

namespace Plumix.UI;

/// <summary>The host's text engine: paragraph layout and the runtime font collection.</summary>
internal interface ITextBackend
{
    /// <summary>
    /// The paragraph a <see cref="ParagraphBuilder"/> built: dart:ui's <c>ParagraphBuilder.build</c>.
    /// </summary>
    Paragraph CreateParagraph(ParagraphContent content);

    /// <summary>
    /// dart:ui's <c>loadFontFromList</c>: adds the font file <paramref name="bytes"/> to the font collection
    /// under <paramref name="family"/>, or under the family stored in the font when it is null. False when
    /// the bytes are not a font the engine can read.
    /// </summary>
    bool RegisterFont(ReadOnlyMemory<byte> bytes, string? family);
}

/// <summary>The text engine the host installed, or the headless one.</summary>
internal static class TextBackend
{
    private static readonly object Gate = new();
    private static ITextBackend s_current = Headless.Instance;

    /// <summary>
    /// The text engine paragraphs and fonts go to. Installing a host's engine hands it every font loaded
    /// before it was installed (a <c>FontLoader</c> in <c>Main</c>), in load order.
    /// </summary>
    internal static ITextBackend Current
    {
        get => s_current;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            (ReadOnlyMemory<byte> Bytes, string? Family)[] pending;
            lock (Gate)
            {
                bool fromHeadless = ReferenceEquals(s_current, Headless.Instance);
                s_current = value;
                pending = fromHeadless ? Headless.Instance.TakePending() : [];
            }

            foreach ((ReadOnlyMemory<byte> bytes, string? family) in pending)
            {
                // The headless engine accepted these unchecked, as a host does before its font manager
                // exists; a font the host cannot read is dropped, not reported again.
                _ = value.RegisterFont(bytes, family);
            }
        }
    }

    // Without a host: `FlutterTest`-metric paragraphs, and fonts kept for the host's engine.
    private sealed class Headless : ITextBackend
    {
        internal static readonly Headless Instance = new();

        private readonly List<(ReadOnlyMemory<byte> Bytes, string? Family)> _pending = [];

        public Paragraph CreateParagraph(ParagraphContent content) => new HeadlessParagraph(content);

        public bool RegisterFont(ReadOnlyMemory<byte> bytes, string? family)
        {
            ITextBackend host;
            lock (Gate)
            {
                if (ReferenceEquals(s_current, this))
                {
                    // Like a host before its font manager exists, the bytes are checked when the host's
                    // engine receives them.
                    _pending.Add((bytes, family));
                    return true;
                }

                // A host engine was installed after the caller read Current.
                host = s_current;
            }

            return host.RegisterFont(bytes, family);
        }

        internal (ReadOnlyMemory<byte> Bytes, string? Family)[] TakePending()
        {
            (ReadOnlyMemory<byte> Bytes, string? Family)[] pending = [.. _pending];
            _pending.Clear();
            return pending;
        }
    }
}
