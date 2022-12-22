using BepInEx;

namespace RainMeadow
{
    [BepInPlugin("henpemaz.rainmeadow", "RainMeadow", "0.1.0")]
    class RainMeadow : BaseUnityPlugin
    {
        public void OnEnable()
        {
            LobbyMenu.Apply();
            SessionHooks.Apply();
        }
    }

}
