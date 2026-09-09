using EpicLoot;
using EpicLoot.CraftingV2;
using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLootLeslieAlphaTest.src
{
    [HarmonyPatch(typeof(EnchantingUIController), "BuildEnchantedRune")]
    internal class EnchantingUIControllerMod

    {
        [HarmonyPostfix]
        static void Postfix(ItemDrop.ItemData selectedItem, int targetEnchant, float powerModifier,
            ItemDrop.ItemData __result)
        {
            if (__result == null) return;
            MagicItem magicItem = __result.GetMagicItem();
            if (magicItem == null || magicItem.Effects.Count != 1) return;

            // Preserve EpicLoot's modifier rules, but do not cap extraction at the
            // effect definition's maximum. Read the source value, not the capped rune.
            MagicItemEffect sourceEffect = selectedItem.GetMagicItem().Effects[targetEnchant];
            float value = sourceEffect.EffectValue;
            if (!float.IsNaN(powerModifier) && powerModifier > 0f && powerModifier < 999f && value > 1f)
            {
                value *= powerModifier;
            }

            magicItem.Effects[0].EffectValue = (float)Math.Round(value, 2);
            __result.SaveMagicItem(magicItem);
        }
    }

    public static class EnchantingHelper
    {
        internal static bool AwaitingConfirmation = false;
        internal static float PendingDestroyChance = 0f;
        public static float RuneTooPowerfulEtchDestructionChance(ItemDrop.ItemData item, ItemDrop.ItemData rune)
        {
            List<MagicItemEffect> runeEffects = rune.GetMagicItem().Effects;
            MagicItemEffect runeEffect = runeEffects[0];

            var valueType = MagicItemEffectDefinitions.AllDefinitions[runeEffect.EffectType].ValuesPerRarity.GetValueDefForRarity(item.GetRarity());

            if (valueType != null)
            {
                float maxDefaultValue = (MagicItemEffectDefinitions.AllDefinitions[runeEffect.EffectType].ValuesPerRarity.GetValueDefForRarity(item.GetRarity()).MaxValue);


                bool tooPowerful = runeEffect.EffectValue >= maxDefaultValue * 1.1f; // bool value to run a check instead of x > y 
                float howPowerful = ((runeEffect.EffectValue / maxDefaultValue) - 1f); // Effect power over expressed as a %

                Debug.LogWarning($"[EpicLootAlpha] effectValue: {runeEffect.EffectValue}, maxDefault: {maxDefaultValue}, tooPowerful: {tooPowerful}");
                if (tooPowerful)
                {
                    float flatChance = howPowerful * 100f;
                    flatChance = Mathf.Clamp(flatChance, 10f, 90f); // 10% over 10% chance base. sliding to 90% chance to break if over 90% stronger. 10 value to 19 value becomes 90% chance to break on etching that value. 
                    float destroyChance = Mathf.Round(flatChance / 5f) * 5f; // Round to increments of 5%
                    return destroyChance;
                }
            }

            return 0f;
        }
    }

    [HarmonyPatch(typeof(EnchantingUIController), "RuneEnhanceItemAndReturnSuccess")]
    internal class RuneEnhanceItem_DestroyChance_Patch
    {
        [HarmonyPrefix]
        static bool Prefix(ItemDrop.ItemData item, ItemDrop.ItemData rune, int enchantment, ref GameObject __result)
        {

            List<MagicItemEffect> runeEffects = rune.GetMagicItem().Effects;

            MagicItemEffect runeEffect = runeEffects[0];

            var valueType = MagicItemEffectDefinitions.AllDefinitions[runeEffect.EffectType].ValuesPerRarity.GetValueDefForRarity(item.GetRarity());

            if (valueType != null)
            {
                float destroyChance = EnchantingHelper.RuneTooPowerfulEtchDestructionChance(item, rune);

                if (destroyChance > 0f)
                {
                    float roll = UnityEngine.Random.value * 100f;
                    //EpicLoot.LogWarningForce($"Etch roll {roll:F1} destroy chance {destroyChance}%");
                    if (destroyChance > roll)
                    {
                        Player.m_localPlayer.UnequipItem(item);
                        Player.m_localPlayer.GetInventory().RemoveItem(item);
                        Player.m_localPlayer.GetInventory().RemoveItem(rune);
                        Player.m_localPlayer.Message(MessageHud.MessageType.Center, $"You rolled {roll:F1} You are not worthy of the rune's power. The item and rune has been destroyed.");
                        __result = null;
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
