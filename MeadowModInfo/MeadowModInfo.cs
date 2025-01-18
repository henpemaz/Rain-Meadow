using Newtonsoft.Json;

namespace RainMeadow;

public sealed class MeadowModInfo
{
    [JsonProperty("high_impact")]
    public bool HighImpact { get; set; }
}
