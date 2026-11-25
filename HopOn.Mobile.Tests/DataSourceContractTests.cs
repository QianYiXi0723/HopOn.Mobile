using System.Text.Json;
using HopOn.Mobile.Core;
using Xunit;

namespace HopOn.Mobile.Tests;

public class DataSourceContractTests
{
    private sealed class FakeSource : IHopOnDataSource
    {
        public Task<AppSnapshot> GetSnapshotAsync(CancellationToken ct = default) =>
            Task.FromResult(new AppSnapshot
            {
                Version = "0.1",
                Games = new[] { new GameStatus { GameId = "genshin", Name = "原神", Current = 120, Max = 200, IsRiskControlled = false } }
            });

        public Task<IReadOnlyList<DailyItem>> GetDailiesAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<DailyItem>>(new[] {
                new DailyItem { Id = "d1", Name = "喝水", Done = false, Times = new[] { "08:30", "20:30" } }
            });

        public Task<CheckInResult> CheckInAsync(string itemId, string date, CancellationToken ct = default) =>
            Task.FromResult(new CheckInResult { Ok = true, Message = "ok" });
    }

    [Fact]
    public async Task Source_Returns_Snapshot_With_Games()
    {
        IHopOnDataSource source = new FakeSource();
        var snap = await source.GetSnapshotAsync();
        Assert.Single(snap.Games);
        Assert.Equal("genshin", snap.Games[0].GameId);
    }

    [Fact]
    public async Task Snapshot_RoundTrips_Through_Json()
    {
        IHopOnDataSource source = new FakeSource();
        var snap = await source.GetSnapshotAsync();
        var json = JsonSerializer.Serialize(snap);
        var back = JsonSerializer.Deserialize<AppSnapshot>(json);
        Assert.NotNull(back);
        Assert.Equal(120, back!.Games[0].Current);
    }

    [Fact]
    public async Task CheckIn_Returns_Ok()
    {
        IHopOnDataSource source = new FakeSource();
        var r = await source.CheckInAsync("d1", "2026-11-25");
        Assert.True(r.Ok);
    }
}
