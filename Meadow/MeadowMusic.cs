using UnityEngine;
using System.IO;
using System.Collections.Generic;
using Music;
using System.Linq;
using RWCustom;
using System;
using System.Text.RegularExpressions;

namespace RainMeadow
{
    public partial class MeadowMusic
    {
        public static void EnableMusic()
        {
            CheckFiles();

            On.VirtualMicrophone.NewRoom += NewRoomPatch;

            On.Music.MusicPiece.SubTrack.StopAndDestroy += SubTrack_StopAndDestroy;
            
            On.Music.PlayerThreatTracker.Update += PlayerThreatTracker_Update; // yo which hook is this one go to? oooOOOoooooo
        }

        private static void PlayerThreatTracker_Update(On.Music.PlayerThreatTracker.orig_Update orig, PlayerThreatTracker self)
        {
            if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MeadowGameMode)
            {
                if (self.musicPlayer.manager.currentMainLoop == null || self.musicPlayer.manager.currentMainLoop.ID != ProcessManager.ProcessID.Game)
                {
                    self.recommendedDroneVolume = 0f;
                    self.currentThreat = 0f;
                    self.currentMusicAgnosticThreat = 0f;
                    self.region = null;
                    return;
                }
                if (self.playerNumber >= (self.musicPlayer.manager.currentMainLoop as RainWorldGame).Players.Count)
                {
                    return;
                }
                Player player = (self.musicPlayer.manager.currentMainLoop as RainWorldGame).Players[self.playerNumber].realizedCreature as Player;
                if (player == null || player.room == null)
                {
                    return;
                }
                if (player.room.game.GameOverModeActive || player.redsIllness != null)
                {
                    self.recommendedDroneVolume = 0f;
                    self.currentThreat = 0f;
                    self.currentMusicAgnosticThreat = 0f;
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

                float highpass = 1f;
                if (self.musicPlayer.gameObj.GetComponent<AudioHighPassFilter>() != null)
                {
                    highpass = self.musicPlayer.gameObj.GetComponent<AudioHighPassFilter>().cutoffFrequency;
                }

                if (self.ghostMode == 0f && self.musicPlayer.gameObj.GetComponent<AudioHighPassFilter>() != null)
                {
                    if (self.musicPlayer.gameObj.GetComponent<AudioHighPassFilter>().cutoffFrequency < 11f)
                    {
                        UnityEngine.Object.Destroy(self.musicPlayer.gameObj.GetComponent<AudioHighPassFilter>());
                    }
                }
                else if (self.ghostMode > 0f && self.musicPlayer.gameObj.GetComponent<AudioHighPassFilter>() == null)
                {
                    self.musicPlayer.gameObj.AddComponent<AudioHighPassFilter>().cutoffFrequency = 1f;
                    //self.musicPlayer.gameObj.GetComponent<AudioHighPassFilter>().cutoffFrequency = 1f;
                }
                if (self.ghostMode > 0f || highpass > 10f)
                {
                    self.recommendedDroneVolume = 0f;
                    //self.musicPlayer.FadeOutAllNonGhostSongs(120f);
                    //if (player.room.world.worldGhost != null && (self.musicPlayer.song == null || !(self.musicPlayer.song is GhostSong)))
                    //{
                    //    self.musicPlayer.RequestGhostSong(player.room.world.worldGhost.songName);
                    //}
                    float currenthighpass = self.musicPlayer.gameObj.GetComponent<AudioHighPassFilter>().cutoffFrequency;
                    float highpassgoal = Mathf.Lerp(0f, 1200f, Mathf.Pow(self.ghostMode, 2f));    
                    self.musicPlayer.gameObj.GetComponent<AudioHighPassFilter>().cutoffFrequency = Custom.LerpAndTick(currenthighpass, highpassgoal, 0.03f, 0.08f);
                }

                if (!player.room.world.singleRoomWorld)
                {
                    if (player.room.abstractRoom.index != self.room)
                    {
                        self.lastLastRoom = self.lastRoom;
                        self.lastRoom = self.room;
                        self.room = player.room.abstractRoom.index;
                        if (self.room != self.lastLastRoom)
                        {
                            self.roomSwitches++;
                            if (player.room.world.region.name != self.region)
                            {
                                self.region = player.room.world.region.name;
                                self.musicPlayer.NewRegion(self.region);
                            }
                        }
                    }
                    if (self.roomSwitches > 0 && self.roomSwitchDelay > 0)
                    {
                        self.roomSwitchDelay--;
                        if (self.roomSwitchDelay < 1)
                        {
                            if (self.musicPlayer.song != null)
                            {
                                self.musicPlayer.song.PlayerToNewRoom();
                            }
                            if (self.musicPlayer.nextSong != null)
                            {
                                self.musicPlayer.nextSong.PlayerToNewRoom();
                            }
                            self.roomSwitchDelay = UnityEngine.Random.Range(80, 400);
                            self.roomSwitches--;
                        }
                    }
                }
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
                
                //on the slight moment after fading out a song, the default threat theme plays
                //so just take away it calculating th threat theme lmao
            }
            else
            {
                orig(self);
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

        static int[] shufflequeue = new int[0];
        static int shuffleindex = 0;

        static float time = 0f;
        static bool timerStopped = true;

        static VibeZone? activeZone = null;
        public static bool AllowPlopping;

        public static float? vibeIntensity = null;
        public static float? vibePan = null;
        static bool UpdateIntensity;

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
        static VibeZone az = new VibeZone();
        static Dictionary<string, float> SongLengthsDict = new();
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

                Dictionary<string, float> DictTho = new();
                string[] thesongs = AssetManager.ListDirectory("music/songs", false, true);
                foreach (string song in thesongs)
                {
                    WWW www = new WWW("file://" + song); 
                    AudioClip? thing2 = www.GetAudioClip(false, true, AudioType.OGGVORBIS); //This method will have vanilla songs be 0 in length, due to its info being in assetbundles
                    float howlonghorse = thing2.length;
                    string filename = song.Split(Path.DirectorySeparatorChar)[song.Split(Path.DirectorySeparatorChar).Length - 1];
                    RainMeadow.Debug("Meadow Music:  Registered song " + filename + " to be of length " + howlonghorse);
                    DictTho.Add(filename, howlonghorse);
                }
                //The Future is here, and it's way dumber than i imagined.
                SongLengthsDict = DictTho;
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
            var creature = mgm.avatar;
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
                        var thing = entity.owner;
                        if (OnlineManager.lobby.playerAvatars[thing].FindEntity() is OnlineCreature oc && oc == entity)//yay
                        {
                            InThisRoom.Add(oc);
                        }
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

                    foreach (var entity in creature.roomSession.activeEntities.Where(v => v != null))
                    {
                        if (entity is OnlineCreature && OnlineManager.lobby.playerAvatars.TryGetValue(entity.owner, out var id) && id == entity.id && !entity.isMine)
                        {
                            playersWithMe.Add(entity.owner);
                        }
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

            if (UpdateIntensity && RoomImIn != null && MyGuyMic != null)
            {
                //i have NO idea how it'll fuck up when the region has not got a vibezone but idccccccccccccccc. oh wait it wont cuz it won't activate updateintensity cuz it'll never go close to one.
                vibePan = Vector2.Dot((RoomImIn.world.RoomToWorldPos(Vector2.zero, closestVibe) - RoomImIn.world.RoomToWorldPos(Vector2.zero, RoomImIn.abstractRoom.index)).normalized, Vector2.right);
                //RainMeadow.Debug("IsFased");
                Vector2 LOL = self.world.RoomToWorldPos(self.world.GetAbstractRoom(closestVibe).size.ToVector2() * 10f, closestVibe);
                Vector2 lol = self.world.RoomToWorldPos(MyGuyMic.listenerPoint, RoomImIn.abstractRoom.index);
                //RainMeadow.Debug("IsZased");
                float vibeIntensityTarget = 
                             Mathf.Pow(Mathf.InverseLerp(az.radius, az.minradius, Vector2.Distance(lol, LOL)), 1.425f)
                           * Custom.LerpMap((float)DegreesOfAwayness, 0f, 3f, 1f, 0.15f) //* Custom.LerpMap((float)DegreesOfAwayness, 1f, 3f, 0.6f, 0.15f)
                           * ((RoomImIn.abstractRoom.layer == self.world.GetAbstractRoom(closestVibe).layer) ? 1f : 0.75f); //az.room also works 
                //RainMeadow.Debug("IsBased");
                vibeIntensityTarget = Custom.LerpAndTick(vibeIntensity == null ? 0 : (float)vibeIntensity, vibeIntensityTarget, 0.025f, 0.002f);
                vibeIntensity = vibeIntensityTarget;
                AllowPlopping = vibeIntensity.Value >= 0.2f;
                if (musicPlayer != null && musicPlayer.song != null)
                {
                    if ((float)vibeIntensity > 0.9f) { musicPlayer.song.baseVolume = 0f; }
                    else { musicPlayer.song.baseVolume = Mathf.Pow(1f - (float)vibeIntensity, 2f) * 0.3f; }
                }                
                //RainMeadow.Debug("IsMased");
            }

            var lmd = OnlineManager.lobby.GetData<LobbyMusicData>();
            var inGroup = lmd.playerGroups[OnlineManager.mePlayer.inLobbyId];
            ushort hostId = inGroup == 0 ? (ushort)0U : lmd.groupHosts[inGroup];

            //RainMeadow.Debug("ingroup: " + inGroup);
            //RainMeadow.Debug("hostid: " + hostId);

            if (inGroup != 0 && inGroup != lastInGroup) // this doesn't get set... yet
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
                    && OnlineManager.lobby.playerAvatars.TryGetValue(other, out var otherOcId)
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
                            skiptopoint = !ivebeenpatientlywaiting;
                        }
                    }
                }
                else
                {
                    RainMeadow.Debug($"host avatar for {hostId} for group {inGroup} not found");
                }
            }
            lastInGroup = inGroup;

