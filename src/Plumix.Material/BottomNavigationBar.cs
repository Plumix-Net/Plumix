using Avalonia;
using Avalonia.Media;
using Plumix;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;
using Plumix.Widgets;

namespace Plumix.Material;

// Dart parity source (reference): flutter/packages/flutter/lib/src/material/bottom_navigation_bar.dart; flutter/packages/flutter/lib/src/widgets/bottom_navigation_bar_item.dart

public enum BottomNavigationBarType
{
    Fixed,
    Shifting,
}

public sealed class BottomNavigationBarItem
{
    public BottomNavigationBarItem(
        Widget icon,
        string label,
        Widget? activeIcon = null,
        Color? backgroundColor = null,
        string? tooltip = null,
        Key? key = null)
    {
        Icon = icon ?? throw new ArgumentNullException(nameof(icon));
        Label = label ?? throw new ArgumentNullException(nameof(label));
        ActiveIcon = activeIcon ?? icon;
        BackgroundColor = backgroundColor;
        Tooltip = tooltip;
        Key = key;
    }

    public Key? Key { get; }

    public Widget Icon { get; }

    public Widget ActiveIcon { get; }

    public string Label { get; }

    public Color? BackgroundColor { get; }

    public string? Tooltip { get; }
}

public sealed class BottomNavigationBar : StatefulWidget
{
    private const double DefaultHeight = 56.0;
    private const double DefaultIconSize = 24.0;

    public BottomNavigationBar(
        IReadOnlyList<BottomNavigationBarItem> items,
        Action<int>? onTap = null,
        int currentIndex = 0,
        BottomNavigationBarType? type = null,
        Color? backgroundColor = null,
        Color? selectedItemColor = null,
        Color? unselectedItemColor = null,
        IconThemeData? selectedIconTheme = null,
        IconThemeData? unselectedIconTheme = null,
        double? elevation = null,
        double iconSize = DefaultIconSize,
        double selectedFontSize = 14.0,
        double unselectedFontSize = 12.0,
        TextStyle? selectedLabelStyle = null,
        TextStyle? unselectedLabelStyle = null,
        bool? showSelectedLabels = null,
        bool? showUnselectedLabels = null,
        Key? key = null) : base(key)
    {
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        if (items.Count < 2)
        {
            throw new ArgumentException("BottomNavigationBar requires at least two items.", nameof(items));
        }

        if (currentIndex < 0 || currentIndex >= items.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(currentIndex), "Current index must be within item range.");
        }

