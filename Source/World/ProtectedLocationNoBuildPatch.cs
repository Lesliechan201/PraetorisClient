using System;
using System.Collections.Generic;
using HarmonyLib;

namespace PraetorisClient
{
    internal static class ProtectedLocationNoBuild
    {
        private static readonly HashSet<string> ProtectedPrefabs = new(StringComparer.Ordinal)
        {
            "Hildir_crypt",
            "Crypt4",
            "Hildir_cave",
            "Hildir_plainsfortress",
            "SunkenCrypt4",
            "Crypt3",
            "TrollCave02",
            "Crypt2",
            "Mistlands_DvergrTownEntrance1",
            "Mistlands_DvergrTownEntrance2"
        };

        internal static void ApplyToLoadedLocations()
        {
            foreach (Location location in Location.s_allLocations)
            {
                if (location == null)
                {
                    continue;
                }

                string prefabName = Utils.GetPrefabName(location.gameObject);
                if (!ProtectedPrefabs.Contains(prefabName))
                {
                    continue;
                }

                location.m_noBuild = true;
            }
        }
    }

    [HarmonyPatch(typeof(Location), nameof(Location.IsInsideNoBuildLocation))]
    internal static class ProtectedLocationNoBuildCheckPatch
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix()
        {
            ProtectedLocationNoBuild.ApplyToLoadedLocations();
        }
    }
}
