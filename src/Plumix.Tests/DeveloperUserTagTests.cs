using Plumix.Developer;
using Xunit;

// C#-only test infrastructure coverage: `dart:developer`'s `UserTag`, against the Dart SDK's
// `_FakeUserTag` (sdk/lib/_internal/js_runtime/lib/developer_patch.dart), which canonicalizes tags by
// label, caps them at `maxUserTags`, and keeps one current tag.

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class DeveloperUserTagTests : IDisposable
{
    public DeveloperUserTagTests() => UserTag.ResetForTests();

    public void Dispose() => UserTag.ResetForTests();

    [Fact]
    public void DefaultTag_IsLabelledDefaultAndCurrent()
    {
        Assert.Equal("Default", UserTag.DefaultTag.Label);
        Assert.Same(UserTag.DefaultTag, DeveloperService.GetCurrentTag());
        Assert.Same(UserTag.DefaultTag, UserTag.Create("Default"));
    }

    [Fact]
    public void Create_CanonicalizesByLabel()
    {
        UserTag tag = UserTag.Create("tag");
        Assert.Same(tag, UserTag.Create("tag"));
        Assert.NotSame(tag, UserTag.Create("other"));
    }

    [Fact]
    public void MakeCurrent_ReturnsThePreviousTag()
    {
        UserTag tag = UserTag.Create("tag");
        Assert.Same(UserTag.DefaultTag, tag.MakeCurrent());
        Assert.Same(tag, DeveloperService.GetCurrentTag());
        Assert.Same(tag, UserTag.DefaultTag.MakeCurrent());
        Assert.Same(UserTag.DefaultTag, DeveloperService.GetCurrentTag());
    }

    [Fact]
    public void Create_ThrowsOnceMaxUserTagsExist()
    {
        // The default tag is one of the 64.
        for (int i = 1; i < UserTag.MaxUserTags; i++)
        {
            UserTag.Create($"tag {i}");
        }

        Assert.Same(UserTag.Create("tag 1"), UserTag.Create("tag 1"));
        NotSupportedException error = Assert.Throws<NotSupportedException>(static () => UserTag.Create("one more"));
        Assert.Equal("UserTag instance limit (64) reached.", error.Message);
    }
}
