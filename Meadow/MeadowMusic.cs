using Music;
using RWCustom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using UnityEngine;
using System.Threading.Tasks;
using AssetBundles;
using IL.MoreSlugcats;
#pragma warning disable CS4014 // "Because this call is not awaited, execution of the current method continues before the call is completed" whatever boomer i want that

namespace RainMeadow
{
    public partial class MeadowMusic
    {
        public const float defaultMusicVolume = 0.3f; //0.27, going to change vol of songs // maybe this could be a slider somewhere
        public const float defaultPlopVolume = 0.6f; 
        
        public static void EnableMusic()
        {
            CheckFiles();

            On.VirtualMicrophone.NewRoom += NewRoomPatch;

            On.Music.MusicPiece.SubTrack.StopAndDestroy += SubTrack_StopAndDestroy;
            
            On.Music.PlayerThreatTracker.Update += PlayerThreatTracker_Update; // joke of how hooks get grouped, so, oooo what about them below vvvvvv??  --> yo which hook is this one go to? oooOOOoooooo

            On.Music.MusicPlayer.UpdateMusicContext += MusicPlayer_UpdateMusicContext;
            On.Music.MusicPiece.StopAndDestroy += MusicPiece_StopAndDestroy;

            //In game music requests we want to hook for treatment if we're in a group (to broadcast/not play)
            On.Music.MusicPlayer.GameRequestsSong += MusicPlayer_GameRequestsSong;
            On.ActiveTriggerChecker.FireEvent += ActiveTriggerChecker_FireEvent;
            On.SSOracleBehavior.TurnOffSSMusic += SSOracleBehavior_TurnOffSSMusic;
            On.Music.SSSong.Update += SSSong_Update;
            On.Music.MusicPlayer.RainRequestStopSong += MusicPlayer_RainRequestStopSong;

            On.ActiveTriggerChecker.Update += ActiveTriggerChecker_Update;

            On.Music.MusicPiece.StartPlaying += MusicPiece_StartPlaying;

            //On.AmbientSoundPlayer.TryInitiation += AmbientSoundPlayer_TryInitiation;
        }
        //SoundId to self if you ever need it, i have gathered wisdom throughout this journey: Processmanager.Preswitchmainprocess calls soundloader.releaseallunityaudio
        //private static void AmbientSoundPlayer_TryInitiation(On.AmbientSoundPlayer.orig_TryInitiation orig, AmbientSoundPlayer self)
        //{
          //fuckoff
        //}

        //Game music hooks 

