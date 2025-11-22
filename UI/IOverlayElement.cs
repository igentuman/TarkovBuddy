using TarkovBuddy.Core;
using TarkovBuddy.Models;

namespace TarkovBuddy.UI
{
    /// <summary>
    /// Interface for all overlay UI elements displayed on the DirectX overlay window.
    /// </summary>
    public interface IOverlayElement : IDisposable
    {
        /// <summary>
        /// Gets the name of this overlay element.
        /// </summary>
        string ElementName { get; }

        /// <summary>
        /// Gets or sets whether this element is currently visible.
        /// </summary>
        bool IsVisible { get; set; }

        /// <summary>
        /// Gets or sets the opacity (0.0 to 1.0) of this overlay element.
        /// </summary>
        float Opacity { get; set; }

        /// <summary>
        /// Gets or sets the X position of the element on screen.
        /// </summary>
        int PositionX { get; set; }

        /// <summary>
        /// Gets or sets the Y position of the element on screen.
        /// </summary>
        int PositionY { get; set; }

        /// <summary>
        /// Gets or sets the width of the element.
        /// </summary>
        int Width { get; set; }

        /// <summary>
        /// Gets or sets the height of the element.
        /// </summary>
        int Height { get; set; }

        /// <summary>
        /// Renders this overlay element.
        /// </summary>
        /// <param name="deltaTime">Elapsed time since last frame in milliseconds.</param>
        Task RenderAsync(float deltaTime);

        /// <summary>
        /// Updates the element state based on game data.
        /// </summary>
        /// <param name="gameState">Current game state.</param>
        /// <param name="frameData">Current frame data from screen capture.</param>
        Task UpdateAsync(GameState gameState, Frame? frameData);

        /// <summary>
        /// Handles a hotkey action triggered for this element.
        /// </summary>
        /// <param name="hotkeyAction">The name of the hotkey action.</param>
        Task OnHotkeyAsync(string hotkeyAction);
    }
}