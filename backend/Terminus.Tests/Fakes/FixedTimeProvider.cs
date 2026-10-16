namespace Terminus.Tests.Fakes;

/// <summary>Sat koji uvijek pokazuje isto vrijeme.</summary>
internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now.ToUniversalTime();
}
