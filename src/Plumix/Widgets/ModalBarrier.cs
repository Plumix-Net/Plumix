using Avalonia;
using Avalonia.Media;
using Plumix.Foundation;
using Plumix.Rendering;
using Plumix.UI;

namespace Plumix.Widgets;

// Dart parity source: flutter/packages/flutter/lib/src/widgets/modal_barrier.dart

internal sealed class RenderSemanticsClipper : RenderProxyBox
{
    private ValueNotifier<Thickness> _clipDetailsNotifier;

    public RenderSemanticsClipper(
        ValueNotifier<Thickness> clipDetailsNotifier,
        RenderBox? child = null)
    {
        _clipDetailsNotifier = clipDetailsNotifier ?? throw new ArgumentNullException(nameof(clipDetailsNotifier));
        Child = child;
    }

    public ValueNotifier<Thickness> ClipDetailsNotifier
    {
        get => _clipDetailsNotifier;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (ReferenceEquals(_clipDetailsNotifier, value))
            {
                return;
            }

            if (Attached)
            {
                _clipDetailsNotifier.RemoveListener(MarkNeedsSemanticsUpdate);
            }

            _clipDetailsNotifier = value;
            if (Attached)
            {
                _clipDetailsNotifier.AddListener(MarkNeedsSemanticsUpdate);
            }

            MarkNeedsSemanticsUpdate();
        }
    }

    protected override Rect SemanticBounds
    {
        get
        {
            Thickness clipDetails = _clipDetailsNotifier.Value;
            Rect originalRect = base.SemanticBounds;
            double width = Math.Max(0, originalRect.Width - clipDetails.Left - clipDetails.Right);
            double height = Math.Max(0, originalRect.Height - clipDetails.Top - clipDetails.Bottom);
            return new Rect(
                originalRect.Left + clipDetails.Left,
                originalRect.Top + clipDetails.Top,
                width,
                height);
        }
    }

    protected override void OnAttach()
    {
        base.OnAttach();
        _clipDetailsNotifier.AddListener(MarkNeedsSemanticsUpdate);
    }

    protected override void OnDetach()
    {
        _clipDetailsNotifier.RemoveListener(MarkNeedsSemanticsUpdate);
        base.OnDetach();
    }

    protected override void DescribeSemanticsConfiguration(SemanticsConfiguration configuration)
    {
        base.DescribeSemanticsConfiguration(configuration);
        configuration.IsSemanticBoundary = true;
    }
}

internal sealed class SemanticsClipper : SingleChildRenderObjectWidget
{
    public SemanticsClipper(
        ValueNotifier<Thickness> clipDetailsNotifier,
        Widget? child = null,
        Key? key = null) : base(child, key)
    {
        ClipDetailsNotifier = clipDetailsNotifier
                              ?? throw new ArgumentNullException(nameof(clipDetailsNotifier));
    }

    public ValueNotifier<Thickness> ClipDetailsNotifier { get; }

    public override RenderObject CreateRenderObject(BuildContext context)
    {
        return new RenderSemanticsClipper(ClipDetailsNotifier);
    }

    public override void UpdateRenderObject(BuildContext context, RenderObject renderObject)
    {
        ((RenderSemanticsClipper)renderObject).ClipDetailsNotifier = ClipDetailsNotifier;
    }
}

public sealed class ModalBarrier : StatelessWidget
{
    public ModalBarrier(
        Color? color = null,
        bool dismissible = true,
        Action? onDismiss = null,
        string? semanticsLabel = null,
        bool? barrierSemanticsDismissible = true,
        ValueNotifier<Thickness>? clipDetailsNotifier = null,
        string? semanticsOnTapHint = null,
        Key? key = null) : base(key)
    {
        Color = color;
        Dismissible = dismissible;
        OnDismiss = onDismiss;
        SemanticsLabel = semanticsLabel;
        BarrierSemanticsDismissible = barrierSemanticsDismissible;
        ClipDetailsNotifier = clipDetailsNotifier;
        SemanticsOnTapHint = semanticsOnTapHint;
    }

