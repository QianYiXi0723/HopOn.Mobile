using System.Net;
using System.Net.Http.Json;
using System.Text;
using HopOn.Mobile.Core;
using Xunit;

namespace HopOn.Mobile.Tests;

public class PcRemoteDataSourceHttpTests
{
    // 假 Handler：记录请求路径与 Authorization 头，按路径返回固定 JSON。
    private sealed class FakeHandler : HttpMessageHandler
    {
        public string? LastAuth;
        public string? LastPath;
        public string? LastMethod;
        public string? LastBody;
        public string? LastHost;
        public bool PlannedToday = true;   // 控制 snapshot 里 custom_dailies 的 planned_today
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct)
        {
            LastHost = request.RequestUri?.Authority;
            LastPath = request.RequestUri?.PathAndQuery;
            LastAuth = request.Headers.TryGetValues("Authorization", out var v)
                ? v.FirstOrDefault() : null;
            LastMethod = request.Method.Method;
            LastBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
            var body = LastPath?.StartsWith("/pair") == true
                ? "{\"ok\":true,\"token\":\"tok-123\"}"
                : LastPath?.StartsWith("/api/mobile/checkin") == true
                ? "{\"ok\":true,\"message\":\"ok\"}"
                : LastPath?.StartsWith("/api/mobile/dailies") == true
                ? "{\"ok\":true,\"items\":[{\"id\":\"d1\",\"name\":\"喝水\",\"done\":false}]}"
                : "{\"ok\":true,\"version\":\"m1\",\"games\":[{\"id\":\"genshin\",\"name\":\"原神\",\"stamina_name\":\"原粹树脂\",\"current\":25,\"max\":200,\"daily_done\":4,\"daily_total\":4,\"daily_progress\":\"4/4\",\"finished\":true}],\"custom_dailies\":[{\"id\":\"d1\",\"name\":\"喝水\",\"done\":false,\"planned_today\":" + (PlannedToday ? "true" : "false") + ",\"times\":[\"08:30\"]}]}";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        }
    }

    [Fact]
    public async Task Redeem_Code_Stores_Token()
    {
        var handler = new FakeHandler();
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:8766") };
        var store = new InMemorySecureStore();
        var ds = new PcRemoteDataSource(store, client);

        var ok = await ds.RedeemCodeAsync("ABC234");

        Assert.True(ok);
        Assert.Equal("/pair?code=ABC234", handler.LastPath);
        Assert.Equal("tok-123", await store.GetAsync("hopon.pair_token"));
    }

    [Fact]
    public async Task Snapshot_Uses_Stored_Bearer_Token()
    {
        var handler = new FakeHandler();
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:8766") };
        var store = new InMemorySecureStore();
        await store.SetAsync("hopon.pair_token", "tok-123");
        var ds = new PcRemoteDataSource(store, client);

        var snap = await ds.GetSnapshotAsync();

        Assert.Equal("m1", snap.Version);
        Assert.Equal("Bearer tok-123", handler.LastAuth);
        Assert.Single(snap.Games);
        Assert.Equal("genshin", snap.Games[0].GameId);
    }

    [Fact]
    public async Task No_Token_Snapshot_Sends_No_Auth()
    {
        var handler = new FakeHandler();
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:8766") };
        var ds = new PcRemoteDataSource(new InMemorySecureStore(), client);

        await ds.GetSnapshotAsync();

        Assert.Null(handler.LastAuth);
    }

    [Fact]
    public async Task Snapshot_Has_Real_Stamina_Daily_And_Custom()
    {
        var handler = new FakeHandler();
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:8766") };
        var ds = new PcRemoteDataSource(new InMemorySecureStore(), client);

        var snap = await ds.GetSnapshotAsync();

        Assert.Equal(25, snap.Games[0].Current);
        Assert.Equal(200, snap.Games[0].Max);
        Assert.Equal("原粹树脂", snap.Games[0].StaminaName);
        Assert.Equal(4, snap.Games[0].DailyDone);
        Assert.Equal(4, snap.Games[0].DailyTotal);
        Assert.Single(snap.CustomDailies);
        Assert.Equal("喝水", snap.CustomDailies[0].Name);
    }

    [Fact]
    public async Task CheckIn_Posts_Id_And_Bearer()
    {
        var handler = new FakeHandler();
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:8766") };
        var store = new InMemorySecureStore();
        await store.SetAsync("hopon.pair_token", "tok-123");
        var ds = new PcRemoteDataSource(store, client);

        var r = await ds.CheckInAsync("d1", "2026-11-25");

        Assert.True(r.Ok);
        Assert.Equal("POST", handler.LastMethod);
        Assert.Equal("Bearer tok-123", handler.LastAuth);
        Assert.Contains("\"id\":\"d1\"", handler.LastBody);
    }

    [Fact]
    public async Task ApplyPairUrl_Sets_Base_And_Redeems_Code()
    {
        var handler = new FakeHandler();
        var client = new HttpClient(handler);  // 无 BaseAddress，必须靠 ApplyPairUrl 设置
        var store = new InMemorySecureStore();
        var ds = new PcRemoteDataSource(store, client);

        var ok = await ds.ApplyPairUrl("http://192.168.1.50:8766/pair?code=ABC234");

        Assert.True(ok);
        Assert.Equal("/pair?code=ABC234", handler.LastPath);
        Assert.Equal("tok-123", await store.GetAsync("hopon.pair_token"));
    }

    [Fact]
    public async Task ApplyPairUrl_Hopon_Scheme_Parses_Ip_Port_Code()
    {
        var handler = new FakeHandler();
        var client = new HttpClient(handler);
        var store = new InMemorySecureStore();
        var ds = new PcRemoteDataSource(store, client);

        // 新格式：自定义协议，手机扫了不会跳浏览器
        var ok = await ds.ApplyPairUrl("hopon://pair?ip=10.253.1.5&port=8766&code=ABC234");

        Assert.True(ok);
        Assert.Equal("/pair?code=ABC234", handler.LastPath);
        Assert.Equal("tok-123", await store.GetAsync("hopon.pair_token"));
    }

    [Fact]
    public async Task DiscoveredIp_PlusTypedCode_Pairs()
    {
        // 新流程：UDP 发现拿到 IP → 手机手输 6 位码，不再靠二维码
        var handler = new FakeHandler();
        var client = new HttpClient(handler);
        var store = new InMemorySecureStore();
        var ds = new PcRemoteDataSource(store, client);

        var pc = new DiscoveredPc("10.253.2.7", 8766);
        ds.SetBaseUrl($"http://{pc.Ip}:{pc.Port}");
        var ok = await ds.RedeemCodeAsync("W5GY73");

        Assert.True(ok);
        Assert.Equal("10.253.2.7:8766", handler.LastHost);
        Assert.Equal("/pair?code=W5GY73", handler.LastPath);
        Assert.Equal("tok-123", await store.GetAsync("hopon.pair_token"));
    }

    [Fact]
    public async Task Snapshot_Parses_PlannedToday()
    {
        var handler = new FakeHandler { PlannedToday = false };
        var client = new HttpClient(handler);
        var ds = new PcRemoteDataSource(new InMemorySecureStore(), client);
        ds.SetBaseUrl("http://127.0.0.1:8766");

        var snap = await ds.GetSnapshotAsync();

        Assert.Single(snap.CustomDailies);
        Assert.False(snap.CustomDailies[0].PlannedToday);
    }
}
