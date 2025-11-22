using Microsoft.Extensions.Logging;
using TarkovBuddy.Core;
using TarkovBuddy.Models;

namespace TarkovBuddy.UI.Overlays
{
    /// <summary>
    /// Overlay component for displaying stash statistics and inventory analysis.
    /// Shows total value, item count, weight, and organization suggestions.
    /// </summary>
    public class StashStatsOverlay : IOverlayElement
    {
        private readonly ILogger<StashStatsOverlay> _logger;
        private bool _isVisible;
        private float _opacity;
        private int _positionX;
        private int _positionY;
        private int _width;
        private int _height;
        private float _totalValue;
        private int _itemCount;
        private float _weightUsed;
        private Dictionary<string, int> _itemCategories;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the StashStatsOverlay class.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        public StashStatsOverlay(ILogger<StashStatsOverlay> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _isVisible = true;
            _opacity = 0.85f;
            _positionX = 500;
            _positionY = 500;
            _width = 350;
            _height = 200;
            _totalValue = 0f;
            _itemCount = 0;
            _weightUsed = 0f;
            _itemCategories = new Dictionary<string, int>();
        }

        public string ElementName => "StashStatsOverlay";
        public bool IsVisible
        {
            get => _isVisible;
            set => _isVisible = value;
        }

        public float Opacity
        {
            get => _opacity;
            set => _opacity = Math.Clamp(value, 0f, 1f);
        }

        public int PositionX
        {
            get => _positionX;
            set => _positionX = value;
        }

        public int PositionY
        {
            get => _positionY;
            set => _positionY = value;
        }

        public int Width
        {
            get => _width;
            set => _width = value;
        }

        public int Height
        {
            get => _height;
            set => _height = value;
        }

        public async Task RenderAsync(float deltaTime)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(StashStatsOverlay));

            try
            {
                // DirectX rendering would happen here
                // For now, just track that render was called
                await Task.Delay(0); // Simulate async work
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rendering stash stats overlay");
            }
        }

        public async Task UpdateAsync(GameState gameState, Frame? frameData)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(StashStatsOverlay));

            try
            {
                // Update overlay based on game state
                if (gameState == GameState.InventoryScreen || gameState == GameState.InHideout)
                {
                    _isVisible = true;
                }
                else
                {
                    _isVisible = false;
                    _itemCount = 0;
                    _totalValue = 0f;
                    _weightUsed = 0f;
                    _itemCategories.Clear();
                }

                await Task.Delay(0); // Simulate async work
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating stash stats overlay");
            }
        }

        public async Task OnHotkeyAsync(string hotkeyAction)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(StashStatsOverlay));

            try
            {
                if (hotkeyAction == "ToggleStashOverlay")
                {
                    _isVisible = !_isVisible;
                    _logger.LogInformation("Stash stats overlay visibility toggled: {IsVisible}", _isVisible);
                }

                await Task.Delay(0); // Simulate async work
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling hotkey in stash stats overlay");
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            try
            {
                _itemCategories.Clear();
                _logger.LogInformation("StashStatsOverlay disposed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing StashStatsOverlay");
            }

            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}