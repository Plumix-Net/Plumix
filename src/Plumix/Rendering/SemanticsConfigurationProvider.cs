// Dart parity source: flutter/packages/flutter/lib/src/rendering/object.dart

namespace Plumix.Rendering;

/// <summary>
/// Caches the <see cref="SemanticsConfiguration"/> a render object describes, and hands out a
/// writable copy the moment the compiler needs to absorb something into it.
/// </summary>
/// <remarks>Flutter's private <c>_SemanticsConfigurationProvider</c>.</remarks>
internal sealed class SemanticsConfigurationProvider
{
    private readonly Action<SemanticsConfiguration> _describe;
    private readonly Action<SemanticsConfiguration> _validate;
    private SemanticsConfiguration? _originalConfiguration;
    private SemanticsConfiguration? _effectiveConfiguration;
    private bool _isEffectiveConfigWritable;
    private bool _wasSemanticsBoundary;

    public SemanticsConfigurationProvider(
        Action<SemanticsConfiguration> describe,
        Action<SemanticsConfiguration> validate)
    {
        _describe = describe;
        _validate = validate;
    }

    /// <summary>Whether a configuration has already been described since the last <see cref="Clear"/>.</summary>
    public bool HasConfiguration => _originalConfiguration != null;

    /// <summary>
    /// Whether the configuration that was last cached declared itself a semantics boundary.
    /// </summary>
    /// <remarks>Flutter's <c>_SemanticsConfigurationProvider.wasSemanticsBoundary</c>.</remarks>
    public bool WasSemanticsBoundary => _wasSemanticsBoundary;

    /// <summary>The configuration exactly as the render object described it.</summary>
    public SemanticsConfiguration Original
    {
        get
        {
            if (_originalConfiguration == null)
            {
                var configuration = new SemanticsConfiguration();
                _describe(configuration);
                _validate(configuration);
                _originalConfiguration = configuration;
                _wasSemanticsBoundary = configuration.IsSemanticBoundary;
            }

            return _originalConfiguration;
        }
    }

    /// <summary>
    /// The configuration after the compiler absorbed the merge-up fragments below this render object.
    /// </summary>
    public SemanticsConfiguration Effective => _effectiveConfiguration ??= Original;

    /// <summary>
    /// Runs <paramref name="callback"/> against a writable copy of <see cref="Effective"/>, cloning
    /// <see cref="Original"/> on the first write so the described configuration stays pristine.
    /// </summary>
    public void UpdateConfig(Action<SemanticsConfiguration> callback)
    {
        if (!_isEffectiveConfigWritable)
        {
            _effectiveConfiguration = Original.Clone();
            _isEffectiveConfigWritable = true;
        }

        callback(_effectiveConfiguration!);
    }

    /// <summary>Absorbs every configuration in <paramref name="configurations"/> into the effective one.</summary>
    public void AbsorbAll(IEnumerable<SemanticsConfiguration> configurations)
    {
        using IEnumerator<SemanticsConfiguration> enumerator = configurations.GetEnumerator();
        if (!enumerator.MoveNext())
        {
            return;
        }

        UpdateConfig(configuration =>
        {
            do
            {
                configuration.Absorb(enumerator.Current);
            }
            while (enumerator.MoveNext());
        });
    }

    /// <summary>Drops the absorbed data, keeping the described configuration.</summary>
    public void Reset()
    {
        _effectiveConfiguration = Original;
        _isEffectiveConfigWritable = false;
    }

    /// <summary>
    /// Drops every cache, so the render object is asked to describe its configuration again.
    /// </summary>
    public void Clear()
    {
        _isEffectiveConfigWritable = false;
        _effectiveConfiguration = null;
        _originalConfiguration = null;
    }
}
