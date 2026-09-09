using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using EpicLoot;
using EpicLoot.CraftingV2;
using Jotunn.Managers;
using UnityEngine;

[BepInPlugin("odin.rune-extraction-validation", "Rune Extraction Validation", "1.0.0")]
[BepInDependency("randyknapp.mods.epicloot")]
[BepInDependency("warpalicious.PraetorisClient")]
public class RuneExtractionValidation : BaseUnityPlugin
{
    private IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 120f;
        while (PrefabManager.Instance.GetPrefab("EtchedRunestoneMagic") == null || ObjectDB.instance == null)
        {
            if (Time.realtimeSinceStartup > deadline)
            {
                Logger.LogError("RUNE_TEST FAIL: prefab wait timed out");
                Application.Quit();
                yield break;
            }
            yield return new WaitForSeconds(1f);
        }
        yield return new WaitForSeconds(2f);
        try { Run(); }
        catch (Exception exception) { Logger.LogError("RUNE_TEST FAIL: " + exception); }
        yield return new WaitForSeconds(1f);
        Application.Quit();
    }

    private void Run()
    {
        MethodInfo method = typeof(EnchantingUIController).GetMethod("BuildEnchantedRune", BindingFlags.Static | BindingFlags.NonPublic);
        string effectType = MagicItemEffectDefinitions.AllDefinitions.First(entry =>
            entry.Value.ValuesPerRarity?.GetValueDefForRarity(ItemRarity.Magic)?.MaxValue > 1f).Key;
        float max = MagicItemEffectDefinitions.AllDefinitions[effectType].ValuesPerRarity.GetValueDefForRarity(ItemRarity.Magic).MaxValue;
        ItemDrop.ItemData template = PrefabManager.Instance.GetPrefab("EtchedRunestoneMagic").GetComponent<ItemDrop>().m_itemData;
        Check(method, template, effectType, max * 2f, .5f, (float)Math.Round(max, 2), "above maximum preserves enhanced value");
        Check(method, template, effectType, max, .5f, (float)Math.Round(max * .5f, 2), "ordinary extraction");
        Check(method, template, effectType, 1f, .5f, 1f, "boolean effect remains one");
        Check(method, template, effectType, 12.3456f, 0f, 12.35f, "zero modifier fallback rounds");
        Check(method, template, effectType, 12.3456f, float.NaN, 12.35f, "NaN modifier fallback");
        Check(method, template, effectType, 12.3456f, 999f, 12.35f, "999 modifier fallback");
        Check(method, template, effectType, 12.3456f, -1f, 12.35f, "negative modifier fallback");
        ItemDrop.ItemData invalid = Create(template, effectType, float.NaN);
        if (method.Invoke(null, new object[] { invalid, 0, .5f }) != null) throw new Exception("NaN source must return null");
        Logger.LogInfo("RUNE_TEST PASS: NaN source remains null");
        Logger.LogInfo("RUNE_TEST ALL PASS (8 cases)");
    }

    private static ItemDrop.ItemData Create(ItemDrop.ItemData template, string effectType, float value)
    {
        ItemDrop.ItemData item = template.Clone();
        item.SaveMagicItem(new MagicItem { Rarity = ItemRarity.Magic, Effects = new List<MagicItemEffect> { new MagicItemEffect(effectType) { EffectValue = value } } });
        return item;
    }

    private void Check(MethodInfo method, ItemDrop.ItemData template, string effectType, float value, float modifier, float expected, string label)
    {
        ItemDrop.ItemData item = Create(template, effectType, value);
        ItemDrop.ItemData result = (ItemDrop.ItemData)method.Invoke(null, new object[] { item, 0, modifier });
        if (result == null) throw new Exception(label + ": null result");
        MagicItem magic = result.GetMagicItem();
        float actual = magic.Effects[0].EffectValue;
        if (float.IsNaN(actual) || Math.Abs(actual - expected) > .001f) throw new Exception(label + $": expected {expected}, got {actual}");
        if (magic.Rarity != ItemRarity.Magic || magic.Effects[0].EffectType != effectType) throw new Exception(label + ": metadata changed");
        if (item.GetMagicItem().Effects[0].EffectValue != value) throw new Exception(label + ": source changed");
        Logger.LogInfo("RUNE_TEST PASS: " + label + $" = {actual}");
    }
}