            if (musicPlayer != null && musicPlayer.song != null && !musicPlayer.song.FadingOut)
            {
                if (skiptopoint)
                {
                    if (musicPlayer.song.subTracks[0].source.time == 0)
                    {
                        musicdata.startedPlayingAt = DJstartedat;
                        float calculatedthing = LobbyTime() - DJstartedat;
                        musicPlayer.song.subTracks[0].source.time = calculatedthing;
                        RainMeadow.Debug("Playing from a point " + LobbyTime() + " " + DJstartedat + " which amounts to " + calculatedthing);
                        RainMeadow.Debug("and thus it's now at " + musicPlayer.song.subTracks[0].source.time + "... why is this always 0? Whatever");
                    }
                    else
                    {
                        skiptopoint = false;
                    }
                }
                else
                {
                    ivebeenpatientlywaiting = false;
                }
            }

            //note for tomorrow: previous method of checking only when your rooms playercount changes or when you switch room is bad, since it doesn't get called by players sticking to one room, nor when my fellow goes outside of my range of view.
            //so adjust *when* updateplayerthing is called: have it be called when players tranfer rooms and *are in a room* (so when it has joined resource).
            //Won't care about joinedresource's of things other than *creatures* of *players* in *my game* in *my region*. but maybe that's already taken care of? :)

