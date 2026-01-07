using HarmonyLib;
using Menu;
using MoreSlugcats;
using RainMeadow.Arena.ArenaOnlineGameModes.TeamBattle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using UnityEngine;
using static RainMeadow.ArenaPrepTimer;

namespace RainMeadow
{
    public class ArenaOnlineGameMode : OnlineGameMode
    {
        public ArenaOnlineSetup myArenaSetup;
        public ExternalArenaGameMode externalArenaGameMode;
        public string currentGameMode;
        public Dictionary<string, ExternalArenaGameMode> registeredGameModes;

        public OnlinePlayer currentLobbyOwner;

        public bool registeredNewGameModes = false;

        public bool isInGame;
        public int playerLeftGame;
        public int currentLevel;
        public int totalLevelCount;
        public bool allPlayersReadyLockLobby;
        public bool returnToLobby;
        public int painCatThrowingSkill;
        public int forceReadyCountdownTimer;
        public bool leaveForNextLevel;
        public bool leaveToRestart;

        public bool voidMasterEnabled = RainMeadow.rainMeadowOptions.VoidMaster.Value;
        public bool sainot = RainMeadow.rainMeadowOptions.ArenaSAINOT.Value;
        public bool painCatThrows = RainMeadow.rainMeadowOptions.PainCatThrows.Value;
        public bool painCatEgg = RainMeadow.rainMeadowOptions.PainCatEgg.Value;
        public bool painCatLizard = RainMeadow.rainMeadowOptions.PainCatLizard.Value;
        public bool disableMaul = RainMeadow.rainMeadowOptions.BlockMaul.Value;
        public bool disableArtiStun = RainMeadow.rainMeadowOptions.BlockArtiStun.Value;
        public bool itemSteal = RainMeadow.rainMeadowOptions.ArenaItemSteal.Value;
        public bool allowJoiningMidRound = RainMeadow.rainMeadowOptions.ArenaAllowMidJoin.Value;
        public bool weaponCollisionFix = RainMeadow.rainMeadowOptions.WeaponCollisionFix.Value;
        public bool enableBombs = RainMeadow.rainMeadowOptions.EnableBombs.Value;
        public bool enableBees = RainMeadow.rainMeadowOptions.EnableBees.Value;
        public bool enableCorpseGrab = RainMeadow.rainMeadowOptions.EnableCorpseGrab.Value;

        public bool piggyBack = RainMeadow.rainMeadowOptions.EnablePiggyBack.Value;
        public bool amoebaControl = RainMeadow.rainMeadowOptions.AmoebaControl.Value;

        public string paincatName;
        public int lizardEvent;

        public override bool PlayersCanHandhold => false;

        public override bool PlayersCanStack => piggyBack;

        public Dictionary<string, MenuScene.SceneID> slugcatSelectMenuScenes;
        public Dictionary<string, string> slugcatSelectDescriptions, slugcatSelectDisplayNames;
        public List<string> slugcatSelectWatcherDescriptions;
        public List<string> slugcatSelectPainCatNames = [];
        // have fun fixing this UO ;)
        public List<string> slugcatSelectPainCatNormalDescriptions, slugcatSelectPainCatJokeDescriptions, slugcatSelectPainCatQuoteDescriptions, slugcatSelectPainCatDevJokeDescriptions, slugcatSelectPainCatSmileyDescriptions, slugcatSelectPainCatUwUDescriptions, slugcatSelectPainCatWaveDescriptions, slugcatSelectPainCatDeadDescriptions;

        public Dictionary<string, int> onlineArenaSettingsInterfaceMultiChoice = new Dictionary<string, int>();
        public Dictionary<string, bool> onlineArenaSettingsInterfaceeBool = new Dictionary<string, bool>();
        public Dictionary<string, int> playerResultColors = new Dictionary<string, int>();
        public Generics.DynamicOrderedPlayerIDs playersReadiedUp = new Generics.DynamicOrderedPlayerIDs();
        public Generics.DynamicOrderedPlayerIDs reigningChamps = new Generics.DynamicOrderedPlayerIDs();

        public Dictionary<string, int> playersInLobbyChoosingSlugs = new Dictionary<string, int>();
        public Dictionary<int, int> playerNumberWithScore = new Dictionary<int, int>();
        public Dictionary<int, int> playerNumberWithDeaths = new Dictionary<int, int>();
        public Dictionary<int, int> playerNumberWithWins = new Dictionary<int, int>();

        public Dictionary<int, int> playerTotScore = new Dictionary<int, int>();
        public Dictionary<int, List<string>> playerNumberWithTrophies = new Dictionary<int, List<string>>();

        public bool playersEqualToOnlineSitting;
        public bool clientWantsToLeaveGame;
        public bool countdownInitiatedHoldFire;
        public bool addedChampstoList;
        public bool hasPermissionToRejoin;
        public bool initiateLobbyCountdown;


        public ArenaPrepTimer arenaPrepTimer;
        public int setupTime = RainMeadow.rainMeadowOptions.ArenaCountDownTimer.Value;
        public int lobbyCountDown;
        public int trackSetupTime;
        public int scrollInitiatedTimer;


        public int arenaSaintAscendanceTimer = RainMeadow.rainMeadowOptions.ArenaSaintAscendanceTimer.Value;
        public int watcherCamoTimer = RainMeadow.rainMeadowOptions.ArenaWatcherCamoTimer.Value;
        public int watcherRippleLevel = RainMeadow.rainMeadowOptions.ArenaWatcherRippleLevel.Value;
        
