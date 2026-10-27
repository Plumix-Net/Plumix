using System.Runtime.InteropServices;
using Plumix.Widgets;

// C#-only host infrastructure: the desktop embedders' readers of the user's preferred languages.
// macOS: flutter/engine/src/flutter/shell/platform/darwin/macos/framework/Source/FlutterEngine.mm
// (`[NSLocale preferredLanguages]`, read here through CoreFoundation's `CFLocaleCopyPreferredLanguages`).
// Windows: flutter/engine/src/flutter/shell/platform/windows/system_utils.cc (`GetPreferredLanguageInfo`).
// Linux: flutter/engine/src/flutter/shell/platform/linux/fl_engine.cc (`setup_locales` over GLib's
// `g_get_language_names`).

namespace Plumix;

/// <summary>Reads the desktop OS's ordered list of preferred languages, as the desktop engines do.</summary>
/// <remarks>
/// All three are plain C APIs, so the core package reads them without platform bindings. A platform that
/// has none, or whose read fails, answers <see langword="null"/> and the host falls back to the system UI
/// culture.
/// </remarks>
internal static class DesktopLocales
{
    /// <summary>The preferred locales of the current desktop OS, or <see langword="null"/> when unknown.</summary>
    internal static IReadOnlyList<Locale>? Read()
    {
        try
        {
            IReadOnlyList<Locale> locales = OperatingSystem.IsMacOS() ? ReadMacOS()
                : OperatingSystem.IsWindows() ? ReadWindows()
                : OperatingSystem.IsLinux() ? ReadLinux(Environment.GetEnvironmentVariable)
                : [];
            return locales.Count == 0 ? null : locales;
        }
        catch (Exception exception) when (exception is DllNotFoundException or EntryPointNotFoundException)
        {
            return null;
        }
    }

    // ----- macOS -----

    /// <summary>
    /// <c>CFLocaleCopyPreferredLanguages</c>, the CoreFoundation side of <c>NSLocale.preferredLanguages</c>,
    /// with each identifier split as <c>NSLocale</c>'s component keys split it.
    /// </summary>
    internal static unsafe IReadOnlyList<Locale> ReadMacOS()
    {
        IntPtr array = CoreFoundation.CFLocaleCopyPreferredLanguages();
        if (array == IntPtr.Zero)
        {
            return [];
        }

        try
        {
            List<Locale> locales = [];
            nint count = CoreFoundation.CFArrayGetCount(array);
            for (nint index = 0; index < count; index++)
            {
                IntPtr value = CoreFoundation.CFArrayGetValueAtIndex(array, index);
                nint length = CoreFoundation.CFStringGetLength(value);
                char[] buffer = new char[length];
                fixed (char* characters = buffer)
                {
                    CoreFoundation.CFStringGetCharacters(value, new CFRange(0, length), characters);
                }

                if (ParseLanguageIdentifier(new string(buffer)) is { } locale)
                {
                    locales.Add(locale);
                }
            }

            return locales;
        }
        finally
        {
            CoreFoundation.CFRelease(array);
        }
    }

    /// <summary>
    /// Splits a preferred-language identifier (<c>en-US</c>, <c>zh-Hans-CN</c>, <c>es-419</c>) into its
    /// language, script and region; <see langword="null"/> without a language, which the engine skips.
    /// Extensions after a singleton subtag (<c>-u-…</c>, <c>-x-…</c>) are ignored.
    /// </summary>
    internal static Locale? ParseLanguageIdentifier(string identifier)
    {
        string[] subtags = identifier.Split('-', '_');
        string language = subtags[0];
        if (language.Length == 0)
        {
            return null;
        }

        string? script = null;
        string? region = null;
        for (int index = 1; index < subtags.Length && subtags[index].Length > 1; index++)
        {
            string subtag = subtags[index];
            if (script is null && region is null && subtag.Length == 4 && subtag.All(char.IsAsciiLetter))
            {
                script = subtag;
            }
            else if (region is null
                     && ((subtag.Length == 2 && subtag.All(char.IsAsciiLetter))
                         || (subtag.Length == 3 && subtag.All(char.IsAsciiDigit))))
            {
                region = subtag;
            }
        }

        return new Locale(language, region, script);
    }

    // ----- Windows -----

    private const uint MuiLanguageName = 0x8;
    private const uint MuiUiFallback = 0x30; // MUI_MERGE_SYSTEM_FALLBACK | MUI_MERGE_USER_FALLBACK

    /// <summary>
    /// <c>GetPreferredLanguageInfo</c>: <c>GetThreadPreferredUILanguages(MUI_LANGUAGE_NAME |
    /// MUI_UI_FALLBACK)</c>, a double-null-terminated list of language names.
    /// </summary>
    internal static unsafe IReadOnlyList<Locale> ReadWindows()
    {
        uint size = 0;
        if (!Kernel32.GetThreadPreferredUILanguages(MuiLanguageName | MuiUiFallback, out _, null, ref size))
        {
            return [];
        }

        char[] buffer = new char[size];
        fixed (char* characters = buffer)
        {
            if (!Kernel32.GetThreadPreferredUILanguages(
                    MuiLanguageName | MuiUiFallback, out _, characters, ref size))
            {
                return [];
            }
        }

        return [.. ParseMultiString(buffer).Select(ParseWindowsLanguageName).OfType<Locale>()];
    }

