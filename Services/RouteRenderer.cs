using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using System;
using System.Collections.Generic;
using TransportMod.Models;
using TransportMod.Objects;

namespace TransportMod.Services
{
    public class RouteRenderer
    {
        private static readonly Vector2[] DirectionOffsets =
        {
            new(1, 0),   // 0 = right
            new(0, 1),   // 1 = down
            new(-1, 0),  // 2 = left
            new(0, -1)   // 3 = up
        };

        // Bright magenta for all direction arrows (easy to see)
        private static readonly Color ArrowColor = new Color(255, 0, 255);  // Magenta #FF00FF

        private Texture2D? _pixel;

        public void Draw(SpriteBatch spriteBatch, List<PipeRoute> routes, GameLocation location,
                         bool showArrows = true, bool showHighlights = true)
        {
            EnsureTexture(spriteBatch.GraphicsDevice);

            // Draw all pipe tiles with their stored directions
            if (showArrows)
            {
                foreach (var kvp in location.Objects.Pairs)
                {
                    if (kvp.Value is PipeObject pipe)
                    {
                        DrawArrowForDirection(spriteBatch, kvp.Key, pipe.Direction, GetArrowColor());
                    }
                }
            }

            // Highlight sources and destinations based on route type
            // Green = Chest (source or destination)
            // Orange = Machine destination (receiving input)
            // Blue = Machine source (output extraction)
            var highlightedChests = new HashSet<Vector2>();
            var highlightedMachineInputs = new HashSet<Vector2>();
            var highlightedMachineOutputs = new HashSet<Vector2>();
            var highlightedTrashCans = new HashSet<Vector2>();

            foreach (var route in routes)
            {
                switch (route.Type)
                {
                    case RouteType.ChestToMachine:
                        highlightedChests.Add(route.SourcePosition);
                        highlightedMachineInputs.Add(route.DestinationPosition);
                        break;
                    case RouteType.ChestToChest:
                        highlightedChests.Add(route.SourcePosition);
                        highlightedChests.Add(route.DestinationPosition);
                        break;
                    case RouteType.MachineToChest:
                        highlightedMachineOutputs.Add(route.SourcePosition);
                        highlightedChests.Add(route.DestinationPosition);
                        break;
                    case RouteType.TrashCanToChest:
                        highlightedTrashCans.Add(route.SourcePosition);
                        highlightedChests.Add(route.DestinationPosition);
                        break;
                }
            }

            if (showHighlights)
            {
                foreach (var pos in highlightedChests)
                {
                    DrawTileHighlight(spriteBatch, pos, Color.Green * 0.4f);
                }

                foreach (var pos in highlightedMachineInputs)
                {
                    DrawTileHighlight(spriteBatch, pos, Color.Orange * 0.4f);
                }

                foreach (var pos in highlightedMachineOutputs)
                {
                    DrawTileHighlight(spriteBatch, pos, Color.Blue * 0.4f);
                }

                foreach (var pos in highlightedTrashCans)
                {
                    DrawTileHighlight(spriteBatch, pos, Color.Purple * 0.4f);
                }
            }
        }

        public void DrawPlacementPreview(SpriteBatch spriteBatch, Vector2 cursorTile, int direction)
        {
            EnsureTexture(spriteBatch.GraphicsDevice);
            DrawArrowForDirection(spriteBatch, cursorTile, direction, GetArrowColor());
        }

        private Color GetArrowColor()
        {
            return ArrowColor;
        }

        private void DrawArrowForDirection(SpriteBatch spriteBatch, Vector2 tile, int direction, Color color)
        {
            if (direction < 0 || direction >= DirectionOffsets.Length)
                return;

            Vector2 targetTile = tile + DirectionOffsets[direction];
            DrawArrow(spriteBatch, tile, targetTile, color);
        }

        private void EnsureTexture(GraphicsDevice graphicsDevice)
        {
            if (_pixel == null)
            {
                _pixel = new Texture2D(graphicsDevice, 1, 1);
                _pixel.SetData(new[] { Color.White });
            }
        }

        private void DrawTileHighlight(SpriteBatch spriteBatch, Vector2 tile, Color color)
        {
            if (!IsTileVisible(tile))
                return;

            var screenPos = TileToScreen(tile);
            var rect = new Rectangle(
                (int)screenPos.X,
                (int)screenPos.Y,
                Game1.tileSize,
                Game1.tileSize
            );

            spriteBatch.Draw(_pixel!, rect, color);
        }

        private void DrawArrow(SpriteBatch spriteBatch, Vector2 from, Vector2 to, Color arrowColor)
        {
            if (!IsTileVisible(from))
                return;

            Vector2 direction = to - from;
            var screenPos = TileToScreen(from);

            // Center of the tile
            float centerX = screenPos.X + Game1.tileSize / 2f;
            float centerY = screenPos.Y + Game1.tileSize / 2f;

            // Arrow dimensions
            float arrowLength = Game1.tileSize * 0.5f;
            float headSize = Game1.tileSize * 0.25f;
            int lineThickness = 4;

            Color finalColor = arrowColor * 0.9f;

            // Calculate direction angle
            float angle = MathF.Atan2(direction.Y, direction.X);

            // Arrow starts behind center, ends ahead of center
            float startX = centerX - MathF.Cos(angle) * (arrowLength * 0.4f);
            float startY = centerY - MathF.Sin(angle) * (arrowLength * 0.4f);
            float tipX = centerX + MathF.Cos(angle) * (arrowLength * 0.5f);
            float tipY = centerY + MathF.Sin(angle) * (arrowLength * 0.5f);

            // Draw arrow shaft
            DrawLine(spriteBatch, startX, startY, tipX, tipY, lineThickness, finalColor);

            // Draw arrow head (two lines forming a V)
            float headAngle1 = angle + MathF.PI * 0.8f;
            float headAngle2 = angle - MathF.PI * 0.8f;

            float head1X = tipX + MathF.Cos(headAngle1) * headSize;
            float head1Y = tipY + MathF.Sin(headAngle1) * headSize;
            float head2X = tipX + MathF.Cos(headAngle2) * headSize;
            float head2Y = tipY + MathF.Sin(headAngle2) * headSize;

            DrawLine(spriteBatch, tipX, tipY, head1X, head1Y, lineThickness, finalColor);
            DrawLine(spriteBatch, tipX, tipY, head2X, head2Y, lineThickness, finalColor);
        }

        private void DrawLine(SpriteBatch spriteBatch, float x1, float y1, float x2, float y2, int thickness, Color color)
        {
            float length = MathF.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
            float angle = MathF.Atan2(y2 - y1, x2 - x1);

            // Center the line thickness
            var origin = new Vector2(0, 0.5f);

            spriteBatch.Draw(
                _pixel!,
                new Vector2(x1, y1),
                null,
                color,
                angle,
                origin,
                new Vector2(length, thickness),
                SpriteEffects.None,
                0
            );
        }

        private Vector2 TileToScreen(Vector2 tile)
        {
            return new Vector2(
                tile.X * Game1.tileSize - Game1.viewport.X,
                tile.Y * Game1.tileSize - Game1.viewport.Y
            );
        }

        private bool IsTileVisible(Vector2 tile)
        {
            float screenX = tile.X * Game1.tileSize - Game1.viewport.X;
            float screenY = tile.Y * Game1.tileSize - Game1.viewport.Y;

            return screenX >= -Game1.tileSize &&
                   screenX <= Game1.viewport.Width + Game1.tileSize &&
                   screenY >= -Game1.tileSize &&
                   screenY <= Game1.viewport.Height + Game1.tileSize;
        }
    }
}