        public int amoebaDuration = RainMeadow.rainMeadowOptions.AmoebaDuration.Value;

        public ArenaClientSettings arenaClientSettings;
        public ArenaTeamClientSettings arenaTeamClientSettings;

        public SlugcatCustomization avatarSettings;

        public bool shufflePlayList;
        public List<string> playList = new List<string>();
        public List<ushort> arenaSittingOnlineOrder = new List<ushort>();
        public List<ushort> playersLateWaitingInLobbyForNextRound = new List<ushort>();
        public List<int> bannedSlugs = new List<int>();


        public ArenaOnlineGameMode(Lobby lobby) : base(lobby)
        {
            ArenaHelpers.RecreateSlugcatCache();
            avatarSettings = new SlugcatCustomization() { nickname = OnlineManager.mePlayer.id.name };
            arenaClientSettings = new ArenaClientSettings();
            arenaTeamClientSettings = new ArenaTeamClientSettings();

            playerResultColors = new Dictionary<string, int>();
            registeredGameModes = new Dictionary<string, ExternalArenaGameMode>();
            playersEqualToOnlineSitting = false;
            painCatThrowingSkill = 0;
            totalLevelCount = 0;
            currentLevel = 0;
            isInGame = false;
            lizardEvent = 0;
            paincatName = "";
            allPlayersReadyLockLobby = false;
            returnToLobby = false;
            playersReadiedUp.list = new List<MeadowPlayerId>();
            reigningChamps.list = new List<MeadowPlayerId>();
            addedChampstoList = false;
            forceReadyCountdownTimer = 15;
            clientWantsToLeaveGame = false;
            hasPermissionToRejoin = false;
            leaveForNextLevel = false;
            lobbyCountDown = 5;
            initiateLobbyCountdown = false;
            slugcatSelectMenuScenes = new Dictionary<string, MenuScene.SceneID>()
            {
                { "White", MenuScene.SceneID.Landscape_SU },
                { "Yellow", MenuScene.SceneID.Yellow_Intro_B },
                { "Red", MenuScene.SceneID.Landscape_LF },
                { "Night", MenuScene.SceneID.Outro_2_Up_Swim },
            };
            slugcatSelectDescriptions = new Dictionary<string, string>()
            {
                { "White", "Your enemies close in around you, but it won't be like your first time.<LINE>Snatch your spear and rock." },
                { "Yellow", "Remember: they struck first, so you'll need to hit back harder." },
                { "Red", "Afflicted from the beginning, and a fighter to the end.<LINE>Show them the meaning of suffering." },
                { "Night", "Observe all weakness - then strike while cloaked in shadows." },
            };
            slugcatSelectDisplayNames = new Dictionary<string, string>()
            {
                { "White", "THE SURVIVOR" },
                { "Yellow", "THE MONK" },
                { "Red", "THE HUNTER" },
                { "Night", "THE NIGHTCAT" },
            };

            if (ModManager.MSC)
            {
                slugcatSelectMenuScenes.Add("Gourmand", MoreSlugcatsEnums.MenuSceneID.Landscape_OE);
                slugcatSelectMenuScenes.Add("Artificer", MoreSlugcatsEnums.MenuSceneID.Landscape_LC);
                slugcatSelectMenuScenes.Add("Spear", MoreSlugcatsEnums.MenuSceneID.Landscape_DM);
                slugcatSelectMenuScenes.Add("Rivulet", MoreSlugcatsEnums.MenuSceneID.Landscape_MS);
                slugcatSelectMenuScenes.Add("Saint", MoreSlugcatsEnums.MenuSceneID.Landscape_CL);
                slugcatSelectMenuScenes.Add("Slugpup", RainMeadow.rainMeadowOptions.SlugpupHellBackground.Value ? MoreSlugcatsEnums.MenuSceneID.Landscape_HR : MenuScene.SceneID.Intro_4_Walking);
                slugcatSelectMenuScenes.Add("Inv", MoreSlugcatsEnums.MenuSceneID.End_Inv);

                slugcatSelectDescriptions.Add("Gourmand", "Your tale of twist and turns is near-complete.<LINE>Crush this one last quest.");
                slugcatSelectDescriptions.Add("Artificer", "An explosive personality and unmatched anger.<LINE>Maul and detonate your way to vengeance.");
                slugcatSelectDescriptions.Add("Spear", "A gnawing hunger grows inside you. Feed it with spears.");
                slugcatSelectDescriptions.Add("Rivulet", "In a world lacking purpose, perhaps you've finally found yours.<LINE>Move quickly so it's not lost.");
                slugcatSelectDescriptions.Add("Saint", "The spear is a weak vessel. Shape the world<LINE>from the markings of your mind.");
                slugcatSelectDescriptions.Add("Sainot", "The mind is a weak vessel. Show your prowess<LINE>by the spear in your hand.");
                slugcatSelectDescriptions.Add("Slugpup", "Desperate. Fearful. Violent.");


                slugcatSelectPainCatNames = ["Inv", "Enot", "Paincat", "Sofanthiel", "Gorbo"]; // not using "???" cause it might cause some confusion to players who don't know Inv

                slugcatSelectPainCatNormalDescriptions =
                [
                    "You have been through hell and back, but now, it's<LINE>time to atone for your sins in your past cycles.",
                    "...",
                    "...why are you here",
                    "Thanks, Andrew.",
                ];
                slugcatSelectPainCatJokeDescriptions =
                [
                    ".kcor dna raeps ruoy hctanS<LINE>.emit tsrif ruoy ekil eb t'now ti tub ,uoy dnuora ni esolc seimene ruoY",
                    "Welcome to tower of gains: where you'll be doing heavy lifting for the<LINE>duration of your stay. I hope you've brought hydration, <USERNAME>!",
                    "$5 to unlock this description.",
                    "egg",
                    "Seeking love will lead you down the<LINE>beautiful path of heartbreaking wrecks.",
                    "How much wood could a wood chuck chuck<LINE>if a wood chuck could chuck wood?",
                    "7",
                    "Feeling Lucky?<LINE>Try holding your slugcat portrait ;)",
                    "Did you know:<LINE>Meadow was released this Friday",
                    "Did you know:<LINE>You're bad at Arena",
                    "Did you know:<LINE>There's more \"Did you know\"s.",
                    "Did you know:<LINE>This is the only \"Did you know\".",
                    "You will lose this round<LINE>Your body will not be found<LINE>6 feet underground"

                ];
                slugcatSelectPainCatQuoteDescriptions =
                [
                    "\"<USERNAME>, youre gonna get us both killed\"",
                    "\"no srsly wheres my egg\"",
                    "\"u dont need 2 be alone, bby.\"",
                    "\"sometimes i wake up with a friend ive never met b4\"",
                    "\"i luv u <3\"",
                ];
                slugcatSelectPainCatDevJokeDescriptions =
                [
                    "WHY DID IT HAVE TO BE A VARIABLE<LINE>num2 IS LITERALLY 0",
                    "Don't Care<LINE>Nuh<LINE>Yuh",
                    "Suddenly, the result rectangle failed to appear, you are softlocked.<LINE>What the hell.<LINE>I thought that glitch was fixed a while ago...",
                    "Ever thought about contributing to<LINE>https://github.com/henpemaz/Rain-Meadow?",
                    "Be careful when selecting the Fartificer",
                    "am getting \"among us potion at 3 am\" vibes<LINE>add that /lh",
                    "Playtesters<LINE>Are<LINE>Replaceable",
                    "There's enough inv descriptions.<LINE>DOESNT FILL MY EMPTY STOMACH"
                ];
                slugcatSelectPainCatSmileyDescriptions =
                [
                    ":)",
                    ":D",
                    ":')",
                    ";)",
                    ";D",
                ];
                slugcatSelectPainCatUwUDescriptions =
                [
                    "uwu",
                    "owo",
                    "UwU",
                    "OwO",
                    ">w<",
                    "^w^",
                ];
                slugcatSelectPainCatWaveDescriptions =
                [
                    "\"hiiii!\"",
                    "  o /<LINE>/|<LINE> / \\",
                ];
                slugcatSelectPainCatDeadDescriptions =
                [
                    "\"i'm ded\"",
                    "bleh",
                    "X.X",
                ];

                slugcatSelectDisplayNames.Add("Gourmand", "THE GOURMAND");
                slugcatSelectDisplayNames.Add("Artificer", "THE ARTIFICER");
                slugcatSelectDisplayNames.Add("Spear", "THE SPEARMASTER");
                slugcatSelectDisplayNames.Add("Rivulet", "THE RIVULET");
                slugcatSelectDisplayNames.Add("Saint", "THE SAINT");
                slugcatSelectDisplayNames.Add("Slugpup", "THE SLUGPUP");
                slugcatSelectDisplayNames.Add("Inv", "INV");
            }

            if (ModManager.Watcher)
            {
                slugcatSelectMenuScenes.Add("Watcher", slugcatSelectMenuScenes["Night"]);
                slugcatSelectDescriptions.Add("Watcher", "Open: Voices. Choice. Burdened.<LINE>Closed: Whispers. Convergence. Drowning.<LINE>Open: Echoes. Clarity. Weightless.");
                slugcatSelectDisplayNames.Add("Watcher", "THE WATCHER");

                slugcatSelectMenuScenes.Remove("Night");
                slugcatSelectDescriptions.Remove("Night");
                slugcatSelectDisplayNames.Remove("Night");

                slugcatSelectWatcherDescriptions = 
                [
                    "With no attachments left, withdrawal is the only option.<LINE>Hide and strike with care before that, too, is taken away.",
                    "My little shadow, show them your peeping eyes!",                
                    "Abandoned, separated from your kin, bound by your fate.<LINE>You've hidden, walked places no others have.<LINE>But now is the time to come out and fight.",
                    "A failed warp has brought you here.<LINE>You must now fight for your life<LINE>With no difference between friend and foe.",
                    "Distant and marooned amongst the waves.<LINE>Will you weave with the tide or against it?",
                    "Shattered by destiny, exiled to the unknown.<LINE>Step out of your ripple to find answers.<LINE>No matter the cost.",
                    "You have bore witness to unforeseen catastrophes<LINE>Watched as the world crumbles around you<LINE>You have stood in the shadows long enough.",
                    "CONSIDER:<LINE>A ripple in silent waters,<LINE>an echo of fear,<LINE>a cycle of violence.",
                    "Oh, before you go... a gift.<LINE>Perhaps we are not so different after all."
                ];
            }

            if (OnlineManager.instance.manager.rainWorld.flatIllustrations || (ModManager.MMF && (OnlineManager.instance.manager.rainWorld.options.quality == Options.Quality.MEDIUM || OnlineManager.instance.manager.rainWorld.options.quality == Options.Quality.LOW)))
            {
                slugcatSelectMenuScenes.Add("MeadowRandom", MenuScene.SceneID.Empty);
            }
            else
            {
                slugcatSelectMenuScenes.Add("MeadowRandom", MenuScene.SceneID.Endgame_Traveller);
            }



            if ((OnlineManager.mePlayer.id.name == "IVLD") || (UnityEngine.Random.Range(0, 4) == 0))
            {
                StringBuilder randomDescBuilder = new();
                if (ModManager.MSC) randomDescBuilder.Append(Utils.Translate("Am I Warrior from the past, or a Messiah from the future?"));
                else randomDescBuilder.Append(Utils.Translate("Am I Cat Searching for many, or a Mouse searching for one?"));
                if (ModManager.Watcher) randomDescBuilder.Append(Utils.Translate("<LINE>Am I a doomed Samaritan, or an Anomaly across time and space?"));
                else randomDescBuilder.Append(Utils.Translate("<LINE>Am I doomed a Samaritan, or am I forever stuck in your shadow?"));
                randomDescBuilder.Append(Utils.Translate("<LINE>I do not know, for I am not one. I am many."));
                slugcatSelectDescriptions.Add("MeadowRandom", randomDescBuilder.ToString());
            }
            else
            {
                slugcatSelectDescriptions.Add("MeadowRandom", "Those who walk a single path may find great treasure.<LINE>Those who wander many paths will find great truth.");
            }

            slugcatSelectDisplayNames.Add("MeadowRandom", "THE UNKNOWN");

            this.AddExternalGameModes(FFA.FFAMode, new FFA());
            this.AddExternalGameModes(TeamBattleMode.TeamBattle, new TeamBattleMode());

        }

