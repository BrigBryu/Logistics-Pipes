using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.GameData.Objects;
using System.Collections.Generic;

namespace TransportMod.Services
{
    public class AssetManager
    {
        private readonly IModHelper _helper;
        private readonly string _modId;

        // All pipe item IDs (unqualified and qualified)
        public static readonly HashSet<string> AllPipeIds = new()
        {
            "bridgerbrundy.TransportMod_WoodenPipe",
            "bridgerbrundy.TransportMod_CopperPipe",
            "bridgerbrundy.TransportMod_IronPipe",
            "bridgerbrundy.TransportMod_GoldPipe",
            "bridgerbrundy.TransportMod_IridiumPipe",
            "(O)bridgerbrundy.TransportMod_WoodenPipe",
            "(O)bridgerbrundy.TransportMod_CopperPipe",
            "(O)bridgerbrundy.TransportMod_IronPipe",
            "(O)bridgerbrundy.TransportMod_GoldPipe",
            "(O)bridgerbrundy.TransportMod_IridiumPipe"
        };


        public AssetManager(IModHelper helper, string modId)
        {
            _helper = helper;
            _modId = modId;
            _helper.Events.Content.AssetRequested += OnAssetRequested;
        }

        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            // Load custom sprite sheet
            if (e.NameWithoutLocale.IsEquivalentTo("Mods/bridgerbrundy.TransportMod/Sprites"))
            {
                e.LoadFromModFile<Texture2D>("assets/sprites.png", AssetLoadPriority.Medium);
            }
            // Load pipe sprite sheets for each tier
            else if (e.NameWithoutLocale.IsEquivalentTo("Mods/bridgerbrundy.TransportMod/PipeSprites/Wood"))
            {
                e.LoadFromModFile<Texture2D>("assets/pipes/wood_sheet.png", AssetLoadPriority.Medium);
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Mods/bridgerbrundy.TransportMod/PipeSprites/Copper"))
            {
                e.LoadFromModFile<Texture2D>("assets/pipes/copper_sheet.png", AssetLoadPriority.Medium);
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Mods/bridgerbrundy.TransportMod/PipeSprites/Iron"))
            {
                e.LoadFromModFile<Texture2D>("assets/pipes/iron_sheet.png", AssetLoadPriority.Medium);
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Mods/bridgerbrundy.TransportMod/PipeSprites/Gold"))
            {
                e.LoadFromModFile<Texture2D>("assets/pipes/gold_sheet.png", AssetLoadPriority.Medium);
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Mods/bridgerbrundy.TransportMod/PipeSprites/Iridium"))
            {
                e.LoadFromModFile<Texture2D>("assets/pipes/iridium_sheet.png", AssetLoadPriority.Medium);
            }
            // Register custom objects
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Objects"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, ObjectData>().Data;
                    RegisterPipes(data);
                });
            }
            // Register crafting recipes
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/CraftingRecipes"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, string>().Data;
                    RegisterCraftingRecipes(data);
                });
            }
        }

        private void RegisterPipes(IDictionary<string, ObjectData> data)
        {
            var pipes = new[]
            {
                ("WoodenPipe", "Wooden Pipe", "Wood", 25),
                ("CopperPipe", "Copper Pipe", "Copper", 50),
                ("IronPipe", "Iron Pipe", "Iron", 100),
                ("GoldPipe", "Gold Pipe", "Gold", 200),
                ("IridiumPipe", "Iridium Pipe", "Iridium", 400)
            };

            foreach (var (id, displayName, tier, price) in pipes)
            {
                var itemId = $"{_modId}_{id}";
                data[itemId] = new ObjectData
                {
                    Name = itemId,
                    DisplayName = displayName,
                    Description = "A pipe for item transport. Place to create directional routes between containers.",
                    Type = "Crafting",
                    Category = -8,
                    Price = price,
                    Texture = $"Mods/bridgerbrundy.TransportMod/PipeSprites/{tier}",
                    SpriteIndex = 2  // Third sprite (0-indexed) for UI/crafting
                };
            }
        }


        private void RegisterCraftingRecipes(IDictionary<string, string> data)
        {
            // Format: "ingredients/Field or Home/output/isBigCraftable/unlockConditions/displayName"
            // unlockConditions: "default" = known immediately, "s SkillName Level" = skill requirement

            // Pipe recipes
            // Wooden Pipe: 10 Wood (388) + 2 Stone (390) = 5 pipes
            data[$"{_modId}_WoodenPipe"] = $"388 10 390 2/Field/{_modId}_WoodenPipe 5/false/default/Wooden Pipe";

            // Copper Pipe: 2 Copper Bar (334) = 5 pipes
            data[$"{_modId}_CopperPipe"] = $"334 2/Field/{_modId}_CopperPipe 5/false/default/Copper Pipe";

            // Iron Pipe: 2 Iron Bar (335) = 5 pipes
            data[$"{_modId}_IronPipe"] = $"335 2/Field/{_modId}_IronPipe 5/false/default/Iron Pipe";

            // Gold Pipe: 2 Gold Bar (336) = 5 pipes
            data[$"{_modId}_GoldPipe"] = $"336 2/Field/{_modId}_GoldPipe 5/false/default/Gold Pipe";

            // Iridium Pipe: 2 Iridium Bar (337) = 5 pipes
            data[$"{_modId}_IridiumPipe"] = $"337 2/Field/{_modId}_IridiumPipe 5/false/default/Iridium Pipe";
        }
    }
}
