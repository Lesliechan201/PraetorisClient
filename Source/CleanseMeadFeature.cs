using System;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace PraetorisClient
{
    internal static class CleanseMeadFeature
    {
        private const string ItemPrefabName = "Warp_MeadCleanse";
        private const string BasePrefabName = "MeadPoisonResist";
        private const string ItemName = "Cleanse Mead";
        private const string ItemDescription = "Removes active status effects and prevents another cleanse for 3 minutes.";
        private const float IconTintStrength = 0.35f;

        private static readonly Color IconTint = new Color(0.45f, 0.95f, 1f, 1f);

        private static bool _registered;

        internal static void Initialize()
        {
            PrefabManager.OnVanillaPrefabsAvailable += Register;
        }

        internal static void Shutdown()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= Register;
            _registered = false;
        }

        private static void Register()
        {
            if (_registered)
            {
                PrefabManager.OnVanillaPrefabsAvailable -= Register;
                return;
            }

            CustomItem customItem = new CustomItem(ItemPrefabName, BasePrefabName, CreateItemConfig());
            if (!customItem.ItemPrefab || !customItem.ItemDrop)
            {
                PraetorisClientPlugin.Log.LogWarning("Failed to clone " + BasePrefabName + " for cleanse mead.");
                return;
            }

            if (!TryCreateCleanseIcon(customItem.ItemDrop, out Sprite icon))
            {
                return;
            }

            CleanseMeadStatusEffect statusEffect = CleanseMeadStatusEffect.Create(icon);
            if (!ItemManager.Instance.AddStatusEffect(new CustomStatusEffect(statusEffect, false)))
            {
                return;
            }

            ConfigureItem(customItem.ItemPrefab, statusEffect, icon);

            if (ItemManager.Instance.AddItem(customItem))
            {
                _registered = true;
                PrefabManager.OnVanillaPrefabsAvailable -= Register;
                PraetorisClientPlugin.Log.LogInfo("Registered cleanse mead.");
            }
        }

        private static ItemConfig CreateItemConfig()
        {
            return new ItemConfig
            {
                Name = ItemName,
                Description = ItemDescription,
                CraftingStation = CraftingStations.MeadKetill,
                Amount = 1,
                Requirements = new[]
                {
                    new RequirementConfig("Honey", 10),
                    new RequirementConfig("Ooze", 5),
                    new RequirementConfig("Pukeberries", 5),
                    new RequirementConfig("SurtlingCore", 1)
                }
            };
        }

        private static void ConfigureItem(GameObject itemPrefab, StatusEffect statusEffect, Sprite icon)
        {
            ItemDrop itemDrop = itemPrefab.GetComponent<ItemDrop>();
            ItemDrop.ItemData.SharedData shared = itemDrop.m_itemData.m_shared;
            shared.m_name = ItemName;
            shared.m_description = ItemDescription;
            shared.m_icons = new[] { icon };
            shared.m_consumeStatusEffect = statusEffect;
        }

        private static bool TryCreateCleanseIcon(ItemDrop itemDrop, out Sprite icon)
        {
            icon = null!;
            Sprite[] icons = itemDrop.m_itemData.m_shared.m_icons;
            if (icons == null || icons.Length == 0 || !icons[0])
            {
                PraetorisClientPlugin.Log.LogWarning("Cleanse mead could not find the cloned " + BasePrefabName + " icon.");
                return false;
            }

            Sprite baseIcon = icons[0];
            Sprite tintedIcon = TryCreateTintedIcon(baseIcon);
            icon = tintedIcon ? tintedIcon : baseIcon;
            return true;
        }

        private static Sprite TryCreateTintedIcon(Sprite sourceIcon)
        {
            Texture2D sourceTexture = sourceIcon.texture;
            Rect sourceRect = sourceIcon.rect;
            int width = Mathf.RoundToInt(sourceRect.width);
            int height = Mathf.RoundToInt(sourceRect.height);

            if (width <= 0 || height <= 0)
            {
                PraetorisClientPlugin.Log.LogWarning("Cleanse mead could not tint the poison mead icon because the icon rectangle is empty.");
                return null!;
            }

            try
            {
                Color[] pixels = sourceTexture.GetPixels(
                    Mathf.RoundToInt(sourceRect.x),
                    Mathf.RoundToInt(sourceRect.y),
                    width,
                    height);

                for (int i = 0; i < pixels.Length; i++)
                {
                    Color pixel = pixels[i];
                    Color tintedPixel = Color.Lerp(pixel, IconTint, IconTintStrength);
                    tintedPixel.a = pixel.a;
                    pixels[i] = tintedPixel;
                }

                Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                texture.name = sourceIcon.name + "_CleanseTint";
                texture.SetPixels(pixels);
                texture.Apply();

                Vector2 pivot = new Vector2(sourceIcon.pivot.x / sourceRect.width, sourceIcon.pivot.y / sourceRect.height);
                Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), pivot, sourceIcon.pixelsPerUnit);
                sprite.name = sourceIcon.name + "_Cleanse";
                return sprite;
            }
            catch (Exception ex)
            {
                PraetorisClientPlugin.Log.LogWarning("Cleanse mead could not tint the poison mead icon. Using the cloned icon unchanged. " + ex.Message);
                return null!;
            }
        }
    }
}
