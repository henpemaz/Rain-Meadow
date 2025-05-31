using Menu;
using Menu.Remix.MixedUI;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.Collections;
using Rewired;
using RWCustom;

namespace RainMeadow.UI.Components
{
    //supports multi view, cuz previous one does not and its messy af
    public class ChatTextBox2 : ButtonTemplate, ICanBeTyped
    {
        public int VisibleTextLimit => visibleTextLimit ?? Mathf.FloorToInt(menuLabel.size.x / Mathf.Max(LabelTest.GetWidth(currentMessage) / Mathf.Max(currentMessage.Length, 1), 1));
        public bool SelectionActive => selectionStartPos != -1 ;
        public bool IgnoreSelect => focused && !menu.manager.menuesMouseMode;
        public Action OnTextSubmit => onTextSubmit ?? HandleTextSubmit;
        public Action<char> OnKeyDown { get; set; }
        public ChatTextBox2(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size) : base(menu, owner, pos, size)
        {
            roundedRect = new(menu, this, Vector2.zero, size, true);
            selectionSprite = new("pixel")
            {
                height = 14,
                width = 0,
                color = Color.grey,
                x = 0,
                anchorX = 0,
                scaleY = (this.size.y * 0.5f) - 1
            };
            Container.AddChild(selectionSprite);
            menuLabel = new(menu, this, "", new(10, 0), new(size.x - 30, size.y), false)
            { labelPosAlignment = FLabelAlignment.Left };
            menuLabel.label.alignment = FLabelAlignment.Left;

            cursorSprite = new("modInputCursor")
            {
                x = menuLabel.size.x,
                y = this.size.y * 0.5f
            };
            Container.AddChild(cursorSprite);

            subObjects.AddRange([roundedRect, menuLabel]);
            gameObject ??= new();
            typingHandler ??= gameObject.AddComponent<ButtonTypingHandler>();
            OnKeyDown = (Action<char>)Delegate.Combine(OnKeyDown, new Action<char>(CaptureInputs));
            typingHandler.Assign(this);
        }
        public override void RemoveSprites()
        {
            base.RemoveSprites();
            cursorSprite.RemoveFromContainer();
            selectionSprite.RemoveFromContainer();
        }
        public override void Clicked()
        {
            base.Clicked();
            if (IgnoreSelect) return;
            if (buttonBehav.clicked) focused = !focused;
        }
        public override void Update()
        {
            base.Update();
            buttonBehav.Update();
            if ((menu.pressButton && menu.manager.menuesMouseMode && !buttonBehav.clicked) || buttonBehav.greyedOut) focused = false;
            if (menu.allowSelectMove) menu.allowSelectMove = !focused;
            UpdateSelection();
            roundedRect.fillAlpha = 1.0f;
            roundedRect.addSize = new Vector2(5f, 3f) * (buttonBehav.sizeBump + 0.5f * Mathf.Sin(buttonBehav.extraSizeBump * 3.14f)) * (buttonBehav.clicked && !IgnoreSelect ? 0 : 1);
            cursorIsInMiddle = cursorPos < currentMessage.Length;
            maxVisibleLength = VisibleTextLimit;
        }
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);

            Vector2 screenPos = ScreenPos;
            roundedRect.size = size;
            menuLabel.size = new(size.x - 30, size.y);
            for (int i = 0; i < 9; i++)
            {
                roundedRect.sprites[i].color = Color.black;
            }
            int firstLetterViewed = cursorPos > maxVisibleLength ? cursorPos - maxVisibleLength : 0,
                lastLetterViewed = Mathf.Max(0, cursorPos > maxVisibleLength ? maxVisibleLength : Mathf.Min(maxVisibleLength, currentMessage.Length));

            menuLabel.text = currentMessage.Substring(firstLetterViewed, lastLetterViewed);
            menuLabel.label.color = InterpColor(timeStacker, labelColor ?? Menu.Menu.MenuColor(Menu.Menu.MenuColors.White));

            int lowestCursorPos = SelectionActive ? Mathf.Min(cursorPos, selectionStartPos) : cursorPos;
            float cursorPosition = LabelTest.GetWidth(menuLabel.label.text.Substring(0, lowestCursorPos > maxVisibleLength ? menuLabel.label.text.Length : lowestCursorPos), false);
            if (cursorIsInMiddle)
            {
                if (cursorSprite.element.name != "pixel") cursorSprite.SetElementByName("pixel");
                cursorSprite.x = cursorPosition + 10 + screenPos.x;
                cursorSprite.height = 13;
            }
            else
            {
                if (cursorSprite.element.name != "modInputCursor") cursorSprite.SetElementByName("modInputCursor");
                cursorSprite.x = cursorPosition + 15 + screenPos.x;
                cursorSprite.height = 6;
            }
            cursorSprite.y = screenPos.y + size.y / 2;
            cursorSprite.alpha = focused ? Mathf.PingPong(Time.time * 4, 1) : 0;
            selectionSprite.alpha = focused ? 1 : 0;

