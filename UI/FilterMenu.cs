using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using System.Collections.Generic;
using System.Linq;
using TransportMod.Models;
using TransportMod.Services;

namespace TransportMod.UI
{
    public class FilterMenu : IClickableMenu
    {
        private enum Tab { Groups, SpecificItems }

        private readonly Chest _chest;
        private readonly ChestFilter _filter;
        private readonly FilterManager _filterManager;
        private readonly IMonitor _monitor;

        // Layout constants - vertical bands
        private const int TitleRowHeight = 50;
        private const int TabRowHeight = 48;
        private const int DescriptionRowHeight = 40;
        private const int FooterHeight = 100;
        private const int RowGap = 8;
        private const int SectionGap = 12;

        // Tab system
        private Tab _currentTab = Tab.Groups;
        private ClickableComponent _groupsTab = null!;
        private ClickableComponent _specificItemsTab = null!;

        // Mode toggle (in tab row, right side)
        private ClickableComponent _modeToggle = null!;

        // Groups tab - Item Groups (left column)
        private readonly List<GroupOption> _groups;
        private readonly List<ClickableComponent> _groupCheckboxes;
        private const int GroupBoxSize = 24;
        private const int GroupRowHeight = 28;

        // Groups tab - Seasons (right column)
        private readonly List<SeasonOption> _seasons;
        private readonly List<ClickableComponent> _seasonCheckboxes;
        private const int SeasonBoxSize = 24;
        private const int SeasonRowHeight = 28;

        // Column widths for Groups tab
        private const int GroupsColumnWidth = 380;
        private const int SeasonsColumnWidth = 220;
        private const int SectionHeaderHeight = 28;

        // Specific Items tab
        private readonly List<ClickableComponent> _filterSlots;
        private readonly List<ClickableComponent> _inventorySlots;
        private readonly Item?[] _filterItems;
        private const int FilterSlotCount = 12;
        private const int FilterSlotsPerRow = 6;
        private const int InventorySlotsPerRow = 12;
        private const int InventorySlotCount = 36;
        private const int SlotSize = 64;
        private const int InventorySlotSize = 52;

        // Buttons
        private ClickableTextureComponent _saveButton = null!;
        private ClickableTextureComponent _cancelButton = null!;
        private ClickableTextureComponent _clearButton = null!;

        // Computed layout positions
        private int _titleY;
        private int _tabRowY;
        private int _descriptionY;
        private int _contentY;
        private int _footerY;

        public FilterMenu(Chest chest, ChestFilter filter, FilterManager filterManager, IMonitor monitor)
            : base(
                (Game1.uiViewport.Width - 750) / 2,
                (Game1.uiViewport.Height - 780) / 2,
                750,
                780,
                showUpperRightCloseButton: true)
        {
            _chest = chest;
            _filter = new ChestFilter
            {
                IsBlockMode = filter.IsBlockMode,
                AllowedGroups = new HashSet<string>(filter.AllowedGroups),
                AllowedSeasons = new HashSet<string>(filter.AllowedSeasons),
                AllowedItemIds = new HashSet<string>(filter.AllowedItemIds)
            };
            _filterManager = filterManager;
            _monitor = monitor;

            _groups = GetGroupOptions();
            _groupCheckboxes = new List<ClickableComponent>();
            _seasons = GetSeasonOptions();
            _seasonCheckboxes = new List<ClickableComponent>();
            _filterSlots = new List<ClickableComponent>();
            _inventorySlots = new List<ClickableComponent>();
            _filterItems = new Item?[FilterSlotCount];

            // Load existing filter items
            int idx = 0;
            foreach (var itemId in _filter.AllowedItemIds)
            {
                if (idx >= FilterSlotCount)
                    break;
                var item = ItemRegistry.Create(itemId);
                if (item != null)
                    _filterItems[idx++] = item;
            }

            ComputeLayout();
            InitializeComponents();
        }

        private void ComputeLayout()
        {
            // Start inside the dialog border (thick top border is ~80px)
            int innerTop = yPositionOnScreen + 85;

            // Row 1: Title
            _titleY = innerTop;

            // Row 2: Tabs + Mode (same row)
            _tabRowY = _titleY + TitleRowHeight + RowGap;

            // Row 3: Description
            _descriptionY = _tabRowY + TabRowHeight + RowGap;

            // Row 4: Content area
            _contentY = _descriptionY + DescriptionRowHeight;

            // Footer - enough room from bottom border
            _footerY = yPositionOnScreen + height - FooterHeight - 20;
        }

