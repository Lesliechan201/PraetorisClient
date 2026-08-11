using EpicLoot.Adventure;
using HarmonyLib;
using UnityEngine;

namespace PraetorisClient.EpicLootFeature
{
    internal static class TreasureChestWardAccess
    {
        private const string TreasureMapChestBiomeKey = "TreasureMapChest.Biome";

        internal static void DisableGuardStoneCheckForTreasureChest(Container container)
        {
            if (container == null || !container.m_checkGuardStone || !IsEpicLootTreasureChest(container))
            {
                return;
            }

            container.m_checkGuardStone = false;
        }

        private static bool IsEpicLootTreasureChest(Component component)
        {
            if (component.GetComponentInParent<TreasureMapChest>() != null)
            {
                return true;
            }

            ZNetView zNetView = component.GetComponent<ZNetView>();
            if (zNetView == null || !zNetView.IsValid())
            {
                return false;
            }

            return !string.IsNullOrEmpty(zNetView.GetZDO().GetString(TreasureMapChestBiomeKey));
        }
    }

    [HarmonyPatch(typeof(Container), "Awake")]
    internal static class EpicLootTreasureChestContainerAwakePatch
    {
        private static void Postfix(Container __instance)
        {
            TreasureChestWardAccess.DisableGuardStoneCheckForTreasureChest(__instance);
        }
    }

    [HarmonyPatch(typeof(TreasureMapChest), nameof(TreasureMapChest.Reinitialize))]
    internal static class EpicLootTreasureChestReinitializePatch
    {
        private static void Postfix(TreasureMapChest __instance)
        {
            TreasureChestWardAccess.DisableGuardStoneCheckForTreasureChest(__instance.GetComponent<Container>());
        }
    }
}
