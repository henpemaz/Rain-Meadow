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

    public const string AtlasPath = "illustrations/slugicons";

    /// <summary>
    /// Used to check if the icon is loaded.
    /// </summary>
    private const string AtlasProbeElement = "basic_head";

    private static readonly List<string> FallbackSpriteNames =
    [
        "basic_head",
        "basic_face",
        "modded_feature",
    ];
    private static readonly List<string?> FallbackHexColors = ["FFFFFF", "101010", "FFFFFF"];

    public static void LoadAtlas()
    {
        if (!Futile.atlasManager.DoesContainElementWithName(AtlasProbeElement))
            Futile.atlasManager.LoadAtlas(AtlasPath);
    }

    public static List<string> GetSpriteNames(string slugcat, bool dead = false)
    {
        List<string> spriteNames = SlugcatToSpriteNames.TryGetValue(slugcat, out List<string> names)
            ? [.. names]
            : [.. FallbackSpriteNames];

        if (dead)
        {
            int faceIndex = spriteNames.FindIndex(name => name.EndsWith("_face"));
            if (faceIndex >= 0)
                spriteNames[faceIndex] = "dead_face";
        }

        return spriteNames;
    }

    public static List<Color> GetColors(string slugcat, List<Color>? colors)
    {
        if (colors != null && colors.Count >= 2)
            return colors;

        if (!SlugcatToDefaultColors.TryGetValue(slugcat, out List<string?> hexColors))
        {
            RainMeadow.Error(
                "Not enough colors provided to SlugIcon and no default colours were found for the given slugcat, using fallback colors"
            );
            hexColors = FallbackHexColors;
        }

        return [.. hexColors.Select(hex => Custom.hexToColor(hex ?? "FFFFFF"))];
    }

    /// <summary>
    /// tints sprite with GetColors
    /// </summary>
    public static int ColorIndexForSprite(string spriteName) =>
        spriteName.Split('_').Last() switch
        {
            "head" => 0,
            "face" => 1,
            "feature" => 2,
            _ => -1,
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
        LoadAtlas();

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

        List<string> spriteNames = GetSpriteNames(slugcat, dead);

        for (int i = 0; i < spriteNames.Count; i++)
        {
            string spriteName = spriteNames[i];
            sprites.Add(new PositionedSprite(menu, this, Vector2.zero, new FSprite(spriteName)));
            spriteLayerToColorIndex[spriteName.Split('_').Last()] = i;
        }

        subObjects.AddRange([.. sprites]);
        ApplyPalette(this.colors);
    }

    public void ApplyPalette(List<Color>? newColors = null)
    {
        this.colors = newColors;

        if (sprites.Count == 0)
            return;

        List<Color> colors = GetColors(slugcat, this.colors);

        foreach (KeyValuePair<string, int> layer in spriteLayerToColorIndex)
        {
            int colorIndex = ColorIndexForSprite(layer.Key);
            if (colorIndex >= 0 && colorIndex < colors.Count)
                sprites[layer.Value].Sprite.color = colors[colorIndex];
        }
    }
}
