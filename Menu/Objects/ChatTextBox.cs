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
        private ButtonTypingHandler typingHandler;
        private GameObject gameObject;
        private bool isUnloading = false;
        private int backspaceHeld = 0;
        private int arrowHeld = 0;
        private static List<IDetour>? inputBlockers;
        public Action<char> OnKeyDown { get; set; }
        public static bool blockInput = false;
        public int textLimit = 75;
        public static int cursorPos = 0;
        public static int selectionPos = -1;
        public bool focused = false, clicked;

        public static event Action? OnShutDownRequest;
        public ChatTextBox(Menu.Menu menu, MenuObject owner, string displayText, Vector2 pos, Vector2 size) : base(menu, owner, displayText, pos, size)
        {
            lastSentMessage = "";
            cursorPos = 0;
            selectionPos = -1;
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
                cursorPos = 0;
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
            if (!focused) return;

            // the "Delete" character, which is emitted by most - but not all - operating systems when ctrl and backspace are used together
            if (input == '\u007F') return;
            string msg = lastSentMessage;
            blockInput = false;
            if ((input == '\b' || input == '\u0008') && !(Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)))
            {
                if (cursorPos > 0 || selectionPos != -1)
                {
                    menu.PlaySound(SoundID.MENY_Already_Selected_MultipleChoice_Clicked);
                    // selection position is -1 when nothing is selected
                    if (selectionPos != -1)
                    {
                        // deletes the selected text
                        menu.PlaySound(SoundID.MENY_Already_Selected_MultipleChoice_Clicked);
                        DeleteSelection();
                        if (cursorPos == lastSentMessage.Length) SetCursorSprite(false);
                    }
                    else
                    {
                        lastSentMessage = msg.Remove(cursorPos - 1, 1);
                        cursorPos--;
                    }
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
                focused = false;
                // only resets the chat text box if in a story lobby menu, otherwise the text box is just destroyed
                OnShutDownRequest.Invoke();
                typingHandler.Unassign(this);
                lastSentMessage = "";
                return;
            }
            else
            {
                if (selectionPos != -1)
                {
                    // replaces the selected text with the emitted character
                    menu.PlaySound(SoundID.MENU_Checkbox_Check);
                    DeleteSelection();
                    lastSentMessage = lastSentMessage.Insert(cursorPos, input.ToString());
                    cursorPos++;
                    if (cursorPos == lastSentMessage.Length)
                    {
                        SetCursorSprite(false);
                    }
                }
                else if (msg.Length < textLimit)
                {
                    menu.PlaySound(SoundID.MENU_Checkbox_Check);
                    lastSentMessage = msg.Insert(cursorPos, input.ToString());
                    cursorPos++;
                }
            }
            blockInput = true;
            menuLabel.text = lastSentMessage;
        }

        public override void Update()
        {
            base.Update();
            if (focused && Input.GetMouseButton(0))
            {
                focused = false;
                clicked = false;
            }

            if (focused)
            {
                cursorWrap.sprite.alpha = Mathf.PingPong(Time.time * 4f, 1f);
                menu.allowSelectMove = false; // Menu.Update() will set this back to true
            }
            else cursorWrap.sprite.alpha = 0f;
        }
        public override void Clicked()
        {
            base.Clicked();

            if (focused && Input.GetKey(KeyCode.Space)) return;

            if (Input.GetMouseButton(0)) clicked = false; // if someone clicks with mouse we reset clicked check since clicking with mouse should always focus 

            focused = !clicked;
            clicked = !clicked;
        }

        public override void GrafUpdate(float timeStacker)
        {
            ShouldCapture(focused);

            var msg = lastSentMessage;
            var len = msg.Length;
            if (len > 0)
            {
                blockInput = false;
                // ctrl backspace stuff here instead of CaptureInputs, because ctrl + backspace doesn't always emit a capturable character on some operating systems
                if (Input.GetKey(KeyCode.Backspace) && (cursorPos > 0 || selectionPos != -1))
                {
                    if ((Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) && backspaceHeld == 0)
                    {
                        lastSentMessage = "";
                        menuLabel.text = lastSentMessage;
                        cursorPos = 0;
                        selectionPos = -1;
                    }
                    // activates on either the first frame the key is held, or every other frame after it's been held down for half a second
                    else if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && (backspaceHeld == 0 || (backspaceHeld >= 30 && (backspaceHeld % 2 == 0))))
                    {
                        if (selectionPos != -1)
                        {
                            menu.PlaySound(SoundID.MENY_Already_Selected_MultipleChoice_Clicked);
                            DeleteSelection();
                            if (cursorPos == lastSentMessage.Length) SetCursorSprite(false);
                        }
                        else if (cursorPos > 0)
                        {
                            menu.PlaySound(SoundID.MENY_Already_Selected_MultipleChoice_Clicked);
                            int space = msg.Substring(0, cursorPos - 1).LastIndexOf(' ') + 1;
                            lastSentMessage = msg.Remove(space, cursorPos - space);
                            menuLabel.text = lastSentMessage;
                            cursorPos = space;
                        }
                    }
                    backspaceHeld++;
                }

                else if (Input.GetKey(KeyCode.Delete))
                {
                    if (selectionPos != -1)
                    {
                        menu.PlaySound(SoundID.MENY_Already_Selected_MultipleChoice_Clicked);
                        DeleteSelection();
                    }
                    else if ((backspaceHeld == 0 || (backspaceHeld >= 30 && (backspaceHeld % 2 == 0))) && cursorPos < msg.Length)
                    {
                        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                        {
                            menu.PlaySound(SoundID.MENY_Already_Selected_MultipleChoice_Clicked);
                            int space = msg.Substring(cursorPos, len - cursorPos).IndexOf(' ');
                            lastSentMessage = msg.Remove(cursorPos, (space < 0 || space >= len) ? (space = len - cursorPos) : space + 1);
                            menuLabel.text = lastSentMessage;

                        }
                        else
                        {
                            menu.PlaySound(SoundID.MENY_Already_Selected_MultipleChoice_Clicked);
                            lastSentMessage = msg.Remove(cursorPos, 1);
                            menuLabel.text = lastSentMessage;
                        }
                    }
                    if (cursorPos == lastSentMessage.Length) SetCursorSprite(false);
                    backspaceHeld++;
                }

                else
                {
                    backspaceHeld = 0;
                    if (Input.GetKeyDown(KeyCode.Home))
                    {
                        bool changeSprite = cursorPos == len;
                        cursorPos = 0;
                        selectionPos = -1;
                        if (changeSprite) SetCursorSprite(true);
                    }

                    else if (Input.GetKeyDown(KeyCode.End) && cursorPos < len)
                    {
                        cursorPos = len;
                        selectionPos = -1;
                        SetCursorSprite(false);
                    }

                    else if (Input.GetKeyDown(KeyCode.A) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
                    {
                        if (cursorPos == len)
                        {
                            SetCursorSprite(true);
                        }
                        cursorPos = 0;
                        selectionPos = msg.Length;
                    }

                    else if (Input.GetKey(KeyCode.LeftArrow))
                    {
                        // cursor position is used as the anchor for selection
                        if ((cursorPos > 0 || selectionPos != -1) && (arrowHeld == 0 || (arrowHeld >= 30 && (arrowHeld % 2 == 0))))
                        {
                            var shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                            var selectionActive = selectionPos != -1;
                            if (selectionActive && !shiftHeld)
                            {
                                var changeSprite = cursorPos == len;
                                if (selectionPos < cursorPos) cursorPos = selectionPos;
                                selectionPos = -1;
                                if (changeSprite) SetCursorSprite(true);
                            }
                            else
                            {
                                var newPos = (shiftHeld && selectionActive) ? selectionPos : cursorPos;
                                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                                {
                                    newPos = msg.Substring(0, newPos - 1).LastIndexOf(' ') + 1;
                                    if (newPos < 0 || newPos > len) newPos = 0;
                                }
                                else newPos--;
                                if (shiftHeld)
                                {
                                    // stops the selection if it's on the same index as the anchor
                                    selectionPos = (newPos == cursorPos) ? -1 : newPos;
                                }
                                else
                                {
                                    cursorPos = newPos;
                                    if (cursorPos < len) SetCursorSprite(true);
                                }
                            }
                        }
                        arrowHeld++;
                    }

                    else if (Input.GetKey(KeyCode.RightArrow))
                    {
                        if ((cursorPos < len || selectionPos != -1) && (arrowHeld == 0 || arrowHeld >= 30 && (arrowHeld % 2 == 0)))
                        {
                            var shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                            var selectionActive = selectionPos != -1;
                            if (selectionActive && !shiftHeld)
                            {
                                if (selectionPos > cursorPos) cursorPos = selectionPos;
                                selectionPos = -1;
                                if (cursorPos == len)
                                {
                                    SetCursorSprite(false);
                                }
                            }
                            else
                            {
                                // starts from the end of the selection if a selection exists
                                if (!selectionActive || selectionPos < msg.Length)
                                {
                                    var newPos = (shiftHeld && selectionActive) ? selectionPos : cursorPos;
                                    if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                                    {
                                        int space = msg.Substring(newPos, len - newPos - 1).IndexOf(' ');
                                        if (space < 0 || space >= len) newPos = len;
                                        else newPos = space + newPos + 1;
                                    }
                                    else newPos++;
                                    if (shiftHeld)
                                    {
                                        selectionPos = (newPos == cursorPos) ? -1 : newPos;
                                    }
                                    else
                                    {
                                        cursorPos = newPos;
                                        if (newPos == len) SetCursorSprite(false);
                                    }
                                }
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

        private void DeleteSelection()
        {
            lastSentMessage = lastSentMessage.Remove(Mathf.Min(cursorPos, selectionPos), Mathf.Abs(selectionPos - cursorPos));
            menuLabel.text = lastSentMessage;
            if (selectionPos < cursorPos) cursorPos = selectionPos;
            selectionPos = -1;
        }

        private void SetCursorSprite(bool inMiddle)
        {
            if (inMiddle)
            {
                _cursor.element = Futile.atlasManager.GetElementWithName("pixel");
                _cursor.height = 13f;
                float width = LabelTest.GetWidth(menuLabel.label.text.Substring(0, cursorPos), false);
                _cursorWidth = width;
                cursorWrap.sprite.x = width + 8f + pos.x;
            }
            else
            {
                _cursor.element = Futile.atlasManager.GetElementWithName("modInputCursor");
                _cursor.height = 6f;
                float width = LabelTest.GetWidth(menuLabel.label.text, false);
                _cursorWidth = width;
                cursorWrap.sprite.x = width + 15f + pos.x;
            }
        }

        public static void InvokeShutDownChat() => OnShutDownRequest.Invoke();

        // input blocker for the sake of dev tools/other outside processes that make use of input keys
        // thanks to SlimeCubed's dev console 
        public static void ShouldCapture(bool shouldCapture)
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
        public static bool GetKey(Func<string, bool> orig, string name) => blockInput ? false : orig(name);
        public static bool GetKey(Func<KeyCode, bool> orig, KeyCode code)
        {
            if (code == KeyCode.UpArrow || code == KeyCode.DownArrow) return orig(code);

            return blockInput ? false : orig(code);
        }
        public static bool GetKeyDown(Func<string, bool> orig, string name) => blockInput ? false : orig(name);
        public static bool GetKeyDown(Func<KeyCode, bool> orig, KeyCode code)
        {
            if (code == KeyCode.Return) return orig(code);

            return blockInput ? false : orig(code);
        }
        public static bool GetKeyUp(Func<string, bool> orig, string name) => blockInput ? false : orig(name);
        public static bool GetKeyUp(Func<KeyCode, bool> orig, KeyCode code) => blockInput ? false : orig(code);
    }
}