using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Tools;
using TransportMod.Services;

namespace TransportMod.Objects
{
    public class PipeObject : StardewValley.Object
    {
        public const string DirectionKey = "TransportMod/direction";
        private const string TexturePath = "Mods/bridgerbrundy.TransportMod/Sprites";

        public int Direction
        {
            get
            {
                if (modData.TryGetValue(DirectionKey, out var d) &&
                    int.TryParse(d, out var dir) && dir >= 0 && dir < 4)
                    return dir;
                return 0;
            }
            set => modData[DirectionKey] = Math.Clamp(value, 0, 3).ToString();
        }

        /// <summary>
        /// Constructor for placement.
        /// </summary>
        public PipeObject(string itemId, Vector2 tile, int direction)
            : base(itemId, 1)
        {
            TileLocation = tile;
            Direction = direction;
            // Clear bounding box so pipes don't block movement
            boundingBox.Value = Rectangle.Empty;
        }

        /// <summary>
        /// Parameterless constructor for deserialization.
        /// </summary>
        public PipeObject() : base()
        {
            boundingBox.Value = Rectangle.Empty;
        }

        /// <summary>
        /// Make passable so player can walk through pipes.
        /// </summary>
        public override bool isPassable() => true;

        /// <summary>
        /// Handle tool actions - drop the correct pipe item when broken.
        /// </summary>
        public override bool performToolAction(Tool t)
        {
            if (t is Pickaxe or Axe or Hoe)
            {
                var location = Location;
                var tile = TileLocation;

                // Create our drop
                var pipeItem = ItemRegistry.Create(ItemId, 1);
                Game1.createItemDebris(pipeItem, tile * 64f, -1, location);
                location.playSound("hammer");

                // Remove the object ourselves
                location.Objects.Remove(tile);

                // Return false - we handled everything, game should not process further
                return false;
            }
            return false;
        }

        /// <summary>
        /// Prevent default drop behavior when removed.
        /// </summary>
        public override void performRemoveAction()
        {
            // Don't call base - we handle drops in performToolAction
        }

        /// <summary>
        /// Custom draw using our sprite sheet with connection-based sprite selection.
        /// </summary>
        public override void draw(SpriteBatch spriteBatch, int x, int y, float alpha = 1)
        {
            if (Location == null)
                return;

            int spriteIndex = PipeSpriteResolver.GetSpriteIndex(Location, TileLocation, Direction);

            // Load the appropriate texture for this pipe's tier
            var texturePath = PipeSpriteResolver.GetTexturePathForTier(ItemId);
            var texture = Game1.content.Load<Texture2D>(texturePath);

            // Calculate source rectangle (16x16 sprites arranged horizontally)
            var sourceRect = new Rectangle(spriteIndex * 16, 0, 16, 16);

            // Calculate screen position (ground level, no y offset)
            var screenPos = Game1.GlobalToLocal(Game1.viewport, new Vector2(x * 64, y * 64));

            // Draw the sprite at ground level (low layer depth so player walks over it)
            spriteBatch.Draw(
                texture,
                screenPos,
                sourceRect,
                Color.White * alpha,
                0f,
                Vector2.Zero,
                4f, // Scale to 64x64 (16 * 4)
                SpriteEffects.None,
                0f // Draw at lowest layer (ground)
            );
        }
    }
}
