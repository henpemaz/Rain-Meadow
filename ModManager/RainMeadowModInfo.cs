using System.Collections.Generic;
using Newtonsoft.Json;

namespace RainMeadow;

public sealed class RainMeadowModInfo
{
    [JsonProperty("required_mods")]
    public List<string> RequiredMods { get; set; } = new();

    [JsonProperty("banned_mods")]
    public List<string> BannedMods { get; set; } = new();


    /// <summary>
    /// Adds the contents of this mod info to the provided mod info.
    /// </summary>
    /// <param name="modInfo">The mod info to add to.</param>
    /// <returns>The provided mod info with the data from this mod info added.</returns>
    public void AddInfoTo(RainMeadowModInfo modInfo)
    {
        modInfo.RequiredMods.AddDistinctRange(RequiredMods);
        modInfo.BannedMods.AddDistinctRange(BannedMods);
    }
}
