using Microsoft.Extensions.Logging;
using System.Windows;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using TarkovBuddy.Core;
using TarkovBuddy.Models;

namespace TarkovBuddy.UI
{
    /// <summary>
    /// Transparent, click-through overlay window for rendering in-game UI elements.
    /// Uses DirectX 11 for rendering and supports multiple overlay components.
    /// </summary>
    public partial class OverlayWindow : Window
    {
        private readonly ILogger<OverlayWindow> _logger;
        private readonly DirectXRenderer _renderer;
        private readonly List<IOverlayElement> _overlayElements;
        private nint _hwnd;
        private bool _isRunning;
        private GameState _currentGameState;
        private Frame? _currentFrame;
        private DateTime _lastFrameTime;
        private const float TargetFps = 60f;
        private const float FrameTimeMs = 1000f / TargetFps;

        // Win32 constants for transparent click-through window
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_LAYERED = 0x00080000;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int LWA_ALPHA = 0x00000002;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(nint hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(nint hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern bool SetLayeredWindowAttributes(nint hwnd, uint crKey, byte bAlpha, uint dwFlags);

        /// <summary>
        /// Initializes a new instance of the OverlayWindow class.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        public OverlayWindow(ILogger<OverlayWindow> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            // Create a logger factory for DirectXRenderer
            var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder => { });
            var dxLogger = loggerFactory.CreateLogger<DirectXRenderer>();
            _renderer = new DirectXRenderer(dxLogger);
            _overlayElements = new List<IOverlayElement>();
            _currentGameState = GameState.InLauncher;
            _lastFrameTime = DateTime.Now;

            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not initialize XAML component - overlay window may not be fully functional");
            }
        }

        /// <summary>
        /// Registers an overlay element to be rendered.
        /// </summary>
        /// <param name="element">The overlay element to register.</param>
        public void RegisterOverlayElement(IOverlayElement element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            _overlayElements.Add(element);
            _logger.LogInformation("Registered overlay element: {ElementName}", element.ElementName);
        }

        /// <summary>
        /// Unregisters an overlay element.
        /// </summary>
        /// <param name="element">The overlay element to unregister.</param>
        public void UnregisterOverlayElement(IOverlayElement element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            if (_overlayElements.Remove(element))
            {
                _logger.LogInformation("Unregistered overlay element: {ElementName}", element.ElementName);
                element.Dispose();
            }
        }

        /// <summary>
        /// Updates overlay with current game state and frame data.
        /// </summary>
        /// <param name="gameState">Current game state.</param>
        /// <param name="frame">Current frame from screen capture.</param>
        public async Task UpdateAsync(GameState gameState, Frame? frame)
        {
            _currentGameState = gameState;
            _currentFrame = frame;

            // Update all registered overlay elements
            foreach (var element in _overlayElements)
            {
                try
                {
                    await element.UpdateAsync(gameState, frame);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating overlay element: {ElementName}", element.ElementName);
                }
            }
        }

        /// <summary>
        /// Starts the overlay rendering loop.
        /// </summary>
        public void StartRendering()
        {
            if (_isRunning)
                return;

            try
            {
                _logger.LogInformation("Starting overlay rendering");
                _isRunning = true;

                // Initialize DirectX renderer
                var helper = new WindowInteropHelper(this);
                if (helper.Handle != nint.Zero)
                {
                    var screenWidth = (int)SystemParameters.PrimaryScreenWidth;
                    var screenHeight = (int)SystemParameters.PrimaryScreenHeight;
                    _renderer.Initialize(helper.Handle, screenWidth, screenHeight);
                }

                // Start rendering loop
                RenderingLoop();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting overlay rendering");
                _isRunning = false;
            }
        }

        /// <summary>
        /// Stops the overlay rendering loop.
        /// </summary>
        public void StopRendering()
        {
            _isRunning = false;
            _logger.LogInformation("Stopped overlay rendering");
        }

        private void RenderingLoop()
        {
            while (_isRunning)
            {
                var now = DateTime.Now;
                var deltaMs = (float)(now - _lastFrameTime).TotalMilliseconds;

                if (deltaMs < FrameTimeMs)
                {
                    System.Threading.Thread.Sleep((int)(FrameTimeMs - deltaMs));
                    continue;
                }

                _lastFrameTime = now;

                try
                {
                    // Clear render target
                    _renderer.Clear();

                    // Render all overlay elements
                    foreach (var element in _overlayElements)
                    {
                        if (element.IsVisible)
                        {
                            try
                            {
                                Task.Run(() => element.RenderAsync(deltaMs)).Wait();
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error rendering overlay element: {ElementName}", element.ElementName);
                            }
                        }
                    }

                    // Present frame
                    _renderer.Present(true);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in rendering loop");
                }
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            try
            {
                var helper = new WindowInteropHelper(this);
                _hwnd = helper.Handle;

                // Make window transparent and click-through
                MakeWindowTransparentAndClickThrough();

                _logger.LogInformation("Overlay window initialized");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing overlay window");
            }
        }

        private void MakeWindowTransparentAndClickThrough()
        {
            try
            {
                // Get current window style
                int exStyle = GetWindowLong(_hwnd, GWL_EXSTYLE);

                // Add transparent and click-through styles
                exStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT;

                // Set new window style
                SetWindowLong(_hwnd, GWL_EXSTYLE, exStyle);

                // Set window transparency
                SetLayeredWindowAttributes(_hwnd, 0, 255, LWA_ALPHA);

                _logger.LogInformation("Window made transparent and click-through");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error making window transparent");
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            StopRendering();

            // Dispose all overlay elements
            foreach (var element in _overlayElements)
            {
                element?.Dispose();
            }
            _overlayElements.Clear();

            // Dispose renderer
            _renderer?.Dispose();

            base.OnClosed(e);
        }
    }
}