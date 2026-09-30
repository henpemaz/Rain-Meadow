using System;
using Menu;
using RWCustom;
using UnityEngine;

namespace RainMeadow.UI.Components.Base;

/// <summary>
/// This class is completely unaware of anything outside itself. It relies entirely on
/// <see cref="IEventfulSelectOneButtonOwner"/> to provide it with selection information in order to manage its state.
/// </summary>
public class EventfulSelectOneButton : EventfulButton
{
    /// <summary>
    /// It's recommended to store a collection of buttons in a group and a private field/setter property to provide the
    /// methods with information.
    /// </summary>
    public interface IEventfulSelectOneButtonOwner
    {
        void SetSelectedSelectOneButton(string groupKey, EventfulSelectOneButton button);

        EventfulSelectOneButton? GetSelectedSelectOneButton(string groupKey);
    }

    public RoundedRect outerRect;

    public bool handleSelectedColInChild;
    public float selectedCol,
        lastSelectedCol;
    public string groupKey;

    public IEventfulSelectOneButtonOwner? GroupManager
    {
        get
        {
            if (owner is IEventfulSelectOneButtonOwner ownerAsGroupManager)
                return ownerAsGroupManager;
            if (menu is IEventfulSelectOneButtonOwner menuAsGroupManager)
                return menuAsGroupManager;

            RainMeadow.Debug(
                "EventfulSelectOneButton doesn't have an IEventfulSelectOneButtonOwner to report to!"
            );
            return null;
        }
    }

    public new event Action<EventfulSelectOneButton>? OnClick;

    public bool SelectedInGroup =>
        ReferenceEquals(GroupManager?.GetSelectedSelectOneButton(groupKey), this);

    public EventfulSelectOneButton(
        Menu.Menu menu,
        MenuObject owner,
        string displayText,
        Vector2 pos,
        Vector2 size,
        string groupKey,
        string description = "",
        Action<EventfulSelectOneButton>? onClick = null
    )
        : base(menu, owner, displayText, pos, size, description)
    {
        this.groupKey = groupKey;
        OnClick = onClick;

        outerRect = new RoundedRect(menu, this, new Vector2(0f, 0f), size, filled: false);
        subObjects.Add(outerRect);
    }

    public override Color MyColor(float timeStacker)
    {
        return Color.Lerp(
            Menu.Menu.MenuRGB(Menu.Menu.MenuColors.DarkGrey),
            base.MyColor(timeStacker),
            Mathf.Lerp(lastSelectedCol, selectedCol, timeStacker)
        );
    }

    public override void Clicked()
    {
        if (SelectedInGroup)
            menu.PlaySound(SoundID.MENY_Already_Selected_MultipleChoice_Clicked);
        else
            menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);

        GroupManager?.SetSelectedSelectOneButton(groupKey, this);
        OnClick?.Invoke(this);
    }

    public override void Update()
    {
        base.Update();
        lastSelectedCol = selectedCol;

        outerRect.addSize =
            new Vector2(8f, 8f)
                * (1f + 0.5f * Mathf.Sin(buttonBehav.extraSizeBump * (float)Math.PI))
                * (SelectedInGroup ? 1f : 0f)
            + new Vector2(10f, 6f) * buttonBehav.sizeBump * (buttonBehav.clicked ? 0f : 1f);

        if (handleSelectedColInChild)
            return;

        if (SelectedInGroup)
        {
            if (
                menu.selectedObject is EventfulSelectOneButton selectOneButton
                && selectOneButton.groupKey == groupKey
            )
                buttonBehav.col = 1f;

            selectedCol = Custom.LerpAndTick(selectedCol, 1f, 0.06f, 0.05f);
        }
        else if (Selected)
            selectedCol = Custom.LerpAndTick(selectedCol, 1f, 0.06f, 0.05f);
        else
            selectedCol = Custom.LerpAndTick(selectedCol, 0f, 0.06f, 0.05f);
    }
}