        public void ResetInvDetails()
        {
            lizardEvent = UnityEngine.Random.Range(0, 100);
            painCatThrowingSkill = UnityEngine.Random.Range(-1, 3);
            int whichPaincatName = UnityEngine.Random.Range(0, 7);
            switch (whichPaincatName)
            {
                case 1:
                    paincatName = "Paincat";
                    break;
                case 2:
                    paincatName = "Inv";
                    break;
                case 3:
                    paincatName = "Enot";
                    break;
                case 4:
                    paincatName = "Sofanthiel";
                    break;
                case 5:
                    paincatName = "Gorbo";
                    break;
                case 6:
                    paincatName = "???";
                    break;
            }

        }

        public bool AddRemoveBannedSlug(int slugcatIndex)
        {
            if (bannedSlugs.Contains(slugcatIndex))
            {
                RainMeadow.Debug($"Removing slugcat index: {slugcatIndex}");
                bannedSlugs.Remove(slugcatIndex);
                return false;
            }
            RainMeadow.Debug($"Adding slugcat index: {slugcatIndex}");
            bannedSlugs.Add(slugcatIndex);
            return true;
        }
        public int GetNewAvailableSlugcatIndex(int slugcatIndex) //has to be part of selectableSlugcats
        {
            int newIndex = slugcatIndex;
            while (bannedSlugs.Contains(newIndex))
            {
                newIndex++;
                newIndex %= ArenaHelpers.selectableSlugcats.Count;
                if (newIndex == slugcatIndex)
                    break; //just incase;
            }
            return newIndex;
        }
        public SlugcatStats.Name[] AvailableSlugcats() => [.. ArenaHelpers.selectableSlugcats.Where((x, i) => !bannedSlugs.Contains(i))];
        public void AddExternalGameModes(ArenaSetup.GameTypeID gametypeID, ExternalArenaGameMode externMode) // external mods will hook and insert
        {

            if (!this.registeredGameModes.ContainsKey(gametypeID.value))
            {
                this.registeredGameModes.Add(gametypeID.value, externMode);
            }
        }
        public void ResetChampAddition()
        {
            this.addedChampstoList = false;
        }

