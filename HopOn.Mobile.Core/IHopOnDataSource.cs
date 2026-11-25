namespace HopOn.Mobile.Core;

public interface IHopOnDataSource
{
    Task<AppSnapshot> GetSnapshotAsync(CancellationToken ct = default);
    Task<IReadOnlyList<DailyItem>> GetDailiesAsync(CancellationToken ct = default);
    Task<CheckInResult> CheckInAsync(string itemId, string date, CancellationToken ct = default);
}
