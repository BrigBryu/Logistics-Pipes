using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;
using TransportMod.Models;
using TransportMod.Objects;
using TransportMod.Services;

namespace TransportMod
{
    public class ModEntry : Mod
    {
        public static ModEntry? Instance { get; private set; }
        private TransportConfig _config = null!;
        private AssetManager _assetManager = null!;
        private RouteScanner _scanner = null!;
        private ItemTransporter _transporter = null!;
        private RouteRenderer _renderer = null!;
        private FilterManager _filterManager = null!;
        private List<PipeRoute> _routes = new();
        private int _ticksSinceLastScan;
        private int _ticksSinceLastTransfer;
        private int _placementDirection;  // 0=right, 1=down, 2=left, 3=up
        private bool _isHoldingPipe;
        private string? _heldPipeItemId;
        private bool _showPipeArrows = false;
        private bool _showHighlights = false;

        public override void Entry(IModHelper helper)
        {
            Instance = this;
            _config = helper.ReadConfig<TransportConfig>();

            // Validate config values
            if (_config.TransferIntervalSeconds < 1)
                _config.TransferIntervalSeconds = 1;
            if (_config.RouteScanIntervalSeconds < 1)
                _config.RouteScanIntervalSeconds = 1;

            _assetManager = new AssetManager(helper, ModManifest.UniqueID);
            _scanner = new RouteScanner(Monitor);
            _filterManager = new FilterManager(Monitor);
            _transporter = new ItemTransporter(_filterManager, Monitor);
            _renderer = new RouteRenderer();

            Monitor.Log("Transport Mod loaded!", LogLevel.Info);

            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.Saving += OnSaving;
            helper.Events.GameLoop.Saved += OnSaved;
            helper.Events.Player.Warped += OnWarped;
            helper.Events.GameLoop.OneSecondUpdateTicked += OnOneSecondUpdateTicked;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.Display.RenderedWorld += OnRenderedWorld;
        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            try
            {
                RestorePipeObjects();
                RescanRoutes();
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error during save load: {ex}", LogLevel.Error);
            }
        }

        private void OnSaving(object? sender, SavingEventArgs e)
        {
            try
            {
                _routes.Clear();
                ConvertPipesToVanilla();
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error during save: {ex}", LogLevel.Error);
            }
        }

        private void OnSaved(object? sender, SavedEventArgs e)
        {
            try
            {
                RestorePipeObjects();
                RescanRoutes();
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error after save: {ex}", LogLevel.Error);
            }
        }

        private void ConvertPipesToVanilla()
        {
            foreach (var location in Game1.locations)
            {
                if (location == null)
                    continue;

                ConvertPipesInLocation(location);

                foreach (var building in location.buildings.ToList())
                {
                    if (building.indoors.Value != null)
                        ConvertPipesInLocation(building.indoors.Value);
                }
            }
        }

        private void ConvertPipesInLocation(GameLocation location)
        {
            var toReplace = new List<(Vector2 tile, PipeObject pipe)>();

            foreach (var pair in location.Objects.Pairs)
            {
                if (pair.Value is PipeObject pipe)
                    toReplace.Add((pair.Key, pipe));
            }

            foreach (var (tile, pipe) in toReplace)
            {
                // Create vanilla object with same item ID
                var vanilla = new StardewValley.Object(pipe.ItemId, 1);
                vanilla.TileLocation = tile;
                // Store direction in modData so we can restore it
                vanilla.modData[PipeObject.DirectionKey] = pipe.Direction.ToString();
                location.Objects[tile] = vanilla;
            }
        }

        private void RestorePipeObjects()
        {
            foreach (var location in Game1.locations)
            {
                if (location == null)
                    continue;

                RestorePipesInLocation(location);

                foreach (var building in location.buildings.ToList())
                {
                    if (building.indoors.Value != null)
                        RestorePipesInLocation(building.indoors.Value);
                }
            }
        }

        private void RestorePipesInLocation(GameLocation location)
        {
            var toReplace = new List<(Vector2 tile, StardewValley.Object obj)>();

            foreach (var pair in location.Objects.Pairs)
            {
                // Check if this is a pipe item (by item ID) that needs to be converted
                if (pair.Value is not PipeObject &&
                    (AssetManager.AllPipeIds.Contains(pair.Value.QualifiedItemId) ||
                     AssetManager.AllPipeIds.Contains(pair.Value.ItemId)))
                {
                    toReplace.Add((pair.Key, pair.Value));
                }
            }

            foreach (var (tile, obj) in toReplace)
            {
                // Get direction from modData, default to 0
                int direction = 0;
                if (obj.modData.TryGetValue(PipeObject.DirectionKey, out var dirStr) &&
                    int.TryParse(dirStr, out var d))
                {
                    direction = d;
                }

                var pipe = new PipeObject(obj.ItemId, tile, direction);
                location.Objects[tile] = pipe;
            }
        }

        private void OnWarped(object? sender, WarpedEventArgs e)
        {
            RescanRoutes();
        }

        private void OnOneSecondUpdateTicked(object? sender, OneSecondUpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            _ticksSinceLastScan++;
            _ticksSinceLastTransfer++;

            // Periodic route rescan
            if (_ticksSinceLastScan >= _config.RouteScanIntervalSeconds)
            {
                RescanRoutes();
            }

            // Transfer items
            if (_ticksSinceLastTransfer >= _config.TransferIntervalSeconds)
            {
                _ticksSinceLastTransfer = 0;
                TransferItems();
            }
        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady)
            {
                _isHoldingPipe = false;
                _heldPipeItemId = null;
                return;
            }

            var player = Game1.player;
            var item = player?.CurrentItem;

            if (item == null)
            {
                _isHoldingPipe = false;
                _heldPipeItemId = null;
                return;
            }

