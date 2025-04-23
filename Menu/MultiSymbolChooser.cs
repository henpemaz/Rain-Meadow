using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Menu;
using Menu.Remix;
using UnityEngine;

namespace RainMeadow
{
    public class MultiSymbolChooser<T> : SimplerSymbolButton, IHaveADescription
    {
        public string Description
        {
            get
            {
                return description ?? "";
            }
            set
            {
                if (description != value)
                {
                    description = value;
                    menu.infolabelDirty = true;
                }
            }
        }
        public int IndexOfObj(T enumValue)
        {
            for (int i = 0; i < listOfObjs?.Length; i++)
            {
                if (listOfObjs[i]?.Equals(enumValue) == true)
                {
                    return i;
                }
            }
            return -1;
        }
        public T CurrentObj
        {
            get
            {
                return currentObj;
            }
            set
            {
                if (!(currentObj?.Equals(value) == true))
                {
                    currentObj = value;
                    uponRecievingNewObj?.Invoke(this, currentObj);
                    Description = GetDesiredDescription();
                }
            }
        }
        public int MaxLength => listOfObjs?.Length ?? 0;
        public MultiSymbolChooser(Menu.Menu menu, MenuObject owner, string startingSymbol, string singalText, Vector2 pos, Vector2 size, T currentObj, T[] list) : base(menu, owner, startingSymbol, singalText, pos)
        {
            this.size = size;
            listOfObjs = list;
            this.currentObj = currentObj;
            OnClick += (_) =>
            {
                int nextIndex = IndexOfObj(this.currentObj);
                nextIndex = nextIndex + 1 >= MaxLength ? 0 : nextIndex + 1;
                CurrentObj = listOfObjs[nextIndex];

            };
        }
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            roundedRect.size = size;
        }
        public virtual string GetOwnDescription()
        {
            return "";
        }
        public string GetDesiredDescription()
        {
            return desiredDescription?.Invoke(this) ?? GetOwnDescription();
        }
        public T[] listOfObjs;
        public T currentObj;
        public string? description;
        public Func<MultiSymbolChooser<T>, string>? desiredDescription;
        public Action<MultiSymbolChooser<T>, T>? uponRecievingNewObj;
    }
    public class EnumSymbolChooser<T> : MultiSymbolChooser<T>
    {
        public EnumSymbolChooser(Menu.Menu menu, MenuObject owner, string startingSymbol, string singalText, Vector2 pos, Vector2 size, T currentEnum) : base(menu, owner, startingSymbol, singalText, pos, size, currentEnum, typeof(T).IsEnum? [..Enum.GetValues(typeof(T)).Cast<T>()] : typeof(T).IsExtEnum()? [..ExtEnumBase.GetNames(typeof(T)).Select(x => Activator.CreateInstance(typeof(T), x, false)).Cast<T>()] :  throw new ElementFormatException("Use multi-chooser instead"))
        {

        }
        public EnumSymbolChooser(Menu.Menu menu, MenuObject owner, string startingSymbol, string singalText, Vector2 pos, Vector2 size, T currentEnum, T[] enumArray) : base(menu, owner, startingSymbol, singalText, pos, size, currentEnum, enumArray)
        {
            if (!typeof(T).IsEnum && !typeof(T).IsExtEnum())
            {
                throw new ElementFormatException("Use MultiSymbolChooser instead");
            }
        }
        public override string GetOwnDescription()
        {
            return CurrentObj != null? typeof(T).IsEnum? Enum.GetName(typeof(T), CurrentObj) : CurrentObj.ToString() : "";
        }
    }
}
