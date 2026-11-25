using System.Net.Http.Json;
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
        // 未配对（无 BaseAddress）时不抛异常，返回明确标记，避免 UI 误判崩溃。
        if (_client.BaseAddress is null)
            return new AppSnapshot { Version = "unpaired", Games = Array.Empty<GameStatus>() };
        var token = await ReadPairTokenAsync();
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/mobile/snapshot");
        if (!string.IsNullOrEmpty(token))
            req.Headers.Add("Authorization", "Bearer " + token);
        var r = await _client.SendAsync(req, ct);
        r.EnsureSuccessStatusCode();
        var body = await r.Content.ReadFromJsonAsync<SnapshotResponse>(ct);
        return new AppSnapshot
        {
            Version = body?.version ?? "",
            Games = new ReadOnlyCollection<GameStatus>(
                (body?.games ?? new List<GameDto>()).Select(g => new GameStatus
                {
                    GameId = g.id, Name = g.name, Max = 0, Current = 0
                }).ToList())
        };
    }

    public Task<IReadOnlyList<DailyItem>> GetDailiesAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<DailyItem>>(Array.Empty<DailyItem>());

    public Task<CheckInResult> CheckInAsync(string itemId, string date, CancellationToken ct = default) =>
        Task.FromResult(new CheckInResult { Ok = false, Message = "M1 未接" });

    private sealed class PairResponse
    {
        public bool ok { get; set; }
        public string? token { get; set; }
    }

    private sealed class SnapshotResponse
    {
        public bool ok { get; set; }
        public string? version { get; set; }
        public List<GameDto>? games { get; set; }
    }

    private sealed class GameDto
    {
        public string id { get; set; } = "";
        public string name { get; set; } = "";
    }
}
