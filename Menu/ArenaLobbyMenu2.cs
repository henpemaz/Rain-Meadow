using Menu;
using UnityEngine;
using RainMeadow.UI.Components;
using Menu.Remix.MixedUI;
using Menu.Remix;

namespace RainMeadow.UI;

public class ArenaLobbyMenu2 : SmartMenu
{
    public SimplerButton playButton;
    public FSprite[] divSprites;
    public Vector2[] divSpritesPos;
    public MenuLabel[] settingsMenuLabels;
    public SimplerCheckbox spearsHitCheckbox;
    public SimplerCheckbox aggressiveAICheckBox;
    public SimplerCheckbox disableSaintAscendCheckBox;
    public SimplerCheckbox disableArtificerStun;
    public SimplerCheckbox disableMaulingCheckBox;
    public SimplerCheckbox enableInvCheckBox;
    public SimplerCheckbox enableInv0SkillSpearCheckBox;
    public SimplerCheckbox enableInv0SkillEggCheckBox;
    public SimplerMultipleChoiceArray roomRepeatChoiceArray;
    public SimplerMultipleChoiceArray rainTimerChoiceArray;
    public SimplerMultipleChoiceArray wildlifeChoiceArray;
    public OpTextBox countdownTimerTextBox;
    public OpTextBox saintAscendDurationTimerTextBox;
    public override MenuScene.SceneID GetScene => ModManager.MMF ? manager.rainWorld.options.subBackground : MenuScene.SceneID.Landscape_SU;

