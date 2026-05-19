using Menu;
using MoreSlugcats;
using Rewired.UI;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    public class SlugIcon : PositionedMenuObject
    {
        public SlugcatStats.Name slugcat;
        public List<Color> colors;
        public bool dead;

        public Vector2 Anchor { get; set; }

        public float Scale = 1.0f;

        public FSprite baseSprite;
        public FSprite eyeSprite;
        public FSprite? featSprite;
        public SlugIcon(Menu.Menu menu, MenuObject owner, Vector2 pos, SlugcatStats.Name slugcat, List<Color> colors, bool dead = false) : base(menu, owner, pos)
        {
            if (!Futile.atlasManager.DoesContainAtlas("illustrations/slugicon"))
            {
                Futile.atlasManager.LoadAtlas("illustrations/slugicon");
            }

            this.slugcat = slugcat;
            this.colors = colors;
            this.dead = dead;

            Refresh();
        }

        //public SlugIcon(Menu.Menu menu, MenuObject owner, Vector2 pos, OnlinePlayer player) : base(menu, owner, pos)
        //{
        //    if (!Futile.atlasManager.DoesContainAtlas("illustrations/slugicon"))
        //    {
        //        Futile.atlasManager.LoadAtlas("illustrations/slugicon");
        //    }

        //    if (OnlineManager.lobby.clientSettings.TryGetValue(player, out var clientSettings) && clientSettings.TryGetData<SlugcatCustomization>(out var customization))
        //    {
        //        UpdateCustomization(customization);
        //    }
        //}

        public void Refresh()
        {
            if (baseSprite != null) baseSprite.RemoveFromContainer();
            if (eyeSprite != null) eyeSprite.RemoveFromContainer();
            if (featSprite != null) featSprite.RemoveFromContainer();

            baseSprite = GetBase(slugcat);
            eyeSprite = GetEyes(slugcat);
            featSprite = GetFeat(slugcat);

            baseSprite.anchorX = Anchor.x;
            baseSprite.anchorY = Anchor.y;

            eyeSprite.anchorX = Anchor.x;
            eyeSprite.anchorY = Anchor.y;

            featSprite?.anchorX = Anchor.x;
            featSprite?.anchorY = Anchor.y;

            //if (slugcat is null)
            //{
            //    Container.AddChild(baseSprite);
            //    Container.AddChild(eyeSprite);
            //    return;
            //}

            if (featSprite != null && slugcat == MoreSlugcatsEnums.SlugcatStatsName.Spear)
            {
                Container.AddChild(featSprite);
            }

            Container.AddChild(baseSprite);

            if (featSprite != null && slugcat == MoreSlugcatsEnums.SlugcatStatsName.Artificer)
            {
                Container.AddChild(featSprite);
            }

            Container.AddChild(eyeSprite);

            if (featSprite != null && (slugcat != MoreSlugcatsEnums.SlugcatStatsName.Spear && slugcat != MoreSlugcatsEnums.SlugcatStatsName.Artificer))
            {
                Container.AddChild(featSprite);
            }
        }

        public override void Update()
        {
            base.Update();

            ApplyPalette();
        }

        public void ApplyPalette()
        {

            if (colors.Count == 0)
            {
                baseSprite.color = PlayerGraphics.DefaultSlugcatColor(slugcat);
                return;
            }

            baseSprite.color = colors[0];
            eyeSprite.color = colors[1];

            if (featSprite == null) return;

            switch(slugcat.value)
            {
                default: featSprite.color = Color.white; break;

                case "Artificer":
                case "Rivulet":
                case "Spear":
                    if (colors.Count > 2)
                        featSprite.color = colors[2];
                    else
                        featSprite.color = Color.white;
                    break;
            }
        }

        // 24 x 24
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);

            baseSprite.x = DrawX(timeStacker) - 24f;
            baseSprite.y = DrawY(timeStacker);

            baseSprite.scale = Scale;

            eyeSprite.x = DrawX(timeStacker) - 24f;
            eyeSprite.y = DrawY(timeStacker);

            eyeSprite.scale = Scale;

            if (featSprite != null)
            {
                featSprite.x = DrawX(timeStacker) - 24f;
                featSprite.y = DrawY(timeStacker);

                featSprite.scale = Scale;
            }
        }

        public override void RemoveSprites()
        {
            baseSprite.RemoveFromContainer();
            eyeSprite.RemoveFromContainer();
            featSprite?.RemoveFromContainer();
            base.RemoveSprites();
        }

        private FSprite GetBase(SlugcatStats.Name name)
        {
            //if (customization is null) return new("basic_base");
            switch (name.value)
            {
                default: return new("basic_base");
                case "Red": return new("hunter_base");
                case "Gourmand": return new("gourmand_base");
                case "Saint": return new("saint_base");
            }
        }

        private FSprite GetEyes(SlugcatStats.Name name)
        {
            //if (customization is null) return new("basic_eyes");
            if (dead) return new("dead_eyes");
            switch (name.value)
            {
                default: return new("basic_eyes");
                case "Yellow": return new("monk_eyes");
                case "Red": return new("hunter_eyes");
                case "Artificer": return new("artificer_eyes");
                case "Saint": return new("saint_eyes");
            }
        }

        private FSprite? GetFeat(SlugcatStats.Name name)
        {
            //if (customization is null) return null;
            switch (name.value)
            {
                default: return null;
                case "Rivulet": return new("rivulet_feat");
                case "Spear": return new("spearmaster_feat");
                case "Artificer": return new("artificer_feat");
                case "Inv": return new("inv_feat");
            }
        }

        public static SlugIcon? FromPlayer(OnlinePlayer player, Menu.Menu menu, MenuObject owner)
        {
            SlugIcon? slugIcon = null;
            var id = OnlineManager.lobby.clientSettings[player].avatars.FirstOrDefault();
            if (id.FindEntity(true) is OnlineCreature oc && oc.TryGetData<SlugcatCustomization>(out var customization))
            {
                slugIcon = new(menu, owner, new(0, 0), customization.playingAs, new() { customization.SlugcatColor(), customization.eyeColor, customization.currentColors.Count > 2 ? customization.currentColors[2] : Color.white }, false);
            }
            return slugIcon;
        }
    }

    public class SlugIconHUD
    {
        public PlayerSpecificOnlineHud owner;

        public SlugcatStats.Name slugcat;
        public List<Color> colors;
        public bool dead;

        public FSprite baseSprite;
        public FSprite eyeSprite;
        public FSprite? featSprite;

        public Vector2 Anchor { get; set; }
        public float Scale = 1.0f;

        public SlugIconHUD(PlayerSpecificOnlineHud owner, SlugcatStats.Name slugcat, List<Color> colors, bool dead = false)
        {
            this.owner = owner;

            this.slugcat = slugcat;
            this.colors = colors;
            this.dead = dead;
        }

        public void Refresh()
        {
            if (baseSprite != null) baseSprite.RemoveFromContainer();
            if (eyeSprite != null) eyeSprite.RemoveFromContainer();
            if (featSprite != null) featSprite.RemoveFromContainer();

            baseSprite = GetBase(slugcat);
            eyeSprite = GetEyes(slugcat);
            featSprite = GetFeat(slugcat);

            baseSprite.anchorX = Anchor.x;
            baseSprite.anchorY = Anchor.y;

            eyeSprite.anchorX = Anchor.x;
            eyeSprite.anchorY = Anchor.y;

            featSprite?.anchorX = Anchor.x;
            featSprite?.anchorY = Anchor.y;

            //if (slugcat is null)
            //{
            //    Container.AddChild(baseSprite);
            //    Container.AddChild(eyeSprite);
            //    return;
            //}

            if (featSprite != null && slugcat == MoreSlugcatsEnums.SlugcatStatsName.Spear)
            {
                owner.hud.fContainers[0].AddChild(featSprite);
            }

            owner.hud.fContainers[0].AddChild(baseSprite);

            if (featSprite != null && slugcat == MoreSlugcatsEnums.SlugcatStatsName.Artificer)
            {
                owner.hud.fContainers[0].AddChild(featSprite);
            }

            owner.hud.fContainers[0].AddChild(eyeSprite);

            if (featSprite != null && (slugcat != MoreSlugcatsEnums.SlugcatStatsName.Spear && slugcat != MoreSlugcatsEnums.SlugcatStatsName.Artificer))
            {
                owner.hud.fContainers[0].AddChild(featSprite);
            }
        }

        private FSprite GetBase(SlugcatStats.Name name)
        {
            //if (customization is null) return new("basic_base");
            switch (name.value)
            {
                default: return new("basic_base");
                case "Red": return new("hunter_base");
                case "Gourmand": return new("gourmand_base");
                case "Saint": return new("saint_base");
            }
        }

        private FSprite GetEyes(SlugcatStats.Name name)
        {
            //if (customization is null) return new("basic_eyes");
            if (dead) return new("dead_eyes");
            switch (name.value)
            {
                default: return new("basic_eyes");
                case "Yellow": return new("monk_eyes");
                case "Red": return new("hunter_eyes");
                case "Artificer": return new("artificer_eyes");
                case "Saint": return new("saint_eyes");
            }
        }

        private FSprite? GetFeat(SlugcatStats.Name name)
        {
            //if (customization is null) return null;
            switch (name.value)
            {
                default: return null;
                case "Rivulet": return new("rivulet_feat");
                case "Spear": return new("spearmaster_feat");
                case "Artificer": return new("artificer_feat");
                case "Inv": return new("inv_feat");
            }
        }
    }
}
