using TarkovBuddy.Core;
using TarkovBuddy.Models;

namespace TarkovBuddy.Modules
{
    /// <summary>
    /// Base interface for all feature modules in the application.
    /// Modules are pluggable components that process game state and frames to provide features.
    /// </summary>
    public interface IModule : IDisposable
    {
        /// <summary>
        /// Gets the user-friendly name of this module.
        /// </summary>
        string ModuleName { get; }

        /// <summary>
        /// Gets or sets whether this module is currently enabled.
        /// When disabled, modules should not process frames or consume resources.
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// Called when the application transitions into the specified game state.
        /// Used for state-specific initialization and resource setup.
        /// </summary>
        void OnStateEnter(GameState state);

        /// <summary>
        /// Called when the application transitions out of the specified game state.
        /// Used for cleanup and resource release.
        /// </summary>
        void OnStateExit(GameState state);

        /// <summary>
        /// Called for each new frame captured from the game screen.
        /// This is where the module performs its main processing logic.
        /// </summary>
        Task OnFrameAsync(Frame frame);

        /// <summary>
        /// Called when a hotkey registered by this module is pressed.
        /// The hotkeyAction parameter identifies which hotkey was pressed.
        /// </summary>
        Task OnHotkeyAsync(string hotkeyAction);

        /// <summary>
        /// Gets the current performance metrics for this module.
        /// Used for monitoring and optimization.
        /// </summary>
        ModuleMetrics GetMetrics();
    }

    /// <summary>
    /// Represents performance metrics for a module.
    /// </summary>
    public class ModuleMetrics
    {
        /// <summary>
        /// Total number of frames processed by this module.
        /// </summary>
        public long FramesProcessed { get; set; }

        /// <summary>
        /// Average processing time per frame in milliseconds.
        /// </summary>
        public double AverageProcessingTimeMs { get; set; }

        /// <summary>
        /// Peak processing time in milliseconds.
        /// </summary>
        public double PeakProcessingTimeMs { get; set; }

        /// <summary>
        /// Number of errors encountered during processing.
        /// </summary>
        public long ErrorCount { get; set; }

        /// <summary>
        /// Timestamp of last successful frame processing.
        /// </summary>
        public DateTime LastProcessedTime { get; set; }
    }
}