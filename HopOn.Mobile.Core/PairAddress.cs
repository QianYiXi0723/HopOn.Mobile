namespace HopOn.Mobile.Core;

/// <summary>配对地址选择：优先用发现到的设备，否则用手输的 IP（端口固定 8766）。</summary>
public static class PairAddress
{
    public const int DefaultPort = 8766;

    public static string? Resolve(DiscoveredPc? selected, string? typedIp)
    {
        if (selected is not null && !string.IsNullOrWhiteSpace(selected.Ip))
            return $"http://{selected.Ip}:{selected.Port}";
        var ip = typedIp?.Trim();
        return string.IsNullOrEmpty(ip) ? null : $"http://{ip}:{DefaultPort}";
    }
}
