using System.Collections.Generic;
using Newtonsoft.Json;

namespace RainMeadow;

public sealed class RainMeadowModInfo
{
    /// <summary>
    /// Mods that, if they are active on a host, are required to be active on any client who wishes to connect.
    /// </summary>
    [JsonProperty("client_required_mods")]
    public List<string> ClientRequiredMods { get; set; } = new();

    /// <summary>
    /// Mods that are banned from being enabled in online game modes.
    /// </summary>
    [JsonProperty("online_banned_mods")]
    public List<string> OnlineBannedMods { get; set; } = new();


    /// <summary>
    /// Merges the contents of the provided mod info into this mod info, ignoring duplicate values.
    /// </summary>
    /// <param name="modInfo">The mod info take from.</param>
    public void MergeInfoFrom(RainMeadowModInfo modInfo)
    {
        ClientRequiredMods.AddDistinctRange(modInfo.ClientRequiredMods);
        OnlineBannedMods.AddDistinctRange(modInfo.OnlineBannedMods);
    }
}