        public void ResetForceReadyCountDown()
        {
            this.forceReadyCountdownTimer = 15;
        }

        public void ResetForceReadyCountDownShort()
        {
            if (this.forceReadyCountdownTimer < 5)
            {
                this.forceReadyCountdownTimer = 5;
            }
        }
        public void ResetScrollTimer()
        {
            this.scrollInitiatedTimer = 0;

        }

        public void ResetAtSession_ctor()
        {
            ResetScrollTimer();
            ResetInvDetails();
            ResetChampAddition();
            AllowJoinOrRejoin();
        }

        public void ResetAtNextLevel()
        {
            InitializeSlugcat();
            ResetScrollTimer();
            ResetGameTimer();
            ResetPlayersEntered();
            ResetChampAddition();

        }
        public void RestartGame()
        {
            if (RWCustom.Custom.rainWorld.processManager.currentMainLoop is not RainWorldGame game) return;
            for (int i = arenaSittingOnlineOrder.Count - 1; i >= 0; i--)
            {
                OnlinePlayer? missingPlayer = ArenaHelpers.FindOnlinePlayerByLobbyId(arenaSittingOnlineOrder[i]);
                if (missingPlayer is null)
                {
                    arenaSittingOnlineOrder.RemoveAt(i);
                }
            }

            AbstractRoom absRoom = game.world.abstractRooms[0];
            Room room = absRoom.realizedRoom;
            WorldSession worldSession = WorldSession.map.GetValue(game.world, (w) => throw new KeyNotFoundException());

            if (RoomSession.map.TryGetValue(absRoom, out var roomSession))
            {
                // we go over all APOs in the room
                RainMeadow.Debug("Restarting level...");
                var entities = absRoom.entities;
                for (int i = entities.Count - 1; i >= 0; i--)
                {
                    if (entities[i] is AbstractPhysicalObject apo && OnlinePhysicalObject.map.TryGetValue(apo, out var oe))
                    {
                        oe.apo.LoseAllStuckObjects();
                        if (!oe.isMine)
                        {
                            // not-online-aware removal
                            RainMeadow.Debug("removing remote entity from game " + oe);
                            oe.beingMoved = true;

                            if (oe.apo.realizedObject is Creature c && c.inShortcut)
                            {
                                if (c.RemoveFromShortcuts()) c.inShortcut = false;
                            }

                            entities.Remove(oe.apo);

                            absRoom.creatures.Remove(oe.apo as AbstractCreature);
                            if (oe.apo.realizedObject != null)
                            {
                                room.RemoveObject(oe.apo.realizedObject);
                                room.CleanOutObjectNotInThisRoom(oe.apo.realizedObject);
                            }
                            oe.beingMoved = false;
                        }
                        else // mine leave the old online world elegantly
                        {
                            RainMeadow.Debug("removing my entity from online " + oe);
                            oe.ExitResource(roomSession);
                            oe.ExitResource(roomSession.worldSession);
                        }
                    }
                }
            }

            List<OnlinePlayer> restartingGamePlayers = new();
            var arenaSitting = game.GetArenaGameSession.arenaSitting;
            List<OnlinePlayer> waitingPlayers = [.. OnlineManager.players.Where(x => ArenaHelpers.GetArenaClientSettings(x)?.ready == true && !x.isMe)];
            arenaSitting.players.Clear();
            for (int i = 0; i < arenaSittingOnlineOrder.Count; i++)
            {
                OnlinePlayer? pl = ArenaHelpers.FindOnlinePlayerByLobbyId(arenaSittingOnlineOrder[i]);
                if (pl != null)
                {
                    ArenaSitting.ArenaPlayer newArenaPlayer = new(i)
                    {
                        playerNumber = i,
                        playerClass = ArenaHelpers.GetArenaClientSettings(pl)!.playingAs,
                        hasEnteredGameArea = true
                    };

                    RainMeadow.Debug($"Arena: Local Sitting Data: {newArenaPlayer.playerNumber}: {newArenaPlayer.playerClass}");
                    AddOrInsertPlayerStats(this, newArenaPlayer, pl);
                    restartingGamePlayers.Add(pl);
                    arenaSitting.players.Add(newArenaPlayer);
                }
            }

            // Add waiting players
            if (allowJoiningMidRound)
            {
                foreach (OnlinePlayer player in waitingPlayers)
                {
                    if (player != null) // always gotta check in case something happened to them
                    {
                        if (!arenaSittingOnlineOrder.Contains(player.inLobbyId) && OnlineManager.lobby.isOwner)
                        {
                            arenaSittingOnlineOrder.Add(player.inLobbyId);
                        }
                        ArenaSitting.ArenaPlayer newArenaPlayer = new(arenaSittingOnlineOrder.Count - 1)
                        {
                            playerNumber = arenaSittingOnlineOrder.Count - 1,
                            playerClass = ArenaHelpers.GetArenaClientSettings(player)!.playingAs,
                            hasEnteredGameArea = true
                        };
                        RainMeadow.Debug($"Arena: Local Sitting Data: {newArenaPlayer.playerNumber}: {newArenaPlayer.playerClass}");
                        AddOrInsertPlayerStats(this, newArenaPlayer, player);
                        arenaSitting.players.Add(newArenaPlayer);
                    }
                }
            }

            if (OnlineManager.lobby.isOwner)
            {
                foreach (OnlinePlayer player in restartingGamePlayers)
                {
                    if (!player.isMe) player.InvokeOnceRPC(ArenaRPCs.Arena_RestartGame);
                }
            }

            game.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Game);
        }
        
