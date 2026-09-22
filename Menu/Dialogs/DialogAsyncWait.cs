using System.Collections.Generic;
using Menu;
using UnityEngine;

namespace RainMeadow.UI.Dialogs;

public class DialogAsyncWait : Dialog
{
    public AtlasAnimator loadingSpinner;

    public DialogAsyncWait(ProcessManager manager, string description, Vector2 size)
        : base(description, size, manager)
    {
        loadingSpinner = new AtlasAnimator(
            0,
            new Vector2(
                ((int)(pos.x + size.x / 2f)) - HorizontalMoveToGetCentered(manager),
                (int)(pos.y + size.y / 2f - 32f)
            ),
            "sleep",
            "sleep",
            20,
            true,
            false
        )
        {
            animSpeed = 0.25f,
            specificSpeeds = new Dictionary<int, float>(),
        };
        loadingSpinner.specificSpeeds[1] = 0.0125f;
        loadingSpinner.specificSpeeds[13] = 0.0125f;
        loadingSpinner.AddToContainer(container);
    }

    public override void Update()
    {
        base.Update();
        loadingSpinner.Update();
    }

    public virtual void RemoveSprites()
    {
        loadingSpinner.RemoveFromContainer();
    }

    public void SetText(string caption)
    {
        descriptionLabel.text = caption;
    }
}
