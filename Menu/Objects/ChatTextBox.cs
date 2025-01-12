using Menu;
using UnityEngine;
using System;
using Menu.Remix.MixedUI;
using System.Collections;
using MonoMod.RuntimeDetour;
using System.Collections.Generic;
using System.Reflection;

namespace RainMeadow
{
    public class ChatTextBox : ChatTemplate, ICanBeTyped
    {
        private SteamMatchmakingManager steamMatchmakingManager;
        private ButtonTypingHandler typingHandler;
        private GameObject gameObject;
        private bool isUnloading = false;
        private static List<IDetour> inputBlockers;
        private static bool blockInput = false;
        public Action<char> OnKeyDown { get; set; }
        public static int textLimit = 75;
        public static string lastSentMessage = "";

        public static event Action? OnShutDownRequest;
        public ChatTextBox(Menu.Menu menu, MenuObject owner, string displayText, Vector2 pos, Vector2 size) : base(menu, owner, displayText, pos, size)
        {
            steamMatchmakingManager = (MatchmakingManager.instances[MatchmakingManager.MatchMakingDomain.Steam] as SteamMatchmakingManager);
            lastSentMessage = "";
            this.menu = menu;
            gameObject ??= new GameObject();
            OnKeyDown = (Action<char>)Delegate.Combine(OnKeyDown, new Action<char>(CaptureInputs));
            typingHandler ??= gameObject.AddComponent<ButtonTypingHandler>();
            typingHandler.Assign(this);
            ShouldCapture(true);
        }

        public void DelayedUnload(float delay)
        {
            if (!isUnloading)
            {
                ShouldCapture(false);
                isUnloading = true;
                typingHandler.StartCoroutine(Unload(delay));
            }
        }

        private IEnumerator Unload(float delay)
        {
            yield return new WaitForSeconds(delay);

            if (typingHandler != null)
            {
                typingHandler.Unassign(this);
                typingHandler.OnDestroy();
            }
        }
        private void CaptureInputs(char input)
        {
            if (input == '\b')
            {
                if (lastSentMessage.Length > 0)
                {
                    menu.PlaySound(SoundID.MENY_Already_Selected_MultipleChoice_Clicked);
                    lastSentMessage = lastSentMessage.Substring(0, lastSentMessage.Length - 1);
                }
            }
            else if (input == '\n' || input == '\r')
            {
                if (lastSentMessage.Length > 0 && !string.IsNullOrWhiteSpace(lastSentMessage))
                {
                    if (MatchmakingManager.currentInstance is SteamMatchmakingManager)
                    {
                        steamMatchmakingManager.SendChatMessage((MatchmakingManager.instances[MatchmakingManager.MatchMakingDomain.Steam] as SteamMatchmakingManager).lobbyID, lastSentMessage);
                    }

                    foreach (var player in OnlineManager.players)
                    {
                        player.InvokeRPC(RPCs.UpdateUsernameTemporarily, lastSentMessage);
                    }
                }
                else
                {
                    menu.PlaySound(SoundID.MENY_Already_Selected_MultipleChoice_Clicked);
                    RainMeadow.Debug("Could not send lastSentMessage because it had no text or only had whitespaces");
                }
                OnShutDownRequest.Invoke();
                typingHandler.Unassign(this);
            }
            else
            {
                if (lastSentMessage.Length < textLimit)
                {
                    menu.PlaySound(SoundID.MENU_Checkbox_Check);
                    lastSentMessage += input.ToString();
                }
            }
            menuLabel.text = lastSentMessage;
        }

        // input blocker for the sake of dev tools/other outside processes that make use of input keys
        // thanks to SlimeCubed's dev console 
        private static void ShouldCapture(bool shouldCapture)
        {
            if (shouldCapture && !blockInput)
            {
                blockInput = true;
                if (inputBlockers == null)
                {
                    var input = typeof(Input);
                    var self = typeof(ChatTextBox);

                    Hook MakeHook(string method, params Type[] types)
                    {
                        Type[] toTypes = new Type[types.Length + 1];
                        types.CopyTo(toTypes, 1);
                        toTypes[0] = (types[0] == typeof(KeyCode)) ? typeof(Func<KeyCode, bool>) : typeof(Func<string, bool>);
                        return new Hook(
                            input.GetMethod(method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, types, null),
                            self.GetMethod(method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, toTypes, null)
                        );
                    }

                    inputBlockers = new List<IDetour>()
                    {
                        MakeHook(nameof(GetKey), typeof(string)),
                        MakeHook(nameof(GetKey), typeof(KeyCode)),
                        MakeHook(nameof(GetKeyDown), typeof(string)),
                        MakeHook(nameof(GetKeyDown), typeof(KeyCode)),
                        MakeHook(nameof(GetKeyUp), typeof(string)),
                        MakeHook(nameof(GetKeyUp), typeof(KeyCode)),
                    };
                }
            }
            else if (!shouldCapture && blockInput)
            {
                blockInput = false;
            }
        }

        private static bool GetKey(Func<string, bool> orig, string name) => blockInput ? false : orig(name);
        private static bool GetKey(Func<KeyCode, bool> orig, KeyCode code) => blockInput ? false : orig(code);
        private static bool GetKeyDown(Func<string, bool> orig, string name) => blockInput ? false : orig(name);
        private static bool GetKeyDown(Func<KeyCode, bool> orig, KeyCode code)
        {
            if (code == KeyCode.Return)
            {
                return orig(code);
            }

            return blockInput ? false : orig(code);
        }
        private static bool GetKeyUp(Func<string, bool> orig, string name) => blockInput ? false : orig(name);
        private static bool GetKeyUp(Func<KeyCode, bool> orig, KeyCode code) => blockInput ? false : orig(code);
    }
}