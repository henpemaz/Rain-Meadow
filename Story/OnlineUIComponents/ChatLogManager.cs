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
            string[] parts = message.Split(new[] { ':' }, 2);
            string username = parts[0].Trim(); // First part is the username
            string chatMessage = parts[1].Trim(); // Second part is the message
            chatLogs.Add(username, chatMessage);
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
