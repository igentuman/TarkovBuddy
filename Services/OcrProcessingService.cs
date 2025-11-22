using System.Diagnostics;
using System.Drawing;
using Microsoft.Extensions.Logging;
using TarkovBuddy.Models;

namespace TarkovBuddy.Services
{
    /// <summary>
    /// Main OCR processing service that orchestrates text extraction from game screenshots.
    /// Combines Tesseract OCR with caching and preprocessing for optimal performance.
    /// </summary>
    public class OcrProcessingService : IOcrProcessingService
    {
        private readonly IConfigurationService _configService;
        private readonly ILogger<OcrProcessingService> _logger;

        private OcrEngine? _ocrEngine;
        private TextExtractor? _textExtractor;
        private OcrCache? _ocrCache;

        private long _totalOperations;
        private long _totalProcessingTimeMs;
        private bool _isInitialized;
        private bool _isDisposed;
        private bool _isRunning;

        private const int DefaultCacheSize = 100;
        private const int MaxProcessingTimeMs = 500; // Target <500ms per operation

        public OcrProcessingService(IConfigurationService configService, ILogger<OcrProcessingService> logger)
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _isInitialized = false;
            _isDisposed = false;
            _totalOperations = 0;
            _totalProcessingTimeMs = 0;
        }

        /// <summary>
        /// Service name for logging and identification.
        /// </summary>
        public string ServiceName => "OcrProcessingService";

        /// <summary>
        /// Gets whether the service is currently running.
        /// </summary>
        public bool IsRunning => _isRunning;

        /// <summary>
        /// Initializes the OCR service asynchronously.
        /// Implements IService.InitializeAsync().
        /// </summary>
        public async Task InitializeAsync()
        {
            ThrowIfDisposed();
            
            if (!Initialize())
            {
                throw new InvalidOperationException("Failed to initialize OcrProcessingService");
            }

            _isRunning = true;
            await Task.CompletedTask; // Return completed task for async compatibility
        }

        /// <summary>
        /// Initializes the OCR service.
        /// Must be called before using Extract methods.
        /// </summary>
        public bool Initialize()
        {
            ThrowIfDisposed();

            try
            {
                if (_isInitialized)
                {
                    _logger.LogWarning("OcrProcessingService already initialized");
                    return true;
                }

                _logger.LogInformation("Initializing OcrProcessingService...");

                // Initialize OCR engine
                _ocrEngine = new OcrEngine(_logger);
                if (!_ocrEngine.Initialize())
                {
                    _logger.LogError("Failed to initialize OcrEngine");
                    return false;
                }

                // Initialize text extractor
                _textExtractor = new TextExtractor(_logger, _ocrEngine);

                // Initialize cache
                _ocrCache = new OcrCache(_logger, DefaultCacheSize);

                _isInitialized = true;
                _logger.LogInformation("OcrProcessingService initialized successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing OcrProcessingService");
                return false;
            }
        }

        /// <summary>
        /// Extracts text from a specified region of a bitmap.
        /// </summary>
        public async Task<OcrResult> ExtractTextAsync(Bitmap bitmap, Rectangle? region = null, CancellationToken ct = default)
        {
            ThrowIfDisposed();

            if (!_isInitialized || _ocrEngine == null || _ocrCache == null)
            {
                return new OcrResult { Text = string.Empty, Confidence = 0 };
            }

            var sw = Stopwatch.StartNew();

            try
            {
                // Try to get from cache first
                if (_ocrCache.TryGetCached(bitmap, out var cached))
                {
                    Interlocked.Increment(ref _totalOperations);
                    return cached!;
                }

                // Process in thread pool to avoid blocking
                var result = await Task.Run(() =>
                {
                    // Extract region if specified
                    Bitmap? workingBitmap = bitmap;
                    bool shouldDispose = false;

                    if (region.HasValue && region != Rectangle.Empty)
                    {
                        workingBitmap = _textExtractor?.ExtractRegion(bitmap, region.Value, preprocess: true);
                        shouldDispose = workingBitmap != null;

                        if (workingBitmap == null)
                        {
                            return new OcrResult { Text = string.Empty, Confidence = 0 };
                        }
                    }

                    try
                    {
                        // Extract text with confidence
                        var (text, confidence) = _ocrEngine.ExtractTextWithConfidence(workingBitmap);

                        sw.Stop();

                        // Add to cache
                        _ocrCache.AddToCache(bitmap, text, confidence, sw.ElapsedMilliseconds);

                        return new OcrResult
                        {
                            Text = text,
                            Confidence = confidence,
                            ProcessingTimeMs = sw.ElapsedMilliseconds,
                            WasFromCache = false,
                            DetectedLanguage = text.Any(c => c >= 'А' && c <= 'я') ? "Russian" : "English"
                        };
                    }
                    finally
                    {
                        if (shouldDispose)
                        {
                            workingBitmap?.Dispose();
                        }
                    }
                }, ct);

                Interlocked.Increment(ref _totalOperations);
                Interlocked.Add(ref _totalProcessingTimeMs, result.ProcessingTimeMs);

                if (result.ProcessingTimeMs > MaxProcessingTimeMs)
                {
                    _logger.LogWarning("OCR operation took longer than target: {Ms}ms (target: <{Max}ms)",
                                    result.ProcessingTimeMs, MaxProcessingTimeMs);
                }

                return result;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("OCR operation cancelled");
                return new OcrResult { Text = string.Empty, Confidence = 0 };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from bitmap");
                return new OcrResult { Text = string.Empty, Confidence = 0 };
            }
        }

