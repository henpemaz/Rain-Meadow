using System;
using System.Collections.Generic;
using System.Linq;

namespace RainMeadow
{
    public class ChatLogManager
    {
        // HACK: put this somewhere better
        public static bool shownChatTutorial = false;

        private static List<IChatSubscriber> subscribers = new();

        public static void Subscribe(IChatSubscriber e) => subscribers.Add(e);
        public static void Unsubscribe(IChatSubscriber e) => subscribers.Remove(e);

        public static void LogMessage(string user, string message)
        {
            if (subscribers.Any(s => !s.Active)) subscribers = subscribers.Where(s => s.Active).ToList();
            subscribers.ForEach(e => e.AddMessage(user, message));
        }
        public static void LogSystemMessage(string message)
        {
            if (subscribers.Any(s => !s.Active)) subscribers = subscribers.Where(s => s.Active).ToList();
            subscribers.ForEach(e => e.AddMessage("", message));
        }
    }
}