        public void InitializeSlugcat()
        {
            int slugIndex = ArenaHelpers.selectableSlugcats.FindIndex(x => x.Equals(arenaClientSettings.playingAs)), newSlugIndex = GetNewAvailableSlugcatIndex(slugIndex);
            if (slugIndex != newSlugIndex)
            {
                myArenaSetup.playerClass[0] = ArenaHelpers.selectableSlugcats.GetValueOrDefault(newSlugIndex, arenaClientSettings.playingAs)!;
                arenaClientSettings.playingAs = myArenaSetup.playerClass[0]!; //try to prevent cheats ig
            }

            if (arenaClientSettings.playingAs == RainMeadow.Ext_SlugcatStatsName.OnlineRandomSlugcat)
            {
                System.Random random = new System.Random((int)DateTime.Now.Ticks);
                SlugcatStats.Name[] allowedSelectableScugs = AvailableSlugcats(), allowedPlayableScugs = [.. ArenaHelpers.allSlugcats.Where(allowedSelectableScugs.Contains)];
                allowedPlayableScugs = allowedPlayableScugs.Length == 0 ? [.. ArenaHelpers.allSlugcats] : allowedPlayableScugs;
                avatarSettings.playingAs = allowedPlayableScugs[random.Next(allowedPlayableScugs.Length)];
                arenaClientSettings.randomPlayingAs = avatarSettings.playingAs;
            }
            else
            {
                avatarSettings.playingAs = arenaClientSettings.playingAs;
            }
            avatarSettings.currentColors = OnlineManager.instance.manager.rainWorld.progression.GetCustomColors(avatarSettings.playingAs);
            arenaClientSettings.slugcatColor = OnlineManager.instance.manager.rainWorld.progression.IsCustomColorEnabled(avatarSettings.playingAs) ? ColorHelpers.HSL2RGB(ColorHelpers.RWJollyPicRange(OnlineManager.instance.manager.rainWorld.progression.GetCustomColorHSL(avatarSettings.playingAs, 0))) : Color.black;
        }