            // Check if holding a pipe item
            _isHoldingPipe = AssetManager.AllPipeIds.Contains(item.QualifiedItemId) ||
                             AssetManager.AllPipeIds.Contains(item.ItemId);
            _heldPipeItemId = _isHoldingPipe ? item.ItemId : null;
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            if (e.Button == SButton.F5)
            {
                RescanRoutes();
                Monitor.Log($"Manual rescan complete. Found {_routes.Count} routes.", LogLevel.Info);
            }
            else if (e.Button == SButton.R && _isHoldingPipe)
            {
                _placementDirection = (_placementDirection + 1) % 4;
                string[] dirNames = { "Right", "Down", "Left", "Up" };
                Monitor.Log($"Pipe direction: {dirNames[_placementDirection]}", LogLevel.Info);
            }
            else if (_config.TogglePipeArrowsKey.JustPressed())
            {
                _showPipeArrows = !_showPipeArrows;
                Monitor.Log($"Pipe arrows: {(_showPipeArrows ? "ON" : "OFF")}", LogLevel.Info);
            }
            else if (_config.ToggleHighlightsKey.JustPressed())
            {
                _showHighlights = !_showHighlights;
                Monitor.Log($"Chest/machine highlights: {(_showHighlights ? "ON" : "OFF")}", LogLevel.Info);
            }
            else if (_config.OpenFilterMenuKey.JustPressed())
            {
                Monitor.Log("Filter key pressed!", LogLevel.Info);
                TryOpenFilterMenu();
            }
            else if (_isHoldingPipe && _heldPipeItemId != null && e.Button.IsUseToolButton())
            {
                TryPlacePipe(e);
            }
        }

        private void TryPlacePipe(ButtonPressedEventArgs e)
        {
            var location = Game1.currentLocation;
            if (location == null)
                return;

            var cursorTile = Game1.currentCursorTile;

            // Check if there's an object at this tile
            if (location.Objects.TryGetValue(cursorTile, out var existingObj))
            {
                // Only allow replacing other pipes, not chests/machines/etc
                if (existingObj is not PipeObject existingPipe)
                    return;

                // Create item from the existing pipe
                var oldPipeItem = ItemRegistry.Create(existingPipe.ItemId, 1);

                // Try to add to player inventory first
                if (!Game1.player.addItemToInventoryBool(oldPipeItem))
                {
                    // Inventory full - drop as debris
                    Game1.createItemDebris(oldPipeItem, cursorTile * 64f, -1, location);
                }

                // Remove the old pipe
                location.Objects.Remove(cursorTile);
            }

            // Check if cursor is within placement range
            if (!Utility.tileWithinRadiusOfPlayer((int)cursorTile.X, (int)cursorTile.Y, 2, Game1.player))
                return;

            // Suppress the default action
            Helper.Input.Suppress(e.Button);

            // Create pipe object at cursor tile
            var pipeObj = new PipeObject(_heldPipeItemId!, cursorTile, _placementDirection);
            location.Objects.Add(cursorTile, pipeObj);

            // Remove one item from player's inventory
            Game1.player.reduceActiveItemByOne();

            // Play placement sound
            location.playSound("stoneStep");

            Monitor.Log($"Placed pipe at {cursorTile} with direction {_placementDirection}", LogLevel.Debug);

            // Trigger route rescan
            RescanRoutes();
        }

        private void TryOpenFilterMenu()
        {
            Monitor.Log("TryOpenFilterMenu called", LogLevel.Info);

            if (Game1.activeClickableMenu != null)
            {
                Monitor.Log("Menu already open, aborting", LogLevel.Info);
                return;
            }

            var location = Game1.currentLocation;
            if (location == null)
            {
                Monitor.Log("No location, aborting", LogLevel.Info);
                return;
            }

            var cursorTile = Game1.currentCursorTile;
            Monitor.Log($"Cursor tile: {cursorTile}", LogLevel.Info);

            if (location.Objects.TryGetValue(cursorTile, out var obj))
            {
                Monitor.Log($"Found object: {obj.GetType().Name} - {obj.Name}", LogLevel.Info);
                if (obj is Chest chest)
                {
                    Monitor.Log("Opening filter menu for chest", LogLevel.Info);
                    _filterManager.ShowFilterMenu(chest);
                }
            }
            else
            {
                Monitor.Log("No object at cursor", LogLevel.Info);
            }
        }

        private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
        {
            if (!Context.IsWorldReady || Game1.currentLocation == null)
                return;

            // Always draw pipe directions on placed tiles
            _renderer.Draw(e.SpriteBatch, _routes, Game1.currentLocation, _showPipeArrows, _showHighlights);

            // Draw placement preview when holding pipe
            if (_isHoldingPipe)
            {
                var cursorTile = Game1.currentCursorTile;
                _renderer.DrawPlacementPreview(e.SpriteBatch, cursorTile, _placementDirection);
            }
        }

        private void RescanRoutes()
        {
            _ticksSinceLastScan = 0;

            if (Game1.currentLocation == null)
            {
                _routes.Clear();
                return;
            }

            _routes = _scanner.ScanLocation(Game1.currentLocation);
        }

        private void TransferItems()
        {
            // Group routes by source position for alternation
            var routesBySource = new Dictionary<string, List<PipeRoute>>();

            foreach (var route in _routes)
            {
                var key = $"{route.Location.Name}:{route.SourcePosition.X}:{route.SourcePosition.Y}";
                if (!routesBySource.ContainsKey(key))
                {
                    routesBySource[key] = new List<PipeRoute>();
                }
                routesBySource[key].Add(route);
            }

            // Transfer with alternation for each source
            foreach (var kvp in routesBySource)
            {
                _transporter.TryTransferWithAlternation(kvp.Value);
            }
        }
    }
}
