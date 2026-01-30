using Menu;
using RainMeadow;
using RainMeadow.Arena.ArenaOnlineGameModes.TeamBattle;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
namespace RainMeadow
{
    public class CreatureBrawl : ExternalArenaGameMode
    {

        public static ArenaSetup.GameTypeID CreatureBrawlMode = new ArenaSetup.GameTypeID("Creature Brawl", register: false);

        private int _timerDuration;

        public override ArenaSetup.GameTypeID GetGameModeId
        {
            get
            {
                return CreatureBrawlMode;
            }
            set { GetGameModeId = value; }

        }
        public static bool isCreatureBrawl(ArenaOnlineGameMode arena, out CreatureBrawl cb)
        {

            cb = null;
            if (arena.currentGameMode == CreatureBrawlMode.value)
            {
                cb = (arena.registeredGameModes.FirstOrDefault(x => x.Key == CreatureBrawlMode.value).Value as CreatureBrawl);
                return true;
            }
            return false;

        }


        public ConditionalWeakTable<Creature, Creature> creatureCWT;

        public static Dictionary<int, CreatureTemplate.Type> lizardMounts = new Dictionary<int, CreatureTemplate.Type>()
        {
            { 0, CreatureTemplate.Type.RedLizard },
            { 1, CreatureTemplate.Type.CyanLizard },
            { 2, CreatureTemplate.Type.WhiteLizard },
            { 3, CreatureTemplate.Type.YellowLizard }
        };

        public void SpawnAndMountPlayerWithLizard(ArenaOnlineGameMode arena, ArenaGameSession self, Room room, ShortcutHandler.ShortCutVessel shortCutVessel)
        {
                RainMeadow.sSpawningAvatar = true;
                self.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.Lizards, -1, 0, 1f);
                AbstractCreature bringTheTrain = new AbstractCreature(room.world, StaticWorld.GetCreatureTemplate("Red Lizard"), null, room.GetWorldCoordinate(shortCutVessel.pos), shortCutVessel.room.world.game.GetNewID()); // Train too big :slight_frown: 
                // (bringTheTrain.state as LizardState).SetRotType(LizardState.RotType.Full);
                room.abstractRoom.AddEntity(bringTheTrain);
                bringTheTrain.Realize();
                bringTheTrain.realizedCreature.PlaceInRoom(room);
                self.room.world.GetResource().ApoEnteringWorld(bringTheTrain);
                self.room.abstractRoom.GetResource()?.ApoEnteringRoom(bringTheTrain, bringTheTrain.pos);
               MeadowAvatarData  avatarData = new MeadowAvatarData();
               avatarData.character = MeadowProgression.Character.Lizard;
               avatarData.characterData = new MeadowProgression.CharacterData();
               avatarData.characterData.displayName = "LIZARD";
               avatarData.characterData.emotePrefix = "liz_";
               avatarData.characterData.emoteAtlas = "emotes_lizard";
               avatarData.characterData.emoteColor = new Color(197, 220, 232, 255f) / 255f;
               avatarData.characterData.voiceId = RainMeadow.Ext_SoundID.RM_Lizard_Call;
               avatarData.characterData.selectSpriteIndexes = new[] { 1, 2 };
               avatarData.characterData.startingCoords = room.GetWorldCoordinate(shortCutVessel.pos);
               avatarData.skin = MeadowProgression.Skin.Lizard_Red;
               avatarData.skinData = new MeadowProgression.SkinData();
               avatarData.skinData.character =avatarData.character;
               avatarData.skinData.creatureType =  lizardMounts[0];
               CreatureController.BindAvatar(bringTheTrain.realizedCreature,  bringTheTrain.GetOnlineCreature(), avatarData);
               RainMeadow.sSpawningAvatar = false;
                // arena.avatars.Add(bringTheTrain.GetOnlineCreature());
                this.creatureCWT.Add(shortCutVessel.creature, bringTheTrain.realizedCreature);
        }