    public ArenaLobbyMenu2(ProcessManager manager) : base(manager, RainMeadow.Ext_ProcessID.ArenaLobbyMenu)
    {
        RainMeadow.DebugMe();

        scene.AddIllustration(new MenuIllustration(this, scene, "", "CompetitiveShadow", new Vector2(-2.99f, 265.01f), crispPixels: true, anchorCenter: false));
        scene.AddIllustration(new MenuIllustration(this, scene, "", "CompetitiveTitle", new Vector2(-2.99f, 265.01f), crispPixels: true, anchorCenter: false));
        scene.flatIllustrations[scene.flatIllustrations.Count - 1].sprite.shader = manager.rainWorld.Shaders["MenuText"];

        playButton = new(this, mainPage, Utils.Translate("READY?"), new Vector2(1056f, 50f), new Vector2(110f, 30f));
        mainPage.subObjects.Add(playButton);

        Vector2 where = new(600f, 300f);
        float settingsElementWidth = 310f;

        spearsHitCheckbox = new SimplerCheckbox(this, mainPage, where + new Vector2(0f, 220f), 95f, Translate("Friendly Fire:"));
        spearsHitCheckbox.OnClick += c => RainMeadow.Debug($"friendly fire: {c}");
        mainPage.subObjects.Add(spearsHitCheckbox);

        aggressiveAICheckBox = new SimplerCheckbox(this, mainPage, where + new Vector2(settingsElementWidth - 24f, 220f), InGameTranslator.LanguageID.UsesLargeFont(CurrLang) ? 120f : 100f, Translate("Aggressive AI:"));
        aggressiveAICheckBox.OnClick += c => RainMeadow.Debug($"aggressive ai: {c}");
        mainPage.subObjects.Add(aggressiveAICheckBox);

        divSprites = new FSprite[2];
        divSpritesPos = new Vector2[2];
        for (int i = 0; i < divSprites.Length; i++)
        {
            mainPage.Container.AddChild(
                divSprites[i] = new FSprite("pixel")
                {
                    anchorX = 0f,
                    scaleX = settingsElementWidth + 95f,
                    scaleY = 2f,
                    color = MenuRGB(MenuColors.VeryDarkGrey),
                }
            );
            divSpritesPos[i] = where + new Vector2(-95f, 197f - (171 * i));
        }

        roomRepeatChoiceArray = new SimplerMultipleChoiceArray(this, mainPage, where + new Vector2(0f, 150f), Translate("Repeat Rooms:"), InGameTranslator.LanguageID.UsesLargeFont(CurrLang) ? 115f : 95f, settingsElementWidth, 5, textInBoxes: true);
        roomRepeatChoiceArray.OnClick += i => RainMeadow.Debug($"room repeat: pressed {i}");
        for (int i = 0; i < roomRepeatChoiceArray.buttons.Length; i++)
            roomRepeatChoiceArray.buttons[i].label.text = $"{i + 1}x";
        mainPage.subObjects.Add(roomRepeatChoiceArray);

        rainTimerChoiceArray = new SimplerMultipleChoiceArray(this, mainPage, where + new Vector2(0f, 100f), Translate("Rain Timer:"), InGameTranslator.LanguageID.UsesLargeFont(CurrLang) ? 100f : 95f, settingsElementWidth, 6, splitText: CurrLang == InGameTranslator.LanguageID.French || CurrLang == InGameTranslator.LanguageID.Spanish || CurrLang == InGameTranslator.LanguageID.Portuguese);
        rainTimerChoiceArray.OnClick += i => RainMeadow.Debug($"rain timer: pressed {i}");
        mainPage.subObjects.Add(rainTimerChoiceArray);

        wildlifeChoiceArray = new SimplerMultipleChoiceArray(this, mainPage, where + new Vector2(0f, 50f), Translate("Wildlife:"), 95f, settingsElementWidth, 4);
        wildlifeChoiceArray.OnClick += i => RainMeadow.Debug($"wildlife: pressed {i}");
        mainPage.subObjects.Add(wildlifeChoiceArray);

        settingsMenuLabels = new MenuLabel[2];
        settingsMenuLabels[0] = new MenuLabel(this, mainPage, "Countdown Timer:", where + new Vector2(0f, -17f), new Vector2(0f, 30f), false);
        settingsMenuLabels[1] = new MenuLabel(this, mainPage, "Saint Ascend Time:", where + new Vector2(125f, -67f), new Vector2(0f, 30f), false);
        mainPage.subObjects.AddRange(settingsMenuLabels);

        countdownTimerTextBox = new OpTextBox(new Configurable<int>(5), where + new Vector2(215f, -20f), 95f);
        countdownTimerTextBox.OnChange += () => { RainMeadow.Debug($"countdown timer textbox: {countdownTimerTextBox.value}"); };
        UIelementWrapper countdownTimerTextBoxWrapper = new(tabWrapper, countdownTimerTextBox);
        mainPage.subObjects.Add(countdownTimerTextBoxWrapper);

        disableSaintAscendCheckBox = new SimplerCheckbox(this, mainPage, where + new Vector2(0f, -70f), 95f, "Sain't:");
        disableSaintAscendCheckBox.OnClick += c => RainMeadow.Debug($"sain't: {c}");
        mainPage.subObjects.Add(disableSaintAscendCheckBox);

        enableInvCheckBox = new SimplerCheckbox(this, mainPage, where + new Vector2(15f, -120f), 110f, "Enable Sofanthiel:");
        mainPage.subObjects.Add(enableInvCheckBox);

        enableInv0SkillSpearCheckBox = new SimplerCheckbox(this, mainPage, where + new Vector2(270f, -120f), 215f, "Sofanthiel Can Always Throw Spear:");
        mainPage.subObjects.Add(enableInv0SkillSpearCheckBox);

        enableInv0SkillEggCheckBox = new SimplerCheckbox(this, mainPage, where + new Vector2(100f, -170f), 195f, "Sofanthiel Can Always Throw Egg:");
        mainPage.subObjects.Add(enableInv0SkillEggCheckBox);
    }

    public override void Update()
    {
        base.Update();
    }

    public override void GrafUpdate(float timeStacker)
    {
        base.GrafUpdate(timeStacker);

        for (int i = 0; i < divSprites.Length; i++)
        {
            var divSprite = divSprites[i];
            var divSpritePos = divSpritesPos[i];

            divSprite.x = mainPage.DrawX(timeStacker) + divSpritePos.x;
            divSprite.y = mainPage.DrawY(timeStacker) + divSpritePos.y;
        }
    }
}