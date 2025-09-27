using System;
using System.Collections.Generic;
using Menu;
using RainMeadow.Generics;
using UnityEngine;
using static RainMeadow.OnlineResource;

namespace RainMeadow
{
    //playerSessionData - passage
    public class StoryLobbyData : OnlineResource.ResourceData
    {
        public override ResourceDataState MakeState(OnlineResource resource)
        {
            return new State(this, resource);
        }

        public class State : ResourceDataState
        {
            [OnlineField(nullable = true)]
            public string? defaultDenPos;
            [OnlineField(nullable = true)]
            public string? region;
            [OnlineField(nullable = true)]
            public string? defaultWarpPos; //this is mainly so clients do not brick saves when activating echoes
            [OnlineField]
            public bool isInGame;
            [OnlineField]
            public bool changedRegions;
            [OnlineField]
            public bool readyForWin;
            [OnlineField]
            public byte readyForTransition;
            [OnlineField]
            public bool friendlyFire;
            [OnlineField]
            public SlugcatStats.Name currentCampaign;
            [OnlineField]
            public int cycleNumber;
            [OnlineField]
            public bool reinforcedKarma;
            [OnlineField]
            public int karmaCap;
            [OnlineField]
            public int karma;
            [OnlineField]
            public bool theGlow;
            [OnlineField]
            public int food;
            [OnlineField]
            public int quarterfood;
            [OnlineField]
            public int mushroomCounter;
            [OnlineField(nullable = true)]
            public string? saveStateString;
            [OnlineField]
            public bool lastMalnourished;

            [OnlineField]
            public bool malnourished;
            [OnlineField]
            public bool requireCampaignSlugcat;
            [OnlineField]
            public List<OnlineEntity.EntityId> pups;
            [OnlineField]
            public bool storyItemSteal;

            //watcher stuff
            // TODO: Food for tought, what if we use a LUT and encode these in bytes? afterall
            // we know they can only go from 1-10 (integer) and 0, 0.25 and 0.5
            [OnlineFieldHalf]
            public float rippleLevel;
            [OnlineFieldHalf]
            public float minimumRippleLevel;
            [OnlineFieldHalf]
            public float maximumRippleLevel;

            [OnlineField(nullable = true)]
            public MenuSaveStateState? currentMenuSaveState;  

            public State() { }

            public State(StoryLobbyData storyLobbyData, OnlineResource onlineResource)
            {
                StoryGameMode storyGameMode = (onlineResource as Lobby).gameMode as StoryGameMode;
                RainWorldGame currentGameState = RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame;

                defaultDenPos = storyGameMode.defaultDenPos;
                currentCampaign = storyGameMode.currentCampaign;
                requireCampaignSlugcat = storyGameMode.requireCampaignSlugcat;
                rippleLevel = storyGameMode.rippleLevel;

                isInGame = RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame && RWCustom.Custom.rainWorld.processManager.upcomingProcess is null;
                changedRegions = storyGameMode.changedRegions;
                readyForWin = storyGameMode.readyForWin;
                readyForTransition = (byte)storyGameMode.readyForTransition;
                saveStateString = storyGameMode.saveStateString;
                storyItemSteal = storyGameMode.itemSteal;
                if (currentGameState?.session is StoryGameSession storySession)
                {
                    cycleNumber = storySession.saveState.cycleNumber;
                    karma = storySession.saveState.deathPersistentSaveData.karma;
                    karmaCap = storySession.saveState.deathPersistentSaveData.karmaCap;
                    minimumRippleLevel = storySession.saveState.deathPersistentSaveData.minimumRippleLevel;
                    maximumRippleLevel = storySession.saveState.deathPersistentSaveData.maximumRippleLevel;
                    theGlow = storySession.saveState.theGlow;
                    reinforcedKarma = storySession.saveState.deathPersistentSaveData.reinforcedKarma;
                    malnourished = storySession.saveState.malnourished;
                    lastMalnourished = storySession.saveState.lastMalnourished;

                    currentMenuSaveState = new MenuSaveStateState(storySession.saveState);
                }
                else
                {
                    if (RWCustom.Custom.rainWorld.processManager.currentMainLoop is StoryOnlineMenu sOM)
                    {
                        if (sOM.saveGameData?[storyGameMode.currentCampaign] is SlugcatSelectMenu.SaveGameData data)
                        {
                            currentMenuSaveState = new MenuSaveStateState(data);
                        }
                        else
                        {
                            currentMenuSaveState = null;
                        }
                    }
                    else
                    {
                        currentMenuSaveState = null;
                    }
                }

                

                food = (currentGameState?.Players[0].state as PlayerState)?.foodInStomach ?? 0;
                quarterfood = (currentGameState?.Players[0].state as PlayerState)?.quarterFoodPoints ?? 0;
                mushroomCounter = (currentGameState?.Players[0].realizedCreature as Player)?.mushroomCounter ?? 0;

                pups = new();
                foreach (AbstractCreature apo in storyGameMode.pups)
                {
                    if (OnlinePhysicalObject.map.TryGetValue(apo, out var oe))
                    {
                        pups.Add(oe.id);
                    }
                }
                friendlyFire = storyGameMode.friendlyFire;
                region = storyGameMode.region;
            }

