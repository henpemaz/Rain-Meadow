using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    public class ChatLogManager
    {
        public const char SystemMessageSign = '\r'; // I need an untypable character for this

        /// <remarks>
        /// Each value corresponds to the <see langword="char"/>
        /// prefixed to the chat 'user'.
        /// </remarks>
        public enum SystemMessageType : ushort
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

        public delegate void MessageLoggedEventHandler(string user, string message);
        public static event MessageLoggedEventHandler? MessageLogged;

        // HACK: put this somewhere better
        public static bool shownChatTutorial = false;
        public static bool logErrorsInChat = false;
        // Shared dictionary for chats, reset each time a new lobby is entered
        public static List<(string User, string Message)> chatLog = [];

        private static Dictionary<string, Color> colorDict = [];

        public static Color defaultSystemColor = new(1f, 1f, 0.3333333f);
        public static Color orangeSystemColor = new(1f, 0.55f, 0.25f);
        public static Color redSystemColor = new(1f, 0.35f, 0.35f);

        public static readonly Dictionary<SystemMessageType, Color> SystemMessageTypeColor = new()
        {
            [SystemMessageType.System] = defaultSystemColor,
            [SystemMessageType.PlayerJoin] = defaultSystemColor,
            [SystemMessageType.CreatureDeath] = defaultSystemColor,
            [SystemMessageType.PlayerJoinFail] = Color.Lerp(defaultSystemColor, Color.black, 0.5f),

            [SystemMessageType.StartOfRound] = Color.Lerp(orangeSystemColor, Color.black, 0.25f),
            [SystemMessageType.EndOfRound] = Color.Lerp(orangeSystemColor, Color.black, 0.25f),
            [SystemMessageType.EndOfSession] = orangeSystemColor,

            [SystemMessageType.LogNotify] = redSystemColor,
            [SystemMessageType.LogError] = Color.Lerp(redSystemColor, Color.black, 0.25f)
        };

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

        public static void AddMessageToChatLog((string User, string Message) userMessagePair)
            => AddMessageToChatLog(userMessagePair.User, userMessagePair.Message);

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

        public static void LogMessage(string user, string message)
        {
            AddMessageToChatLog(user, message);
            MessageLogged?.Invoke(user, message);
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
            if (systemMessageType is SystemMessageType.PlayerJoin or SystemMessageType.PlayerJoinFail
                && (RainMeadow.isArenaMode(out _)
                    ? !RainMeadow.rainMeadowOptions.EnableChatArenaJoinNotification.Value
                    : RainMeadow.isStoryMode(out _)
                        ? !RainMeadow.rainMeadowOptions.EnableChatStoryJoinNotification.Value
                        : true))
                    return;
            if (systemMessageType == SystemMessageType.EndOfSession
                && !RainMeadow.rainMeadowOptions.EnableChatSessionNotification.Value)
                    return;
            if (systemMessageType is SystemMessageType.EndOfRound or SystemMessageType.StartOfRound
                && !RainMeadow.rainMeadowOptions.EnableChatRoundNotification.Value)
                    return;

            string signature = TypeToSysMesSignature(systemMessageType);
            AddMessageToChatLog(signature, message);
            MessageLogged?.Invoke(signature, message);
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
            if (OnlineManager.lobby == null) return; // no lobby to query, keep the colors we already have

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
        public static Color GetDisplayPlayerColor(string playerName, Color colorIfNotFound = default)
        {
            float H = 0f;
            float S = 0f;
            float V = 0f;
            if (colorDict.TryGetValue(playerName, out var colorOrig))
            {
                Color.RGBToHSV(colorOrig, out H, out S, out V);
                if (V < 0.8f) return Color.HSVToRGB(H, S, 0.8f);
                return colorOrig;
            }
            return colorIfNotFound == default(Color) ? Color.white : colorIfNotFound;
        }

        public static Color GetColorOfSystemMessage(SystemMessageType systemMessageType)
            => SystemMessageTypeColor.TryGetValue(systemMessageType, out var color) ? color : defaultSystemColor;

        public static Color GetColorOfSystemMessage(SystemMessageType? systemMessageType)
            => systemMessageType is SystemMessageType type ? GetColorOfSystemMessage(type) : defaultSystemColor;

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
}
