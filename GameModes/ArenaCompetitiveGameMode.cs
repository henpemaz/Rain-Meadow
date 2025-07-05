using System;
using System.Collections.Generic;
using System.Text;
using Menu;
using MoreSlugcats;
using UnityEngine;
using static RainMeadow.ArenaPrepTimer;

namespace RainMeadow
{
    public class ArenaOnlineGameMode : OnlineGameMode
    {
        public ArenaOnlineSetup myArenaSetup;
        public ExternalArenaGameMode onlineArenaGameMode;
        public string currentGameMode;
        public Dictionary<ExternalArenaGameMode, string> registeredGameModes;

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

        public bool sainot = RainMeadow.rainMeadowOptions.ArenaSAINOT.Value;
        public bool painCatThrows = RainMeadow.rainMeadowOptions.PainCatThrows.Value;
        public bool painCatEgg = RainMeadow.rainMeadowOptions.PainCatEgg.Value;
        public bool painCatLizard = RainMeadow.rainMeadowOptions.PainCatLizard.Value;
        public bool disableMaul = RainMeadow.rainMeadowOptions.BlockMaul.Value;
        public bool disableArtiStun = RainMeadow.rainMeadowOptions.BlockArtiStun.Value;
        public bool itemSteal = RainMeadow.rainMeadowOptions.ArenaItemSteal.Value;
        public bool allowJoiningMidRound = RainMeadow.rainMeadowOptions.ArenaAllowMidJoin.Value;
        public bool weaponCollisionFix = RainMeadow.rainMeadowOptions.WeaponCollisionFix.Value;

        public string paincatName;
        public int lizardEvent;

        public override bool PlayersCanHandhold => false;

        public Dictionary<string, MenuScene.SceneID> slugcatSelectMenuScenes;
        public Dictionary<string, string> slugcatSelectDescriptions, slugcatSelectDisplayNames;
        public List<string> slugcatSelectPainCatNames = [];
        // have fun fixing this UO ;)
        public List<string> slugcatSelectPainCatNormalDescriptions, slugcatSelectPainCatJokeDescriptions, slugcatSelectPainCatQuoteDescriptions, slugcatSelectPainCatDevJokeDescriptions, slugcatSelectPainCatSmileyDescriptions, slugcatSelectPainCatUwUDescriptions, slugcatSelectPainCatWaveDescriptions, slugcatSelectPainCatDeadDescriptions;

        public Dictionary<string, int> onlineArenaSettingsInterfaceMultiChoice = new Dictionary<string, int>();
        public Dictionary<string, bool> onlineArenaSettingsInterfaceeBool = new Dictionary<string, bool>();
        public Dictionary<string, int> playerResultColors = new Dictionary<string, int>();
        public Generics.DynamicOrderedPlayerIDs playersReadiedUp = new Generics.DynamicOrderedPlayerIDs();
        public Generics.DynamicOrderedPlayerIDs reigningChamps = new Generics.DynamicOrderedPlayerIDs();

        public Dictionary<string, int> playersInLobbyChoosingSlugs = new Dictionary<string, int>();
        public Dictionary<int, int> playerNumberWithKills = new Dictionary<int, int>();
        public Dictionary<int, int> playerNumberWithDeaths = new Dictionary<int, int>();
        public Dictionary<int, int> playerNumberWithWins = new Dictionary<int, int>();


        public int playerEnteredGame;
        public bool clientWantsToLeaveGame;
        public bool countdownInitiatedHoldFire;
        public bool addedChampstoList;
        public bool hasPermissionToRejoin;

        public ArenaPrepTimer arenaPrepTimer;
        public int setupTime = RainMeadow.rainMeadowOptions.ArenaCountDownTimer.Value;
        public int lobbyCountDown;
        public bool initiateLobbyCountdown;
        public int trackSetupTime;
        public int scrollInitiatedTimer;


        public int arenaSaintAscendanceTimer = RainMeadow.rainMeadowOptions.ArenaSaintAscendanceTimer.Value;


        public ArenaClientSettings arenaClientSettings;
        public SlugcatCustomization avatarSettings;

        public bool shufflePlayList;
        public List<string> playList = new List<string>();
        public List<ushort> arenaSittingOnlineOrder = new List<ushort>();
        public List<ushort> playersLateWaitingInLobbyForNextRound = new List<ushort>();