            public override Type GetDataType() => typeof(StoryLobbyData);

            public override void ReadTo(ResourceData data, OnlineResource resource)
            {
                RainWorldGame currentGameState = RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame;

                var lobby = (resource as Lobby);
                var story = (StoryGameMode)lobby.gameMode;

                (lobby.gameMode as StoryGameMode).rippleLevel = rippleLevel;

                if (currentGameState is not null)
                {
                    for (int i = 0; i < currentGameState.StoryPlayerCount; i++)
                    {

                        var playerstate = (currentGameState?.Players[i].state as PlayerState);


                        if (playerstate != null)
                        {
                            playerstate.foodInStomach = food;
                            playerstate.quarterFoodPoints = quarterfood;
                        }

                        if ((currentGameState?.Players[i].realizedCreature is Player player))
                        {
                            player.mushroomCounter = mushroomCounter;
                            player.AddFood(0); // refreshes malnourished and reds illness state
                        }
                    }
                }

                if (story.menuSaveState != currentMenuSaveState)
                {
                    story.menuSaveState = currentMenuSaveState;
                    story.menuSaveGameData = story.menuSaveState?.CreateSaveData();
                    story.needMenuSaveUpdate = true;
                }
                
                

                if (currentGameState?.session is StoryGameSession storySession)
                {
                    storySession.saveState.cycleNumber = cycleNumber;
                    storySession.saveState.deathPersistentSaveData.karma = karma;
                    storySession.saveState.deathPersistentSaveData.karmaCap = karmaCap;
                    storySession.saveState.deathPersistentSaveData.rippleLevel = rippleLevel;

                    storySession.saveState.deathPersistentSaveData.minimumRippleLevel = minimumRippleLevel;
                    storySession.saveState.deathPersistentSaveData.maximumRippleLevel = maximumRippleLevel;
                    storySession.saveState.deathPersistentSaveData.reinforcedKarma = reinforcedKarma;
                    storySession.saveState.theGlow = theGlow;
                    storySession.saveState.lastMalnourished = lastMalnourished;
                    storySession.saveState.malnourished = malnourished;
                    for (int i = 0; i < currentGameState.StoryPlayerCount; i++)
                    {
                        if (currentGameState.Players[i].realizedCreature != null)
                            (currentGameState.Players[i].realizedCreature as Player).glowing = theGlow;
                    }
                }
                story.currentCampaign = currentCampaign;

                story.requireCampaignSlugcat = requireCampaignSlugcat;
                story.isInGame = isInGame;
                story.changedRegions = changedRegions;
                story.readyForWin = readyForWin;
                story.readyForTransition = (StoryGameMode.ReadyForTransition)readyForTransition;
                story.friendlyFire = friendlyFire;
                story.region = region;

                story.saveStateString = saveStateString;
                story.itemSteal = storyItemSteal;


                foreach (OnlineEntity.EntityId pupid in pups)
                {
                    if ((pupid.FindEntity() as OnlineCreature)?.apo is AbstractCreature apo)
                    {
                        (lobby.gameMode as StoryGameMode).pups.Add(apo);
                    }
                }
            }
        }

        public class MenuSaveStateState : Serializer.ICustomSerializable
        {
            public int karma;
            public float rippleLevel;
            public int food;
            public int cycle;
            public bool karmaReinforced;
            public bool hasGlow;
            public bool hasMark;
            public bool ascended;
            public bool altEnd;
            public string shelterName;
            public int gameTimeAlive;
            public int gameTimeDead;

