using System;
using System.Collections.Generic;
using System.Linq;
using MonoMod.RuntimeDetour;

namespace RainMeadow;
public static partial class ExtEnumSync
{
    internal static void ApplyHooks()
    {
        On.ExtEnumType.AddEntry += ExtEnumType_AddEntry_AddEntryToMap;
    }

    private static void ExtEnumType_AddEntry_AddEntryToMap(On.ExtEnumType.orig_AddEntry orig, ExtEnumType self, string name)
    {
        bool newEntryAdded = !self.entries.Contains(name);

        orig(self, name);

        if (OnlineManager.lobby is not null
            && newEntryAdded
            && self.TryGetExtEnumType(out var type)
            && IsSyncedExtEnum(type, out var compressedExtEnum)
            && !compressedExtEnum.IsEntryMapped(name))
        {
            if (OnlineManager.lobby.isOwner)
            {
                RainMeadow.Warn($"New entry \"{name}\" of ExtEnum {compressedExtEnum.enumType.FullName} added mid-game! Adding to the map immediatly!");
                compressedExtEnum.AddEntryToMap(name);

                foreach (var player in OnlineManager.lobby.participants)
                {
                    if (!player.isMe)
                    {
                        player.InvokeRPC(AddNewExtEnumEntry,
                            name,
                            (byte)(compressedExtEnum.entriesMap.Count - 1),
                            compressedExtEnum.enumType.FullName);
                    }
                }
            }
            else
            {
                RainMeadow.Error($"New entry \"{name}\" of ExtEnum {compressedExtEnum.enumType.FullName} added mid-game while not host ! This enum won't be synced until the host tells us to add it.");
            }
        }
    }
}