    public Color? Color { get; }

    public bool Dismissible { get; }

    public Action? OnDismiss { get; }

    public string? SemanticsLabel { get; }

    public bool? BarrierSemanticsDismissible { get; }

    public ValueNotifier<Thickness>? ClipDetailsNotifier { get; }

    public string? SemanticsOnTapHint { get; }

    public override Widget Build(BuildContext context)
    {
        bool platformSupportsDismissingBarrier =
            PlatformSupportsDismissingBarrier(PlatformDefaults.TargetPlatform);
        bool semanticsDismissible = Dismissible && platformSupportsDismissingBarrier;
        bool modalBarrierSemanticsDismissible = BarrierSemanticsDismissible ?? semanticsDismissible;

        void HandleDismiss()
        {
            if (Dismissible)
            {
                if (OnDismiss != null)
                {
                    OnDismiss();
                }
                else
                {
                    Navigator.MaybePop(context);
                }
            }
            else
            {
                SystemSound.Play(SystemSoundType.Alert);
            }
        }

        Widget barrier = new Semantics(
            onTapHint: SemanticsOnTapHint,
            onTap: semanticsDismissible && SemanticsLabel != null ? HandleDismiss : null,
            onDismiss: semanticsDismissible && SemanticsLabel != null ? HandleDismiss : null,
            label: semanticsDismissible ? SemanticsLabel : null,
            child: new MouseRegion(
                cursor: SystemMouseCursors.Basic,
                child: new ConstrainedBox(
                    constraints: BoxConstraints.Expand(),
                    child: Color.HasValue ? new ColoredBox(Color.Value) : null)));

        bool excluding = !semanticsDismissible || !modalBarrierSemanticsDismissible;
        if (!excluding && ClipDetailsNotifier != null)
        {
            barrier = new SemanticsClipper(
                clipDetailsNotifier: ClipDetailsNotifier,
                child: barrier);
        }

        return new BlockSemantics(
            child: new ExcludeSemantics(
                excluding: excluding,
                child: new GestureDetector(
                    behavior: HitTestBehavior.Opaque,
                    onTap: HandleDismiss,
                    onSecondaryTap: HandleDismiss,
                    child: barrier)));
    }

    internal static bool PlatformSupportsDismissingBarrier(TargetPlatform platform)
    {
        return platform is TargetPlatform.Android
            or TargetPlatform.IOS
            or TargetPlatform.MacOS;
    }
}

public sealed class AnimatedModalBarrier : AnimatedWidget
{
    public AnimatedModalBarrier(
        Animation<Color?> color,
        bool dismissible = true,
        string? semanticsLabel = null,
        bool? barrierSemanticsDismissible = null,
        Action? onDismiss = null,
        ValueNotifier<Thickness>? clipDetailsNotifier = null,
        string? semanticsOnTapHint = null,
        Key? key = null) : base(color ?? throw new ArgumentNullException(nameof(color)), key)
    {
        Dismissible = dismissible;
        SemanticsLabel = semanticsLabel;
        BarrierSemanticsDismissible = barrierSemanticsDismissible;
        OnDismiss = onDismiss;
        ClipDetailsNotifier = clipDetailsNotifier;
        SemanticsOnTapHint = semanticsOnTapHint;
    }

    public Animation<Color?> Color => (Animation<Color?>)Listenable;

    public bool Dismissible { get; }

    public string? SemanticsLabel { get; }

    public bool? BarrierSemanticsDismissible { get; }

    public Action? OnDismiss { get; }

    public ValueNotifier<Thickness>? ClipDetailsNotifier { get; }

    public string? SemanticsOnTapHint { get; }

    public override Widget Build(BuildContext context)
    {
        return new ModalBarrier(
            color: Color.Value,
            dismissible: Dismissible,
            semanticsLabel: SemanticsLabel,
            barrierSemanticsDismissible: BarrierSemanticsDismissible,
            onDismiss: OnDismiss,
            clipDetailsNotifier: ClipDetailsNotifier,
            semanticsOnTapHint: SemanticsOnTapHint);
    }
}