            public MenuSaveStateState() { }
            public MenuSaveStateState(SlugcatSelectMenu.SaveGameData saveData)
            {
                karma = saveData.karma;
                rippleLevel = saveData.rippleLevel;
                food = saveData.food;
                cycle = saveData.cycle;
                karmaReinforced = saveData.karmaReinforced;
                hasGlow = saveData.hasGlow;
                hasMark = saveData.hasMark;
                ascended = saveData.ascended;
                altEnd = saveData.altEnding;
                shelterName = saveData.shelterName;
                gameTimeAlive = saveData.gameTimeAlive;
                gameTimeDead = saveData.gameTimeDead;
            }

            public MenuSaveStateState(SaveState saveState)
            {
                karma = saveState.deathPersistentSaveData.karma;
                rippleLevel = saveState.deathPersistentSaveData.rippleLevel;
                food = saveState.food;
                cycle = saveState.cycleNumber;
                karmaReinforced = saveState.deathPersistentSaveData.reinforcedKarma;
                hasGlow = saveState.theGlow;
                hasMark = saveState.deathPersistentSaveData.theMark;
                ascended = saveState.deathPersistentSaveData.ascended;
                altEnd = saveState.deathPersistentSaveData.altEnding;
                shelterName = saveState.GetSaveStateDenToUse();
                gameTimeAlive = saveState.totTime;
                gameTimeDead = saveState.deathPersistentSaveData.deathTime;
            }

            public SlugcatSelectMenu.SaveGameData CreateSaveData()
            {
                var saveGameData = new SlugcatSelectMenu.SaveGameData();
                saveGameData.karma = karma;
                saveGameData.karmaCap = karma; // undisplayed
                saveGameData.rippleLevel = rippleLevel;
                saveGameData.food = food;
                saveGameData.cycle = cycle;
                saveGameData.karmaReinforced = karmaReinforced;
                saveGameData.hasGlow = hasGlow;
                saveGameData.hasMark = hasMark;
                saveGameData.ascended = ascended;
                saveGameData.altEnding = altEnd;
                saveGameData.shelterName = shelterName;
                saveGameData.gameTimeAlive = gameTimeAlive;
                saveGameData.gameTimeDead = gameTimeDead;
                
                return saveGameData;
            }

            public void CustomSerialize(Serializer serializer)
            {
                if (serializer.IsWriting)
                {
                    serializer.writer.Write(karmaReinforced);
                    serializer.writer.Write(hasGlow);
                    serializer.writer.Write(hasMark);
                    serializer.writer.Write(ascended);
                    serializer.writer.Write(altEnd);

                    serializer.writer.Write(cycle);
                    serializer.writer.Write(gameTimeAlive);
                    serializer.writer.Write(gameTimeDead);
                    serializer.writer.Write((byte)karma);
                    serializer.writer.Write((byte)Mathf.FloorToInt(rippleLevel / 0.25f));
                    serializer.writer.Write((byte)food);
                    serializer.writer.Write(shelterName);
                }

                if (serializer.IsReading)
                {
                    karmaReinforced = serializer.reader.ReadBoolean();
                    hasGlow = serializer.reader.ReadBoolean();
                    hasMark = serializer.reader.ReadBoolean();
                    ascended = serializer.reader.ReadBoolean();
                    altEnd = serializer.reader.ReadBoolean();

                    cycle = serializer.reader.ReadInt32();
                    gameTimeAlive = serializer.reader.ReadInt32();
                    gameTimeDead = serializer.reader.ReadInt32();
                    karma = serializer.reader.ReadByte();
                    rippleLevel = serializer.reader.ReadByte() * 0.25f;
                    food = serializer.reader.ReadByte();
                    shelterName = serializer.reader.ReadString();
                }
            }

            public static bool operator !=(MenuSaveStateState? left, MenuSaveStateState? right) => !(left == right);

            public static bool operator ==(MenuSaveStateState? left, MenuSaveStateState? right)
            {
                if (left is null) return right is null;
                if (right is null) return false;
                return left.Compare(right);
            }
            public bool Compare(MenuSaveStateState other)
            {
                return (karma == other.karma) &&
                    (rippleLevel == other.rippleLevel) &&
                    (food == other.food) &&
                    (cycle == other.cycle) &&
                    (karmaReinforced == other.karmaReinforced) &&
                    (hasGlow == other.hasGlow) &&
                    (hasMark == other.hasMark) &&
                    (ascended == other.ascended) &&
                    (altEnd == other.altEnd) &&
                    (shelterName == other.shelterName) &&
                    (gameTimeAlive == other.gameTimeAlive) &&
                    (gameTimeDead == other.gameTimeDead);
            }
        }
    }
}
