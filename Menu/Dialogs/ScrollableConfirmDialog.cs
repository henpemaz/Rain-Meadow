using Menu;
using RainMeadow.UI.Components;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RainMeadow
{
    /// <summary>
    /// A confirm/notify dialog whose body text lives inside a <see cref="ButtonScroller"/>, so long
    /// lists (e.g. mod mismatches) scroll instead of overflowing the box. Mirrors the base game
    /// DialogConfirm/DialogNotify flow: OK invokes <c>onOK</c>, Cancel invokes <c>onCancel</c>, and
    /// either one stops the side process afterwards.
    /// </summary>
    public class ScrollableConfirmDialog : Dialog
    {
        /// <summary>A single body line. Headers are drawn brighter/centered, body lines grey/left-aligned.</summary>
        public readonly struct Line
        {
            public readonly string text;
            public readonly bool header;
            public Line(string text, bool header = false)
            {
                this.text = text;
                this.header = header;
            }
            public static implicit operator Line(string text) => new(text);
        }

        private const float Margin = 20f;
        private const float TitleHeight = 40f;
        private const float ButtonAreaHeight = 55f;
        private const float LineHeight = 24f;
        private const float LineSpacing = 4f;

        private readonly Action? onOK;
        private readonly Action? onCancel;

        public ScrollableConfirmDialog(ProcessManager manager, string title, IEnumerable<Line> lines, Vector2 size,
            Action? onOK, Action? onCancel, bool showCancel) : base("", size, manager)
        {
            this.onOK = onOK;
            this.onCancel = onCancel;

            var titleLabel = new MenuLabel(this, pages[0], title,
                new Vector2(pos.x + size.x * 0.5f, pos.y + size.y - Margin - TitleHeight * 0.5f), new Vector2(0f, 0f), true);
            pages[0].subObjects.Add(titleLabel);

            float scrollTop = size.y - Margin - TitleHeight;
            float scrollHeight = scrollTop - ButtonAreaHeight;
            float scrollWidth = size.x - Margin * 2f - 30f; // leave room for the slider on the right
            var scroller = new ButtonScroller(this, roundedRect, new Vector2(Margin, ButtonAreaHeight), new Vector2(scrollWidth, scrollHeight), true)
            {
                greyOutWhenNoScroll = true,
                buttonHeight = LineHeight,
                buttonSpacing = LineSpacing,
            };
            roundedRect.SafeAddSubobjects(scroller);

            foreach (Line line in lines)
            {
                var label = new AlignedMenuLabel(this, scroller, line.text,
                    scroller.GetIdealPosWithScrollForButton(scroller.buttons.Count), new Vector2(scrollWidth, LineHeight), false)
                {
                    labelPosAlignment = line.header ? FLabelAlignment.Center : FLabelAlignment.Left
                };
                label.label.color = line.header ? MenuColorEffect.rgbWhite : MenuColorEffect.rgbMediumGrey;
                label.label.alignment = label.labelPosAlignment;
                label.label.anchorX = line.header ? 0.5f : 0f;
                scroller.AddScrollObjects(label);
            }

            float buttonY = pos.y + 15f;
            float buttonWidth = 110f;
            if (showCancel)
            {
                var okButton = new SimplerButton(this, pages[0], Translate("OK"),
                    new Vector2(pos.x + size.x * 0.5f - buttonWidth - 5f, buttonY), new Vector2(buttonWidth, 30f));
                okButton.OnClick += _ => Confirm();
                var cancelButton = new SimplerButton(this, pages[0], Translate("CANCEL"),
                    new Vector2(pos.x + size.x * 0.5f + 5f, buttonY), new Vector2(buttonWidth, 30f));
                cancelButton.OnClick += _ => Deny();
                pages[0].subObjects.Add(okButton);
                pages[0].subObjects.Add(cancelButton);
            }
            else
            {
                var okButton = new SimplerButton(this, pages[0], Translate("OK"),
                    new Vector2(pos.x + size.x * 0.5f - buttonWidth * 0.5f, buttonY), new Vector2(buttonWidth, 30f));
                okButton.OnClick += _ => Confirm();
                pages[0].subObjects.Add(okButton);
            }
        }

        private void Confirm()
        {
            onOK?.Invoke();
            manager.StopSideProcess(this);
        }

        private void Deny()
        {
            onCancel?.Invoke();
            manager.StopSideProcess(this);
        }
    }
}