            //RainMeadow.Debug("It's gotten this far, " + musicdata.inGroup + " " + musicdata.isDJ + " " + musicdata.providedSong + " " + musicdata.startedPlayingAt);

            if (musicPlayer != null && musicPlayer.song == null && self.world.rainCycle.RainApproaching > 0.5f)
            {
                musicdata.providedSong = null;
                timerStopped = false;
                if (time > waitSecs)
                {
                    RainMeadow.Debug("Tryna find a song to play");
                    if (ambienceSongArray != null)
                    {
                        if (inGroup == 0 || hostId == OnlineManager.mePlayer.inLobbyId)
                        {

                            if (shuffleindex + 1 >= shufflequeue.Length) { ShuffleSongs(); }
                            else { shuffleindex++; }
                            musicdata.providedSong = ambienceSongArray[shufflequeue[shuffleindex]];
                            RainMeadow.Debug("Meadow Music: Playing ambient song: " + musicdata.providedSong);
                            musicdata.startedPlayingAt = LobbyTime();
                            Song song = new(musicPlayer, musicdata.providedSong, MusicPlayer.MusicContext.StoryMode)
                            {
                                playWhenReady = true,
                                volume = 1,
                                fadeInTime = 1f
                            };
                            musicPlayer.song = song;

                        }
                        else if (OnlineManager.lobby.PlayerFromId(hostId) is OnlinePlayer other
                            && OnlineManager.lobby.playerAvatars.TryGetValue(other, out var otherOcId)
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

                                    DJstartedat = (float)myDJsdata.startedPlayingAt; //if it is providing a song, it should be providing a time
                                    string tring = myDJsdata.providedSong + ".ogg";
                                    RainMeadow.Debug(tring);
                                    bool IHaveThisSong = SongLengthsDict.TryGetValue(tring, out float hostsonglength);
                                    RainMeadow.Debug(IHaveThisSong);
                                    if (IHaveThisSong && hostsonglength != 0)
                                    {
                                        float hostsongprogress = (LobbyTime() - DJstartedat) / hostsonglength;
                                        if (hostsongprogress < 0.95f)
                                        {
                                            RainMeadow.Debug("Meadow Music: Playing my DJs provided song: " + myDJsdata.providedSong + (ivebeenpatientlywaiting ? " supposedly from the beginning, after patiently waiting" : " from a specific point, since i haven't waited"));

                                            Song song = new(musicPlayer, myDJsdata.providedSong, MusicPlayer.MusicContext.StoryMode)
                                            {
                                                playWhenReady = true,
                                                volume = 1,
                                                fadeInTime = ivebeenpatientlywaiting ? 1f : 120f
                                            };
                                            musicPlayer.song = song;
                                            musicdata.providedSong = myDJsdata.providedSong;

                                            RainMeadow.Debug(LobbyTime() + " " + DJstartedat);
                                        }
                                        else
                                        {
                                            RainMeadow.Debug("Meadow Music: DJ is soon done, i'll wait to play my song from the start");
                                            ivebeenpatientlywaiting = true;
                                        }
                                    }
                                    else
                                    {
                                        RainMeadow.Debug("Meadow Music: I don't have my DJs provided song.");
                                        if (shuffleindex + 1 >= shufflequeue.Length) { ShuffleSongs(); }
                                        else { shuffleindex++; }
                                        musicdata.providedSong = ambienceSongArray[shufflequeue[shuffleindex]];
                                        RainMeadow.Debug("Playing ambient song: " + musicdata.providedSong);
                                        musicdata.startedPlayingAt = LobbyTime();
                                        Song song = new(musicPlayer, musicdata.providedSong, MusicPlayer.MusicContext.StoryMode)
                                        {
                                            playWhenReady = true,
                                            volume = 1,
                                            fadeInTime = 1f
                                        };
                                        musicPlayer.song = song;
                                    }
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

        public static void TheThingTHatsCalledWhenPlayersUpdated()
        {
            var mgm = OnlineManager.lobby.gameMode as MeadowGameMode;
            var creature = mgm.avatar;
            var musicdata = creature.GetData<MeadowMusicData>();
            var RoomImIn = creature.creature.Room.realizedRoom;
            if (RoomImIn == null || creature.roomSession == null) return;

            var mgrr2 = OnlineManager.lobby.GetData<LobbyMusicData>();
            var inGroup = mgrr2.playerGroups[OnlineManager.mePlayer.inLobbyId];
            var isDJ = inGroup == 0 ? false : mgrr2.groupHosts[inGroup] == OnlineManager.mePlayer.inLobbyId;

            RainMeadow.Debug("Checking Players");

            if (inGroup == 0)
            {
                if (creature.roomSession.activeEntities.Any(
                    e => e is OnlineCreature && !e.isMine // someone elses
                    && OnlineManager.lobby.playerAvatars.TryGetValue(e.owner, out var avatarid) && e.id == avatarid)) // avatar
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
                    bool djinsameregion = false;
                    if (!isDJ)
                    {
                        var theDJ = mgrr2.groupHosts[inGroup];
                        if(OnlineManager.lobby.playerAvatars.TryGetValue(OnlineManager.lobby.PlayerFromId(theDJ), out var ocid) && ocid.FindEntity(true) is OnlineCreature oc)
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
                                        foreach (var other in OnlineManager.lobby.playerAvatars.Values.Where(v => v != null))
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

        static int closestVibe;
        //static Room? RoomImIn;
        static int DegreesOfAwayness;
        static int CalculateDegreesOfAwayness(AbstractRoom testRoom)
        {
            var vibeRoom = testRoom.world.GetAbstractRoom(az.room);
            if (vibeRoom == null) return -1;
            
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
                return -1;
            }
            return num;
        }

        private static byte lastInGroup;
        //private static bool IntegrationToNewGroup;
        public static float playthissongat;
        private static float DJstartedat;
        private static bool skiptopoint;
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
                        minDist = dist;
                        closestVibe = rooms[i];
                    }
                }
                //and just grab its corresponding vibezone from the dict
                az = activeZonesDict[closestVibe];
                if (minDist > az.radius)
                {
                    RainMeadow.Debug("Meadow Music: Out of Vibezone Radius, Disabled plopping, stopped updating intensity, and musicvolume set to max... ");
                    MusicPlayer? musicPlayer = room.game.manager.musicPlayer;
                    //musicPlayer.song.FadeOut(40f);
                    //activeZone = null;
                    vibeIntensity = 0f;
                    if (musicPlayer != null && musicPlayer.song != null) musicPlayer.song.baseVolume = 0.3f;
                    AllowPlopping = false;
                    UpdateIntensity = false;
                    //vibePan = null;
                }
                else if (minDist < az.radius)
                {
                    RainMeadow.Debug("Meadow Music: Started updating intensity and allowing plopping... ");
                    UpdateIntensity = true;
                    AllowPlopping = true;
                    //activeZone = az;
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
                foreach(VibeZone vz in vzArray)
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