    /// <summary>
    /// <c>GetPreferredLanguages</c>: the null-terminated strings of a buffer that an empty string (a double
    /// null) ends.
    /// </summary>
    internal static IReadOnlyList<string> ParseMultiString(ReadOnlySpan<char> buffer)
    {
        List<string> strings = [];
        int start = 0;
        while (start < buffer.Length && buffer[start] != '\0')
        {
            ReadOnlySpan<char> rest = buffer[start..];
            int length = rest.IndexOf('\0');
            if (length < 0)
            {
                length = rest.Length;
            }

            strings.Add(rest[..length].ToString());
            start += length + 1;
        }

        return strings;
    }

    /// <summary>
    /// <c>ParseLanguageName</c>: splits a Windows language name by <c>-</c>, discarding any supplemental
    /// language info (<c>-x-foo</c>); of three components the middle one is the script, of two the second
    /// is the script when four characters long and the region otherwise.
    /// </summary>
    internal static Locale? ParseWindowsLanguageName(string languageName)
    {
        List<string> components = [];
        foreach (string component in languageName.Split('-'))
        {
            if (component == "x")
            {
                break;
            }

            components.Add(component);
        }

        if (components.Count == 0 || components[0].Length == 0)
        {
            return null;
        }

        string? script = null;
        string? region = null;
        if (components.Count == 3)
        {
            script = components[1];
            region = components[2];
        }
        else if (components.Count == 2)
        {
            // A script code will always be four characters long.
            if (components[1].Length == 4)
            {
                script = components[1];
            }
            else
            {
                region = components[1];
            }
        }

        return new Locale(components[0], region, script);
    }

    // ----- Linux -----

    /// <summary>
    /// <c>setup_locales</c> over GLib's <c>g_get_language_names</c>: the first non-empty of <c>LANGUAGE</c>
    /// (a colon-separated list), <c>LC_ALL</c>, <c>LC_MESSAGES</c> and <c>LANG</c>; each entry followed by its
    /// language alone (GLib's locale variants), with codesets and modifiers stripped and duplicates dropped.
    /// </summary>
    /// <remarks>
    /// GLib ends the list with <c>C</c>, which the engine reports as a language; Plumix drops <c>C</c> and
    /// <c>POSIX</c>, which name no language. GLib's <c>locale.alias</c> lookup is not ported.
    /// </remarks>
    internal static IReadOnlyList<Locale> ReadLinux(Func<string, string?> getEnvironmentVariable)
    {
        string? value = new[] { "LANGUAGE", "LC_ALL", "LC_MESSAGES", "LANG" }
            .Select(getEnvironmentVariable)
            .FirstOrDefault(candidate => !string.IsNullOrEmpty(candidate));
        List<Locale> locales = [];
        if (value is null)
        {
            return locales;
        }

        foreach (string entry in value.Split(':'))
        {
            // Ignore empty locales, caused by settings like `LANGUAGE=pt_BR:`.
            string name = entry.Trim();
            if (name.Length == 0)
            {
                continue;
            }

            ParseLinuxLocale(name, out string language, out string? territory);
            if (language.Length == 0 || language is "C" or "POSIX")
            {
                continue;
            }

            AddDistinct(locales, new Locale(language, territory));
            if (territory is not null)
            {
                AddDistinct(locales, new Locale(language));
            }
        }

        return locales;
    }

    /// <summary><c>parse_locale</c>: <c>language[_territory][.codeset][@modifier]</c>.</summary>
    internal static void ParseLinuxLocale(string locale, out string language, out string? territory)
    {
        string rest = locale;
        int match = rest.LastIndexOf('@');
        if (match >= 0)
        {
            rest = rest[..match];
        }

        match = rest.LastIndexOf('.');
        if (match >= 0)
        {
            rest = rest[..match];
        }

        match = rest.LastIndexOf('_');
        territory = null;
        if (match >= 0)
        {
            territory = rest[(match + 1)..];
            rest = rest[..match];
        }

        language = rest;
    }

    // Ignore duplicate locales, caused by settings like `LANGUAGE=en:en`.
    private static void AddDistinct(List<Locale> locales, Locale locale)
    {
        if (!locales.Contains(locale))
        {
            locales.Add(locale);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct CFRange(nint location, nint length)
    {
        public readonly nint Location = location;
        public readonly nint Length = length;
    }

    private static unsafe class CoreFoundation
    {
        private const string Library = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

        [DllImport(Library)]
        internal static extern IntPtr CFLocaleCopyPreferredLanguages();

        [DllImport(Library)]
        internal static extern nint CFArrayGetCount(IntPtr array);

        [DllImport(Library)]
        internal static extern IntPtr CFArrayGetValueAtIndex(IntPtr array, nint index);

        [DllImport(Library)]
        internal static extern nint CFStringGetLength(IntPtr value);

        [DllImport(Library)]
        internal static extern void CFStringGetCharacters(IntPtr value, CFRange range, char* buffer);

        [DllImport(Library)]
        internal static extern void CFRelease(IntPtr value);
    }

    private static unsafe class Kernel32
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetThreadPreferredUILanguages(
            uint flags, out uint numLanguages, char* languagesBuffer, ref uint languagesBufferSize);
    }
}
