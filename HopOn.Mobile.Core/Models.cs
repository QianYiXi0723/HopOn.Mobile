namespace HopOn.Mobile.Core;

public sealed class AppSnapshot
{
    public string Version { get; set; } = "";
    public IReadOnlyList<GameStatus> Games { get; set; } = Array.Empty<GameStatus>();
}

public sealed class GameStatus
{
    public string GameId { get; set; } = "";
    public string Name { get; set; } = "";
    public int Current { get; set; }
    public int Max { get; set; }
    public bool IsRiskControlled { get; set; }
}

public sealed class DailyItem
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public bool Done { get; set; }
    public IReadOnlyList<string> Times { get; set; } = Array.Empty<string>();
}

public sealed class CheckInResult
{
    public bool Ok { get; set; }
    public string Message { get; set; } = "";
}
