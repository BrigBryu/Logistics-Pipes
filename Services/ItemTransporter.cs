using System;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using System.Collections.Generic;
using TransportMod.Models;

namespace TransportMod.Services
{
    public class ItemTransporter
    {
        private readonly FilterManager _filterManager;
        private readonly IMonitor _monitor;
        private readonly Dictionary<string, int> _lastOutputIndex = new();

        public ItemTransporter(FilterManager filterManager, IMonitor monitor)
        {
            _filterManager = filterManager;
            _monitor = monitor;
        }

        public bool TryTransferWithAlternation(List<PipeRoute> routesFromSource)
        {
            if (routesFromSource.Count == 0)
                return false;

            var first = routesFromSource[0];
            var key = $"{first.Location.Name}:{first.SourcePosition.X}:{first.SourcePosition.Y}";

            _lastOutputIndex.TryGetValue(key, out int lastIndex);

            // Try routes starting from next index, wrapping around (round-robin)
            for (int i = 0; i < routesFromSource.Count; i++)
            {
                int idx = (lastIndex + 1 + i) % routesFromSource.Count;
                if (TryTransfer(routesFromSource[idx]))
                {
                    _lastOutputIndex[key] = idx;
                    return true;
                }
            }
            return false;
        }

        public bool TryTransfer(PipeRoute route)
        {
            return route.Type switch
            {
                RouteType.ChestToMachine => TryChestToMachine(route),
                RouteType.ChestToChest => TryChestToChest(route),
                RouteType.MachineToChest => TryMachineToChest(route),
                RouteType.TrashCanToChest => TryTrashCanToChest(route),
                _ => false
            };
        }

        private bool TryChestToMachine(PipeRoute route)
        {
            // Get chest at source position
            if (!route.Location.Objects.TryGetValue(route.SourcePosition, out var sourceObj) ||
                sourceObj is not Chest chest)
                return false;

            // Get machine at destination position
            if (!route.Location.Objects.TryGetValue(route.DestinationPosition, out var destObj))
                return false;

            // Special handling for CrabPot
            if (destObj is CrabPot crabPot)
                return TryLoadCrabPot(chest, crabPot);

            // Check if machine accepts input and is ready
            if (!destObj.HasContextTag("machine_input") || destObj.heldObject?.Value != null)
                return false;

            // Use AttemptAutoLoad - this handles multi-item recipes like furnace (ore + coal)
            var chestInventory = chest.GetItemsForPlayer();
            return destObj.AttemptAutoLoad(chestInventory, Game1.player);
        }

        private bool TryLoadCrabPot(Chest chest, CrabPot crabPot)
        {
            // Check if crab pot already has bait or needs harvest
            if (crabPot.bait.Value != null || crabPot.heldObject.Value != null)
                return false;

            var chestInventory = chest.GetItemsForPlayer();

            // Find bait in chest
            for (int i = 0; i < chestInventory.Count; i++)
            {
                var item = chestInventory[i];
                if (item == null)
                    continue;

                if (item.Category == StardewValley.Object.baitCategory && item is StardewValley.Object baitObj)
                {
                    if (crabPot.performObjectDropInAction(baitObj, probe: false, Game1.player))
                    {
                        chestInventory[i].Stack--;
                        if (chestInventory[i].Stack <= 0)
                            chestInventory[i] = null;
                        return true;
                    }
                }
            }

            return false;
        }


        private bool TryChestToChest(PipeRoute route)
        {
            // Get source chest
            if (!route.Location.Objects.TryGetValue(route.SourcePosition, out var sourceObj) ||
                sourceObj is not Chest sourceChest)
                return false;

            // Get destination chest
            if (!route.Location.Objects.TryGetValue(route.DestinationPosition, out var destObj) ||
                destObj is not Chest destChest)
                return false;

            // Get destination chest filter
            var filter = _filterManager.GetFilter(destChest);
            var sourceInventory = sourceChest.GetItemsForPlayer();

            // Get flow rate based on lowest tier pipe in route
            int flowRate = route.GetFlowRate();
            int transferred = 0;

            // Transfer up to flowRate items (iterate backwards to avoid index issues when items are removed)
            for (int i = sourceInventory.Count - 1; i >= 0 && transferred < flowRate; i--)
            {
                // Bounds check in case list was modified
                if (i >= sourceInventory.Count)
                    continue;

                var item = sourceInventory[i];
                if (item == null || !filter.Accepts(item))
                    continue;

                // Transfer as many as possible from this stack (up to remaining flow rate)
                int toTransfer = Math.Min(item.Stack, flowRate - transferred);

                for (int j = 0; j < toTransfer; j++)
                {
                    var singleItem = item.getOne();
                    var leftover = destChest.addItem(singleItem);
                    if (leftover == null)
                    {
                        // Successfully added - remove from source
                        sourceInventory[i].Stack--;
                        transferred++;
                        if (sourceInventory[i].Stack <= 0)
                        {
                            sourceInventory[i] = null;
                            break;
                        }
                    }
                    else
                    {
                        // Destination full
                        break;
                    }
                }
            }

            return transferred > 0;
        }

        private bool TryMachineToChest(PipeRoute route)
        {
            // Get machine at source position
            if (!route.Location.Objects.TryGetValue(route.SourcePosition, out var machine))
                return false;

            // Get chest at destination position
            if (!route.Location.Objects.TryGetValue(route.DestinationPosition, out var destObj) ||
                destObj is not Chest chest)
                return false;

            // Check if machine has finished output
            if (machine.heldObject?.Value == null)
                return false;

            // Check if ready for harvest
            if (!machine.readyForHarvest.Value)
                return false;

            _monitor.Log($"Transferring {machine.heldObject.Value.Name} from {machine.Name}", LogLevel.Trace);

            // Check if output passes the destination filter
            var output = machine.heldObject.Value;
            var filter = _filterManager.GetFilter(chest);
            if (!filter.Accepts(output))
                return false;

            // Try to add output to chest
            var leftover = chest.addItem(output);
            if (leftover == null)
            {
                machine.heldObject.Value = null;
                machine.readyForHarvest.Value = false;
                return true;
            }

            return false;
        }

        private bool TryTrashCanToChest(PipeRoute route)
        {
            if (string.IsNullOrEmpty(route.TrashCanId))
                return false;

            // Get chest at destination position
            if (!route.Location.Objects.TryGetValue(route.DestinationPosition, out var destObj) ||
                destObj is not Chest chest)
                return false;

            // Check if this trash can has already been checked today
            if (Game1.netWorldState.Value.CheckedGarbage.Contains(route.TrashCanId))
                return false;

            // Try to get an item from the garbage can
            route.Location.TryGetGarbageItem(route.TrashCanId, Game1.player.DailyLuck, out Item? item, out _, out _);

            if (item != null)
            {
                // Check if output passes the destination filter
                var filter = _filterManager.GetFilter(chest);
                if (!filter.Accepts(item))
                    return false;

                // Try to add to chest
                var leftover = chest.addItem(item);
                if (leftover == null)
                {
                    MarkTrashCanChecked(route.TrashCanId);
                    _monitor.Log($"Collected {item.Name} from trash can {route.TrashCanId}", LogLevel.Trace);
                    return true;
                }
            }

            return false;
        }

        private void MarkTrashCanChecked(string trashCanId)
        {
            if (Game1.netWorldState.Value.CheckedGarbage.Add(trashCanId))
                Game1.stats.Increment("trashCansChecked");
        }
    }
}
