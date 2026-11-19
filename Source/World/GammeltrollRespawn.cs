using HarmonyLib;
using UnityEngine;

namespace PraetorisClient
{
    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    internal static class GammeltrollRespawn
    {
        private const string PrefabName = "Spawner_TrollFrost";
        private const float RespawnMinutes = 1440f;

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

            spawner.m_respawnTimeMinuts = RespawnMinutes;
        }
    }
}
