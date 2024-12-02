using Music;
using RWCustom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using System.Text.RegularExpressions;
using UnityEngine;
using Mono.Cecil;
using IL.MoreSlugcats;
using System.Drawing.Printing;
using System.Threading.Tasks;
using AssetBundles;

namespace RainMeadow
{
    public partial class MeadowMusic
    {
        //Todo:
        // Song Priority.
        // Refactor and remove vanillas variables of songs (stopongate, startonlywhen X, remove that stuff) ,
        // Inspect how vanilla treats song priority, for then to have
        //   - songs given by yourself to yourself be given a normal priority, even when you're a host
        //   - songs given to yourself by host to be given MAX priority
        //
        //- Make RPC for the host to interupt everyone else with a song  ,  !!!!!! still gotta !!!! do this !!!!!
        //
        //- Make it so that when you enter lobby/quit to menu/quit game , you leave the group simultaniously 
        //
        //note to self, you can set a shit in  volumegroups     check virtualmicrophone
        //there's 5 of them,
        
        public static void EnableMusic()
        {
            CheckFiles();

            On.VirtualMicrophone.NewRoom += NewRoomPatch;

            On.Music.MusicPiece.SubTrack.StopAndDestroy += SubTrack_StopAndDestroy;
            
            On.Music.PlayerThreatTracker.Update += PlayerThreatTracker_Update; // yo which hook is this one go to? oooOOOoooooo

            On.Music.MusicPlayer.UpdateMusicContext += MusicPlayer_UpdateMusicContext;
            On.Music.MusicPiece.StopAndDestroy += MusicPiece_StopAndDestroy;


            On.Music.MusicPlayer.GameRequestsSong += MusicPlayer_GameRequestsSong;
            On.ActiveTriggerChecker.FireEvent += ActiveTriggerChecker_FireEvent;
            On.SSOracleBehavior.TurnOffSSMusic += SSOracleBehavior_TurnOffSSMusic;
            On.Music.SSSong.Update += SSSong_Update;
            On.Music.MusicPlayer.RainRequestStopSong += MusicPlayer_RainRequestStopSong;

            On.VirtualMicrophone.SoundObject.SetLowPassCutOff += SoundObject_SetLowPassCutOff;
        
        }

        private static void SoundObject_SetLowPassCutOff(On.VirtualMicrophone.SoundObject.orig_SetLowPassCutOff orig, VirtualMicrophone.SoundObject self, float effect)
        {
            if (effect == 0f && self.gameObject.GetComponent<AudioLowPassFilter>() != null)
            {
                UnityEngine.Object.Destroy(self.gameObject.GetComponent<AudioLowPassFilter>());
            }
            else if (effect > 0f && self.gameObject.GetComponent<AudioLowPassFilter>() == null)
            {
                self.gameObject.AddComponent<AudioLowPassFilter>();
            }
            if (effect > 0f)
            {
                self.gameObject.GetComponent<AudioLowPassFilter>().cutoffFrequency = Mathf.Lerp(22000f, 1500f, Mathf.Pow(effect, 0.5f)); // THIS PART is what causes the cuts, cuz it's not caring about being lerped
            }
        }


        //Game music requests 
        private static void MusicPlayer_RainRequestStopSong(On.Music.MusicPlayer.orig_RainRequestStopSong orig, MusicPlayer self) { if (OnlineManager.lobby == null || OnlineManager.lobby.gameMode is not MeadowGameMode) orig.Invoke(self); } //hehe Just in Case lmao

        private static void SSSong_Update(On.Music.SSSong.orig_Update orig, SSSong self)
        {
            if (self.setVolume == null && self.destroyCounter > 150)
            {
                if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MeadowGameMode mgm)
                {
                    //RPC to destroy everyone
                }
            }
            orig(self);
        }

