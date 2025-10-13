﻿using Menu;
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
        private float DASDelay = 1f / 2f; //In seconds
        private float DASRepeatRate = 1f / 30f; //In seconds/proc
        private float backspaceHeld = 0f;
        private float backspaceRepeater = 0f;
        private float arrowHeld = 0f;
        private float arrowRepeater = 0f;
        private static List<IDetour>? inputBlockers;
        public Action<char> OnKeyDown { get; set; }
        public static bool blockInput = false;
        public static int textLimit = 75;
        public static int cursorPos = 0;
        public static int selectionPos = -1;
        public static string lastSentMessage = "";

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
            ShouldCapture(true);
            if (OnlineManager.lobby.clientSettings.TryGetValue(OnlineManager.mePlayer, out var cs))
            {
                cs.isInteracting = true;
            }
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
                blockInput = false;
            }

        }
        private void CaptureInputs(char input)
        {
            // the "Delete" character, which is emitted by most - but not all - operating systems when ctrl and backspace are used together
            if (input == '\u007F') return;
            string msg = lastSentMessage;
            blockInput = false;
            if (input == '\b')
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
                if (OnlineManager.lobby.clientSettings.TryGetValue(OnlineManager.mePlayer, out var cs))
                {
                    cs.isInteracting = false;
                }
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
            else if (!isUnloading)
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
            if (!isUnloading) blockInput = true;
            menuLabel.text = lastSentMessage;
        }
        public override void Update()
        {
            base.Update();
            menu.allowSelectMove = false;
        }
        public override void GrafUpdate(float timeStacker)
        {
            var msg = lastSentMessage;
            var len = msg.Length;
            if (len > 0)
            {
                blockInput = false;
                // ctrl backspace stuff here instead of CaptureInputs, because ctrl + backspace doesn't always emit a capturable character on some operating systems
                if (Input.GetKey(KeyCode.Backspace) && (cursorPos > 0 || selectionPos != -1))
                {
                    // no alt + backspace, because alt can be finnicky
                    // activates on either the first frame the key is held, or for every (DASRepeatRate)th of a second after (DASDelay) seconds of being held
                    if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && (backspaceHeld == 0 || (backspaceHeld >= DASDelay && backspaceRepeater >= DASRepeatRate)))
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
                        backspaceRepeater %= DASRepeatRate; //Modulus instead of subtract so the repeater can't scale out of control if DeltaTime > DASRepeatRate.
                    }
                    backspaceHeld += Time.deltaTime;
                    backspaceRepeater += Time.deltaTime;
                }

                else if (Input.GetKey(KeyCode.Delete))
                {
                    if (selectionPos != -1)
                    {
                        menu.PlaySound(SoundID.MENY_Already_Selected_MultipleChoice_Clicked);
                        DeleteSelection();
                    }
                    else if ((backspaceHeld == 0 || (backspaceHeld >= DASDelay && backspaceRepeater >= DASRepeatRate)) && cursorPos < msg.Length)
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
                        backspaceRepeater %= DASRepeatRate;
                    }
                    if (cursorPos == lastSentMessage.Length) SetCursorSprite(false);
                    backspaceHeld += Time.deltaTime;
                    backspaceRepeater += Time.deltaTime;
                }

                else
                {
                    backspaceHeld = 0f;
                    backspaceRepeater = 0f;
                    if (Input.GetKey(KeyCode.Home))
                    {
                        bool changeSprite = cursorPos == len;
                        cursorPos = 0;
                        selectionPos = -1;
                        if (changeSprite) SetCursorSprite(true);
                    }

                    else if (Input.GetKey(KeyCode.End) && cursorPos < len)
                    {
                        cursorPos = len;
                        selectionPos = -1;
                        SetCursorSprite(false);
                    }

                    else if (Input.GetKey(KeyCode.A) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
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
                        if ((cursorPos > 0 || selectionPos != -1) && (arrowHeld == 0 || (arrowHeld >= DASDelay && arrowRepeater >= DASRepeatRate)))
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
                                if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && newPos > 0)
                                {
                                    newPos = msg.Substring(0, newPos - 1).LastIndexOf(' ') + 1;
                                    if (newPos < 0 || newPos > len) newPos = 0;
                                }
                                else newPos = Math.Max(0, newPos - 1);
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
                            arrowRepeater %= DASRepeatRate;
                        }
                        arrowHeld += Time.deltaTime;
                        arrowRepeater += Time.deltaTime;
                    }

                    else if (Input.GetKey(KeyCode.RightArrow))
                    {
                        if ((cursorPos < len || selectionPos != -1) && (arrowHeld == 0 || (arrowHeld >= DASDelay && arrowRepeater >= DASRepeatRate)))
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
                            arrowRepeater %= DASRepeatRate;
                        }
                        arrowHeld += Time.deltaTime;
                        arrowRepeater += Time.deltaTime;
                    }
                    else
                    {
                        arrowHeld = 0f;
                        arrowRepeater = 0f;
                    }
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
                cursorWrap.sprite.x = width + 11f + pos.x;
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

        public static void InvokeShutDownChat()
        {
            if (OnlineManager.lobby.clientSettings.TryGetValue(OnlineManager.mePlayer, out var cs))
            {
                cs.isInteracting = false;
            }
            OnShutDownRequest.Invoke();
        }

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
        private static bool GetKey(Func<string, bool> orig, string name) => blockInput ? false : orig(name);
        private static bool GetKey(Func<KeyCode, bool> orig, KeyCode code)
        {
            if (code == KeyCode.UpArrow || code == KeyCode.DownArrow) return orig(code);

            return blockInput ? false : orig(code);
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