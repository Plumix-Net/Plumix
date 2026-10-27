using System.Reflection;
using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Material;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;
using Xunit;

// Dart parity source: material_ui/lib/src/dialog_theme.dart
// Flutter tests: material_ui/test/dialog_theme_test.dart

namespace Plumix.Tests;

[Collection(SchedulerTestCollection.Name)]
public sealed class MaterialDialogThemeTests
{
    [Fact]
    public void DefaultsCopyEqualityAndHashMatchFlutter()
    {
        var empty = new DialogThemeData();
        Assert.All(typeof(DialogThemeData).GetProperties(), property => Assert.Null(property.GetValue(empty)));
        Assert.Equal(empty, empty.CopyWith());
        Assert.Equal(empty.GetHashCode(), empty.CopyWith().GetHashCode());
        Assert.Equal(empty, DialogThemeData.Lerp(null, null, 0));
        Assert.Same(empty, DialogThemeData.Lerp(empty, empty, 0.5));

        var widget = new DialogTheme();
        Assert.IsType<SizedBox>(widget.Child);
        Assert.Equal(empty, widget.Data);
        Assert.NotSame(widget.Data, widget.Data);
        Assert.All(LegacyProperties(), property => Assert.Null(property.GetValue(widget)));
        Assert.Equal(empty, DialogTheme.Lerp(null, null, 0).Data);
        Assert.Same(widget, DialogTheme.Lerp(widget, widget, 0.5));
        Assert.NotEqual(widget, new DialogTheme());
    }

    [Fact]
    public void DataCopyPreservesNullArgumentsAndReplacesEveryField()
    {
        DialogThemeData data = PopulatedData();
        Assert.Equal(data, data.CopyWith());
        Assert.Equal(data.GetHashCode(), data.CopyWith().GetHashCode());
        Assert.NotSame(data, data.CopyWith());
        var copy = new DialogThemeData().CopyWith(
            backgroundColor: data.BackgroundColor,
            elevation: data.Elevation,
            shadowColor: data.ShadowColor,
            surfaceTintColor: data.SurfaceTintColor,
            shape: data.Shape,
            alignment: data.Alignment,
            iconColor: data.IconColor,
            titleTextStyle: data.TitleTextStyle,
            contentTextStyle: data.ContentTextStyle,
            actionsPadding: data.ActionsPadding,
            barrierColor: data.BarrierColor,
            insetPadding: data.InsetPadding,
            clipBehavior: data.ClipBehavior,
            constraints: data.Constraints);
        Assert.Equal(data, copy);
        Assert.Equal(data, copy.CopyWith(backgroundColor: null, constraints: null));
        Assert.NotEqual(data, data.CopyWith(constraints: new BoxConstraints(MaxWidth: 400)));
        DialogThemeData[] changed =
        [
            data with { BackgroundColor = null }, data with { Elevation = null }, data with { ShadowColor = null },
            data with { SurfaceTintColor = null }, data with { Shape = null }, data with { Alignment = null },
            data with { IconColor = null }, data with { TitleTextStyle = null }, data with { ContentTextStyle = null },
            data with { ActionsPadding = null }, data with { BarrierColor = null }, data with { InsetPadding = null },
            data with { ClipBehavior = null }, data with { Constraints = null },
        ];
        Assert.All(changed, value => Assert.NotEqual(data, value));
    }

    [Fact]
    public void DataStoresUnvalidatedThemeValuesAndAllowsInterpolationExtrapolation()
    {
        var data = new DialogThemeData(Elevation: -1, InsetPadding: EdgeInsets.All(-2));
        Assert.Equal(-1, data.Elevation);
        Assert.Equal(EdgeInsets.All(-2), data.InsetPadding);
        Assert.True(double.IsNaN(new DialogThemeData(Elevation: double.NaN).Elevation!.Value));
        var from = new DialogThemeData(Elevation: 2, InsetPadding: EdgeInsets.All(4));
        var to = new DialogThemeData(Elevation: 6, InsetPadding: EdgeInsets.All(8));
        Assert.Equal(8, DialogThemeData.Lerp(from, to, 1.5).Elevation);
        Assert.Equal(-2, DialogThemeData.Lerp(from, to, -1).Elevation);
        Assert.Equal(EdgeInsets.All(-4), DialogThemeData.Lerp(from, to, -2).InsetPadding);
    }

