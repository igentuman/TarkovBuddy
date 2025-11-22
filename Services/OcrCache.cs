using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace TarkovBuddy.Services
{
    /// <summary>
    /// Cache for OCR results to avoid re-processing identical images.
    /// Uses image hashing to identify duplicate content.
    /// </summary>
    public class OcrCache
    {
        private readonly ConcurrentDictionary<string, CacheEntry> _cache;
        private readonly int _maxSize;
        private readonly ILogger _logger;

        private long _cacheHits;
        private long _cacheMisses;

        private const int DefaultMaxSize = 100;

        /// <summary>
        /// Represents a cached OCR result.
        /// </summary>
        private class CacheEntry
        {
            public string Text { get; set; } = string.Empty;
            public double Confidence { get; set; }
            public long ProcessingTimeMs { get; set; }
            public DateTime CachedTime { get; set; }
            public long AccessCount { get; set; }
            public string? ImageHash { get; set; }
        }

        public OcrCache(ILogger logger, int maxSize = DefaultMaxSize)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _maxSize = Math.Max(1, maxSize);
            _cache = new ConcurrentDictionary<string, CacheEntry>();
            _cacheHits = 0;
            _cacheMisses = 0;

            _logger.LogInformation("OcrCache initialized with max size: {MaxSize}", _maxSize);
        }

        /// <summary>
        /// Tries to get a cached OCR result for a bitmap.
        /// </summary>
        /// <param name="bitmap">Bitmap to look up.</param>
        /// <param name="result">Cached OCR result if found.</param>
        /// <returns>True if result was found in cache.</returns>
        public bool TryGetCached(System.Drawing.Bitmap bitmap, out OcrResult? result)
        {
            result = null;

            try
            {
                var hash = ComputeImageHash(bitmap);

                if (_cache.TryGetValue(hash, out var entry))
                {
                    entry.AccessCount++;
                    Interlocked.Increment(ref _cacheHits);

                    result = new OcrResult
                    {
                        Text = entry.Text,
                        Confidence = entry.Confidence,
                        ProcessingTimeMs = entry.ProcessingTimeMs,
                        WasFromCache = true
                    };

                    return true;
                }

                Interlocked.Increment(ref _cacheMisses);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error accessing cache");
                return false;
            }
        }

        /// <summary>
        /// Adds an OCR result to the cache.
        /// </summary>
        /// <param name="bitmap">Bitmap that was processed.</param>
        /// <param name="text">Extracted text.</param>
        /// <param name="confidence">Confidence score.</param>
        /// <param name="processingTimeMs">Time taken to extract text.</param>
        public void AddToCache(System.Drawing.Bitmap bitmap, string text, double confidence, long processingTimeMs)
        {
            try
            {
                // Evict oldest entries if cache is full
                if (_cache.Count >= _maxSize)
                {
                    EvictOldest();
                }

                var hash = ComputeImageHash(bitmap);
                var entry = new CacheEntry
                {
                    Text = text,
                    Confidence = confidence,
                    ProcessingTimeMs = processingTimeMs,
                    CachedTime = DateTime.UtcNow,
                    AccessCount = 0,
                    ImageHash = hash
                };

                _cache.AddOrUpdate(hash, entry, (key, old) =>
                {
                    old.AccessCount++; // Track repeated caching
                    return entry;
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error adding entry to cache");
            }
        }

        /// <summary>
        /// Gets the hash of a bitmap for cache lookup.
        /// </summary>
        private static string ComputeImageHash(System.Drawing.Bitmap bitmap)
        {
            // Simple hash based on image dimensions and first/last pixel
            var data = $"{bitmap.Width}x{bitmap.Height}";

            // Sample first and last pixels
            if (bitmap.Width > 0 && bitmap.Height > 0)
            {
                var first = bitmap.GetPixel(0, 0);
                var last = bitmap.GetPixel(bitmap.Width - 1, bitmap.Height - 1);
                data += $":{first.ToArgb()}:{last.ToArgb()}";
            }

            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(data));
                return Convert.ToBase64String(hash);
            }
        }

        /// <summary>
        /// Evicts the oldest cached entry.
        /// </summary>
        private void EvictOldest()
        {
            var oldest = _cache.Values
                .OrderBy(e => e.CachedTime)
                .FirstOrDefault();

            if (oldest?.ImageHash != null)
            {
                _cache.TryRemove(oldest.ImageHash, out _);
                _logger.LogDebug("Evicted oldest cache entry");
            }
        }

        /// <summary>
        /// Clears all cached entries.
        /// </summary>
        public void Clear()
        {
            _cache.Clear();
            _logger.LogInformation("OcrCache cleared");
        }

        /// <summary>
        /// Gets cache statistics.
        /// </summary>
        public OcrCacheStats GetStats()
        {
            return new OcrCacheStats
            {
                CachedItems = _cache.Count,
                CacheHits = _cacheHits,
                CacheMisses = _cacheMisses
            };
        }

        /// <summary>
        /// Gets the number of cached items.
        /// </summary>
        public int CacheSize => _cache.Count;

        /// <summary>
        /// Gets the maximum cache size.
        /// </summary>
        public int MaxSize => _maxSize;
    }
}