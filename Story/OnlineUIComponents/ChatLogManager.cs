namespace RainMeadow
{
    public class ChatLogManager
    {
        private static ChatHud chatHud;

        public static void Initialize(ChatHud hud)
        {
            chatHud = hud;
        }
        public static void LogMessage(string message)
        {
            chatHud.AddMessage(message);
            RainMeadow.Error("Success sending message dude");
        }
    }
}
