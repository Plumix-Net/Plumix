using System.Globalization;
using System.Text.RegularExpressions;

// Dart parity source: flutter/packages/flutter/lib/src/foundation/stack_frame.dart

namespace Plumix.Foundation;

/// <summary>
/// An object representation of a frame from a stack trace.
/// </summary>
/// <remarks>
/// Dart's stack traces are Dart-VM (or dart2js) formatted; a Plumix stack trace is a CLR one, so
/// [FromStackTraceLine] recognizes both grammars: Dart's `#N method (uri:line:column)` (kept
/// verbatim, which is what Flutter's own tests pin) and the CLR's `at Namespace.Type.Method(...)
/// in file:line N`. Dart's dart2js/DDC web-frame parsers have no CLR counterpart and are not
/// ported; see `docs/ai/DIVERGENCES.md`.
/// </remarks>
public sealed class StackFrame : IEquatable<StackFrame>
{
    private static readonly Regex VmFramePattern =
        new(@"^#(\d+) +(.+) \((.+?):?(\d+){0,1}:?(\d+){0,1}\)$", RegexOptions.Compiled);

    private static readonly Regex ClrFramePattern =
        new(@"^\s*at (?<target>.+?)(?: in (?<file>.+?):line (?<line>\d+))?\s*$", RegexOptions.Compiled);

    private static readonly Regex ClrGapPattern =
        new(@"^\s*---.*---\s*$", RegexOptions.Compiled);

    /// Creates a new StackFrame instance.
    ///
    /// The [ClassName] may be the empty string if there is no class (e.g. for a top level library
    /// method).
    public StackFrame(
        int number,
        int column,
        int line,
        string packageScheme,
        string package,
        string packagePath,
        string method,
        string source,
        string className = "",
        bool isConstructor = false)
    {
        Number = number;
        Column = column;
        Line = line;
        PackageScheme = packageScheme;
        Package = package;
        PackagePath = packagePath;
        ClassName = className;
        Method = method;
        IsConstructor = isConstructor;
        Source = source;
    }

    /// A stack frame representing an asynchronous suspension.
    public static StackFrame AsynchronousSuspension { get; } = new(
        number: -1,
        column: -1,
        line: -1,
        method: "asynchronous suspension",
        packageScheme: string.Empty,
        package: string.Empty,
        packagePath: string.Empty,
        source: "<asynchronous suspension>");

    /// A stack frame representing an elided stack overflow frame.
    public static StackFrame StackOverFlowElision { get; } = new(
        number: -1,
        column: -1,
        line: -1,
        method: "...",
        packageScheme: string.Empty,
        package: string.Empty,
        packagePath: string.Empty,
        source: "...");

    /// The original source of this stack frame.
    public string Source { get; }

    /// The zero-indexed frame number.
    ///
    /// This value may be -1 to indicate an unknown frame number.
    public int Number { get; }

    /// The scheme of the package for this frame, e.g. "dart" for `dart:core/errors_patch.dart`,
    /// "package" for `package:flutter/src/widgets/text.dart`, or "dotnet" for a CLR frame.
    public string PackageScheme { get; }

    /// The package for this frame, e.g. "core" for `dart:core/errors_patch.dart` or "flutter" for
    /// `package:flutter/src/widgets/text.dart`. For a CLR frame this is the root namespace of the
    /// declaring type.
    public string Package { get; }

    /// The path of the file for this frame, e.g. "errors_patch.dart" for
    /// `dart:core/errors_patch.dart`. For a CLR frame this is the rest of the declaring type's
    /// fully qualified name, with `.` replaced by `/`.
    public string PackagePath { get; }

    /// The source line number.
    public int Line { get; }

    /// The source column number.
    public int Column { get; }

    /// The class name, if any, for this frame.
    ///
    /// This may be empty for top level methods in a library or anonymous closure methods.
    public string ClassName { get; }

    /// The method name for this frame.
    ///
    /// This will be an empty string if the stack frame is from the default constructor.
    public string Method { get; }

    /// Whether or not this was thrown from a constructor.
    public bool IsConstructor { get; }

    /// Parses a list of [StackFrame]s from a stack trace object.
    public static List<StackFrame> FromStackTrace(System.Diagnostics.StackTrace stack)
    {
        ArgumentNullException.ThrowIfNull(stack);

        return FromStackString(stack.ToString());
    }

    /// Parses a list of [StackFrame]s from the string form of a stack trace.
    public static List<StackFrame> FromStackString(string stack)
    {
        ArgumentNullException.ThrowIfNull(stack);

        return [.. stack
            .Trim()
            .Split('\n')
            .Where(line => line.Length > 0)
            .Select(FromStackTraceLine)
            .OfType<StackFrame>()];
    }

