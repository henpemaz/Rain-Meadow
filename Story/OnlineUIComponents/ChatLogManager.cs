using System.Collections.Generic;

namespace RainMeadow
{
    public class ChatLogManager
    {
        private static ChatHud chatHud;
        public static Dictionary<string, string> chatLogs = new Dictionary<string, string>();


        public static void Initialize(ChatHud hud)
        {
            chatHud = hud;
        }
        public static void LogMessage(string message)
        {
            if (chatHud != null)
            {
                chatHud.AddMessage(message);
            } else
            {
                RainMeadow.Error("Chat HUD not initialized yet");
            }
        }
    }
}
