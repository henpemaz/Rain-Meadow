using Menu;
using UnityEngine;
using System;
using Menu.Remix.MixedUI;
using System.Collections;
using System.Linq;

namespace RainMeadow
{
    public class ChatTextBox : SimpleButton, ICanBeTyped
    {
        private SteamMatchmakingManager steamMatchmakingManager;
        private ButtonTypingHandler typingHandler;
        private GameObject gameObject;
        private bool isUnloading = false;
        public Action<char> OnKeyDown { get; set; }
        public static int textLimit = 75;
        public static string lastSentMessage = "";
        public ChatTextBox(Menu.Menu menu, MenuObject owner, string displayText, Vector2 pos, Vector2 size) : base(menu, owner, displayText, "", pos, size)
        {
            steamMatchmakingManager = MatchmakingManager.instance as SteamMatchmakingManager;
            lastSentMessage = "";
            this.menu = menu;
            gameObject ??= new GameObject();
            OnKeyDown = (Action<char>)Delegate.Combine(OnKeyDown, new Action<char>(CaptureInputs));
            typingHandler ??= gameObject.AddComponent<ButtonTypingHandler>();
            typingHandler.Assign(this);
        }

        public void DelayedUnload(float delay)
        {
            if (!isUnloading)
            {
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
                    if (MatchmakingManager.instance is SteamMatchmakingManager)
                    {
                        steamMatchmakingManager.SendChatMessage((MatchmakingManager.instance as SteamMatchmakingManager).lobbyID, lastSentMessage);
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
    }
}