        private void InitializeComponents()
        {
            int leftMargin = xPositionOnScreen + 40;
            int contentWidth = width - 80;

            // Tabs (left side of tab row) - taller buttons for text to fit
            int tabWidth = 120;
            int tabHeight = 44;
            _groupsTab = new ClickableComponent(
                new Rectangle(leftMargin, _tabRowY, tabWidth, tabHeight),
                "groups_tab");
            _specificItemsTab = new ClickableComponent(
                new Rectangle(leftMargin + tabWidth + 12, _tabRowY, tabWidth, tabHeight),
                "specific_items_tab");

            // Mode toggle (right side of tab row)
            int toggleWidth = 100;
            _modeToggle = new ClickableComponent(
                new Rectangle(xPositionOnScreen + width - 44 - toggleWidth, _tabRowY, toggleWidth, tabHeight),
                "mode_toggle");

            // Groups checkboxes (left column)
            int groupsStartY = _contentY + SectionHeaderHeight;
            for (int i = 0; i < _groups.Count; i++)
            {
                int y = groupsStartY + i * GroupRowHeight;
                _groupCheckboxes.Add(new ClickableComponent(
                    new Rectangle(leftMargin, y, GroupBoxSize, GroupBoxSize),
                    $"group_{i}"));
            }

            // Seasons checkboxes (right column)
            int seasonsStartX = leftMargin + GroupsColumnWidth + 20;
            int seasonsStartY = _contentY + SectionHeaderHeight;
            for (int i = 0; i < _seasons.Count; i++)
            {
                int y = seasonsStartY + i * SeasonRowHeight;
                _seasonCheckboxes.Add(new ClickableComponent(
                    new Rectangle(seasonsStartX, y, SeasonBoxSize, SeasonBoxSize),
                    $"season_{i}"));
            }

            // Filter slots (for Specific Items tab)
            int filterSlotsTop = _contentY + 40;
            int slotsAreaWidth = FilterSlotsPerRow * (SlotSize + 4) - 4;
            int slotsStartX = leftMargin + (contentWidth - slotsAreaWidth) / 2;

            for (int i = 0; i < FilterSlotCount; i++)
            {
                int row = i / FilterSlotsPerRow;
                int col = i % FilterSlotsPerRow;
                _filterSlots.Add(new ClickableComponent(
                    new Rectangle(slotsStartX + col * (SlotSize + 4), filterSlotsTop + row * (SlotSize + 4), SlotSize, SlotSize),
                    $"filter_{i}"));
            }

            // Player inventory slots (full 36 slots - 3 rows x 12 columns, smaller size)
            int inventoryTop = filterSlotsTop + 2 * (SlotSize + 4) + SectionGap + 30;
            int invSlotSpacing = InventorySlotSize + 4;
            int invWidth = InventorySlotsPerRow * invSlotSpacing - 4;
            int invStartX = leftMargin + (contentWidth - invWidth) / 2;

            for (int i = 0; i < InventorySlotCount; i++)
            {
                int row = i / InventorySlotsPerRow;
                int col = i % InventorySlotsPerRow;
                _inventorySlots.Add(new ClickableComponent(
                    new Rectangle(invStartX + col * invSlotSpacing, inventoryTop + row * invSlotSpacing, InventorySlotSize, InventorySlotSize),
                    $"inventory_{i}"));
            }

            // Footer buttons (centered, evenly spaced)
            int buttonSpacing = 110;
            int centerX = xPositionOnScreen + width / 2;
            int buttonY = _footerY + 10;

            _saveButton = new ClickableTextureComponent(
                new Rectangle(centerX - buttonSpacing - 32, buttonY, 64, 64),
                Game1.mouseCursors,
                new Rectangle(128, 256, 64, 64),
                1f)
            { myID = 100 };

            _clearButton = new ClickableTextureComponent(
                "Clear",
                new Rectangle(centerX - 32, buttonY, 64, 64),
                null,
                "Clear all filters",
                Game1.mouseCursors,
                new Rectangle(323, 433, 9, 10),
                4f)
            { myID = 102 };

            _cancelButton = new ClickableTextureComponent(
                new Rectangle(centerX + buttonSpacing - 32, buttonY, 64, 64),
                Game1.mouseCursors,
                new Rectangle(192, 256, 64, 64),
                1f)
            { myID = 101 };
        }

