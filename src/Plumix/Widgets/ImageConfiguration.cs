using Avalonia;
using Plumix.Foundation;
using Plumix.Rendering;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/image.dart

public sealed class DefaultAssetBundle : InheritedWidget
{
    public DefaultAssetBundle(AssetBundle bundle, Widget child, Key? key = null) : base(key)
    {
        Bundle = bundle ?? throw new ArgumentNullException(nameof(bundle));
        Child = child ?? throw new ArgumentNullException(nameof(child));
    }

    public AssetBundle Bundle { get; }
    public Widget Child { get; }

    public override Widget Build(BuildContext context) => Child;

    protected override bool UpdateShouldNotify(InheritedWidget oldWidget)
    {
        return !Equals(Bundle, ((DefaultAssetBundle)oldWidget).Bundle);
    }

    public static AssetBundle Of(BuildContext context)
    {
        return MaybeOf(context) ?? PlatformAssetBundle.Root;
    }

    public static AssetBundle? MaybeOf(BuildContext context)
    {
        return context.DependOnInherited<DefaultAssetBundle>()?.Bundle;
    }
}

public static class ImageConfigurationUtils
{
    public static ImageConfiguration CreateLocalImageConfiguration(
        BuildContext context,
        Size? size = null)
    {
        return new ImageConfiguration(
            Bundle: DefaultAssetBundle.Of(context),
            DevicePixelRatio: MediaQuery.MaybeOf(context)?.DevicePixelRatio,
            Locale: System.Globalization.CultureInfo.CurrentUICulture,
            TextDirection: Directionality.MaybeOf(context),
            Size: size,
            Platform: ResolvePlatform());
    }

    private static ImageTargetPlatform ResolvePlatform()
    {
        if (OperatingSystem.IsIOS()) return ImageTargetPlatform.IOS;
        if (OperatingSystem.IsMacOS()) return ImageTargetPlatform.MacOS;
        if (OperatingSystem.IsAndroid()) return ImageTargetPlatform.Android;
        if (OperatingSystem.IsWindows()) return ImageTargetPlatform.Windows;
        if (OperatingSystem.IsLinux()) return ImageTargetPlatform.Linux;
        return ImageTargetPlatform.Android;
    }
}
