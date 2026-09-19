using System;
using System.Collections.Generic;
using System.Linq;

namespace RainMeadow;
public static partial class ExtEnumSync
{
    // Quite heavy RPCs, even with the compression, I hope this isn't too much of an issue.
    // Heh, this is for the greater good.

    [RPCMethod(security = RPCSecurity.NoSecurity)] // Asking the owner for the compressed list of the enums. Client -> Owner
    public static void RequestCompressedExtEnums(RPCEvent request)
    {
        if (OnlineManager.lobby is null || OnlineManager.mePlayer != OnlineManager.lobby.owner)
        {
            RainMeadow.Error($"False request of enums : {(OnlineManager.lobby is null ? "Lobby is null" : "I am not the owner")} !");
            request.from.QueueEvent(new GenericResult.Fail(request));
            return;
        }

        Dictionary<string, string> compressedExtEnumTable = [];
        try
        {
            for (int i = 0; i < SyncedExtEnumList.Count; i++)
            {
                compressedExtEnumTable.Add(
                    SyncedExtEnumList[i].enumType.FullName,
                    CompressedExtEnumArrayToString(SyncedExtEnumList[i].GetAndCacheCompressedEntries())
                );
            }
        }
        catch (System.Exception er)
        {
            RainMeadow.Error("Failed to send compressed enums : " + er);
            request.from.QueueEvent(new GenericResult.Error(request));
            return;
        }
        request.from.QueueEvent(new GenericResult.Ok(request));
        request.from.InvokeRPC(SendToSyncCompressedExtEnums, compressedExtEnumTable);
    }
    [RPCMethod(security = RPCSecurity.NoSecurity)] // Checking and syncing the enums recieved, also ask for clarification if necessary. Owner -> Client
    public static void SendToSyncCompressedExtEnums(RPCEvent rpc, Dictionary<string, string> compressedExtEnumTable)
    {
        if (OnlineManager.lobby is null || rpc.from != OnlineManager.lobby.owner || OnlineManager.lobby.enumsChecked) { return; }

        List<CompressedExtEnumBase.DecompressionResult> clarificationTable = [];

        foreach (var compressedExtEnumKeyPair in compressedExtEnumTable)
        {
            int i = SyncedExtEnumList.FindIndex(x => x.enumType.FullName == compressedExtEnumKeyPair.Key);
            if (i > -1)
            {
                string[] compressedExtEnum = CompressedExtEnumStringToArray(compressedExtEnumKeyPair.Value);
                CompressedExtEnumBase.DecompressionResult result = SyncedExtEnumList[i].ReadAndSyncCompressedEntries(compressedExtEnum);
                RainMeadow.Info($"Read and Synced ExtEnum {compressedExtEnumKeyPair.Key} of host ! Missing enums : {result.MissingExtEnum.Length}, Ambiguous enums : {result.AmbiguousExtEnum.Length}, Extra enums : {result.AdditionnalExtEnum.Length}, Status OK ? {result.IsOK}");
                if (!result.IsOK)
                {
                    SyncedExtEnumList[i].storedCompressedValues = compressedExtEnum;
                    SyncedExtEnumList[i].clarificationAttempt = 0;
                    clarificationTable.Add(result);
                }
                else
                {
                    SyncedExtEnumList[i].LogMappedExtEnum();
                }
            }
        }

        if (clarificationTable.Count > 0)
        {
            RainMeadow.Info($"Asking clarification for {clarificationTable.Count} enums : [{string.Join(", ", clarificationTable.Select(x => x.TypeFullName))}]");
            rpc.from.InvokeRPC(AskFromClarification, clarificationTable);
        }
        else
        {
            OnlineManager.lobby.OnEnumSyncSuccessful();
        }
    }
    [RPCMethod(security = RPCSecurity.NoSecurity)] // Asking the owner for clarification on some enums in their compressed form. The owner will send them back in full. Client -> Owner
    public static void AskFromClarification(RPCEvent rpc, List<CompressedExtEnumBase.DecompressionResult> clarificationTable)
    {
        if (OnlineManager.lobby is null || OnlineManager.mePlayer != OnlineManager.lobby.owner) { return; }

        List<CompressedExtEnumBase.DecompressionResult> thingsThatShoubldBeClearerTable = [];

        foreach (var resultErrors in clarificationTable)
        {
            int i = SyncedExtEnumList.FindIndex(x => x.enumType.FullName == resultErrors.TypeFullName);
            if (i > -1)
            {
                ExtEnumEntry[] clarifiedMissingExtEnum = new ExtEnumEntry[resultErrors.MissingExtEnum.Length];
                ExtEnumEntry[] clarifiedAmbiguousExtEnum = new ExtEnumEntry[resultErrors.AmbiguousExtEnum.Length];

                // there we can directly assume that the index matches, since... you were the one sending it a few ticks ago
                for (int j = 0; j < resultErrors.MissingExtEnum.Length; j++)
                {
                    clarifiedMissingExtEnum[j] = new(
                        SyncedExtEnumList[i].GetValueFromIndex(resultErrors.MissingExtEnum[j].position)!,
                        resultErrors.MissingExtEnum[j].position
                    );
                }
                for (int j = 0; j < resultErrors.AmbiguousExtEnum.Length; j++)
                {
                    clarifiedAmbiguousExtEnum[j] = new(
                        SyncedExtEnumList[i].GetValueFromIndex(resultErrors.AmbiguousExtEnum[j].position)!,
                        resultErrors.AmbiguousExtEnum[j].position
                    );
                }

                CompressedExtEnumBase.DecompressionResult result = new(clarifiedMissingExtEnum, clarifiedAmbiguousExtEnum, resultErrors.TypeFullName);
                thingsThatShoubldBeClearerTable.Add(result);
            }
            else
            {
                RainMeadow.Error($"Couldn't find Enum to clarify : {resultErrors.TypeFullName} ! Will ignore it.");
            }
        }
        RainMeadow.Info($"Sending clarification for {thingsThatShoubldBeClearerTable.Count} enums : [{string.Join(", ", thingsThatShoubldBeClearerTable.Select(x => x.TypeFullName))}]");
        rpc.from.InvokeRPC(SendToSyncClarification, thingsThatShoubldBeClearerTable);
    }
    [RPCMethod(security = RPCSecurity.NoSecurity)]  // Checking and syncing (again) the enums clarified. If everything goes right, the client should have everything clear and done here. Owner -> Client
    public static void SendToSyncClarification(RPCEvent rpc, List<CompressedExtEnumBase.DecompressionResult> thingsThatShoubldBeClearerTable)
    {
        if (OnlineManager.lobby is null || rpc.from != OnlineManager.lobby.owner || OnlineManager.lobby.enumsChecked) { return; }
        List<CompressedExtEnumBase.DecompressionResult> reclarificationTable = [];

        foreach (var resultClarification in thingsThatShoubldBeClearerTable)
        {
            int i = SyncedExtEnumList.FindIndex(x => x.enumType.FullName == resultClarification.TypeFullName);
            if (i > -1)
            {
                if (SyncedExtEnumList[i].storedCompressedValues.Length == 0) { return; } // you never asked for clarification, cmon, you know what you were doing !

                for (int j = 0; j < resultClarification.MissingExtEnum.Length; j++)
                {
                    // This is technically creating enums if some are missings ? That's such a rare case, I don't think it'd cause error anyway.
                    if (SyncedExtEnumList[i].entriesMap.ContainsKey(resultClarification.MissingExtEnum[j].value))
                    {
                        RainMeadow.Warn($"Tried to assign missing enum {resultClarification.MissingExtEnum[j].value} of {resultClarification.TypeFullName} but it's already here!");
                    }
                    else
                    {
                        SyncedExtEnumList[i].entriesMap.Add(resultClarification.MissingExtEnum[j].value, SyncedExtEnumList[i].entriesMap.Count);
                    }
                }
                for (int j = 0; j < resultClarification.AmbiguousExtEnum.Length; j++)
                {
                    // Putting the exact value so it's more clear :D
                    SyncedExtEnumList[i].storedCompressedValues[resultClarification.AmbiguousExtEnum[j].position] = resultClarification.AmbiguousExtEnum[j].value;
                }

                CompressedExtEnumBase.DecompressionResult result = SyncedExtEnumList[i].ReadAndSyncCompressedEntries(SyncedExtEnumList[i].storedCompressedValues);
                RainMeadow.Info($"Read and Synced ExtEnum {resultClarification.TypeFullName} of host again..! Attemps : {SyncedExtEnumList[i].clarificationAttempt + 1}, Missing enums : {result.MissingExtEnum.Length}, Ambiguous enums : {result.AmbiguousExtEnum.Length}, Extra enums : {result.AdditionnalExtEnum.Length}, Status OK ? {result.IsOK}");
                if (!result.IsOK)
                {
                    // We don't want this to run indefinetly !
                    SyncedExtEnumList[i].clarificationAttempt++;
                    if (SyncedExtEnumList[i].clarificationAttempt >= CompressedExtEnumBase.Patience)
                    {
                        RainMeadow.Error($"Tried to clarify {resultClarification.TypeFullName} more than {CompressedExtEnumBase.Patience} times ! Not syncing enum.");
                    }
                    else
                    {
                        reclarificationTable.Add(result);
                        continue;
                    }
                }
                SyncedExtEnumList[i].storedCompressedValues = [];
                SyncedExtEnumList[i].LogMappedExtEnum();
            }
            else
            {
                RainMeadow.Error($"Couldn't find Enum to sync : {resultClarification.TypeFullName} ! Will ignore it.");
            }
        }

        if (reclarificationTable.Count > 0)
        {
            RainMeadow.Info($"Asking clarification again for {reclarificationTable.Count} enums : [{string.Join(", ", reclarificationTable.Select(x => x.TypeFullName))}]");
            rpc.from.InvokeRPC(AskFromClarification, reclarificationTable);
        }
        else
        {
            OnlineManager.lobby.OnEnumSyncSuccessful();
        }
    }
}