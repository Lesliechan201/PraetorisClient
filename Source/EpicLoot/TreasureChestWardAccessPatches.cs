using EpicLoot.Adventure;
using HarmonyLib;
using UnityEngine;

namespace PraetorisClient.EpicLootFeature
{
    internal static class TreasureChestWardAccess
    {
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
            return component.GetComponentInParent<TreasureMapChest>() != null;
        }
    }

    [HarmonyPatch(typeof(Container), nameof(Container.Interact))]
    internal static class EpicLootTreasureChestContainerInteractPatch
    {
        private static void Prefix(Container __instance)
        {
            TreasureChestWardAccess.DisableGuardStoneCheckForTreasureChest(__instance);
        }
    }

    [HarmonyPatch(typeof(Container), nameof(Container.TakeAll))]
    internal static class EpicLootTreasureChestContainerTakeAllPatch
    {
        private static void Prefix(Container __instance)
        {
            TreasureChestWardAccess.DisableGuardStoneCheckForTreasureChest(__instance);
        }
    }

    [HarmonyPatch(typeof(Container), nameof(Container.GetHoverText))]
    internal static class EpicLootTreasureChestContainerHoverTextPatch
    {
        private static void Prefix(Container __instance)
        {
            TreasureChestWardAccess.DisableGuardStoneCheckForTreasureChest(__instance);
        }
    }
}
