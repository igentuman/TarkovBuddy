using Microsoft.Extensions.Logging;
using TarkovBuddy.Core;
using TarkovBuddy.Models;

namespace TarkovBuddy.UI.Overlays
{
    /// <summary>
    /// Overlay component for displaying map information and extraction points.
    /// Shows identified map name, extract points, and player spawn location.
    /// </summary>
    public class MapOverlay : IOverlayElement
    {
        private readonly ILogger<MapOverlay> _logger;
        private bool _isVisible;
        private float _opacity;
        private int _positionX;
        private int _positionY;
        private int _width;
        private int _height;
        private string _currentMap;
        private List<string> _extractionPoints;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the MapOverlay class.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        public MapOverlay(ILogger<MapOverlay> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _isVisible = true;
            _opacity = 0.9f;
            _positionX = 50;
            _positionY = 50;
            _width = 400;
            _height = 300;
            _currentMap = "Unknown";
            _extractionPoints = new List<string>();
        }

        public string ElementName => "MapOverlay";
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
                throw new ObjectDisposedException(nameof(MapOverlay));

            try
            {
                // DirectX rendering would happen here
                // For now, just track that render was called
                await Task.Delay(0); // Simulate async work
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rendering map overlay");
            }
        }

        public async Task UpdateAsync(GameState gameState, Frame? frameData)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(MapOverlay));

            try
            {
                // Update overlay based on game state and frame data
                if (gameState == GameState.InRaid || gameState == GameState.InventoryScreen)
                {
                    _isVisible = true;
                }
                else
                {
                    _isVisible = false;
                    _extractionPoints.Clear();
                    _currentMap = "Unknown";
                }

                await Task.Delay(0); // Simulate async work
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating map overlay");
            }
        }

        public async Task OnHotkeyAsync(string hotkeyAction)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(MapOverlay));

            try
            {
                if (hotkeyAction == "ToggleMapOverlay")
                {
                    _isVisible = !_isVisible;
                    _logger.LogInformation("Map overlay visibility toggled: {IsVisible}", _isVisible);
                }

                await Task.Delay(0); // Simulate async work
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling hotkey in map overlay");
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            try
            {
                _extractionPoints.Clear();
                _logger.LogInformation("MapOverlay disposed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing MapOverlay");
            }

            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}