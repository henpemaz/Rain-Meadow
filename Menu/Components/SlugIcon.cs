using System.Collections.Generic;
using System.Linq;
using RWCustom;
using UnityEngine;

namespace RainMeadow.UI.Components;

/// <summary>
/// A container of 2-3 sprites to draw a little slugcat icon. Use PositionedSlugIcon and SlugIconHud<br/>
/// to use this in menus and in-game HUD respectively.<br/>
/// <br/>
/// SlugcatNameToSpriteNames and SlugcatNameToDefaultColors can have entries added to them to support<br/>
/// modded slugcats in both the aforementioned SlugIcon components.
/// </summary>
public class SlugIcon
{
    /// <summary>
    /// Sprites are drawn on top of each other from earlier items in list to later, so use that<br/>
    /// to control layering. Colours are given by name of sprite (ending in _head gets base<br/>
    /// colour, ending in _face gets eye colour, ending in _feature gets feature colour if applicable)
    /// </summary>
    public static Dictionary<string, List<string>> SlugcatNameToSpriteNames { get; set; } =
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
    /// handled everywhere else in Rain World.
    /// </summary>
    public static Dictionary<string, List<string?>> SlugcatNameToDefaultColors { get; set; } =
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

    public FContainer container = new();
    public List<FSprite> sprites = [];
    public List<Color>? colors;
    public Dictionary<string, int> spriteLayerNameToIndex = [];

    public string slugcatName = "";

    public SlugIcon(string slugcatName, List<Color>? colors = null, bool dead = false)
    {
        Futile.atlasManager.LoadAtlas("illustrations/slugicons");
        this.colors = colors;
        DrawScugSprites(slugcatName, dead);
    }

    public void ClearSprites()
    {
        foreach (FSprite sprite in sprites)
            sprite.RemoveFromContainer();
        sprites.Clear();
        spriteLayerNameToIndex.Clear();
    }

    public void RemoveFromContainer()
    {
        ClearSprites();
        container.RemoveFromContainer();
    }

    public void DrawScugSprites(string? newSlugcatName = null, bool dead = false)
    {
        ClearSprites();

        if (newSlugcatName != null)
            slugcatName = newSlugcatName;

        if (slugcatName == "")
            return;

        List<string> spriteNames = SlugcatNameToSpriteNames.TryGetValue(
            slugcatName,
            out List<string> names
        )
            ? names
            : ["basic_head", "basic_face", "modded_feature"];
        if (dead)
            spriteNames[spriteNames.FindIndex(name => name.EndsWith("_face"))] = "dead_face";

        for (int i = 0; i < spriteNames.Count; i++)
        {
            string spriteName = spriteNames[i];
            sprites.Add(new FSprite(spriteName));
            spriteLayerNameToIndex.Add(spriteName.Split('_').Last(), i);
        }

        sprites.ForEach(container.AddChild);
        ApplyPalette();
    }

    public void ApplyPalette(List<Color>? newColors = null)
    {
        colors = newColors;

        if (sprites.Count == 0)
            return;

        List<Color> colorsList = colors ?? [];

        if (colorsList.Count < 2)
        {
            if (!SlugcatNameToDefaultColors.TryGetValue(slugcatName, out List<string?> hexCodes))
            {
                RainMeadow.Debug(
                    "Not enough colours were provided to SlugIcon and no default colours were found for the given slugcat, using fallback colours"
                );
                hexCodes = ["FFFFFF", "101010", "FFFFFF"];
            }
            colorsList = [.. hexCodes.Select(hex => Custom.hexToColor(hex ?? "FFFFFF"))];
        }

        void TryMapColorsToSprites(string spritePart, Color color)
        {
            if (spriteLayerNameToIndex.TryGetValue(spritePart, out int index))
                sprites[index].color = color;
        }

        TryMapColorsToSprites("head", colorsList[0]);
        TryMapColorsToSprites("face", colorsList[1]);
        if (colorsList.Count > 2)
            TryMapColorsToSprites("feature", colorsList[2]);
    }
}
