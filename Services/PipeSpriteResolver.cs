using Microsoft.Xna.Framework;
using StardewValley;
using TransportMod.Objects;

namespace TransportMod.Services
{
    /// <summary>
    /// Simple direction-based pipe sprite resolver.
    ///
    /// Rules (from sprite names):
    /// - If direction is E or W (horizontal): check for pipes to E and W
    /// - If direction is N or S (vertical): check for pipes to N and S
    /// - Select sprite based on which neighbors have pipes
    /// </summary>
    public static class PipeSpriteResolver
    {
        // Direction constants matching PipeObject.Direction
        private const int Right = 0;  // E
        private const int Down = 1;   // S
        private const int Left = 2;   // W
        private const int Up = 3;     // N

        // Sprite indices - matching the simplified sprite order
        // Horizontal (direction E or W)
        private const int H_PIPE_E_AND_W = 0;      // pipes on both sides
        private const int H_PIPE_E_NO_W = 1;       // pipe on E, no pipe on W
        private const int H_PIPE_W_NO_E = 2;       // pipe on W, no pipe on E
        private const int H_NO_CONNECTION = 3;    // no pipe connection

        // Vertical (direction N or S)
        private const int V_PIPE_N_AND_S = 4;      // pipes on both sides
        private const int V_PIPE_N_NO_S = 5;       // pipe on N, no pipe on S
        private const int V_PIPE_S_NO_N = 6;       // pipe on S, no pipe on N
        private const int V_NO_CONNECTION = 7;    // no pipe connection

        // Fallback
        private const int ELSE = 8;

        // Corners
        private const int CORNER_SE = 9;   // direction S or E, pipes E and S
        private const int CORNER_NE = 10;  // direction N or E, pipes E and N
        private const int CORNER_NW = 11;  // direction N or W, pipes N and W
        private const int CORNER_SW = 12;  // direction W or S, pipes W and S

        // T-Junctions (named by which side is MISSING)
        private const int T_SOUTH = 15;    // missing S, open N+E+W
        private const int T_EAST = 14;     // missing E, open N+S+W
        private const int T_NORTH = 13;    // missing N, open E+S+W
        private const int T_WEST = 16;     // missing W, open N+E+S

        // 4-Way Junction
        private const int FOUR_WAY = 17;   // direction N/S, pipes all 4 sides, E and W pointing in

        // Direction offsets
        private static readonly Vector2[] DirectionOffsets = new Vector2[]
        {
            new Vector2(1, 0),   // Right (E)
            new Vector2(0, 1),   // Down (S)
            new Vector2(-1, 0),  // Left (W)
            new Vector2(0, -1)   // Up (N)
        };

        /// <summary>
        /// Check if there's a pipe at the given position.
        /// </summary>
        private static bool HasPipe(GameLocation loc, Vector2 pos)
        {
            return loc.Objects.TryGetValue(pos, out var obj) && obj is PipeObject;
        }

        /// <summary>
        /// Check if there's a flow-compatible pipe connection on a perpendicular side.
        /// Only returns true if the neighbor pipe points INTO this tile.
        /// </summary>
        private static bool IsFlowConnected(GameLocation loc, Vector2 pos, int ourDirection, int side)
        {
            Vector2 neighborPos = pos + DirectionOffsets[side];
            if (!loc.Objects.TryGetValue(neighborPos, out var obj))
                return false;

            if (obj is PipeObject neighborPipe)
            {
                int oppositeSide = (side + 2) % 4;
                // Only connect if neighbor points toward us (into this tile)
                return neighborPipe.Direction == oppositeSide;
            }

            return false;
        }

        /// <summary>
        /// Check if object is a supported endpoint (chest, crab pot, machine).
        /// </summary>
        private static bool IsEndpoint(StardewValley.Object obj)
        {
            if (obj is StardewValley.Objects.Chest)
                return true;
            if (obj is StardewValley.Objects.CrabPot)
                return true;
            // Big craftables are typically machines
            if (obj.bigCraftable.Value)
                return true;
            return false;
        }

        /// <summary>
        /// Check if there's an inline connection.
        /// For pipes, only counts if the neighbor is on the same axis (both horizontal or both vertical).
        /// If includeEndpoints is true, endpoints count (for T/X/corner detection).
        /// </summary>
        private static bool IsInlineOpen(GameLocation loc, Vector2 pos, int ourDirection, int side, bool includeEndpoints)
        {
            Vector2 neighborPos = pos + DirectionOffsets[side];
            if (!loc.Objects.TryGetValue(neighborPos, out var obj))
                return false;

            if (obj is PipeObject neighborPipe)
            {
                // Same axis = inline connection
                bool ourHorizontal = ourDirection == Right || ourDirection == Left;
                bool neighborHorizontal = neighborPipe.Direction == Right || neighborPipe.Direction == Left;
                if (ourHorizontal == neighborHorizontal)
                    return true;

                // If we're pointing INTO this side and there's a pipe there,
                // we're flowing into it - show as connected, not capped
                if (ourDirection == side)
                    return true;

                return false;
            }

            // Endpoints only count when we need them for T/X/corner shapes
            if (includeEndpoints && IsEndpoint(obj))
                return true;

            return false;
        }

