using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using TarkovBuddy.Models;

namespace TarkovBuddy.Services
{
    /// <summary>
    /// High-performance screen capture service using Windows Graphics Capture API via DirectX.
    /// Captures frames at 30 FPS with minimal latency (<50ms).
    /// </summary>
    public class ScreenCaptureService : IScreenCaptureService
    {
        private readonly IConfigurationService _configService;
        private readonly ILogger<ScreenCaptureService> _logger;

        private FrameBuffer _frameBuffer = null!;
        private Thread? _captureWorkerThread;
        private CancellationTokenSource _workerCts = null!;
        private bool _isRunning;

        private Device? _device;
        private DeviceContext? _context;
        private Texture2D? _stagingTexture;
        private Output? _output;

        private long _totalFramesCaptured;
        private long _totalFramesDropped;
        private Stopwatch _fpsStopwatch = null!;
        private long _fpsFrameCounter;

        private const int TargetFpsNumerator = 30;
        private const int FrameBufferSize = 5;
        private const int FrameTimeMs = 33; // 1000 / 30 FPS

        public event EventHandler<FrameCapturedEventArgs>? FrameCaptured;

        public ScreenCaptureService(IConfigurationService configService, ILogger<ScreenCaptureService> logger)
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _isRunning = false;
            _totalFramesCaptured = 0;
            _totalFramesDropped = 0;
            _fpsFrameCounter = 0;
        }

        /// <summary>
        /// Starts the screen capture worker thread.
        /// </summary>
        public void Start()
        {
            if (_isRunning)
            {
                _logger.LogWarning("Screen capture already running");
                return;
            }

            try
            {
                _logger.LogInformation("Starting screen capture service...");

                // Initialize DirectX
                InitializeDirectX();

                // Create frame buffer
                _frameBuffer = new FrameBuffer(FrameBufferSize);

                // Create and start worker thread
                _workerCts = new CancellationTokenSource();
                _fpsStopwatch = Stopwatch.StartNew();

                _captureWorkerThread = new Thread(CaptureWorkerLoop)
                {
                    Name = "ScreenCaptureWorker",
                    IsBackground = true,
                    Priority = ThreadPriority.AboveNormal
                };

                _isRunning = true;
                _captureWorkerThread.Start();

                _logger.LogInformation("Screen capture service started successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start screen capture service");
                _isRunning = false;
                throw;
            }
        }

        /// <summary>
        /// Stops the screen capture worker thread and releases resources.
        /// </summary>
        public void Stop()
        {
            if (!_isRunning)
                return;

            try
            {
                _logger.LogInformation("Stopping screen capture service...");

                _isRunning = false;
                _workerCts?.Cancel();
                _captureWorkerThread?.Join(5000);

                CleanupDirectX();
                _frameBuffer?.Dispose();
                _workerCts?.Dispose();

                _logger.LogInformation("Screen capture service stopped. " +
                    $"Captured: {_totalFramesCaptured} frames, " +
                    $"Dropped: {_totalFramesDropped} frames, " +
                    $"Avg FPS: {CurrentFps:F1}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping screen capture service");
            }
        }

        /// <summary>
        /// Asynchronously yields frames from the capture buffer.
        /// </summary>
        public async IAsyncEnumerable<Frame> GetFramesAsync(CancellationToken ct = default)
        {
            if (_frameBuffer == null)
                throw new InvalidOperationException("Capture service not started. Call Start() first.");

            _logger.LogInformation("Starting frame enumeration");

            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _workerCts!.Token);

