using BenchmarkDotNet.Attributes;
using Teltonika.Avl.Elements;
using Teltonika.Avl.Elements.Models;

namespace Teltonika.Avl.Benchmarks;

[MemoryDiagnoser]
public class IoElementResolverBenchmarks
{
    [Benchmark]
    public ResolvedProperty? Resolve_Single() =>
        IoElementResolver.Resolve(BenchmarkData.KnownProperty);

    [Benchmark]
    public ResolvedProperty? Resolve_WithModel() =>
        IoElementResolver.Resolve(BenchmarkData.OverrideProperty, TrackerModel.TMT250);

    [Benchmark]
    public ResolvedProperty? Resolve_UnknownId() =>
        IoElementResolver.Resolve(BenchmarkData.UnknownProperty);

    [Benchmark]
    public IReadOnlyList<ResolvedProperty> ResolveAll_Small() =>
        IoElementResolver.ResolveAll(BenchmarkData.SmallIoElement);

    [Benchmark]
    public IReadOnlyList<ResolvedProperty> ResolveAll_Large() =>
        IoElementResolver.ResolveAll(BenchmarkData.LargeIoElement);

    [Benchmark]
    public IoElementDefinition? GetDefinition() =>
        IoElementResolver.GetDefinition(66);
}
