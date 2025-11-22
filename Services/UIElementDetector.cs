using Microsoft.Extensions.Logging;
using TarkovBuddy.Models;
using System.Drawing;

namespace TarkovBuddy.Services
{
    /// <summary>
    /// Specialized detector for game UI elements including item icons, extraction buttons, map elements, etc.
    /// Builds on top of the general object detection service with game-specific knowledge.
    /// </summary>
    public class UIElementDetector
    {
        private readonly IObjectDetectionService _detectionService;
        private readonly ILogger<UIElementDetector> _logger;

        /// <summary>
        /// Cache of detected UI elements keyed by frame number for rapid access.
        /// </summary>
        private Dictionary<long, UIElementDetectionResult> _detectionCache = new();
        private const int MaxCacheSize = 100;

        public UIElementDetector(IObjectDetectionService detectionService, ILogger<UIElementDetector> logger)
        {
            _detectionService = detectionService ?? throw new ArgumentNullException(nameof(detectionService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Detects all UI elements in the provided frame.
        /// Returns categorized results for different element types.
        /// </summary>
        public async Task<UIElementDetectionResult> DetectUIElementsAsync(Frame frame, CancellationToken ct = default)
        {
            try
            {
                // Check cache first
                if (_detectionCache.TryGetValue(frame.FrameNumber, out var cached))
                {
                    _logger.LogDebug($"UI element detection cache hit for frame {frame.FrameNumber}");
                    return cached;
                }

                // Run detection
                var detections = await _detectionService.DetectAsync(frame, confidenceThreshold: 0.5f, ct);

                // Categorize detections
                var result = CategorizeDetections(detections, frame);

                // Cache result
                CacheResult(frame.FrameNumber, result);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting UI elements");
                return new UIElementDetectionResult();
            }
        }

        /// <summary>
        /// Detects item icons in the stash/inventory grid.
        /// </summary>
        public async Task<List<ItemIconDetection>> DetectItemIconsAsync(Frame frame, Rectangle gridRegion, CancellationToken ct = default)
        {
            try
            {
                var detections = await _detectionService.DetectInRegionAsync(frame, gridRegion, confidenceThreshold: 0.7f, ct);
                return detections
                    .Where(d => d.Label.Contains("item", StringComparison.OrdinalIgnoreCase))
                    .Select(d => new ItemIconDetection
                    {
                        DetectionResult = d,
                        GridPosition = GetGridPosition(d.BoundingBox, gridRegion),
                        Confidence = d.Confidence
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting item icons");
                return new List<ItemIconDetection>();
            }
        }

        /// <summary>
        /// Detects extraction buttons/indicators on the extraction screen.
        /// </summary>
        public async Task<List<ExtractionPointDetection>> DetectExtractionPointsAsync(Frame frame, CancellationToken ct = default)
        {
            try
            {
                var detections = await _detectionService.DetectAsync(frame, confidenceThreshold: 0.65f, ct);
                return detections
                    .Where(d => d.Label.Contains("extraction", StringComparison.OrdinalIgnoreCase))
                    .Select(d => new ExtractionPointDetection
                    {
                        DetectionResult = d,
                        Confidence = d.Confidence,
                        ScreenPosition = d.GetCenter()
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting extraction points");
                return new List<ExtractionPointDetection>();
            }
        }

        /// <summary>
        /// Detects map selection UI elements.
        /// </summary>
        public async Task<List<MapElementDetection>> DetectMapElementsAsync(Frame frame, CancellationToken ct = default)
        {
            try
            {
                var detections = await _detectionService.DetectAsync(frame, confidenceThreshold: 0.6f, ct);
                return detections
                    .Where(d => d.Label.Contains("map", StringComparison.OrdinalIgnoreCase))
                    .Select(d => new MapElementDetection
                    {
                        DetectionResult = d,
                        Confidence = d.Confidence,
                        ElementType = DetermineMapElementType(d.Label)
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting map elements");
                return new List<MapElementDetection>();
            }
        }

        /// <summary>
        /// Detects status/health bar UI elements.
        /// </summary>
        public async Task<List<StatusBarDetection>> DetectStatusBarsAsync(Frame frame, CancellationToken ct = default)
        {
            try
            {
                var detections = await _detectionService.DetectAsync(frame, confidenceThreshold: 0.7f, ct);
                return detections
                    .Where(d => d.Label.Contains("bar", StringComparison.OrdinalIgnoreCase) ||
                               d.Label.Contains("health", StringComparison.OrdinalIgnoreCase) ||
                               d.Label.Contains("energy", StringComparison.OrdinalIgnoreCase))
                    .Select(d => new StatusBarDetection
                    {
                        DetectionResult = d,
                        BarType = DetermineBarType(d.Label),
                        Confidence = d.Confidence
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting status bars");
                return new List<StatusBarDetection>();
            }
        }

        /// <summary>
        /// Clears the detection cache to free memory.
        /// </summary>
        public void ClearCache()
        {
            _detectionCache.Clear();
            _logger.LogDebug("UI element detection cache cleared");
        }

        /// <summary>
        /// Gets cache statistics.
        /// </summary>
        public (int CachedFrames, int MaxCacheSize) GetCacheStats()
        {
            return (_detectionCache.Count, MaxCacheSize);
        }

        /// <summary>
        /// Categorizes raw detections into structured UI element types.
        /// </summary>
        private UIElementDetectionResult CategorizeDetections(List<DetectionResult> detections, Frame frame)
        {
            var result = new UIElementDetectionResult
            {
                FrameNumber = frame.FrameNumber,
                DetectionTimestamp = DateTime.UtcNow,
                TotalDetections = detections.Count
            };

            foreach (var detection in detections)
            {
                if (detection.Label.Contains("item", StringComparison.OrdinalIgnoreCase))
                    result.ItemIcons.Add(detection);
                else if (detection.Label.Contains("extraction", StringComparison.OrdinalIgnoreCase))
                    result.ExtractionPoints.Add(detection);
                else if (detection.Label.Contains("map", StringComparison.OrdinalIgnoreCase))
                    result.MapElements.Add(detection);
                else if (detection.Label.Contains("health", StringComparison.OrdinalIgnoreCase) ||
                        detection.Label.Contains("energy", StringComparison.OrdinalIgnoreCase))
                    result.StatusBars.Add(detection);
                else if (detection.Label.Contains("button", StringComparison.OrdinalIgnoreCase))
                    result.Buttons.Add(detection);
                else
                    result.OtherElements.Add(detection);
            }

            return result;
        }

        /// <summary>
        /// Determines the position of an item icon within the grid.
        /// </summary>
        private (int Row, int Column) GetGridPosition(Rectangle itemBbox, Rectangle gridRegion)
        {
            // Approximate grid position based on bounding box location within grid
            const int CellSize = 64; // Approximate item cell size
            int row = (itemBbox.Y - gridRegion.Y) / CellSize;
            int col = (itemBbox.X - gridRegion.X) / CellSize;
            return (Math.Max(0, row), Math.Max(0, col));
        }

        /// <summary>
        /// Determines map element type from label.
        /// </summary>
        private string DetermineMapElementType(string label)
        {
            return label.ToLower() switch
            {
                var s when s.Contains("customs") => "Customs",
                var s when s.Contains("factory") => "Factory",
                var s when s.Contains("shoreline") => "Shoreline",
                var s when s.Contains("woods") => "Woods",
                var s when s.Contains("lighthouse") => "Lighthouse",
                var s when s.Contains("streets") => "Streets",
                var s when s.Contains("reserve") => "Reserve",
                var s when s.Contains("ground-zero") => "Ground Zero",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Determines status bar type from label.
        /// </summary>
        private string DetermineBarType(string label)
        {
            var lowerLabel = label.ToLower();
            return lowerLabel switch
            {
                var s when s.Contains("health") => "Health",
                var s when s.Contains("energy") => "Energy",
                var s when s.Contains("hydration") => "Hydration",
                var s when s.Contains("bar") => "Generic",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Caches detection result with LRU eviction.
        /// </summary>
        private void CacheResult(long frameNumber, UIElementDetectionResult result)
        {
            if (_detectionCache.Count >= MaxCacheSize)
            {
                // Remove oldest entry
                var oldestKey = _detectionCache.Keys.First();
                _detectionCache.Remove(oldestKey);
            }

            _detectionCache[frameNumber] = result;
        }
    }

    /// <summary>
    /// Complete UI element detection result for a single frame.
    /// </summary>
    public class UIElementDetectionResult
    {
        public long FrameNumber { get; set; }
        public DateTime DetectionTimestamp { get; set; }
        public int TotalDetections { get; set; }

        public List<DetectionResult> ItemIcons { get; set; } = new();
        public List<DetectionResult> ExtractionPoints { get; set; } = new();
        public List<DetectionResult> MapElements { get; set; } = new();
        public List<DetectionResult> StatusBars { get; set; } = new();
        public List<DetectionResult> Buttons { get; set; } = new();
        public List<DetectionResult> OtherElements { get; set; } = new();

        public int TotalUIElements =>
            ItemIcons.Count + ExtractionPoints.Count + MapElements.Count +
            StatusBars.Count + Buttons.Count + OtherElements.Count;
    }

    /// <summary>
    /// Detection result for item icon.
    /// </summary>
    public class ItemIconDetection
    {
        public DetectionResult DetectionResult { get; set; } = null!;
        public (int Row, int Column) GridPosition { get; set; }
        public float Confidence { get; set; }
    }

    /// <summary>
    /// Detection result for extraction point.
    /// </summary>
    public class ExtractionPointDetection
    {
        public DetectionResult DetectionResult { get; set; } = null!;
        public Point ScreenPosition { get; set; }
        public float Confidence { get; set; }
    }

    /// <summary>
    /// Detection result for map element.
    /// </summary>
    public class MapElementDetection
    {
        public DetectionResult DetectionResult { get; set; } = null!;
        public string ElementType { get; set; } = string.Empty;
        public float Confidence { get; set; }
    }

    /// <summary>
    /// Detection result for status bar.
    /// </summary>
    public class StatusBarDetection
    {
        public DetectionResult DetectionResult { get; set; } = null!;
        public string BarType { get; set; } = string.Empty;
        public float Confidence { get; set; }
    }
}