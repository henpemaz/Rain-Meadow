using Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RWCustom;
using UnityEngine;

namespace RainMeadow
{
    /// <summary>
    /// Based on ColorSlugcatDialog, but adds support multiple slugcats. List of slugcats count shouldnt be 0
    /// </summary>
    public class ColorMultipleSlugcatsDialog : ColorSlugcatDialog
    {
        public bool SlugcatPagesOn => selectableSlugcats.Count > 1;
        public ColorMultipleSlugcatsDialog(ProcessManager manager, Action onOK, List<SlugcatStats.Name> colorableSlugcats, int slugcatIndex = -1) : this(manager, onOK, colorableSlugcats, colorableSlugcats[Mathf.Max(slugcatIndex, 0)]) { }
        public ColorMultipleSlugcatsDialog(ProcessManager manager, Action onOK, List<SlugcatStats.Name> colorableSlugcats, SlugcatStats.Name? slugcat) : base(manager, slugcat ?? colorableSlugcats[0], onOK)
        {
            selectableSlugcats = colorableSlugcats;
            if (slugcat != null && !selectableSlugcats.Contains(slugcat))
            {
                RainMeadow.Debug("slugcat choosen first isnt part of the slugcat list? adding it if it was unintended. be warned for your original list used for the parameter");
                selectableSlugcats.Insert(0, slugcat);
            }
            if (SlugcatPagesOn) ActivateSlugcatButtons();
        }
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            if (slugcatPageStater != null)
            {
                slugcatPageStater.text = Translate(SlugcatStats.getSlugcatName(id));
                slugcatPageStater.label.color = MenuColorEffect.rgbMediumGrey;
            }
        }
        public void GotoNextPrevSlugcat(bool next)
        {
            PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
            int currentIndex = selectableSlugcats.IndexOf(id);
            int nextIndex = next ? (currentIndex + 1 >= selectableSlugcats.Count ? 0 : currentIndex + 1) : 
                (currentIndex - 1 < 0 ? selectableSlugcats.Count - 1 : currentIndex - 1);
            ChangeSlugcat(nextIndex);
        }
        public void ChangeSlugcat(int index)
        {
            RemoveColorButtons();
            id = selectableSlugcats[index];
            GetSaveColorEnabled();
        }
        public void ActivateSlugcatButtons()
        {
            if (prevSlugcatButton == null)
            {
                prevSlugcatButton = new(this, pages[0], "Menu_Symbol_Arrow", $"Prev{NextPrevSingal}", new(pos.x + size.x * 0.75f + 5, okButton.pos.y + 3));
                prevSlugcatButton.OnClick += _ => GotoNextPrevSlugcat(false);
                prevSlugcatButton.symbolSprite.rotation = 270;
                pages[0].subObjects.Add(prevSlugcatButton);
            }
            if (nextSlugcatButton == null)
            {
                nextSlugcatButton = new(this, pages[0], "Menu_Symbol_Arrow", $"Next{NextPrevSingal}", new(prevSlugcatButton.pos.x + prevSlugcatButton.size.x + 10, prevSlugcatButton.pos.y));
                nextSlugcatButton.OnClick += _ => GotoNextPrevSlugcat(true);
                nextSlugcatButton.symbolSprite.rotation = 90;
                pages[0].subObjects.Add(nextSlugcatButton);
            }
            if (slugcatPageStater == null)
            {
                slugcatPageStater = new(this, pages[0], "", new(prevSlugcatButton.pos.x + ((nextSlugcatButton.pos.x - prevSlugcatButton.pos.x + prevSlugcatButton.size.x) / 2) - 40, nextSlugcatButton.pos.y  + nextSlugcatButton.size.y + 10), new(80, 30), true);
                pages[0].subObjects.Add(slugcatPageStater);
            }
            MutualHorizontalButtonBind(prevSlugcatButton, nextSlugcatButton);
            MutualHorizontalButtonBind(okButton, prevSlugcatButton);
        }
        public void DeactivateSlugcatButtons()
        {
            pages[0].ClearMenuObject(ref prevSlugcatButton);
            pages[0].ClearMenuObject(ref nextSlugcatButton);
            pages[0].ClearMenuObject(ref slugcatPageStater);
        }
        public const string NextPrevSingal = "PageSlugcat_COLORMULTIPLESLUGCATS";
        public List<SlugcatStats.Name> selectableSlugcats;
        public MenuLabel? slugcatPageStater;
        public SimplerSymbolButton? prevSlugcatButton, nextSlugcatButton;
    }
}
