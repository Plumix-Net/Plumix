// Dart parity source (reference): flutter/packages/flutter/lib/src/rendering/semantics.dart (approximate)

namespace Plumix.Rendering;

internal sealed class SemanticsConfigurationProvider
{
    private readonly Action<SemanticsConfiguration> _describe;
    private readonly Action<SemanticsConfiguration> _validate;
    private SemanticsConfiguration? _original;
    private SemanticsConfiguration? _effective;

    public SemanticsConfigurationProvider(
        Action<SemanticsConfiguration> describe,
        Action<SemanticsConfiguration> validate)
    {
        _describe = describe;
        _validate = validate;
    }

    public bool HasEffective => _effective != null;

    public SemanticsConfiguration? TryGetCachedEffective()
    {
        return _effective;
    }

    public SemanticsConfiguration Original
    {
        get
        {
            EnsureInitialized();
            return _original!;
        }
    }

    public SemanticsConfiguration Effective
    {
        get
        {
            EnsureInitialized();
            return _effective!;
        }
    }

    public SemanticsConfiguration Snapshot()
    {
        var configuration = new SemanticsConfiguration();
        _describe(configuration);
        _validate(configuration);
        return configuration;
    }

    public void Reset()
    {
        _original = null;
        _effective = null;
    }

    public void UpdateConfig(Action<SemanticsConfiguration> callback)
    {
        callback(Effective);
    }

    public void AbsorbAll(IEnumerable<SemanticsConfiguration> configurations)
    {
        UpdateConfig(configuration =>
        {
            foreach (var fragment in configurations)
            {
                configuration.Absorb(fragment);
            }
        });
    }

    private void EnsureInitialized()
    {
        if (_effective != null && _original != null)
        {
            return;
        }

        var original = new SemanticsConfiguration();
        _describe(original);
        _validate(original);
        _original = original;
        _effective = original.Clone();
    }
}