        private static void SSOracleBehavior_TurnOffSSMusic(On.SSOracleBehavior.orig_TurnOffSSMusic orig, SSOracleBehavior self, bool abruptEnd)
        {
            orig.Invoke(self, abruptEnd);

            if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MeadowGameMode mgm)
            {
                if (abruptEnd
                    && self.oracle.room.game.manager.musicPlayer != null 
                    && self.oracle.room.game.manager.musicPlayer.song != null 
                    && self.oracle.room.game.manager.musicPlayer.song is SSSong
                    )
                {
                    //self.oracle.room.game.manager.musicPlayer.song.FadeOut(2f);
                    //do the rpc to mute everyone else here yeah yeah?
                }
            } // so yeah note to self that this is what you gotta do like,, yeah account for this bitch

        }

        private static void ActiveTriggerChecker_FireEvent(On.ActiveTriggerChecker.orig_FireEvent orig, ActiveTriggerChecker self)
        {
            if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MeadowGameMode mgm)
            {
                if (self.eventTrigger.tEvent == null)
                {
                    return;
                }
                if (self.eventTrigger.tEvent.type == TriggeredEvent.EventType.MusicEvent)
                {
                    //if (self.room.game.manager.musicPlayer != null 
                        // && (self.room.game.world.rainCycle.MusicAllowed || self.room.roomSettings.DangerType == RoomRain.DangerType.None) 
                        //&& (!self.room.game.IsStorySession || !self.room.game.GetStorySession.RedIsOutOfCycles) //oh wow that's cute, no more music, time to die cancer boy
                    //    ) 
                    //{
                    //
                    //}
                    //no need to check RainRequestStopSong lmao  no rain dude
                    self.room.game.manager.musicPlayer?.GameRequestsSong(self.eventTrigger.tEvent as MusicEvent);
                }
                else if (self.eventTrigger.tEvent.type == TriggeredEvent.EventType.StopMusicEvent) self.room.game.manager.musicPlayer?.GameRequestsSongStop(self.eventTrigger.tEvent as StopMusicEvent); //for when it wants to uhhh, loop thing? idek
            }
            else
            {
                orig.Invoke(self);
            }
        }

        private static void MusicPlayer_GameRequestsSong(On.Music.MusicPlayer.orig_GameRequestsSong orig, MusicPlayer self, MusicEvent musicEvent)
        {
            if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MeadowGameMode mgm)
            {
                var smg = OnlineManager.lobby.GetData<LobbyMusicData>();
                var inGroup = smg.playerGroups[OnlineManager.mePlayer.inLobbyId];
                ushort hostId = inGroup == 0 ? (ushort)0U : smg.groupHosts[inGroup];

                if (hostId == OnlineManager.mePlayer.inLobbyId)
                {
                    //hmmmmm, (!this.manager.rainWorld.setup.playMusic) is a thing? concerning...

                    //PlaySong(musicEvent.songName) Since it's a 
                    
                    Song song = new(self, musicEvent.songName, MusicPlayer.MusicContext.StoryMode);
                    song.fadeOutAtThreat = 6969f; //wont be important cuz we *set* the threatvariable anyways haha,,,,,
                    //song.Loop = false;
                    song.Loop = musicEvent.loop;
                    //song.roomTransitions haha no need to set this  cuz PlayerToNewRoom isn't called when in meadowmode
                    //actually, might be a bit important, cuz pebbles loop, we should want to find out what to do then,
                    //presumably latch onto musicplayerupdate to if my (the host's) song is empty send: an empty ReplaceSong RPC, just to end the loop for everyone.
                    song.priority = 420f;
                    song.stopAtDeath = false;
                    song.stopAtGate = false;


                    if (self.song == null)
                    {
                        self.song = song;
                        self.song.playWhenReady = true;
                    }
                    else
                    {
                    if (self.nextSong != null && (self.nextSong.priority >= musicEvent.prio || self.nextSong.name == musicEvent.songName))
                        {
                            return;
                        }
                        self.nextSong = song;
                        self.nextSong.playWhenReady = false;
                    }

                    //do a method here that sends an RPC to everyone that they need to change their song to THIS SONG dude. (and mayb loop it too :))
                }
                else
                {
                    return;
                }
            }
            else
            {
                orig.Invoke(self, musicEvent);
            }
        }





        //musiccode
        private static void MusicPlayer_UpdateMusicContext(On.Music.MusicPlayer.orig_UpdateMusicContext orig, MusicPlayer self, MainLoopProcess currentProcess)
        {
            if (self.musicContext != null)
            {
                if (currentProcess.ID == ProcessManager.ProcessID.Game)
                {
                    if (((RainWorldGame)currentProcess).IsStorySession && OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MeadowGameMode mgm)
                    {
                        if (self.song != null)
                        {
                            if (self.gameObj.GetComponent<AudioHighPassFilter>() == null)
                            {
                                self.gameObj.AddComponent<AudioHighPassFilter>();
                                self.gameObj.GetComponent<AudioHighPassFilter>().enabled = true;
                                self.gameObj.GetComponent<AudioHighPassFilter>().cutoffFrequency = 10f;
                            }
                        }//else, we shoudl save adding these for when we actually do have a song
                    }
                    else
                    {
                        if (self.song != null && self.gameObj.GetComponent<AudioHighPassFilter>() != null) self.song.FadeOut(120f);
                    }
                }
                else
                {
                    if (self.song != null && self.gameObj.GetComponent<AudioHighPassFilter>() != null) self.song.FadeOut(120f);
                }
            }
            else
            {
                if (self.song != null && self.gameObj.GetComponent<AudioHighPassFilter>() != null) self.song.FadeOut(120f);
                
            }
            orig.Invoke(self, currentProcess);
        }

        private static void PlayerThreatTracker_Update(On.Music.PlayerThreatTracker.orig_Update orig, PlayerThreatTracker self)
        {
            // replace vanilla handling, ours is better
            if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MeadowGameMode mgm)
            {
                if (self.musicPlayer == null || self.musicPlayer.gameObj == null || self.musicPlayer.song == null) return; //don't really need to track it if you don't have a song.

                if (self.musicPlayer.manager.currentMainLoop == null || self.musicPlayer.manager.currentMainLoop.ID != ProcessManager.ProcessID.Game)
                {
                    self.recommendedDroneVolume = 0f;
                    self.currentThreat = 0f;
                    self.currentMusicAgnosticThreat = 0f;
                    self.region = null;
                    return;
                }
                self.currentThreat = 0f;
                self.currentMusicAgnosticThreat = 0f;
                Creature player = mgm.avatars[0].realizedCreature;
                if (player == null || player.room == null)
                {
                    return;
                }
                
                self.recommendedDroneVolume = player.room.roomSettings.BkgDroneVolume;
                if (!player.room.world.rainCycle.MusicAllowed && player.room.roomSettings.DangerType != RoomRain.DangerType.None)
                {
                    self.recommendedDroneVolume = 0f;
                }
                if ((self.musicPlayer.manager.currentMainLoop as RainWorldGame).cameras[0].ghostMode > (ModManager.MMF ? 0.1f : 0f))
                {
                    if (player.room.world.worldGhost != null)
                    {
                        self.ghostMode = player.room.world.worldGhost.GhostMode(player.room.abstractRoom, player.abstractCreature.world.RoomToWorldPos(player.mainBodyChunk.pos, player.room.abstractRoom.index));
                    }
                    else
                    {
                        self.ghostMode = 1f;
                    }
                }
                else
                {
                    self.ghostMode = 0f;
                }

                if (self.musicPlayer.gameObj.GetComponent<AudioHighPassFilter>() == null)
                {
                    self.musicPlayer.gameObj.AddComponent<AudioHighPassFilter>();
                    self.musicPlayer.gameObj.GetComponent<AudioHighPassFilter>().enabled = true;
                    self.musicPlayer.gameObj.GetComponent<AudioHighPassFilter>().cutoffFrequency = 10f;
                }//like here, yeah?

                if (self.ghostMode == 0f && self.musicPlayer.gameObj.GetComponent<AudioHighPassFilter>().enabled)
                {
                    if (self.musicPlayer.gameObj.GetComponent<AudioHighPassFilter>().cutoffFrequency < 12f)
                    {
                        self.musicPlayer.gameObj.GetComponent<AudioHighPassFilter>().enabled = false;
                    }
                }
                else if (self.ghostMode > 0f && !self.musicPlayer.gameObj.GetComponent<AudioHighPassFilter>().enabled)
                {
                    self.musicPlayer.gameObj.GetComponent<AudioHighPassFilter>().enabled = true;
                }
                if (self.ghostMode > 0f || self.musicPlayer.gameObj.GetComponent<AudioHighPassFilter>().cutoffFrequency > 12f)
                {
                    self.recommendedDroneVolume = 0f;
                    //self.musicPlayer.FadeOutAllNonGhostSongs(120f);
                    //if (player.room.world.worldGhost != null && (self.musicPlayer.song == null || !(self.musicPlayer.song is GhostSong)))
                    //{
                    //    self.musicPlayer.RequestGhostSong(player.room.world.worldGhost.songName);
                    //}
                    float currenthighpass = self.musicPlayer.gameObj.GetComponent<AudioHighPassFilter>().cutoffFrequency;
                    float highpassgoal = Mathf.Lerp(0f, 1200f, Mathf.Pow(self.ghostMode, 2.5f));    
                    self.musicPlayer.gameObj.GetComponent<AudioHighPassFilter>().cutoffFrequency = Custom.LerpAndTick(currenthighpass, highpassgoal, currenthighpass > highpassgoal ? 0.025f : 0.005f, 0.0f);
                }
                
                //if (!player.room.world.singleRoomWorld)
                //{
                //    if (player.room.abstractRoom.index != self.room)
                //    {
                //        self.lastLastRoom = self.lastRoom;
                //        self.lastRoom = self.room;
                //        self.room = player.room.abstractRoom.index;
                //        if (self.room != self.lastLastRoom)
                //        {
                //            self.roomSwitches++;
                //            if (player.room.world.region.name != self.region)
                //            {
                //                self.region = player.room.world.region.name;
                //                self.musicPlayer.NewRegion(self.region);
                //            }
                //        }
                //    }
                //    if (self.roomSwitches > 0 && self.roomSwitchDelay > 0)
                //    {
                //        self.roomSwitchDelay--;
                //        if (self.roomSwitchDelay < 1)
                //        {
                //            if (self.musicPlayer.song != null)
                //            {
                //                self.musicPlayer.song.PlayerToNewRoom();
                //            }
                //                if (self.musicPlayer.nextSong != null)
                //            {
                //                self.musicPlayer.nextSong.PlayerToNewRoom();
                //            }
                //            self.roomSwitchDelay = UnityEngine.Random.Range(80, 400);
                //            self.roomSwitches--;
                //        }
                //    }
                //}
                //else if ((self.musicPlayer.manager.currentMainLoop as RainWorldGame).IsArenaSession && (self.musicPlayer.manager.currentMainLoop as RainWorldGame).GetArenaGameSession.arenaSitting.gameTypeSetup.gameType == MoreSlugcatsEnums.GameTypeID.Challenge && (self.musicPlayer.manager.currentMainLoop as RainWorldGame).GetArenaGameSession.chMeta != null && !string.IsNullOrEmpty((self.musicPlayer.manager.currentMainLoop as RainWorldGame).GetArenaGameSession.chMeta.threatMusic))
                //{
                //    string threatMusic = (self.musicPlayer.manager.currentMainLoop as RainWorldGame).GetArenaGameSession.chMeta.threatMusic;
                //    if (self.region != threatMusic)
                //    {
                //        self.region = threatMusic;
                //        self.musicPlayer.NewRegion(self.region);
                //    }
                //}
                //
                //self.threatDetermine.Update(self.musicPlayer.manager.currentMainLoop as RainWorldGame);
                //if (self.musicPlayer.song != null)
                //{
                //    self.threatDetermine.currentThreat = 0f;
                //}
                //self.currentThreat = self.threatDetermine.currentThreat;
                //self.currentMusicAgnosticThreat = self.threatDetermine.currentMusicAgnosticThreat;

                self.threatDetermine.currentThreat = 0f;
                //on the slight moment after fading out a song, the default threat theme plays
                //so just take away it calculating th threat theme lmao
            }
            else
            {
                orig(self);
            }
        }
        private static void MusicPiece_StopAndDestroy(On.Music.MusicPiece.orig_StopAndDestroy orig, MusicPiece self)
        {
            //RainMeadow.Debug("DESTROYED SONG");
            orig.Invoke(self);

            if (OnlineManager.lobby == null || OnlineManager.lobby.gameMode is not MeadowGameMode mgm)
            {
                if (self.musicPlayer.gameObj.GetComponent<AudioHighPassFilter>() != null)
                {
                    self.musicPlayer.gameObj.GetComponent<AudioHighPassFilter>().cutoffFrequency = 10f;
                    self.musicPlayer.gameObj.GetComponent<AudioHighPassFilter>().enabled = false;
                    UnityEngine.Object.Destroy(self.musicPlayer.gameObj.GetComponent<AudioHighPassFilter>());
                }
            }
        }
        private static void SubTrack_StopAndDestroy(On.Music.MusicPiece.SubTrack.orig_StopAndDestroy orig, MusicPiece.SubTrack self)
        {
            orig(self);
            self.source?.clip?.UnloadAudioData();
        } 

        internal static void NewGame()
        {
            time = 0f;
            timerStopped = true;

            // there's proooobably more stuff that needs resetting here
        }

        const int waitSecs = 5;

        static bool filesChecked = false;

        static readonly Dictionary<string, string[]> ambientDict = new();
        internal static readonly Dictionary<string, VibeZone[]> vibeZonesDict = new();

        internal static Dictionary<int, VibeZone> activeZonesDict = null;
        static string[] ambienceSongArray = null;
        static bool loadingsong = false;

        static int[] shufflequeue = new int[0];
        static int shuffleindex = 0;

        static float time = 0f;
        static bool timerStopped = true;

        static VibeZone activeZone = new VibeZone();
        public static bool AllowPlopping;

        public static float? vibeIntensity = null;
        public static float? vibePan = null;
        static bool UpdateIntensity;
        static bool IDontWantToGoToZero;

        static bool ivebeenpatientlywaiting = false;

        static float? demiseTimer;
        static float? groupdemiseTimer;
        static float? joinTimer;

        internal struct VibeZone
        {
            public VibeZone(string room, float radius, float minradius, string sampleUsed)
            {
                this.room = room;
                this.radius = radius;
                this.minradius = minradius;
                this.sampleUsed = sampleUsed;
            }

            public string room;
            public float radius;
            public float minradius;
            public string sampleUsed;
        }
        private static void CheckFiles()
        {
            if (!filesChecked)
            {
                string[] dirs = AssetManager.ListDirectory("world", true, true);
                foreach (string dir in dirs)
                {
                    string regName = new DirectoryInfo(dir).Name.ToUpper();
                    string path = dir + Path.DirectorySeparatorChar + "playlist.txt";
                    if (regName.Length == 2 && File.Exists(path) && !ambientDict.ContainsKey(regName))
                    {
                        string[] lines = File.ReadAllLines(path).Where(l => l != string.Empty).ToArray();
                        foreach (string line in lines)
                        {
                            RainMeadow.Debug("Meadow Music: Registered song " + line + " in " + regName);
                        }
                        ambientDict.Add(regName, lines);
                    }
                    path = dir + Path.DirectorySeparatorChar + "vibe_zones.txt";
                    if (File.Exists(path) && !vibeZonesDict.ContainsKey(regName))
                    {
                        string[] lines = File.ReadAllLines(path);
                        VibeZone[] zones = new VibeZone[lines.Length];
                        for (int i = 0; i < lines.Length; i++)
                        {
                            string[] arr = lines[i].Split(',');
                            zones[i] = new VibeZone(arr[0], float.Parse(arr[1]), float.Parse(arr[2]), arr[3]);
                        }
                        vibeZonesDict.Add(regName, zones);
                    }
                }

                filesChecked = true;
            }
        }

        //we don't check if players just leave, and don't rejoin. which is something they do, due to me not really caring about them when they're so far away, so they don't join for me.
        //also, newroompatch will no longer call to updateplayers, since it'd already get called by my request to JoinThisResource.
        internal static void RawUpdate(RainWorldGame self, float dt)
        {
            if (!timerStopped) time += dt;
            MusicPlayer musicPlayer = self.manager.musicPlayer;
            var RoomImIn = self.cameras[0].room;
            var MyGuyMic = self.cameras[0].virtualMicrophone;

            var mgm = OnlineManager.lobby.gameMode as MeadowGameMode;
            var creature = mgm.avatars[0];
            var musicdata = creature.GetData<MeadowMusicData>();

            if (demiseTimer != null)
            {
                demiseTimer -= dt;
                if (demiseTimer < 0)
                {
                    //LeaveGroup
                    RainMeadow.Debug("I will be asking to leave");
                    self.cameras[0].virtualMicrophone.PlaySound(SoundID.Snail_Pop, creature.owner.inLobbyId == 1 ? -0.8f : 0.8f, 1, 1);
                    OnlineManager.lobby.owner.InvokeRPC(AskNowLeave);
                    demiseTimer = null;
                    ivebeenpatientlywaiting = false;
                }
            }
            if (groupdemiseTimer != null)
            {
                groupdemiseTimer -= dt;
                if (groupdemiseTimer < 0)
                {
                    //Squash the room together 
                    //generate an array of all the players in your current room and feed that tooooooooooo
                    //MeadowPlayerId[] ballers = new MeadowPlayerId[4]; //temp

                    self.cameras[0].virtualMicrophone.PlaySound(SoundID.Leviathan_Bite, creature.owner.inLobbyId == 1 ? -0.8f : 0.8f, 1, 1);
                    List<OnlineCreature> InThisRoom = new List<OnlineCreature>();
                    foreach (var entity in creature.roomSession.activeEntities.Where(v => v is OnlineCreature))
                    {
                        if(entity.TryGetData<MeadowMusicData>(out _))
                            InThisRoom.Add((OnlineCreature)entity);
                    }
                    ushort[] ballers = InThisRoom.Select(v => v.owner.inLobbyId).ToArray();
                    OnlineManager.lobby.owner.InvokeRPC(AskNowSquashPlayers, ballers);
                    groupdemiseTimer = null;
                }
            }
            if (joinTimer != null)
            {
                joinTimer -= dt;
                if (joinTimer < 0)
                {
                    //If there's other IDs here, join the predominant one if one exists
                    //else, join a random other player
                    List<OnlinePlayer> playersWithMe = new List<OnlinePlayer>();

                    self.cameras[0].virtualMicrophone.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, creature.owner.inLobbyId == 1 ? -0.8f : 0.8f, 1, 1);

                    foreach (var entity in creature.roomSession.activeEntities.Where(v => v is OnlineCreature))
                    {
                        if (entity.TryGetData<MeadowMusicData>(out _) && !entity.isMine)
                            playersWithMe.Add(entity.owner);
                    }

                    bool theresaguywithanID = false;
                    var mgms = OnlineManager.lobby.GetData<LobbyMusicData>();
                    List<byte> IDs = playersWithMe.Select(p => mgms.playerGroups[p.inLobbyId]).ToList();
                    theresaguywithanID = IDs.Count(v => v != 0) > 0;
                    if (theresaguywithanID)
                    {
                        var g = IDs.GroupBy(v => v);
                        var result = g.OrderByDescending(v => v).ToList();
                        var the = result[0].Key;
                        RainMeadow.Debug("I will ask to join this ID " + the);
                        var who = playersWithMe.First(p => mgms.playerGroups[p.inLobbyId] == the);
                        OnlineManager.lobby.owner.InvokeRPC(AskNowJoinPlayer, who);
                    }
                    else 
                    {
                        //choose a random guy you're currently with
                        var who = playersWithMe[UnityEngine.Random.Range(0, playersWithMe.Count)];
                        RainMeadow.Debug("I will ask to join this player named " + who);
                        OnlineManager.lobby.owner.InvokeRPC(AskNowJoinPlayer, who); // the ordering
                    }
                    joinTimer = null;
                }
            }
            if (UpdateIntensity && RoomImIn != null && MyGuyMic != null && activeZonesDict != null && closestVibe != -1)
            {
                vibePan = Vector2.Dot((RoomImIn.world.RoomToWorldPos(Vector2.zero, closestVibe) - RoomImIn.world.RoomToWorldPos(Vector2.zero, RoomImIn.abstractRoom.index)).normalized, Vector2.right);
                //RainMeadow.Debug("Has Calculated Pan");
                Vector2 VibeRoomCenterPos = self.world.RoomToWorldPos(self.world.GetAbstractRoom(closestVibe).size.ToVector2() * 10f, closestVibe);
                Vector2 PlayerPos = self.world.RoomToWorldPos(MyGuyMic.listenerPoint, RoomImIn.abstractRoom.index);
                //RainMeadow.Debug("Has made vectors to viberoom and player");
                float vibeIntensityTarget =
                             Mathf.Pow(Mathf.InverseLerp(activeZone.radius, activeZone.minradius, Vector2.Distance(PlayerPos, VibeRoomCenterPos)), 1.425f)
                           * Mathf.Clamp01(1f - (float)((float)DegreesOfAwayness * 0.3f))
                           * ((RoomImIn.abstractRoom.layer == self.world.GetAbstractRoom(closestVibe).layer) ? 1f : 0.75f) //activeZone.room also works   <--- DOES NOT ??? <--- YES IT DOES??? we got overloads on that bitch
                           * (IDontWantToGoToZero ? 1f : 0f);
                //RainMeadow.Debug("Has Figured out TargetIntensity");
                //reminder set vibeintensity to null on occasions where you go to menu or some shit
                vibeIntensityTarget = Custom.LerpAndTick(vibeIntensity == null ? (IDontWantToGoToZero?vibeIntensityTarget:0f) : vibeIntensity.Value, vibeIntensityTarget, 0, dt * 0.2f * (IDontWantToGoToZero?1f:3f)); //lol   vibeIntensityTarget = Custom.LerpAndTick(vibeIntensity == null ? 0 : vibeIntensity.Value, vibeIntensityTarget, 0.005f, 0.002f); // 0.025, 0.002 Actually we probably shouldn't calculate this here, in *raw update*, yknow?
                vibeIntensity = vibeIntensityTarget;
                AllowPlopping = vibeIntensity.Value >= 0.05f;
                if (musicPlayer != null && musicPlayer.song != null)
                {
                    if ((float)vibeIntensity > 0.9f) 
                    { 
                        musicPlayer.song.baseVolume = 0f; 
                    }
                    else 
                    { 
                        musicPlayer.song.baseVolume = Mathf.Pow(1f - (float)vibeIntensity, 2.5f) * 0.3f; 
                    }
                }
                if (vibeIntensity < 0.001f && vibeIntensityTarget == 0f)
                {
                    RainMeadow.Debug("vibe intensity locked to the zero... :(");
                    UpdateIntensity = false;
                    vibeIntensity = 0f;
                    if (musicPlayer != null && musicPlayer.song != null) musicPlayer.song.baseVolume = 0.3f;
                }
                else if (vibeIntensity > 0.999f && vibeIntensityTarget == 1f)
                {
                    RainMeadow.Debug("VIBE INTENSITY LOCKED TO THE MEGA!!! :D");
                    UpdateIntensity = false;
                    vibeIntensity = 1f;
                    if (musicPlayer != null && musicPlayer.song != null) musicPlayer.song.baseVolume = 0f;
                }
                //RainMeadow.Debug("Has assigned vibeintensity, plopping, and maybe musicvolume.");
            }

            var lmd = OnlineManager.lobby.GetData<LobbyMusicData>();
            var inGroup = lmd.playerGroups[OnlineManager.mePlayer.inLobbyId];
            ushort hostId = inGroup == 0 ? (ushort)0U : lmd.groupHosts[inGroup];

            //RainMeadow.Debug("ingroup: " + inGroup);
            //RainMeadow.Debug("hostid: " + hostId);

            if (inGroup != 0 && inGroup != inGroupbuffer)
            {
                RainMeadow.Debug("new group!");
                RainMeadow.Debug("ingroup: " + inGroup);
                RainMeadow.Debug("hostid: " + hostId);
                if (hostId == OnlineManager.mePlayer.inLobbyId)
                {
                    // huh
                    RainMeadow.Debug("I'm the host");
                }
                else if (OnlineManager.lobby.PlayerFromId(hostId) is OnlinePlayer other 
                    && OnlineManager.lobby.playerAvatars.FirstOrDefault(kvp => kvp.Key == other).Value is OnlineEntity.EntityId otherOcId
                    && otherOcId.FindEntity() is OnlineCreature oc)
                {
                    // found
                    var myDJsdata = oc.GetData<MeadowMusicData>();
                    RainMeadow.Debug($"So do our songs match? {musicdata.providedSong} == {myDJsdata.providedSong}? And how far apart are we then? {musicdata.startedPlayingAt}, {myDJsdata.startedPlayingAt}");
                    if (musicdata.providedSong != myDJsdata.providedSong || Math.Max(musicdata.startedPlayingAt, myDJsdata.startedPlayingAt) - Math.Min(musicdata.startedPlayingAt, myDJsdata.startedPlayingAt) > 5)
                    {
                        RainMeadow.Debug($"Yeah let's swtich gears dude");
                        if (musicPlayer != null && musicPlayer.song != null)
                        {
                            musicPlayer.song.FadeOut(40f);
                            //skiptopoint = !ivebeenpatientlywaiting;
                        }
                    }
                }
                else
                {
                    RainMeadow.Debug($"host avatar for {hostId} for group {inGroup} not found");
                }
            }
            inGroupbuffer = inGroup;
            bool ImFollowingMyOrder = true;
            if (musicPlayer != null && musicPlayer.song == null && self.world.rainCycle.RainApproaching > 0.5f && !loadingsong)
            {
                if (ivebeenpatientlywaiting)
                {
                    //intilatch for when dj doesn't provide a song or is ending theirs
                    if (OnlineManager.lobby.PlayerFromId(hostId) is OnlinePlayer other
                    && OnlineManager.lobby.playerAvatars.FirstOrDefault(kvp => kvp.Key == other).Value is OnlineEntity.EntityId otherOcId
                    && otherOcId.FindEntity() is OnlineCreature oc)
                    {
                        MeadowMusicData myDJsdata = oc.GetData<MeadowMusicData>();

                        if (myDJsdata.providedSong == "" || myDJsdata.providedSong == null || myDJsdata.providedSong == songtoavoid)
                        {
                            ImFollowingMyOrder = false;
                        }
                        else
                        {
                            songtoavoid = "";
                        }
                    }
                }
                //Hmmm, we could make it so that if if the group host says it starts at a time *in the future*, then everyone will latch to play it at that time.
                musicdata.providedSong = null;
                timerStopped = false;
                if (time > waitSecs && ImFollowingMyOrder)
                {
                    RainMeadow.Debug("Tryna find a song to play");
                    if (ambienceSongArray != null)
                    {
                        if (inGroup == 0 || hostId == OnlineManager.mePlayer.inLobbyId)
                        {

                            _ = PlaySong(musicPlayer);
                            RainMeadow.Debug("Meadow Music: Playing ambient song: " + musicdata.providedSong);

                        }
                        else if (OnlineManager.lobby.PlayerFromId(hostId) is OnlinePlayer other
                            && OnlineManager.lobby.playerAvatars.FirstOrDefault(kvp => kvp.Key == other).Value is OnlineEntity.EntityId otherOcId
                            && otherOcId.FindEntity() is OnlineCreature oc)
                        {
                            // found
                            RainMeadow.Debug("Trying to get host");
                            MeadowMusicData myDJsdata = oc.GetData<MeadowMusicData>();

                            RainMeadow.Debug("My DJ *blinks eyes*: " + myDJsdata.providedSong + " " + myDJsdata.startedPlayingAt);

                            if (myDJsdata != null)
                            {
                                if (myDJsdata.providedSong != null)
                                {
                                    RainMeadow.Debug("My host has a song, gonna try playing it");
                                    _ = PlaySong(musicPlayer, myDJsdata);
                                }
                                else
                                {
                                    RainMeadow.Debug("Meadow Music: DJ isn't providing a song, i'll wait from the start");
                                    ivebeenpatientlywaiting = true;
                                }
                            }
                        }
                        else
                        {
                            RainMeadow.Debug($"host {hostId} for group {inGroup} not found");
                        }
                    }
                }
                else
                {

                }
            }
            else
            {
                time = 0f;
                timerStopped = true;
            }
        }

        private static async Task PlaySong(MusicPlayer musicPlayer, MeadowMusicData? portableDJ = null)
        {
            string songtobesang;
            float? timetobestarted = null;
            if (portableDJ == null)
            {
                if (shuffleindex + 1 >= shufflequeue.Length) { ShuffleSongs(); }
                else { shuffleindex++; }
                songtobesang = ambienceSongArray[shufflequeue[shuffleindex]];
            }
            else
            {
                songtobesang = portableDJ.providedSong;
                timetobestarted = portableDJ.startedPlayingAt;
            }

            Song? song = await Task.Run(() => LoadSong(musicPlayer, songtobesang, timetobestarted));

            if (song != null)
            {
                var mgm = OnlineManager.lobby.gameMode as MeadowGameMode;
                var creature = mgm.avatars[0];
                var musicdata = creature.GetData<MeadowMusicData>();

                if (portableDJ != null)
                {
                    song.priority = 255_207_64f; // :)
                    song.stopAtDeath = false; //well, just to be sure!
                    song.stopAtGate = false;
                }
                musicPlayer.song = song;
                if (!UpdateIntensity) musicPlayer.song.baseVolume = ((vibeIntensity == 0f)?0.3f:0f);
                musicdata.providedSong = songtobesang;
                musicdata.startedPlayingAt = LobbyTime();
                RainMeadow.Debug("my song is now " + musicdata.providedSong);

                if (!ivebeenpatientlywaiting && timetobestarted != null)
                {
                    musicdata.startedPlayingAt = timetobestarted.Value;
                    float calculatedthing = LobbyTime() - timetobestarted.Value;
                    musicPlayer.song.subTracks[0].source.time = calculatedthing;
                    RainMeadow.Debug("Playing from a point " + LobbyTime() + " " + timetobestarted.Value + " which amounts to " + calculatedthing);
                    RainMeadow.Debug("and thus it's now at " + musicPlayer.song.subTracks[0].source.time + "... is this no longer 0? Yay!!!");
                    if (musicPlayer.song.subTracks[0].source.time == 0f) RainMeadow.Error("Oh wait it is zero, fuck");
                    ivebeenpatientlywaiting = true; //for the next track, now that we're synced up
                }
            }
        }
        static string songtoavoid = "";
        private static Song? LoadSong(MusicPlayer musicPlayer, string providedsong, float? DJstartedat)
        {
            loadingsong = true;
            //just putting more and more technical debt onto songnames
            AudioClip? clipclip = null;
            string text1 = string.Concat(new string[] { "Music", Path.DirectorySeparatorChar.ToString(), "Songs", Path.DirectorySeparatorChar.ToString(), providedsong, ".ogg" });
            string text2 = AssetManager.ResolveFilePath(text1);
            if (text2 != Path.Combine(Custom.RootFolderDirectory(), text1.ToLowerInvariant()) && File.Exists(text2))
            {
                RainMeadow.Debug("It can load the song safetly ");
                clipclip = AssetManager.SafeWWWAudioClip("file://" + text2, false, true, AudioType.OGGVORBIS);

            }
            else
            {
                string text6;
                LoadedAssetBundle loadedAssetBundle2 = AssetBundleManager.GetLoadedAssetBundle("music_songs", out text6);
                if (loadedAssetBundle2 != null)
                {
                    RainMeadow.Debug("Loads the song unsafetly?");
                    clipclip = loadedAssetBundle2.m_AssetBundle.LoadAsset<AudioClip>(providedsong);
                }
            }

            if (clipclip == null)
            {
                RainMeadow.Debug($"Could not fetch the clip to the requested song {providedsong}");
                loadingsong = false;
                return null;
            }

            bool willfadein = false;
            if (DJstartedat != null) //Here the todo
            {
                float hostsonglength = clipclip.length;
                if (hostsonglength != 0)
                {
                    float hostsongprogress = (LobbyTime() - DJstartedat.Value) / hostsonglength;
                    if (hostsongprogress < 0.95f)
                    {
                        //RainMeadow.Debug("Meadow Music: Playing my DJs provided song: " + myDJsdata.providedSong + (ivebeenpatientlywaiting ? " supposedly from the beginning, after patiently waiting" : " from a specific point, since i haven't waited"));
                        //_ = PlaySong(musicPlayer, myDJsdata.providedSong, ivebeenpatientlywaiting);
                    }
                    else
                    {
                        RainMeadow.Debug("Meadow Music: I Have the song, and i've figured since my DJ is soon done, i'll wait to play the next song from the start");
                        ivebeenpatientlywaiting = true;
                        songtoavoid = providedsong;
                        loadingsong = false;
                        return null;
                    }
                }
                else
                {
                    //RainMeadow.Debug("Meadow Music: I don't have my DJs provided song [even after all those checks :< ]. Playing a random song");
                    //if (shuffleindex + 1 >= shufflequeue.Length) { ShuffleSongs(); }
                    //else { shuffleindex++; }
                    //string provide = ambienceSongArray[shufflequeue[shuffleindex]];
                    //_ = PlaySong(musicPlayer, provide);
                    //RainMeadow.Debug("Playing ambient song: " + musicdata.providedSong);
                }

                if (!ivebeenpatientlywaiting)
                {
                    RainMeadow.Debug("Fading it in slowly");
                    willfadein = true;
                }
            }

            Song song = new(musicPlayer, providedsong, MusicPlayer.MusicContext.StoryMode)
            {
                playWhenReady = true,
                volume = 0
            };

            if (willfadein) song.fadeInTime = 120f;
            else song.fadeInTime = 2f; 
            MusicPiece.SubTrack sub = song.subTracks[0];
            sub.isStreamed = true;

            sub.source.clip = clipclip;
			sub.readyToPlay = true;
            loadingsong = false;
            return song;
        }

        public static void TheThingTHatsCalledWhenPlayersUpdated()
        {
            var mgm = OnlineManager.lobby.gameMode as MeadowGameMode;
            var creature = mgm.avatars[0];
            var musicdata = creature.GetData<MeadowMusicData>();
            var RoomImIn = creature.creature.Room.realizedRoom;
            if (RoomImIn == null || creature.roomSession == null) return;

            var mgrr2 = OnlineManager.lobby.GetData<LobbyMusicData>();
            var inGroup = mgrr2.playerGroups[OnlineManager.mePlayer.inLobbyId];
            var isDJ = inGroup != 0 && (mgrr2.groupHosts[inGroup] == OnlineManager.mePlayer.inLobbyId);

            RainMeadow.Debug("Checking Players");

            var VibeRoomCreatures = creature.abstractCreature.Room.world.GetAbstractRoom(closestVibe);
            if (VibeRoomCreatures != null)
            { 
                //PlopMachine.agora = VibeRoomCreatures.creatures.Count(); commented out for TESTING
            }

            if (inGroup == 0)
            {
                if (creature.roomSession.activeEntities.Any(
                    e => e is OnlineCreature && !e.isMine // someone elses
                    && e.TryGetData<MeadowMusicData>(out _))) // avatar
                {
                    RainMeadow.Debug("There are other people here!");
                    if (joinTimer == null) joinTimer = 5;
                }
                else
                {
                    joinTimer = null;
                }
            }
            else
            {
                RainMeadow.Debug("Checking onlinecreatures for belonging in the same room");

                List<byte> IDsWithMe = new List<byte>();
                foreach (var other in RoomImIn.abstractRoom.creatures)
                {
                    if (other.GetOnlineCreature() is OnlineCreature oc && mgrr2.playerGroups.TryGetValue(oc.owner.inLobbyId, out var group))
                    {
                        IDsWithMe.Add(group);
                    }
                }

                bool IAmWithMyFriends = IDsWithMe.Count(v => v == inGroup) > 1;
                //if (vibeRoom == null) return -1;
                if (!IAmWithMyFriends)
                {
                    RainMeadow.Debug("No dice, checks one degree of seperation for anyone, Room creature is in: " + creature.abstractCreature.Room.name);
                    List<byte> GangNextDoor = new List<byte>();

                    RainMeadow.Debug("And it thinks that my connections are " + Newtonsoft.Json.JsonConvert.SerializeObject(creature.abstractCreature.Room.connections));
                    foreach (int connection in creature.abstractCreature.Room.connections)
                    {
                        //var game = vibeRoom.connections[i];
                        RainMeadow.Debug("Pointing towards connection: " + connection);
                        if (connection != -1)
                        {
                            AbstractRoom abstractRoom = creature.abstractCreature.Room.world.GetAbstractRoom(connection); //ok so this says that there's no people because the people haven't joined the new resource yet so they just don't exist
                            if (abstractRoom != null) //worry more about how connection can be -1 than an abstractroom being null.  
                            {
                                RainMeadow.Debug("My neighbor " + abstractRoom.name); //this is having an error because it's saying one of the connections is -1
                                if (abstractRoom.creatures.Count() != 0)
                                {
                                    RainMeadow.Debug("Hey this room has creatures in it");
                                    foreach (var entity in abstractRoom.creatures)
                                    {
                                        RainMeadow.Debug(entity);

                                        if (entity.GetOnlineCreature() is OnlineCreature oc && mgrr2.playerGroups.TryGetValue(oc.owner.inLobbyId, out var group))
                                        {
                                            GangNextDoor.Add(group);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    IAmWithMyFriends = GangNextDoor.Count(v => v == inGroup) != 0;
                    if (!IAmWithMyFriends) RainMeadow.Debug("I don't believe anyone around me is my guys");
                }

                if (!IAmWithMyFriends)
                {
                    if (demiseTimer == null) { demiseTimer = 12.5f; RainMeadow.Debug("Started Demisetimer due to not being with friends"); }
                    groupdemiseTimer = null;
                }
                else
                {
                    //checks if the host is in the same region as you
                    bool djinsameregion = true;
                    if (!isDJ)
                    {
                        djinsameregion = false;
                        var theDJ = mgrr2.groupHosts[inGroup];
                        if(OnlineManager.lobby.PlayerFromId(theDJ) is OnlinePlayer other
                            && OnlineManager.lobby.playerAvatars.FirstOrDefault(kvp => kvp.Key == other).Value is OnlineEntity.EntityId ocid
                            && ocid.FindEntity(true) is OnlineCreature oc)
                        {
                            djinsameregion = oc.abstractCreature.world == creature.abstractCreature.world;
                        }
                    }

                    //if mydj is not in same region as me
                    if (!djinsameregion)
                    {
                        if (demiseTimer == null) { demiseTimer = 12.5f; RainMeadow.Debug("Started Demisetimer due to not being in the same region as DJ"); };
                    }
                    else
                    {
                        //check the amount 

                        var IDs = IDsWithMe.ToList();
                        IDs.RemoveAll(v => v == 0);
                        var g = IDs.GroupBy(v => v);
                        var result = g.OrderByDescending(v => v).ToList();
                        if (result.Count > 1)
                        {//dramaaa~
                            if (result[0].Count() == result[1].Count())
                            {
                                if (result[0].Key == inGroup || result[1].Key == inGroup)
                                {
                                    //groupdemistimer thingy
                                    groupdemiseTimer = (result[0].Count() + result[1].Count()) * 6f;
                                    demiseTimer = null;
                                }
                                else
                                {
                                    if (demiseTimer != null)
                                    {
                                        int i = 0;
                                        foreach (var other in OnlineManager.lobby.playerAvatars.Select(kvp => kvp.Value))
                                        {
                                            if (other.FindEntity() is OnlineCreature oc)
                                            {
                                                var otherinGroup = mgrr2.playerGroups[oc.owner.inLobbyId];
                                                if (otherinGroup == inGroup)
                                                {
                                                    i++;
                                                }
                                            }
                                        }
                                        demiseTimer = 6f * i;
                                    }
                                    groupdemiseTimer = null;
                                }
                            }
                            else
                            {
                                if (result[0].Key == inGroup)
                                {
                                    groupdemiseTimer = null;
                                    demiseTimer = null;
                                }
                                else
                                {
                                    if (demiseTimer != null) { demiseTimer = 12.5f; RainMeadow.Debug("Started Demisetimer due to the only group there being not mine"); }//*X being group
                                    groupdemiseTimer = null;
                                }
                            }
                        }
                        else
                        {
                            //well, should just be my group here then, aye?
                            demiseTimer = null;
                            groupdemiseTimer = null;
                        }
                    }
                }
            }
        }

        static float LobbyTime()
        {
            //do some shit that sends back the current time of the lobby host
            return OnlineManager.lobby.owner.tick / OnlineManager.instance.framesPerSecond;
        }

        internal static void NewWorld(World activeWorld)
        {
            AnalyzeRegion(activeWorld);
        }

        public static int closestVibe;
        //static Room? RoomImIn;
        static int DegreesOfAwayness;
        static int CalculateDegreesOfAwayness(AbstractRoom testRoom)
        {
            var vibeRoom = testRoom.world.GetAbstractRoom(closestVibe);
            if (vibeRoom == null) return 100;
            
            if (testRoom.index == vibeRoom.index)
            {
                return 0;
            }
            int num = 100;
            for (int i = 0; i < vibeRoom.connections.Length; i++)
            {
                if (vibeRoom.connections[i] == testRoom.index)
                {
                    return 1;
                }
                if (vibeRoom.connections[i] > -1)
                {
                    AbstractRoom abstractRoom = testRoom.world.GetAbstractRoom(vibeRoom.connections[i]);
                    for (int j = 0; j < abstractRoom.connections.Length; j++)
                    {
                        if (abstractRoom.connections[j] == testRoom.index)
                        {
                            num = Math.Min(num, 2);
                            break;
                        }
                        if (abstractRoom.connections[j] > -1)
                        {
                            AbstractRoom abstractRoom2 = testRoom.world.GetAbstractRoom(abstractRoom.connections[j]);
                            for (int k = 0; k < abstractRoom2.connections.Length; k++)
                            {
                                if (abstractRoom2.connections[k] == testRoom.index)
                                {
                                    num = Math.Min(num, 3);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            if (num > 3)
            {
                return 4; //I'm just using it for one thing that'll have =< 4 equal 0
            }
            return num;
        }

        private static byte inGroupbuffer;
        //private static bool IntegrationToNewGroup;
        //public static float playthissongat;
        //private static float DJstartedat;
        //private static bool skiptopoint;
        static void NewRoomPatch(On.VirtualMicrophone.orig_NewRoom orig, VirtualMicrophone self, Room room)
        {
            orig.Invoke(self, room);
            if(OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MeadowGameMode)
            {
                NewRoom(room);
            }
        }
        public static void NewRoom(Room room)
        {
            RainMeadow.Debug("New room is being checked"); 
            // If i don't know the activezones, do it immediately when you do know
            if (activeZonesDict == null)
            {
                RainMeadow.Error("missing activeZonesDict");
            }
            else
            {
                //activezonedict has the room ids of each vibe zone's room as keys
                int[] rooms = activeZonesDict.Keys.ToArray(); //why does it have to be the keys? can't this just be a list and have the id defined in class vibezone?
                float minDist = float.MaxValue;
                closestVibe = -1;
                //find the closest one
                for (int i = 0; i < rooms.Length; i++)
                {
                    //yoink the coordinates of the player's current room
                    Vector2 v1 = room.world.RoomToWorldPos(Vector2.zero, room.abstractRoom.index);
                    //yoink the coordinates of the vibe zone room
                    Vector2 v2 = room.world.RoomToWorldPos(Vector2.zero, rooms[i]);
                    //calculate the flat distance between these two vectors
                    var dist = (v2 - v1).magnitude;
                    if (dist < minDist)
                    {
                        //the closest vibezone will be the one we evaluate our distance from
                        minDist = dist;
                        closestVibe = rooms[i];
                    }
                }
                //and just grab its corresponding vibezone from the dict
                activeZone = activeZonesDict[closestVibe];
                if (minDist > activeZone.radius)
                {
                    RainMeadow.Debug("Meadow Music: Out of Vibezone Radius, set updatingtarget to false, and musicvolume set to max... ");
                    if (vibeIntensity == null) vibeIntensity = 0f;
                    if (vibeIntensity == 1f) UpdateIntensity = true; 
                    IDontWantToGoToZero = false;
                }
                else if (minDist < activeZone.radius)
                {
                    RainMeadow.Debug("Meadow Music: Inside of vibezone radius, started updating intensity...");
                    UpdateIntensity = true; //we're jumpstarting it for *every* room we traverse, if it's continuously
                    IDontWantToGoToZero = true;
                }
            }
            
            DegreesOfAwayness = CalculateDegreesOfAwayness(room.abstractRoom);
        }

        static void ShuffleSongs()
        {
            shuffleindex = 0;
            bool isfirstshuffle = shufflequeue.Length == 0;
            int shufflelastbuffle = 0;
            if (!isfirstshuffle) shufflelastbuffle = shufflequeue[shufflequeue.Length - 1];
            shufflequeue = new int[ambienceSongArray.Length];
            List<int> Hah = new List<int>();
            for (int i = 0; i < ambienceSongArray.Length; i++) { Hah.Add(i); }
            if (!isfirstshuffle) Hah.Remove(shufflelastbuffle);
            int j = 0;
            while (j < shufflequeue.Length) 
            {
                int RandomInt = UnityEngine.Random.Range(0, Hah.Count);
                shufflequeue[j] = Hah[RandomInt];
                Hah.RemoveAt(RandomInt);
                if (j == 0 && !isfirstshuffle) Hah.Add( shufflelastbuffle ); 
                j++;
            }
        }
        static void AnalyzeRegion(World world)
        {
            RainMeadow.Debug("Meadow Music: Analyzing " + world.name);
            VibeZone[] vzArray;
            activeZonesDict = null;
            if (vibeZonesDict.TryGetValue(world.region.name, out vzArray))
            {
                RainMeadow.Debug("Meadow Music: found zones " + vzArray.Length);
                activeZonesDict = new Dictionary<int, VibeZone>();
                foreach (VibeZone vz in vzArray)
                {
                    RainMeadow.Debug("Meadow Music: looking for room " + vz.room);
                    foreach (AbstractRoom room in world.abstractRooms)
                    {
                        if (room.name == vz.room)
                        {
                            RainMeadow.Debug("Meadow Music: found hub " + room.name);
                            activeZonesDict.Add(room.index, vz);
                            break;
                        }
                    }
                }
                if (activeZonesDict.Count == 0)
                {
                    RainMeadow.Debug("Meadow Music: no hubs found");
                    activeZonesDict = null;
                }
            }
            if (ambientDict.TryGetValue(world.region.name, out string[] songArr))
            {
                RainMeadow.Debug("Meadow Music: ambiences loaded");
                ambienceSongArray = songArr;
                ShuffleSongs();
            }
            else
            {
                RainMeadow.Debug("Meadow Music: no ambiences for region");
                ambienceSongArray = null;
            }
        }
    }
}