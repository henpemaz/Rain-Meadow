using Menu;
using UnityEngine;

namespace RainMeadow.UI.Components
{
    class ChatEmote : RectangularMenuObject, ButtonScroller.IPartOfButtonScroller
    {
        public float Alpha { get; set; }
        public Vector2 sourceSize = Vector2.zero;
        public Vector2 Pos { get => pos; set => pos = value; }
        public Vector2 Size { get => size + Margin; set {}  }
        public Vector2 Margin = new Vector2(40, 10);
        
        private FSprite[] sprites;

        public ChatEmote(MeadowProgression.Character character, MeadowProgression.Emote emote, Menu.Menu menu, MenuObject owner, Vector2 pos) : 
            base(menu, owner, pos, Vector2.zero)
        {
            if (!Futile.atlasManager.DoesContainAtlas("emotes_common"))
            {
                HeavyTexturesCache.futileAtlasListings.Add(Futile.atlasManager.LoadAtlas("illustrations/emotes/emotes_common").name);
            }
            var chardata = MeadowProgression.characterData[character];
            if (!Futile.atlasManager.DoesContainAtlas(chardata.emoteAtlas))
            {
                HeavyTexturesCache.futileAtlasListings.Add(Futile.atlasManager.LoadAtlas("illustrations/emotes/" + chardata.emoteAtlas).name);
            }

            this.sprites = new FSprite[2];
            string emote_sprite = (emote.value.StartsWith("emote") ? chardata.emotePrefix + emote.value : emote.value).ToLowerInvariant();
            string background_string = emote.value.StartsWith("emote") ? "emote_background" : "symbols_background";
            sprites[0] = new FSprite(background_string);
            sprites[1] = new FSprite(emote_sprite);
            this.sourceSize = sprites[0].element.sourceSize;
            this.size = sourceSize*0.35f;
            foreach (var sprite in sprites) 
            {
                sprite.SetAnchor(0f, 1.0f);
                sprite.shader = FShader.defaultShader;
                Container.AddChild(sprite);
            }
        }
        
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            sprites[0].alpha = Alpha*0.6f;
            sprites[1].alpha = Alpha;
            foreach (var item in sprites)
            {
                item.SetPosition(Margin + DrawPos(timeStacker));
                
                item.scaleX = (size.x / sourceSize.x);
                item.scaleY = size.y / sourceSize.y;
            }
            
        }

        public override void RemoveSprites()
        {
            base.RemoveSprites();
            foreach (var item in sprites) item.RemoveFromContainer();
        }
    
    }

    class ChatEmoteSpace : RectangularMenuObject, ButtonScroller.IPartOfButtonScroller
    {
        public ChatEmote origin;
        public ChatEmoteSpace(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size, ChatEmote origin) : base(menu, owner, pos, size)
        {
            this.origin = origin;
        }

        public float Alpha { get; set; }
        public Vector2 Pos { get; set; }
        public Vector2 Size { get => new Vector2(origin.Size.x, size.y); set => size = value; }
    }
}