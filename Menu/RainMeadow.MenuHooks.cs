using Mono.Cecil.Cil;
using MonoMod.Cil;
using Steamworks;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using Menu;
using System.Linq;
using System.Collections.Generic;

namespace RainMeadow
{
    public partial class RainMeadow
    {
        private void MenuHooks()
        {
            On.Menu.MainMenu.ctor += MainMenu_ctor;
            On.Menu.InputOptionsMenu.ctor += InputOptionsMenu_ctor;

            On.ProcessManager.PostSwitchMainProcess += ProcessManager_PostSwitchMainProcess;

            IL.Menu.SlugcatSelectMenu.SlugcatPage.AddImage += SlugcatPage_AddImage;
            On.Menu.MenuScene.BuildScene += MenuScene_BuildScene;
            
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
                    self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "bkg", new Vector2(0f, 0f), 3.8f, MenuDepthIllustration.MenuShader.Normal));
                    self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "mg", new Vector2(0f, 0f), 2.9f, MenuDepthIllustration.MenuShader.Normal));
                    self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "squit", new Vector2(0f, 0f), 2.1f, MenuDepthIllustration.MenuShader.LightEdges));
                    (self as InteractiveMenuScene).idleDepths.Add(3.2f);
                    (self as InteractiveMenuScene).idleDepths.Add(2.2f);
                    (self as InteractiveMenuScene).idleDepths.Add(2.1f);
                    (self as InteractiveMenuScene).idleDepths.Add(2.0f);
                    (self as InteractiveMenuScene).idleDepths.Add(1.5f);
                }
            }
            if (self.sceneID == RainMeadow.Ext_SceneID.Slugcat_MeadowLizard)
            {
                self.sceneFolder = "Scenes" + Path.DirectorySeparatorChar.ToString() + "meadow - lizard";
                if (self.flatMode)
                {
                    self.AddIllustration(new MenuIllustration(self.menu, self, self.sceneFolder, "MeadowLizard - Flat", new Vector2(683f, 384f), false, true));
                }
                else
                {
                    self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "bg", new Vector2(0f, 0f), 3.5f, MenuDepthIllustration.MenuShader.Normal));
                    self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "liz2", new Vector2(0f, 0f), 2.4f, MenuDepthIllustration.MenuShader.Normal));
                    self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "liz1", new Vector2(0f, 0f), 2.2f, MenuDepthIllustration.MenuShader.Normal));
                    self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "fgplants", new Vector2(0f, 0f), 2.1f, MenuDepthIllustration.MenuShader.LightEdges));
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
            var SlugData = new List<SlugcatStats.Name>();
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
                    if (mcsp.character == MeadowProgression.Character.Cicada)
                    {
                        sceneID = RainMeadow.Ext_SceneID.Slugcat_MeadowSquidcicada;
                        self.sceneOffset = new Vector2(-10f, 100f);
                        self.slugcatDepth = 3.1000001f;
                    }
                    if (mcsp.character == MeadowProgression.Character.Lizard)
                    {
                        sceneID = RainMeadow.Ext_SceneID.Slugcat_MeadowLizard;
                        self.sceneOffset = new Vector2(-10f, 100f);
                        self.slugcatDepth = 3.1000001f;
                    }

                }
            });
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
            if (ID == Ext_ProcessID.LobbyMenu)
            {
                self.currentMainLoop = new LobbyMenu(self);
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
                            MatchmakingManager.instance.JoinLobby(new LobbyInfo(new CSteamID(id), "", "", 0));
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

        private void InputOptionsMenu_ctor(On.Menu.InputOptionsMenu.orig_ctor orig, InputOptionsMenu self, ProcessManager manager)
        {

            orig(self, manager);


            List<string> inputLabelTextsList = new List<string>(new string[9] { "Pause", "Map", "Pick up / Eat", "Jump", "Throw", "Left", "Up", "Right", "Down" }); // pain
            inputLabelTextsList.Add("View Friends");
            string[] inputLabelTexts = inputLabelTextsList.ToArray();


            self.inputLabelTexts = inputLabelTexts;





        }


        private void MainMenu_ctor(On.Menu.MainMenu.orig_ctor orig, MainMenu self, ProcessManager manager, bool showRegionSpecificBkg)
        {
            orig(self, manager, showRegionSpecificBkg);

            MatchmakingManager.instance.LeaveLobby();

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