            try
            {
                while (!linkedCts.Token.IsCancellationRequested)
                {
                    if (_frameBuffer.TryDequeue(out var frame))
                    {
                        if (frame != null)
                        {
                            yield return frame;
                            await Task.Delay(1, linkedCts.Token).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        // No frames available, yield control briefly
                        await Task.Delay(1, linkedCts.Token).ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                linkedCts.Dispose();
            }
        }

        /// <summary>
        /// Captures a single screenshot immediately.
        /// </summary>
        public Bitmap? CaptureScreenshot()
        {
            try
            {
                var frame = CaptureFrame();
                if (frame == null)
                    return null;

                // Convert frame to bitmap
                var bitmap = new Bitmap(frame.Width, frame.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                var bitmapData = bitmap.LockBits(
                    new Rectangle(0, 0, frame.Width, frame.Height),
                    System.Drawing.Imaging.ImageLockMode.WriteOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                try
                {
                    Marshal.Copy(frame.PixelData, 0, bitmapData.Scan0, frame.PixelData.Length);
                }
                finally
                {
                    bitmap.UnlockBits(bitmapData);
                    frame.Dispose();
                }

                return bitmap;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to capture screenshot");
                return null;
            }
        }

        public int BufferedFrameCount => _frameBuffer?.Count ?? 0;

        public double CurrentFps
        {
            get
            {
                if (_fpsStopwatch == null || _fpsFrameCounter == 0)
                    return 0;

                var elapsed = _fpsStopwatch.Elapsed.TotalSeconds;
                return elapsed > 0 ? _fpsFrameCounter / elapsed : 0;
            }
        }

        public bool IsRunning => _isRunning;

        /// <summary>
        /// Worker thread that continuously captures frames.
        /// </summary>
        private void CaptureWorkerLoop()
        {
            _logger.LogInformation("Capture worker thread started");

            var frameStopwatch = Stopwatch.StartNew();
            int framesSinceLastLog = 0;

            try
            {
                while (_isRunning && !_workerCts!.Token.IsCancellationRequested)
                {
                    frameStopwatch.Restart();

                    try
                    {
                        var frame = CaptureFrame();

                        if (frame != null)
                        {
                            int droppedFrames = _frameBuffer!.Enqueue(frame);
                            _totalFramesCaptured++;
                            _totalFramesDropped += droppedFrames;
                            _fpsFrameCounter++;
                            framesSinceLastLog++;

                            var captureTime = frameStopwatch.ElapsedMilliseconds;
                            FrameCaptured?.Invoke(this, new FrameCapturedEventArgs
                            {
                                Frame = frame,
                                DroppedFrames = droppedFrames,
                                CaptureTimeMs = captureTime
                            });

                            // Log every 30 frames (~1 second)
                            if (framesSinceLastLog >= 30)
                            {
                                _logger.LogDebug($"FPS: {CurrentFps:F1}, Buffered: {_frameBuffer.Count}");
                                framesSinceLastLog = 0;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during frame capture");
                    }

                    // Maintain target FPS
                    var elapsedMs = frameStopwatch.ElapsedMilliseconds;
                    if (elapsedMs < FrameTimeMs)
                    {
                        var sleepMs = (int)(FrameTimeMs - elapsedMs);
                        if (sleepMs > 0)
                            Thread.Sleep(sleepMs);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Capture worker thread crashed");
            }
            finally
            {
                _logger.LogInformation("Capture worker thread ended");
            }
        }

        /// <summary>
        /// Captures a single frame from the screen.
        /// </summary>
        private Frame? CaptureFrame()
        {
            try
            {
                if (_device == null || _context == null || _output == null)
                    return null;

                // Get the desktop image
                using var resource = _output.GetParent<Adapter>().GetParent<Factory>();
                using var factory = new Factory1();

                // Duplicate the output
                using var duplicatedOutput = _output.DuplicateOutput(_device);

                duplicatedOutput.AcquireNextFrame(100, out var frameResourcePtr, out var desktopResource);

                try
                {
                    using var screenTexture = desktopResource.QueryInterface<Texture2D>();

                    // Copy to staging texture
                    _context.CopyResource(screenTexture, _stagingTexture);

                    // Map the staging texture for reading
                    var mappedResource = _context.MapSubresource(_stagingTexture, 0, MapMode.Read, MapFlags.None);

                    try
                    {
                        var desc = _stagingTexture!.Description;
                        var frame = new Frame(desc.Width, desc.Height, mappedResource.RowPitch)
                        {
                            Timestamp = DateTime.UtcNow,
                            FrameNumber = _totalFramesCaptured
                        };

                        // Copy pixel data
                        Marshal.Copy(mappedResource.DataPointer, frame.PixelData, 0, frame.PixelData.Length);

                        return frame;
                    }
                    finally
                    {
                        _context.UnmapSubresource(_stagingTexture, 0);
                    }
                }
                finally
                {
                    duplicatedOutput?.ReleaseFrame();
                }
            }
            catch (SharpDXException ex) when ((uint)ex.HResult == 0x80070005)
            {
                // Access denied - window may have changed
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to capture frame");
                return null;
            }
        }

        /// <summary>
        /// Initializes DirectX components for screen capture.
        /// </summary>
        private void InitializeDirectX()
        {
            try
            {
                // Create device and context
                Device.CreateWithSwapChain(
                    DriverType.Hardware,
                    DeviceCreationFlags.BgraSupport,
                    new[] { FeatureLevel.Level_11_0 },
                    null,
                    out _device,
                    out var swapChain);

                _context = _device!.ImmediateContext;

                // Get the output (screen)
                using (var adapter = _device.Adapter as Adapter)
                {
                    _output = adapter?.GetOutput(0);
                }

                if (_output == null)
                    throw new InvalidOperationException("Failed to get screen output");

                // Create staging texture for frame reading
                var desc = new Texture2DDescription
                {
                    Width = _output.Description.DesktopCoordinates.Right,
                    Height = _output.Description.DesktopCoordinates.Bottom,
                    MipLevels = 1,
                    ArraySize = 1,
                    Format = Format.B8G8R8A8_UNorm,
                    SampleDescription = new SampleDescription(1, 0),
                    Usage = ResourceUsage.Staging,
                    BindFlags = BindFlags.None,
                    CpuAccessFlags = CpuAccessFlags.Read,
                    OptionFlags = ResourceOptionFlags.None
                };

                _stagingTexture = new Texture2D(_device, desc);

                _logger.LogInformation($"DirectX initialized. Screen resolution: {desc.Width}x{desc.Height}");
            }
            catch (Exception ex)
            {
                CleanupDirectX();
                _logger.LogError(ex, "Failed to initialize DirectX");
                throw;
            }
        }

        /// <summary>
        /// Cleans up DirectX resources.
        /// </summary>
        private void CleanupDirectX()
        {
            try
            {
                _stagingTexture?.Dispose();
                _output?.Dispose();
                _context?.Dispose();
                _device?.Dispose();

                _stagingTexture = null;
                _output = null;
                _context = null;
                _device = null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during DirectX cleanup");
            }
        }

        public void Dispose()
        {
            Stop();
            GC.SuppressFinalize(this);
        }
    }
}