using System.Collections.Generic;
using System.Linq;
using Menu;
using RainMeadow.UI.Components.Base;
using RWCustom;
using UnityEngine;

namespace RainMeadow.UI.Components;

public class SlugIcon : PositionedMenuObject
{
    /// <summary>
    /// Sprites are drawn on top of each other from earlier items in list to later, so use that<br/>
    /// to control layering. Colours are given by name of sprite (ending in _head gets base<br/>
    /// colour, ending in _face gets eye colour, ending in _feature gets feature colour if applicable)
    /// </summary>
    public static Dictionary<string, List<string>> SlugcatToSpriteNames { get; set; } =
        new()
        {
            { "White", ["basic_head", "basic_face"] },
            { "Yellow", ["basic_head", "monk_face"] },
            { "Red", ["hunter_head", "hunter_face"] },
            { "Artificer", ["basic_head", "artificer_feature", "artificer_face"] },
            { "Gourmand", ["gourmand_head", "basic_face"] },
            { "Spear", ["spearmaster_feature", "basic_head", "basic_face"] },
            { "Rivulet", ["basic_head", "basic_face", "rivulet_feature"] },
            { "Saint", ["saint_head", "saint_face"] },
            { "Inv", ["basic_head", "basic_face", "inv_feature"] },
            { "Watcher", ["basic_head", "basic_face", "watcher_feature"] },
        };

    /// <summary>
    /// First hex is head colour, second is face colour, third is an optional feature colour. Don't<br/>
    /// add a hex or make it null if you have a coloured sprite and want to preserve its colour.<br/>
    /// <br/>
    /// The difference in colour mapping and sprite mapping comes from the way slugcat colour is<br/>
    /// handled everywhere else.
    /// </summary>
    public static Dictionary<string, List<string?>> SlugcatToDefaultColors { get; set; } =
        new()
        {
            { "White", ["FFFFFF", "101010"] },
            { "Yellow", ["FFFF73", "101010"] },
            { "Red", ["FF7373", "101010"] },
            { "Artificer", ["70233B", "FFFFFF", "45283C"] },
            { "Gourmand", ["F0C197", "101010"] },
            { "Spear", ["4F2E69", "FFFFFF"] },
            { "Rivulet", ["91CCF0", "101010", "DF2DEA"] },
            { "Saint", ["AAF156", "101010"] },
            { "Inv", ["17244F", "FFFFFF"] },
            { "Watcher", ["17234F", "FFFFFF"] },
        };

    public List<PositionedSprite> sprites = [];
    public Dictionary<string, int> spriteLayerToColorIndex = [];
    public List<Color>? colors = [];

    public string slugcat = "";

    public SlugIcon(
        Menu.Menu menu,
        MenuObject owner,
        Vector2 pos,
        string slugcat,
        List<Color>? colors = null,
        bool dead = false
    )
        : base(menu, owner, pos)
    {
        Futile.atlasManager.LoadAtlas("illustrations/slugicons");

        this.colors = colors;

        DrawScugSprites(slugcat, dead);
    }

    public void ClearSprites()
    {
        for (int i = 0; i < sprites.Count; i++)
            this.ClearMenuObject(sprites[i]);
        sprites = [];
        spriteLayerToColorIndex = [];
    }

    public void DrawScugSprites(string? newSlugcat = null, bool dead = false)
    {
        ClearSprites();

        if (newSlugcat != null)
            slugcat = newSlugcat;

        if (slugcat == "")
            return;

        List<string> spriteNames = SlugcatToSpriteNames.TryGetValue(slugcat, out List<string> names)
            ? names
            : ["basic_head", "basic_face", "modded_feature"];
        if (dead)
            spriteNames[spriteNames.FindIndex(name => name.EndsWith("_face"))] = "dead_face";

        for (int i = 0; i < spriteNames.Count; i++)
        {
            string spriteName = spriteNames[i];
            sprites.Add(new PositionedSprite(menu, this, Vector2.zero, new FSprite(spriteName)));
            spriteLayerToColorIndex.Add(spriteName.Split('_').Last(), i);
        }

        subObjects.AddRange([.. sprites]);
        ApplyPalette();
    }

    public void ApplyPalette(List<Color>? newColors = null)
    {
        this.colors = newColors;

        if (sprites.Count == 0)
            return;

        List<Color> colors = this.colors ?? [];

        if (colors.Count < 2)
        {
            if (!SlugcatToDefaultColors.TryGetValue(slugcat, out List<string?> hexColors))
            {
                RainMeadow.Debug(
                    "Not enough colours were provided to SlugIcon and no default colours were found for the given slugcat, using fallback colours"
                );
                hexColors = ["FFFFFF", "101010", "FFFFFF"];
            }
            colors = [.. hexColors.Select(hex => Custom.hexToColor(hex ?? "FFFFFF"))];
        }

        void TryMapColorsToSprites(string spritePart, Color color)
        {
            if (spriteLayerToColorIndex.TryGetValue(spritePart, out int index))
                sprites[index].Sprite.color = color;
        }

        TryMapColorsToSprites("head", colors[0]);
        TryMapColorsToSprites("face", colors[1]);
        if (colors.Count > 2)
            TryMapColorsToSprites("feature", colors[2]);
    }
}
