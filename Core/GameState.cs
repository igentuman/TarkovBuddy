namespace TarkovBuddy.Core
{
    /// <summary>
    /// Represents the current state of the Escape from Tarkov game client.
    /// The state machine transitions between these states based on screen analysis.
    /// </summary>
    public enum GameState
    {
        /// <summary>
        /// Game client is closed, minimized, or launcher is active.
        /// </summary>
        InLauncher = 0,

        /// <summary>
        /// Main menu screen is displayed.
        /// </summary>
        InLobby = 1,

        /// <summary>
        /// Character hideout or inventory screen is displayed (in lobby).
        /// </summary>
        InHideout = 2,

        /// <summary>
        /// Map loading screen is displayed (entering a raid).
        /// </summary>
        LoadingRaid = 3,

        /// <summary>
        /// Active gameplay within a raid.
        /// </summary>
        InRaid = 4,

        /// <summary>
        /// Raid completed successfully, extraction occurred.
        /// </summary>
        Extracted = 5,

        /// <summary>
        /// Character died during raid.
        /// </summary>
        Died = 6,

        /// <summary>
        /// Inventory or stash screen is open (during raid or lobby).
        /// </summary>
        InventoryScreen = 7,

        /// <summary>
        /// Flea market trading interface is open.
        /// </summary>
        FleaMarketScreen = 8
    }
}