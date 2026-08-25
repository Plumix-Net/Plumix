// Port of `package:intl` 0.20.3 `lib/src/intl/string_stack.dart`.

namespace Plumix.Foundation.Intl;

/// <summary>A cursor over the string being parsed, as Dart's <c>StringStack</c>.</summary>
internal sealed class StringStack
{
    private readonly string contents;
    private int index;

    public StringStack(string contents)
    {
        this.contents = contents;
    }

    public bool AtStart => index == 0;

    public bool AtEnd => index >= contents.Length;

    public char Next() => contents[index++];

    public void Pop(int count = 1) => index += count;

    public string Read(int count = 1)
    {
        string result = Peek(count);
        Pop(count);
        return result;
    }

    public bool StartsWith(string pattern) =>
        string.CompareOrdinal(contents, index, pattern, 0, pattern.Length) == 0;

    public string Peek(int howMany = 1)
    {
        int end = Math.Min(index + howMany, contents.Length);
        return index >= end ? string.Empty : contents[index..end];
    }

    public string PeekAll() => Peek(contents.Length - index);

    public override string ToString() => $"{contents} at {index}";
}
