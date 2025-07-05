using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Menu;
using Menu.Remix.MixedUI;
using RainMeadow.UI.Components;
using RWCustom;
using UnityEngine;
using UnityEngine.UI;

namespace RainMeadow.UI
{
    public class ArenaPostGameStatsDialog : Dialog
    {
        public StoredResults[] storedResults;
        public SimplerButton closeButton;
        public AlignedMenuLabel postGameStatsLabel;
        public ArenaOnlineGameMode arenaMode;
        public float spacing = 20;
        public ArenaPostGameStatsDialog(ProcessManager manager, ArenaOnlineGameMode arena) : base("", new(800, 460), manager)
        {
            arenaMode = arena;
            postGameStatsLabel = new(this, pages[0], Translate("POST-GAME STATS"), new(pos.x + size.x * 0.5f, pos.y + size.y + 10), new(0, 0), true);
            postGameStatsLabel.label.anchorY = 0;
            closeButton = new(this, pages[0], Translate("BACK"), new(roundedRect.pos.x + roundedRect.size.x - 80, roundedRect .pos.y - 40), new(80, 30));
            closeButton.OnClick += _ =>
            {
                manager.StopSideProcess(this);
                PlaySound(SoundID.MENU_Remove_Level);
            };
            pages[0].subObjects.AddRange([postGameStatsLabel, closeButton]);
            storedResults = [];
            SetUpStoredResults();
        }
        public void SetUpStoredResults()
        {
            storedResults = new StoredResults[3];
            float totalOccupiedXSize = size.x - (storedResults.Length + 1) * spacing, totalOccupiedYSize = size.y - spacing * 2, 
                storedResultXSize = totalOccupiedXSize / storedResults.Length;
            for (int i = 0; i < storedResults.Length; i++)
            {
                float posX = spacing * (i + 1) + storedResultXSize * i;
                string name = i == 0? Translate("WINS") : i == 1? Translate("KILLS") : Translate("DEATHS");
                storedResults[i] = new(this, roundedRect, new(posX, spacing), new(storedResultXSize, totalOccupiedYSize), name);
            }
            roundedRect.SafeAddSubobjects(storedResults);
        }
        public override void Update()
        {
            base.Update();
            for (int i = 0; i < storedResults.Length; i++)
                UpdateStoredResults(storedResults[i], GetStrings(storedResults[i], i));
        }
        public string[] GetStrings(StoredResults storedResults, int i)
        {

            if (i == 0)
                return [.. arenaMode.playerNumberWithWins.Where(x => ArenaHelpers.FindOnlinePlayerByLobbyId((ushort)x.Key) != null).Select(x => $"{LabelTest.TrimText(ArenaHelpers.FindOnlinePlayerByLobbyId((ushort)x.Key).id.name, storedResults.size.x - LabelTest.GetWidth($" - {x.Value}") - 10, true)} - {x.Value}")];
            if (i == 1)
                return [.. arenaMode.localAllKills.Select(x => $"{LabelTest.TrimText(ArenaHelpers.FindOnlinePlayerByLobbyId((ushort)x.Key).id.name, storedResults.size.x - LabelTest.GetWidth($" - {x.Value.Count}") - 10, true)} - {x.Value.Count}")];
            if (i == 2)
                return [.. arenaMode.playerNumberWithDeaths.Select(x => $"{LabelTest.TrimText(ArenaHelpers.FindOnlinePlayerByLobbyId((ushort)x.Key).id.name, storedResults.size.x - LabelTest.GetWidth($" - {x.Value}") - 10, true)} - {x.Value}")];
            return [];
        }
        public void UpdateStoredResults(StoredResults storedResults, string[] strings)
        {
            List<AlignedMenuLabel> menulabels = storedResults.scroller.GetSpecificButtons<AlignedMenuLabel>();
            for (int i = 0; i < menulabels.Count; i++)
            {
                if (strings.Length<= i)
                {
                    storedResults.scroller.RemoveButton(menulabels[i], true);
                    continue;
                }
                menulabels[i].text = strings[i];
            }
            int count = storedResults.scroller.GetSpecificButtons<AlignedMenuLabel>().Count;
            if (strings.Length == count) return;
            IEnumerable<string> newStrings = strings.Skip(count);
            foreach (string s in newStrings)
            {
                AlignedMenuLabel label = new(this, storedResults.scroller, s, storedResults.scroller.GetIdealPosWithScrollForButton(storedResults.scroller.buttons.Count), new(storedResults.scroller.size.x, 30), false);
                label.label.color = MenuColorEffect.rgbMediumGrey;
                storedResults.scroller.AddScrollObjects(label);
            }
        }
        public class StoredResults : RectangularMenuObject
        {
            public MenuLabel label;
            public RoundedRect roundedRect;
            public ButtonScroller scroller;
            public StoredResults(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size, string name) : base(menu, owner, pos, size)
            {
                roundedRect = new(menu, this, Vector2.zero, size, true);
                scroller = new(menu, this, Vector2.zero, new(size.x, size.y - 45), false, new(30, 20), -20)
                {
                    greyOutWhenNoScroll = true,
                    buttonHeight = 30,
                    buttonSpacing = 5,
                };
                scroller.CreateSideButtonLines();
                label = new(menu, this, name, new(0, scroller.size.y), new(size.x, 40), true);
                this.SafeAddSubobjects(roundedRect, scroller, label);
            }
        }
    }
}
