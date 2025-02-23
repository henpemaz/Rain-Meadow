using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using UnityEngine;

namespace RainMeadow
{
    public class MeadowCredits : SmartMenu
    {
        string credits = $"""

                         {Utils.Translate("Rain Meadow is a unique multiplayer engine for a unique game. Nothing else would fit. - Henpemaz")}

                         {Utils.Translate("Meadow is a gamemode inspired by the game of same name from Might&Delight. It's a buggy, barely playable game, that one can fall in love with. Sounds familiar?")}

                         {Utils.Translate("This mod and all its features were brought to you by really awesome people from the community.")}

                         Henpemaz - {Utils.Translate("Lead Developer")}
                         Wolfycatt(Ana) - {Utils.Translate("Lead Artist")}
                         Intikus - {Utils.Translate("Lead Audio Designer")}
                         Noircatto - {Utils.Translate("Programming, engine")}
                         HelloThere - {Utils.Translate("Programming, sound")}
                         A1iex - {Utils.Translate("UI Design")}
                         FranklyGD - {Utils.Translate("Programming, engine")}
                         MC41Games - {Utils.Translate("Programming, menus")}
                         Silvyger - {Utils.Translate("Programming, arena")}
                         Vigaro - {Utils.Translate("Programming, menus")}
                         BitiLope - {Utils.Translate("Programming, story")}
                         Pudgy Turtle - {Utils.Translate("Programming, story")}
                         ddemile - {Utils.Translate("Programming, modsync")}
                         UO - {Utils.Translate("Programming, story, arena")}
                         Saddest - {Utils.Translate("Programming, UI, chat")}
                         notchoc - {Utils.Translate("Programming, story")}
                         phanie_ - {Utils.Translate("Illustration")}
                         Timbits - {Utils.Translate("Programming, UI, menus")}
                         Zedreus - {Utils.Translate("Programming, UI, story")}
                         Persondotexe - {Utils.Translate("Programming, modsync")}
                         invalidunits - {Utils.Translate("Programming, UI, LAN")}
                         forthfora - {Utils.Translate("Programming, modsync")}

                         {Utils.Translate("Thank you playtesters from the Rain Meadow discord as well.")}

                         """;


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
