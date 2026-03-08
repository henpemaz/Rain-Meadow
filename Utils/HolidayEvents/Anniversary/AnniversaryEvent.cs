using System;
using System;
using System.CodeDom;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.Generic;
using System.IO;
using System.IO;
using System.Linq;
using System.Linq;
using System.Security;
using System.Security;
using Menu;
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
                DateTime startDate = new DateTime(2024, 12, 20);
                DateTime today = DateTime.UtcNow;
                int yearsSince = today.Year - startDate.Year;
                int daysLeft = EndDay - DateTime.UtcNow.Day;
                string message = self.Translate(
                    $"Special event: {Name} number {yearsSince}!<LINE>Days remaining: {daysLeft}"
                );

                DialogNotify dialog = new DialogNotify(message, self.manager, null);
                dialog.okButton.size = new Vector2(100f, 30f);
                dialog.pos = new Vector2(dialog.size.x * 0.5f, 0);
                self.manager.ShowDialog(dialog);
            }
        }
    }
}
