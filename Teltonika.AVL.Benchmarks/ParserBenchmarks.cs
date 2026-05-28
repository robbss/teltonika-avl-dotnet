using BenchmarkDotNet.Attributes;
using Teltonika.AVL.Protocol;

namespace Teltonika.AVL.Benchmarks;

[MemoryDiagnoser]
public class ParserBenchmarks
{
    private byte[] _codec8ExtendedPayload = default!;
    private Codec8ExtendedParser _extendedParser = default!;

    [GlobalSetup]
    public void Setup()
    {
        string hex = "000000000000004A8E010000016B412CEE000100000000000000000000000000000000010005000100010100010011001D00010010015E2C880002000B000000003544C87A000E000000001DD7E06A00000100002994";
        byte[] fullPacket = ConvertHexStringToByteArray(hex);

        // Extract just the payload that the parser receives (excluding preamble, length, and CRC)
        _codec8ExtendedPayload = [.. fullPacket.Skip(8).Take(fullPacket.Length - 12)];
        _extendedParser = new Codec8ExtendedParser();
    }

    [Benchmark]
    public void ParseCodec8Extended()
    {
        _ = _extendedParser.TryParse(_codec8ExtendedPayload, out _);
    }

    private static byte[] ConvertHexStringToByteArray(string hex)
    {
        return [.. Enumerable.Range(0, hex.Length)
                         .Where(x => x % 2 == 0)
                         .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))];
    }
}