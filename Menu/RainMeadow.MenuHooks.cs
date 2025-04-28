using IL.RWCustom;
using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RWCustom;
using Steamworks;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace RainMeadow
{
    public partial class RainMeadow
    {
        private void MenuHooks()
        {
            IntroRollReplacement.OnEnable();
            IL.Menu.Menu.Update += IL_Menu_Update;
            On.Menu.Menu.SelectNewObject += On_Menu_SelectNewObject;
            On.Menu.MainMenu.ctor += MainMenu_ctor;
            //On.Menu.InputOptionsMenu.ctor += InputOptionsMenu_ctor;

            On.ProcessManager.RequestMainProcessSwitch_ProcessID += ProcessManager_RequestMainProcessSwitch_ProcessID;
            On.ProcessManager.PostSwitchMainProcess += ProcessManager_PostSwitchMainProcess;

            IL.Menu.SlugcatSelectMenu.SlugcatPage.AddImage += SlugcatPage_AddImage;

            On.Menu.MenuScene.BuildScene += MenuScene_BuildScene;

            On.Menu.SlugcatSelectMenu.SlugcatUnlocked += SlugcatSelectMenu_SlugcatUnlocked;
            On.Menu.SlugcatSelectMenu.StartGame += SlugcatSelectMenu_StartGame;
            On.Menu.SlugcatSelectMenu.UpdateStartButtonText += SlugcatSelectMenu_UpdateStartButtonText;

            On.Menu.SlugcatSelectMenu.AddColorButtons += SlugcatSelectMenu_AddColorButtons;
            On.Menu.MenuObject.GrafUpdate += On_MenuObject_GrafUpdate;
        }
        void IL_Menu_Update(ILContext il)
        {
            try
            {
                ILCursor cursor = new(il);
                cursor.TryGotoNext(MoveType.After, x => x.MatchStfld<Menu.Menu>(nameof(Menu.Menu.allowSelectMove)));
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate((Menu.Menu self) =>
                {
                    self.allowSelectMove = self.allowSelectMove && !(self is SpectatorOverlay { forceNonMouseSelectFreeze: true });
                });
            }
            catch (Exception ex)
            {
                Error(ex);
            }
        }
        void On_Menu_SelectNewObject(On.Menu.Menu.orig_SelectNewObject orig, Menu.Menu self, RWCustom.IntVector2 direction)
        {
            if(!ChatTextBox.blockInput) orig(self, direction);
        }
        void On_MenuObject_GrafUpdate(On.Menu.MenuObject.orig_GrafUpdate orig, MenuObject self, float timestacker)
        {
            orig(self, timestacker);
            if (self is ButtonScroller.IPartOfButtonScroller buttonScroll)
            {
                buttonScroll.UpdateAlpha(buttonScroll.Alpha);
            }
        }
        void SlugcatSelectMenu_AddColorButtons(On.Menu.SlugcatSelectMenu.orig_AddColorButtons orig, SlugcatSelectMenu self) 
        {
            if (self is StoryOnlineMenu sOM)
            {
                if (sOM.colorInterface == null)
                {
                    sOM.SetupSelectableSlugcats();
                    Vector2 pos = new(1000f - (1366f - sOM.manager.rainWorld.options.ScreenSize.x) / 2f, sOM.manager.rainWorld.options.ScreenSize.y - 100f);
                    self.colorInterface = self.GetColorInterfaceForSlugcat(sOM.CurrentSlugcat, pos);
                    self.pages[0].subObjects.Add(self.colorInterface);
                    //return; removed return due to the orig making a new the color interface if it is null, so unnecessary
                }
            }       
            orig(self);
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

        private void SlugcatSelectMenu_StartGame(On.Menu.SlugcatSelectMenu.orig_StartGame orig, SlugcatSelectMenu self, SlugcatStats.Name storyGameCharacter)
        {
            if (self is StoryOnlineMenu menu)
            {
                menu.StartGame(storyGameCharacter);
            }
            else
            {
                orig(self, storyGameCharacter);
            }
        }

        private void SlugcatSelectMenu_UpdateStartButtonText(On.Menu.SlugcatSelectMenu.orig_UpdateStartButtonText orig, SlugcatSelectMenu self)
        {
            if (self is StoryOnlineMenu menu && OnlineManager.lobby != null && !OnlineManager.lobby.isOwner)
            {
                self.startButton.fillTime = 40f;
                self.startButton.menuLabel.text = self.Translate("ENTER");
            }
            else
            {
                orig(self);
            }
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

                if (isStoryMode(out var _) && !OnlineManager.lobby.isOwner)
                {
                    sceneID = Menu.MenuScene.SceneID.Intro_6_7_Rain_Drop;
                    self.sceneOffset = new Vector2(-10f, 100f);
                    self.slugcatDepth = 3.1000001f;
                }
            });
        }

        private void ProcessManager_RequestMainProcessSwitch_ProcessID(On.ProcessManager.orig_RequestMainProcessSwitch_ProcessID orig, ProcessManager self, ProcessManager.ProcessID ID)
        {
            if (OnlineManager.lobby?.gameMode is OnlineGameMode gameMode and not MeadowGameMode)
            {
                // todo figure out a better way to do this proccess redirection, this isn't ideal
                if (ID == ProcessManager.ProcessID.MainMenu || ID == ProcessManager.ProcessID.MultiplayerMenu || ID == ProcessManager.ProcessID.SlugcatSelect)
                {
                    if (self.currentMainLoop.ID == gameMode.MenuProcessId())
                    {
                        ID = Ext_ProcessID.LobbySelectMenu;
                    }
                    else
                    {
                        ID = gameMode.MenuProcessId();

                        if (OnlineManager.lobby.isOwner)
                        {
                            foreach (OnlinePlayer player in OnlineManager.players)
                                if (!player.isMe) player.InvokeOnceRPC(RPCs.ExitToGameModeMenu);
                        }
                    }
                }
            }

            orig(self, ID);
        }

        private void ProcessManager_PostSwitchMainProcess(On.ProcessManager.orig_PostSwitchMainProcess orig, ProcessManager self, ProcessManager.ProcessID ID)
        {
            if (ID == Ext_ProcessID.LobbySelectMenu) self.currentMainLoop = new LobbySelectMenu(self);
            if (ID == Ext_ProcessID.LobbyCreateMenu) self.currentMainLoop = new LobbyCreateMenu(self);
            if (ID == Ext_ProcessID.ArenaLobbyMenu) self.currentMainLoop = new ArenaLobbyMenu(self);
            if (ID == Ext_ProcessID.MeadowMenu) self.currentMainLoop = new MeadowMenu(self);
            if (ID == Ext_ProcessID.StoryMenu) self.currentMainLoop = new StoryOnlineMenu(self);
            if (ID == Ext_ProcessID.MeadowCredits) self.currentMainLoop = new MeadowCredits(self);

            if (ID == ProcessManager.ProcessID.IntroRoll)
            {
                try
                {
                    var args = System.Environment.GetCommandLineArgs();
                    
                    MatchmakingManager.JoinLobbyUsingCode(string.Join(" ", args));
                }
                catch (Exception ex)
                {
                    RainMeadow.Debug(ex);
                }
            }
            orig(self, ID);
        }

        private bool showed_no_steam_warning = false;

        private void MainMenu_ctor(On.Menu.MainMenu.orig_ctor orig, MainMenu self, ProcessManager manager, bool showRegionSpecificBkg)
        {
            orig(self, manager, showRegionSpecificBkg);

            if (!fullyInit)
            {
                self.manager.ShowDialog(new DialogNotify(self.Translate("Rain Meadow failed to start"), self.manager, null));
                return;
            }

            // we might get here from quitting out of game
            OnlineManager.LeaveLobby();

            var meadowButton = new SimpleButton(self, self.pages[0], self.Translate("MEADOW"), "MEADOW", Vector2.zero, new Vector2(Menu.MainMenu.GetButtonWidth(self.CurrLang), 30f));
            self.AddMainMenuButton(meadowButton, () =>
            {
                if (!(OnlineManager.netIO is SteamNetIO) && !showed_no_steam_warning)
                {
                    showed_no_steam_warning = true;
                    self.manager.ShowDialog(new DialogNotify(self.Translate("Steam is not currently available. Some features of Rain Meadow have been disabled."), self.manager, 
                        () => self.manager.RequestMainProcessSwitch(Ext_ProcessID.LobbySelectMenu)));
                    return;
                }

                OnlineManager.LeaveLobby();
                self.manager.RequestMainProcessSwitch(Ext_ProcessID.LobbySelectMenu);
            }, self.mainMenuButtons.Count - 2);
        }
    }
}
