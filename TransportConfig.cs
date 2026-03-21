using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace TransportMod
{
    public class TransportConfig
    {
        public int TransferIntervalSeconds { get; set; } = 3;
        public int RouteScanIntervalSeconds { get; set; } = 10;
        public KeybindList OpenFilterMenuKey { get; set; } = KeybindList.Parse("O");
        public KeybindList TogglePipeArrowsKey { get; set; } = KeybindList.Parse("P");
        public KeybindList ToggleHighlightsKey { get; set; } = KeybindList.Parse("L");
    }
}
