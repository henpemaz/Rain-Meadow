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
        private static Dictionary<string, Color> colorDict = new();
        // UI, colour of system messages
        public static Color defaultSystemColor = new(1f, 1f, 0.3333333f);
        public static Color orangeSystemColor = new(1f, 0.55f, 0.25f);
        public static Color redSystemColor = new(1f, 0.35f, 0.35f);
        private static List<IChatSubscriber> subscribers = new();
        public static List<(string, string)> chatLog = new();
        public static bool logErrorsInChat = false;
        
        public static bool ShouldMuteMessageFromUser(string user)
            => !IsUserSystemSignature(user)
                && (RainMeadow.rainMeadowOptions.GlobalMute.Value
                    || OnlineManager.lobby?.gameMode?.mutedPlayers?.Contains(user) is true);
        public static bool ShouldPingFromMessage(string user, string message)
            => RainMeadow.rainMeadowOptions.ChatPing.Value 
                && !IsUserSystemSignature(user)
                && user != OnlineManager.mePlayer.id.GetPersonaName() 
                && !string.IsNullOrEmpty(message)
                && message.IndexOf(OnlineManager.mePlayer.id.DisplayName, StringComparison.OrdinalIgnoreCase) >= 0;
        public static bool ShouldMakeSoundFromMessage(string user, string message, out bool quiet)
        {
            quiet = !IsUserSystemSignature(user);
            return RainMeadow.rainMeadowOptions.ChatSound.Value 
                && user != OnlineManager.mePlayer.id.GetPersonaName() 
                && !string.IsNullOrEmpty(message)
                && !ShouldPingFromMessage(user, message);
        }
                
        public static void ClearChatLog() 
        { 
            chatLog.Clear(); 
            RainMeadow.Debug("Chat log cleared"); 
        }
        public static void AddMessageToChatLog((string, string) userMessagePair) 
            => AddMessageToChatLog(userMessagePair.Item1, userMessagePair.Item2);
        public static void AddMessageToChatLog(string user, string message) 
        {
            if (!ShouldMuteMessageFromUser(user))
            {
                chatLog.Add((user, message));
                // RainMeadow.Debug($"Adding message in log from {user} : {message}"); 
            }
        }
        public static void ToggleLogErrorInChat() 
        { 
            logErrorsInChat = !logErrorsInChat;
            if (logErrorsInChat)
            {
                LogSystemMessage(Utils.Translate("Enabled Error Logging in chat."), SystemMessageType.LogNotify);
                // RainMeadow.Error("Hi I'm an Error :D"); 
            }
            else
            {
                LogSystemMessage(Utils.Translate("Disabled Error Logging in chat."), SystemMessageType.LogNotify);
            }
        }

        public static void Subscribe(IChatSubscriber e) => subscribers.Add(e);
        public static void Unsubscribe(IChatSubscriber e) => subscribers.Remove(e);

        public static void LogMessage(string user, string message)
        {
            if (subscribers.Any(s => !s.Active)) subscribers = subscribers.Where(s => s.Active).ToList();
            AddMessageToChatLog(user, message);
            subscribers.ForEach(e => e.AddMessage(user, message));
        }
        public static void LogSystemMessage(string message, SystemMessageType systemMessageType = SystemMessageType.System)
        {
            if (systemMessageType == SystemMessageType.CreatureDeath 
                && (RainMeadow.isArenaMode(out _) 
                    ? !RainMeadow.rainMeadowOptions.EnableChatArenaDeathNotification.Value
                    : RainMeadow.isStoryMode(out _) 
                        ? !RainMeadow.rainMeadowOptions.EnableChatStoryDeathNotification.Value
                        : true))
                    return;
            if ((systemMessageType == SystemMessageType.PlayerJoin || systemMessageType == SystemMessageType.PlayerJoinFail) 
                && (RainMeadow.isArenaMode(out _) 
                    ? !RainMeadow.rainMeadowOptions.EnableChatArenaJoinNotification.Value
                    : RainMeadow.isStoryMode(out _) 
                        ? !RainMeadow.rainMeadowOptions.EnableChatStoryJoinNotification.Value
                        : true))
                    return;
            if (systemMessageType == SystemMessageType.EndOfSession 
                && !RainMeadow.rainMeadowOptions.EnableChatSessionNotification.Value) 
                    return;
            if ((systemMessageType == SystemMessageType.EndOfRound || systemMessageType == SystemMessageType.StartOfRound) 
                && !RainMeadow.rainMeadowOptions.EnableChatRoundNotification.Value) 
                    return;
            
            if (subscribers.Any(s => !s.Active)) subscribers = subscribers.Where(s => s.Active).ToList();
            string signature = TypeToSysMesSignature(systemMessageType);
            AddMessageToChatLog(signature, message);
            subscribers.ForEach(e => e.AddMessage(signature, message));
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
            foreach (OnlinePlayer onlinePlayer in OnlineManager.lobby.participants)
            {
                if (OnlineManager.lobby.clientSettings.TryGetValue(onlinePlayer, out var cs) && cs.chatUsernameColor is Color color)
                {
                    colorDict[onlinePlayer.id.DisplayName] = color;
                }
                else if (OnlineManager.lobby.playerAvatars.Exists(kv => kv.Key == onlinePlayer)
                    && OnlineManager.lobby.playerAvatars.First(kv => kv.Key == onlinePlayer).Value?.FindEntity(true) is OnlinePhysicalObject opo)
                {
                    // If we successfully get the customization data, upsert
                    if (opo.TryGetData<SlugcatCustomization>(out var customization))
                        colorDict[onlinePlayer.id.DisplayName] = customization.bodyColor;
                }
            }
        }

        /// <summary>
        /// Obtains a player's color from it's name, with HSV adjustment
        /// </summary>
        public static UnityEngine.Color GetDisplayPlayerColor(string playerName, UnityEngine.Color colorIfNotFound = default)
        {
            float H = 0f;
            float S = 0f;
            float V = 0f;
            if (colorDict.TryGetValue(playerName, out var colorOrig))
            {
                UnityEngine.Color.RGBToHSV(colorOrig, out H, out S, out V);
                if (V < 0.8f) return UnityEngine.Color.HSVToRGB(H, S, 0.8f);
                return colorOrig;
            }
            return colorIfNotFound == default ? Color.white : colorIfNotFound;
        }

        public enum SystemMessageType
        {
            System = 'y',
            LogError = 'e',
            LogNotify = 'n',
            PlayerJoin = 'j',
            PlayerJoinFail = 'J',
            CreatureDeath = 'd',
            EndOfRound = 'R',
            StartOfRound = 'r',
            EndOfSession = 'S',
        }
        public static readonly Dictionary<SystemMessageType, Color> SystemMessageTypeColor = new(){
            [SystemMessageType.System] = defaultSystemColor,
            [SystemMessageType.PlayerJoin] = defaultSystemColor,   
            [SystemMessageType.CreatureDeath] = defaultSystemColor,
            [SystemMessageType.PlayerJoinFail] = Color.Lerp(defaultSystemColor, Color.black, 0.5f),
            
            [SystemMessageType.StartOfRound] = Color.Lerp(orangeSystemColor, Color.black, 0.25f),
            [SystemMessageType.EndOfRound] = Color.Lerp(orangeSystemColor, Color.black, 0.25f),
            [SystemMessageType.EndOfSession] = orangeSystemColor,

            [SystemMessageType.LogNotify] = redSystemColor,
            [SystemMessageType.LogError] = Color.Lerp(redSystemColor, Color.black, 0.25f),
        };
        public static Color GetColorOfSystemMessage(SystemMessageType systemMessageType)
            => SystemMessageTypeColor.TryGetValue(systemMessageType, out var color) ? color : defaultSystemColor;
        public static Color GetColorOfSystemMessage(SystemMessageType? systemMessageType)
            => systemMessageType is SystemMessageType type ? GetColorOfSystemMessage(type) : defaultSystemColor;
        public const char SystemMessageSign = '\r'; // I need an untypable character for this
        public static SystemMessageType? SysMesSignatureToType(string signature)
        {
            if (string.IsNullOrEmpty(signature)) return SystemMessageType.System;
            if (signature.Length == 2 && signature[0] == SystemMessageSign)
            {
                return (SystemMessageType)signature[1];
            }
            return null;
        }
        public static string TypeToSysMesSignature(SystemMessageType systemMessageType)
            => SystemMessageSign.ToString() + ((char)systemMessageType).ToString();
        public static bool IsUserSystemSignature(string user)
            => SysMesSignatureToType(user) is not null;
    }

    public interface IChatSubscriber
    {
        public bool Active { get; }
        public void AddMessage(string user, string text);
    }
}
