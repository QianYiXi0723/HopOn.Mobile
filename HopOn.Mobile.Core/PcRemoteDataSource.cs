using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Collections.ObjectModel;

namespace HopOn.Mobile.Core;

public sealed class PcRemoteDataSource : IHopOnDataSource
{
    private const string TokenKey = "hopon.pair_token";
    private readonly ISecureStore _store;
    private readonly HttpClient _client;

    public PcRemoteDataSource(ISecureStore store, HttpClient? client = null)
    {
        _store = store;
        _client = client ?? new HttpClient();
    }

    public void SetBaseUrl(string url) => _client.BaseAddress = new Uri(url.TrimEnd('/'));
    public Task SavePairTokenAsync(string token) => _store.SetAsync(TokenKey, token);
    public Task<string?> ReadPairTokenAsync() => _store.GetAsync(TokenKey);

    /// <summary>
    /// 手机扫到的配对串。新格式（自定义协议，避免被浏览器劫持）：
    /// hopon://pair?ip=1.2.3.4&port=8766&code=XXX
    /// 旧格式（兼容）：http://ip:port/pair?code=XXX
    /// </summary>
    public async Task<bool> ApplyPairUrl(string url)
    {
        var q = new Dictionary<string, string>();
        var query = url.Contains('?') ? url.Substring(url.IndexOf('?') + 1) : "";
        foreach (var p in query.Split('&'))
        {
            var kv = p.Split('=', 2);
            if (kv.Length == 2) q[kv[0]] = Uri.UnescapeDataString(kv[1]);
        }
        string baseUrl;
        if (url.StartsWith("hopon://", StringComparison.OrdinalIgnoreCase))
        {
            if (!q.TryGetValue("ip", out var ip) || string.IsNullOrEmpty(ip)) return false;
            var port = q.GetValueOrDefault("port", "8766");
            baseUrl = $"http://{ip}:{port}";
        }
        else
        {
            // 旧格式兼容：从 http://ip:port/... 取 scheme+authority
            var u = new Uri(url);
            baseUrl = $"{u.Scheme}://{u.Authority}";
        }
        if (!q.TryGetValue("code", out var code) || string.IsNullOrEmpty(code)) return false;
        SetBaseUrl(baseUrl);
        return await RedeemCodeAsync(code);
    }

    public async Task<bool> RedeemCodeAsync(string code)
    {
        var r = await _client.GetAsync($"/pair?code={Uri.EscapeDataString(code)}");
        if (!r.IsSuccessStatusCode) return false;
        var body = await r.Content.ReadFromJsonAsync<PairResponse>();
        if (body?.ok != true || string.IsNullOrEmpty(body.token)) return false;
        await SavePairTokenAsync(body.token!);
        return true;
    }

    public async Task<AppSnapshot> GetSnapshotAsync(CancellationToken ct = default)
    {
        if (_client.BaseAddress is null)
            return new AppSnapshot { Version = "unpaired", Games = Array.Empty<GameStatus>() };
        var token = await ReadPairTokenAsync();
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/mobile/snapshot");
        if (!string.IsNullOrEmpty(token)) req.Headers.Add("Authorization", "Bearer " + token);
        var r = await _client.SendAsync(req, ct);
        r.EnsureSuccessStatusCode();
        var body = await r.Content.ReadFromJsonAsync<SnapshotResponse>(ct);
        var games = new ReadOnlyCollection<GameStatus>(
            (body?.games ?? new List<GameDto>()).Select(g => new GameStatus
            {
                GameId = g.id, Name = g.name, StaminaName = g.stamina_name,
                Current = g.current, Max = g.max,
                DailyDone = g.daily_done, DailyTotal = g.daily_total,
                DailyProgress = g.daily_progress, Finished = g.finished, Stale = g.stale
            }).ToList());
        var customs = new ReadOnlyCollection<DailyItem>(
            (body?.custom_dailies ?? new List<DailyDto>()).Select(d => new DailyItem
            {
                Id = d.id, Name = d.name, Done = d.done,
                Times = new ReadOnlyCollection<string>(d.times ?? new List<string>())
            }).ToList());
        return new AppSnapshot { Version = body?.version ?? "", Games = games, CustomDailies = customs };
    }

    public async Task<IReadOnlyList<DailyItem>> GetDailiesAsync(CancellationToken ct = default)
    {
        if (_client.BaseAddress is null) return Array.Empty<DailyItem>();
        var token = await ReadPairTokenAsync();
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/mobile/dailies");
        if (!string.IsNullOrEmpty(token)) req.Headers.Add("Authorization", "Bearer " + token);
        var r = await _client.SendAsync(req, ct);
        r.EnsureSuccessStatusCode();
        var body = await r.Content.ReadFromJsonAsync<DailiesResponse>(ct);
        return new ReadOnlyCollection<DailyItem>(
            (body?.items ?? new List<DailyDto>()).Select(d => new DailyItem
            {
                Id = d.id, Name = d.name, Done = d.done,
                Times = new ReadOnlyCollection<string>(d.times ?? new List<string>())
            }).ToList());
    }

    public async Task<CheckInResult> CheckInAsync(string itemId, string date, CancellationToken ct = default)
    {
        if (_client.BaseAddress is null) return new CheckInResult { Ok = false, Message = "未配对" };
        var token = await ReadPairTokenAsync();
        var payload = JsonSerializer.Serialize(new { id = itemId, date });
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/mobile/checkin")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        if (!string.IsNullOrEmpty(token)) req.Headers.Add("Authorization", "Bearer " + token);
        var r = await _client.SendAsync(req, ct);
        var body = await r.Content.ReadFromJsonAsync<CheckInResult>(ct);
        return body ?? new CheckInResult { Ok = false, Message = "无响应" };
    }

    private sealed class PairResponse { public bool ok { get; set; } public string? token { get; set; } }
    private sealed class SnapshotResponse
    {
        public bool ok { get; set; }
        public string? version { get; set; }
        public List<GameDto>? games { get; set; }
        public List<DailyDto>? custom_dailies { get; set; }
    }
    private sealed class DailiesResponse { public bool ok { get; set; } public List<DailyDto>? items { get; set; } }
    private sealed class GameDto
    {
        public string id { get; set; } = ""; public string name { get; set; } = "";
        public string stamina_name { get; set; } = "";
        public int current { get; set; } public int max { get; set; }
        public int daily_done { get; set; } public int daily_total { get; set; }
        public string daily_progress { get; set; } = "";
        public bool finished { get; set; } public bool stale { get; set; }
    }
    private sealed class DailyDto
    {
        public string id { get; set; } = ""; public string name { get; set; } = "";
        public bool done { get; set; } public List<string>? times { get; set; }
    }
}
