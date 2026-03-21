using System.Collections.Generic;
using System.Linq;
using StardewValley;

namespace TransportMod.Services
{
    public static class SeasonHelper
    {
        public static bool ItemMatchesSeason(Item item, string season)
        {
            var tags = item.GetContextTags();
            var seasonLower = season.ToLower();

            // Check for exact season tags used by Stardew Valley
            // Primary: season_spring, season_summer, season_fall, season_winter
            // Also check for fish and forage patterns
            return tags.Contains($"season_{seasonLower}") ||
                   tags.Any(t => t.StartsWith($"fish_{seasonLower}_")) ||
                   tags.Any(t => t.StartsWith($"forage_{seasonLower}"));
        }

        public static bool ItemMatchesAnySelectedSeason(Item item, HashSet<string> seasons)
        {
            if (seasons.Count == 0)
                return true; // No season filter = all items pass

            return seasons.Any(s => ItemMatchesSeason(item, s));
        }
    }
}