        public override void SpawnPlayer(ArenaOnlineGameMode arena, ArenaGameSession self, Room room, List<int> suggestedDens)
        {
            List<OnlinePlayer> list = new List<OnlinePlayer>();
            List<OnlinePlayer> list2 = new List<OnlinePlayer>();

            for (int j = 0; j < OnlineManager.players.Count; j++)
            {
                if (arena.arenaSittingOnlineOrder.Contains(OnlineManager.players[j].inLobbyId))
                {
                    list2.Add(OnlineManager.players[j]);
                }
            }

            while (list2.Count > 0)
            {
                int index = UnityEngine.Random.Range(0, list2.Count);
                list.Add(list2[index]);
                list2.RemoveAt(index);
            }

            int totalExits = self.game.world.GetAbstractRoom(0).exits;
            int[] exitScores = new int[totalExits];
            if (suggestedDens != null)
            {
                for (int k = 0; k < suggestedDens.Count; k++)
                {
                    if (suggestedDens[k] >= 0 && suggestedDens[k] < exitScores.Length)
                    {
                        exitScores[suggestedDens[k]] -= 1000;
                    }
                }
            }

            int randomExitIndex = UnityEngine.Random.Range(0, totalExits);
            float highestScore = float.MinValue;


            for (int currentExitIndex = 0; currentExitIndex < totalExits; currentExitIndex++)
            {
                float score = UnityEngine.Random.value - (float)exitScores[currentExitIndex] * 1000f;
                RWCustom.IntVector2 startTilePosition = room.ShortcutLeadingToNode(currentExitIndex).StartTile;

                for (int otherExitIndex = 0; otherExitIndex < totalExits; otherExitIndex++)
                {
                    if (otherExitIndex != currentExitIndex && exitScores[otherExitIndex] > 0)
                    {
                        float distanceAdjustment = Mathf.Clamp(startTilePosition.FloatDist(room.ShortcutLeadingToNode(otherExitIndex).StartTile), 8f, 17f) * UnityEngine.Random.value;
                        score += distanceAdjustment;
                    }
                }

                if (score > highestScore)
                {
                    randomExitIndex = currentExitIndex;
                    highestScore = score;
                }
            }

            RainMeadow.Debug("Trying to create an abstract creature");
            RainMeadow.Debug($"RANDOM EXIT INDEX: {randomExitIndex}");
            RainMeadow.Debug($"RANDOM START TILE INDEX: {room.ShortcutLeadingToNode(randomExitIndex).StartTile}");
            RainMeadow.sSpawningAvatar = true;
            AbstractCreature abstractCreature = new AbstractCreature(self.game.world, StaticWorld.GetCreatureTemplate("Slugcat"), null, new WorldCoordinate(0, -1, -1, -1), new EntityID(-1, 0));
            abstractCreature.pos.room = self.game.world.GetAbstractRoom(0).index;
            abstractCreature.pos.abstractNode = room.ShortcutLeadingToNode(randomExitIndex).destNode;
            abstractCreature.Room.AddEntity(abstractCreature);

            RainMeadow.Debug("assigned ac, registering");

            self.game.world.GetResource().ApoEnteringWorld(abstractCreature);
            RainMeadow.sSpawningAvatar = false;

            self.game.cameras[0].followAbstractCreature = abstractCreature;

            if (abstractCreature.GetOnlineObject(out var oe) && oe.TryGetData<SlugcatCustomization>(out var customization))
            {
                abstractCreature.state = new PlayerState(abstractCreature, 0, customization.playingAs, isGhost: false);
            }
            else
            {
                RainMeadow.Error("Could not get online owner for spawned player!");
                abstractCreature.state = new PlayerState(abstractCreature, 0, self.arenaSitting.players[ArenaHelpers.FindOnlinePlayerNumber(arena, OnlineManager.mePlayer)].playerClass, isGhost: false);
            }

            RainMeadow.Debug("Arena: Realize Creature!");
            abstractCreature.Realize();
            var shortCutVessel = new ShortcutHandler.ShortCutVessel(room.ShortcutLeadingToNode(randomExitIndex).DestTile, abstractCreature.realizedCreature, self.game.world.GetAbstractRoom(0), 0);
            shortCutVessel.entranceNode = abstractCreature.pos.abstractNode;
            shortCutVessel.room = self.game.world.GetAbstractRoom(abstractCreature.Room.name);

            self.game.shortcuts.betweenRoomsWaitingLobby.Add(shortCutVessel);
            self.AddPlayer(abstractCreature);
            SpawnAndMountPlayerWithLizard(arena, self, room, shortCutVessel);

            if ((abstractCreature.realizedCreature as Player).SlugCatClass == SlugcatStats.Name.Night)
            {
                (abstractCreature.realizedCreature as Player).slugcatStats.throwingSkill = 1;
            }
            if (ModManager.MSC)
            {
                if ((abstractCreature.realizedCreature as Player).SlugCatClass == SlugcatStats.Name.Red)
                {
                    self.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.All, -1, 0, -0.75f);
                    self.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.Scavengers, -1, 0, 0.5f);
                }