        public void SetProfileColor(ArenaOnlineGameMode arena)
        {
            int profileColor = 0;
            for (int i = 0; i < arena.arenaSittingOnlineOrder.Count; i++)
            {
                var currentPlayer = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(arena, i);

                if (ArenaHelpers.baseGameSlugcats.Contains(arena.avatarSettings.playingAs) && ModManager.MSC)
                {
                    profileColor = UnityEngine.Random.Range(0, 4);
                    arena.playerResultColors[currentPlayer.GetUniqueID()] = profileColor;
                }
                else
                {
                    arena.playerResultColors[currentPlayer.GetUniqueID()] = profileColor;
                }

            }
        }

        public void CheckToAddPlayerStatsToDicts(OnlinePlayer getPlayer)
        {
            if (!playerNumberWithScore.ContainsKey(getPlayer.inLobbyId))
            {
                playerNumberWithScore.Add(getPlayer.inLobbyId, 0);
            }
            if (!playerNumberWithDeaths.ContainsKey(getPlayer.inLobbyId))
            {
                playerNumberWithDeaths.Add(getPlayer.inLobbyId, 0);
            }
            if (!playerNumberWithWins.ContainsKey(getPlayer.inLobbyId))
            {
                playerNumberWithWins.Add(getPlayer.inLobbyId, 0);
            }
            if (!playerTotScore.ContainsKey(getPlayer.inLobbyId))
            {
                playerTotScore.Add(getPlayer.inLobbyId, 0);
            }
            if (!playerNumberWithTrophies.ContainsKey(getPlayer.inLobbyId))
            {
                playerNumberWithTrophies.Add(getPlayer.inLobbyId, new List<string>());
            }
        }

