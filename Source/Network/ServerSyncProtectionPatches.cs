using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace PraetorisClient
{
    internal static class ServerSyncProtection
    {
        private const string ServerSyncConfigSyncMethodSuffix = " ConfigSync";
        private static readonly HashSet<int> ServerSyncConfigSyncMethodHashes = new();

        internal static bool ShouldBlockOutgoing(string methodName)
        {
            if (!IsEnabledOnClient || !IsServerSyncConfigSyncMethod(methodName))
            {
                return false;
            }

            PraetorisClientPlugin.Log.LogInfo("Blocked outgoing peer ServerSync config packet: " + methodName);
            return true;
        }

        internal static bool ShouldBlockIncoming(long senderPeerId, int methodHash)
        {
            if (!ServerSyncConfigSyncMethodHashes.Contains(methodHash))
            {
                return false;
            }

            if (ZRoutedRpc.instance == null || senderPeerId == ZRoutedRpc.instance.GetServerPeerID())
            {
                return false;
            }

            PraetorisClientPlugin.Log.LogInfo("Blocked incoming peer ServerSync config packet from " + senderPeerId);
            return true;
        }

        internal static void TrackRegisteredMethod(string methodName)
        {
            if (IsServerSyncConfigSyncMethod(methodName))
            {
                ServerSyncConfigSyncMethodHashes.Add(methodName.GetStableHashCode());
            }
        }

        internal static bool IsEnabledOnClient
        {
            get
            {
                return PraetorisClientPlugin.BlockPeerServerSyncConfigSync?.Value == true
                       && ZNet.instance != null
                       && !ZNet.instance.IsServer();
            }
        }

        private static bool IsServerSyncConfigSyncMethod(string methodName)
        {
            return !string.IsNullOrEmpty(methodName)
                   && methodName.EndsWith(ServerSyncConfigSyncMethodSuffix, StringComparison.Ordinal);
        }
    }

    [HarmonyPatch]
    internal static class ServerSyncProtectionRegisterPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            foreach (MethodInfo method in AccessTools.GetDeclaredMethods(typeof(ZRoutedRpc)))
            {
                if (method.Name != nameof(ZRoutedRpc.Register))
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length >= 2 && parameters[0].ParameterType == typeof(string))
                {
                    yield return method;
                }
            }
        }

        private static void Prefix(string name)
        {
            ServerSyncProtection.TrackRegisteredMethod(name);
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