        /// <summary>
        /// Extracts text from a frame.
        /// </summary>
        public async Task<OcrResult> ExtractTextFromFrameAsync(Frame frame, Rectangle? region = null, CancellationToken ct = default)
        {
            ThrowIfDisposed();

            if (frame?.PixelData == null)
            {
                return new OcrResult { Text = string.Empty, Confidence = 0 };
            }

            try
            {
                using (var bitmap = new Bitmap(frame.Width, frame.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                {
                    var bitmapData = bitmap.LockBits(
                        new Rectangle(0, 0, frame.Width, frame.Height),
                        System.Drawing.Imaging.ImageLockMode.WriteOnly,
                        System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                    try
                    {
                        System.Runtime.InteropServices.Marshal.Copy(frame.PixelData, 0, bitmapData.Scan0, frame.PixelData.Length);
                    }
                    finally
                    {
                        bitmap.UnlockBits(bitmapData);
                    }

                    return await ExtractTextAsync(bitmap, region, ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from frame");
                return new OcrResult { Text = string.Empty, Confidence = 0 };
            }
        }

        /// <summary>
        /// Extracts text from multiple regions in batch.
        /// </summary>
        public async Task<List<OcrResult>> ExtractTextBatchAsync(Bitmap bitmap, List<Rectangle> regions, CancellationToken ct = default)
        {
            ThrowIfDisposed();

            var results = new List<OcrResult>();

            try
            {
                var tasks = regions.Select(region => ExtractTextAsync(bitmap, region, ct)).ToList();
                var batchResults = await Task.WhenAll(tasks);
                results.AddRange(batchResults);

                _logger.LogInformation("Batch OCR completed: {Count} regions processed", regions.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in batch OCR processing");
            }

            return results;
        }

        /// <summary>
        /// Sets character whitelist for OCR.
        /// </summary>
        public void SetCharacterWhitelist(string whitelist)
        {
            ThrowIfDisposed();

            if (_ocrEngine == null)
            {
                _logger.LogWarning("OcrEngine not initialized");
                return;
            }

            _ocrEngine.SetCharacterWhitelist(whitelist);
            _ocrCache?.Clear(); // Clear cache when whitelist changes
        }

        /// <summary>
        /// Gets cache statistics.
        /// </summary>
        public OcrCacheStats GetCacheStats()
        {
            return _ocrCache?.GetStats() ?? new OcrCacheStats();
        }

        /// <summary>
        /// Clears the cache.
        /// </summary>
        public void ClearCache()
        {
            _ocrCache?.Clear();
        }

        /// <summary>
        /// Gets average processing time.
        /// </summary>
        public double AverageProcessingTimeMs
        {
            get
            {
                if (_totalOperations == 0)
                    return 0;
                return (double)_totalProcessingTimeMs / _totalOperations;
            }
        }

        /// <summary>
        /// Gets total operations.
        /// </summary>
        public long TotalOcrOperations => _totalOperations;

        /// <summary>
        /// Disposes resources.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            try
            {
                _ocrEngine?.Dispose();
                _logger.LogInformation("OcrProcessingService disposed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing OcrProcessingService");
            }

            _isDisposed = true;
            GC.SuppressFinalize(this);
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(OcrProcessingService));
        }

        ~OcrProcessingService()
        {
            Dispose();
        }
    }
}