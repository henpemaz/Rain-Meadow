using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Steamworks;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace RainMeadow
{
    public partial class RainMeadow
    {
        private void MenuHooks()
        {
            On.Menu.MainMenu.ctor += MainMenu_ctor;
            //On.Menu.InputOptionsMenu.ctor += InputOptionsMenu_ctor;

            On.ProcessManager.RequestMainProcessSwitch_ProcessID += ProcessManager_RequestMainProcessSwitch_ProcessID;
            On.ProcessManager.PostSwitchMainProcess += ProcessManager_PostSwitchMainProcess;

            IL.Menu.SlugcatSelectMenu.SlugcatPage.AddImage += SlugcatPage_AddImage;

            On.Menu.MenuScene.BuildScene += MenuScene_BuildScene;

            On.Menu.SlugcatSelectMenu.SlugcatUnlocked += SlugcatSelectMenu_SlugcatUnlocked;
        }

        private bool SlugcatSelectMenu_SlugcatUnlocked(On.Menu.SlugcatSelectMenu.orig_SlugcatUnlocked orig, SlugcatSelectMenu self, SlugcatStats.Name i)
        {
            if (OnlineManager.lobby == null)
            {
                return orig(self, i);
            }

            //TODO MSC: do something smarter, this stops the crash; I'm being lazy -Turtle
            return true;
        }

        private void MenuScene_BuildScene(On.Menu.MenuScene.orig_BuildScene orig, MenuScene self)
        {
            orig(self);
            if (!string.IsNullOrEmpty(self.sceneFolder))
            {
                return;
            }
            if (self.sceneID == RainMeadow.Ext_SceneID.Slugcat_MeadowSquidcicada)
            {
                self.sceneFolder = "Scenes" + Path.DirectorySeparatorChar.ToString() + "meadow - squidcicada";
                if (self.flatMode)
                {
                    self.AddIllustration(new MenuIllustration(self.menu, self, self.sceneFolder, "MeadowSquidcicada - Flat", new Vector2(683f, 384f), false, true));
                }
                else
                {
                    self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "rmsquid bg", new Vector2(0f, 0f), 3.8f, MenuDepthIllustration.MenuShader.Normal));
                    self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "rmsquid mg", new Vector2(0f, 0f), 2.9f, MenuDepthIllustration.MenuShader.Normal));
                    self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "rmsquid squit", new Vector2(0f, 0f), 2.1f, MenuDepthIllustration.MenuShader.LightEdges));
                    (self as InteractiveMenuScene).idleDepths.Add(3.2f);
                    (self as InteractiveMenuScene).idleDepths.Add(2.2f);
                    (self as InteractiveMenuScene).idleDepths.Add(2.1f);
                    (self as InteractiveMenuScene).idleDepths.Add(2.0f);
                    (self as InteractiveMenuScene).idleDepths.Add(1.5f);
                }
            }
            else if (self.sceneID == RainMeadow.Ext_SceneID.Slugcat_MeadowLizard)
            {
                self.sceneFolder = "Scenes" + Path.DirectorySeparatorChar.ToString() + "meadow - lizard";
                if (self.flatMode)
                {
                    self.AddIllustration(new MenuIllustration(self.menu, self, self.sceneFolder, "MeadowLizard - Flat", new Vector2(683f, 384f), false, true));
                }
                else
                {
                    self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "rmliz bg", new Vector2(0f, 0f), 3.5f, MenuDepthIllustration.MenuShader.Normal));
                    self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "rmliz liz2", new Vector2(0f, 0f), 2.4f, MenuDepthIllustration.MenuShader.Normal));
                    self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "rmliz liz1", new Vector2(0f, 0f), 2.2f, MenuDepthIllustration.MenuShader.Normal));
                    self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "rmliz fgplants", new Vector2(0f, 0f), 2.1f, MenuDepthIllustration.MenuShader.LightEdges));
                    (self as InteractiveMenuScene).idleDepths.Add(3.2f);
                    (self as InteractiveMenuScene).idleDepths.Add(2.2f);
                    (self as InteractiveMenuScene).idleDepths.Add(2.1f);
                    (self as InteractiveMenuScene).idleDepths.Add(2.0f);
                    (self as InteractiveMenuScene).idleDepths.Add(1.5f);
                }
            }
            else if (self.sceneID == RainMeadow.Ext_SceneID.Slugcat_MeadowScav)
            {
                self.sceneFolder = "Scenes" + Path.DirectorySeparatorChar.ToString() + "meadow - scav";
                if (self.flatMode)
                {
                    self.AddIllustration(new MenuIllustration(self.menu, self, self.sceneFolder, "MeadowScav - Flat", new Vector2(683f, 384f), false, true));
                }
                else
                {
                    self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "rmscav bg", new Vector2(0f, 0f), 3.5f, MenuDepthIllustration.MenuShader.Normal));
                    self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "rmscav scav", new Vector2(0f, 0f), 2.2f, MenuDepthIllustration.MenuShader.Normal));
                    self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "rmscav fg", new Vector2(0f, 0f), 2.1f, MenuDepthIllustration.MenuShader.LightEdges));
                    (self as InteractiveMenuScene).idleDepths.Add(3.2f);
                    (self as InteractiveMenuScene).idleDepths.Add(2.2f);
                    (self as InteractiveMenuScene).idleDepths.Add(2.1f);
                    (self as InteractiveMenuScene).idleDepths.Add(2.0f);
                    (self as InteractiveMenuScene).idleDepths.Add(1.5f);
                }
            }
            else if (self.sceneID == RainMeadow.Ext_SceneID.Slugcat_MeadowEggbug)
            {
                self.sceneFolder = "Scenes" + Path.DirectorySeparatorChar.ToString() + "meadow - eggbug";
                if (self.flatMode)
                {
                    self.AddIllustration(new MenuIllustration(self.menu, self, self.sceneFolder, "MeadowEggbug - Flat", new Vector2(683f, 384f), false, true));
                }
                else
                {
                    self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "rmbug bg", new Vector2(0f, 0f), 3.5f, MenuDepthIllustration.MenuShader.Normal));
                    self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "rmbug mg", new Vector2(0f, 0f), 2.4f, MenuDepthIllustration.MenuShader.Normal));
                    self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "rmbug bug", new Vector2(0f, 0f), 2.2f, MenuDepthIllustration.MenuShader.Normal));
                    self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "rmbug fg", new Vector2(0f, 0f), 2.1f, MenuDepthIllustration.MenuShader.LightEdges));
                    (self as InteractiveMenuScene).idleDepths.Add(3.2f);
                    (self as InteractiveMenuScene).idleDepths.Add(2.2f);
                    (self as InteractiveMenuScene).idleDepths.Add(2.1f);
                    (self as InteractiveMenuScene).idleDepths.Add(2.0f);
                    (self as InteractiveMenuScene).idleDepths.Add(1.5f);
                }
            }
            else if (self.sceneID == RainMeadow.Ext_SceneID.Slugcat_MeadowNoot)
            {
                self.sceneFolder = "Scenes" + Path.DirectorySeparatorChar.ToString() + "meadow - noot";
                if (self.flatMode)
                {
                    self.AddIllustration(new MenuIllustration(self.menu, self, self.sceneFolder, "MeadowNoot - Flat", new Vector2(683f, 384f), false, true));
                }
                else
                {
                    self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "rmnoot bg", new Vector2(0f, 0f), 3.5f, MenuDepthIllustration.MenuShader.Normal));
                    self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "rmnoot mg", new Vector2(0f, 0f), 2.8f, MenuDepthIllustration.MenuShader.Normal));
                    self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "rmnoot noot", new Vector2(0f, 0f), 2.2f, MenuDepthIllustration.MenuShader.LightEdges));
                    self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "rmnoot fg", new Vector2(0f, 0f), 1.8f, MenuDepthIllustration.MenuShader.LightEdges));
                    (self as InteractiveMenuScene).idleDepths.Add(3.2f);
                    (self as InteractiveMenuScene).idleDepths.Add(2.6f);
                    (self as InteractiveMenuScene).idleDepths.Add(2.5f);
                    (self as InteractiveMenuScene).idleDepths.Add(2.4f);
                    //(self as InteractiveMenuScene).idleDepths.Add(1.5f);
                }
            }
            else if (self.sceneID == RainMeadow.Ext_SceneID.Slugcat_MeadowMouse)
            {
                self.sceneFolder = "Scenes" + Path.DirectorySeparatorChar.ToString() + "meadow - mouse";
                if (self.flatMode)
                {
                    self.AddIllustration(new MenuIllustration(self.menu, self, self.sceneFolder, "MeadowMouse - Flat", new Vector2(683f, 384f), false, true));
                }
                else
                {
                    self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "rmmouse bg", new Vector2(0f, 0f), 3.5f, MenuDepthIllustration.MenuShader.Normal));
                    self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "rmmouse lights", new Vector2(0f, 0f), 2.4f, MenuDepthIllustration.MenuShader.SoftLight));
                    self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "rmmouse mouse", new Vector2(0f, 0f), 2.2f, MenuDepthIllustration.MenuShader.Normal));
                    self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "rmmouse fg", new Vector2(0f, 0f), 2.1f, MenuDepthIllustration.MenuShader.LightEdges));
                    (self as InteractiveMenuScene).idleDepths.Add(3.2f);
                    (self as InteractiveMenuScene).idleDepths.Add(2.2f);
                    (self as InteractiveMenuScene).idleDepths.Add(2.1f);
                    (self as InteractiveMenuScene).idleDepths.Add(2.0f);
                    (self as InteractiveMenuScene).idleDepths.Add(1.5f);
                }
            }
            if (string.IsNullOrEmpty(self.sceneFolder))
            {
                return;
            }

            string path2 = AssetManager.ResolveFilePath(self.sceneFolder + Path.DirectorySeparatorChar.ToString() + "positions_ims.txt");
            if (!File.Exists(path2) || !(self is InteractiveMenuScene))
            {
                path2 = AssetManager.ResolveFilePath(self.sceneFolder + Path.DirectorySeparatorChar.ToString() + "positions.txt");
            }
            if (File.Exists(path2))
            {
                string[] array3 = File.ReadAllLines(path2);

                for (int num3 = 0; num3 < array3.Length && num3 < self.depthIllustrations.Count; num3++)
                {
                    self.depthIllustrations[num3].pos.x = float.Parse(Regex.Split(RWCustom.Custom.ValidateSpacedDelimiter(array3[num3], ","), ", ")[0], NumberStyles.Any, CultureInfo.InvariantCulture);
                    self.depthIllustrations[num3].pos.y = float.Parse(Regex.Split(RWCustom.Custom.ValidateSpacedDelimiter(array3[num3], ","), ", ")[1], NumberStyles.Any, CultureInfo.InvariantCulture);
                    self.depthIllustrations[num3].lastPos = self.depthIllustrations[num3].pos;
                }
            }
        }

        private void SlugcatPage_AddImage(ILContext il)
        {
            var c = new ILCursor(il);
            c.Index = il.Instrs.Count - 1;
            c.GotoPrev(MoveType.Before,
                (i) => i.MatchLdarg(0),
                (i) => i.MatchLdflda<SlugcatSelectMenu.SlugcatPage>("sceneOffset"),
                (i) => i.MatchLdflda<Vector2>("x"));
            c.MoveAfterLabels();
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloca, 0);
            c.EmitDelegate((SlugcatSelectMenu.SlugcatPage self, ref MenuScene.SceneID sceneID) =>
            {
                if (self.slugcatNumber == RainMeadow.Ext_SlugcatStatsName.OnlineSessionPlayer && self is MeadowCharacterSelectPage mcsp)
                {
                    if (mcsp.character == MeadowProgression.Character.Slugcat)
                    {
                        sceneID = Menu.MenuScene.SceneID.Slugcat_White;
                        self.sceneOffset = new Vector2(-10f, 100f);
                        self.slugcatDepth = 3.1000001f;
                    }
                    else if (mcsp.character == MeadowProgression.Character.Cicada)
                    {
                        sceneID = RainMeadow.Ext_SceneID.Slugcat_MeadowSquidcicada;
                        self.sceneOffset = new Vector2(-10f, 100f);
                        self.slugcatDepth = 3.1000001f;
                    }
                    else if (mcsp.character == MeadowProgression.Character.Lizard)
                    {
                        sceneID = RainMeadow.Ext_SceneID.Slugcat_MeadowLizard;
                        self.sceneOffset = new Vector2(-10f, 100f);
                        self.slugcatDepth = 3.1000001f;
                    }
                    else if (mcsp.character == MeadowProgression.Character.Scavenger)
                    {
                        sceneID = RainMeadow.Ext_SceneID.Slugcat_MeadowScav;
                        self.sceneOffset = new Vector2(-10f, 100f);
                        self.slugcatDepth = 3.1000001f;
                    }
                    else if (mcsp.character == MeadowProgression.Character.Eggbug)
                    {
                        sceneID = RainMeadow.Ext_SceneID.Slugcat_MeadowEggbug;
                        self.sceneOffset = new Vector2(-10f, 100f);
                        self.slugcatDepth = 3.1000001f;
                    }
                    else if (mcsp.character == MeadowProgression.Character.Noodlefly)
                    {
                        sceneID = RainMeadow.Ext_SceneID.Slugcat_MeadowNoot;
                        self.sceneOffset = new Vector2(-10f, 100f);
                        self.slugcatDepth = 3.1000001f;
                    }
                    else if (mcsp.character == MeadowProgression.Character.LanternMouse)
                    {
                        sceneID = RainMeadow.Ext_SceneID.Slugcat_MeadowMouse;
                        self.sceneOffset = new Vector2(-10f, 100f);
                        self.slugcatDepth = 3.1000001f;
                    }
                    else
                    {
                        //throw new InvalidProgrammerException("implement me");
                    }

                }

                if (self.slugcatNumber == RainMeadow.Ext_SlugcatStatsName.OnlineSessionPlayer && self is SlugcatCustomSelection slugcatCustom)
                {
                    if (OnlineManager.lobby.isOwner) // Host
                    {

                        if (slugcatCustom.slug == SlugcatStats.Name.White)
                        {
                            sceneID = Menu.MenuScene.SceneID.Slugcat_White;
                            self.sceneOffset = new Vector2(-10f, 100f);
                            self.slugcatDepth = 3.1000001f;

                        }

                        else if (slugcatCustom.slug == SlugcatStats.Name.Yellow)
                        {
                            sceneID = Menu.MenuScene.SceneID.Slugcat_Yellow;
                            self.sceneOffset = new Vector2(-10f, 100f);
                            self.slugcatDepth = 3.1000001f;
                        }

                        else if (slugcatCustom.slug == SlugcatStats.Name.Red)
                        {
                            sceneID = Menu.MenuScene.SceneID.Slugcat_Red;
                            self.sceneOffset = new Vector2(-10f, 100f);
                            self.slugcatDepth = 3.1000001f;
                        }
                        else if (ModManager.MSC)
                        {

                            if (slugcatCustom.slug == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Rivulet)
                            {

                                sceneID = MoreSlugcats.MoreSlugcatsEnums.MenuSceneID.Slugcat_Rivulet;
                                self.sceneOffset = new Vector2(-10f, 100f);
                                self.slugcatDepth = 3.1000001f;
                            }
                            else if (slugcatCustom.slug == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Artificer)
                            {

                                sceneID = MoreSlugcats.MoreSlugcatsEnums.MenuSceneID.Slugcat_Artificer;
                                self.sceneOffset = new Vector2(-10f, 100f);
                                self.slugcatDepth = 3.1000001f;
                            }

                            else if (slugcatCustom.slug == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Gourmand)
                            {

                                sceneID = MoreSlugcats.MoreSlugcatsEnums.MenuSceneID.Slugcat_Gourmand;
                                self.sceneOffset = new Vector2(-10f, 100f);
                                self.slugcatDepth = 3.1000001f;
                            }
                            else if (slugcatCustom.slug == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Saint)
                            {

                                sceneID = MoreSlugcats.MoreSlugcatsEnums.MenuSceneID.Slugcat_Saint;
                                self.sceneOffset = new Vector2(-10f, 100f);
                                self.slugcatDepth = 3.1000001f;
                            }

                            else if (slugcatCustom.slug == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Spear)
                            {

                                sceneID = MoreSlugcats.MoreSlugcatsEnums.MenuSceneID.Slugcat_Spear;
                                self.sceneOffset = new Vector2(-10f, 100f);
                                self.slugcatDepth = 3.1000001f;
                            }

                            else
                            {
                                sceneID = Menu.MenuScene.SceneID.NewDeath;
                                self.sceneOffset = new Vector2(-10f, 100f);
                                self.slugcatDepth = 3.1000001f;
                            }
                        }

                    }
                    else // Client
                    {
                        var result = UnityEngine.Random.Range(0, 3);
                        switch (result)
                        {
                            case 0:
                                sceneID = Menu.MenuScene.SceneID.Intro_4_Walking;
                                break;
                            case 1:
                                sceneID = Menu.MenuScene.SceneID.Intro_11_Drowning;
                                break;
                            case 2:
                                sceneID = Menu.MenuScene.SceneID.Intro_8_Climbing;
                                break;
                            case 3:
                                sceneID = Menu.MenuScene.SceneID.Intro_6_7_Rain_Drop;
                                break;
                        }

                        self.sceneOffset = new Vector2(-10f, 100f);
                        self.slugcatDepth = 3.1000001f;

                    }
                }

            });
        }

        private void ProcessManager_RequestMainProcessSwitch_ProcessID(On.ProcessManager.orig_RequestMainProcessSwitch_ProcessID orig, ProcessManager self, ProcessManager.ProcessID ID)
        {
            if (OnlineManager.lobby?.gameMode is OnlineGameMode gameMode && RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame)
            {
                if (ID == ProcessManager.ProcessID.MainMenu || ID == ProcessManager.ProcessID.MultiplayerMenu)
                {
                    ID = gameMode.MenuProcessId();

                    if (OnlineManager.lobby.isOwner)
                    {
                        foreach (OnlinePlayer player in OnlineManager.players)
                            if (!player.isMe) player.InvokeOnceRPC(RPCs.ExitToGameModeMenu);
                    }
                }
            }

            orig(self, ID);
        }

        private void ProcessManager_PostSwitchMainProcess(On.ProcessManager.orig_PostSwitchMainProcess orig, ProcessManager self, ProcessManager.ProcessID ID)
        {
            if (ID == Ext_ProcessID.LobbySelectMenu)
            {
                self.currentMainLoop = new LobbySelectMenu(self);
            }
            if (ID == Ext_ProcessID.ArenaLobbyMenu)
            {
                self.currentMainLoop = new ArenaLobbyMenu(self);
            }
            if (ID == Ext_ProcessID.MeadowMenu)
            {
                self.currentMainLoop = new MeadowMenu(self);
            }
            if (ID == Ext_ProcessID.StoryMenu)
            {
                self.currentMainLoop = new StoryMenu(self);
            }
            if (ID == Ext_ProcessID.CustomLobbyMenu)
            {
                self.currentMainLoop = new CustomLobbyMenu(self);
            }

#if !LOCAL_P2P
            if (ID == ProcessManager.ProcessID.IntroRoll)
            {
                var args = System.Environment.GetCommandLineArgs();
                for (var i = 0; i < args.Length; i++)
                {
                    if (args[i] == "+connect_lobby")
                    {
                        if (args.Length > i + 1 && ulong.TryParse(args[i + 1], out var id))
                        {
                            Debug($"joining lobby with id {id} from the command line");
                            MatchmakingManager.instance.RequestJoinLobby(new LobbyInfo(new CSteamID(id), "", "", 0, false, 4), null);
                        }
                        else
                        {
                            Error($"found +connect_lobby but no valid lobby id in the command line");
                        }
                        break;
                    }
                }
            }
#endif
            orig(self, ID);
        }

        private void MainMenu_ctor(On.Menu.MainMenu.orig_ctor orig, MainMenu self, ProcessManager manager, bool showRegionSpecificBkg)
        {
            orig(self, manager, showRegionSpecificBkg);

            if (!fullyInit)
            {
                self.manager.ShowDialog(new DialogNotify("Rain Meadow failed to start", self.manager, null));
                return;
            }

            // we might get here from quitting out of game
            OnlineManager.LeaveLobby();

            var meadowButton = new SimpleButton(self, self.pages[0], self.Translate("MEADOW"), "MEADOW", Vector2.zero, new Vector2(Menu.MainMenu.GetButtonWidth(self.CurrLang), 30f));
            self.AddMainMenuButton(meadowButton, () =>
            {
#if !LOCAL_P2P
                if (!SteamManager.Instance.m_bInitialized || !SteamUser.BLoggedOn())
                {
                    self.manager.ShowDialog(new DialogNotify("You need Steam active to play Rain Meadow", self.manager, null));
                    return;
                }
#endif
                self.manager.RequestMainProcessSwitch(Ext_ProcessID.LobbySelectMenu);
            }, self.mainMenuButtons.Count - 2);
        }
    }
}