    [Fact]
    public void LegacyFieldsExposeEffectiveDataAndCopiesDropChildKeyAndConstraints()
    {
        DialogThemeData data = PopulatedData();
        var child = new Text("original");
        var key = new UniqueKey();
        var widget = new DialogTheme(data: data, child: child, key: key);
        Assert.Same(data, widget.Data);
        foreach (PropertyInfo property in LegacyProperties())
        {
            Assert.Equal(typeof(DialogThemeData).GetProperty(property.Name)!.GetValue(data), property.GetValue(widget));
        }

        DialogTheme copy = widget.CopyWith(elevation: 12);
        Assert.Equal(data with { Elevation = 12, Constraints = null }, copy.Data);
        Assert.Null(copy.Key);
        Assert.IsType<SizedBox>(copy.Child);
        Assert.NotSame(copy.Data, copy.Data);
        Assert.False(widget.UpdateShouldNotify(new DialogTheme(data: data.CopyWith())));
        Assert.True(widget.UpdateShouldNotify(copy));
        Assert.True(widget.UpdateShouldNotify(new DialogTheme(data: data with { Constraints = null })));

        Widget wrappedChild = new SizedBox(width: 24);
        var wrapped = Assert.IsType<DialogTheme>(widget.Wrap(null!, wrappedChild));
        Assert.Same(data, wrapped.Data);
        Assert.Same(wrappedChild, wrapped.Child);
        Assert.Null(wrapped.Key);
        Assert.Same(widget, DialogTheme.Lerp(widget, widget, 0.25));
        Assert.Null(DialogTheme.Lerp(widget, null, 0.25).Data.Constraints);
    }

    [DebugOnlyFact]
    public void DataCannotBeMixedWithAnyLegacyField()
    {
        var constructor = typeof(DialogTheme).GetConstructors().Single(info => info.GetParameters().Length > 3);
        ParameterInfo[] parameters = constructor.GetParameters();
        DialogThemeData values = PopulatedData();
        foreach (PropertyInfo property in LegacyProperties())
        {
            object?[] arguments = parameters.Select(parameter => parameter.DefaultValue).ToArray();
            arguments[Array.FindIndex(parameters, parameter => parameter.Name == "data")] = new DialogThemeData();
            int fieldIndex = Array.FindIndex(parameters, parameter =>
                string.Equals(parameter.Name, property.Name, StringComparison.OrdinalIgnoreCase));
            arguments[fieldIndex] = typeof(DialogThemeData).GetProperty(property.Name)!.GetValue(values);
            var error = Assert.Throws<TargetInvocationException>(() => constructor.Invoke(arguments));
            Assert.IsType<AssertionError>(error.InnerException);
        }
    }

