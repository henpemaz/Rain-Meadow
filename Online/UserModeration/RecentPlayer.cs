using JetBrains.Annotations;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RainMeadow
{
    public class RecentPlayer
    {
        public string name;
        public ulong id;
        public Customization customization;

        public RecentPlayer()
        {
            name = "Unknown User";
            id = 0;
            customization = new();
        }

        public RecentPlayer(string name = "Unknown User", ulong id = 0, Customization? customization = null)
        {
            this.name = name;
            this.id = id;
            this.customization = customization ?? new Customization();
        }

        public RecentPlayer(OnlinePlayer player, SlugcatCustomization? slugcatCustomization = null)
        {
            name = player.id.name;
            // This should not be used for any domain other than Steam.
            if (player.id is SteamMatchmakingManager.SteamPlayerId steamID)
            {
                id = steamID.steamID.m_SteamID;
            }
            else
            {
                id = 0;
            }
            customization = slugcatCustomization != null ? new(slugcatCustomization) : new();
        }

        public class Customization
        {
            public string slugcatClass;
            public string primary;
            public string secondary;
            public string special;

            public SlugcatStats.Name SlugcatClass() => SlugcatStats.Name.values.entries.Contains(slugcatClass) ? new SlugcatStats.Name(slugcatClass) : SlugcatStats.Name.White;
            public Color Primary() => Utils.SafeHexToColor(primary);
            public Color Secondary() => Utils.SafeHexToColor(secondary);
            public Color Special() => Utils.SafeHexToColor(special);

            public Customization(string slugcatClass, string primary = "FFFFFF", string secondary = "FFFFFF", string special = "FFFFFF")
            {
                this.slugcatClass = slugcatClass;
                this.primary = primary;
                this.secondary = secondary;
                this.special = special;
            }

            public Customization()
            {
                slugcatClass = SlugcatStats.Name.White.value;
                primary = Custom.colorToHex(Color.white);
                secondary = Custom.colorToHex(Color.black);
                special = Custom.colorToHex(Color.gray);
            }

            public Customization(SlugcatCustomization slugcatCustomization)
            {
                slugcatClass = slugcatCustomization.playingAs.value ?? SlugcatStats.Name.White.value;
                primary = Custom.colorToHex(slugcatCustomization.bodyColor);
                secondary = Custom.colorToHex(slugcatCustomization.eyeColor);
                special = Custom.colorToHex(slugcatCustomization.currentColors.Last());
            }
        }
    }
}