        if (!double.IsFinite(iconSize) || iconSize < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(iconSize), "Icon size must be finite and non-negative.");
        }

        if (!double.IsFinite(selectedFontSize) || selectedFontSize < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(selectedFontSize), "Selected font size must be finite and non-negative.");
        }

        if (!double.IsFinite(unselectedFontSize) || unselectedFontSize < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(unselectedFontSize), "Unselected font size must be finite and non-negative.");
        }

        if (elevation.HasValue && (!double.IsFinite(elevation.Value) || elevation.Value < 0))
        {
            throw new ArgumentOutOfRangeException(nameof(elevation), "Elevation must be finite and non-negative.");
        }

        if ((selectedIconTheme is null) != (unselectedIconTheme is null))
        {
            throw new ArgumentException("Both selectedIconTheme and unselectedIconTheme must be provided together.");
        }

        ValidateIconTheme(nameof(selectedIconTheme), selectedIconTheme);
        ValidateIconTheme(nameof(unselectedIconTheme), unselectedIconTheme);

        Items = items;
        OnTap = onTap;
        CurrentIndex = currentIndex;
        Type = type;
        BackgroundColor = backgroundColor;
        SelectedItemColor = selectedItemColor;
        UnselectedItemColor = unselectedItemColor;
        SelectedIconTheme = selectedIconTheme;
        UnselectedIconTheme = unselectedIconTheme;
        Elevation = elevation;
        IconSize = iconSize;
        SelectedFontSize = selectedFontSize;
        UnselectedFontSize = unselectedFontSize;
        SelectedLabelStyle = selectedLabelStyle;
        UnselectedLabelStyle = unselectedLabelStyle;
        ShowSelectedLabels = showSelectedLabels;
        ShowUnselectedLabels = showUnselectedLabels;
    }

    public IReadOnlyList<BottomNavigationBarItem> Items { get; }

    public Action<int>? OnTap { get; }

    public int CurrentIndex { get; }

    public BottomNavigationBarType? Type { get; }

    public Color? BackgroundColor { get; }

    public Color? SelectedItemColor { get; }

    public Color? UnselectedItemColor { get; }

    public IconThemeData? SelectedIconTheme { get; }

    public IconThemeData? UnselectedIconTheme { get; }

    public double? Elevation { get; }

    public double IconSize { get; }

    public double SelectedFontSize { get; }

    public double UnselectedFontSize { get; }

    public TextStyle? SelectedLabelStyle { get; }

    public TextStyle? UnselectedLabelStyle { get; }

    public bool? ShowSelectedLabels { get; }

    public bool? ShowUnselectedLabels { get; }

    public override State CreateState()
    {
        return new BottomNavigationBarState();
    }

    private sealed class BottomNavigationBarState : State
    {
        private const int FlexScale = 1000;
        private const double SelectedFlexBonus = 0.5;
        private static readonly TimeSpan SelectionTransitionDuration = TimeSpan.FromMilliseconds(250);
        private static readonly TimeSpan BackgroundTransitionDuration = TimeSpan.FromMilliseconds(300);

        private readonly ColorTween _colorTween = new();

        private List<AnimationController> _selectionControllers = [];
        private AnimationController? _backgroundController;

        private bool _backgroundInitialized;
        private Color _backgroundFromColor;
        private Color _backgroundToColor;
        private int _backgroundOriginIndex;
        private int? _pendingTapIndex;

        private BottomNavigationBar CurrentWidget => (BottomNavigationBar)StateWidget;

        public override void InitState()
        {
            InitializeSelectionControllers(CurrentWidget.Items.Count, CurrentWidget.CurrentIndex);

            _backgroundController = new AnimationController(BackgroundTransitionDuration)
            {
                Curve = Curves.EaseOut,
            };
            _backgroundController.Changed += HandleAnimationTick;
            _backgroundController.Completed += HandleBackgroundCompleted;
            _backgroundOriginIndex = CurrentWidget.CurrentIndex;
        }

        public override void DidUpdateWidget(StatefulWidget oldWidget)
        {
            var oldBar = (BottomNavigationBar)oldWidget;

            if (oldBar.Items.Count != CurrentWidget.Items.Count)
            {
                InitializeSelectionControllers(CurrentWidget.Items.Count, CurrentWidget.CurrentIndex);
                if (_pendingTapIndex.HasValue && _pendingTapIndex.Value >= CurrentWidget.Items.Count)
                {
                    _pendingTapIndex = null;
                }
            }
            else if (oldBar.CurrentIndex != CurrentWidget.CurrentIndex)
            {
                StartSelectionAnimation(oldBar.CurrentIndex, CurrentWidget.CurrentIndex);
            }
        }

        public override void Dispose()
        {
            foreach (var controller in _selectionControllers)
            {
                controller.Changed -= HandleAnimationTick;
                controller.Dispose();
            }

            _selectionControllers.Clear();

            if (_backgroundController is not null)
            {
                _backgroundController.Changed -= HandleAnimationTick;
                _backgroundController.Completed -= HandleBackgroundCompleted;
                _backgroundController.Dispose();
                _backgroundController = null;
            }
        }

        public override Widget Build(BuildContext context)
        {
            var theme = Theme.Of(context);
            var bottomTheme = BottomNavigationBarTheme.Of(context);
            var effectiveType = CurrentWidget.ResolveEffectiveType(bottomTheme);
            var effectiveBackground = CurrentWidget.ResolveBackgroundColor(theme, bottomTheme, effectiveType);

            EnsureBackgroundTransition(effectiveType, effectiveBackground);

            var effectiveSelectedColor = CurrentWidget.SelectedItemColor
                                         ?? bottomTheme.SelectedItemColor
                                         ?? ResolveDefaultSelectedColor(theme, effectiveType);
            var effectiveUnselectedColor = CurrentWidget.UnselectedItemColor
                                           ?? bottomTheme.UnselectedItemColor
                                           ?? ResolveDefaultUnselectedColor(theme, effectiveType);

            var effectiveSelectedIconTheme = ResolveIconTheme(
                CurrentWidget.SelectedIconTheme ?? bottomTheme.SelectedIconTheme,
                effectiveSelectedColor,
                CurrentWidget.IconSize);
            var effectiveUnselectedIconTheme = ResolveIconTheme(
                CurrentWidget.UnselectedIconTheme ?? bottomTheme.UnselectedIconTheme,
                effectiveUnselectedColor,
                CurrentWidget.IconSize);

            var labelBaseStyle = theme.TextTheme.LabelLarge with
            {
                FontSize = CurrentWidget.SelectedFontSize,
            };
            var effectiveSelectedLabelStyle = ResolveLabelStyle(
                labelBaseStyle,
                CurrentWidget.SelectedLabelStyle ?? bottomTheme.SelectedLabelStyle,
                CurrentWidget.SelectedFontSize,
                effectiveSelectedColor);
            var effectiveUnselectedLabelStyle = ResolveLabelStyle(
                labelBaseStyle,
                CurrentWidget.UnselectedLabelStyle ?? bottomTheme.UnselectedLabelStyle,
                CurrentWidget.UnselectedFontSize,
                effectiveUnselectedColor);

            var effectiveShowSelectedLabels = CurrentWidget.ShowSelectedLabels
                                              ?? bottomTheme.ShowSelectedLabels
                                              ?? true;
            var effectiveShowUnselectedLabels = CurrentWidget.ShowUnselectedLabels
                                                ?? bottomTheme.ShowUnselectedLabels
                                                ?? (effectiveType == BottomNavigationBarType.Fixed);
            var effectiveElevation = CurrentWidget.Elevation ?? bottomTheme.Elevation;

            var tiles = new List<Widget>(CurrentWidget.Items.Count);
            for (var index = 0; index < CurrentWidget.Items.Count; index++)
            {
                var itemIndex = index;
                var item = CurrentWidget.Items[itemIndex];
                var selectionValue = ResolveSelectionValue(index);
                var iconFromColor = effectiveUnselectedIconTheme.Color ?? effectiveUnselectedColor;
                var iconToColor = effectiveSelectedIconTheme.Color ?? effectiveSelectedColor;
                var iconFromSize = effectiveUnselectedIconTheme.Size ?? CurrentWidget.IconSize;
                var iconToSize = effectiveSelectedIconTheme.Size ?? CurrentWidget.IconSize;
                var iconColor = _colorTween.Evaluate(
                    selectionValue,
                    iconFromColor,
                    iconToColor);
                var iconSize = Lerp(
                    iconFromSize,
                    iconToSize,
                    selectionValue);
                var iconTheme = new IconThemeData(
                    Color: iconColor,
                    Size: iconSize);

                var selectedVisual = selectionValue >= 0.5;
                var icon = selectedVisual ? item.ActiveIcon : item.Icon;
                var labelStyle = selectedVisual ? effectiveSelectedLabelStyle : effectiveUnselectedLabelStyle;
                var labelOpacity = ResolveLabelOpacity(
                    selectionValue,
                    effectiveShowSelectedLabels,
                    effectiveShowUnselectedLabels);
                var tileFlex = ResolveTileFlex(selectionValue, effectiveType);

                var tileChildren = new List<Widget>
                {
                    new IconTheme(
                        data: iconTheme,
                        child: icon),
                };

                if (labelOpacity > 0)
                {
                    tileChildren.Add(new Opacity(
                        labelOpacity,
                        child: CreateLabel(item.Label, labelStyle)));
                }

                var tileBody = new SizedBox(
                    height: DefaultHeight,
                    child: new Center(
                        child: new Column(
                            mainAxisSize: MainAxisSize.Min,
                            crossAxisAlignment: CrossAxisAlignment.Center,
                            spacing: 4,
                            children: tileChildren)));

                Action? onTileTap = CurrentWidget.OnTap is null
                    ? null
                    : () => HandleTileTap(itemIndex);

                var semanticsChildren = new List<Widget>
                {
                    tileBody,
                    new Semantics(label: CreateIndexSemanticsLabel(context, itemIndex, CurrentWidget.Items.Count)),
                };

                var tileNeedsSemanticLabel = !(effectiveShowSelectedLabels && effectiveShowUnselectedLabels);
                var tileSemanticsLabel = tileNeedsSemanticLabel ? item.Label : null;

                Widget tileContent = new Semantics(
                    label: tileSemanticsLabel,
                    flags: ResolveTileSemanticsFlags(selectedVisual, onTileTap is not null),
                    onTap: onTileTap,
                    child: new Stack(children: semanticsChildren));

                tileContent = new GestureDetector(
                    behavior: HitTestBehavior.Opaque,
                    onTap: onTileTap,
                    child: tileContent);

                if (!string.IsNullOrWhiteSpace(item.Tooltip))
                {
                    tileContent = new Tooltip(
                        message: item.Tooltip!,
                        preferBelow: false,
                        verticalOffset: CurrentWidget.IconSize + CurrentWidget.SelectedFontSize,
                        excludeFromSemantics: true,
                        child: tileContent);
                }

                tiles.Add(new Expanded(
                    key: item.Key,
                    flex: tileFlex,
                    child: tileContent));
            }

            var row = new Row(
                mainAxisAlignment: MainAxisAlignment.SpaceBetween,
                spacing: 0,
                children: tiles);

            var barWidth = ResolveBarWidth(context);
            var overlay = BuildRadialBackgroundOverlay(barWidth, effectiveType);
            Widget rowWithOverlay = row;
            if (overlay is not null)
            {
                rowWithOverlay = new ClipRect(
                    clipRect: new Rect(0, 0, barWidth, DefaultHeight),
                    child: new Stack(
                        children:
                        [
                            overlay,
                            row,
                        ]));
            }

            var containerColor = ResolveContainerBackground(effectiveType, effectiveBackground);
            var content = new SafeArea(
                top: false,
                child: new SizedBox(
                    height: DefaultHeight,
                    child: rowWithOverlay));

            if (effectiveElevation.HasValue && effectiveElevation.Value > 0)
            {
                return new Container(
                    decoration: new BoxDecoration(
                        Color: containerColor,
                        BoxShadows: BuildBoxShadows(theme.ShadowColor, effectiveElevation.Value)),
                    child: content);
            }

            return new Container(
                color: containerColor,
                child: content);
        }

        private void InitializeSelectionControllers(int count, int selectedIndex)
        {
            foreach (var controller in _selectionControllers)
            {
                controller.Changed -= HandleAnimationTick;
                controller.Dispose();
            }

            _selectionControllers = new List<AnimationController>(count);
            for (var index = 0; index < count; index++)
            {
                var controller = new AnimationController(SelectionTransitionDuration)
                {
                    Curve = Curves.EaseOut,
                };
                controller.Changed += HandleAnimationTick;
                SetControllerValue(controller, index == selectedIndex ? 1.0 : 0.0);
                _selectionControllers.Add(controller);
            }
        }

        private void StartSelectionAnimation(int fromIndex, int toIndex)
        {
            if (fromIndex == toIndex)
            {
                return;
            }

            for (var index = 0; index < _selectionControllers.Count; index++)
            {
                var controller = _selectionControllers[index];
                var target = index == toIndex ? 1.0 : 0.0;
                var value = controller.Value;

                if (target > value)
                {
                    controller.Forward(from: value);
                }
                else if (target < value)
                {
                    controller.Reverse(from: value);
                }
            }
        }

        private void EnsureBackgroundTransition(BottomNavigationBarType effectiveType, Color targetBackground)
        {
            if (!_backgroundInitialized)
            {
                _backgroundInitialized = true;
                _backgroundFromColor = targetBackground;
                _backgroundToColor = targetBackground;
                _backgroundOriginIndex = CurrentWidget.CurrentIndex;
                return;
            }

            if (effectiveType != BottomNavigationBarType.Shifting)
            {
                _backgroundFromColor = targetBackground;
                _backgroundToColor = targetBackground;
                _backgroundController?.Stop();
                return;
            }

            if (_backgroundToColor == targetBackground)
            {
                return;
            }

            _backgroundFromColor = ResolveCurrentAnimatedBackgroundColor();
            _backgroundToColor = targetBackground;
            _backgroundOriginIndex = ResolveBackgroundOriginIndex();
            _pendingTapIndex = null;
            _backgroundController?.Forward(from: 0);
        }

        private Color ResolveContainerBackground(BottomNavigationBarType effectiveType, Color fallback)
        {
            if (!_backgroundInitialized)
            {
                return fallback;
            }

            if (effectiveType != BottomNavigationBarType.Shifting)
            {
                return _backgroundToColor;
            }

            if (_backgroundController?.IsAnimating == true)
            {
                return _backgroundFromColor;
            }

            return _backgroundToColor;
        }

        private Widget? BuildRadialBackgroundOverlay(double barWidth, BottomNavigationBarType effectiveType)
        {
            if (effectiveType != BottomNavigationBarType.Shifting
                || !_backgroundInitialized
                || _backgroundController is null
                || !_backgroundController.IsAnimating
                || _backgroundFromColor == _backgroundToColor)
            {
                return null;
            }

            var progress = _backgroundController.Evaluate();
            var maxRadius = Math.Sqrt((barWidth * barWidth) + (DefaultHeight * DefaultHeight));
            var radius = maxRadius * progress;
            if (radius <= 0)
            {
                return null;
            }

            var centerX = ResolveBackgroundCenterX(barWidth, effectiveType);
            var centerY = DefaultHeight / 2.0;

            return new Positioned(
                left: centerX - radius,
                top: centerY - radius,
                width: radius * 2.0,
                height: radius * 2.0,
                child: new DecoratedBox(
                    decoration: new BoxDecoration(
                        Color: _backgroundToColor,
                        BorderRadius: BorderRadius.Circular(radius))));
        }

        private int ResolveTileFlex(double selectionValue, BottomNavigationBarType type)
        {
            if (type != BottomNavigationBarType.Shifting)
            {
                return 1;
            }

            var weight = ResolveTileWeight(selectionValue, type);
            return Math.Max(1, (int)Math.Round(weight * FlexScale));
        }

        private static double ResolveTileWeight(double selectionValue, BottomNavigationBarType type)
        {
            if (type != BottomNavigationBarType.Shifting)
            {
                return 1.0;
            }

            return 1.0 + (SelectedFlexBonus * selectionValue);
        }

        private static double ResolveLabelOpacity(double selectionValue, bool showSelectedLabels, bool showUnselectedLabels)
        {
            if (showSelectedLabels && showUnselectedLabels)
            {
                return 1.0;
            }

            if (showSelectedLabels && !showUnselectedLabels)
            {
                return selectionValue;
            }

            if (!showSelectedLabels && showUnselectedLabels)
            {
                return 1.0 - selectionValue;
            }

            return 0.0;
        }

        private double ResolveSelectionValue(int index)
        {
            if (index >= 0 && index < _selectionControllers.Count)
            {
                return _selectionControllers[index].Evaluate();
            }

            return index == CurrentWidget.CurrentIndex ? 1.0 : 0.0;
        }

        private double ResolveBackgroundCenterX(double barWidth, BottomNavigationBarType type)
        {
            var itemCount = CurrentWidget.Items.Count;
            if (itemCount <= 0)
            {
                return barWidth / 2.0;
            }

            var originIndex = Math.Clamp(_backgroundOriginIndex, 0, itemCount - 1);
            var totalWeight = 0.0;
            for (var i = 0; i < itemCount; i++)
            {
                totalWeight += ResolveTileWeight(ResolveSelectionValue(i), type);
            }

            if (totalWeight <= 0)
            {
                return barWidth / 2.0;
            }

            var leadingWeight = 0.0;
            for (var i = 0; i < originIndex; i++)
            {
                leadingWeight += ResolveTileWeight(ResolveSelectionValue(i), type);
            }

            var originWeight = ResolveTileWeight(ResolveSelectionValue(originIndex), type);
            var centerFraction = (leadingWeight + (originWeight / 2.0)) / totalWeight;
            return centerFraction * barWidth;
        }

        private double ResolveBarWidth(BuildContext context)
        {
            var mediaSize = MediaQuery.MaybeOf(context)?.Size;
            if (mediaSize.HasValue && mediaSize.Value.Width > 0)
            {
                return mediaSize.Value.Width;
            }

            return CurrentWidget.Items.Count * 96.0;
        }

        private Color ResolveCurrentAnimatedBackgroundColor()
        {
            if (_backgroundController is null)
            {
                return _backgroundToColor;
            }

            var t = _backgroundController.Evaluate();
            return _colorTween.Evaluate(t, _backgroundFromColor, _backgroundToColor);
        }

        private int ResolveBackgroundOriginIndex()
        {
            if (_pendingTapIndex.HasValue
                && _pendingTapIndex.Value >= 0
                && _pendingTapIndex.Value < CurrentWidget.Items.Count)
            {
                return _pendingTapIndex.Value;
            }

            return Math.Clamp(CurrentWidget.CurrentIndex, 0, CurrentWidget.Items.Count - 1);
        }

        private void HandleTileTap(int index)
        {
            _pendingTapIndex = index;
            CurrentWidget.OnTap?.Invoke(index);
        }

        private void HandleAnimationTick()
        {
            SetState(() => { });
        }

        private void HandleBackgroundCompleted()
        {
            SetState(() =>
            {
                _backgroundFromColor = _backgroundToColor;
            });
        }

        private static void SetControllerValue(AnimationController controller, double target)
        {
            controller.Forward(from: target);
            controller.Stop();
        }

        private static double Lerp(double from, double to, double t)
        {
            return from + ((to - from) * Math.Clamp(t, 0, 1));
        }
    }

    private BottomNavigationBarType ResolveEffectiveType(BottomNavigationBarThemeData bottomTheme)
    {
        return Type
               ?? bottomTheme.Type
               ?? (Items.Count <= 3
                   ? BottomNavigationBarType.Fixed
                   : BottomNavigationBarType.Shifting);
    }

    private Color ResolveBackgroundColor(
        ThemeData theme,
        BottomNavigationBarThemeData bottomTheme,
        BottomNavigationBarType effectiveType)
    {
        if (effectiveType == BottomNavigationBarType.Shifting)
        {
            var shiftingBackground = Items[CurrentIndex].BackgroundColor;
            if (shiftingBackground.HasValue)
            {
                return shiftingBackground.Value;
            }
        }

        return BackgroundColor
               ?? bottomTheme.BackgroundColor
               ?? theme.CanvasColor;
    }

    private static TextStyle ResolveLabelStyle(
        TextStyle baseStyle,
        TextStyle? overrideStyle,
        double fallbackFontSize,
        Color fallbackColor)
    {
        overrideStyle ??= new TextStyle();
        return new TextStyle(
            FontFamily: overrideStyle.FontFamily ?? baseStyle.FontFamily,
            FontSize: overrideStyle.FontSize ?? fallbackFontSize,
            Color: overrideStyle.Color ?? fallbackColor,
            FontWeight: overrideStyle.FontWeight ?? baseStyle.FontWeight,
            FontStyle: overrideStyle.FontStyle ?? baseStyle.FontStyle,
            Height: overrideStyle.Height ?? baseStyle.Height,
            LetterSpacing: overrideStyle.LetterSpacing ?? baseStyle.LetterSpacing);
    }

    private static IconThemeData ResolveIconTheme(
        IconThemeData? iconTheme,
        Color fallbackColor,
        double fallbackSize)
    {
        return new IconThemeData(
            Color: iconTheme?.Color ?? fallbackColor,
            Size: iconTheme?.Size ?? fallbackSize);
    }

    private static void ValidateIconTheme(string paramName, IconThemeData? iconTheme)
    {
        if (iconTheme?.Size is not double size)
        {
            return;
        }

        if (!double.IsFinite(size) || size < 0)
        {
            throw new ArgumentOutOfRangeException(paramName, "Icon theme size must be finite and non-negative.");
        }
    }

    private static Color ResolveDefaultSelectedColor(ThemeData theme, BottomNavigationBarType type)
    {
        if (type == BottomNavigationBarType.Shifting)
        {
            return Colors.White;
        }

        return theme.PrimaryColor;
    }

    private static Color ResolveDefaultUnselectedColor(ThemeData theme, BottomNavigationBarType type)
    {
        if (type == BottomNavigationBarType.Shifting)
        {
            return MaterialButtonCore.ApplyOpacity(Colors.White, 0.70);
        }

        return theme.UseMaterial3
            ? theme.OnSurfaceVariantColor
            : MaterialButtonCore.ApplyOpacity(theme.OnSurfaceColor, 0.60);
    }

    private static BoxShadows? BuildBoxShadows(Color shadowColor, double elevation)
    {
        if (elevation <= 0 || shadowColor.A == 0)
        {
            return null;
        }

        var keyShadow = new BoxShadow
        {
            OffsetX = 0,
            OffsetY = Math.Max(1, Math.Round(elevation * 0.5)),
            Blur = Math.Max(2, elevation * 2.4),
            Spread = 0,
            Color = MaterialButtonCore.ApplyOpacity(shadowColor, 0.20),
            IsInset = false,
        };

        var ambientShadow = new BoxShadow
        {
            OffsetX = 0,
            OffsetY = Math.Max(1, Math.Round(elevation * 0.5)),
            Blur = Math.Max(3, elevation * 3.2),
            Spread = 0,
            Color = MaterialButtonCore.ApplyOpacity(shadowColor, 0.14),
            IsInset = false,
        };

        return new BoxShadows(keyShadow, [ambientShadow]);
    }

    private static Widget CreateLabel(string label, TextStyle style)
    {
        return new Text(
            label,
            fontFamily: style.FontFamily,
            fontSize: style.FontSize,
            color: style.Color,
            fontWeight: style.FontWeight,
            fontStyle: style.FontStyle,
            height: style.Height,
            letterSpacing: style.LetterSpacing,
            softWrap: false,
            maxLines: 1,
            overflow: TextOverflow.Clip);
    }

    private static SemanticsFlags ResolveTileSemanticsFlags(bool selected, bool enabled)
    {
        var flags = SemanticsFlags.IsButton;
        if (selected)
        {
            flags |= SemanticsFlags.IsSelected;
        }

        if (enabled)
        {
            flags |= SemanticsFlags.IsEnabled;
        }

        return flags;
    }

    private static string CreateIndexSemanticsLabel(BuildContext context, int index, int count)
    {
        return MaterialLocalizations.Of(context).TabLabel(index, count);
    }
}
