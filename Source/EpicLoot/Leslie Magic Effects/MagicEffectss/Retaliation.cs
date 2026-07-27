using EpicLootAPI;
//using EpicLoot;
using EpicLootLeslieAlphaTest.src.StatusEffects;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Attack;

namespace EpicLootLeslieAlphaTest.src.MagicEffectss
{
    public static class Retaliation
    {
        private static readonly Stopwatch chainSw = new Stopwatch();
        private static readonly Stopwatch attackSw = new Stopwatch();
        private static long lastNow = 0;
        public static string AttackType;

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.BlockAttack))]

        public class Block_Attack_Retaliate_Patch
        {
            public static void Postfix(Humanoid __instance, bool __result)
            {
                if (!Player.m_localPlayer.HasActiveMagicEffect("Retaliation", out float _, 1f) ||
                    __result == false ||
                    __instance != Player.m_localPlayer) return;

                SEMan seman = __instance.GetSEMan();
                var existing = seman.GetStatusEffect(SE_Retaliation.EffectName.GetStableHashCode()) as SE_Retaliation;
                if (existing != null)
                {
                    existing.AddStack();
                }
                else
                {
                    var se = seman.AddStatusEffect(SE_Retaliation.EffectName.GetStableHashCode()) as SE_Retaliation;
                    se.AddStack();
                }
            }
        }

        [HarmonyPatch(typeof(Attack), nameof(Attack.Start))]
        public class Humanoid_Attack_Patch
        {
            static void Prefix(Attack __instance, Humanoid character)
            {
                if (character != Player.m_localPlayer || !Player.m_localPlayer.HasActiveMagicEffect("Retaliation", out float _, 1f)) return;
                AttackType = __instance.m_attackAnimation;
                Jotunn.Logger.LogError($" Attack Start logs {AttackType} character anim speed {Player.m_localPlayer.m_animator.speed} player attack anim{Player.m_localPlayer.m_actionAnimation}");
                //var se = Player.m_localPlayer.GetSEMan().GetStatusEffect(SE_Retaliation.EffectName.GetStableHashCode()) as SE_Retaliation;
                //if (!__instance.m_character.InAttack() && se.m_time >= .3f) se.ConsumeStack();
            }
        }
    }
}
