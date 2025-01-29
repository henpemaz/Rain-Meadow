using System.Collections.Generic;
using Newtonsoft.Json;

namespace RainMeadow;

public sealed class RainMeadowModInfo
{
    /// <summary>
    /// Mods that must be synced between the host and clients in order to allow a connection.
    /// </summary>
    [JsonProperty("sync_required_mods")]
    public List<string> SyncRequiredMods { get; set; } = [];

    /// <summary>
    /// Override for the above, allows the following to be de-synced despite being on the list.
    /// </summary>
    [JsonProperty("sync_required_mods_override")]
    public List<string> SyncRequiredModsOverride { get; set; } = [];

    /// <summary>
    /// Mods that are banned from being enabled in online game modes.
    /// </summary>
    [JsonProperty("banned_online_mods")]
    public List<string> BannedOnlineMods { get; set; } = [];

    /// <summary>
    /// Override for the above, allows the following to be active despite being on the list.
    /// </summary>
    [JsonProperty("banned_online_mods_override")]
    public List<string> BannedOnlineModsOverride { get; set; } = [];


    /// <summary>
    /// Merges the contents of the provided mod info into this mod info, ignoring duplicate values.
    /// </summary>
    /// <param name="modInfo">The mod info take from.</param>
    public void MergeInfoFrom(RainMeadowModInfo modInfo)
    {
        SyncRequiredMods.AddDistinctRange(modInfo.SyncRequiredMods);
        SyncRequiredModsOverride.AddDistinctRange(modInfo.SyncRequiredModsOverride);

        BannedOnlineMods.AddDistinctRange(modInfo.BannedOnlineMods);
        BannedOnlineModsOverride.AddDistinctRange(modInfo.BannedOnlineModsOverride);
    }
}
