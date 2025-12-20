using System.Collections.Generic;
using DevInterface;
using HUD;

namespace RainMeadow
{
    public class MeadowCoinHUD : HudPart
    {
        private RoomCamera camera;
        private readonly OnlineGameMode onlineGameMode;
        private Page page;

        FSprite coinSprite;
        List<FLabel> coinLabels; // main display and the +1 


        public MeadowCoinHUD(HUD.HUD hud, RoomCamera camera, OnlineGameMode onlineGameMode) : base(hud)
        {
            this.camera = camera;
            this.onlineGameMode = onlineGameMode;

            // coinSprite = new FSprite("meadowCoin");
            // camera.ReturnFContainer("HUD").AddChild(coinSprite);

        }





        public override void Update()
        {




            base.Update();
        }

        public override void ClearSprites()
        {
            base.ClearSprites();

        }

        public override void Draw(float timeStacker)
        {


            base.Draw(timeStacker);
        }
    }
}