        public ArenaOnlineGameMode(Lobby lobby) : base(lobby)
        {
            ArenaHelpers.RecreateSlugcatCache();
            avatarSettings = new SlugcatCustomization() { nickname = OnlineManager.mePlayer.id.name };
            arenaClientSettings = new ArenaClientSettings();
            playerResultColors = new Dictionary<string, int>();
            registeredGameModes = new Dictionary<ExternalArenaGameMode, string>();
            playerEnteredGame = 0;
            painCatThrowingSkill = 0;
            totalLevelCount = 0;
            currentLevel = 0;
            playerLeftGame = 0;
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
                    ".kcor dna raeps ruoy hctanS<LINE>.emit tsrif ruoy ekil ton s'ti tub ,uoy dnuora ni esolc seimene ruoY",
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
                slugcatSelectDescriptions.Add("Watcher", "Open: Voices. Heat. Burdened.<LINE>Closed: Whispers. Freezing. Drowning.<LINE>Open: Echoes. Balance. Weightless.");
                slugcatSelectDisplayNames.Add("Watcher", "THE WATCHER");

                slugcatSelectMenuScenes.Remove("Night");
                slugcatSelectDescriptions.Remove("Night");
                slugcatSelectDisplayNames.Remove("Night");
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

        public void AddExternalGameModes(ExternalArenaGameMode externMode, ArenaSetup.GameTypeID gametypeID) // external mods will hook and insert
        {

            if (!this.registeredGameModes.ContainsKey(externMode))
            {
                this.registeredGameModes.Add(externMode, gametypeID.value);
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
        }

        public void ResetAtNextLevel()
        {
            InitializeSlugcat();
            ResetScrollTimer();
            ResetGameTimer();
            ResetPlayersEntered();
            ResetChampAddition();

        }

        public void InitializeSlugcat()
        {
            if (arenaClientSettings.playingAs == RainMeadow.Ext_SlugcatStatsName.OnlineRandomSlugcat)
            {
                System.Random random = new System.Random((int)DateTime.Now.Ticks);
                avatarSettings.playingAs = ArenaHelpers.allSlugcats[random.Next(ArenaHelpers.allSlugcats.Count)]!;
                arenaClientSettings.randomPlayingAs = avatarSettings.playingAs;
            }
            else
            {
                avatarSettings.playingAs = arenaClientSettings.playingAs;
            }
            avatarSettings.currentColors = OnlineManager.instance.manager.rainWorld.progression.GetCustomColors(avatarSettings.playingAs);
            arenaClientSettings.slugcatColor = OnlineManager.instance.manager.rainWorld.progression.IsCustomColorEnabled(avatarSettings.playingAs) ? ColorHelpers.HSL2RGB(ColorHelpers.RWJollyPicRange(OnlineManager.instance.manager.rainWorld.progression.GetCustomColorHSL(avatarSettings.playingAs, 0))) : Color.black;
        }

        public void ResetGameTimer()
        {
            setupTime = RainMeadow.rainMeadowOptions.ArenaCountDownTimer.Value;
            trackSetupTime = setupTime;
        }

        public void ResetPlayersEntered()
        {
            playerEnteredGame = 0;
        }

        public override bool ShouldLoadCreatures(RainWorldGame game, WorldSession worldSession)
        {
            return false;
        }

        public override ProcessManager.ProcessID MenuProcessId()
        {
            return RainMeadow.Ext_ProcessID.ArenaLobbyMenu;
        }
        static HashSet<AbstractPhysicalObject.AbstractObjectType> blockList = new()
        {
            AbstractPhysicalObject.AbstractObjectType.BlinkingFlower,
            AbstractPhysicalObject.AbstractObjectType.SporePlant,
            AbstractPhysicalObject.AbstractObjectType.AttachedBee

        };
        public override bool ShouldSyncAPOInWorld(WorldSession ws, AbstractPhysicalObject apo)
        {
            if (blockList.Contains(apo.type))
            {
                return false;
            }
            return true;
        }

        public override bool ShouldSyncAPOInRoom(RoomSession rs, AbstractPhysicalObject apo)
        {
            if (blockList.Contains(apo.type))
            {
                return false;
            }
            return true;
        }

        public override bool ShouldRegisterAPO(OnlineResource resource, AbstractPhysicalObject apo)
        {
            if (blockList.Contains(apo.type))
            {
                return false;
            }
            return true;
        }
        public override bool PlayerCanOwnResource(OnlinePlayer from, OnlineResource onlineResource)
        {
            if (onlineResource is WorldSession || onlineResource is RoomSession)
            {
                return lobby.owner == from;
            }
            return true;
        }


        public override void PlayerLeftLobby(OnlinePlayer player)
        {
            base.PlayerLeftLobby(player);
            if (player == lobby.owner)
            {
                OnlineManager.instance.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.MainMenu);
            }
        }

        public override bool AllowedInMode(PlacedObject item)
        {
            if (item.type == PlacedObject.Type.SporePlant)
            {
                return false;
            }

            return base.AllowedInMode(item) || playerGrabbableItems.Contains(item.type);
        }
        private int previousSecond = -1;
        public override void LobbyTick(uint tick)
        {
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
                            setupTime = onlineArenaGameMode.TimerDirection(this, setupTime);

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
            }
        }

        public override void AddClientData()
        {
            clientSettings.AddData(arenaClientSettings);
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
            return onlineArenaGameMode.SpawnBatflies(self, spawnRoom);


        }

    }
}