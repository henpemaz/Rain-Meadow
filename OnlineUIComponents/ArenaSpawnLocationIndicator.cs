using HUD;
using UnityEngine;

namespace RainMeadow;

public class ArenaSpawnLocationIndicator : HudPart
{
    private RoomCamera camera;
    private int counter = 0;
    private ClientSettings clientSettings;

    public ArenaSpawnLocationIndicator(HUD.HUD hud, RoomCamera camera) : base(hud)
    {
        this.camera = camera;
        if (!RainMeadow.isArenaMode(out ArenaOnlineGameMode arena)) RainMeadow.Error("ArenaSpawnPositionIndicator was constructed outside of an arena game");
        clientSettings = arena.clientSettings;
    }

    public override void Update()
    {
        counter++;
        if (counter != 30) return;

        if (clientSettings.avatars.Count == 0
            || clientSettings.avatars[0]?.FindEntity(true) is not OnlineCreature oc
            || oc.realizedCreature is not Player player)
        {
            counter = 0;
            return;
        }

        Vector2 pos = Vector2.Lerp(player.bodyChunks[0].pos, player.bodyChunks[1].pos, 0.33333334f) - camera.pos;
        hud.fadeCircles.Add(new FadeCircle(this.hud, 20f, 30f, 0.94f, 60f, 4f, pos, this.hud.fContainers[1]) { alphaMultiply = 0.5f, fadeThickness = false });

        slatedForDeletion = true;
    }
}