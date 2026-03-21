using System;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using System.Collections.Generic;
using System.Linq;
using TransportMod.Models;
using TransportMod.Objects;

namespace TransportMod.Services
{
    public class RouteScanner
    {
        private const int MaxPathLength = 100;
        private readonly IMonitor _monitor;

        public RouteScanner(IMonitor monitor)
        {
            _monitor = monitor;
        }

        private static readonly Vector2[] DirectionOffsets =
        {
            new(1, 0),   // 0 = right
            new(0, 1),   // 1 = down
            new(-1, 0),  // 2 = left
            new(0, -1)   // 3 = up
        };

        /// <summary>Get the tier of a pipe (1=Wood, 2=Copper, 3=Iron, 4=Gold, 5=Iridium).</summary>
        private static int GetPipeTier(string itemId)
        {
            if (itemId.Contains("Wooden") || itemId.Contains("Wood"))
                return 1;
            if (itemId.Contains("Copper"))
                return 2;
            if (itemId.Contains("Iron"))
                return 3;
            if (itemId.Contains("Gold"))
                return 4;
            if (itemId.Contains("Iridium"))
                return 5;
            return 1; // Default to lowest tier
        }

        public List<PipeRoute> ScanLocation(GameLocation location)
        {
            var routes = new List<PipeRoute>();
            var chests = FindChests(location);
            var machines = FindMachines(location);

            _monitor.Log($"Found {chests.Count} chests, {machines.Count} machines", LogLevel.Trace);

            // Scan routes starting from chests (→ machine or → chest)
            foreach (var chestPos in chests)
            {
                foreach (var neighbor in GetAdjacentTiles(chestPos))
                {
                    var route = FollowPipeFromTile(location, chestPos, neighbor, isSourceChest: true);
                    if (route != null)
                    {
                        routes.Add(route);
                    }
                }
            }

            // Scan routes starting from machines (→ chest only)
            foreach (var machinePos in machines)
            {
                foreach (var neighbor in GetAdjacentTiles(machinePos))
                {
                    var route = FollowPipeFromTile(location, machinePos, neighbor, isSourceChest: false);
                    if (route != null)
                    {
                        routes.Add(route);
                    }
                }
            }

            // Scan routes starting from trash cans (tile-based, → chest only)
            var trashCans = FindTrashCans(location);
            if (trashCans.Count > 0)
                _monitor.Log($"Found {trashCans.Count} trash cans in {location.Name}", LogLevel.Debug);
            foreach (var (trashPos, trashCanId) in trashCans)
            {
                foreach (var neighbor in GetAdjacentTiles(trashPos))
                {
                    var route = FollowPipeFromTrashCan(location, trashPos, neighbor, trashCanId);
                    if (route != null)
                    {
                        _monitor.Log($"Created route from trash can {trashCanId} at {trashPos} to chest at {route.DestinationPosition}", LogLevel.Debug);
                        routes.Add(route);
                    }
                }
            }

            return routes;
        }

        private PipeRoute? FollowPipeFromTrashCan(GameLocation location, Vector2 source, Vector2 start, string trashCanId)
        {
            if (!TryGetPipeDirection(location, start, out int direction, out int tier))
                return null;

            var path = new List<Vector2> { start };
            var current = start;
            var visited = new HashSet<Vector2> { start };
            int lowestTier = tier;

            while (true)
            {
                Vector2 next = GetNextTile(current, direction);

                // Check if we hit a chest
                if (IsChest(location, next))
                {
                    return new PipeRoute
                    {
                        SourcePosition = source,
                        DestinationPosition = next,
                        Location = location,
                        LocationName = location.Name,
                        PipePath = path,
                        Type = RouteType.TrashCanToChest,
                        TrashCanId = trashCanId,
                        LowestTier = lowestTier
                    };
                }

                // Check if next tile is a pipe
                if (!TryGetPipeDirection(location, next, out direction, out tier))
                    break;  // Dead end

                lowestTier = Math.Min(lowestTier, tier);

                if (visited.Contains(next))
                    break;  // Loop detected

                if (path.Count > MaxPathLength)
                    break;  // Path too long

                visited.Add(next);
                path.Add(next);
                current = next;
            }

            return null;  // No valid destination found
        }

        private List<(Vector2 position, string trashCanId)> FindTrashCans(GameLocation location)
        {
            var trashCans = new List<(Vector2, string)>();

            // Scan tile properties for "Action Garbage" on the Buildings layer
            var map = location.Map;
            if (map == null)
                return trashCans;

            var buildingsLayer = map.GetLayer("Buildings");
            if (buildingsLayer == null)
                return trashCans;

            for (int x = 0; x < buildingsLayer.LayerWidth; x++)
            {
                for (int y = 0; y < buildingsLayer.LayerHeight; y++)
                {
                    string? action = location.doesTileHaveProperty(x, y, "Action", "Buildings");
                    if (string.IsNullOrWhiteSpace(action))
                        continue;

                    string[] fields = action.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (fields.Length >= 2 && string.Equals(fields[0], "Garbage", StringComparison.OrdinalIgnoreCase))
                    {
                        var pos = new Vector2(x, y);
                        var trashCanId = GetActualTrashCanId(fields[1]);
                        trashCans.Add((pos, trashCanId));
                    }
                }
            }

            return trashCans;
        }

