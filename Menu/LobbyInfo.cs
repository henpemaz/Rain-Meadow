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
    string activeTimeline = ""
)
{
    public string name = name,
        mode = mode,
        requiredMods = highImpactMods,
        bannedMods = bannedMods;
    public string activeTimeline = activeTimeline;
    public int playerCount = playerCount,
        maxPlayerCount = maxPlayerCount ?? 0;
    public bool hasPassword = hasPassword,
        pinned;

    public abstract string GetLobbyJoinCode(string? password = null);
}
