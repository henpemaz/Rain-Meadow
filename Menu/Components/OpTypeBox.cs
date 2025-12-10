using Menu.Remix;
using Menu.Remix.MixedUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RainMeadow.UI.Components
{
    //Basically OpTextBox but uses the scrapped copy/paste features
    //rn just replaces the entire value from clipboard
    public class OpTypeBox : OpTextBox
    {
        public bool lastCopyHeld, lastPasteHeld;
        public OpTypeBox(ConfigurableBase config, Vector2 pos, float sizeX) : base(config, pos, sizeX)
        {

        }
        public override bool CopyFromClipboard(string value)
        {
            try
            {
                string prevValue = this.value;
                this.value = value;
                return this.value != prevValue;
            }
            catch
            {
                return false;
            }        
        }
        public override string CopyToClipboard() => value;
        public override void Update()
        {
            base.Update();
            if (!Focused) return;
            bool leftctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.LeftCommand);
            bool cpy = Input.GetKey(KeyCode.C) && leftctrl, pste = Input.GetKey(KeyCode.V) && leftctrl;
            if (pste && !lastPasteHeld)
            {
                if (!CopyFromClipboard(UniClipboard.GetText()))
                    PlaySound(SoundID.MENU_Greyed_Out_Button_Clicked);
            }
            else if (cpy && !lastCopyHeld)
            {
                string val = CopyToClipboard();
                UniClipboard.SetText(val);
            }

            lastCopyHeld = cpy;
            lastPasteHeld = pste;
        }
    }
}
