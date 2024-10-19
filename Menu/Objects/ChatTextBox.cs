using Menu;
using UnityEngine;
using System;
using Menu.Remix.MixedUI;
using DevInterface;
using System.Collections;

namespace RainMeadow
{
    public class ChatTextBox : SimpleButton, ICanBeTyped
    {
        private SteamMatchmakingManager steamMatchmakingManager;
        private ButtonTypingHandler typingHandler;
        private GameObject gameObject;
        public Action<char> OnKeyDown { get; set; }
        public static int textLimit = 150;
        public string value;
        public event Action OnUnload;
        public ChatTextBox(Menu.Menu menu, MenuObject owner, string displayText, Vector2 pos, Vector2 size) : base(menu, owner, displayText, "", pos, size)
        {
            steamMatchmakingManager = MatchmakingManager.instance as SteamMatchmakingManager;
            value = "";
            this.menu = menu;
            gameObject ??= new GameObject();
            OnKeyDown = (Action<char>)Delegate.Combine(OnKeyDown, new Action<char>(CaptureInputs));
            typingHandler ??= gameObject.AddComponent<ButtonTypingHandler>();
            typingHandler.Assign(this);
        }

        public void DelayedUnload(float delay) => typingHandler.StartCoroutine(UnloadAfterDelay(delay));
        private IEnumerator UnloadAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);

            Unload();
        }

        public void Unload()
        {
            try 
            {
                OnUnload?.Invoke();
            }
            catch (Exception e)
            {
                RainMeadow.Error($"Error unloading: {e}");
                return;
            }

            OnKeyDown = null;
            typingHandler.Unassign(this);
            typingHandler.OnDestroy();
        }
        private void CaptureInputs(char input)
        {
            if (ChatHud.gamePaused)
            {
                OnKeyDown = null;
                return;
            }
                if (input == '\b')
                {
                    if (value.Length > 0)
                    {
                        menu.PlaySound(SoundID.MENY_Already_Selected_MultipleChoice_Clicked);
                        value = value.Substring(0, value.Length - 1);
                    }
                }
                else if (input == '\n' || input == '\r')
                {
                    if (value.Length > 0)
                    {
                        steamMatchmakingManager.SendChatMessage((MatchmakingManager.instance as SteamMatchmakingManager).lobbyID, value);
                    }
                    else
                    {
                        RainMeadow.Debug("Could not send value because it had no text");
                    }
                    RainMeadow.Debug("closing out chat text box");
                    typingHandler.Unassign(this);
                }
                else
                {
                    if (value.Length < textLimit)
                    {
                        menu.PlaySound(SoundID.MENU_Checkbox_Check);
                        value += input.ToString();
                    }
                }
                menuLabel.text = value;
            
        }
    }
}