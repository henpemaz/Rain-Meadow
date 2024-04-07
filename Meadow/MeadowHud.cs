using HUD;
using UnityEngine;

namespace RainMeadow
{
    internal class MeadowHud : HudPart
    {
        private RoomCamera self;
        private Creature owner;

        public MeadowHud(HUD.HUD hud, RoomCamera self, Creature owner) : base(hud)
        {
            this.hud = hud;
            this.self = self;
            this.owner = owner;
        }

        public override void Update()
        {

        }

        public override void Draw(float timeStacker)
        {
            base.Draw(timeStacker);
        }

        public override void ClearSprites()
        {
            base.ClearSprites();
        }

        public class TokenSparkIcon
        {
            public TokenSparkIcon(Color color)
            {

            }

            void Update()
            {

            }

            void Draw(float timeStacker)
            {

            }

            void ClearSprites()
            {

            }
        }
    }
}