    [Theory]
    [InlineData(0.25, null, 20.0)]
    [InlineData(0.5, 20.0, null)]
    [InlineData(1.5, 20.0, null)]
    public void LerpPreservesNullTextEndpointsAndSwitchesClipAtMidpoint(
        double t, double? appearingSize, double? disappearingSize)
    {
        var style = new TextStyle(Inherit: false, FontSize: 20, Color: Colors.Blue);
        var data = new DialogThemeData(
            Elevation: 8,
            TitleTextStyle: style,
            ContentTextStyle: style,
            ClipBehavior: Clip.AntiAlias,
            Constraints: new BoxConstraints(100, 200, 50, 150));
        DialogThemeData appearing = DialogThemeData.Lerp(null, data, t);
        DialogThemeData disappearing = DialogThemeData.Lerp(data, null, t);
        Assert.Equal(appearingSize, appearing.TitleTextStyle!.FontSize);
        Assert.Equal(appearingSize, appearing.ContentTextStyle!.FontSize);
        Assert.Equal(disappearingSize, disappearing.TitleTextStyle!.FontSize);
        Assert.False(appearing.TitleTextStyle.Inherit);
        Assert.Equal(t < 0.5 ? null : Clip.AntiAlias, appearing.ClipBehavior);
        Assert.Equal(t < 0.5 ? Clip.AntiAlias : null, disappearing.ClipBehavior);
        Assert.Equal(8 * t, appearing.Elevation);
        Assert.Equal(data.Constraints!.Value * t, appearing.Constraints);

        var widget = new DialogTheme(data: data);
        DialogTheme legacy = DialogTheme.Lerp(null, widget, t);
        Assert.Equal(appearing.TitleTextStyle, legacy.TitleTextStyle);
        Assert.Equal(appearing.ContentTextStyle, legacy.ContentTextStyle);
        Assert.Equal(appearing.Elevation, legacy.Elevation);
        Assert.Equal(appearing.ClipBehavior, legacy.ClipBehavior);
        Assert.Null(legacy.Data.Constraints);
    }

    [DebugOnlyFact]
    public void DiagnosticsOmitNullDefaultsAndKeepFlutterPropertyOrder()
    {
        var empty = new DiagnosticPropertiesBuilder();
        new DialogThemeData().DebugFillProperties(empty);
        Assert.Empty(empty.Properties.Where(node => !node.IsFiltered(DiagnosticLevel.Info)));

        DialogThemeData data = PopulatedData();
        var builder = new DiagnosticPropertiesBuilder();
        data.DebugFillProperties(builder);
        string[] names =
        [
            "backgroundColor", "elevation", "shadowColor", "surfaceTintColor", "shape", "alignment", "iconColor",
            "titleTextStyle", "contentTextStyle", "actionsPadding", "barrierColor", "insetPadding", "clipBehavior",
            "constraints",
        ];
        Assert.Equal(names, builder.Properties.Select(node => node.Name));
        Assert.All(builder.Properties, node => Assert.False(node.IsFiltered(DiagnosticLevel.Info)));
        var widgetBuilder = new DiagnosticPropertiesBuilder();
        new DialogTheme(data: data).DebugFillProperties(widgetBuilder);
        Assert.Equal(names[..^1], widgetBuilder.Properties.Select(node => node.Name));
        Assert.Equal("elevation: 8.0", builder.Properties[1].ToString());
    }

