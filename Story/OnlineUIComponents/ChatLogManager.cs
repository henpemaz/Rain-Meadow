using System;
using System.Collections.Generic;

namespace RainMeadow
{
    public class ChatLogManager
    {
        // HACK: put this somewhere better
        public static bool shownChatTutorial = false;

        private static List<ChatHud> subscribers = new();

        public static void Subscribe(ChatHud chatHud) => subscribers.Add(chatHud);

        public static void Unsubscribe(ChatHud chatHud) => subscribers.Remove(chatHud);

        public static void LogMessage(string user, string message)
        {
            subscribers.ForEach(chatHud => chatHud.AddMessage(user, message));
        }
    }
}
