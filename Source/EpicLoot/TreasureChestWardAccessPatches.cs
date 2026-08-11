using EpicLoot.Adventure;
using HarmonyLib;
using UnityEngine;

namespace PraetorisClient.EpicLootFeature
{
    internal readonly struct ContainerGuardStoneState
    {
        internal ContainerGuardStoneState(bool shouldRestore, bool checkGuardStone)
        {
            ShouldRestore = shouldRestore;
            CheckGuardStone = checkGuardStone;
        }

        internal bool ShouldRestore { get; }
        internal bool CheckGuardStone { get; }
    }

    internal static class TreasureChestWardAccess
    {
        internal static ContainerGuardStoneState DisableGuardStoneCheckForTreasureChest(Container container)
        {
            if (container == null || !container.m_checkGuardStone || !IsEpicLootTreasureChest(container))
            {
                return new ContainerGuardStoneState(false, false);
            }

            container.m_checkGuardStone = false;
            return new ContainerGuardStoneState(true, true);
        }

        internal static void RestoreGuardStoneCheck(Container container, ContainerGuardStoneState state)
        {
            if (!state.ShouldRestore || container == null)
            {
                return;
            }

            container.m_checkGuardStone = state.CheckGuardStone;
        }

        private static bool IsEpicLootTreasureChest(Component component)
        {
            return component.GetComponentInParent<TreasureMapChest>() != null;
        }
    }

    [HarmonyPatch(typeof(Container), nameof(Container.Interact))]
    internal static class EpicLootTreasureChestContainerInteractPatch
    {
        private static void Prefix(Container __instance, ref ContainerGuardStoneState __state)
        {
            __state = TreasureChestWardAccess.DisableGuardStoneCheckForTreasureChest(__instance);
        }

        private static void Finalizer(Container __instance, ContainerGuardStoneState __state)
        {
            TreasureChestWardAccess.RestoreGuardStoneCheck(__instance, __state);
        }
    }

    [HarmonyPatch(typeof(Container), nameof(Container.TakeAll))]
    internal static class EpicLootTreasureChestContainerTakeAllPatch
    {
        private static void Prefix(Container __instance, ref ContainerGuardStoneState __state)
        {
            __state = TreasureChestWardAccess.DisableGuardStoneCheckForTreasureChest(__instance);
        }

        private static void Finalizer(Container __instance, ContainerGuardStoneState __state)
        {
            TreasureChestWardAccess.RestoreGuardStoneCheck(__instance, __state);
        }
    }

    [HarmonyPatch(typeof(Container), nameof(Container.GetHoverText))]
    internal static class EpicLootTreasureChestContainerHoverTextPatch
    {
        private static void Prefix(Container __instance, ref ContainerGuardStoneState __state)
        {
            __state = TreasureChestWardAccess.DisableGuardStoneCheckForTreasureChest(__instance);
        }

        private static void Finalizer(Container __instance, ContainerGuardStoneState __state)
        {
            TreasureChestWardAccess.RestoreGuardStoneCheck(__instance, __state);
        }
    }
}