        private List<GroupOption> GetGroupOptions()
        {
            return new List<GroupOption>
            {
                // Seasonal groups (marked with * in UI)
                new("Fruit", IsSeasonal: true),
                new("Vegetables", IsSeasonal: true),
                new("Flowers", IsSeasonal: true),
                new("Seeds", IsSeasonal: true),
                new("Forage", IsSeasonal: true),
                new("Fish", IsSeasonal: true),
                // Non-seasonal groups
                new("Animal Products", IsSeasonal: false),
                new("Artisan Goods", IsSeasonal: false),
                new("Mining", IsSeasonal: false),
                new("Fuel", IsSeasonal: false),
                new("Bait & Tackle", IsSeasonal: false),
                new("Monster Loot", IsSeasonal: false),
                new("Crafting Materials", IsSeasonal: false),
                new("Cooked Food", IsSeasonal: false)
            };
        }

        private List<SeasonOption> GetSeasonOptions()
        {
            return new List<SeasonOption>
            {
                new("Spring"),
                new("Summer"),
                new("Fall"),
                new("Winter")
            };
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            base.receiveLeftClick(x, y, playSound);

            if (upperRightCloseButton?.containsPoint(x, y) == true)
            {
                exitThisMenu();
                return;
            }

            // Tab clicks
            if (_groupsTab.containsPoint(x, y))
            {
                _currentTab = Tab.Groups;
                if (playSound) Game1.playSound("smallSelect");
                return;
            }
            if (_specificItemsTab.containsPoint(x, y))
            {
                _currentTab = Tab.SpecificItems;
                if (playSound) Game1.playSound("smallSelect");
                return;
            }

            // Mode toggle
            if (_modeToggle.containsPoint(x, y))
            {
                _filter.IsBlockMode = !_filter.IsBlockMode;
                if (playSound) Game1.playSound("drumkit6");
                return;
            }

            // Tab-specific clicks
            if (_currentTab == Tab.Groups)
            {
                HandleGroupsTabClick(x, y, playSound);
            }
            else
            {
                HandleSpecificItemsTabClick(x, y, playSound);
            }

            // Buttons
            if (_saveButton.containsPoint(x, y))
            {
                SaveAndClose();
                if (playSound) Game1.playSound("bigSelect");
                return;
            }
            if (_cancelButton.containsPoint(x, y))
            {
                exitThisMenu();
                if (playSound) Game1.playSound("bigDeSelect");
                return;
            }
            if (_clearButton.containsPoint(x, y))
            {
                ClearAllFilters();
                if (playSound) Game1.playSound("trashcan");
                return;
            }
        }

        private void HandleGroupsTabClick(int x, int y, bool playSound)
        {
            // Check group checkboxes
            for (int i = 0; i < _groupCheckboxes.Count; i++)
            {
                var checkbox = _groupCheckboxes[i];
                var expandedBounds = new Rectangle(checkbox.bounds.X, checkbox.bounds.Y, GroupsColumnWidth - 20, checkbox.bounds.Height);

                if (expandedBounds.Contains(x, y))
                {
                    var group = _groups[i];
                    if (_filter.AllowedGroups.Contains(group.Name))
                        _filter.AllowedGroups.Remove(group.Name);
                    else
                        _filter.AllowedGroups.Add(group.Name);

                    if (playSound) Game1.playSound("drumkit6");
                    return;
                }
            }

            // Check season checkboxes
            for (int i = 0; i < _seasonCheckboxes.Count; i++)
            {
                var checkbox = _seasonCheckboxes[i];
                var expandedBounds = new Rectangle(checkbox.bounds.X, checkbox.bounds.Y, SeasonsColumnWidth - 20, checkbox.bounds.Height);

                if (expandedBounds.Contains(x, y))
                {
                    var season = _seasons[i];
                    if (_filter.AllowedSeasons.Contains(season.Name))
                        _filter.AllowedSeasons.Remove(season.Name);
                    else
                        _filter.AllowedSeasons.Add(season.Name);

                    if (playSound) Game1.playSound("drumkit6");
                    return;
                }
            }
        }

        private void HandleSpecificItemsTabClick(int x, int y, bool playSound)
        {
            for (int i = 0; i < _inventorySlots.Count && i < Game1.player.Items.Count; i++)
            {
                if (_inventorySlots[i].containsPoint(x, y))
                {
                    var item = Game1.player.Items[i];
                    if (item != null)
                    {
                        AddItemToFilter(item);
                        if (playSound) Game1.playSound("stoneStep");
                    }
                    return;
                }
            }
        }

