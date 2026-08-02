using Avalonia.Media;
using Plumix.Material;
using Plumix.Widgets;
using Xunit;

namespace Plumix.Tests;

public sealed class MaterialGridTileBarTests
{
    [Fact]
    public void GridTileBar_ConstructorPreservesSourceApi()
    {
        var leading = new SizedBox(width: 12, height: 12);
        var title = new Text("Title");
        var subtitle = new Text("Subtitle");
        var trailing = new SizedBox(width: 14, height: 14);
        var color = Color.Parse("#CC102030");
        var bar = new GridTileBar(
            backgroundColor: color,
            leading: leading,
            title: title,
            subtitle: subtitle,
            trailing: trailing);

        Assert.Equal(color, bar.BackgroundColor);
        Assert.Same(leading, bar.Leading);
        Assert.Same(title, bar.Title);
        Assert.Same(subtitle, bar.Subtitle);
        Assert.Same(trailing, bar.Trailing);
    }

    [Fact]
    public void IconThemeData_MergePreservesUnspecifiedValues()
    {
        var inherited = new IconThemeData(
            Color: Colors.Black,
            Size: 18.0,
            Opacity: 0.4);

        IconThemeData merged = inherited.Merge(new IconThemeData(Color: Colors.White));

        Assert.Equal(Colors.White, merged.Color);
        Assert.Equal(18.0, merged.Size);
        Assert.Equal(0.4, merged.Opacity);
    }
}
