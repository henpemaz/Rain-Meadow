using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Drown;
using Menu;
using MoreSlugcats;
using RainMeadow.Arena.ArenaOnlineGameModes.ArenaChallengeModeNS;
using RainMeadow.Arena.ArenaOnlineGameModes.TeamBattle;
using RWCustom;
using UnityEngine;

namespace RainMeadow;

public struct ArenaPersistentStats(int wins, int death, int totscore, List<IconSymbol.IconSymbolData> allkills)
{
    public int wins = wins;
    public int death = death;
    public int totscore = totscore;
    public List<IconSymbol.IconSymbolData> allkills = allkills;
}