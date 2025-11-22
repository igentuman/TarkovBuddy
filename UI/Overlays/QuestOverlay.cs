using Microsoft.Extensions.Logging;
using TarkovBuddy.Core;
using TarkovBuddy.Models;

namespace TarkovBuddy.UI.Overlays
{
    /// <summary>
    /// Overlay component for displaying active quest objectives and progress.
    /// Shows quest name, objectives, and map markers for objective locations.
    /// </summary>
    public class QuestOverlay : IOverlayElement
    {
        private readonly ILogger<QuestOverlay> _logger;
        private bool _isVisible;
        private float _opacity;
        private int _positionX;
        private int _positionY;
        private int _width;
        private int _height;
        private List<string> _activeQuests;
        private List<string> _objectives;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the QuestOverlay class.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        public QuestOverlay(ILogger<QuestOverlay> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _isVisible = true;
            _opacity = 0.9f;
            _positionX = 50;
            _positionY = 400;
            _width = 400;
            _height = 250;
            _activeQuests = new List<string>();
            _objectives = new List<string>();
        }

        public string ElementName => "QuestOverlay";
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
                throw new ObjectDisposedException(nameof(QuestOverlay));

            try
            {
                // DirectX rendering would happen here
                // For now, just track that render was called
                await Task.Delay(0); // Simulate async work
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rendering quest overlay");
            }
        }

        public async Task UpdateAsync(GameState gameState, Frame? frameData)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(QuestOverlay));

            try
            {
                // Update overlay based on game state
                if (gameState == GameState.InRaid)
                {
                    _isVisible = true;
                }
                else
                {
                    _isVisible = false;
                    _activeQuests.Clear();
                    _objectives.Clear();
                }

                await Task.Delay(0); // Simulate async work
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating quest overlay");
            }
        }

        public async Task OnHotkeyAsync(string hotkeyAction)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(QuestOverlay));

            try
            {
                if (hotkeyAction == "ToggleQuestOverlay")
                {
                    _isVisible = !_isVisible;
                    _logger.LogInformation("Quest overlay visibility toggled: {IsVisible}", _isVisible);
                }

                await Task.Delay(0); // Simulate async work
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling hotkey in quest overlay");
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            try
            {
                _activeQuests.Clear();
                _objectives.Clear();
                _logger.LogInformation("QuestOverlay disposed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing QuestOverlay");
            }

            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}