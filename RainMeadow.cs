using BepInEx;

namespace RainMeadow
{
    [BepInPlugin("henpemaz.rainmeadow", "RainMeadow", "0.0.1")]
    class RainMeadow : BaseUnityPlugin
    {
        public void OnEnable()
        {
            OnlineHooks.Apply();
        }
    }
}
