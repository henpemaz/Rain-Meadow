using UnityEngine;
using System.IO;
using System.Collections.Generic;
using Music;
using System.Linq;
using RWCustom;
using IL.MoreSlugcats;
using System;
using IL;
using Steamworks;
using UnityEngine.Assertions.Must;
using HarmonyLib;
using System.Net.NetworkInformation;
using On;
using UnityEngine.UI;

namespace RainMeadow
{
    public partial class MeadowMusic
    {
        public static void EnableMusic()
        {
            CheckFiles();

            On.RainWorldGame.ctor += GameCtorPatch;
            On.RainWorldGame.RawUpdate += RawUpdatePatch;
            On.OverWorld.WorldLoaded += WorldLoadedPatch;
            On.OverWorld.LoadFirstWorld += OverWorld_LoadFirstWorld;
            On.VirtualMicrophone.NewRoom += NewRoomPatch;
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

        static void GameCtorPatch(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
        {
            orig.Invoke(self, manager);
            if (OnlineManager.lobby == null || OnlineManager.lobby.gameMode is not MeadowGameMode) return;

            time = 0f;
            timerStopped = true;
        }
        [RPCMethod]
        static void AskNowLeave(RPCEvent rpcEvent, ushort meadowLobbyPlayerId) //could maybe have "i'm a host" be a parameter but perspectiveeeee
        {
            RainMeadow.Debug("A player is asking to leave");
            int? HostOf = null;
            var oc2 = OnlineManager.lobby.playerAvatars[OnlineManager.lobby.PlayerFromId(meadowLobbyPlayerId)]?.FindEntity();

            var ootherdata = oc2.GetData<MeadowMusicData>();
            if (ootherdata.isDJ) { HostOf = ootherdata.inGroup; }

            if (HostOf != null)
            {
                foreach (var other in OnlineManager.lobby.playerAvatars.Values.Where(v => v != null))
                {
                    if (other.FindEntity() is OnlineCreature oc)
                    {
                        if (oc.owner.inLobbyId != meadowLobbyPlayerId)
                        {
                            var otherdata = oc.GetData<MeadowMusicData>();
                            if (otherdata.inGroup == HostOf) 
                            {
                                OnlinePlayer ThePlayer = oc.owner;
                                ThePlayer.InvokeRPC(TellNowJoinPlayer, otherdata.inGroup, true);
                                break;
                            }
                        }
                    }
                }
            }

            rpcEvent.from.InvokeRPC(TellNowJoinPlayer, -1, true);
        }
        [RPCMethod]
        static void AskNowJoinID(RPCEvent rpcEvent, int RequestedID) //the server serving
        {
            RainMeadow.Debug("A player is asking to join another ID");
            bool IDisUnique = true;
            foreach (var other in OnlineManager.lobby.playerAvatars.Values.Where(v => v != null))
            {
                if (other.FindEntity() is OnlineCreature oc)
                {
                    var otherdata = oc.GetData<MeadowMusicData>();
                    // proccess other data
                    if (otherdata.inGroup == RequestedID) IDisUnique = false;
                }
            }

            int newgroup = RequestedID;
            bool isdj = IDisUnique;

            rpcEvent.from.InvokeRPC(TellNowJoinPlayer, newgroup, isdj);
        }
        [RPCMethod]
        static void AskNowJoinPlayer(RPCEvent rpcEvent, OnlineEntity.EntityId entityid) //the server serving
        {
            RainMeadow.Debug("A player is asking to join another Player named " + entityid);
            OnlineCreature? JoingingThisGuy = entityid.FindEntity() as OnlineCreature; //actually this'll have to just be a name eventually.
            int? newgroup = null;
            bool StartUnique = false;
            

            if (JoingingThisGuy == null) return; //henp this is fucked up im scared
            
            var TheirData = JoingingThisGuy.GetData<MeadowMusicData>();
            if (TheirData.inGroup == -1)
            {
                List<int> ints = new List<int>();
                RainMeadow.Debug("Creating new groupID");
                {
                    foreach (var other in OnlineManager.lobby.playerAvatars.Values.Where(v => v != null))
                    {
                        if (other.FindEntity() is OnlineCreature oc && !oc.owner.isMe)
                        {
                            var otherdata = oc.GetData<MeadowMusicData>();
                            if (otherdata.inGroup != -1) ints.Add(otherdata.inGroup);
                        }
                    }
                }

                if (ints.Count != 0)
                {
                    ints.Sort();
                    int i = 0;
                    int j = ints[i];
                    while(newgroup == null)
                    {
                        i++;
                        if (i == ints.Count)
                        {
                            newgroup = j + 1;
                            break;
                        }
                        if (ints[i] != j + 1 && ints[i] != j)
                        {
                            newgroup = j;
                        }
                        j = ints[i];
                    }
                }
                else
                {
                    newgroup = 0;
                }
                StartUnique = true;
            }
            else
            {
                newgroup = TheirData.inGroup;
            }

            rpcEvent.from.InvokeRPC(TellNowJoinPlayer, newgroup, StartUnique);
        }
        [RPCMethod]
        static void AskNowSquashPlayers(RPCEvent rpcEvent, ushort[] playersinquestion)
        {
            RainMeadow.Debug("A player is asking to squash an array of folks together");
            //make unique ID and feed it to all the people
            List<int> ints = new List<int>();

            foreach (var other in OnlineManager.lobby.playerAvatars.Values.Where(v => v != null))
            {
                if (other.FindEntity() is OnlineCreature oc)
                {
                    // proccess other data
                    if (!ints.Contains(oc.GetData<MeadowMusicData>().inGroup))
                    {
                        ints.Add(oc.GetData<MeadowMusicData>().inGroup);
                    }
                }
            }
            int i = 0;
            while (true)
            {
                if (ints.Contains(i))
                {
                    i++;
                }
                else
                {
                    break;
                }
            }

            for (int j = 0; j < playersinquestion.Length; j++)
            {
                //send a request to playersinquestion[j]
                OnlineManager.lobby.PlayerFromId(playersinquestion[j]).InvokeRPC(TellNowJoinPlayer, i, j == 0);
            }
        }
        [RPCMethod]
        static void TellNowJoinPlayer(int newgroup, bool isdj) //the eating that shit up
        {
            //nullcheks :3
            var mgm = OnlineManager.lobby.gameMode as MeadowGameMode; // could be *not* meadowgamemode, if so break everything
            var creature = mgm.avatar;
            var musicdata = creature.GetData<MeadowMusicData>();
            int oldgroup = musicdata.inGroup;
            if (newgroup != -1 && oldgroup == -1 && !isdj)
            {
                FadeThatShitOut = true;
            }
            musicdata.isDJ = isdj;
            musicdata.inGroup = newgroup;
        }
        static bool HasCheckedPlayers = false;
        public static void TheThingTHatsCalledWhenPlayersUpdated()
        {
            HasCheckedPlayers = true;
            RainMeadow.Debug("Amount of players have been updated");
            var mgm = OnlineManager.lobby.gameMode as MeadowGameMode;
            var creature = mgm.avatar;
            var musicdata = creature.GetData<MeadowMusicData>();
            
            if (musicdata.inGroup == -1)
            {
                bool theresotherbozoes = false;
                if (creature.roomSession != null) 
                {
                    foreach (var entity in creature.roomSession.activeEntities.Where(v => v != null))
                    {
                        var thing = entity.owner;
                        RainMeadow.Debug("I see " + thing + " " + entity);
                        if (OnlineManager.lobby.playerAvatars[thing].FindEntity() is OnlineCreature oc && !oc.owner.isMe) //yay
                        {
                            if (!oc.isTransferable) //if it's not transferrable ig it'd be a players? Henp please fix
                            {
                                theresotherbozoes = true;
                                RainMeadow.Debug("Yay!");
                                //break;
                            }
                        }
                    }
                }
                else
                {
                    RainMeadow.Debug("So the bugger,,,, is still here.....");
                    //until they do their magic with fixing roomsessions or whatever
                    ItchingForRoomSession = true;
                }

                if (theresotherbozoes)
                {
                    joinTimer = 5;
                }
                else
                {
                    joinTimer = null;
                }
            }
            else
            {
                //checks one degree of seperation for anyone
                List<OnlineCreature> RoomWithMe = new List<OnlineCreature>();
                if (creature.roomSession != null)
                {
                    foreach (var entity in creature.roomSession.activeEntities.Where(v => v != null))
                    {
                        var thing = entity.owner;
                        if (OnlineManager.lobby.playerAvatars[thing].FindEntity() is OnlineCreature oc && !oc.owner.isMe)
                        {
                            RoomWithMe.Add(oc);
                        }
                    }
                }

                bool IAmWithMyFriends = RoomWithMe.Count(v => v.GetData<MeadowMusicData>().inGroup == musicdata.inGroup) != 0;
                //if (vibeRoom == null) return -1;
                if (!IAmWithMyFriends)
                {
                    List<OnlineCreature> GangNextDoor = new List<OnlineCreature>();
                    RainMeadow.Debug(az.room);

                    for (int i = 0; i < RoomImIn.abstractRoom.connections.Length; i++)
                    {
                        //var game = vibeRoom.connections[i];
                        AbstractRoom abstractRoom = RoomImIn.world.GetAbstractRoom(RoomImIn.abstractRoom.connections[i]);

                        foreach (var entity in abstractRoom.creatures)
                        {
                            foreach (var other in OnlineManager.lobby.playerAvatars.Values.Where(v => v != null))
                            {
                                if (other.FindEntity() is OnlineCreature oc)
                                {
                                    if (oc.creature == entity) GangNextDoor.Add(oc);
                                }
                            }
                        }
                    }
                    IAmWithMyFriends = GangNextDoor.Count(v => v.GetData<MeadowMusicData>().inGroup == musicdata.inGroup) != 0;
                }

                if (!IAmWithMyFriends) 
                {
                    if (demiseTimer == null) demiseTimer = 12.5f;
                    groupdemiseTimer = null;
                }
                else
                {
                    //checks if the host is in the same region as you
                    bool djinsameregion = false;
                    MeadowMusicData? myDJsdata = musicdata; //just to make another line shut up + if noone else is, then i am
                    foreach (var other in OnlineManager.lobby.playerAvatars.Values.Where(v => v != null))
                    {
                        if (other.FindEntity() is OnlineCreature oc && !oc.owner.isMe)
                        {
                            var otherdata = oc.GetData<MeadowMusicData>();
                            // proccess other data
                            if (otherdata.inGroup == musicdata.inGroup && otherdata.isDJ)
                            {
                                //myDJsdata = otherdata;
                                djinsameregion = oc.abstractCreature.world.region.name == creature.abstractCreature.world.region.name;
                            }
                        }
                    }
                    
                    //if mydj is in same region as me
                    if (!djinsameregion)
                    {
                        if (demiseTimer == null) demiseTimer = 12.5f;
                    }
                    else
                    {
                        //check the amount 
                        
                        List<int> IDs = RoomWithMe.ToList().ConvertAll(v => v.GetData<MeadowMusicData>().inGroup);
                        IDs.RemoveAll(v => v == -1);
                        var g = IDs.GroupBy(v => v);
                        var result = g.OrderByDescending(v => v).ToList();
                        if (result.Count > 1)
                        {
                            if (result[0].Count() == result[1].Count())
                            {
                                if (result[0].Key == musicdata.inGroup || result[1].Key == musicdata.inGroup)
                                {
                                    //groupdemistimer thingy
                                    groupdemiseTimer = (result[0].Count() + result[1].Count()) * 6f;
                                    demiseTimer = null;
                                }
                                else
                                {
                                    if (demiseTimer != null) {

                                        int i = 0;
                                        foreach (var other in OnlineManager.lobby.playerAvatars.Values.Where(v => v != null))
                                        {
                                            if (other.FindEntity() is OnlineCreature oc)
                                            {
                                                var otherdata = oc.GetData<MeadowMusicData>();
                                                if (otherdata.inGroup == musicdata.inGroup)
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
                                if (result[0].Key == musicdata.inGroup)
                                {
                                    groupdemiseTimer = null;
                                    demiseTimer = null;
                                }
                                else 
                                {
                                    if (demiseTimer != null) demiseTimer = 12.5f; //*X being group
                                    groupdemiseTimer = null;
                                }
                            }
                        }
                    }
                }
            }
        }
        static bool ItchingForRoomSession = false;
        static void RawUpdatePatch(On.RainWorldGame.orig_RawUpdate orig, RainWorldGame self, float dt)
        {
            orig.Invoke(self, dt);
            if (OnlineManager.lobby == null || OnlineManager.lobby.gameMode is not MeadowGameMode) return;
            if (!timerStopped) time += dt;
            MusicPlayer musicPlayer = self.manager.musicPlayer;

            var mgm = OnlineManager.lobby.gameMode as MeadowGameMode;
            var creature = mgm.avatar;
            var musicdata = creature.GetData<MeadowMusicData>();
            HasCheckedPlayers = false;

            if (demiseTimer != null)
            {
                demiseTimer -= dt;
                if (demiseTimer < 0)
                {
                    //LeaveGroup
                    RainMeadow.Debug("I will be asking to leave");
                    OnlineManager.lobby.owner.InvokeRPC(AskNowLeave, creature.owner.inLobbyId);
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

                    List<OnlineCreature> InThisRoom = new List<OnlineCreature>();

                    foreach (var entity in creature.roomSession.activeEntities.Where(v => v != null))
                    {
                        var thing = entity.owner;
                        if (OnlineManager.lobby.playerAvatars[thing].FindEntity() is OnlineCreature oc)//yay
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
                    List<OnlineCreature> RoomWithMe = new List<OnlineCreature>();

                    foreach (var entity in creature.roomSession.activeEntities.Where(v => v != null))
                    {
                        var thing = entity.owner;
                        if (OnlineManager.lobby.playerAvatars[thing].FindEntity() is OnlineCreature oc && !oc.owner.isMe)//yay
                        {
                            RoomWithMe.Add(oc);
                        }
                    }

                    bool theresaguywithanID = false;
                    List<int> IDs = RoomWithMe.ToList().ConvertAll(v => v.GetData<MeadowMusicData>().inGroup);
                    theresaguywithanID = IDs.Count(v => v != -1) > 0;
                    if (theresaguywithanID)
                    {
                        var g = IDs.GroupBy(v => v);
                        var result = g.OrderByDescending(v => v).ToList();
                        //l1 = l1.Select(v => v.Key);
                        RainMeadow.Debug("I ask to join this ID " + result[0].Key);
                        OnlineManager.lobby.owner.InvokeRPC(AskNowJoinID, result[0].Key);
                    }
                    else 
                    {
                        //choose a random guy you're currently with
                        int rand = UnityEngine.Random.Range(0, RoomWithMe.Count);
                        OnlineManager.lobby.owner.InvokeRPC(AskNowJoinPlayer, RoomWithMe[rand].id); // the ordering
                        RainMeadow.Debug("I ask to join this player named " + RoomWithMe[rand].id);
                    }
                    joinTimer = null;
                }
            }
            if (ItchingForRoomSession)
            {

                if(creature.roomSession != null)
                {
                    TheThingTHatsCalledWhenPlayersUpdated();
                    ItchingForRoomSession = false;
                }
            }

            
            if (dontskiptopoint)
            {
                if (musicPlayer != null && musicPlayer.song != null)
                {
                    if (playfromstart)
                    {
                        RainMeadow.Debug("Playing from start");
                    }
                    else
                    {
                        self.manager.musicPlayer.song.subTracks[0].source.time = LobbyTime() - DJstartedat;   
                        RainMeadow.Debug("Playing from a point " + LobbyTime() +" "+ DJstartedat);
                    }
                    ivebeenpatientlywaiting = true;
                    dontskiptopoint = false;
                    playfromstart = false;
                }
            }
            else
            {

            }
            

            //RainMeadow.Debug("Roomsessionshit " + (creature.roomSession != null) + "    Group of" + musicdata.inGroup);

            //RainMeadow.Debug("I am in a room aye? " + RoomImIn + " Yeah room, and is it not null? " + RoomImIn != null);

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
                           * ((RoomImIn.abstractRoom.layer == self.world.GetAbstractRoom(closestVibe).layer) ? 1f : 0.75f);
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

            // use musicdata
            // this is my own music data
            
            
            if (FadeThatShitOut)
            {
                if (musicPlayer != null && musicPlayer.song != null)
                {
                    musicPlayer.song.FadeOut(40f);
                }
                FadeThatShitOut = false;
            }

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
                        if (musicdata.isDJ)
                        {
                            RainMeadow.Debug("Meadow Music: Playing ambient song");
                            
                            if (shuffleindex + 1 >= shufflequeue.Length) { ShuffleSongs(); }
                            else { shuffleindex++; }
                            musicdata.providedSong = ambienceSongArray[shufflequeue[shuffleindex]];
                            musicdata.startedPlayingAt = LobbyTime();
                            Song song = new(musicPlayer, musicdata.providedSong, MusicPlayer.MusicContext.StoryMode)
                            {
                                playWhenReady = true,
                                volume = 1,
                                fadeInTime = 1f
                            };
                            musicPlayer.song = song;
                        }
                        else
                        {
                            RainMeadow.Debug("Trying to get host");

                            MeadowMusicData? myDJsdata = musicdata; //just to make line 226 shut up + if noone else is a DJ, then i am
                            foreach (var other in OnlineManager.lobby.playerAvatars.Values.Where(v => v != null))
                            {
                                if (other.FindEntity() is OnlineCreature oc && !oc.owner.isMe)
                                {
                                    var otherdata = oc.GetData<MeadowMusicData>();
                                    // proccess other data
                                    RainMeadow.Debug("I see "+ other + "  *blinks eyes*: " + otherdata.inGroup + " " + otherdata.inGroup + " " + otherdata.isDJ + " " + otherdata.startedPlayingAt);

                                    if (otherdata.inGroup == musicdata.inGroup && otherdata.isDJ)
                                    {
                                        myDJsdata = otherdata;
                                    }
                                }
                            }

                            RainMeadow.Debug("My DJ *blinks eyes*: " + myDJsdata.providedSong + " " + myDJsdata.inGroup + " " + myDJsdata.isDJ + " " + myDJsdata.startedPlayingAt);

                            if (myDJsdata != null)
                            {
                                if (myDJsdata.providedSong != null)
                                {
                                    RainMeadow.Debug("My host has a song, gonna try playing it");
                                    
                                    float lobbydottime = LobbyTime();
                                    DJstartedat = (float)myDJsdata.startedPlayingAt; //if it is providing a song, it should be providing a time
                                    string tring = myDJsdata.providedSong + ".ogg";
                                    RainMeadow.Debug(tring);
                                    bool IHaveThisSong = SongLengthsDict.TryGetValue(tring, out float hostsonglength);
                                    RainMeadow.Debug(IHaveThisSong);
                                    if (IHaveThisSong && hostsonglength != 0)
                                    {
                                        float hostsongprogress = ( lobbydottime - DJstartedat ) / hostsonglength;
                                        if ( hostsongprogress < 0.95f)
                                        {
                                            if (hostsongprogress < 0.05f) playfromstart = true; 
                                            RainMeadow.Debug("Meadow Music: Playing my DJs provided song");

                                            Song song = new(musicPlayer, myDJsdata.providedSong, MusicPlayer.MusicContext.StoryMode)
                                            {
                                                playWhenReady = true,
                                                volume = 1,
                                                fadeInTime = ivebeenpatientlywaiting && !playfromstart ? 1f : 120f
                                            };
                                            musicPlayer.song = song;
                                            RainMeadow.Debug(lobbydottime + " " + DJstartedat);
                                            dontskiptopoint = !ivebeenpatientlywaiting;
                                        }
                                        else
                                        {
                                            RainMeadow.Debug("Meadow Music: DJs soon done, i'll wait");
                                            ivebeenpatientlywaiting = true;
                                        }
                                    }
                                    else
                                    {
                                        RainMeadow.Debug("Meadow Music: I don't have my DJs provided song. Playing ambient song");
                                        
                                        if (shuffleindex + 1 >= shufflequeue.Length) { ShuffleSongs(); }
                                        else { shuffleindex++; }
                                        musicdata.providedSong = ambienceSongArray[shufflequeue[shuffleindex]];
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
                                    RainMeadow.Debug("Meadow Music: DJ isn't providing a song, i'll wait");
                                    ivebeenpatientlywaiting = true;
                                }
                            }
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
        
        static float LobbyTime()
        {
            //do some shit that sends back the current time of the lobby host
            return OnlineManager.lobby.owner.tick / OnlineManager.instance.framesPerSecond;
        }
        static void WorldLoadedPatch(On.OverWorld.orig_WorldLoaded orig, OverWorld self)
        {
            orig.Invoke(self);

            if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MeadowGameMode)
            {
                AnalyzeRegion(self.activeWorld);
            }
            UpdateIntensity = true;
        }

        static void OverWorld_LoadFirstWorld(On.OverWorld.orig_LoadFirstWorld orig, OverWorld self)
        {
            orig.Invoke(self);

            if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MeadowGameMode)
            {
                AnalyzeRegion(self.activeWorld);
            }
            UpdateIntensity = true;
        }
        static int closestVibe;
        static Room? RoomImIn;
        static int DegreesOfAwayness;
        static bool ItchingForKnowledge = false;
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
        static VirtualMicrophone? MyGuyMic;
        private static bool FadeThatShitOut;
        public static float playthissongat;
        private static float DJstartedat;
        private static bool dontskiptopoint;
        private static bool playfromstart;


        static void NewRoomPatch(On.VirtualMicrophone.orig_NewRoom orig, VirtualMicrophone self, Room room)
        {
            orig.Invoke(self, room);
            RainMeadow.Debug("The normal method is done");
            HelloNewRoom(self, room);
        }

        public static void HelloNewRoom(VirtualMicrophone self, Room room)
        {
            RainMeadow.Debug("New room is being checked"); 
            MyGuyMic = self;
            RoomImIn = room;

            if (OnlineManager.lobby == null || OnlineManager.lobby.gameMode is not MeadowGameMode) return;

            MusicPlayer musicPlayer = room.game.manager.musicPlayer;

            // If i don't know the activezones, do it immediately when you do know

            if (musicPlayer != null)
            {
                if (activeZonesDict == null)
                {
                    ItchingForKnowledge = true;
                    //BrushForSound = self;
                    //BrushForSpace = room;
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
                        //musicPlayer.song.FadeOut(40f);
                        //activeZone = null;
                        vibeIntensity = 0f;
                        if (musicPlayer.song != null) musicPlayer.song.baseVolume = 0.3f;
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
                if (ItchingForKnowledge)
                {
                    HelloNewRoom(MyGuyMic, RoomImIn); //Nah, those variables were set right before the itch started.
                    ItchingForKnowledge = false;
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