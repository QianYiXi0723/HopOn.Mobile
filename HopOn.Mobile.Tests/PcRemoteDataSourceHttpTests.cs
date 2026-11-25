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
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct)
        {
            LastPath = request.RequestUri?.PathAndQuery;
            LastAuth = request.Headers.TryGetValues("Authorization", out var v)
                ? v.FirstOrDefault() : null;
            var body = LastPath?.StartsWith("/pair") == true
                ? "{\"ok\":true,\"token\":\"tok-123\"}"
                : "{\"ok\":true,\"version\":\"m1\",\"games\":[{\"id\":\"genshin\",\"name\":\"原神\"}]}";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
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
}
