using Microsoft.Extensions.Logging;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using System.Windows.Interop;

namespace TarkovBuddy.UI
{
    /// <summary>
    /// DirectX 11 rendering backend for transparent overlay window.
    /// Manages GPU resources and rendering pipeline for overlay elements.
    /// </summary>
    public class DirectXRenderer : IDisposable
    {
        private readonly ILogger<DirectXRenderer> _logger;
        private SharpDX.Direct3D11.Device? _device;
        private DeviceContext? _deviceContext;
        private SwapChain? _swapChain;
        private RenderTargetView? _renderTargetView;
        private Texture2D? _backBuffer;
        private int _screenWidth;
        private int _screenHeight;
        private bool _isInitialized;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the DirectXRenderer class.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        public DirectXRenderer(ILogger<DirectXRenderer> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _isInitialized = false;
        }

        /// <summary>
        /// Initializes DirectX 11 resources for rendering.
        /// Must be called before any rendering operations.
        /// </summary>
        /// <param name="windowHandle">Handle to the target window.</param>
        /// <param name="width">Window width in pixels.</param>
        /// <param name="height">Window height in pixels.</param>
        public void Initialize(IntPtr windowHandle, int width, int height)
        {
            try
            {
                _logger.LogInformation("Initializing DirectX 11 renderer: {Width}x{Height}", width, height);
                
                _screenWidth = width;
                _screenHeight = height;

                // Create device and device context
                SharpDX.Direct3D11.Device.CreateWithSwapChain(
                    DriverType.Hardware,
                    DeviceCreationFlags.Debug,
                    new[] { FeatureLevel.Level_11_1, FeatureLevel.Level_11_0 },
                    GetSwapChainDescription(windowHandle, width, height),
                    out var device,
                    out var swapChain);

                _device = device;
                _swapChain = swapChain;
                _deviceContext = device.ImmediateContext;

                // Get backbuffer and create render target
                _backBuffer = SharpDX.Direct3D11.Resource.FromSwapChain<Texture2D>(_swapChain, 0);
                _renderTargetView = new RenderTargetView(_device, _backBuffer);

                // Setup render target
                _deviceContext.OutputMerger.SetRenderTargets(_renderTargetView);

                // Setup viewport
                var viewport = new Viewport(0, 0, width, height);
                _deviceContext.Rasterizer.SetViewport(viewport);

                _isInitialized = true;
                _logger.LogInformation("DirectX 11 renderer initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize DirectX 11 renderer");
                throw;
            }
        }

        /// <summary>
        /// Clears the render target with a transparent color.
        /// </summary>
        public void Clear()
        {
            if (!_isInitialized || _deviceContext == null || _renderTargetView == null)
                return;

            // Clear with transparent black
            _deviceContext.ClearRenderTargetView(_renderTargetView, new RawColor4(0, 0, 0, 0));
        }

        /// <summary>
        /// Presents the rendered frame to the screen.
        /// </summary>
        /// <param name="vsync">Whether to enable vertical sync.</param>
        public void Present(bool vsync = true)
        {
            if (!_isInitialized || _swapChain == null)
                return;

            try
            {
                _swapChain.Present(vsync ? 1 : 0, PresentFlags.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error presenting render target");
            }
        }

        /// <summary>
        /// Gets the device context for custom rendering operations.
        /// </summary>
        public DeviceContext? GetDeviceContext() => _deviceContext;

        /// <summary>
        /// Gets the underlying Direct3D device.
        /// </summary>
        public SharpDX.Direct3D11.Device? GetDevice() => _device;

        /// <summary>
        /// Resizes the swap chain to match new window dimensions.
        /// </summary>
        /// <param name="width">New width in pixels.</param>
        /// <param name="height">New height in pixels.</param>
        public void Resize(int width, int height)
        {
            if (!_isInitialized || _swapChain == null || _deviceContext == null)
                return;

            try
            {
                _logger.LogInformation("Resizing render target: {OldWidth}x{OldHeight} -> {NewWidth}x{NewHeight}",
                    _screenWidth, _screenHeight, width, height);

                // Release old resources
                _renderTargetView?.Dispose();
                _backBuffer?.Dispose();

                // Resize swap chain
                _swapChain.ResizeBuffers(1, width, height, Format.B8G8R8A8_UNorm, SwapChainFlags.None);

                // Recreate render target
                _backBuffer = SharpDX.Direct3D11.Resource.FromSwapChain<Texture2D>(_swapChain, 0);
                _renderTargetView = new RenderTargetView(_device, _backBuffer);

                // Update render target
                _deviceContext.OutputMerger.SetRenderTargets(_renderTargetView);

                // Update viewport
                var viewport = new Viewport(0, 0, width, height);
                _deviceContext.Rasterizer.SetViewport(viewport);

                _screenWidth = width;
                _screenHeight = height;

                _logger.LogInformation("Render target resized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resizing render target");
            }
        }

        private SwapChainDescription GetSwapChainDescription(IntPtr windowHandle, int width, int height)
        {
            return new SwapChainDescription
            {
                BufferCount = 1,
                ModeDescription = new ModeDescription(width, height, new Rational(60, 1), Format.B8G8R8A8_UNorm),
                IsWindowed = true,
                OutputHandle = windowHandle,
                SampleDescription = new SampleDescription(1, 0),
                SwapEffect = SwapEffect.Discard,
                Usage = Usage.RenderTargetOutput,
                Flags = SwapChainFlags.None
            };
        }

        /// <summary>
        /// Disposes all DirectX resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            try
            {
                _renderTargetView?.Dispose();
                _backBuffer?.Dispose();
                _swapChain?.Dispose();
                _deviceContext?.Dispose();
                _device?.Dispose();
                _isInitialized = false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing DirectX renderer");
            }

            _disposed = true;
            GC.SuppressFinalize(this);
        }

        public bool IsInitialized => _isInitialized;
        public int ScreenWidth => _screenWidth;
        public int ScreenHeight => _screenHeight;
    }
}