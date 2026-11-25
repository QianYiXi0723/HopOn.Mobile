using System.Collections.ObjectModel;

namespace HopOn.Mobile.Core;

public sealed class PcRemoteDataSource : IHopOnDataSource
{
    private const string TokenKey = "hopon.pair_token";
    private readonly ISecureStore _store;

    public PcRemoteDataSource(ISecureStore store) => _store = store;

    public Task SavePairTokenAsync(string token) => _store.SetAsync(TokenKey, token);
    public Task<string?> ReadPairTokenAsync() => _store.GetAsync(TokenKey);

    public Task<AppSnapshot> GetSnapshotAsync(CancellationToken ct = default) =>
        Task.FromResult(new AppSnapshot
        {
            Version = "M0-stub",
            Games = new ReadOnlyCollection<GameStatus>(new[] {
                new GameStatus { GameId = "genshin", Name = "原神", Current = 0, Max = 200 }
            })
        });

    public Task<IReadOnlyList<DailyItem>> GetDailiesAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<DailyItem>>(Array.Empty<DailyItem>());

    public Task<CheckInResult> CheckInAsync(string itemId, string date, CancellationToken ct = default) =>
        Task.FromResult(new CheckInResult { Ok = false, Message = "M0 桩未接 PC" });
}
