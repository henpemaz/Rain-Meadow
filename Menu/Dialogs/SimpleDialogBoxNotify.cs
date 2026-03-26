using Menu;
namespace RainMeadow
{
    /// <summary>
    /// A simple, self-contained dialog box that destroys itself upon pressing Continue. Automatically translates input.
    /// Merely displays text and can be cleared, it does NOT block mouse interaction with other UI elements.
    /// </summary>
    public class SimpleDialogBoxNotify : DialogBoxNotify
    {
        private bool continueEverClicked;
        public SimpleDialogBoxNotify(Menu.Menu menu, MenuObject owner, string dialogText, string buttonText = "CONTINUE", bool forceWrapping = true, bool disableTimeOut = true)
            : base(menu, owner, dialogText, "", new(menu.manager.rainWorld.options.ScreenSize.x / 2f - 240f + (1366f - menu.manager.rainWorld.options.ScreenSize.x) / 2f, 224f), new(480f, 320f), forceWrapping)
        {
            owner.subObjects.Add(this);
            continueEverClicked = false;
            descriptionLabel.text = menu.LongTranslate(descriptionLabel.text);
            continueButton.menuLabel.text = menu.Translate(buttonText);
            timeOut = disableTimeOut ? 0f : timeOut;
        }
        public override void Update()
        {
            base.Update();
            if (continueButton.buttonBehav.clicked)
            {
                continueEverClicked = true;
            }
            else if (continueEverClicked && !continueButton.buttonBehav.clicked)
            {
                RemoveSprites();
            }
        }
        public override void RemoveSprites()
        {
            base.RemoveSprites();
            owner.subObjects.Remove(this);
        }
    }
}
