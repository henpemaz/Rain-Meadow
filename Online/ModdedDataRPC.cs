using System;
using System.Collections.Generic;

namespace RainMeadow;

/// <summary>
/// Makes it possible for a mod to use RPCs without becoming a highImpact mod.
/// This should be used ONLY when necessary.
/// Its sole purpose is to allow mods to send purely optional data to clients who have the same mod enabled.
/// For example, Push To Meow: Everyone who has the mod should hear the meows, but it shouldn't be a highImpact mod.
/// </summary>
public static class ModdedDataRPC
{
    /// <summary>
    /// A private dictionary of actions that can ideally be called as if they are RPCs.
    /// Arranged by keys in the form of "MOD_ID : NAME"
    /// </summary>
    private static Dictionary<string, Action<RPCEvent, ArgData>> moddedRPCs = new();

    [RPCMethod]
    internal static void ProcessModdedRPC(RPCEvent e, string key, ArgData args)
    {
        if (moddedRPCs.TryGetValue(key, out var rpc))
        {
            rpc.Invoke(e, args);
            RainMeadow.Debug($"Received modded RPC: {key}");
        }
        else RainMeadow.Debug($"Unable to find modded RPC: {key}");
    }

    /// <summary>
    /// Create a class that overrides this one in order to store/send your data.
    /// This class MUST have an empty constructor like, public MyData() { }
    /// And it MUST override CustomSerialize if you want it to do anything!
    /// </summary>
    public class ArgData : Serializer.ICustomSerializable
    {
        public ArgData() { }
        public virtual void CustomSerialize(Serializer serializer) { }
    }

    /// <summary>
    /// Registers an action to trigger for players whenever it is invoked.
    /// </summary>
    /// <param name="modId">The ID of your mod. Please include this.</param>
    /// <param name="rpcName">The name of the RPC you are registering; used to send the RPC.</param>
    /// <param name="rpc">The action that you want to be executed when this RPC is received.</param>
    /// <exception cref="KeyNotFoundException">Thrown an RPC with this modId and name has already been registered.</exception>
    public static void RegisterModRPC(string modId, string rpcName, Action<RPCEvent, ArgData> rpc)
    {
        string key = modId + " : " + rpcName;
        if (moddedRPCs.ContainsKey(key)) throw new KeyNotFoundException($"Modded RPC {rpcName} for {modId} is already registered!");
        else moddedRPCs.Add(key, rpc);
    }

    /// <summary>
    /// Sends an RPC to <paramref name="player"/> of the specified name with the given arguments/parameters.
    /// </summary>
    /// <param name="player">The player to whom you wish to send the RPC.</param>
    /// <param name="invokeOnce">Whether the RPC should send ONLY IF it is certainly not already being sent.</param>
    /// <param name="modId">The ID of your mod. Must be the same as the one used in RegisterModRPC.</param>
    /// <param name="rpcName">The name of your RPC. Must be the same as the one used in RegisterModRPC.</param>
    /// <param name="args">The arguments/parameters for your RPC, stored in an ArgData object.</param>
    /// <exception cref="KeyNotFoundException">Thrown if your RPC is has not been registered.</exception>
    public static void InvokeModRPC(this OnlinePlayer player, bool invokeOnce, string modId, string rpcName, ArgData args)
    {
        string key = modId + " : " + rpcName;
        if (!moddedRPCs.TryGetValue(key, out var rpc)) throw new KeyNotFoundException($"Modded RPC {rpcName} is not found for {modId}");
        if (invokeOnce) player.InvokeOnceRPC(ProcessModdedRPC, key, args);
        else player.InvokeRPC(ProcessModdedRPC, key, args);
    }
}
