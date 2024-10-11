using System.Collections.Generic;
using System.Linq;

namespace RainMeadow
{
    public partial class MeadowMusic
    {
        [RPCMethod]
        static void AskNowLeave(RPCEvent rpcEvent, ushort meadowLobbyPlayerId) //could maybe have "i'm a host" be a parameter but perspectiveeeee
        {
            RainMeadow.Debug("A player is asking to leave");
            int? HostOf = null;
            var myoc = OnlineManager.lobby.playerAvatars[OnlineManager.lobby.PlayerFromId(meadowLobbyPlayerId)]?.FindEntity();

            MeadowMusicData mydata = myoc.GetData<MeadowMusicData>();
            if (mydata.isDJ) { HostOf = mydata.inGroup; }
            rpcEvent.from.InvokeRPC(TellNowJoinPlayer, -1, true);

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
            if (entityid.FindEntity() is not OnlineCreature JoingingThisGuy) return; //im scared
            int? newgroup = null;
            bool StartUnique = false;

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
                    while (newgroup == null)
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
            RainMeadow.Debug($"I will join group {newgroup} as a {(isdj ? "DJ" : "player")}");
            var mgm = OnlineManager.lobby.gameMode as MeadowGameMode; // could be *not* meadowgamemode, if so break everything
            var creature = mgm.avatar;
            var musicdata = creature.GetData<MeadowMusicData>();
            int oldgroup = musicdata.inGroup;
            if (newgroup != -1 && oldgroup == -1 && !isdj)
            {
                IntegrationToNewGroup = true;
            }
            musicdata.isDJ = isdj;
            musicdata.inGroup = newgroup;
        }
        //[RPCMethod]
        //static void TellNowCheckRoom() //Call When somebody updates their Group or DJ
        //{
        //    TheThingTHatsCalledWhenPlayersUpdated();
        //}
        public static void TheThingTHatsCalledWhenPlayersUpdated()
        {
            var mgm = OnlineManager.lobby.gameMode as MeadowGameMode;
            var creature = mgm.avatar;
            var musicdata = creature.GetData<MeadowMusicData>();
            var RoomImIn = creature.creature.Room.realizedRoom;
            if (RoomImIn == null || creature.roomSession == null) return;

            RainMeadow.Debug("Checking Players");

            if (musicdata.inGroup == -1)
            {
                if (creature.roomSession.activeEntities.Any(
                    e=>e is OnlineCreature && !e.isMine // someone elses
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

                List<int> IDsWithMe = new List<int>();
                foreach (var other in OnlineManager.lobby.playerAvatars.Values.Where(v => v != null))
                {
                    if (other.FindEntity() is OnlineCreature oc)
                    {
                        if (oc != null && oc.realizedCreature != null)
                        {
                            if (oc.realizedCreature.room == RoomImIn)
                            {
                                IDsWithMe.Add(oc.GetData<MeadowMusicData>().inGroup);
                            }
                        }
                    }
                }//can be optimised aka don't need to check this cuz i already know from what made me do this

                bool IAmWithMyFriends = IDsWithMe.Count(v => v == musicdata.inGroup) > 1;
                //if (vibeRoom == null) return -1;
                if (!IAmWithMyFriends)
                {
                    RainMeadow.Debug("No dice, checks one degree of seperation for anyone, Room creature is in: " + creature.abstractCreature.Room.name);
                    List<int> GangNextDoor = new List<int>();

                    RainMeadow.Debug("And it thinks that my connections are " + Newtonsoft.Json.JsonConvert.SerializeObject(creature.abstractCreature.Room.connections));
                    foreach (int connection in creature.abstractCreature.Room.connections)
                    {
                        //var game = vibeRoom.connections[i];
                        RainMeadow.Debug("Pointing towards connection: " + connection);
                        if (connection != -1)
                        {
                            AbstractRoom abstractRoom = creature.abstractCreature.Room.world.GetAbstractRoom(connection); //ok so this says that there's no people because the people haven't joined the new resource yet so they just don't exist
                            RainMeadow.Debug("My neighbor " + abstractRoom.name); //this is having an error because it's saying one of the connections is -1

                            if (abstractRoom != null) //worry more about how connection can be -1 than an abstractroom being null.  
                            {
                                if (abstractRoom.creatures.Count() != 0)
                                {
                                    RainMeadow.Debug("Hey this room has creatures in it");
                                    foreach (var entity in abstractRoom.creatures)
                                    {
                                        RainMeadow.Debug(entity);

                                        foreach (var other in OnlineManager.lobby.playerAvatars.Values.Where(v => v != null))
                                        {
                                            if (other.FindEntity() is OnlineCreature oc)
                                            {
                                                if (oc.creature == entity)
                                                {
                                                    GangNextDoor.Add(oc.GetData<MeadowMusicData>().inGroup);
                                                    RainMeadow.Debug("Yeah i can find this guy's thingy");
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    IAmWithMyFriends = GangNextDoor.Count(v => v == musicdata.inGroup) != 0;
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
                    if (!musicdata.isDJ)
                    {
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
                    }

                    //if mydj is in same region as me
                    if (!djinsameregion)
                    {
                        if (demiseTimer == null) { demiseTimer = 12.5f; RainMeadow.Debug("Started Demisetimer due to not being in the same region as DJ"); };
                    }
                    else
                    {
                        //check the amount 

                        List<int> IDs = IDsWithMe.ToList();
                        IDs.RemoveAll(v => v == -1);
                        var g = IDs.GroupBy(v => v);
                        var result = g.OrderByDescending(v => v).ToList();
                        if (result.Count > 1)
                        {//dramaaa~
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
                                    if (demiseTimer != null)
                                    {

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
    }
}