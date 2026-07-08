using System.Runtime.CompilerServices;

namespace Plumix.Foundation;

// Dart parity source: flutter/packages/flutter/lib/src/foundation/licenses.dart
public readonly record struct LicenseParagraph(string Text, int Indent)
{
    public const int CenteredIndent = -1;
}

public abstract class LicenseEntry
{
    public abstract IEnumerable<string> Packages { get; }

    public abstract IEnumerable<LicenseParagraph> Paragraphs { get; }
}

public sealed class LicenseEntryWithLineBreaks : LicenseEntry
{
    public LicenseEntryWithLineBreaks(IEnumerable<string> packages, string text)
    {
        PackagesList = packages?.ToArray() ?? throw new ArgumentNullException(nameof(packages));
        Text = text ?? throw new ArgumentNullException(nameof(text));
    }

    public IReadOnlyList<string> PackagesList { get; }

    public string Text { get; }

    public override IEnumerable<string> Packages => PackagesList;

    public override IEnumerable<LicenseParagraph> Paragraphs => ParseParagraphs();

    private IReadOnlyList<LicenseParagraph> ParseParagraphs()
    {
        string normalized = Text
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Replace("\f", "\n\n", StringComparison.Ordinal);
        var result = new List<LicenseParagraph>();
        var paragraphLines = new List<string>();
        int? paragraphIndent = null;
        int lastLineIndent = 0;

        void Flush()
        {
            if (paragraphLines.Count == 0) return;
            result.Add(new LicenseParagraph(string.Join(' ', paragraphLines), paragraphIndent ?? 0));
            paragraphLines.Clear();
            paragraphIndent = null;
            lastLineIndent = 0;
        }

        foreach (string rawLine in normalized.Split('\n'))
        {
            if (rawLine.Length == 0 || string.IsNullOrWhiteSpace(rawLine))
            {
                Flush();
                continue;
            }

            int indent = CountIndent(rawLine);
            string text = rawLine.TrimStart(' ', '\t');
            if (paragraphLines.Count > 0 && indent > lastLineIndent)
            {
                Flush();
            }

            if (paragraphIndent is null)
            {
                paragraphIndent = indent > 10 ? LicenseParagraph.CenteredIndent : indent / 3;
            }

            paragraphLines.Add(text);
            lastLineIndent = indent;
        }

        Flush();
        return result;
    }

    private static int CountIndent(string line)
    {
        int result = 0;
        foreach (char character in line)
        {
            if (character == ' ') result++;
            else if (character == '\t') result += 8;
            else break;
        }
        return result;
    }
}

public delegate IAsyncEnumerable<LicenseEntry> LicenseEntryCollector();

public static class LicenseRegistry
{
    private static readonly object Sync = new();
    private static readonly List<LicenseEntryCollector> Collectors = [];

    public static void AddLicense(LicenseEntryCollector collector)
    {
        ArgumentNullException.ThrowIfNull(collector);
        lock (Sync) Collectors.Add(collector);
    }

    public static void AddLicense(Func<IEnumerable<LicenseEntry>> collector)
    {
        ArgumentNullException.ThrowIfNull(collector);
        AddLicense(() => ToAsyncEnumerable(collector()));
    }

    public static async IAsyncEnumerable<LicenseEntry> Licenses(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        LicenseEntryCollector[] snapshot;
        lock (Sync) snapshot = Collectors.ToArray();
        foreach (var collector in snapshot)
        {
            await foreach (var entry in collector().WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                yield return entry;
            }
        }
    }

    public static void Reset()
    {
        lock (Sync) Collectors.Clear();
    }

    private static async IAsyncEnumerable<LicenseEntry> ToAsyncEnumerable(IEnumerable<LicenseEntry> entries)
    {
        foreach (var entry in entries)
        {
            yield return entry;
        }
        await Task.CompletedTask;
    }
}