        public void ReadFromStats(ArenaSitting.ArenaPlayer player, OnlinePlayer pl)
        {
            if (playerNumberWithWins.TryGetValue(pl.inLobbyId, out var wins))
            {
                player.wins = wins;
                player.score = playerNumberWithScore[pl.inLobbyId];
                player.deaths = playerNumberWithDeaths[pl.inLobbyId];
                player.totScore = playerTotScore[pl.inLobbyId];
                player.allKills = ArenaHelpers.GetOnlinePlayerTrophies(this, player.playerNumber);

                RainMeadow.Debug($"Read stats: {player} from online player: {pl}");
                RainMeadow.Debug($"Read witih stats: {player.wins} from online player: {player}");
                RainMeadow.Debug($"Read witih score stats: {player.score} from online player: {player}");
                RainMeadow.Debug($"Read witih death stats: {player.deaths} from online player: {player}");
                RainMeadow.Debug($"Read witih totScore stats: {player.totScore} from online player: {player}");
                RainMeadow.Debug($"Client read stats with allKills stats: {player.allKills} from online player: {player}");

            }
        }
        public void AddOrInsertPlayerStats(ArenaOnlineGameMode arena, ArenaSitting.ArenaPlayer newArenaPlayer, OnlinePlayer pl)
        {

            if (arena.playerNumberWithWins.TryGetValue(pl.inLobbyId, out var wins))
            {
                if (OnlineManager.lobby.isOwner)
                {
                    arena.playerNumberWithWins[pl.inLobbyId] += newArenaPlayer.wins;
                    arena.playerNumberWithScore[pl.inLobbyId] += newArenaPlayer.score;
                    arena.playerNumberWithDeaths[pl.inLobbyId] += newArenaPlayer.deaths;
                    arena.playerTotScore[pl.inLobbyId] += newArenaPlayer.totScore;
                    if (arena.playerNumberWithTrophies[pl.inLobbyId].Count < ArenaHelpers.GetPlayerTrophies(arena, newArenaPlayer).Count)
                    {
                        arena.playerNumberWithTrophies[pl.inLobbyId] = ArenaHelpers.GetPlayerTrophies(arena, newArenaPlayer);
                    }
                    RainMeadow.Debug($"Player found witih stats: {newArenaPlayer} from online player: {pl}");
                    RainMeadow.Debug($"Player found witih stats: {newArenaPlayer.wins} from online player: {pl} => NOW {arena.playerNumberWithWins[pl.inLobbyId]} ");
                    RainMeadow.Debug($"Player found witih score stats: {newArenaPlayer.score} from online player: {pl} {arena.playerNumberWithWins[pl.inLobbyId]} ");
                    RainMeadow.Debug($"Player found witih death stats: {newArenaPlayer.deaths} from online player: {pl} {arena.playerNumberWithWins[pl.inLobbyId]} ");
                    RainMeadow.Debug($"Player found witih totScore stats: {newArenaPlayer.totScore} from online player: {pl} {arena.playerNumberWithWins[pl.inLobbyId]}");
                    RainMeadow.Debug($"Client read stats with allKills stats: {newArenaPlayer.allKills} from online player: {pl} {arena.playerNumberWithTrophies[pl.inLobbyId]}");

                }
                else
                {
                    newArenaPlayer.wins = wins;
                    newArenaPlayer.score = arena.playerNumberWithScore[pl.inLobbyId];
                    newArenaPlayer.deaths = arena.playerNumberWithDeaths[pl.inLobbyId];
                    newArenaPlayer.totScore = arena.playerTotScore[pl.inLobbyId];
                    newArenaPlayer.allKills = ArenaHelpers.GetOnlinePlayerTrophies(arena, newArenaPlayer.playerNumber);

                    RainMeadow.Debug($"Client read stats: {newArenaPlayer} from online player: {pl}");
                    RainMeadow.Debug($"Client read stats witih stats: {newArenaPlayer.wins} from online player: {pl}");
                    RainMeadow.Debug($"Client read stats witih score stats: {newArenaPlayer.score} from online player: {pl}");
                    RainMeadow.Debug($"Client read stats witih death stats: {newArenaPlayer.deaths} from online player: {pl}");
                    RainMeadow.Debug($"Client read stats witih totScore stats: {newArenaPlayer.totScore} from online player: {pl}");
                    RainMeadow.Debug($"Client read stats with allKills stats: {newArenaPlayer.allKills} from online player: {pl}");

                }

            }
            else
            {
                if (OnlineManager.lobby.isOwner)
                {
                    arena.playerNumberWithScore.Add(pl.inLobbyId, newArenaPlayer.score);
                    arena.playerNumberWithDeaths.Add(pl.inLobbyId, newArenaPlayer.deaths);
                    arena.playerNumberWithWins.Add(pl.inLobbyId, newArenaPlayer.wins);
                    arena.playerTotScore.Add(pl.inLobbyId, newArenaPlayer.totScore);
                    arena.playerNumberWithTrophies.Add(pl.inLobbyId, ArenaHelpers.GetPlayerTrophies(arena, newArenaPlayer));

                    RainMeadow.Debug($"New Player assigned witih stats: {newArenaPlayer} from online player: {pl}");
                    RainMeadow.Debug($"New Player assigned witih stats: {newArenaPlayer.wins} from online player: {pl}");
                    RainMeadow.Debug($"New Player assigned witih score stats: {newArenaPlayer.score} from online player: {pl}");
                    RainMeadow.Debug($"New Player assigned witih death stats: {newArenaPlayer.deaths} from online player: {pl}");
                    RainMeadow.Debug($"New Player assigned witih totScore stats: {newArenaPlayer.totScore} from online player: {pl}");
                    RainMeadow.Debug($"New Player assigned witih allKills stats: {newArenaPlayer.allKills} from online player: {pl}");

                }
            }
        }
        public void ResetOnReturnToMenu(ArenaLobbyMenu lobby)
        {
            ResetGameTimer();
            if (externalArenaGameMode != null)
            {
                externalArenaGameMode.ResetOnSessionEnd();
            }
            currentLevel = 0;
            arenaSittingOnlineOrder.Clear();
            playersReadiedUp.list.Clear();
            playersLateWaitingInLobbyForNextRound.Clear();
        }
        public void ResetOnReturnMenu(ProcessManager manager)
        {
            manager.rainWorld.options.DeleteArenaSitting();
            if (!OnlineManager.lobby.isOwner) return;
            isInGame = false;
            leaveForNextLevel = false;
            ResetGameTimer();
            currentLevel = 0;
            lobbyCountDown = 5;
            initiateLobbyCountdown = false;
            playersEqualToOnlineSitting = false;
        }
        public void OnStartGame(ProcessManager manager)
        {
            manager.rainWorld.progression.ClearOutSaveStateFromMemory();
            manager.rainWorld.progression.SaveProgression(true, true);
            if (!OnlineManager.lobby.isOwner) return;
            arenaSittingOnlineOrder.Clear();
            playerNumberWithWins.Clear();
            playerNumberWithDeaths.Clear();
            playerNumberWithScore.Clear();
            playerTotScore.Clear();
            playerNumberWithTrophies.Clear();
        }
        public void ResetReadyUpLogic(ArenaOnlineGameMode arena, ArenaLobbyMenu lobby)
        {
            if (lobby.playButton != null)
            {
                lobby.playButton.menuLabel.text = Utils.Translate("READY?");
                lobby.playButton.inactive = false;

            }
            if (OnlineManager.lobby.isOwner)
            {
                arena.allPlayersReadyLockLobby = arena.playersReadiedUp.list.Count == OnlineManager.players.Count;
                arena.isInGame = false;
                arena.leaveForNextLevel = false;
            }
            if (arena.returnToLobby)
            {
                arena.playersReadiedUp.list.Clear();
                arena.returnToLobby = false;
            }


            lobby.manager.rainWorld.options.DeleteArenaSitting();
            //Nightcat.ResetNightcat();


        }

