using System;
using HarmonyLib;

namespace PraetorisClient
{
    internal static class ServerSyncProtection
    {
        private const string AzuAutoStoreConfigSyncMethod = "Azumatt.AzuAutoStore ConfigSync";
        private static readonly int AzuAutoStoreConfigSyncMethodHash = AzuAutoStoreConfigSyncMethod.GetStableHashCode();

        internal static bool ShouldBlockOutgoing(string methodName)
        {
            return IsEnabledOnClient && string.Equals(methodName, AzuAutoStoreConfigSyncMethod, StringComparison.Ordinal);
        }

        internal static bool ShouldBlockIncoming(long senderPeerId, int methodHash)
        {
            if (methodHash != AzuAutoStoreConfigSyncMethodHash)
            {
                return false;
            }

            return ZRoutedRpc.instance != null && senderPeerId != ZRoutedRpc.instance.GetServerPeerID();
        }

        internal static bool IsEnabledOnClient
        {
            get
            {
                return PraetorisClientPlugin.BlockAzuAutoStoreClientConfigSync?.Value == true
                       && ZNet.instance != null
                       && !ZNet.instance.IsServer();
            }
        }
    }

    [HarmonyPatch(typeof(ZRoutedRpc), nameof(ZRoutedRpc.InvokeRoutedRPC), typeof(long), typeof(string), typeof(object[]))]
    internal static class ServerSyncProtectionOutgoingPatch
    {
        private static bool Prefix(string methodName)
        {
            return !ServerSyncProtection.ShouldBlockOutgoing(methodName);
        }
    }

    [HarmonyPatch(typeof(ZRoutedRpc), "RPC_RoutedRPC")]
    internal static class ServerSyncProtectionIncomingPatch
    {
        private static bool Prefix(ZRpc rpc, ZPackage pkg)
        {
            if (!ServerSyncProtection.IsEnabledOnClient)
            {
                return true;
            }

            int originalPosition = pkg.GetPos();
            try
            {
                pkg.SetPos(0);
                pkg.ReadLong();
                long senderPeerId = pkg.ReadLong();
                pkg.ReadLong();
                pkg.ReadZDOID();
                int methodHash = pkg.ReadInt();

                return !ServerSyncProtection.ShouldBlockIncoming(senderPeerId, methodHash);
            }
            catch (Exception ex)
            {
                PraetorisClientPlugin.Log.LogWarning("Failed to inspect routed RPC for ServerSync protection: " + ex.Message);
                return true;
            }
            finally
            {
                pkg.SetPos(originalPosition);
            }
        }
    }
}
