using Plumix.UI;

// C#-only engine adapter: paragraph layout and loadFontFromList over Avalonia's text stack (ITextBackend,
// src/Plumix/UI/TextBackend.cs). Paragraphs come from ParagraphBackend, whose one-time font-manager probe
// picks AvaloniaParagraph or the headless FlutterTest-metric HeadlessParagraph; fonts go to AvaloniaFonts.

namespace Plumix;

/// <summary>The Avalonia text engine.</summary>
internal sealed class AvaloniaTextBackend : ITextBackend
{
    internal static readonly AvaloniaTextBackend Instance = new();

    private AvaloniaTextBackend()
    {
    }

    public Paragraph CreateParagraph(ParagraphContent content) => ParagraphBackend.Create(content);

    public bool RegisterFont(ReadOnlyMemory<byte> bytes, string? family) => AvaloniaFonts.Register(bytes, family);
}
