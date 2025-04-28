using Menu;
using UnityEngine;
using System;
using Menu.Remix.MixedUI;
using System.Collections;
using MonoMod.RuntimeDetour;
using System.Collections.Generic;
using System.Reflection;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace RainMeadow
{
    public class ChatTextBox : ChatTemplate, ICanBeTyped
    {
        private ButtonTypingHandler typingHandler;
        private GameObject gameObject;
        private bool isUnloading = false;
        private int backspaceHeld = 0;
        private int arrowHeld = 0;
        private static List<IDetour>? inputBlockers;
        public Action<char> OnKeyDown { get; set; }
        public static bool blockInput = false;
        public static int textLimit = 75;
        public static int cursorPos = 0;
        public static string lastSentMessage = "";

        public static event Action? OnShutDownRequest;
        public ChatTextBox(Menu.Menu menu, MenuObject owner, string displayText, Vector2 pos, Vector2 size) : base(menu, owner, displayText, pos, size)
        {
            lastSentMessage = "";
            cursorPos = 0;
            if (menu is ChatInputOverlay) inMenu = false;
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
                cursorPos = 0;
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
            // the "Delete" character, which is emitted by most - but not all - operating systems when ctrl and backspace are used together
            if (input == '\u007F') return;
            string msg = lastSentMessage;
            blockInput = false;
            if ((input == '\b' || input == '\u0008') && !(Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)))
            {
                if (cursorPos > 0)
                {
                    menu.PlaySound(SoundID.MENY_Already_Selected_MultipleChoice_Clicked);
                    lastSentMessage = msg.Remove(cursorPos - 1, 1);
                    cursorPos--;
                }
            }
            else if (input == '\n' || input == '\r')
            {
                if (msg.Length > 0 && !string.IsNullOrWhiteSpace(msg))
                {
                    MatchmakingManager.currentInstance.SendChatMessage(msg);
                    foreach (var player in OnlineManager.players)
                    {
                        player.InvokeRPC(RPCs.UpdateUsernameTemporarily, msg);
                    }
                }
                else
                {
                    menu.PlaySound(SoundID.MENY_Already_Selected_MultipleChoice_Clicked);
                    RainMeadow.Debug("Could not send lastSentMessage because it had no text or only had whitespaces");
                }
                // only resets the chat text box if in a story lobby menu, otherwise the text box is just destroyed
                OnShutDownRequest.Invoke();
                typingHandler.Unassign(this);
                lastSentMessage = "";
                return;
            }
            else
            {
                if (msg.Length < textLimit)
                {
                    menu.PlaySound(SoundID.MENU_Checkbox_Check);
                    lastSentMessage = msg.Insert(cursorPos, input.ToString());
                    cursorPos++;
                }
            }
            blockInput = true;
            menuLabel.text = lastSentMessage;
        }

        public override void GrafUpdate(float timeStacker)
        {
            var msg = ChatTextBox.lastSentMessage;
            var len = msg.Length;
            if (len > 0)
            {
                blockInput = false;
                // ctrl backspace stuff here instead of CaptureInputs, because ctrl + backspace doesn't always emit a capturable character on some operating systems
                if (Input.GetKey(KeyCode.Backspace) && cursorPos > 0)
                {
                    if ((Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) && backspaceHeld == 0)
                    {
                        ChatTextBox.lastSentMessage = "";
                        menuLabel.text = ChatTextBox.lastSentMessage;
                        cursorPos = 0;
                    }
                    // activates on either the first frame the key is held, or every other frame after it's been held down for half a second
                    else if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && (backspaceHeld == 0 || (backspaceHeld >= 30 && (backspaceHeld % 2 == 0))))
                    {
                        if (cursorPos > 0)
                        {
                            menu.PlaySound(SoundID.MENY_Already_Selected_MultipleChoice_Clicked);
                            int pos = (cursorPos > 0) ? cursorPos - 1 : 0;
                            int space = msg.Substring(0, pos).LastIndexOf(' ') + 1;
                            ChatTextBox.lastSentMessage = msg.Remove(space, cursorPos - space);
                            menuLabel.text = ChatTextBox.lastSentMessage;
                            if (space > msg.Length) space = msg.Length;
                            cursorPos = space;
                        }
                    }
                    backspaceHeld++;
                }
                else
                {
                    backspaceHeld = 0;
                    if (Input.GetKey(KeyCode.LeftArrow) && cursorPos > 0)
                    {
                        if (arrowHeld == 0 || (arrowHeld >= 30 && (arrowHeld % 2 == 0)))
                        {
                            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                            {
                                cursorPos = msg.Substring(0, cursorPos - 1).LastIndexOf(' ') + 1;
                            }
                            else if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
                            {
                                cursorPos = 0;
                            }
                            else cursorPos--;
                            if (cursorPos < len)
                            {
                                // sets cursor sprite to a one pixel wide vertical line to fit between characters
                                _cursor.element = Futile.atlasManager.GetElementWithName("pixel");
                                _cursor.height = 13f;
                                float width = LabelTest.GetWidth(menuLabel.label.text.Substring(0, cursorPos), false);
                                _cursorWidth = width;
                                cursorWrap.sprite.x = width + 8f + pos.x;
                            }
                        }
                        arrowHeld++;
                    }
                    else if (Input.GetKey(KeyCode.RightArrow) && cursorPos < len)
                    {
                        if (arrowHeld == 0 || (arrowHeld >= 30 && (arrowHeld % 2 == 0)))
                        {
                            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                            {
                                int space = msg.Substring(cursorPos, len - cursorPos - 1).IndexOf(' ');
                                if (space < 0 || space >= len) cursorPos = len;
                                else cursorPos = space + cursorPos + 1;

                            }
                            else if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
                            {
                                cursorPos = len;
                            }
                            else cursorPos++;
                            if (cursorPos == len)
                            {
                                // resets cursor sprite
                                _cursor.element = Futile.atlasManager.GetElementWithName("modInputCursor");
                                _cursor.height = 6f;
                                float width = LabelTest.GetWidth(menuLabel.label.text, false);
                                _cursorWidth = width;
                                cursorWrap.sprite.x = width + 15f + pos.x;
                            }
                        }
                        arrowHeld++;
                    }
                    else arrowHeld = 0;
                }
                blockInput = true;
            }
            base.GrafUpdate(timeStacker);
        }

        public static void InvokeShutDownChat() => OnShutDownRequest.Invoke();

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
        private static bool GetKey(Func<KeyCode, bool> orig, KeyCode code)
        {
            if (code == KeyCode.UpArrow || code == KeyCode.DownArrow) return orig(code);

            return blockInput? false : orig(code);
        }
        private static bool GetKeyDown(Func<string, bool> orig, string name) => blockInput ? false : orig(name);
        private static bool GetKeyDown(Func<KeyCode, bool> orig, KeyCode code)
        {
            if (code == KeyCode.Return) return orig(code);
            
            return blockInput ? false : orig(code);
        }
        private static bool GetKeyUp(Func<string, bool> orig, string name) => blockInput ? false : orig(name);
        private static bool GetKeyUp(Func<KeyCode, bool> orig, KeyCode code) => blockInput ? false : orig(code);
    }
}