using HUD;
using UnityEngine;

namespace RainMeadow
{
    public class ChatHud : HudPart
    {

        private RoomCamera camera;
        private readonly OnlineGameMode onlineGameMode;
        private Menu.Menu chatBox;
        private RainWorldGame game;
        private bool chatActive = false;


        public ChatHud(HUD.HUD hud, RoomCamera camera, OnlineGameMode onlineGameMode) : base(hud)
        {
            this.camera = camera;
            this.onlineGameMode = onlineGameMode;
            game = camera.game;
        }

        public override void Draw(float timeStacker)
        {
            base.Draw(timeStacker);
            if (Input.GetKeyDown(RainMeadow.rainMeadowOptions.ChatKey.Value) && chatBox == null && chatActive == false)
            {
                RainMeadow.Debug("creating chat box");
                chatBox = new ChatOverlay(game.manager, game);
                chatActive = true;
            }

            chatBox?.GrafUpdate(timeStacker);

            if (Input.GetKeyDown(KeyCode.Return) && chatBox != null) ShutDownChat();
        }
        public void ShutDownChat()
        {
            RainMeadow.Debug("shut down chat box");
            chatBox.ShutDownProcess();
            chatBox = null;
            chatActive = false;
        }
        public override void Update()
        {
            base.Update();
            if (OnlineManager.lobby.gameMode is StoryGameMode)
            {
                if ((game.pauseMenu != null || camera.hud.map.visible || game.manager.upcomingProcess != null) && chatBox != null) ShutDownChat();

                chatBox?.Update();
            }
        }
    }
}