    [Theory]
    [InlineData(true, 0)]
    [InlineData(true, 1)]
    [InlineData(true, 2)]
    [InlineData(false, 0)]
    [InlineData(false, 1)]
    [InlineData(false, 2)]
    public void AlertDialogAppliesGlobalLocalAndExplicitThemePrecedence(bool material3, int level)
    {
        using var tester = new FrameworkDartTester();
        DialogThemeData global = PopulatedData();
        DialogThemeData local = global.CopyWith(backgroundColor: Colors.Green, elevation: 9);
        DialogThemeData explicitData = local.CopyWith(
            backgroundColor: Colors.Pink,
            elevation: 12,
            iconColor: Colors.Purple,
            titleTextStyle: new TextStyle(Color: Colors.Orange),
            contentTextStyle: new TextStyle(Color: Colors.Yellow),
            clipBehavior: Clip.HardEdge,
            alignment: Alignment.TopRight);
        DialogThemeData expected = level == 2 ? explicitData : level == 1 ? local : global;
        var dialog = new AlertDialog(
            icon: new Icon(Icons.Search),
            title: new Text("Title"),
            content: new Text("Content"),
            actions: [new Icon(Icons.Cancel)],
            backgroundColor: level == 2 ? explicitData.BackgroundColor : null,
            elevation: level == 2 ? explicitData.Elevation : null,
            iconColor: level == 2 ? explicitData.IconColor : null,
            titleTextStyle: level == 2 ? explicitData.TitleTextStyle : null,
            contentTextStyle: level == 2 ? explicitData.ContentTextStyle : null,
            clipBehavior: level == 2 ? explicitData.ClipBehavior : null,
            alignment: level == 2 ? explicitData.Alignment : null);
        Widget home = level == 0 ? dialog : new DialogTheme(data: local, child: dialog);
        tester.PumpWidget(new MaterialApp(
            theme: new ThemeData(useMaterial3: material3) with { DialogTheme = global }, home: home));
        tester.PumpAndSettle();

        Plumix.Material.Material surface = tester.AllElements().Select(element => element.Widget)
            .OfType<Plumix.Material.Material>()
            .Single(widget => widget.Type == MaterialType.Card);
        Assert.Equal(expected.BackgroundColor, surface.Color);
        Assert.Equal(expected.Elevation, surface.Elevation);
        Assert.Equal(expected.ShadowColor, surface.ShadowColor);
        Assert.Equal(expected.SurfaceTintColor, surface.SurfaceTintColor);
        Assert.Equal(expected.Shape, surface.Shape);
        Assert.Equal(expected.ClipBehavior, surface.ClipBehavior);
        Assert.Equal(expected.TitleTextStyle!.Color, Paragraph(tester, "Title").Text.Style!.Color);
        Assert.Equal(expected.ContentTextStyle!.Color, Paragraph(tester, "Content").Text.Style!.Color);
        Element icon = tester.ElementsOfType<Icon>().Single(element => ((Icon)element.Widget).IconData == Icons.Search);
        Assert.Equal(expected.IconColor, IconTheme.Of(icon).Color);
        Assert.Contains(tester.AllElements().Select(element => element.Widget).OfType<Padding>(),
            padding => padding.Insets == expected.ActionsPadding);
        Assert.Contains(tester.AllElements().Select(element => element.Widget).OfType<Align>(),
            align => align.Alignment == expected.Alignment);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DefaultAlertAndSimpleTypographyAndIconColorsMatchMaterialVersion(bool material3)
    {
        using var tester = new FrameworkDartTester();
        var theme = new ThemeData(useMaterial3: material3) with { IconTheme = new IconThemeData(Color: Colors.Yellow) };
        BuildContext? context = null;
        tester.PumpWidget(new MaterialApp(theme: theme, home: new Builder(value =>
        {
            context = value;
            return new AlertDialog(
                icon: new Icon(Icons.Search), title: new Text("Title"), content: new Text("Content"));
        })));
        ThemeData effective = Theme.Of(context!);
        Assert.Equal(material3 ? effective.TextTheme.HeadlineSmall : effective.TextTheme.TitleLarge,
            DefaultTextStyle.Of(tester.ElementsWithText("Title").Single()).Style);
        Assert.Equal(material3 ? effective.TextTheme.BodyMedium : effective.TextTheme.TitleMedium,
            DefaultTextStyle.Of(tester.ElementsWithText("Content").Single()).Style);
        Assert.Equal(material3 ? effective.ColorScheme.Secondary : Colors.Yellow,
            IconTheme.Of(tester.ElementOfType<Icon>()).Color);

        tester.PumpWidget(new MaterialApp(theme: theme, home: new SimpleDialog(title: new Text("Simple title"))));
        Assert.Equal(effective.TextTheme.TitleLarge,
            DefaultTextStyle.Of(tester.ElementsWithText("Simple title").Single()).Style);
    }

    [Fact]
    public void LocalNullDataDoesNotMergeGlobalFieldsAndRegistersAnInheritedDependency()
    {
        using var tester = new FrameworkDartTester();
        var global = new DialogThemeData(BackgroundColor: Colors.Blue);
        Color? observed = Colors.Blue;
        int builds = 0;
        var child = new Builder(context =>
        {
            builds++;
            observed = DialogTheme.Of(context).BackgroundColor;
            return new SizedBox();
        });
        tester.PumpWidget(new MaterialApp(
            theme: new ThemeData() with { DialogTheme = global },
            home: new DialogTheme(child: child)));
        Assert.Null(observed);
        int previousBuilds = builds;
        tester.PumpWidget(new MaterialApp(
            theme: new ThemeData() with { DialogTheme = global },
            home: new DialogTheme(backgroundColor: Colors.Green, child: child)));
        Assert.Equal(Colors.Green, observed);
        Assert.True(builds > previousBuilds);
        Assert.Null(tester.TakeException());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DemoDataAndLegacyModesCaptureTheSameDialogTheme(bool legacy)
    {
        using var tester = new FrameworkDartTester(logicalSize: new Size(350, 700));
        tester.PumpWidget(new MaterialApp(home: new DialogDemoPage()));
        if (legacy)
        {
            tester.Tap(tester.ElementsWithText("Data theme").Single());
            tester.PumpAndSettle();
            Assert.Single(tester.ElementsWithText("Legacy theme"));
        }
        tester.Tap(tester.ElementsWithText("Theme off").Single());
        tester.PumpAndSettle();
        tester.Tap(tester.ElementsWithText("SHOW ALERT").Single());
        tester.PumpAndSettle();
        Assert.Contains(MaterialSurfaceProbe.FindAll(tester.RenderView),
            surface => surface.Color == new Color(0xFFE0F2F1));
        Assert.Single(tester.ElementsWithText("Delete draft?"));
        tester.Tap(tester.ElementsWithText("CANCEL").Single());
        tester.PumpAndSettle();
        Assert.Single(tester.ElementsWithText("Last result: cancel"));
        Assert.Null(tester.TakeException());
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(false, true)]
    public void DialogAlignmentInsetsAndClipFollowThemeAndExplicitOverrides(bool material3, bool explicitOverrides)
    {
        using var tester = new FrameworkDartTester();
        var data = new DialogThemeData(
            Alignment: Alignment.BottomLeft,
            InsetPadding: EdgeInsets.FromLTRB(10, 15, 20, 25),
            ClipBehavior: Clip.HardEdge,
            Shape: new RoundedRectangleBorder(borderRadius: BorderRadius.Circular(16)));
        var dialog = new Dialog(
            alignment: explicitOverrides ? Alignment.TopRight : null,
            clipBehavior: explicitOverrides ? Clip.AntiAlias : null,
            constraints: BoxConstraints.Tight(new Size(280, 100)),
            child: SizedBox.Expand());
        tester.PumpWidget(new MaterialApp(
            theme: new ThemeData(useMaterial3: material3) with { DialogTheme = data }, home: dialog));
        tester.PumpAndSettle();
        MaterialSurfaceProbe surface = Assert.Single(MaterialSurfaceProbe.FindAll(tester.RenderView));
        Assert.Equal(new Size(280, 100), surface.Size);
        Assert.Equal(explicitOverrides ? new Point(500, 15) : new Point(10, 475),
            surface.RenderObject.LocalToGlobal(default));
        Assert.Equal(explicitOverrides ? Clip.AntiAlias : Clip.HardEdge, surface.ClipBehavior);
        Assert.Equal(BorderRadius.Circular(16), surface.BorderRadius);
    }

    [Fact]
    public void ThemeConstraintsAndAsymmetricInsetsReachTheExpandedChild()
    {
        using var tester = new FrameworkDartTester();
        var childKey = new UniqueKey();
        tester.PumpWidget(new MaterialApp(
            theme: new ThemeData() with
            {
                DialogTheme = new DialogThemeData(InsetPadding: EdgeInsets.FromLTRB(10, 15, 20, 25)),
            },
            home: new Dialog(child: SizedBox.Expand(key: childKey))));
        var box = Assert.IsAssignableFrom<RenderBox>(tester.ElementsWithKey(childKey).Single().RenderObject);
        Assert.Equal(new Point(10, 15), box.LocalToGlobal(default));
        Assert.Equal(new Size(770, 560), box.Size);

        tester.PumpWidget(new MaterialApp(
            theme: new ThemeData() with
            {
                DialogTheme = new DialogThemeData(Constraints: new BoxConstraints(MaxWidth: 500, MaxHeight: 500)),
            },
            home: new Dialog(child: SizedBox.Expand(key: childKey))));
        tester.PumpAndSettle();
        box = Assert.IsAssignableFrom<RenderBox>(tester.ElementsWithKey(childKey).Single().RenderObject);
        Assert.Equal(new Size(500, 500), box.Size);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SimpleDialogTitleStyleHonorsThemeAndWidgetOverride(bool explicitOverride)
    {
        using var tester = new FrameworkDartTester();
        tester.PumpWidget(new MaterialApp(
            theme: new ThemeData() with
            {
                DialogTheme = new DialogThemeData(TitleTextStyle: new TextStyle(Color: Colors.Green)),
            },
            home: new SimpleDialog(
                title: new Text("Simple title"),
                titleTextStyle: explicitOverride ? new TextStyle(Color: Colors.Pink) : null)));
        Assert.Equal(explicitOverride ? Colors.Pink : Colors.Green,
            Paragraph(tester, "Simple title").Text.Style!.Color);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void ShowDialogBarrierUsesExplicitThenLocalThenGlobalColor(int level)
    {
        using var tester = new FrameworkDartTester();
        BuildContext? context = null;
        tester.PumpWidget(new MaterialApp(
            theme: new ThemeData() with { DialogTheme = new DialogThemeData(BarrierColor: Colors.Blue) },
            home: new DialogTheme(
                barrierColor: level > 0 ? Colors.Green : null,
                child: new Builder(value =>
                {
                    context = value;
                    return new SizedBox();
                }))));
        _ = MaterialDialogs.ShowDialog<object>(context!, _ => new Dialog(child: new Text("Dialog")),
            barrierColor: level == 2 ? Colors.Pink : null);
        tester.PumpAndSettle();
        Assert.Contains(tester.AllElements().Select(element => element.Widget).OfType<ModalBarrier>(),
            barrier => barrier.Color == (level == 2 ? Colors.Pink : level == 1 ? Colors.Green : Colors.Blue));
    }

    private static IEnumerable<PropertyInfo> LegacyProperties() => typeof(DialogTheme).GetProperties()
        .Where(property => typeof(DialogThemeData).GetProperty(property.Name) is not null && property.Name != "Data");

    private static DialogThemeData PopulatedData() => new(
        BackgroundColor: new Color(0xff123456),
        Elevation: 8,
        ShadowColor: new Color(0xff000001),
        SurfaceTintColor: new Color(0xff000002),
        Shape: new BeveledRectangleBorder(borderRadius: BorderRadius.Circular(20.5)),
        Alignment: Alignment.BottomLeft,
        IconColor: new Color(0xff654321),
        TitleTextStyle: new TextStyle(Color: Colors.White),
        ContentTextStyle: new TextStyle(Color: Colors.Black),
        ActionsPadding: EdgeInsetsGeometry.All(8),
        BarrierColor: new Color(0xff000005),
        InsetPadding: EdgeInsets.All(30),
        ClipBehavior: Clip.AntiAlias,
        Constraints: new BoxConstraints(MaxWidth: 500, MaxHeight: 500));

    private static RenderParagraph Paragraph(FrameworkDartTester tester, string text) =>
        Assert.IsType<RenderParagraph>(tester.ElementsWithText(text).Single().RenderObject);
}