        private static void SSSong_Update(On.Music.SSSong.orig_Update orig, SSSong self)
        {
            if (self.setVolume == null && self.destroyCounter > 150)
            {
                if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MeadowGameMode mgm)
                {
                    OnlineManager.lobby.owner.InvokeRPC(BroadcastInterruption, "");
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
                    self.oracle.room.game.manager.musicPlayer.song.FadeOut(2f);
                    OnlineManager.lobby.owner.InvokeRPC(BroadcastInterruption, "");
                    //do the rpc to cancel everyone else here yeah yeah?
                    //^^yeah ok did so :) woke :)
                }
            }
        }
        private static void ActiveTriggerChecker_Update(On.ActiveTriggerChecker.orig_Update orig, ActiveTriggerChecker self, bool eu)
        {
            if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MeadowGameMode mgm)
            {
                self.evenUpdate = eu;
                if (self.counter >= 0)
                {
                    self.counter++;
                    if (self.counter >= self.eventTrigger.delay)
                    {
                        self.FireEvent();
                        self.counter = -1;
                        return;
                    }
                }
                else
                {
                    var omd = OnlineManager.lobby.GetData<LobbyMusicData>(); // this might be super expensive to let run 4 times every frame dudes. hey henp is this ok  answer here   "  "  and with your  signature here :  "   "  intkkus signature: "yarrrr"
                    var groupImIn = omd.playerGroups[OnlineManager.mePlayer.inLobbyId];
                    ushort hostId = groupImIn == 0 ? (ushort)0U : omd.groupHosts[groupImIn];
                    if (groupImIn != 0 && hostId != OnlineManager.mePlayer.inLobbyId) return;
                    AbstractCreature featuringthe = mgm.avatars[0].creature;

                    if (self.eventTrigger.type == EventTrigger.TriggerType.Spot) // && self.eventTrigger.tEvent is MusicEvent find some fucking fucking way to find this out here if you even need to
                    {

                        if (featuringthe.Room == self.room.abstractRoom)
                        {
                            if (self.wait > 0)
                            {
                                self.wait--;
                            }
                            else if (//(self.eventTrigger.entrance < 0 || featuringthe.pos.abstractNode == self.eventTrigger.entrance)  &&
                                           featuringthe.realizedCreature != null 
                                       && !featuringthe.realizedCreature.inShortcut 
                                   && Custom.DistLess(featuringthe.realizedCreature.mainBodyChunk.pos, (self.eventTrigger as SpotTrigger).pos, 
                                                     (self.eventTrigger as SpotTrigger).rad)) //Dereference of a possibly null type? reference of possybly my dick 
                            {
                                self.Positive();
                            }
                        }
                        return;
                    }
                    //if (self.eventTrigger.type switch { EventTrigger.TriggerType.PreRegionBump => 0, EventTrigger.TriggerType.RegionBump => 1, _ => 2 } < 2)
                    if (self.eventTrigger.type == EventTrigger.TriggerType.PreRegionBump || self.eventTrigger.type == EventTrigger.TriggerType.RegionBump)
                    {
                        if (self.eventTrigger.tEvent is MusicEvent)
                        {
                            //Inti - electing to simplify the game code because it's not assigning a subregion tracker because we're using our own ownertype??? the fact its another creature 
                            if (featuringthe.Room == self.room.abstractRoom)
                            {
                                if (self.room.game.cameras.Select(c=>c.followAbstractCreature).Contains(featuringthe) &&
                                    featuringthe.realizedCreature != null && !featuringthe.realizedCreature.inShortcut)
                                {
                                    self.Positive();
                                }
                            }
                        }
                        /*
                        for (int j = 0; j < self.room.game.Players.Count; j++)
                        {
                            if (self.room.game.Players[j].Room == self.room.abstractRoom)
                            {
                                int k = 0;

                                var omd = OnlineManager.lobby.GetData<LobbyMusicData>(); // this might be super expensive to let run 4 times every frame dudes. 
                                var groupImIn = omd.playerGroups[OnlineManager.mePlayer.inLobbyId];
                                ushort hostId = groupImIn == 0 ? (ushort)0U : omd.groupHosts[groupImIn]; // maybe make this into an optimized thingy :)
                                if (groupImIn != 0 && hostId != OnlineManager.mePlayer.inLobbyId) return;

                                AbstractCreature featuringthe = mgm.avatars[0].creature;
                                RainMeadow.Debug("A");
                                while (k < self.room.game.cameras.Length)
                                {
                                    RainMeadow.Debug("B");
                                    if (self.room.game.cameras[k].followAbstractCreature == self.room.game.Players[j]
                                        && featuringthe.realizedCreature != null && !featuringthe.realizedCreature.inShortcut)
                                    {
                                        RainMeadow.Debug("C");
                                        RainMeadow.Debug(self.room.game.cameras[k].hud != null);
                                        RainMeadow.Debug(self.room.game.cameras[k].hud.textPrompt != null);
                                        RainMeadow.Debug(self.room.game.cameras[k].hud.textPrompt.subregionTracker != null); // <--- this is always null? 

                                        RainMeadow.Debug(self.room.game.cameras[k].hud?.textPrompt?.subregionTracker is HUD.SubregionTracker thingy);
                                        RainMeadow.Debug(self.eventTrigger.type == EventTrigger.TriggerType.PreRegionBump);
                                        RainMeadow.Debug(self.eventTrigger.type == EventTrigger.TriggerType.RegionBump);

                                        if (self.room.game.cameras[k].hud?.textPrompt?.subregionTracker is HUD.SubregionTracker thing
                                            &&
                                            ((self.eventTrigger.type == EventTrigger.TriggerType.PreRegionBump && thing.PreRegionBump) 
                                            || (self.eventTrigger.type == EventTrigger.TriggerType.RegionBump && thing.RegionBump)))
                                        {
                                            RainMeadow.Debug("D");
                                            self.Positive();
                                            break;
                                        }
                                        break;
                                    }
                                    else
                                    {
                                        k++;
                                    }
                                }
                            }
                        }
                        */
                    }
                }
            }
            else
            {
                orig.Invoke(self, eu);
            }
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
                { //no need to check RainRequestStopSong lmao there's no rain dude it's meadow
                    self.room.game.manager.musicPlayer?.GameRequestsSong(self.eventTrigger.tEvent as MusicEvent);
                }
                else if (self.eventTrigger.tEvent.type == TriggeredEvent.EventType.StopMusicEvent)
                { 
                    self.room.game.manager.musicPlayer?.GameRequestsSongStop(self.eventTrigger.tEvent as StopMusicEvent); 
                } //for when i need to destroy a song that loops (although, this isn't what destroys a song when exiting to lobby, nor does it stop random gods, etc. do i really need this?)
            }
            else
            {
                orig.Invoke(self);
            }
        }
        private static void MusicPlayer_RainRequestStopSong(On.Music.MusicPlayer.orig_RainRequestStopSong orig, MusicPlayer self) { if (OnlineManager.lobby == null || OnlineManager.lobby.gameMode is not MeadowGameMode) orig.Invoke(self); } //hehe Just in Case lmao
        private static void MusicPlayer_GameRequestsSong(On.Music.MusicPlayer.orig_GameRequestsSong orig, MusicPlayer self, MusicEvent musicEvent)
        {
            if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MeadowGameMode mgm)
            {
                // haha fixed   shoddily bumbo --> is only called when you're a slugcat apperantly, fuck.
                //RainMeadow.Debug("Game requesting a song");
                if (self == null || loadingsong ||
                   (self.song != null && self.song.name == musicEvent.songName) ||
                   (self.song == null && self.nextSong != null && self.nextSong.name == musicEvent.songName) || 
                   songHistory.Contains(musicEvent.songName) ||
                   self.song != null && self.song.subTracks[0].source.time > 20)
                   return;
                //NO NEED TO REQUEST THE SONG IF YOU CAN'T/COULD PLAY IT OR ALREADY ARE, WILL, OR HAVE
                RainMeadow.Debug("Checking if i'm in a group");
                var smg = OnlineManager.lobby.GetData<LobbyMusicData>();
                var groupImIn = smg.playerGroups[OnlineManager.mePlayer.inLobbyId];
                ushort hostId = groupImIn == 0 ? (ushort)0U : smg.groupHosts[groupImIn];
                if (groupImIn != 0)
                {
                    if (hostId != OnlineManager.mePlayer.inLobbyId) return;
                    else OnlineManager.lobby.owner.InvokeRPC(BroadcastInterruption, musicEvent.songName); 
                }
                //if (!this.manager.rainWorld.setup.playMusic) { return; } Noting down the cause of concern 
                QueueSong(self, musicEvent.songName);
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
                if (self.musicPlayer == null || self.musicPlayer.gameObj == null || self.musicPlayer.song == null) return; //don't really need to track it in meadowmode if you don't have a song.
                if (self.musicPlayer.manager.currentMainLoop == null || self.musicPlayer.manager.currentMainLoop.ID != ProcessManager.ProcessID.Game) return;
                if (mgm.avatars[0].realizedCreature is not Creature player || player.room == null) return;

                // notes of things deleted from above: self.region gets nulled, recommendeddrone only sets dronegoalmix, which is the Volume of threat music. self.region is just for threat music.
                // Hey Henp, check out the og code. I previously thought that the song was cancelled cuz ghostmode was set by the ghostiness value from meadowghost, but that's not the whole story!
                // Since there's no *actual* worldghost (only a copy by meadowghost), it actually BREAKNECKS between 0f and 1f, so. yeah! lets change that.

                self.currentThreat = 0f;
                self.currentMusicAgnosticThreat = 0f;
                self.recommendedDroneVolume = 0f;
                self.region = null;

                float ghostiness = ((RainWorldGame)self.musicPlayer.manager.currentMainLoop).cameras[0].ghostMode;
                self.ghostMode = ghostiness > 0.1f ? ghostiness : 0f;
                
                var Components = self.musicPlayer.gameObj.GetComponent<AudioHighPassFilter>;
                if (Components() == null)
                {
                    self.musicPlayer.gameObj.AddComponent<AudioHighPassFilter>();
                    Components().enabled = true;
                    Components().cutoffFrequency = 10f;
                }

                if (self.ghostMode == 0f && Components().enabled)
                {
                    if (Components().cutoffFrequency < 12f)
                    {
                        Components().enabled = false;
                    }
                }
                else if (self.ghostMode > 0f && !Components().enabled)
                {
                    Components().enabled = true;
                }
                
                if (self.ghostMode > 0f || Components().cutoffFrequency > 12f)
                {
                    float currenthighpass = Components().cutoffFrequency;
                    float highpassgoal = Mathf.Lerp(0f, 2400f, Mathf.Pow(self.ghostMode, 2.5f));    
                    Components().cutoffFrequency = Custom.LerpAndTick(currenthighpass, highpassgoal, currenthighpass > highpassgoal ? 0.025f : 0.005f, 0.0f);
                }

                // further things commented out by this: roomswitches updating, a deltaactivated NewRegion call, a threatmusic thing for msc challenges (ew why is that there),
                // musicagnostic threat is set even when ghostmode is active, so, the threat in the world without gods LMAOOO. 
                // (Current)(MusicAgnostic)Threat. assigning music's currentthreat as the determined currenthread.
                // fun fact, msc hooks onto agnosticthreat for the pulsing trackers, which is why they will pulse even at times/places where you wouldn't *hear* anything. it's a cheat, straight up.
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
            time = null;
            timeleftofsong = null;
            PlopMachine.agora = 0;
            songHistory.Clear();
            // there's proooobably more stuff that needs resetting here
        }

        const int waitSecs = 30;

        static bool filesChecked = false;

        static readonly Dictionary<string, string[]> ambientDict = new();
        internal static readonly Dictionary<string, VibeZone[]> vibeZonesDict = new();

        internal static Dictionary<int, VibeZone> activeZonesDict = null;
        static string[] ambienceSongArray = null;

        static float? time = 0f;
        static float? timeleftofsong;
        static bool loadingsong = false;

        static int[] shufflequeue = new int[0];
        static int shuffleindex = 0;
        
        public static int closestVibe;
        static VibeZone activeZone = new VibeZone();
        public static bool AllowPlopping;

        public static float? vibeIntensity = null;
        public static float? vibePan = null;
        static bool UpdateIntensity;
        static int DegreesOfAwayness;
        static bool IDontWantToGoToZero;

        static List<string> songHistory = new();

        private static byte inGroupbuffer;
        static string songtoavoid = "";

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
        static bool gamedontload = false;
        static char thisis = 'a';
        static int sosad = 0;

        //static string[] meadowsongnames = new string[] { "403rings", "71104", "Cascen", "DustAshWrong", "Establish", "Eyes Vain", "Eyto", "Folkada", "Grasp", "Gray Orange", "Icy Parchment", "Indufor", "Live more.", "Me", "MTC", "Nevertop Side", "Ones", "Pedal Petal", "Porls", "Purple Puff", "Significance", "Slightly Ill", "Smoothed Ash", "Soup", "StepsSteps", "Swan ode", "The Crewmate", "tredjeplanen", "Triptrap X", "Trists", "Void Genesis", "Walked", "Well Phoe", "Woodback", "NA_40 - Unseen Lands" };
        //static string[] meadowsongnames = new string[] { "Swan ode", "403rings", "71104", "Folkada", "Pedal Petal", "Porls", "Significance", "The Crewmate", "Void Genesis", "Well Phoe"};
        //static int queuething = 1;
        internal static void RawUpdate(RainWorldGame self, float dt)
        {
            if (time.HasValue) time += dt;
            if (timeleftofsong.HasValue) timeleftofsong -= dt;

            if(OnlineManager.lobby?.gameMode is not MeadowGameMode mgm)
            {
                AllowPlopping = false;
                return;
            }

            MusicPlayer musicPlayer = self.manager.musicPlayer;
            var RoomImIn = self.cameras[0].room;
            var MyGuyMic = self.cameras[0].virtualMicrophone;

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
                    //If there's other IDs here, join the dominating one if one exists
                    //else, join a random other player
                    joinTimer = null; //Placing this here so that if any of the below code breaks (even though it really shouldn't), it doesn't spam.

                    RainMeadow.Debug("Jointimer elapsed, will attenpt to join someone");
                    List<OnlinePlayer> playersWithMe = new();

                    creature.roomSession.activeEntities.ForEach(e =>
                    {
                        if (e is OnlineCreature && !e.isMine && e.TryGetData<MeadowMusicData>(out _))
                            playersWithMe.Add(e.owner);
                    });

                    var mgms = OnlineManager.lobby.GetData<LobbyMusicData>();
                    List<byte> IDs = playersWithMe.Select(p => mgms.playerGroups[p.inLobbyId]).ToList();
                    int amountofIDs = IDs.Count(v => v != 0);
                    if (amountofIDs > 0)
                    {
                        byte the;
                        if (amountofIDs == 1) the = IDs[0];
                        else
                        {
                            var g = IDs.GroupBy(v => v).ToDictionary(k => k.Key, v => v.Count());
                            the = g.Count > 1 ? g.OrderByDescending(v => v.Value).ToList()[0].Key : IDs[0];
                        }
                        RainMeadow.Debug("I will ask to join this ID " + the);
                        OnlinePlayer who = playersWithMe.First(p => mgms.playerGroups[p.inLobbyId] == the);
                        OnlineManager.lobby.owner.InvokeRPC(AskNowJoinPlayer, who);
                        self.cameras[0].virtualMicrophone.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, creature.owner.inLobbyId == 1 ? -0.8f : 0.8f, 1, 1);
                    }
                    else 
                    {
                        //choose a random guy you're currently with
                        var who = playersWithMe[UnityEngine.Random.Range(0, playersWithMe.Count)];
                        RainMeadow.Debug("I will ask to join this player named " + who);
                        OnlineManager.lobby.owner.InvokeRPC(AskNowJoinPlayer, who); // the ordering
                        self.cameras[0].virtualMicrophone.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, creature.owner.inLobbyId == 1 ? -0.8f : 0.8f, 1, 1);
                    }
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
                             !IDontWantToGoToZero ? 0f : (
                             Mathf.Pow(Mathf.InverseLerp(activeZone.radius, activeZone.minradius, Vector2.Distance(PlayerPos, VibeRoomCenterPos)), 1.425f)
                           * Mathf.Clamp01(1f - (float)((float)DegreesOfAwayness * 0.3f))
                           * ((RoomImIn.abstractRoom.layer == self.world.GetAbstractRoom(closestVibe).layer) ? 1f : 0.75f) //activeZone.room also works   <--- DOES NOT ??? <--- YES IT DOES??? we got overloads on that bitch
                             );
                //RainMeadow.Debug("Has Figured out TargetIntensity");
                //reminder set vibeintensity to null on occasions where you go to menu or some shit
                vibeIntensityTarget = Custom.LerpAndTick(vibeIntensity ?? (IDontWantToGoToZero?vibeIntensityTarget:0f) , vibeIntensityTarget, 0, dt * 0.2f * (IDontWantToGoToZero?1f:3f)); //lol   vibeIntensityTarget = Custom.LerpAndTick(vibeIntensity == null ? 0 : vibeIntensity.Value, vibeIntensityTarget, 0.005f, 0.002f); // 0.025, 0.002 Actually we probably shouldn't calculate this here, in *raw update*, yknow?
                vibeIntensity = vibeIntensityTarget;
                AllowPlopping = vibeIntensity.Value >= 0.05f;
                if (musicPlayer != null && musicPlayer.song != null)
                {
                    musicPlayer.song.baseVolume = (float)vibeIntensity > 0.9f ? 0f : Mathf.Pow(1f - (float)vibeIntensity, 2.5f) * defaultMusicVolume;
                }
                if (vibeIntensity < 0.001f && vibeIntensityTarget == 0f)
                {
                    RainMeadow.Debug("vibe intensity locked to the zero... :(");
                    UpdateIntensity = false;
                    vibeIntensity = 0f;
                    if (musicPlayer != null && musicPlayer.song != null) musicPlayer.song.baseVolume = defaultMusicVolume;
                }
                else if (vibeIntensity > 0.999f && vibeIntensityTarget == 1f)
                {
                    RainMeadow.Debug("VIBE INTENSITY LOCKED TO THE MEGA!!! :D");
                    UpdateIntensity = false;
                    vibeIntensity = 1f;
                    if (musicPlayer != null && musicPlayer.song != null) musicPlayer.song.baseVolume = 0f;
                }
            }

            var lmd = OnlineManager.lobby.GetData<LobbyMusicData>();
            var groupImIn = lmd.playerGroups[OnlineManager.mePlayer.inLobbyId];
            ushort hostId = groupImIn == 0 ? (ushort)0U : lmd.groupHosts[groupImIn];

            if (groupImIn != 0 && groupImIn != inGroupbuffer)
            {
                RainMeadow.Debug("new group!");
                RainMeadow.Debug("ingroup: " + groupImIn);
                RainMeadow.Debug("hostid: " + hostId);
                if (hostId == OnlineManager.mePlayer.inLobbyId)
                {
                    // huh
                    RainMeadow.Debug("I'm the host");
                }
                else if (TryGetIfIShouldPlaySongNameThatThisIdProvides(hostId, out MeadowMusicData myDJsdata))
                {
                    // found
                    RainMeadow.Debug($"So do me and my DJs songs match? {musicdata.providedSong} == {myDJsdata.providedSong}? And how far apart are we then? {musicdata.startedPlayingAt}, {myDJsdata.startedPlayingAt}");
                    if (musicdata.providedSong != myDJsdata.providedSong || Math.Abs(musicdata.startedPlayingAt - myDJsdata.startedPlayingAt) > 5)
                    {
                        RainMeadow.Debug($"Yeah let's switch gears dude");
                        QueueSong(musicPlayer, myDJsdata.providedSong, myDJsdata.startedPlayingAt);
                    }
                }
                else
                {
                    RainMeadow.Debug($"host avatar for {hostId} for group {groupImIn} not found");
                }
            }
            inGroupbuffer = groupImIn;
            if ((timeleftofsong ?? -1f) < 10) musicdata.providedSong = "";
            if ((timeleftofsong ?? -1f) < 0 && !loadingsong) //if (musicPlayer != null && musicPlayer.song == null && musicPlayer.nextSong == null && self.world.rainCycle.RainApproaching > 0.5f && !loadingsong)
            {
                timeleftofsong = null;
                time ??= 0f;
                if (groupImIn == 0 || hostId == OnlineManager.mePlayer.inLobbyId)
                {
                    if (ambienceSongArray != null)
                    {
                        if (time > waitSecs) //&& musicdata.providedSong != songtoavoid no need for this really uh huh
                        {
                            if (shuffleindex + 1 >= shufflequeue.Length) ShuffleSongs(); 
                            else shuffleindex++; 
                            string songtobesang = ambienceSongArray[shufflequeue[shuffleindex]];

                            RainMeadow.Debug("Meadow Music: Queuing ambient song: " + songtobesang); // musicdata.providedSong
                            QueueSong(musicPlayer, songtobesang);
                        }
                    }
                }
                else if (TryGetIfIShouldPlaySongNameThatThisIdProvides(hostId, out MeadowMusicData hostMusicData)) //hmmm, maybe we don't wanna run this *every frame*. Optimise pls? Get some cache method thingy? which updates when dj updates or switches
                {
                    RainMeadow.Debug("My host now has a song i care 'bout, gonna try playing it");
                    QueueSong(musicPlayer, hostMusicData.providedSong); //fuck it always returns the same fucking song
                }
                else
                {
                    RainMeadow.Debug("Meadow Music: DJ isn't providing a song, fucking USELESS i'll keep on asking");
                }
            }
            else
            {
                time = null;
            }
            if (Input.anyKey && !gamedontload)
            {
                if (Input.GetKey(thisis.ToString()))
                {
                    sosad++;
                    thisis = sosad switch { 0 or 4 or 7 or 15 => 'a', 1 or 6 => 'l', 2 => 'e', 3 => 'x', 5 => 'p', 8 => 'y', 9 or 13 => 't', 10 or 14 => 'r', 11 => 'i', 12 or 16 => 'p', _ => 'a' };
                    if (sosad == 17)
                    {
                        sosad = 0; 
                        if (musicPlayer == null ||
                            (musicPlayer.manager.musicPlayer.song == null && musicPlayer.nextSong != null && musicPlayer.nextSong.name == "TripTrap X") ||
                            (musicPlayer.song != null && musicPlayer.song.name == "TripTrap X") ||
                            songHistory.Contains("TripTrap X")) { }
                        else
                        {
                            if (hostId == OnlineManager.mePlayer.inLobbyId)
                            {
                                OnlineManager.lobby.owner.InvokeRPC(MeadowMusic.BroadcastInterruption, "Triptrap X");
                            }
                            QueueSong(musicPlayer, "Triptrap X");
                        }
                    }
                }
                else
                {
                    thisis = 'a';
                    sosad = 0;
                }
                /*
                if (Input.GetKey(KeyCode.Insert)) QueueSong(musicPlayer, meadowsongnames[queuething = 0]);
                if (Input.GetKey(KeyCode.Delete)) QueueSong(musicPlayer, "");
                if (Input.GetKey(KeyCode.PageUp)) QueueSong(musicPlayer, meadowsongnames[queuething = ((queuething + 1) >= meadowsongnames.Length) ? 0 : (queuething + 1)]);
                if (musicPlayer != null && musicPlayer.song != null) {
                    if (Input.GetKey(KeyCode.PageDown)) musicPlayer.song.subTracks[0].source.time += 10;
                    if (Input.GetKey(KeyCode.End)     ) musicPlayer.song.subTracks[0].volume -= 0.25f; //starts at 1 
                    if (Input.GetKey(KeyCode.Home)    ) musicPlayer.song.subTracks[0].isSynced = true;
                } */
            }
            gamedontload = Input.anyKey;
        }

        /*
        //static KeyValuePair<ushort, OnlineCreature> HostAvatar;
        //private static string lastsongplayed;
        private static bool TryGetIfIShouldPlaySongNameThatThisIdProvides(ushort Id, out MeadowMusicData nameOfSong)
        {
            nameOfSong = null;
            if (OnlineManager.lobby.PlayerFromId(Id) is OnlinePlayer other
            && OnlineManager.lobby.playerAvatars.FirstOrDefault(kvp => kvp.Key == other).Value is OnlineEntity.EntityId otherOcId
            && otherOcId.FindEntity() is OnlineCreature oc)
            {
                if (oc.GetData<MeadowMusicData>() is MeadowMusicData myDJsdata
                    && !(myDJsdata.providedSong == "" || myDJsdata.providedSong == null || myDJsdata.providedSong == songtoavoid))
                {
                    //if (myDJsdata.providedSong == lastsongplayed)
                    //{
                    //    RainMeadow.Debug("Fake, " + myDJsdata.providedSong);
                    //    return false;
                    //}
                    //lastsongplayed = myDJsdata.providedSong;
                    songtoavoid = "";
                    nameOfSong = myDJsdata;
                    return true;
                }
                return false;
            }
            else
            {
                RainMeadow.Debug($"{Id} does not link to a creature"); //Maybe I should make it null
                return false;
            }
        }
        */
        static KeyValuePair<ushort, OnlineCreature> HostAvatar;
        private static bool TryGetIfIShouldPlaySongNameThatThisIdProvides(ushort Id, out MeadowMusicData nameOfSong)
        {
            nameOfSong = null;
            if (Id == HostAvatar.Key)
            {
                //We already got the creature of ID
            }
            else if (OnlineManager.lobby.PlayerFromId(Id) is OnlinePlayer other
            && OnlineManager.lobby.playerAvatars.FirstOrDefault(kvp => kvp.Key == other).Value is OnlineEntity.EntityId otherOcId
            && otherOcId.FindEntity() is OnlineCreature oc)
            {
                //Register the new host and its creature
                HostAvatar = new KeyValuePair<ushort, OnlineCreature>(Id, oc);
            }
            else
            {
                RainMeadow.Debug($"{Id} does not link to a creature"); //Maybe I should make it null
                return false;
            }

            if (HostAvatar.Value.TryGetData(out MeadowMusicData myDJsdata)
                && !(myDJsdata.providedSong == "" || myDJsdata.providedSong == null || myDJsdata.providedSong == songtoavoid))
            {
                songtoavoid = "";
                nameOfSong = myDJsdata;
                return true;
            }
            else
            {
                RainMeadow.Debug("Nah");
                return false;
            }
        }
        
        private static void MusicPiece_StartPlaying(On.Music.MusicPiece.orig_StartPlaying orig, MusicPiece self)
        {
            orig.Invoke(self);
            if (OnlineManager.lobby == null || OnlineManager.lobby.gameMode is not MeadowGameMode mgm) return;
            var creature = mgm.avatars[0];
            var musicdata = creature.GetData<MeadowMusicData>();
            RainMeadow.Debug("Sets my song into being " + self.name);
            musicdata.providedSong = self.name;
            songHistory.Add(self.name);
            while ( songHistory.Count >= 8 ) { songHistory.RemoveAt(0); }
        }
        static string latestrequest = "";
        private static async Task QueueSong(MusicPlayer? musicPlayer, string songtobesang, float? timetobestarted = null) //start the brain worm of accounting for muted folks by making queuesong nullable lol
        {
            if (songtobesang == "" || songtobesang == null)
            {
                musicPlayer?.song?.FadeOut(20f);
                RainMeadow.Debug("the song of silence");
                return;
            }
            latestrequest = songtobesang;
            loadingsong = true;
            RainMeadow.Debug("Queued song " + songtobesang);
            float timmmetaken = Time.time;
            if (UnityEngine.Debug.isDebugBuild) // debug build can't do certain things outside of main thread, go figure
            {
                RainMeadow.Debug("Loading song");
                Song? song = LoadSong(musicPlayer, songtobesang, timetobestarted);
                RainMeadow.Debug("Playing song");
                if (musicPlayer != null) PlaySong(musicPlayer, song, timmmetaken, timetobestarted);
            }
            else
            {
                Task.Run(() =>
                {
                    try
                    {
                        RainMeadow.Debug("Loading song");
                        Song? song = LoadSong(musicPlayer, songtobesang, timetobestarted);
                        RainMeadow.Debug("Playing song");
                        if (musicPlayer != null) PlaySong(musicPlayer, song, timmmetaken, timetobestarted);
                    }
                    catch (Exception e)
                    {
                        RainMeadow.Error(e);
                    }
                });
            }
            RainMeadow.Debug("Done queueing song " + songtobesang);
        }
        private static Song? LoadSong(MusicPlayer? musicPlayer, string providedsong, float? DJstartedat)
        {
            RainMeadow.Debug("Loading song");
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
                LoadedAssetBundle loadedAssetBundle2 = AssetBundleManager.GetLoadedAssetBundle("music_songs", out _);
                if (loadedAssetBundle2 != null)
                {
                    RainMeadow.Debug("Loads the song unsafetly?");
                    clipclip = loadedAssetBundle2.m_AssetBundle.LoadAsset<AudioClip>(providedsong);
                }
            }

            if (clipclip == null)
            {
                RainMeadow.Debug($"Could not fetch the clip to the requested song {providedsong}");
                return null;
            }
            
            bool willfadein = false;
            if (DJstartedat.HasValue) 
            {
                float hostsonglength = clipclip.length;
                if (hostsonglength != 0)
                {
                    float hostsongprogress = (LobbyTime() - DJstartedat.Value) / hostsonglength;
                    if (hostsongprogress < 0.95f)
                    {
                        willfadein = hostsongprogress > 0.05f;
                    }
                    else
                    {
                        RainMeadow.Debug("Meadow Music: I have my DJs song, but i've figured since my DJ is soon done, i'll stop and wait for a differently named song");
                        if (musicPlayer != null)
                        {
                            musicPlayer.song?.FadeOut(20f);
                            musicPlayer.nextSong = null;
                        }
                        songtoavoid = providedsong;
                        return null;
                    }
                }
                else
                {
                    RainMeadow.Error("Meadow Music: I don't have my DJs provided song [even after all those checks :< ]. Waiting for a differently named song");
                    songtoavoid = providedsong;
                    return null;
                }
            }

            if (providedsong == latestrequest) timeleftofsong = clipclip.length - (DJstartedat.HasValue ? (LobbyTime() - DJstartedat.Value) : 0f);

            if (musicPlayer == null)
            {
                var musicdata = ((MeadowGameMode)OnlineManager.lobby.gameMode).avatars[0].GetData<MeadowMusicData>();
                musicdata.startedPlayingAt = DJstartedat ?? LobbyTime();
                musicdata.providedSong = providedsong;
                return null;
            }
            else
            {
                Song song = new(musicPlayer, providedsong, MusicPlayer.MusicContext.StoryMode);
                if (willfadein) song.fadeInTime = 120f;
                MusicPiece.SubTrack sub = song.subTracks[0];
                sub.source.Pause();
                sub.source.clip = clipclip;
                sub.isStreamed = true;
                if (sub.piece.musicPlayer.manager.rainWorld.OptionsReady) sub.source.volume = Mathf.Pow((sub.volume * sub.piece.volume * sub.piece.musicPlayer.manager.rainWorld.options.musicVolume), sub.piece.musicPlayer.manager.soundLoader.volumeExponent); 
                //&& !sub.piece.startedPlaying
                //sub.requestPlay = true;
                sub.readyToPlay = true;
                sub.isSynced = false;
                song.subTracks[0] = sub;
                return song;
            }
        }
        private static void PlaySong(MusicPlayer musicPlayer, Song? song, float timmmetaken, float? timetobestarted = null)
        {
            RainMeadow.Debug("Playing song");
            if (song == null)
            {
                RainMeadow.Debug("Song was null");
            }
            else if (song.name != latestrequest)
            {
                RainMeadow.Debug("Song was fucking SUPID i DONT like this one i want the NEW one");
                return;
            }
            else
            {
                var musicdata = ((MeadowGameMode)OnlineManager.lobby.gameMode).avatars[0].GetData<MeadowMusicData>();
                song.priority = 255_207_64f; // :) I trust my DJ's songs to be tantamount :)
                song.stopAtDeath = false; //well, just to be sure!
                song.stopAtGate = false;
                song.lp = song.name == "NA_41 - Random Gods"; //a name that's right next to "NA_40 - Unseen Lands", funny

                if (timetobestarted != null)
                {
                    musicdata.startedPlayingAt = timetobestarted.Value;
                    float calculatedthing = LobbyTime() - timetobestarted.Value;
                    song.subTracks[0].source.time = calculatedthing + (Time.time - timmmetaken) + 1f + ((musicPlayer.song != null) ? (2f / 3f) : 0f); //rough guesstimate, this is a *lazy* solution sponsored by line 158 186. In the future maybe we'd whip some cooler methodformulathingamajig up , maybe hooking onto musicplayer.Update ? ababa
                    RainMeadow.Debug("Playing from a point " + LobbyTime() + " " + timetobestarted.Value + " which amounts to " + calculatedthing);
                }
                else musicdata.startedPlayingAt = LobbyTime();
                if (musicPlayer.nextSong != null && (musicPlayer.nextSong.priority >= song.priority || musicPlayer.nextSong.name == song.name))
                {
                    RainMeadow.Debug("song collision happened!" + musicPlayer.nextSong.name);
                    loadingsong = false;
                    return;
                }
                musicPlayer.nextSong = song; //an interuption will thencefourthe (theory and henceforce) always still be honored 
                musicPlayer.nextSong.playWhenReady = false;
                if (!UpdateIntensity) musicPlayer.nextSong.baseVolume = ((vibeIntensity == 0f) ? defaultMusicVolume : 0f);
                RainMeadow.Debug("my song is now " + musicdata.providedSong);
                RainMeadow.Debug("my song is to be " + song.name);
            }
            loadingsong = false;
        }
        public static void TheThingTHatsCalledWhenPlayersUpdated()
        {
            var creature = (OnlineManager.lobby.gameMode as MeadowGameMode).avatars[0];
            var RoomImIn = creature.creature.Room.realizedRoom;
            if (RoomImIn == null || creature.roomSession == null) return;

            var mgrr2 = OnlineManager.lobby.GetData<LobbyMusicData>();
            var groupImIn = mgrr2.playerGroups[OnlineManager.mePlayer.inLobbyId];
            var isDJ = groupImIn != 0 && (mgrr2.groupHosts[groupImIn] == OnlineManager.mePlayer.inLobbyId);

            RainMeadow.Debug("Checking Players");

            var VibeRoomCreatures = creature.abstractCreature.Room.world.GetAbstractRoom(closestVibe);
            if (VibeRoomCreatures != null)
            { 
                //PlopMachine.agora = VibeRoomCreatures.creatures.Count(); 
            }

            if (groupImIn == 0)
            {
                if (creature.roomSession.activeEntities.Any(
                    e => e is OnlineCreature 
                    && !e.isMine // someone elses
                    && e.TryGetData<MeadowMusicData>(out _)) // avatar
                    ) 
                {
                    RainMeadow.Debug("There are other people here!");
                    joinTimer ??= 5;
                }
                else
                {
                    RainMeadow.Debug("Nobody to join");
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

                bool IAmWithMyFriends = IDsWithMe.Count(v => v == groupImIn) > 1;
                //if (vibeRoom == null) return -1;
                if (!IAmWithMyFriends)
                {
                    RainMeadow.Debug("No dice, checks one degree of seperation for anyone, Room creature is in: " + creature.abstractCreature.Room.name);
                    RainMeadow.Debug("And it thinks that my connections are " + Newtonsoft.Json.JsonConvert.SerializeObject(creature.abstractCreature.Room.connections));
                    foreach (int connection in creature.abstractCreature.Room.connections)
                    {
                        if (connection != -1  //RainMeadow.Debug("Pointing towards connection: " + connection);
                            && creature.abstractCreature.Room.world.GetAbstractRoom(connection) is AbstractRoom abstractRoom // RainMeadow.Debug("My neighbor " + abstractRoom.name);
                            && abstractRoom.creatures.Count() != 0) //RainMeadow.Debug("Hey this room has creatures in it");
                        {
                            foreach (AbstractCreature? entity in abstractRoom.creatures)
                            {
                                if (entity.GetOnlineCreature() is OnlineCreature oc 
                                    && mgrr2.playerGroups.TryGetValue(oc.owner.inLobbyId, out var group)) //RainMeadow.Debug(entity);
                                {
                                    IAmWithMyFriends = group == groupImIn;
                                    if (IAmWithMyFriends) { RainMeadow.Debug("Foundafriend!"); break; }
                                }
                            }
                        }
                    }
                    if (!IAmWithMyFriends) RainMeadow.Debug("I don't believe anyone around me is my guys");
                }

                if (!IAmWithMyFriends)
                {
                    demiseTimer ??= 12.5f;
                    RainMeadow.Debug(demiseTimer == 12.5 ? "Started" : "Continuing" + " demisetimer due to not being with friends");
                    groupdemiseTimer = null;
                }
                else
                {
                    //checks if the host is in the same region as you
                    bool djinsameregion = true;
                    if (!isDJ)
                    {
                        djinsameregion = false;
                        var theDJ = mgrr2.groupHosts[groupImIn];
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
                        if (demiseTimer == null) RainMeadow.Debug("Started Demisetimer due to not being in the same region as DJ"); 
                        demiseTimer ??= 12.5f; 
                    }
                    else
                    {
                        //check the amount 

                        List<byte> IDs = IDsWithMe.ToList();
                        IDs.RemoveAll(v => v == 0);
                        var result = IDs.GroupBy(v => v)
                                        .ToDictionary(k => k.Key, v => v.Count())
                                        .OrderByDescending(v => v.Value)
                                        .ToList();
                        if (result.Count > 1)
                        {// dramaaa~
                            if (result[0].Value == result[1].Value)
                            {
                                if (result[0].Key == groupImIn || result[1].Key == groupImIn)
                                {
                                    demiseTimer = null;
                                    groupdemiseTimer = (result[0].Value + result[1].Value) * 6f;
                                }
                                else
                                {
                                    demiseTimer ??= 6f * OnlineManager.lobby.playerAvatars.Select(kvp => kvp.Value).Count(other => other.FindEntity() is OnlineCreature oc && mgrr2.playerGroups[oc.owner.inLobbyId] == groupImIn); //fuck it, lol
                                    groupdemiseTimer = null;
                                }
                            }
                            else
                            {
                                if (result[0].Key == groupImIn)
                                {
                                    demiseTimer = null;
                                    groupdemiseTimer = null;
                                }
                                else
                                {
                                    if (demiseTimer == null) { demiseTimer = 12.5f; RainMeadow.Debug("Started Demisetimer due to the only group there being not mine"); } //*X being group
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

        static void NewRoomPatch(On.VirtualMicrophone.orig_NewRoom orig, VirtualMicrophone self, Room room)
        {
            var waspos = self.listenerPoint;
            orig.Invoke(self, room);
            if(OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MeadowGameMode)
            {
                self.listenerPoint = waspos; // no mmf bs fr fr
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
                    vibeIntensity ??= 0f;
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