        /// <summary>
        /// Get the sprite index for a pipe based on its direction and neighbors.
        /// </summary>
        public static int GetSpriteIndex(GameLocation loc, Vector2 pos, int direction)
        {
            bool isHorizontal = direction == Right || direction == Left;

            // First check perpendicular flow connections
            bool perpN, perpS, perpE, perpW;
            if (isHorizontal)
            {
                perpN = IsFlowConnected(loc, pos, direction, Up);
                perpS = IsFlowConnected(loc, pos, direction, Down);
                perpE = false;
                perpW = false;
            }
            else
            {
                perpN = false;
                perpS = false;
                perpE = IsFlowConnected(loc, pos, direction, Right);
                perpW = IsFlowConnected(loc, pos, direction, Left);
            }

            bool hasPerpendicularConnection = perpN || perpS || perpE || perpW;

            // Inline sides: include endpoints only if we have perpendicular connections
            // (for T/X/corner shapes). Otherwise endpoints get end caps.
            bool openN, openS, openE, openW;

            if (isHorizontal)
            {
                openE = IsInlineOpen(loc, pos, direction, Right, hasPerpendicularConnection);
                openW = IsInlineOpen(loc, pos, direction, Left, hasPerpendicularConnection);
                openN = perpN;
                openS = perpS;
            }
            else
            {
                openN = IsInlineOpen(loc, pos, direction, Up, hasPerpendicularConnection);
                openS = IsInlineOpen(loc, pos, direction, Down, hasPerpendicularConnection);
                openE = perpE;
                openW = perpW;
            }

            int openCount = (openN ? 1 : 0) + (openS ? 1 : 0) + (openE ? 1 : 0) + (openW ? 1 : 0);

            int result;

            // 4-way
            if (openN && openS && openE && openW)
                result = FOUR_WAY;
            // T-junctions (3 open)
            else if (openCount == 3)
            {
                if (!openN) result = T_SOUTH;
                else if (!openE) result = T_EAST;
                else if (!openS) result = T_NORTH;
                else result = T_WEST;
            }
            // Corners (2 perpendicular)
            else if (openCount == 2 && openN && openE) result = CORNER_NE;
            else if (openCount == 2 && openN && openW) result = CORNER_NW;
            else if (openCount == 2 && openS && openE) result = CORNER_SE;
            else if (openCount == 2 && openS && openW) result = CORNER_SW;
            // Straight pipes
            else if (isHorizontal)
            {
                if (openE && openW) result = H_PIPE_E_AND_W;
                else if (openE) result = H_PIPE_E_NO_W;
                else if (openW) result = H_PIPE_W_NO_E;
                else result = H_NO_CONNECTION;
            }
            else
            {
                if (openN && openS) result = V_PIPE_N_AND_S;
                else if (openN) result = V_PIPE_N_NO_S;
                else if (openS) result = V_PIPE_S_NO_N;
                else result = V_NO_CONNECTION;
            }

            return result;
        }

        /// <summary>
        /// Sprite ordering in the combined sheet.
        /// </summary>
        public static readonly string[] SpriteOrder = new string[]
        {
            // Horizontal (direction E or W)
            "iron_pipe_direction_E_or_W_pipe_E_and_W",      // 0
            "iron_pipe_direction_E_or_W_pipe_E_no_pipe_W",  // 1
            "iron_pipe_direction_E_or_W_pipe_W_no_pipe_E",  // 2
            "iron_direction_E_or_W_no_pipe_conection",      // 3

            // Vertical (direction N or S)
            "iron_pipe_direction_N_or_S_pipe_N_and_S",      // 4
            "iron_pipe_direction_N_or_S_pipe_N_no_pipe_S",  // 5
            "iron_pipe_direction_N_or_S_pipe_S_no_pipe_N",  // 6
            "iron_direction_N_or_S_no_pipe_conection",      // 7

            // Fallback
            "iron_else",                                     // 8

            // Corners
            "iron_pipe_direction_S_or_E_pipe_E_and_S",       // 9
            "iron_pipe_direction_N_or_E_pipe_E_and_N",       // 10
            "iron_pipe_direction_N_or_W_pipe_N_and_W",       // 11
            "iron_pipe_direction_W_or_S_pipe_W_and_S",       // 12

            // T-Junctions
            "iron_pipe_direction_N_or_E_or_W_pipe_E_and_S_and_W",   // 13
            "iron_pipe_direction_N_or_W_or_S_pipe_N_and_W_and_S",   // 14
            "iron_pipe_direction_E_or_W_or_S_pipe_E_and_W_and_S",   // 15
            "iron_pipe_direction_N_or_E_or_S_pipe_N_and_E_and_S",   // 16

            // 4-Way Junction
            "iron_pipe_direction_N_or_S_pipe_all_4_E_and_W_pointing_in",   // 17
        };

        /// <summary>
        /// Get the texture path for a pipe tier based on its itemId.
        /// </summary>
        public static string GetTexturePathForTier(string itemId)
        {
            // Extract tier from itemId (e.g., "bridgerbrundy.TransportMod_WoodenPipe" -> "Wood")
            if (itemId.Contains("Wooden") || itemId.Contains("Wood"))
                return "Mods/bridgerbrundy.TransportMod/PipeSprites/Wood";
            if (itemId.Contains("Copper"))
                return "Mods/bridgerbrundy.TransportMod/PipeSprites/Copper";
            if (itemId.Contains("Gold"))
                return "Mods/bridgerbrundy.TransportMod/PipeSprites/Gold";
            if (itemId.Contains("Iridium"))
                return "Mods/bridgerbrundy.TransportMod/PipeSprites/Iridium";
            // Default to Iron
            return "Mods/bridgerbrundy.TransportMod/PipeSprites/Iron";
        }
    }
}