                if ((abstractCreature.realizedCreature as Player).SlugCatClass == SlugcatStats.Name.Yellow)
                {
                    self.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.All, -1, 0, 0.75f);
                    self.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.Scavengers, -1, 0, 0.3f);
                }

                if ((abstractCreature.realizedCreature as Player).SlugCatClass == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Artificer)
                {
                    self.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.All, -1, 0, -0.5f);
                    self.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.Scavengers, -1, 0, -1f);
                }

                if ((abstractCreature.realizedCreature as Player).SlugCatClass == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Slugpup)
                {
                    (abstractCreature.realizedCreature as Player).slugcatStats.throwingSkill = 1;
                }

                if ((abstractCreature.realizedCreature as Player).SlugCatClass == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel)
                {
                    (abstractCreature.realizedCreature as Player).slugcatStats.throwingSkill = arena.painCatThrowingSkill;
                    RainMeadow.Debug("ENOT THROWING SKILL " + (abstractCreature.realizedCreature as Player).slugcatStats.throwingSkill);
                    if ((abstractCreature.realizedCreature as Player).slugcatStats.throwingSkill == 0 && arena.painCatEgg)
                    {
                        AbstractPhysicalObject bringThePain = new AbstractPhysicalObject(room.world, DLCSharedEnums.AbstractObjectType.SingularityBomb, null, abstractCreature.pos, shortCutVessel.room.world.game.GetNewID());
                        room.abstractRoom.AddEntity(bringThePain);
                        bringThePain.RealizeInRoom();

                        self.room.world.GetResource().ApoEnteringWorld(bringThePain);
                        self.room.abstractRoom.GetResource()?.ApoEnteringRoom(bringThePain, bringThePain.pos);
                    }
                }
                
                if ((abstractCreature.realizedCreature as Player).SlugCatClass == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Saint)
                {
                    if (!arena.sainot) // ascendance saint
                    {
                        (abstractCreature.realizedCreature as Player).slugcatStats.throwingSkill = 0;
                    }
                    else
                    {
                        (abstractCreature.realizedCreature as Player).slugcatStats.throwingSkill = 1;

                    }
                }

            }

            if (ModManager.Watcher && (abstractCreature.realizedCreature as Player).SlugCatClass == Watcher.WatcherEnums.SlugcatStatsName.Watcher)
            {
                (abstractCreature.realizedCreature as Player).enterIntoCamoDuration = 40;
            }
            self.playersSpawned = true;
            if (OnlineManager.lobby.isOwner)
            {
                arena.isInGame = true; // used for readied players at the beginning
                arena.leaveForNextLevel = false;
                foreach (var onlineArenaPlayer in arena.arenaSittingOnlineOrder)
                {
                    OnlinePlayer? getPlayer = ArenaHelpers.FindOnlinePlayerByLobbyId(onlineArenaPlayer);
                    if (getPlayer != null)
                    {

                        arena.CheckToAddPlayerStatsToDicts(getPlayer);
                    }
                }
                arena.playersLateWaitingInLobbyForNextRound.Clear();
                arena.hasPermissionToRejoin = false;

            }


        }

        public override bool IsExitsOpen(ArenaOnlineGameMode arena, On.ArenaBehaviors.ExitManager.orig_ExitsOpen orig, ArenaBehaviors.ExitManager self)
        {
            int playersStillStanding = self.gameSession.Players?.Count(player =>
                player.realizedCreature != null &&
                (player.realizedCreature.State.alive)) ?? 0;

            if (playersStillStanding == 1 && arena.arenaSittingOnlineOrder.Count > 1)
            {
                return true;
            }

            if (self.world.rainCycle.TimeUntilRain <= 100)
            {
                return true;
            }

            return orig(self);
        }

        public override bool SpawnBatflies(FliesWorldAI self, int spawnRoom)
        {
            return false;
        }
        public override string TimerText()
        {
            return Utils.Translate("Prepare for chaos,") + " " + Utils.Translate(PlayingAsText());
        }
        public override int SetTimer(ArenaOnlineGameMode arena)
        {
            return arena.setupTime = RainMeadow.rainMeadowOptions.ArenaCountDownTimer.Value;
        }
        public override int TimerDuration
        {
            get { return _timerDuration; }
            set { _timerDuration = value; }
        }
        public override int TimerDirection(ArenaOnlineGameMode arena, int timer)
        {
            return --arena.setupTime;
        }
        public override bool HoldFireWhileTimerIsActive(ArenaOnlineGameMode arena)
        {
            if (arena.setupTime > 0)
            {
                return arena.countdownInitiatedHoldFire = true;
            }
            else
            {
                return arena.countdownInitiatedHoldFire = false;
            }
        }

        public override void LandSpear(ArenaOnlineGameMode arena, ArenaGameSession self, Player player, Creature target, ArenaSitting.ArenaPlayer aPlayer)
        {
            aPlayer.AddSandboxScore(self.GameTypeSetup.spearHitScore);

        }

        public override string AddIcon(ArenaOnlineGameMode arena, PlayerSpecificOnlineHud owner, SlugcatCustomization customization, OnlinePlayer player)
        {

            if (owner.clientSettings.owner == OnlineManager.lobby.owner)
            {
                return "ChieftainA";
            }
            return base.AddIcon(arena, owner, customization, player);

        }

        public override Color IconColor(ArenaOnlineGameMode arena, OnlinePlayerDisplay display, PlayerSpecificOnlineHud owner, SlugcatCustomization customization, OnlinePlayer player)
        {
            if (owner.PlayerConsideredDead)
            {
                return Color.grey;
            }
            if (arena.reigningChamps != null && arena.reigningChamps.list != null && arena.reigningChamps.list.Contains(player.id))
            {
                return Color.yellow;
            }

            return base.IconColor(arena, display, owner, customization, player);
        }

        public override Dialog AddGameModeInfo(ArenaOnlineGameMode arena, Menu.Menu menu)
        {
            return new DialogNotify(menu.LongTranslate("Trust no one. Last scug standing wins"), new Vector2(500f, 400f), menu.manager, () => { menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed); });
        }

        public override void ArenaSessionCtor(ArenaOnlineGameMode arena, On.ArenaGameSession.orig_ctor orig, ArenaGameSession self, RainWorldGame game)
        {
            base.ArenaSessionCtor(arena, orig, self, game);
            this.creatureCWT = new ConditionalWeakTable<Creature, Creature>();
        }
    }
}
