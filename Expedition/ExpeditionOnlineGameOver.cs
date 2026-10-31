using Menu.Remix;
using Menu.Remix.MixedUI;

namespace RainMeadow
{
    internal class ExpeditionOnlineGameOver : Menu.ExpeditionGameOver
    {
        Menu.HoldButton retryButton;
        public ExpeditionOnlineGameOver(ProcessManager manager) : base(manager)
        {
            RainMeadow.Debug("ExpeditionOnlineGameOver");
            retryButton = new Menu.HoldButton(this, pages[0], Translate("RETRY<LINE>EXPEDITION").Replace("<LINE>", "\n"), "RETRY", new UnityEngine.Vector2(leftAnchor + 270f, 170f), 50f);
            pages[0].subObjects.Add(retryButton);
            menuTabWrapper = new MenuTabWrapper(this, pages[0]);
            pages[0].subObjects.Add(menuTabWrapper);
            if (OnlineManager.lobby.isOwner)
            {                
                abandonButton = new OpHoldButton(retryButton.pos + new UnityEngine.Vector2(-55f, -120f), new UnityEngine.Vector2(110f, 30f), Translate("ABANDON"), 90f);
                abandonButton.colorEdge = new UnityEngine.Color(0.6f, 0f, 0f);
            }
            else 
            {
                abandonButton = new OpHoldButton(retryButton.pos + new UnityEngine.Vector2(-55f, -120f), new UnityEngine.Vector2(110f, 30f), Translate("TO LOBBY"), 90f);
            }
            abandonButton.OnPressDone += AbandonButton_OnBackToLobbyDone;
            abandonButton.description = " ";
            new Menu.Remix.UIelementWrapper(menuTabWrapper, abandonButton);            
        }

        private void AbandonButton_OnBackToLobbyDone(UIfocusable trigger)
        {
            manager.RequestMainProcessSwitch(RainMeadow.Ext_ProcessID.ExpeditionMenu);
        }
        public override void Update()
        {

            if (RainMeadow.isExpeditionMode(out var expedition))
            {
                if (!OnlineManager.lobby.isOwner)
                    retryButton.buttonBehav.greyedOut = !expedition.canJoinGame;
            }

            base.Update();
        }
    }
}
