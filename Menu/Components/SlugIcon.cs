using System.Collections.Generic;
using System.Linq;
using RWCustom;
using UnityEngine;

namespace RainMeadow.UI.Components;

/// <summary>
/// A container of 2-3 sprites to draw a little slugcat icon. See PositionedSlugIcon and PlayerIcon<br/>
/// for usage in menus and in in-game HUDs.<br/>
/// <br/>
/// SlugcatNameToSpriteNames and SlugcatNameToDefaultColors can have entries added to them to support<br/>
/// modded slugcats in both the aforementioned SlugIcon components.
/// </summary>
public class SlugIcon
{
    /// <summary>
    /// Sprites are drawn on top of each other from earlier items in list to later, so use that<br/>
    /// to control layering. Colours are given by name of sprite (ending in _head gets base<br/>
    /// colour, ending in _face gets eye colour, ending in _feature gets feature colour if applicable)<br/>
    /// <br/>
    /// Make sure to load the asset file containing your illustrations as SlugIcon does not do that automatically!
    /// </summary>
    public static Dictionary<string, List<string>> SlugcatNameToSpriteNames { get; set; } =
        new()
        {
            { "White", ["basic_head", "basic_face"] },
            { "Yellow", ["basic_head", "monk_face"] },
            { "Red", ["hunter_head", "hunter_face"] },
            { "Night", ["basic_head", "basic_face", "watcher_feature"] },
            { "Artificer", ["basic_head", "artificer_feature", "artificer_face"] },
            { "Gourmand", ["gourmand_head", "basic_face"] },
            { "Spear", ["spearmaster_feature", "basic_head", "basic_face"] },
            { "Rivulet", ["basic_head", "basic_face", "rivulet_feature"] },
            { "Saint", ["saint_head", "saint_face"] },
            { "Inv", ["basic_head", "basic_face", "inv_feature"] },
            { "Slugpup", ["slugpup_head", "slugpup_face"] },
            { "Watcher", ["basic_head", "basic_face", "watcher_feature"] },
        };

    /// <summary>
    /// The same concept as SlugcatNameToSpriteNames, but for when the slugcat is to be drawn dead.<br/>
    /// By default, dead slugs will just use the original sprites provided in SlugcatNameToSpriteNames, but<br/>
    /// with the face replaced by the "dead_face" sprite. If sprites are provided here, they will override<br/>
    /// the corresponding sprite with the provided one. The default dead_face sprite will not be used if<br/>
    /// an override is present, so be sure to add "dead_face" yourself if you have other overrides.
    /// </summary>
    public static Dictionary<string, List<string>> SlugcatNameToDeathSpriteNames { get; set; } =
        new() { { "Slugpup", ["slugpup_dead_face"] } };

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
            { "Night", ["17234F", "FFFFFF"] },
            { "Artificer", ["70233B", "FFFFFF", "45283C"] },
            { "Gourmand", ["F0C197", "101010"] },
            { "Spear", ["4F2E69", "FFFFFF"] },
            { "Rivulet", ["91CCF0", "101010", "DF2DEA"] },
            { "Saint", ["AAF156", "101010"] },
            { "Inv", ["17244F", "FFFFFF"] },
            { "Slugpup", ["77DDCF", "101010"] },
            { "Watcher", ["17234F", "FFFFFF"] },
        };

    public FContainer container = new();
    public List<FSprite> sprites = [];
    public List<Color>? colors;
    public Dictionary<string, int> spriteLayerNameToIndex = [];

    public bool usingFallbackSprites;
    public bool? dead = null;
    public string slugcatName = "";

    public SlugIcon(string slugcatName, List<Color>? colors = null, bool dead = false)
    {
        Futile.atlasManager.LoadAtlas("illustrations/slugicons");
        this.colors = colors;
        DrawScugSprites(slugcatName, dead);
    }

    public void ClearSprites()
    {
        sprites.ForEach(sprite => sprite.RemoveFromContainer());
        sprites.Clear();
        spriteLayerNameToIndex.Clear();
    }

    public void RemoveFromContainer()
    {
        ClearSprites();
        container.RemoveFromContainer();
    }

    private List<string> GetScugSpriteNames()
    {
        usingFallbackSprites = !SlugcatNameToSpriteNames.TryGetValue(
            slugcatName,
            out List<string> names
        );

        List<string> spriteNames = usingFallbackSprites
            ? ["basic_head", "basic_face", "modded_feature"]
            : [.. names];

        if (!dead ?? false)
            return spriteNames;

        bool hasDeathSprites = SlugcatNameToDeathSpriteNames.TryGetValue(
            slugcatName,
            out List<string> deathSpriteNames
        );

        if (!hasDeathSprites)
        {
            int faceIndex = spriteNames.FindIndex(name => name.EndsWith("_face"));
            if (faceIndex > -1)
                spriteNames[faceIndex] = "dead_face";
            else
                RainMeadow.Debug(
                    $"No face sprite was found for {slugcatName}, so no death face could be provided"
                );
            return spriteNames;
        }

        foreach (string deathSpriteName in deathSpriteNames)
        {
            string spriteType = deathSpriteName.Split('_').Last();
            int spriteIndexToReplace = spriteNames.FindIndex(name =>
                name.EndsWith("_" + spriteType)
            );
            if (spriteIndexToReplace <= -1)
                continue;
            spriteNames[spriteIndexToReplace] = deathSpriteName;
        }

        return spriteNames;
    }

    public void DrawScugSprites(string? newSlugcatName = null, bool drawDead = false)
    {
        if (
            sprites.Count > 0
            && (slugcatName == newSlugcatName || newSlugcatName == null)
            && dead == drawDead
        )
            return;
        ClearSprites();

        if (newSlugcatName != null)
            slugcatName = newSlugcatName;
        dead = drawDead;

        if (slugcatName == "")
            return;

        List<string> spriteNames = GetScugSpriteNames();

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
        if (newColors != null)
            colors = newColors;

        if (sprites.Count == 0)
            return;

        List<Color> colorsList = colors ?? [];

        if (colorsList.Count < 2)
        {
            if (!SlugcatNameToDefaultColors.TryGetValue(slugcatName, out List<string?> hexCodes))
            {
                RainMeadow.Debug(
                    $"Not enough colours were provided to SlugIcon and no default colours were found for the given slugcat ({slugcatName}), using fallback colours"
                );
                hexCodes = ["FFFFFF", "101010", "E59D52"];
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
        else if (usingFallbackSprites)
            sprites[2].color = Custom.hexToColor("E59D52");
    }
}
