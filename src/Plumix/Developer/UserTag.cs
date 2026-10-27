// C#-only infrastructure: `dart:developer`'s `UserTag` (profiler.dart), following the reference
// implementation the Dart SDK ships for runtimes without a VM profiler (dart2js's `_FakeUserTag`):
// tags are canonicalized by label, at most `maxUserTags` exist, and the current tag is a plain
// variable. Dart keeps the current tag per isolate; a Plumix process has one, like a dart2js program.
// No profiler reads the tag yet; the framework sets it so a tool bridge can.

namespace Plumix.Developer;

/// <summary>A user tag can be used to group samples in a CPU profile.</summary>
/// <remarks>
/// <c>dart:developer</c>'s <c>UserTag</c>. Dart's <c>UserTag(label)</c> factory constructor returns the
/// existing tag with that label; C# constructors cannot, so it is <see cref="Create"/>.
/// </remarks>
public sealed class UserTag
{
    /// <summary>The maximum number of <see cref="UserTag"/> instances that can be created by a program.</summary>
    public const int MaxUserTags = 64;

    private static readonly Lock InstancesLock = new();
    private static readonly Dictionary<string, UserTag> Instances = [];

    private static readonly UserTag DefaultTagInstance = Create("Default");

    private static UserTag _currentTag = DefaultTagInstance;

    private UserTag(string label)
    {
        Label = label;
    }

    /// <summary>The default <see cref="UserTag"/> with label <c>Default</c>.</summary>
    public static UserTag DefaultTag => DefaultTagInstance;

    /// <summary>Label of this <see cref="UserTag"/>.</summary>
    public string Label { get; }

    /// <summary>The current tag; <c>dart:developer</c>'s top-level <c>getCurrentTag</c>.</summary>
    internal static UserTag Current => Volatile.Read(ref _currentTag);

    /// <summary>
    /// Returns the tag with the given <paramref name="label"/>, creating it if no tag carries that label
    /// yet.
    /// </summary>
    /// <exception cref="NotSupportedException">
    /// <see cref="MaxUserTags"/> tags exist already (Dart's <c>UnsupportedError</c>).
    /// </exception>
    public static UserTag Create(string label)
    {
        ArgumentNullException.ThrowIfNull(label);
        lock (InstancesLock)
        {
            // Canonicalize by name.
            if (Instances.TryGetValue(label, out UserTag? existingTag))
            {
                return existingTag;
            }

            // Throw an exception if we've reached the maximum number of user tags.
            if (Instances.Count == MaxUserTags)
            {
                throw new NotSupportedException($"UserTag instance limit ({MaxUserTags}) reached.");
            }

            return Instances[label] = new UserTag(label);
        }
    }

    /// <summary>Makes this tag the current tag. Returns the current tag before setting.</summary>
    public UserTag MakeCurrent() => Interlocked.Exchange(ref _currentTag, this);

    /// <summary>Forgets every tag but <see cref="DefaultTag"/> and makes it current, for a test.</summary>
    internal static void ResetForTests()
    {
        lock (InstancesLock)
        {
            Instances.Clear();
            Instances[DefaultTagInstance.Label] = DefaultTagInstance;
        }

        DefaultTagInstance.MakeCurrent();
    }
}
