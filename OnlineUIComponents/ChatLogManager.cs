using System;
using System.Collections.Generic;
using System.Linq;

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
            if (subscribers.Any(s => !s.Active)) subscribers = subscribers.Where(s => s.Active).ToList();
            subscribers.ForEach(chatHud => chatHud.AddMessage(user, message));
        }
        public static void LogSystemMessage(string message)
        {
            if (subscribers.Any(s => !s.Active)) subscribers = subscribers.Where(s => s.Active).ToList();
            subscribers.ForEach(chatHud => chatHud.AddMessage("", message));
        }
    }
}
