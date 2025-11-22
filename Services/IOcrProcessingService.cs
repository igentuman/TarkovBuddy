using System.Drawing;
using TarkovBuddy.Models;

namespace TarkovBuddy.Services
{
    /// <summary>
    /// OCR (Optical Character Recognition) service for extracting text from screen regions.
    /// Processes game screenshots to identify text elements like extraction names, quest objectives, and item names.
    /// </summary>
    public interface IOcrProcessingService : IService
    {
        /// <summary>
        /// Extracts text from a specified region of a bitmap image.
        /// </summary>
        /// <param name="bitmap">Source image to extract text from.</param>
        /// <param name="region">Region of interest (optional). If null, processes entire bitmap.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Extracted text with confidence score (0-1).</returns>
        Task<OcrResult> ExtractTextAsync(Bitmap bitmap, Rectangle? region = null, CancellationToken ct = default);

        /// <summary>
        /// Extracts text from a frame with optional preprocessing.
        /// </summary>
        /// <param name="frame">Frame to process.</param>
        /// <param name="region">Region of interest (optional).</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Extracted text with confidence score.</returns>
        Task<OcrResult> ExtractTextFromFrameAsync(Frame frame, Rectangle? region = null, CancellationToken ct = default);

        /// <summary>
        /// Extracts text from multiple regions of a single frame for batch processing.
        /// </summary>
        /// <param name="bitmap">Source bitmap.</param>
        /// <param name="regions">List of regions to extract text from.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>List of OCR results in the same order as input regions.</returns>
        Task<List<OcrResult>> ExtractTextBatchAsync(Bitmap bitmap, List<Rectangle> regions, CancellationToken ct = default);

        /// <summary>
        /// Sets character whitelist for OCR (e.g., alphanumeric only, specific characters).
        /// </summary>
        /// <param name="whitelist">String containing allowed characters.</param>
        void SetCharacterWhitelist(string whitelist);

        /// <summary>
        /// Gets OCR cache statistics.
        /// </summary>
        OcrCacheStats GetCacheStats();

        /// <summary>
        /// Clears the OCR result cache.
        /// </summary>
        void ClearCache();

        /// <summary>
        /// Gets average OCR processing time in milliseconds.
        /// </summary>
        double AverageProcessingTimeMs { get; }

        /// <summary>
        /// Gets total OCR operations performed.
        /// </summary>
        long TotalOcrOperations { get; }
    }

    /// <summary>
    /// Result of an OCR extraction operation.
    /// </summary>
    public class OcrResult
    {
        /// <summary>
        /// Extracted text.
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Confidence score (0.0 to 1.0).
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// Time taken to extract text in milliseconds.
        /// </summary>
        public long ProcessingTimeMs { get; set; }

        /// <summary>
        /// Whether result was retrieved from cache.
        /// </summary>
        public bool WasFromCache { get; set; }

        /// <summary>
        /// Bounding boxes of detected text regions.
        /// </summary>
        public List<Rectangle> TextRegions { get; set; } = [];

        /// <summary>
        /// Language detected in the text.
        /// </summary>
        public string DetectedLanguage { get; set; } = "Unknown";
    }

    /// <summary>
    /// Cache statistics for OCR operations.
    /// </summary>
    public class OcrCacheStats
    {
        /// <summary>
        /// Number of items in cache.
        /// </summary>
        public int CachedItems { get; set; }

        /// <summary>
        /// Number of cache hits.
        /// </summary>
        public long CacheHits { get; set; }

        /// <summary>
        /// Number of cache misses.
        /// </summary>
        public long CacheMisses { get; set; }

        /// <summary>
        /// Cache hit rate (0.0 to 1.0).
        /// </summary>
        public double HitRate => CacheHits + CacheMisses > 0 ? (double)CacheHits / (CacheHits + CacheMisses) : 0;
    }
}