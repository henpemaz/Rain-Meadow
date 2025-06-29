using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    public class ChatLogManager
    {
        // HACK: put this somewhere better
        public static bool shownChatTutorial = false;
        // Shared dictionary for chats, reset each time a new lobby is entered
        private static Dictionary<string, UnityEngine.Color> colorDict = new();
        // UI, colour of system messages
        public static UnityEngine.Color defaultSystemColor = new(1f, 1f, 0.3333333f);
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

        /// <summary>
        /// Resets the cache of saved colors for each player
        /// </summary>
        public static void ResetPlayerColors()
        {
            colorDict.Clear();
        }

        /// <summary>
        /// Queries the player colours and populates the given dictionary
        /// </summary>
        public static void UpdatePlayerColors()
        {
            foreach (var playerAvatar in OnlineManager.lobby.playerAvatars.Select(kv => kv.Value))
            {
                if (playerAvatar.FindEntity(true) is OnlinePhysicalObject opo)
                {
                    if (!colorDict.ContainsKey(opo.owner.id.name) && opo.TryGetData<SlugcatCustomization>(out var customization))
                    {
                        colorDict.Add(opo.owner.id.name, customization.bodyColor);
                    }
                }
            }
        }

        /// <summary>
        /// Obtains a player's color from it's name, with HSV adjustment
        /// </summary>
        public static UnityEngine.Color GetDisplayPlayerColor(string s, UnityEngine.Color colorIfNotFound = default)
        {
            float H = 0f;
            float S = 0f;
            float V = 0f;
            if (colorDict.TryGetValue(s, out var colorOrig))
            {
                UnityEngine.Color.RGBToHSV(colorOrig, out H, out S, out V);
                if (V < 0.8f) return UnityEngine.Color.HSVToRGB(H, S, 0.8f);
                return colorOrig;
            }
            return colorIfNotFound == default? Color.white : colorIfNotFound;
        }
    }

    public interface IChatSubscriber
    {
        public bool Active{ get; }
        public void AddMessage(string user, string text);
    }
}
