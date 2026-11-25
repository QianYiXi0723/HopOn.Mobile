using HopOn.Mobile.Core;
using Xunit;

namespace HopOn.Mobile.Tests;

public class PcRemoteDataSourceTests
{
    [Fact]
    public async Task Save_And_Read_Pair_Token()
    {
        ISecureStore store = new InMemorySecureStore();
        var source = new PcRemoteDataSource(store);
        await source.SavePairTokenAsync("pair-token-123");
        Assert.Equal("pair-token-123", await source.ReadPairTokenAsync());
    }

    [Fact]
    public async Task Snapshot_Is_Available_Before_Real_Http()
    {
        ISecureStore store = new InMemorySecureStore();
        IHopOnDataSource source = new PcRemoteDataSource(store);
        var snap = await source.GetSnapshotAsync();
        Assert.NotNull(snap);
        Assert.Equal("M0-stub", snap.Version);
    }
}
