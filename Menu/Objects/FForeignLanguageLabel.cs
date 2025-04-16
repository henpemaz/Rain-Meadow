using Menu;
using UnityEngine;
using System;
using Menu.Remix.MixedUI;
using System.Collections;
using MonoMod.RuntimeDetour;
using System.Collections.Generic;
using System.Reflection;
using Mono.Cecil.Cil;

namespace RainMeadow
{
    // Label that supports (1) foreign language
    public class FForeignLanguageLabel : FLabel
    {
        public FForeignLanguageLabel(string fontName, string text) : this(fontName, text, new FTextParams())
        {
        }
        
        public FForeignLanguageLabel(string fontName, string text, FTextParams textParams) : base(fontName, text, textParams)
        {
        }

        public void SetFont(string fontName)
        {
            this._fontName = fontName;
            this._font = Futile.atlasManager.GetFontWithName(this._fontName);
            this._doesTextNeedUpdate = true;
            this._doesLocalPositionNeedUpdate = true;
        }
    }
}
