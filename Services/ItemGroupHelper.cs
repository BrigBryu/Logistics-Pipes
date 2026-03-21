using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley;

namespace TransportMod.Services
{
    public record GroupDefinition(int[] CategoryIds, string[]? ItemIds = null, bool IsSeasonal = false);

    public static class ItemGroupHelper
    {
        private static readonly Dictionary<string, GroupDefinition> Groups = new()
        {
            // Seasonal groups
            ["Fruit"] = new(new[] { -79 }, IsSeasonal: true),
            ["Vegetables"] = new(new[] { -75 }, IsSeasonal: true),
            ["Flowers"] = new(new[] { -80 }, IsSeasonal: true),
            ["Seeds"] = new(new[] { -74 }, IsSeasonal: true),
            ["Forage"] = new(new[] { -81 }, IsSeasonal: true),
            ["Fish"] = new(new[] { -4 }, IsSeasonal: true),

            // Non-seasonal groups
            ["Animal Products"] = new(new[] { -5, -6 }),
            ["Artisan Goods"] = new(new[] { -26, -27 }),
            ["Mining"] = new(new[] { -2, -12, -15 }),
            ["Fuel"] = new(Array.Empty<int>(), new[] { "(O)388", "(O)709", "(O)382" }),
            ["Bait & Tackle"] = new(new[] { -21, -22 }),
            ["Monster Loot"] = new(new[] { -28 }),
            ["Crafting Materials"] = new(new[] { -8, -16 }),
            ["Cooked Food"] = new(new[] { -7 })
        };

        public static IReadOnlyDictionary<string, GroupDefinition> GetAllGroups() => Groups;

        public static bool ItemMatchesGroup(Item item, string groupName)
        {
            if (!Groups.TryGetValue(groupName, out var def))
                return false;

            if (def.CategoryIds.Contains(item.Category))
                return true;

            if (def.ItemIds != null && def.ItemIds.Contains(item.QualifiedItemId))
                return true;

            return false;
        }

        public static bool ItemMatchesAnyGroup(Item item, HashSet<string> groups)
        {
            return groups.Any(g => ItemMatchesGroup(item, g));
        }

        public static List<string> GetMatchingGroups(Item item, HashSet<string> selectedGroups)
        {
            return selectedGroups.Where(g => ItemMatchesGroup(item, g)).ToList();
        }

        public static bool IsItemInSeasonalGroup(Item item, HashSet<string> selectedGroups)
        {
            var matchingGroups = GetMatchingGroups(item, selectedGroups);
            return matchingGroups.Any(g => Groups.TryGetValue(g, out var def) && def.IsSeasonal);
        }

        // For "seasons only" filter - check if item belongs to ANY seasonal group (not just selected)
        public static bool IsItemInAnySeasonalGroup(Item item)
        {
            return Groups
                .Where(kvp => kvp.Value.IsSeasonal)
                .Any(kvp =>
                {
                    var def = kvp.Value;
                    return def.CategoryIds.Contains(item.Category) ||
                           (def.ItemIds != null && def.ItemIds.Contains(item.QualifiedItemId));
                });
        }
    }
}