        /// <summary>Map legacy trash can IDs to current IDs.</summary>
        private string GetActualTrashCanId(string id)
        {
            return id switch
            {
                "0" => "JodiAndKent",
                "1" => "EmilyAndHaley",
                "2" => "Mayor",
                "3" => "Museum",
                "4" => "Blacksmith",
                "5" => "Saloon",
                "6" => "Evelyn",
                "7" => "JojaMart",
                _ => id
            };
        }

        private PipeRoute? FollowPipeFromTile(GameLocation location, Vector2 source, Vector2 start, bool isSourceChest)
        {
            if (!TryGetPipeDirection(location, start, out int direction, out int tier))
                return null;

            var path = new List<Vector2> { start };
            var current = start;
            var visited = new HashSet<Vector2> { start };
            int lowestTier = tier;

            while (true)
            {
                Vector2 next = GetNextTile(current, direction);

                // Check if we hit a chest
                if (IsChest(location, next))
                {
                    // Chest → Chest or Machine → Chest
                    return new PipeRoute
                    {
                        SourcePosition = source,
                        DestinationPosition = next,
                        Location = location,
                        LocationName = location.Name,
                        PipePath = path,
                        Type = isSourceChest ? RouteType.ChestToChest : RouteType.MachineToChest,
                        LowestTier = lowestTier
                    };
                }

                // Check if we hit a machine (only valid if source is a chest)
                if (IsMachine(location, next))
                {
                    if (isSourceChest)
                    {
                        return new PipeRoute
                        {
                            SourcePosition = source,
                            DestinationPosition = next,
                            Location = location,
                            LocationName = location.Name,
                            PipePath = path,
                            Type = RouteType.ChestToMachine,
                            LowestTier = lowestTier
                        };
                    }
                    // Machine → Machine not supported
                    return null;
                }

                // Check if next tile is a pipe
                if (!TryGetPipeDirection(location, next, out direction, out tier))
                    break;  // Dead end

                lowestTier = Math.Min(lowestTier, tier);

                if (visited.Contains(next))
                    break;  // Loop detected

                if (path.Count > MaxPathLength)
                    break;  // Path too long

                visited.Add(next);
                path.Add(next);
                current = next;
            }

            return null;  // No valid destination found
        }

        private bool TryGetPipeDirection(GameLocation location, Vector2 pos, out int direction, out int tier)
        {
            direction = 0;
            tier = 1;
            if (!location.Objects.TryGetValue(pos, out var obj))
                return false;

            if (obj is PipeObject pipe)
            {
                direction = pipe.Direction;
                tier = GetPipeTier(pipe.ItemId);
                return true;
            }
            return false;
        }

        private Vector2 GetNextTile(Vector2 current, int direction)
        {
            if (direction < 0 || direction >= DirectionOffsets.Length)
                return current;
            return current + DirectionOffsets[direction];
        }

        private bool IsMachine(GameLocation location, Vector2 pos)
        {
            if (!location.Objects.TryGetValue(pos, out var obj))
                return false;
            // A machine has machine data and is not a chest
            // CrabPot is a special case - it's a machine but doesn't use GetMachineData()
            if (obj is Chest)
                return false;
            return obj.GetMachineData() != null || obj is CrabPot;
        }

        private List<Vector2> FindChests(GameLocation location)
        {
            return location.Objects.Pairs
                .Where(pair => pair.Value is Chest)
                .Select(pair => pair.Key)
                .ToList();
        }

        private List<Vector2> FindMachines(GameLocation location)
        {
            return location.Objects.Pairs
                .Where(pair => pair.Value is not Chest && (pair.Value.GetMachineData() != null || pair.Value is CrabPot))
                .Select(pair => pair.Key)
                .ToList();
        }

        private bool IsChest(GameLocation location, Vector2 pos)
        {
            return location.Objects.TryGetValue(pos, out var obj) && obj is Chest;
        }

        private IEnumerable<Vector2> GetAdjacentTiles(Vector2 pos)
        {
            yield return new Vector2(pos.X - 1, pos.Y);
            yield return new Vector2(pos.X + 1, pos.Y);
            yield return new Vector2(pos.X, pos.Y - 1);
            yield return new Vector2(pos.X, pos.Y + 1);
        }
    }
}
