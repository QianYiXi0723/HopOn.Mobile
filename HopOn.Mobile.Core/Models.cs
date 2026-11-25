namespace HopOn.Mobile.Core;

public sealed class AppSnapshot
{
    public string Version { get; set; } = "";
    public IReadOnlyList<GameStatus> Games { get; set; } = Array.Empty<GameStatus>();
    public IReadOnlyList<DailyItem> CustomDailies { get; set; } = Array.Empty<DailyItem>();
}

public sealed class GameStatus
{
    public string GameId { get; set; } = "";
    public string Name { get; set; } = "";
    public string StaminaName { get; set; } = "";
    public int Current { get; set; }
    public int Max { get; set; }
    public int DailyDone { get; set; }
    public int DailyTotal { get; set; }
    public string DailyProgress { get; set; } = "";
    public bool Finished { get; set; }
    public bool Stale { get; set; }
    public bool IsRiskControlled { get; set; }
}

public sealed class DailyItem
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public bool Done { get; set; }
    public IReadOnlyList<string> Times { get; set; } = Array.Empty<string>();
    // 后端按 04:00 日界算出的"今天是否在周计划内"；非计划日整条不显示。
    public bool PlannedToday { get; set; } = true;
}

public sealed class CheckInResult
{
    public bool Ok { get; set; }
    public string Message { get; set; } = "";
}
