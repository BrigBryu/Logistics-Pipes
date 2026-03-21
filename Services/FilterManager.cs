using System;
using System.Text.Json;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using TransportMod.Models;
using TransportMod.UI;

namespace TransportMod.Services
{
    public class FilterManager
    {
        private const string FilterKey = "TransportMod/filter";

        private readonly IMonitor _monitor;

        public FilterManager(IMonitor monitor)
        {
            _monitor = monitor;
        }

        public ChestFilter GetFilter(Chest chest)
        {
            if (chest.modData.TryGetValue(FilterKey, out string? json) && !string.IsNullOrEmpty(json))
            {
                try
                {
                    var filter = JsonSerializer.Deserialize<ChestFilter>(json);
                    if (filter != null)
                        return filter;
                }
                catch (Exception ex)
                {
                    _monitor.Log($"Failed to deserialize chest filter: {ex.Message}", LogLevel.Warn);
                }
            }
            return new ChestFilter();
        }

        public void SetFilter(Chest chest, ChestFilter filter)
        {
            if (filter.IsEmpty)
                chest.modData.Remove(FilterKey);
            else
                chest.modData[FilterKey] = JsonSerializer.Serialize(filter);
        }

        public void ShowFilterMenu(Chest chest)
        {
            var currentFilter = GetFilter(chest);
            var menu = new FilterMenu(chest, currentFilter, this, _monitor);
            Game1.activeClickableMenu = menu;
        }

    }
}
