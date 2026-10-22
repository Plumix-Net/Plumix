using System.Runtime.InteropServices;

// C#-only infrastructure: the ICU word iterator SkParagraph uses in the Flutter engine.
// Hosts without a native ICU ABI can supply their engine's UTF-16 word boundaries.

namespace Plumix.UI;

/// Word segmentation supplied by the text backend.
public static class TextSegmentation
{
    private static readonly Lazy<IcuApi?> NativeApi = new(LoadNativeApi);

    /// Optional host backend (for example, the browser's Intl.Segmenter). Return increasing UTF-16
    /// offsets including zero and the text length, or null when the backend is unavailable.
    /// Install before creating paragraphs; each paragraph caches its boundaries.
    public static Func<string, int[]?>? WordBoundaryProvider { get; set; }

    internal static int[]? GetWordBoundaries(string text)
    {
        if (WordBoundaryProvider?.Invoke(text) is { } boundaries)
        {
            return boundaries;
        }

        return NativeApi.Value?.GetWordBoundaries(text);
    }

    private static IcuApi? LoadNativeApi()
    {
        if (OperatingSystem.IsBrowser())
        {
            return null;
        }

        string[] libraries = OperatingSystem.IsWindows()
            ? ["icu.dll"]
            : OperatingSystem.IsMacOS() || OperatingSystem.IsIOS() || OperatingSystem.IsMacCatalyst()
                ? ["/usr/lib/libicucore.A.dylib"]
                : OperatingSystem.IsAndroid()
                    ? ["libicu.so"]
                    : ["libicuuc.so", .. Enumerable.Range(50, 50).Reverse().Select(v => $"libicuuc.so.{v}")];
        foreach (string library in libraries)
        {
            if (!NativeLibrary.TryLoad(library, out nint handle))
            {
                continue;
            }

            // System ICU can export unversioned names or ICU's default major-version suffix.
            foreach (string suffix in new[] { string.Empty }
                         .Concat(Enumerable.Range(50, 50).Reverse().Select(v => $"_{v}")))
            {
                if (NativeLibrary.TryGetExport(handle, $"ubrk_open{suffix}", out nint open)
                    && NativeLibrary.TryGetExport(handle, $"ubrk_close{suffix}", out nint close)
                    && NativeLibrary.TryGetExport(handle, $"ubrk_first{suffix}", out nint first)
                    && NativeLibrary.TryGetExport(handle, $"ubrk_next{suffix}", out nint next))
                {
                    // Keep this library loaded for the process lifetime, like the shaping backend.
                    return new IcuApi(open, close, first, next);
                }
            }

            NativeLibrary.Free(handle);
        }

        return null;
    }

    private sealed class IcuApi(nint open, nint close, nint first, nint next)
    {
        private readonly OpenIterator _open = Marshal.GetDelegateForFunctionPointer<OpenIterator>(open);
        private readonly CloseIterator _close = Marshal.GetDelegateForFunctionPointer<CloseIterator>(close);
        private readonly MoveIterator _first = Marshal.GetDelegateForFunctionPointer<MoveIterator>(first);
        private readonly MoveIterator _next = Marshal.GetDelegateForFunctionPointer<MoveIterator>(next);

        public unsafe int[]? GetWordBoundaries(string text)
        {
            fixed (char* characters = text)
            {
                // ParagraphImpl passes no locale to SkUnicode; ICU uses its default rules.
                int status = 0;
                nint iterator = _open(1, null, characters, text.Length, &status); // UBRK_WORD
                if (iterator == 0 || status > 0) // ICU's negative statuses are success warnings.
                {
                    if (iterator != 0) _close(iterator);
                    return null;
                }

                try
                {
                    var result = new List<int>();
                    for (int offset = _first(iterator); offset != -1; offset = _next(iterator))
                    {
                        result.Add(offset);
                    }

                    return result.ToArray();
                }
                finally
                {
                    // ICU retains the text pointer until close; keep it pinned for the entire walk.
                    _close(iterator);
                }
            }
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private unsafe delegate nint OpenIterator(int type, byte* locale, char* text, int length, int* status);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void CloseIterator(nint iterator);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int MoveIterator(nint iterator);
}
