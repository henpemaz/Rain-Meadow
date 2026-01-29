using Menu;
using UnityEngine;
using System;
using Menu.Remix.MixedUI;
using System.Collections;
using MonoMod.RuntimeDetour;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text.RegularExpressions;

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
        private bool clipboardHeld = false;
        private bool tabHeld = false;
        private static List<IDetour>? inputBlockers;
        public Action<char> OnKeyDown { get; set; }
        public static bool blockInput = false;
        public const int textLimit = 100;
        public static int cursorPos = 0;
        public static int selectionPos = -1;
        public static int historyCursor = -1;
        public static string lastSentMessage = "";
        public static string lastTyped = "";

        public static List<string> messageHistory = new();
        public static List<string> completions = new();
        public static bool completed = false;
        public static int autoCompleteIndex = 0;
        public string lastCompletion = "";
        public int lastCompletionStart = -1;
        public int lastCompletionEnd = -1;

        public static event Action? OnShutDownRequest;
        public event Action? OnTextSubmit;

        public int VisibleTextLimit => visibleTextLimit ?? Mathf.FloorToInt(menuLabel.size.x / Mathf.Max(LabelTest.GetWidth(lastSentMessage) / Mathf.Max(lastSentMessage.Length, 1), 1));
        public int? visibleTextLimit;
        public static string Clipboard
        {
            get 
            {
                var contents = GUIUtility.systemCopyBuffer;
                RainMeadow.Debug($"Clipboard was accessed! Reading {contents.Length} chars from system clipboard!");
                return contents;
            }
            set
            {
                RainMeadow.Debug($"Clipboard was accessed! Writing {value.Length} chars to system clipboard!");
                GUIUtility.systemCopyBuffer = value;
            }
        }
        // Multiview Support
        public bool focused, forceMenuMouseMode, lastFreezeMenuFunctions, lastMenuMouseMode, previouslySubmittedText;

        public bool Focused
        {
            get => focused;
            set
            {
                focused = value;
                forceMenuMouseMode = value ? menu.manager.menuesMouseMode : forceMenuMouseMode;
            }
        }
        public bool IgnoreSelect => (focused && !menu.manager.menuesMouseMode);
        public bool TypingOnOtherObjects => CanBeTypedExt._handler?._focused != null;
        public bool DontGetInputs => menu.FreezeMenuFunctions || lastFreezeMenuFunctions || !menu.Active || page != menu.pages.GetValueOrDefault(menu.currentPage);
        //

        public static bool AnyCtrl => (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftApple));

        public ChatTextBox(Menu.Menu menu, MenuObject owner, string displayText, Vector2 pos, Vector2 size, bool multiView = false) : base(menu, owner, displayText, pos, size)
        {
            MultiView = multiView;
            lastSentMessage = "";
            cursorPos = 0;
            selectionPos = -1;
            historyCursor = messageHistory.Count;
            this.menu = menu;
            gameObject ??= new GameObject();
            OnKeyDown = (Action<char>)Delegate.Combine(OnKeyDown, new Action<char>(CaptureInputs));
            typingHandler ??= gameObject.AddComponent<ButtonTypingHandler>();
            typingHandler.Assign(this);
            ShouldCapture(true);
            if (OnlineManager.lobby.clientSettings.TryGetValue(OnlineManager.mePlayer, out var cs))
            {
                if (!MultiView)
                    cs.isInteracting = true;
                else 
                    cs.isInteracting = Focused;
            }
        }

        public override void Clicked()
        {
            base.Clicked();
            if (!MultiView) return;
            if (previouslySubmittedText)
            {
                previouslySubmittedText = false;
                if (!menu.manager.menuesMouseMode) return;
            }
            if (IgnoreSelect) return;
            if (!buttonBehav.clicked) return;
            SetFocused(!Focused);
        }
        public void CheckToUnfocus()
        {
            if ((menu.pressButton && menu.manager.menuesMouseMode && !buttonBehav.clicked) || buttonBehav.greyedOut)
            {
                SetFocused(false, menu.selectedObject == null || buttonBehav.greyedOut ? null : SoundID.None);
                return;
            }
            if (TypingOnOtherObjects)
                SetFocused(false, SoundID.None);
        }
        public void HandleDeselect()
        {
            SetFocused(false);
            blockInput = false;
            previouslySubmittedText = !menu.manager.menuesMouseMode;
            if (OnlineManager.lobby.clientSettings.TryGetValue(OnlineManager.mePlayer, out var cs))
            {
                cs.isInteracting = false;
            }
        }
        public void SetFocused(bool focused, SoundID? overrideSoundID = null)
        {
            if (!MultiView) return;
            if (Focused != focused)
                menu.PlaySound(overrideSoundID ?? (focused ? SoundID.MENU_Button_Standard_Button_Pressed : SoundID.MENU_Checkbox_Uncheck));
            Focused = focused;
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
            if (MultiView)
            {
                if (DontGetInputs) return;
                blockInput = false;
                if (!Focused)
                {
                    Player.InputPackage currentInput = RWInput.PlayerUIInput(-1); //race conditions when update isnt called on time
                    bool shouldActuallyGetInput = menu.selectedObject == null || (!menu.pressButton && !menu.holdButton && !menu.lastHoldButton && !menu.modeSwitch && !currentInput.jmp);
                    if (Input.GetKeyDown(RainMeadow.rainMeadowOptions.ChatButtonKey.Value) && shouldActuallyGetInput && !TypingOnOtherObjects)
                    {
                        SetFocused(true);
                        forceMenuMouseMode = forceMenuMouseMode || lastMenuMouseMode;
                        if (!forceMenuMouseMode) menu.selectedObject = this;
                    }
                    return;
                }
            }
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
                    if (messageHistory.Count == 0 || messageHistory[messageHistory.Count - 1] != msg)
                    {
                        messageHistory.Add(msg);
                    }
                    MatchmakingManager.currentInstance.SendChatMessage(msg);
                    foreach (var player in OnlineManager.players)
                    {
                        player.InvokeRPC(RPCs.UpdateUsernameTemporarily, msg);
                    }
                    if (MultiView)
                    {
                        HandleTextSubmit();
                        return;
                    }
                }
                else
                {
                    if (MultiView) HandleDeselect();
                    menu.PlaySound(SoundID.MENY_Already_Selected_MultipleChoice_Clicked);
                    RainMeadow.Debug("Could not send lastSentMessage because it had no text or only had whitespaces");
                    if (MultiView) return;
                }
                // only resets the chat text box if in a story lobby menu, otherwise the text box is just destroyed
                OnShutDownRequest?.Invoke();
                typingHandler.Unassign(this);
                lastSentMessage = "";
                completed = false;
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
            UpdateLabel(lastSentMessage);
        }
        public override void Update()
        {
            base.Update();
            if (MultiView)
            {
                CheckToUnfocus();
                menu.allowSelectMove = !Focused;
                roundedRect.addSize = new Vector2(5f, 3f) * (buttonBehav.sizeBump + 0.5f * Mathf.Sin(buttonBehav.extraSizeBump * 3.14f)) * (buttonBehav.clicked && !IgnoreSelect ? 0 : 1);
                forceMenuMouseMode = (menu.holdButton || menu.pressButton || menu.modeSwitch || Focused) && forceMenuMouseMode;
                menu.manager.menuesMouseMode = forceMenuMouseMode || menu.manager.menuesMouseMode;
                lastMenuMouseMode = menu.manager.menuesMouseMode;
                lastFreezeMenuFunctions = menu.FreezeMenuFunctions;
            }
            else
            {
                menu.allowSelectMove = false;
            }
            maxVisibleLength = VisibleTextLimit;
        }

        public void UpdateLabel(string text)
        {
            int firstLetterViewed = cursorPos > maxVisibleLength ? cursorPos - maxVisibleLength : 0,
                lastLetterViewed = Mathf.Max(0, cursorPos > maxVisibleLength ? maxVisibleLength : Mathf.Min(maxVisibleLength, text.Length));

            menuLabel.text = text.Substring(firstLetterViewed, lastLetterViewed);
        }

        public override void GrafUpdate(float timeStacker)
        {
            if (MultiView)
            {
                ShouldCapture(Focused);
            }
            var msg = lastSentMessage;
            var len = msg.Length;
            var hasText = len > 0;
            blockInput = false;
            if (MultiView && !Focused)
            {
                base.GrafUpdate(timeStacker);
                return;
            }
            // ctrl backspace stuff here instead of CaptureInputs, because ctrl + backspace doesn't always emit a capturable character on some operating systems
            if (Input.GetKey(KeyCode.Backspace) && (cursorPos > 0 || selectionPos != -1))
            {
                // no alt + backspace, because alt can be finnicky
                // activates on either the first frame the key is held, or for every (DASRepeatRate)th of a second after (DASDelay) seconds of being held
                if (AnyCtrl && (backspaceHeld == 0 || (backspaceHeld >= DASDelay && backspaceRepeater >= DASRepeatRate)))
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
                        //UpdateLabel(lastSentMessage);
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
                    if (AnyCtrl)
                    {
                        menu.PlaySound(SoundID.MENY_Already_Selected_MultipleChoice_Clicked);
                        int space = msg.Substring(cursorPos, Mathf.Max(len - cursorPos, 0)).IndexOf(' ');
                        lastSentMessage = msg.Remove(cursorPos, (space < 0 || space >= len) ? (space = Mathf.Max(len - cursorPos, 0)) : space + 1);
                        //UpdateLabel(lastSentMessage);

                    }
                    else
                    {
                        menu.PlaySound(SoundID.MENY_Already_Selected_MultipleChoice_Clicked);
                        lastSentMessage = msg.Remove(cursorPos, 1);
                        //UpdateLabel(lastSentMessage);
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
                // double check
                else if (Input.GetKey(KeyCode.A) && (AnyCtrl))
                {
                    if (cursorPos == len)
                    {
                        SetCursorSprite(true);
                    }
                    cursorPos = 0;
                    selectionPos = msg.Length;
                }

                // Auto Complete
                //else if (Input.GetKey(KeyCode.Tab) && !tabHeld)
                //{
                //    AutoComplete();
                //    tabHeld = true;
                //}

                // CTRL + C / Command + C
                else if (Input.GetKey(KeyCode.C) && !clipboardHeld && (AnyCtrl))
                {
                    CopySelection();
                    clipboardHeld = true;
                }
                // CTRL + V / Command + V
                else if (Input.GetKey(KeyCode.V) && !clipboardHeld && (AnyCtrl))
                {
                    menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
                    lastSentMessage = Paste(msg);
                    UpdateLabel(lastSentMessage);
                    cursorPos = Mathf.Min(lastSentMessage.Length, cursorPos + Clipboard.Length);
                    selectionPos = -1;
                    clipboardHeld = true;
                }
                // CTRL + X / Command + X
                else if (Input.GetKey(KeyCode.X) && !clipboardHeld && (AnyCtrl))
                {
                    menu.PlaySound(SoundID.MENY_Already_Selected_MultipleChoice_Clicked);
                    CopySelection();
                    DeleteSelection();
                    clipboardHeld = true;
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
                            if (AnyCtrl && newPos > 0)
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
                                if (AnyCtrl)
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
                else if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow))
                {
                    if (AnyCtrl && (arrowHeld == 0 || (arrowHeld >= DASDelay && arrowRepeater >= DASRepeatRate)))
                    {
                        if (Input.GetKey(KeyCode.UpArrow))
                        {
                            GetMessageHistory(-1);
                        }
                        else if (Input.GetKey(KeyCode.DownArrow))
                        {
                            GetMessageHistory(1);
                        }
                        arrowRepeater %= DASRepeatRate;
                    }
                    arrowHeld += Time.deltaTime;
                    arrowRepeater += Time.deltaTime;
                    // Prevent arrowHeld & arrowRepeater from being reset.
                }
                else
                {
                    arrowHeld = 0f;
                    arrowRepeater = 0f;
                }
                if (!(Input.GetKey(KeyCode.C) || Input.GetKey(KeyCode.V) || Input.GetKey(KeyCode.X)))
                {
                    clipboardHeld = false;
                }
                if (!Input.GetKey(KeyCode.Tab))
                {
                    tabHeld = false;
                }
            }
            blockInput = true;
            base.GrafUpdate(timeStacker);
        }

        private void DeleteSelection()
        {
            lastSentMessage = lastSentMessage.Remove(Mathf.Min(cursorPos, selectionPos), Mathf.Abs(selectionPos - cursorPos));
            //UpdateLabel(lastSentMessage);
            if (selectionPos < cursorPos) cursorPos = selectionPos;
            selectionPos = -1;
        }

        public void HandleTextSubmit()
        {
            lastSentMessage = "";
            menuLabel.text = "";
            cursorPos = 0;
            selectionPos = -1;
            historyCursor = messageHistory.Count;
            lastCompletion = "";
            completed = false;
            HandleDeselect();
            OnTextSubmit?.Invoke();
        }

        private void CopySelection()
        {
            if (selectionPos == -1) return;
            Clipboard = lastSentMessage.Substring(Mathf.Max(0, Mathf.Min(cursorPos, selectionPos)), Mathf.Abs(selectionPos - cursorPos));
        }

        private string Paste(string msg)
        {
            var paste = string.Copy(Clipboard);
            if (string.IsNullOrEmpty(paste))
            {
                RainMeadow.Debug("Clipboard was empty.");
                return msg;
            }

            paste = Clean(paste);

            int space = textLimit - msg.Length;

            if (space <= 0) return msg;
            if (paste.Length > space) paste = paste.Substring(0, space);

            RainMeadow.Debug($"Pasted {paste.Length} chars from clipboard.");
            return msg.Insert(Mathf.Clamp(cursorPos, 0, msg.Length), paste);
        }

        private string Clean(string msg)
        {
            return Regex.Replace(msg, @"\r\n?|\n", "");
        }

        private void GetMessageHistory(int dir)
        {
            int last = messageHistory.Count;
            int index = Mathf.Clamp(historyCursor + dir, 0, last);
            if (index == historyCursor) return;
            if (index == last)
            {
                historyCursor = last;
                lastSentMessage = lastTyped;
            }
            else
            {
                if (historyCursor == last)
                {
                    lastTyped = lastSentMessage;
                }

                historyCursor = index;
                lastSentMessage = messageHistory[index];
            }
            //UpdateLabel(lastSentMessage);
            cursorPos = lastSentMessage.Length;
            selectionPos = -1;
        }

        private void AutoComplete()
        {
            int start, end;
            string current;

            if (lastCompletionStart != -1 && cursorPos >= lastCompletionStart && cursorPos <= lastCompletionEnd)
            {
                start = lastCompletionStart; 
                end = lastCompletionEnd;
                current = lastCompletion;
            }
            else
            {
                UpdateCompletions();

                start = GetStart(lastSentMessage, cursorPos);
                end = GetEnd(lastSentMessage, cursorPos);
                current = lastSentMessage.Substring(start, end - start);

                completions = completions.Where(x => x.StartsWith(current, StringComparison.OrdinalIgnoreCase)).OrderBy(x => x).ToList();
            }

            if (completions.Count == 0)
            {
                ResetCompletions();
                return;
            }

            string complete = completions[autoCompleteIndex];
            string newText = lastSentMessage.Substring(0, start) + complete + lastSentMessage.Substring(end);
            if (newText.Length > textLimit)
            {
                ResetCompletions();
                return;
            }

            lastSentMessage = newText;

            lastCompletionStart = start;
            lastCompletionEnd = end;
            lastCompletion = completions[autoCompleteIndex];

            cursorPos = Mathf.Clamp(end + complete.Length, 0, lastSentMessage.Length);
            autoCompleteIndex = (autoCompleteIndex + 1) % completions.Count;
        }

        private void UpdateCompletions()
        {
            completions.Clear();
            autoCompleteIndex = 0;
            foreach (var player in OnlineManager.players)
            {
                completions.Add(player.id.GetPersonaName());
            }
        }

        private void ResetCompletions()
        {
            lastCompletionEnd = -1;
            lastCompletionStart = -1;
            lastCompletion = "";
            autoCompleteIndex = 0;
            completions.Clear();
        }

        private int GetStart(string text, int pos)
        {
            int i = pos - 1;
            while(i >= 0 && text[i] != ' ')
            {
                i--;
            }
            return i + 1;
        }

        private int GetEnd(string text, int pos)
        {
            int i = pos;
            while (i < text.Length && text[i] != ' ')
            {
                i++;
            }
            return i;
        }

        private void SetCursorSprite(bool inMiddle)
        {
            int lowestCursorPos = selectionPos != -1 ? Mathf.Min(cursorPos, selectionPos) : cursorPos;
            float width = LabelTest.GetWidth(menuLabel.label.text.Substring(0, lowestCursorPos > maxVisibleLength ? menuLabel.label.text.Length : lowestCursorPos), false);
            if (inMiddle)
            {
                _cursor.element = Futile.atlasManager.GetElementWithName("pixel");
                _cursor.height = 13f;
                _cursorWidth = width;
                cursorWrap.sprite.x = width + 11f + pos.x;
            }
            else
            {
                _cursor.element = Futile.atlasManager.GetElementWithName("modInputCursor");
                _cursor.height = 6f;
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

        public override bool IsFoucsed()
        {
            if (!MultiView) return true;
            return Focused;
        }

        private static bool GetKey(Func<string, bool> orig, string name) => blockInput ? false : orig(name);
        private static bool GetKey(Func<KeyCode, bool> orig, KeyCode code)
        {
            if (code == KeyCode.UpArrow || code == KeyCode.DownArrow ||
                code == KeyCode.LeftControl || code == KeyCode.RightControl || 
                code == KeyCode.LeftApple) return orig(code);

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