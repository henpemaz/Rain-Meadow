using System;
using System.Text.RegularExpressions;
using Menu;
using UnityEngine;

namespace RainMeadow
{
    public static partial class SpecialEvents
    {
        public static Anniversary AnniversaryEvent = new Anniversary()
        {
            Name = Utils.Translate("Rain Meadow Anniversary"),
            StartMonth = 12,
            StartDay = 20,
            EndDay = 30,
        };

        public class Anniversary : Event
        {
            public override void UpdateLoginMessage(Menu.Menu self)
            {
                int chanceToShowMessage = UnityEngine.Random.Range(0, 11);
                if (chanceToShowMessage > 5 && RainMeadow.rainMeadowOptions.MeadowCoins.Value > 0)
                {
                    return;
                }

                DateTime startDate = new DateTime(2024, 12, 20);
                DateTime today = DateTime.UtcNow;
                int yearsSince = today.Year - startDate.Year;
                string m0 = self.Translate("Special event:");
                string m1 = self.Translate("number");
                string m2 = self.Translate("Event days remaining:");
                string message = Regex.Replace($"{m0} {Name} {m1} {yearsSince}!<LINE>>{m2} {AnniversaryEvent.DaysRemaining}", "<LINE>", "\r\n");

                DialogNotify dialog = new DialogNotify(message, self.manager, null);
                dialog.okButton.size = new Vector2(100f, 30f);
                dialog.pos = new Vector2(dialog.size.x * 0.5f, 0);
                self.manager.ShowDialog(dialog);
            }
        }
    }
}
