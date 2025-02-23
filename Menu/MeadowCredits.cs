using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using UnityEngine;

namespace RainMeadow
{
    public class MeadowCredits : SmartMenu
    {
        string credits = @"
Rain Meadow is a unique multiplayer engine for a unique game. Nothing else would fit. - Henpemaz

Meadow is a gamemode inspired by the game of same name from Might&Delight. It's a buggy, barely playable game, that one can fall in love with. Sounds familiar?

This mod and all its features were brought to you by really awesome people from the community.

Henpemaz - Lead Developer
Wolfycatt(Ana) - Lead Artist
Intikus - Lead Audio Designer
Noircatto - Programming, engine
HelloThere - Programming, sound
A1iex - UI Design
FranklyGD - Programming, engine
MC41Games - Programming, menus
Silvyger - Programming, arena
Vigaro - Programming, menus
BitiLope - Programming, story
Pudgy Turtle - Programming, story
ddemile - Programming, modsync
UO - Programming, story, arena
Saddest - Programming, UI, chat
notchoc - Programming, story
phanie_ - Illustration
Timbits - Programming, UI, menus
Zedreus - Programming, UI, story
Persondotexe - Programming, modsync
invalidunits - Programming, UI, LAN
forthfora - Programming, modsync

Thank you playtesters from the Rain Meadow discord as well.
";


        public override MenuScene.SceneID GetScene => ModManager.MMF ? manager.rainWorld.options.subBackground : MenuScene.SceneID.Landscape_SU;
        public MeadowCredits(ProcessManager manager) : base(manager, RainMeadow.Ext_ProcessID.MeadowCredits)
        {
            RainMeadow.DebugMe();

            this.scene.AddIllustration(new MenuIllustration(this, this.scene, "illustrations/rainmeadowtitle", Utils.GetMeadowTitleFileName(true), new Vector2(-2.99f, 265.01f), true, false));
            this.scene.AddIllustration(new MenuIllustration(this, this.scene, "illustrations/rainmeadowtitle", Utils.GetMeadowTitleFileName(false), new Vector2(-2.99f, 265.01f), true, false));
            this.scene.flatIllustrations[this.scene.flatIllustrations.Count - 1].sprite.shader = this.manager.rainWorld.Shaders["MenuText"];

            this.backTarget = RainMeadow.Ext_ProcessID.LobbySelectMenu;

            OpLabelLong text = new OpLabelLong(new Vector2(manager.rainWorld.screenSize.x / 2f - 300f, 40), new Vector2(600, 580), credits);
            new UIelementWrapper(tabWrapper, text);
        }
    }
}
