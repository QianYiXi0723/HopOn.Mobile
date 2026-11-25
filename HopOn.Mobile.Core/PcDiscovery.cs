using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace HopOn.Mobile.Core;

public sealed record DiscoveredPc(string Ip, int Port);

/// <summary>
/// 局域网设备发现：广播约定帧，收集 PC 回应（只含 IP+端口）。
/// 发现只负责拿地址，不建立信任——配对靠 6 位码（见 PcRemoteDataSource.RedeemCodeAsync）。
/// </summary>
public static class PcDiscovery
{
    public const string Magic = "HOPON_DISCOVER_V1";
    public const int DiscoveryPort = 8767;

    public static DiscoveredPc ParseResponse(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var ip = root.GetProperty("ip").GetString();
            if (string.IsNullOrEmpty(ip)) throw new FormatException("missing ip");
            var port = root.GetProperty("port").GetInt32();
            return new DiscoveredPc(ip, port);
        }
        catch (JsonException e)
        {
            throw new FormatException("bad discovery frame", e);
        }
        catch (KeyNotFoundException e)
        {
            throw new FormatException("missing ip/port", e);
        }
    }

    public static async Task<IReadOnlyList<DiscoveredPc>> DiscoverAsync(int timeoutMs = 1500)
    {
        var found = new List<DiscoveredPc>();
        using var udp = new UdpClient { EnableBroadcast = true };
        var req = Encoding.UTF8.GetBytes(Magic);
        try
        {
            await udp.SendAsync(req, req.Length, new IPEndPoint(IPAddress.Broadcast, DiscoveryPort));
        }
        catch (SocketException)
        {
            return found;
        }

        using var cts = new CancellationTokenSource(timeoutMs);
        while (!cts.IsCancellationRequested)
        {
            try
            {
                var res = await udp.ReceiveAsync(cts.Token);
                found.Add(ParseResponse(Encoding.UTF8.GetString(res.Buffer)));
            }
            catch (OperationCanceledException) { break; }
            catch (SocketException) { break; }
            catch (FormatException) { /* 非本工具响应，忽略 */ }
            catch (JsonException) { /* 坏帧，忽略 */ }
        }
        return found.Distinct().ToList();
    }
}
