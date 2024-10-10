using System;
using System.Linq.Expressions;
using HUD;
using UnityEngine;

namespace RainMeadow
{
    public class ChatHud : HudPart
    {
        private readonly OnlineGameMode onlineGameMode;
        private Menu.Menu ChatBox;
        public ChatHud(HUD.HUD hud, OnlineGameMode onlineGameMode) : base(hud)
        {
            this.onlineGameMode = onlineGameMode;
        }

        public override void Draw(float timeStacker)
        {
            base.Draw(timeStacker);

            try
            {
                if (Input.GetKeyDown(RainMeadow.rainMeadowOptions.ChatKey.Value))
                {
                    RainMeadow.Debug("Test of trying to get the damn chat feature working");

                }
            }
            catch (Exception e) {
                RainMeadow.Error(e);
            }
        }
    }
}