            if (selectionStartPos == -1)
            {
                cursorSprite.isVisible = true;
                selectionSprite.isVisible = false;
            }
            else
            {
                int start = lowestCursorPos > firstLetterViewed ? lowestCursorPos - firstLetterViewed : firstLetterViewed > 0 ? 0 : lowestCursorPos;
                cursorPosition = LabelTest.GetWidth(menuLabel.text.Substring(0, start));
                float width = LabelTest.GetWidth(menuLabel.text.Substring(start, Mathf.Min(Mathf.Abs(selectionStartPos - cursorPos), maxVisibleLength - start)), false);
                cursorSprite.isVisible = false;
                selectionSprite.isVisible = true;
                selectionSprite.x = cursorPosition + screenPos.x + 10;
                selectionSprite.y = screenPos.y + size.y / 2;
                selectionSprite.width = width;
            }

        }
        public void CaptureInputs(char input)
        {
            if (!focused) return;
            // the "Delete" character, which is emitted by most - but not all - operating systems when ctrl and backspace are used together
            if (input == '\u007F') return;
            string msg = currentMessage;
            ChatTextBox.blockInput = false;
            //u0008 backspace in unicode
            if ((input == '\b' || input == '\u0008') && !(Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)))
            {
                if (cursorPos > 0 || selectionStartPos != -1)
                {
                    menu.PlaySound(SoundID.MENY_Already_Selected_MultipleChoice_Clicked);
                    if (selectionStartPos != -1)  //delete selected text
                    {
                        DeleteSelectedText();
                        if (cursorPos == currentMessage.Length) cursorIsInMiddle = false;
                    }
                    else
                    {
                        currentMessage = msg.Remove(cursorPos - 1, 1);
                        cursorPos--;
                    }
                }
            }
            else if ((input == '\n' || input == '\r'))
            {
                if (msg.Length > 0 && !string.IsNullOrWhiteSpace(msg))
                {
                    // /n is type a new line, not supported and usually its ENTER, so we sending message. sending to players if messg has one letter
                    MatchmakingManager.currentInstance.SendChatMessage(msg);
                    foreach (var player in OnlineManager.players)
                    {
                        player.InvokeRPC(RPCs.UpdateUsernameTemporarily, msg);
                    }
                    focused = false;
                    OnTextSubmit();
                }
            }
            else  //any other character, lets type
            {
                if (selectionStartPos != -1) // replaces the selected text with the emitted character
                {
                    menu.PlaySound(SoundID.MENU_Checkbox_Check);
                    DeleteSelectedText();
                    currentMessage = currentMessage.Insert(cursorPos, input.ToString());
                    cursorPos++;
                    if (cursorPos == currentMessage.Length) cursorIsInMiddle = false;
                }
                else if (msg.Length < textLimit)
                {
                    menu.PlaySound(SoundID.MENU_Checkbox_Check);
                    currentMessage = msg.Insert(cursorPos, input.ToString());
                    cursorPos++;
                }
            }
            ChatTextBox.blockInput = true;
        }
        public void UpdateSelection()
        {
            ChatTextBox.ShouldCapture(focused);
            string msg = currentMessage;
            int len = msg.Length;
            if (len > 0)
            {
                ChatTextBox.blockInput = false; //orelse get key wont be set to true
                // ctrl backspace stuff here instead of CaptureInputs, because ctrl + backspace doesn't always emit a capturable character on some operating systems
                if (Input.GetKey(KeyCode.Backspace) && (cursorPos > 0 || selectionStartPos != -1))
                {
                    if ((Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) && backspaceHeld == 0) //remove everything
                    {
                        currentMessage = "";
                        cursorPos = 0;
                        selectionStartPos = -1;
                    }
                    else if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && (backspaceHeld == 0 || (backspaceHeld >= 30 && (backspaceHeld % 1 == 0))))
                    {
                        if (selectionStartPos != -1) //remove selected message
                        {
                            menu.PlaySound(SoundID.MENY_Already_Selected_MultipleChoice_Clicked);
                            DeleteSelectedText();
                        }
                        else if (cursorPos > 0)
                        {
                            menu.PlaySound(SoundID.MENY_Already_Selected_MultipleChoice_Clicked);
                            int space = msg.Substring(0, cursorPos - 1).LastIndexOf(' ') + 1;
                            currentMessage = msg.Remove(space, cursorPos - space);
                            cursorPos = space;
                        }
                    }
                    backspaceHeld++;
                }
                else if (Input.GetKey(KeyCode.Delete))
                {
                    if (selectionStartPos != -1)
                    {
                        menu.PlaySound(SoundID.MENY_Already_Selected_MultipleChoice_Clicked);
                        DeleteSelectedText();
                    }
                    else if ((backspaceHeld == 0 || (backspaceHeld >= 30 && (backspaceHeld % 2 == 0))) && cursorPos < msg.Length)
                    {
                        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                        {
                            menu.PlaySound(SoundID.MENY_Already_Selected_MultipleChoice_Clicked);
                            int space = msg.Substring(cursorPos, len - cursorPos).IndexOf(' ');
                            currentMessage = msg.Remove(cursorPos, (space < 0 || space >= len) ? (space = len - cursorPos) : space + 1);
                        }
                        else
                        {
                            menu.PlaySound(SoundID.MENY_Already_Selected_MultipleChoice_Clicked);
                            currentMessage = msg.Remove(cursorPos, 1);
                        }
                    }
                    backspaceHeld++;
                }
                else
                {
                    backspaceHeld = 0;
                    if (Input.GetKeyDown(KeyCode.Home))
                    {
                        cursorPos = 0;
                        selectionStartPos = -1;
                    }
                    else if (Input.GetKeyDown(KeyCode.End) && cursorPos < len)
                    {
                        cursorPos = len;
                        selectionStartPos = -1;
                    }
                    else if (Input.GetKeyDown(KeyCode.A) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
                    {
                        cursorPos = msg.Length;
                        selectionStartPos = 0;
                    }

                    else if (Input.GetKey(KeyCode.LeftArrow))
                    {
                        // cursor position is used as the anchor for selection
                        if ((cursorPos > 0 || SelectionActive) && arrowHeld == 0 || (arrowHeld >= 30 && (arrowHeld % 1 == 0)))
                        {
                            var shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                            if (SelectionActive && !shiftHeld) selectionStartPos = -1;
                            else if (!SelectionActive || cursorPos > 0)
                            {
                                if (!SelectionActive && shiftHeld) selectionStartPos = cursorPos;
                                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                                {
                                    cursorPos = msg.Substring(0, cursorPos - 1).LastIndexOf(' ') + 1;
                                }
                                else cursorPos--;
                            }
                        }
                        arrowHeld++;
                    }
                    else if (Input.GetKey(KeyCode.RightArrow))
                    {
                        if ((cursorPos < len || SelectionActive) && arrowHeld == 0 || arrowHeld >= 30 && (arrowHeld % 1 == 0))
                        {
                            bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                            if (SelectionActive && !shiftHeld)
                            {
                                if (selectionStartPos > cursorPos) cursorPos = selectionStartPos;
                                selectionStartPos = -1;
                            }
                            else if (!SelectionActive || cursorPos < len)
                            {
                                if (!SelectionActive && shiftHeld) selectionStartPos = cursorPos;
                                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                                {
                                    int space = msg.Substring(cursorPos, len - cursorPos - 1).IndexOf(' ');
                                    cursorPos = space < 0 || space >= len ? len : space + cursorPos + 1;
                                }
                                else cursorPos++;
                            }
                        }
                        arrowHeld++;
                    }
                    else arrowHeld = 0;
                }
                ChatTextBox.blockInput = false;
            }
            cursorPos = Mathf.Clamp(cursorPos, 0, currentMessage.Length);
            selectionStartPos = selectionStartPos > currentMessage.Length ? currentMessage.Length : selectionStartPos;
        }
        public void DeleteSelectedText()
        {
            currentMessage = currentMessage.Remove(Mathf.Min(selectionStartPos, cursorPos), Mathf.Abs(selectionStartPos - cursorPos));
            if (selectionStartPos < cursorPos) cursorPos = selectionStartPos;
            selectionStartPos = -1;
        }
        public void HandleTextSubmit()
        {
            ChatTextBox.blockInput = false;
            focused = false;
            currentMessage = "";
            cursorPos = 0;
            selectionStartPos = -1;
        }
        public void DelayedUnload(float delay)
        {
            if (isUnloading) return;
            cursorPos = 0;
            isUnloading = true;
            typingHandler.StartCoroutine(Unload(delay));

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

        private int cursorPos, selectionStartPos = -1, backspaceHeld, arrowHeld, maxVisibleLength; //cursorPos follows exact num of letters not the num of letters viewed, selection position is -1 when nothing is selected
        public int? visibleTextLimit;
        public int textLimit = 75;
        public bool focused, cursorIsInMiddle, isUnloading;
        public string currentMessage = "";
        public HSLColor? labelColor;
        public FSprite cursorSprite, selectionSprite;
        public AlignedMenuLabel menuLabel;
        public RoundedRect roundedRect;
        public GameObject gameObject;
        public ButtonTypingHandler typingHandler;
        public event Action? onTextSubmit;
    }
}