        public void AllowJoinOrRejoin()
        {
            if (allowJoiningMidRound)
            {
                hasPermissionToRejoin = true;
            }
            else
            {
                hasPermissionToRejoin = currentLevel == 0;
            }
        }
        public void ResetGameTimer()
        {
            setupTime = RainMeadow.rainMeadowOptions.ArenaCountDownTimer.Value;
            trackSetupTime = setupTime;
        }

        public void ResetPlayersEntered()
        {
            playersEqualToOnlineSitting = false;
        }

        public override bool ShouldLoadCreatures(RainWorldGame game, WorldSession worldSession)
        {
            return false;
        }

        public override ProcessManager.ProcessID MenuProcessId()
        {
            return RainMeadow.Ext_ProcessID.ArenaLobbyMenu;
        }
        public static HashSet<AbstractPhysicalObject.AbstractObjectType> blockList = new()
        {
            AbstractPhysicalObject.AbstractObjectType.BlinkingFlower,
            AbstractPhysicalObject.AbstractObjectType.AttachedBee

        };
        public override bool ShouldSyncAPOInWorld(WorldSession ws, AbstractPhysicalObject apo)
        {
            if (blockList.Contains(apo.type))
            {
                return false;
            }
            if (apo.type == AbstractPhysicalObject.AbstractObjectType.ScavengerBomb) {
                return this.enableBombs;
            }
            if (apo.type == AbstractPhysicalObject.AbstractObjectType.SporePlant) {
                return this.enableBees;
            }
            return true;
        }

        public override bool ShouldSyncAPOInRoom(RoomSession rs, AbstractPhysicalObject apo)
        {
            if (blockList.Contains(apo.type))
            {
                return false;
            }
            if (apo.type == AbstractPhysicalObject.AbstractObjectType.ScavengerBomb) {
                return this.enableBombs;
            }
            if (apo.type == AbstractPhysicalObject.AbstractObjectType.SporePlant) {
                return this.enableBees;
            }
            return true;
        }

        public override bool ShouldRegisterAPO(OnlineResource resource, AbstractPhysicalObject apo)
        {
            if (blockList.Contains(apo.type))
            {
                return false;
            }
            if (apo.type == AbstractPhysicalObject.AbstractObjectType.ScavengerBomb) {
                return this.enableBombs;
            }
            if (apo.type == AbstractPhysicalObject.AbstractObjectType.SporePlant) {
                return this.enableBees;
            }
            return true;
        }

        public override bool PlayerCanOwnResource(OnlinePlayer from, OnlineResource onlineResource)
        {
            if (onlineResource is OverworldSession || onlineResource is WorldSession || onlineResource is RoomSession)
            {
                return lobby.owner == from;
            }
            return true;
        }

        public override void EstablishWorlds(OverworldSession overworldSession)
        {
            overworldSession.EstablishWorld("arena", 0);
        }
        
        public override WorldSession LinkWorld(World world)
        {
            OnlineManager.lobby.overworld.worldSessions.TryGetValue("arena", out var worldSession);
            return worldSession;
        }


        public override bool AllowedInMode(PlacedObject item)
        {
            if (item.type == PlacedObject.Type.StuckDaddy || item.type == DLCSharedEnums.PlacedObjectType.Stowaway)
            {
                return OnlineManager.lobby.isOwner;
            }

            return true;
        }
        private int previousSecond = -1;
        public override void LobbyTick(uint tick)
        {
            if (leaveToRestart)
            {
                leaveToRestart = false;
                RestartGame();
            }
            
            base.LobbyTick(tick);
            if (OnlineManager.lobby.isOwner)
            {
                DateTime currentTime = DateTime.UtcNow;
                int currentSecond = currentTime.Second;
                if (currentSecond != previousSecond)
                {
                    if (forceReadyCountdownTimer > 0)
                    {
                        forceReadyCountdownTimer--;
                    }
                    if (lobbyCountDown > 0 && initiateLobbyCountdown)
                    {
                        lobbyCountDown--;
                    }

                    if (arenaPrepTimer != null)
                    {
                        if (setupTime > 0 && arenaPrepTimer.showMode == TimerMode.Countdown)
                        {
                            setupTime = externalArenaGameMode.TimerDirection(this, setupTime);

                        }
                    }
                    previousSecond = currentSecond;
                }
            }

        }

        public override bool ShouldSpawnRoomItems(RainWorldGame game, RoomSession roomSession)
        {
            return roomSession.owner == null || roomSession.isOwner;
        }


        public override void ResourceAvailable(OnlineResource onlineResource)
        {
            base.ResourceAvailable(onlineResource);

            if (onlineResource is Lobby lobby)
            {
                lobby.AddData(new ArenaLobbyData());
                lobby.AddData(new TeamBattleLobbyData());
            }
        }

        public override void AddClientData()
        {
            clientSettings.AddData(arenaClientSettings);
            clientSettings.AddData(arenaTeamClientSettings);

        }

        public override void ConfigureAvatar(OnlineCreature onlineCreature)
        {
            onlineCreature.AddData(avatarSettings);
        }

        public override void Customize(Creature creature, OnlineCreature oc)
        {
            if (oc.TryGetData<SlugcatCustomization>(out var data))
            {
                RainMeadow.Debug(oc);
                RainMeadow.creatureCustomizations.GetValue(creature, (c) => data);
            }
        }

        public override bool ShouldSpawnFly(FliesWorldAI self, int spawnRoom)
        {
            return externalArenaGameMode.SpawnBatflies(self, spawnRoom);


        }

    }
}