    /// Parses a single [StackFrame] from a single line of a stack trace.
    ///
    /// Returns null if the format is not as expected.
    public static StackFrame? FromStackTraceLine(string line)
    {
        ArgumentNullException.ThrowIfNull(line);

        line = line.TrimEnd('\r');
        if (line == "<asynchronous suspension>")
        {
            return AsynchronousSuspension;
        }

        if (line == "...")
        {
            return StackOverFlowElision;
        }

        if (!line.StartsWith('#'))
        {
            return TryParseClrFrame(line);
        }

        Match match = VmFramePattern.Match(line);
        if (!match.Success)
        {
            return null;
        }

        bool isConstructor = false;
        string className = string.Empty;
        string method = match.Groups[2].Value.Replace(".<anonymous closure>", string.Empty, StringComparison.Ordinal);
        if (method.StartsWith("new", StringComparison.Ordinal))
        {
            string[] methodParts = method.Split(' ');

            // Sometimes a web frame will only read "new" and have no class name.
            className = methodParts.Length > 1 ? methodParts[1] : "<unknown>";
            method = string.Empty;
            if (className.Contains('.', StringComparison.Ordinal))
            {
                string[] parts = className.Split('.');
                className = parts[0];
                method = parts[1];
            }

            isConstructor = true;
        }
        else if (method.Contains('.', StringComparison.Ordinal))
        {
            string[] parts = method.Split('.');
            className = parts[0];
            method = parts[1];
        }

        (string scheme, string path, List<string> pathSegments) = ParseUri(match.Groups[3].Value);
        string package = "<unknown>";
        string packagePath = path;
        if ((scheme == "dart" || scheme == "package") && pathSegments.Count > 0)
        {
            package = pathSegments[0];
            packagePath = ReplaceFirst(path, $"{pathSegments[0]}/", string.Empty);
        }

        return new StackFrame(
            number: int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture),
            className: className,
            method: method,
            packageScheme: scheme,
            package: package,
            packagePath: packagePath,
            line: match.Groups[4].Success ? int.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture) : -1,
            column: match.Groups[5].Success ? int.Parse(match.Groups[5].Value, CultureInfo.InvariantCulture) : -1,
            isConstructor: isConstructor,
            source: line);
    }

    /// <inheritdoc />
    public bool Equals(StackFrame? other)
    {
        return other is not null
            && other.Number == Number
            && string.Equals(other.Package, Package, StringComparison.Ordinal)
            && other.Line == Line
            && other.Column == Column
            && string.Equals(other.ClassName, ClassName, StringComparison.Ordinal)
            && string.Equals(other.Method, Method, StringComparison.Ordinal)
            && string.Equals(other.Source, Source, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as StackFrame);

    /// <inheritdoc />
    public override int GetHashCode() =>
        HashCode.Combine(Number, Package, Line, Column, ClassName, Method, Source);

    /// <inheritdoc />
    public override string ToString() =>
        $"{Diagnostics.ObjectRuntimeType(this, "StackFrame")}(#{Number}, {PackageScheme}:{Package}/{PackagePath}:"
        + $"{Line}:{Column}, className: {ClassName}, method: {Method})";

    /// Parses a single CLR stack-trace line (`at Namespace.Type.Method(args) in file:line N`).
    ///
    /// Plumix-only: this replaces Dart's dart2js/DDC web-frame parsers, which describe a JavaScript
    /// stack no .NET runtime produces.
    private static StackFrame? TryParseClrFrame(string line)
    {
        if (ClrGapPattern.IsMatch(line))
        {
            return new StackFrame(
                number: -1,
                column: -1,
                line: -1,
                method: "asynchronous suspension",
                packageScheme: string.Empty,
                package: string.Empty,
                packagePath: string.Empty,
                source: line.Trim());
        }

        Match match = ClrFramePattern.Match(line);
        if (!match.Success)
        {
            return null;
        }

        string target = match.Groups["target"].Value;
        int arguments = target.IndexOf('(', StringComparison.Ordinal);
        if (arguments >= 0)
        {
            target = target[..arguments];
        }

        target = target.TrimEnd();
        int generics = target.IndexOf('[', StringComparison.Ordinal);
        if (generics >= 0)
        {
            target = target[..generics];
        }

        // `Namespace.Type..ctor` carries an empty segment between the type and the constructor.
        string[] parts = target.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            return null;
        }

        string method = parts[^1];
        string className = parts[^2];
        bool isConstructor = method is "ctor" or "cctor";
        if (isConstructor)
        {
            method = string.Empty;
        }

        string package = parts.Length > 2 ? parts[0] : "<unknown>";
        string packagePath = parts.Length > 2
            ? string.Join('/', parts[1..^1])
            : className;

        return new StackFrame(
            number: -1,
            column: -1,
            line: match.Groups["line"].Success
                ? int.Parse(match.Groups["line"].Value, CultureInfo.InvariantCulture)
                : -1,
            packageScheme: "dotnet",
            package: package,
            packagePath: packagePath,
            className: className,
            method: method,
            isConstructor: isConstructor,
            source: line.TrimEnd());
    }

    /// Splits `text` the way Dart's `Uri.parse` splits a stack-frame URI.
    ///
    /// `System.Uri` rejects `dart:core/errors_patch.dart` and normalizes what it accepts, so the
    /// three fields the parser needs are derived directly.
    private static (string Scheme, string Path, List<string> PathSegments) ParseUri(string text)
    {
        string scheme = string.Empty;
        string rest = text;
        int colon = text.IndexOf(':', StringComparison.Ordinal);
        if (colon > 0 && char.IsAsciiLetter(text[0]))
        {
            string candidate = text[..colon];
            if (candidate.All(c => char.IsAsciiLetterOrDigit(c) || c is '+' or '.' or '-'))
            {
                scheme = candidate;
                rest = text[(colon + 1)..];
            }
        }

        if (rest.StartsWith("//", StringComparison.Ordinal))
        {
            int slash = rest.IndexOf('/', 2);
            rest = slash < 0 ? string.Empty : rest[slash..];
        }

        List<string> segments = [.. rest.Split('/').Where(segment => segment.Length > 0)];
        return (scheme, rest, segments);
    }

    private static string ReplaceFirst(string text, string pattern, string replacement)
    {
        int index = text.IndexOf(pattern, StringComparison.Ordinal);
        return index < 0 ? text : text[..index] + replacement + text[(index + pattern.Length)..];
    }
}
