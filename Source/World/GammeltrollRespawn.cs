using HarmonyLib;
using UnityEngine;

namespace PraetorisClient
{
    internal static class GammeltrollRespawn
    {
        private const string PrefabName = "Spawner_TrollFrost";
        private static readonly int PrefabHash = PrefabName.GetStableHashCode();

        [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
        private static class ConfigurePrefab
        {
            [HarmonyPostfix]
            private static void Postfix(ZNetScene __instance)
            {
                GameObject? prefab = __instance.GetPrefab(PrefabName);
                CreatureSpawner? spawner = prefab?.GetComponent<CreatureSpawner>();
                if (spawner == null)
                {
                    PraetorisClientPlugin.Log.LogWarning("Could not configure Gammeltroll respawning: Spawner_TrollFrost or its CreatureSpawner is missing.");
                    return;
                }

                spawner.m_respawnTimeMinuts = PraetorisClientPlugin.GammeltrollRespawnMinutes.Value;
            }
        }

        [HarmonyPatch(typeof(CreatureSpawner), nameof(CreatureSpawner.UpdateSpawner))]
        private static class ApplySynchronizedInterval
        {
            [HarmonyPrefix]
            private static void Prefix(CreatureSpawner __instance)
            {
                // Config synchronization can arrive after the prefab and saved spawners load.
                // Only change the interval; vanilla preserves living trolls and spawn history.
                ZDO? zdo = __instance.m_nview?.GetZDO();
                if (zdo != null && zdo.GetPrefab() == PrefabHash)
                {
                    __instance.m_respawnTimeMinuts = PraetorisClientPlugin.GammeltrollRespawnMinutes.Value;
                }
            }
        }
    }
}
