using Microsoft.Xna.Framework;
using StardewValley;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TransportMod.Models
{
    public enum RouteType
    {
        ChestToMachine,
        ChestToChest,
        MachineToChest,
        TrashCanToChest
    }

    public class PipeRoute
    {
        public Vector2 SourcePosition { get; set; }
        public Vector2 DestinationPosition { get; set; }
        [JsonIgnore]
        public GameLocation Location { get; set; } = null!;
        public string LocationName { get; set; } = string.Empty;
        public List<Vector2> PipePath { get; set; } = new();
        public RouteType Type { get; set; }
        /// <summary>For TrashCanToChest routes, the garbage can ID.</summary>
        public string? TrashCanId { get; set; }
        /// <summary>The lowest tier pipe in the route (1=Wood, 2=Copper, 3=Iron, 4=Gold, 5=Iridium).</summary>
        public int LowestTier { get; set; } = 1;

        /// <summary>Get the number of items that can be transferred per cycle based on lowest tier.</summary>
        public int GetFlowRate()
        {
            return LowestTier switch
            {
                1 => 1,   // Wood: 1 item
                2 => 2,   // Copper: 2 items
                3 => 4,   // Iron: 4 items
                4 => 8,   // Gold: 8 items
                5 => 16,  // Iridium: 16 items
                _ => 1
            };
        }
    }
}
