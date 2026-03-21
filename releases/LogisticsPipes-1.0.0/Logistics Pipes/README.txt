LOGISTICS PIPES
Version: 1.0.0

A Stardew Valley mod that adds directional pipes for moving items between chests, machines, crab pots, and trash cans.

# REQUIREMENTS
- Stardew Valley 1.6
- SMAPI

# INSTALLATION
- Install SMAPI
- Extract the Logistics Pipes folder into your Stardew Valley Mods folder
- Launch the game

# BASIC USE
- Place pipes to create a route between valid endpoints
- Pipes move items in the direction they face
- Use R to rotate a pipe before or after placement
- Point at a chest and press o (NOT zero) to open its filter menu
- Use P to toggle pipe direction arrows
- Use L to toggle recognized route highlights
- Press F5 to manually rescan routes after making changes

# SUPPORTED ROUTES
- Chest -> Chest
- Chest -> Machine
- Machine -> Chest
- Trash Can -> Chest
- Chest -> Crab Pot
- Crab Pot -> Chest

# PIPE TIERS
- Wooden Pipe = 1 item per cycle
- Copper Pipe = 2 items per cycle
- Iron Pipe = 4 items per cycle
- Gold Pipe = 8 items per cycle
- Iridium Pipe = 16 items per cycle

The throughput of a route is limited by the lowest-tier pipe in that route.

# FILTERS
Destination chests can use filters to control what they accept.

Filter options include:
- Item groups
- Season filters
- Specific item filters
- Allow mode
- Block mode

Available item groups:
- Fruit
- Vegetables
- Flowers
- Seeds
- Forage
- Fish
- Animal Products
- Artisan Goods
- Mining
- Fuel
- Bait & Tackle
- Monster Loot
- Crafting Materials
- Cooked Food

Available seasons:
- Spring
- Summer
- Fall
- Winter

Season filters can be used by themselves or combined with item groups.
When a season filter is active, non-seasonal items are blocked.

# PIPE REPLACEMENT
You do not need to break and replace pipes repeatedly.
If you place a pipe onto an existing pipe:
- the new pipe type is applied
- the new orientation is applied
- the old pipe is returned to your inventory

# WALL PLACEMENT
Pipes can also be placed on walls.

If a wall pipe is placed more than 1 tile away, you will not be able to reach it to break it later.
You can still change its direction by placing another pipe onto it like normal.

# NOTES
- Pipes only work within the current location
- Machine-to-machine transfers are not supported
- NPCs can walk over pipes
- Routes are rescanned automatically
- You can manually force a rescan with F5

# CONFIGURATION
The config.json file includes:
- TransferIntervalSeconds
- RouteScanIntervalSeconds
- OpenFilterMenuKey
- TogglePipeArrowsKey
- ToggleHighlightsKey

# EXAMPLE USES
- Sorting mining loot into multiple chests
- Feeding furnaces automatically
- Collecting finished products from machines
- Mushroom cave processing setups
- Trash can collection
- Crab pot baiting and catch collection

# KNOWN LIMITATIONS
- Pipes do not move items across different locations
- Machine-to-machine transfers are not supported
- Wall pipes placed too far away cannot be broken directly later

# CREDITS
- Created by Bridger