        public override void receiveRightClick(int x, int y, bool playSound = true)
        {
            if (_currentTab == Tab.SpecificItems)
            {
                for (int i = 0; i < _filterSlots.Count; i++)
                {
                    if (_filterSlots[i].containsPoint(x, y) && _filterItems[i] != null)
                    {
                        _filter.AllowedItemIds.Remove(_filterItems[i]!.QualifiedItemId);
                        _filterItems[i] = null;
                        if (playSound) Game1.playSound("throwDownITem");
                        return;
                    }
                }
            }
        }

        private void AddItemToFilter(Item item)
        {
            if (_filter.AllowedItemIds.Contains(item.QualifiedItemId))
                return;

            for (int i = 0; i < _filterItems.Length; i++)
            {
                if (_filterItems[i] == null)
                {
                    _filterItems[i] = item.getOne();
                    _filter.AllowedItemIds.Add(item.QualifiedItemId);
                    return;
                }
            }
        }

        private void ClearAllFilters()
        {
            _filter.AllowedGroups.Clear();
            _filter.AllowedSeasons.Clear();
            _filter.AllowedItemIds.Clear();
            for (int i = 0; i < _filterItems.Length; i++)
                _filterItems[i] = null;
        }

        public override void receiveKeyPress(Keys key)
        {
            base.receiveKeyPress(key);
            if (key == Keys.Escape)
            {
                exitThisMenu();
            }
        }

        public override void performHoverAction(int x, int y)
        {
            base.performHoverAction(x, y);
            _saveButton.tryHover(x, y);
            _clearButton.tryHover(x, y);
            _cancelButton.tryHover(x, y);
        }

        private void SaveAndClose()
        {
            _filter.AllowedItemIds.Clear();
            foreach (var item in _filterItems)
            {
                if (item != null)
                    _filter.AllowedItemIds.Add(item.QualifiedItemId);
            }

            _filterManager.SetFilter(_chest, _filter);
            exitThisMenu();
        }

        public override void draw(SpriteBatch b)
        {
            // Darken background
            b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.75f);

            // Draw menu box
            Game1.drawDialogueBox(xPositionOnScreen, yPositionOnScreen, width, height, false, true);

            // Row 1: Title (centered)
            string title = "Chest Filter";
            Vector2 titleSize = Game1.dialogueFont.MeasureString(title);
            Utility.drawTextWithShadow(b, title, Game1.dialogueFont,
                new Vector2(xPositionOnScreen + (width - titleSize.X) / 2, _titleY + 12),
                Game1.textColor);

            // Row 2: Tabs + Mode
            DrawTabRow(b);

            // Row 3: Description
            DrawDescription(b);

            // Row 4: Content
            if (_currentTab == Tab.Groups)
                DrawGroupsContent(b);
            else
                DrawSpecificItemsContent(b);

            // Footer
            DrawFooter(b);

            // Tooltips
            DrawTooltips(b);

            // Close button and cursor
            base.draw(b);
            drawMouse(b);
        }

        private void DrawTabRow(SpriteBatch b)
        {
            // Groups tab
            Color groupsColor = _currentTab == Tab.Groups ? Color.White : Color.Gray * 0.8f;
            IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
                _groupsTab.bounds.X, _groupsTab.bounds.Y,
                _groupsTab.bounds.Width, _groupsTab.bounds.Height,
                groupsColor, 1f, false);
            string groupsText = "Groups";
            Vector2 groupsSize = Game1.smallFont.MeasureString(groupsText);
            Utility.drawTextWithShadow(b, groupsText, Game1.smallFont,
                new Vector2(_groupsTab.bounds.X + (_groupsTab.bounds.Width - groupsSize.X) / 2,
                           _groupsTab.bounds.Y + (_groupsTab.bounds.Height - groupsSize.Y) / 2),
                Game1.textColor);

