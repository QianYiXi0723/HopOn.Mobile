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
    public async Task Snapshot_Before_Pairing_Returns_Unpaired_No_Throw()
    {
        // M1：未设置 BaseAddress（未扫码配对）时不抛异常，返回明确标记的空快照，
        // 让 UI 的 try/catch 不误触发崩溃、也能提示"未配对"。
        ISecureStore store = new InMemorySecureStore();
        IHopOnDataSource source = new PcRemoteDataSource(store);
        var snap = await source.GetSnapshotAsync();
        Assert.NotNull(snap);
        Assert.Equal("unpaired", snap.Version);
    }
}
