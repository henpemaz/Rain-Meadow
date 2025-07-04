using Menu;
using RainMeadow.Arena.ArenaOnlineGameModes.TeamBattle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RainMeadow.UI.Components
{
    public class TeamBattlePlayerBox : PositionedMenuObject
    {
        public Color teamColor;
        public FSprite teamSymbol;
        public ArenaPlayerBox? PlayerBox => owner as ArenaPlayerBox;
        public TeamBattlePlayerBox(Menu.Menu menu, MenuObject owner, Vector2 pos, string teamSymbolName) : base(menu, owner, pos)
        {
            teamSymbol = new(teamSymbolName);
            Container.AddChild(teamSymbol);
        }
        public override void RemoveSprites()
        {
            if (PlayerBox != null) PlayerBox.desiredSlugcatButtonSecondaryColor = null;
            teamSymbol.RemoveFromContainer();
            base.RemoveSprites();
        }
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            teamSymbol.color = teamColor;
            if (PlayerBox != null)
            {
                Vector2 pos = PlayerBox.infoKickButton != null? PlayerBox.infoKickButton.DrawPos(timeStacker) : PlayerBox.colorInfoButton.DrawPos(timeStacker);
                teamSymbol.x = pos.x + 55;
                teamSymbol.y = pos.y + 15;
            }
        }
        public override void Update()
        {
            base.Update();
            if (PlayerBox != null) PlayerBox.desiredSlugcatButtonSecondaryColor = teamColor;
        }
    }
}
