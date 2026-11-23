namespace RainMeadow;

// abstracted for displaying lobby data
public abstract class LobbyInfo(
    string name,
    string mode,
    int playerCount,
    bool hasPassword,
    int? maxPlayerCount,
    string highImpactMods = "",
    string bannedMods = "",
    string activeTimeline = "",
    string whitelistMods = ""
)
{
    public string name = name,
        mode = mode,
        requiredMods = highImpactMods,
        bannedMods = bannedMods,
        whitelistMods = whitelistMods;
    public string activeTimeline = activeTimeline;
    public int playerCount = playerCount,
        maxPlayerCount = maxPlayerCount ?? 0;
    public bool hasPassword = hasPassword,
        pinned;

    public string[] WhitelistModArray => whitelistMods.Split(new[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries);

    public abstract MatchmakingManager.MatchMakingDomain domain { get; }
    protected abstract object[] JoinCodeArgs { get; }

    public string GetLobbyJoinCode(string? password = null)
        => MatchmakingManager.instances[domain].FormatJoinCode(password, JoinCodeArgs);
}
