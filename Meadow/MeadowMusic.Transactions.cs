using RainMeadow.Generics;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    public partial class MeadowMusic
    {

        public class LobbyMusicData : OnlineResource.ResourceData
        {
            private byte nextGroupId = 1;
            public Dictionary<ushort, byte> playerGroups = new Dictionary<ushort, byte>();
            public Dictionary<byte, ushort> groupHosts = new Dictionary<byte, ushort>();

            public LobbyMusicData() : base()
            {

            }

            public override ResourceDataState MakeState(OnlineResource inResource)
            {
                return new State(this);
            }

            public byte NextGroupId()
            {
                while (playerGroups.ContainsValue(nextGroupId) || nextGroupId == 0)
                {
                    nextGroupId++;
                }
                return nextGroupId;
            }

            public void PlayerLeaveGroups(ushort player)
            {
                playerGroups[player] = 0;

                // look for groups owned by this loser
                foreach (var groupHost in groupHosts.ToList())
                {
                    if (groupHost.Value == player) // theirs
                    {
                        // someone else in to become the new host?
                        if (playerGroups.FirstOrDefault(v => v.Value == groupHost.Key) is KeyValuePair<ushort, byte> other and { Key: not 0, Value: not 0 })
                        {
                            groupHosts[groupHost.Key] = other.Key;
                        }
                        else
                        {
                            groupHosts.Remove(groupHost.Key);
                        }
                    }
                }
            }

            public class State : ResourceDataState
            {
                [OnlineField(nullable = true)]
                private UshortToByteDict playerGroups;
                [OnlineField(nullable = true)]
                private ByteToUshortDict groupHosts;

                public State() { }
                public State(LobbyMusicData lobbyMusicData)
                {
                    this.playerGroups = new Generics.UshortToByteDict(lobbyMusicData.playerGroups.ToList());
                    this.groupHosts = new Generics.ByteToUshortDict(lobbyMusicData.groupHosts.ToList());
                    //RainMeadow.Debug("I am writing: " + Json.Serialize(lobbyMusicData.playerGroups));
                }

                public override Type GetDataType() => typeof(LobbyMusicData);

                public override void ReadTo(OnlineResource.ResourceData data, OnlineResource resource)
                {
                    var lobbyMusicData = (LobbyMusicData)data;
                    lobbyMusicData.playerGroups = playerGroups.list.ToDictionary();
                    lobbyMusicData.groupHosts = groupHosts.list.ToDictionary();
                    //RainMeadow.Debug("I am reading: " + Json.Serialize(lobbyMusicData.playerGroups));
                }
            }
        }

        [RPCMethod]
        static void AskNowLeave(RPCEvent rpcEvent)
        {
            RainMeadow.Debug($"{rpcEvent.from} is asking to leave");
            if (!OnlineManager.lobby.isOwner) return;

            var lmd = OnlineManager.lobby.GetData<LobbyMusicData>();
            var who = rpcEvent.from.inLobbyId;
            lmd.PlayerLeaveGroups(who);
        }

        [RPCMethod]
        static void AskNowJoinPlayer(RPCEvent rpcEvent, OnlinePlayer other) //the server serving
        {
            var mgrr = OnlineManager.lobby.GetData<LobbyMusicData>();
            var from = rpcEvent.from;
            RainMeadow.Debug($"{from} is asking to join another Player named " + other);
            if (from == other) { RainMeadow.Error("they're the same you goof??"); return; }
            
            var groupA = mgrr.playerGroups[from.inLobbyId];
            var groupB = mgrr.playerGroups[other.inLobbyId];

            if (groupA == 0 && groupB == 0)
            {
                RainMeadow.Debug("new group for both");
                var newId = mgrr.NextGroupId();
                RainMeadow.Debug("newid: " + newId);
                mgrr.playerGroups[from.inLobbyId] = newId;
                mgrr.playerGroups[other.inLobbyId] = newId;
                mgrr.groupHosts[newId] = from.inLobbyId;
            }
            else if (groupA == 0)
            {
                RainMeadow.Debug("A joined B's: " + groupB);
                mgrr.playerGroups[from.inLobbyId] = groupB;
            } 
            else if (groupB == 0)
            {
                RainMeadow.Debug("B joined A's: " + groupA);
                mgrr.playerGroups[other.inLobbyId] = groupA;
            }
            else if(groupA != groupB)
            {
                RainMeadow.Debug($"A left own group ({groupA}) and joined B's: " + groupB);
                // A leaves to join B
                mgrr.PlayerLeaveGroups(from.inLobbyId);
                mgrr.playerGroups[from.inLobbyId] = groupB;
            }
            else
            {
                // already in same group
                RainMeadow.Debug("already in the same group");
            }
        }

        [RPCMethod]
        static void AskNowSquashPlayers(RPCEvent rpcEvent, ushort[] playersinquestion)
        {
            RainMeadow.Debug("A player is asking to squash an array of folks together");
            
            //make unique ID and feed it to all the people
            var lmd = OnlineManager.lobby.GetData<LobbyMusicData>();
            var newid = lmd.NextGroupId();
            RainMeadow.Debug("newid: " + newid);
            for (int j = 0; j < playersinquestion.Length; j++)
            {
                RainMeadow.Debug("player: " + playersinquestion[j]);
                lmd.PlayerLeaveGroups(playersinquestion[j]);
                lmd.playerGroups[playersinquestion[j]] = newid;
            }
            RainMeadow.Debug("host: " + playersinquestion[0]);
            lmd.groupHosts[newid] = playersinquestion[0];
        }
    }
}