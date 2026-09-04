using System;
using Menu;
using RainMeadow.UI.Components;
using UnityEngine;

namespace RainMeadow.UI.Dialogs;

public static class ScrollableDialog
{
    public class Notify : NotifyDialog, IScrollableDialog
    {
        public TextScroller TextScroller { get; }

        public Notify(
            ProcessManager manager,
            string title,
            Vector2 size,
            Action? onContinue = null,
            bool forceWrapping = false,
            float timeOut = 1
        )
            : base(manager, "", size, onContinue, forceWrapping, timeOut)
        {
            TextScroller = new TextScroller(
                this,
                dialogPage,
                dialogBox.pos + new Vector2(40, 80),
                size - new Vector2(80, 150),
                sliderSizeYOffset: -10
            )
            {
                greyOutWhenNoScroll = true,
            };

            MenuLabel titleLabel = new(
                this,
                dialogPage,
                Translate(title),
                UIUtils.ScreenCenter(manager) + new Vector2(0, size.y / 2 - 40),
                Vector2.zero,
                true
            );

            dialogPage.subObjects.AddRange([TextScroller, titleLabel]);
        }
    }

    public class Confirm : ConfirmCancelDialog, IScrollableDialog
    {
        public TextScroller TextScroller { get; }

        public Confirm(
            ProcessManager manager,
            string title,
            Vector2 size,
            Action? onConfirm = null,
            Action? onCancel = null,
            string confirmButtonText = "OK",
            string cancelButtonText = "CANCEL"
        )
            : base(manager, "", size, onConfirm, onCancel, confirmButtonText, cancelButtonText)
        {
            TextScroller = new TextScroller(
                this,
                dialogPage,
                dialogBox.pos + new Vector2(40, 80),
                size - new Vector2(80, 150),
                sliderSizeYOffset: -10
            )
            {
                greyOutWhenNoScroll = true,
            };

            MenuLabel titleLabel = new(
                this,
                dialogPage,
                Translate(title),
                UIUtils.ScreenCenter(manager) + new Vector2(0, size.y / 2 - 40),
                Vector2.zero,
                true
            );

            dialogPage.subObjects.AddRange([TextScroller, titleLabel]);
        }
    }

    public interface IScrollableDialog
    {
        TextScroller TextScroller { get; }
    }
}
