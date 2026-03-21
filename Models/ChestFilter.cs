using StardewValley;
using System.Collections.Generic;
using TransportMod.Services;

namespace TransportMod.Models
{
    public class ChestFilter
    {
        public bool IsBlockMode { get; set; } = false;
        public HashSet<string> AllowedGroups { get; set; } = new();
        public HashSet<string> AllowedSeasons { get; set; } = new();
        public HashSet<string> AllowedItemIds { get; set; } = new();

        public bool IsEmpty => AllowedGroups.Count == 0 &&
                               AllowedSeasons.Count == 0 &&
                               AllowedItemIds.Count == 0;

        public bool Accepts(Item item)
        {
            if (IsEmpty)
                return true;

            bool hasItemFilter = AllowedItemIds.Count > 0;
            bool hasGroupFilter = AllowedGroups.Count > 0;
            bool hasSeasonFilter = AllowedSeasons.Count > 0;

            // Specific item IDs always checked first
            if (AllowedItemIds.Contains(item.QualifiedItemId))
                return !IsBlockMode;

            // If only item IDs are set (no groups/seasons), items not in list get opposite treatment
            if (hasItemFilter && !hasGroupFilter && !hasSeasonFilter)
                return IsBlockMode;

            // Case 1: Groups only (no seasons)
            if (hasGroupFilter && !hasSeasonFilter)
            {
                bool matches = ItemGroupHelper.ItemMatchesAnyGroup(item, AllowedGroups);
                return IsBlockMode ? !matches : matches;
            }

            // Case 2: Seasons only (no groups) - accept all seasonal items from those seasons
            if (!hasGroupFilter && hasSeasonFilter)
            {
                bool isSeasonalItem = ItemGroupHelper.IsItemInAnySeasonalGroup(item);
                if (!isSeasonalItem)
                    return IsBlockMode; // Non-seasonal blocked when seasons selected

                bool matchesSeason = SeasonHelper.ItemMatchesAnySelectedSeason(item, AllowedSeasons);
                return IsBlockMode ? !matchesSeason : matchesSeason;
            }

            // Case 3: Both groups AND seasons
            if (hasGroupFilter && hasSeasonFilter)
            {
                // Must match a selected group first
                if (!ItemGroupHelper.ItemMatchesAnyGroup(item, AllowedGroups))
                    return IsBlockMode;

                // Check if this item is in a seasonal group
                bool isInSeasonalGroup = ItemGroupHelper.IsItemInSeasonalGroup(item, AllowedGroups);

                if (isInSeasonalGroup)
                {
                    // Seasonal item - must also match season
                    bool matchesSeason = SeasonHelper.ItemMatchesAnySelectedSeason(item, AllowedSeasons);
                    return IsBlockMode ? !matchesSeason : matchesSeason;
                }
                else
                {
                    // Non-seasonal item in selected group - blocked when seasons selected
                    return IsBlockMode;
                }
            }

            return !IsBlockMode;
        }
    }
}