            // Items tab
            Color itemColor = _currentTab == Tab.SpecificItems ? Color.White : Color.Gray * 0.8f;
            IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
                _specificItemsTab.bounds.X, _specificItemsTab.bounds.Y,
                _specificItemsTab.bounds.Width, _specificItemsTab.bounds.Height,
                itemColor, 1f, false);
            string itemsText = "Items";
            Vector2 itemsSize = Game1.smallFont.MeasureString(itemsText);
            Utility.drawTextWithShadow(b, itemsText, Game1.smallFont,
                new Vector2(_specificItemsTab.bounds.X + (_specificItemsTab.bounds.Width - itemsSize.X) / 2,
                           _specificItemsTab.bounds.Y + (_specificItemsTab.bounds.Height - itemsSize.Y) / 2),
                Game1.textColor);

            // Mode toggle (right side)
            DrawModeToggle(b);
        }

        private void DrawModeToggle(SpriteBatch b)
        {
            // Color-coded toggle: green for Allow, red for Block
            Color toggleColor = _filter.IsBlockMode ? Color.LightCoral : Color.LightGreen;
            IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
                _modeToggle.bounds.X, _modeToggle.bounds.Y,
                _modeToggle.bounds.Width, _modeToggle.bounds.Height,
                toggleColor, 1f, false);

            string modeText = _filter.IsBlockMode ? "Block" : "Allow";
            Vector2 modeSize = Game1.smallFont.MeasureString(modeText);
            Utility.drawTextWithShadow(b, modeText, Game1.smallFont,
                new Vector2(_modeToggle.bounds.X + (_modeToggle.bounds.Width - modeSize.X) / 2,
                           _modeToggle.bounds.Y + (_modeToggle.bounds.Height - modeSize.Y) / 2),
                Game1.textColor);
        }

        private void DrawDescription(SpriteBatch b)
        {
            string desc = _currentTab == Tab.Groups
                ? "Select groups. Add seasons to filter seasonal items."
                : "Click inventory items to add. Right-click slots to remove.";

            Utility.drawTextWithShadow(b, desc, Game1.smallFont,
                new Vector2(xPositionOnScreen + 40, _descriptionY),
                Color.DimGray);
        }

        private void DrawGroupsContent(SpriteBatch b)
        {
            int leftMargin = xPositionOnScreen + 40;

            // Section header: ITEM GROUPS
            Utility.drawTextWithShadow(b, "ITEM GROUPS", Game1.smallFont,
                new Vector2(leftMargin, _contentY),
                Game1.textColor);

            // Section header: SEASONS
            int seasonsStartX = leftMargin + GroupsColumnWidth + 20;
            Utility.drawTextWithShadow(b, "SEASONS", Game1.smallFont,
                new Vector2(seasonsStartX, _contentY),
                Game1.textColor);

            // Draw group checkboxes
            for (int i = 0; i < _groupCheckboxes.Count; i++)
            {
                var checkbox = _groupCheckboxes[i];
                var group = _groups[i];
                bool isChecked = _filter.AllowedGroups.Contains(group.Name);

                // Checkbox background
                b.Draw(Game1.mouseCursors,
                    new Rectangle(checkbox.bounds.X, checkbox.bounds.Y, GroupBoxSize, GroupBoxSize),
                    new Rectangle(227, 425, 9, 9),
                    Color.White);

                // Checkmark
                if (isChecked)
                {
                    b.Draw(Game1.mouseCursors,
                        new Vector2(checkbox.bounds.X, checkbox.bounds.Y),
                        new Rectangle(236, 425, 9, 9),
                        Color.White, 0f, Vector2.Zero, GroupBoxSize / 9f, SpriteEffects.None, 1f);
                }

                // Label (with * for seasonal groups)
                string label = group.IsSeasonal ? $"{group.Name} *" : group.Name;
                Utility.drawTextWithShadow(b, label, Game1.smallFont,
                    new Vector2(checkbox.bounds.Right + 8, checkbox.bounds.Y),
                    Game1.textColor);
            }

            // Draw season checkboxes
            for (int i = 0; i < _seasonCheckboxes.Count; i++)
            {
                var checkbox = _seasonCheckboxes[i];
                var season = _seasons[i];
                bool isChecked = _filter.AllowedSeasons.Contains(season.Name);

                // Checkbox background
                b.Draw(Game1.mouseCursors,
                    new Rectangle(checkbox.bounds.X, checkbox.bounds.Y, SeasonBoxSize, SeasonBoxSize),
                    new Rectangle(227, 425, 9, 9),
                    Color.White);

                // Checkmark
                if (isChecked)
                {
                    b.Draw(Game1.mouseCursors,
                        new Vector2(checkbox.bounds.X, checkbox.bounds.Y),
                        new Rectangle(236, 425, 9, 9),
                        Color.White, 0f, Vector2.Zero, SeasonBoxSize / 9f, SpriteEffects.None, 1f);
                }

                // Label
                Utility.drawTextWithShadow(b, season.Name, Game1.smallFont,
                    new Vector2(checkbox.bounds.Right + 8, checkbox.bounds.Y),
                    Game1.textColor);
            }

            // Draw legend for seasonal marker
            int legendY = _seasonCheckboxes[_seasonCheckboxes.Count - 1].bounds.Bottom + 20;
            Utility.drawTextWithShadow(b, "* Seasonal", Game1.smallFont,
                new Vector2(seasonsStartX, legendY),
                Color.DimGray);
        }

        private void DrawSpecificItemsContent(SpriteBatch b)
        {
            // Section label
            string slotsLabel = _filter.IsBlockMode ? "Blocked Items" : "Allowed Items";
            Utility.drawTextWithShadow(b, slotsLabel, Game1.smallFont,
                new Vector2(_filterSlots[0].bounds.X, _contentY + 8),
                Game1.textColor);

            // Filter slots
            foreach (var slot in _filterSlots)
            {
                b.Draw(Game1.menuTexture, slot.bounds, new Rectangle(128, 128, 64, 64), Color.White);
            }

            // Items in filter slots
            for (int i = 0; i < _filterItems.Length; i++)
            {
                if (_filterItems[i] != null)
                {
                    _filterItems[i]!.drawInMenu(b, new Vector2(_filterSlots[i].bounds.X, _filterSlots[i].bounds.Y - 2), 1f);
                }
            }

            // Inventory label
            int invLabelY = _filterSlots[FilterSlotCount - 1].bounds.Bottom + SectionGap;
            Utility.drawTextWithShadow(b, "Your Inventory", Game1.smallFont,
                new Vector2(_inventorySlots[0].bounds.X, invLabelY),
                Game1.textColor);

            // Inventory slots (full 36 slots at smaller scale)
            float invScale = InventorySlotSize / 64f;
            for (int i = 0; i < _inventorySlots.Count && i < InventorySlotCount; i++)
            {
                var slot = _inventorySlots[i];
                b.Draw(Game1.menuTexture, slot.bounds, new Rectangle(128, 128, 64, 64), Color.White);

                if (i < Game1.player.Items.Count && Game1.player.Items[i] != null)
                {
                    Game1.player.Items[i].drawInMenu(b,
                        new Vector2(slot.bounds.X - 5, slot.bounds.Y - 5),
                        invScale, 1f, 0.9f, StackDrawType.Hide, Color.White, false);
                }
            }
        }

        private void DrawFooter(SpriteBatch b)
        {
            // Labels ABOVE buttons (inside dialog)
            Vector2 saveSize = Game1.smallFont.MeasureString("Save");
            Utility.drawTextWithShadow(b, "Save", Game1.smallFont,
                new Vector2(_saveButton.bounds.X + (_saveButton.bounds.Width - saveSize.X) / 2, _saveButton.bounds.Y - 28),
                Game1.textColor);

            Vector2 clearSize = Game1.smallFont.MeasureString("Clear");
            Utility.drawTextWithShadow(b, "Clear", Game1.smallFont,
                new Vector2(_clearButton.bounds.X + (_clearButton.bounds.Width - clearSize.X) / 2, _clearButton.bounds.Y - 28),
                Game1.textColor);

            Vector2 cancelSize = Game1.smallFont.MeasureString("Cancel");
            Utility.drawTextWithShadow(b, "Cancel", Game1.smallFont,
                new Vector2(_cancelButton.bounds.X + (_cancelButton.bounds.Width - cancelSize.X) / 2, _cancelButton.bounds.Y - 28),
                Game1.textColor);

            // Buttons below labels
            _saveButton.draw(b);
            _clearButton.draw(b);
            _cancelButton.draw(b);
        }

        private void DrawTooltips(SpriteBatch b)
        {
            int mouseX = Game1.getMouseX();
            int mouseY = Game1.getMouseY();

            if (_currentTab == Tab.SpecificItems)
            {
                for (int i = 0; i < _filterSlots.Count; i++)
                {
                    if (_filterSlots[i].containsPoint(mouseX, mouseY) && _filterItems[i] != null)
                    {
                        drawHoverText(b, _filterItems[i]!.DisplayName, Game1.smallFont);
                        return;
                    }
                }

                for (int i = 0; i < _inventorySlots.Count && i < Game1.player.Items.Count; i++)
                {
                    if (_inventorySlots[i].containsPoint(mouseX, mouseY) && Game1.player.Items[i] != null)
                    {
                        drawHoverText(b, Game1.player.Items[i].DisplayName, Game1.smallFont);
                        return;
                    }
                }
            }
        }

        private record GroupOption(string Name, bool IsSeasonal);
        private record SeasonOption(string Name);
    }
}
