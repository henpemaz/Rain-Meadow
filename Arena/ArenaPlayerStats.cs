using System.Collections.Generic;
using System.Linq;

namespace RainMeadow
{
    /// <summary>
    /// A single player's arena stats, keyed by <see cref="OnlinePlayer.inLobbyId"/>.
    /// <para>Serialized as one entry of <see cref="ArenaLobbyData.State.playerStats"/>,
    /// so the player key is only sent once for all of their stats.</para>
    /// </summary>
    public class ArenaPlayerStats : Serializer.ICustomSerializable
    {
        public ushort inLobbyId;
        public int wins;
        public int deaths;
        public int roundDeaths;
        public int totalScore;
        public int score;
        public List<IconSymbol.IconSymbolData> allKills = [];
        public List<IconSymbol.IconSymbolData> roundKills = [];

public ushort ID => inLobbyId;

        public ArenaPlayerStats() { }

        /// <summary>
        /// Reads a player's stats out of <see cref="ArenaOnlineGameMode"/>'s dictionaries.
        /// </summary>
        /// <remarks>
        /// <see cref="ArenaOnlineGameMode.AddMissingStatEntries"/> is called before
        /// accessing dictionaries. No exceptions will be thrown.
        /// </remarks>
        public ArenaPlayerStats(ArenaOnlineGameMode arenaOnline, OnlinePlayer player)
        {
            arenaOnline.AddMissingStatEntries(player);

            inLobbyId   = player.inLobbyId;
            wins        = arenaOnline.WinsByOPlayer[player];
            deaths      = arenaOnline.DeathsByOPlayer[player];
            roundDeaths = arenaOnline.RoundDeathsByOPlayer[player];
            totalScore  = arenaOnline.TotalScoreByOPlayer[player];
            score       = arenaOnline.ScoreByOPlayer[player];
            allKills    = arenaOnline.AllKillsByOPlayer[player].ToList();
            roundKills  = arenaOnline.RoundKillsByOPlayer[player].ToList();
        }

        /// <summary>
        /// Writes these stats into <see cref="ArenaOnlineGameMode"/>'s
        /// dictionaries under <paramref name="player"/>.
        /// </summary>
        public void CopyTo(ArenaOnlineGameMode arenaOnline, OnlinePlayer player)
        {
            arenaOnline.WinsByOPlayer[player]        = wins;
            arenaOnline.DeathsByOPlayer[player]      = deaths;
            arenaOnline.RoundDeathsByOPlayer[player] = roundDeaths;
            arenaOnline.TotalScoreByOPlayer[player]  = totalScore;
            arenaOnline.ScoreByOPlayer[player]       = score;
            arenaOnline.AllKillsByOPlayer[player]    = allKills.ToList();
            arenaOnline.RoundKillsByOPlayer[player]  = roundKills.ToList();
        }

        public void CustomSerialize(Serializer serializer)
        {
            serializer.Serialize(ref inLobbyId);
            serializer.Serialize(ref wins);
            serializer.Serialize(ref deaths);
            serializer.Serialize(ref roundDeaths);
            serializer.Serialize(ref totalScore);
            serializer.Serialize(ref score);
            SerializeTrophies(serializer, ref allKills);
            SerializeTrophies(serializer, ref roundKills);
        }

        // IconSymbolData has no serializer of its own, it travels as its string form
        private static void SerializeTrophies(Serializer serializer, ref List<IconSymbol.IconSymbolData> trophies)
        {
            List<string> trophiesAsStrings = serializer.IsWriting
                ? trophies.Select(trophy => trophy.ToString()).ToList()
                : null;

            serializer.Serialize(ref trophiesAsStrings);

            if (serializer.IsReading)
            {
                trophies = trophiesAsStrings
                    .Select(IconSymbol.IconSymbolData.IconSymbolDataFromString)
                    .ToList();
            }
        }
    }
}
