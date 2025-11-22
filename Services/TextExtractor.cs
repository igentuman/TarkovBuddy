using System.Drawing;
using Microsoft.Extensions.Logging;

namespace TarkovBuddy.Services
{
    /// <summary>
    /// Utility service for extracting and preprocessing text regions from images.
    /// Handles region cropping, image preprocessing, and multi-region extraction.
    /// </summary>
    public class TextExtractor
    {
        private readonly ILogger _logger;
        private readonly OcrEngine _ocrEngine;

        public TextExtractor(ILogger logger, OcrEngine ocrEngine)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _ocrEngine = ocrEngine ?? throw new ArgumentNullException(nameof(ocrEngine));
        }

        /// <summary>
        /// Extracts a region from a bitmap and optionally preprocesses it.
        /// </summary>
        /// <param name="bitmap">Source bitmap.</param>
        /// <param name="region">Region to extract.</param>
        /// <param name="preprocess">Whether to apply preprocessing (grayscale, contrast).</param>
        /// <returns>Extracted and optionally preprocessed bitmap.</returns>
        public Bitmap? ExtractRegion(Bitmap bitmap, Rectangle region, bool preprocess = true)
        {
            try
            {
                // Clamp region to bitmap bounds
                var clampedRegion = Rectangle.Intersect(region, new Rectangle(0, 0, bitmap.Width, bitmap.Height));

                if (clampedRegion.IsEmpty || clampedRegion.Width < 5 || clampedRegion.Height < 5)
                {
                    _logger.LogWarning("Extracted region is too small: {Region}", clampedRegion);
                    return null;
                }

                var extracted = new Bitmap(clampedRegion.Width, clampedRegion.Height);
                using (var g = Graphics.FromImage(extracted))
                {
                    g.DrawImage(bitmap, new Rectangle(0, 0, clampedRegion.Width, clampedRegion.Height),
                                clampedRegion, GraphicsUnit.Pixel);
                }

                if (preprocess)
                {
                    var preprocessed = PreprocessImage(extracted);
                    extracted.Dispose();
                    return preprocessed;
                }

                return extracted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting region {Region}", region);
                return null;
            }
        }

        /// <summary>
        /// Preprocesses an image to improve OCR accuracy.
        /// Applies grayscale conversion, contrast enhancement, and noise reduction.
        /// </summary>
        /// <param name="bitmap">Image to preprocess.</param>
        /// <returns>Preprocessed bitmap.</returns>
        public Bitmap PreprocessImage(Bitmap bitmap)
        {
            try
            {
                // Convert to grayscale
                var grayscale = ConvertToGrayscale(bitmap);

                // Enhance contrast
                var enhanced = EnhanceContrast(grayscale);
                grayscale.Dispose();

                return enhanced;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error preprocessing image");
                return bitmap;
            }
        }

        /// <summary>
        /// Converts a bitmap to grayscale.
        /// </summary>
        private static Bitmap ConvertToGrayscale(Bitmap bitmap)
        {
            var grayscale = new Bitmap(bitmap.Width, bitmap.Height);

            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    var color = bitmap.GetPixel(x, y);
                    // Standard luminance formula
                    int gray = (int)(color.R * 0.299 + color.G * 0.587 + color.B * 0.114);
                    grayscale.SetPixel(x, y, System.Drawing.Color.FromArgb(gray, gray, gray));
                }
            }

            return grayscale;
        }

        /// <summary>
        /// Enhances contrast of a bitmap using adaptive histogram equalization.
        /// </summary>
        private static Bitmap EnhanceContrast(Bitmap bitmap)
        {
            var enhanced = new Bitmap(bitmap);
            int width = bitmap.Width;
            int height = bitmap.Height;

            // Simple contrast enhancement: scale pixel values around midpoint
            var midpoint = 128;
            var scale = 1.2;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var color = bitmap.GetPixel(x, y);
                    int gray = color.R; // Grayscale image, so R=G=B

                    // Scale away from midpoint
                    int adjusted = (int)((gray - midpoint) * scale + midpoint);
                    adjusted = Math.Clamp(adjusted, 0, 255);

                    enhanced.SetPixel(x, y, System.Drawing.Color.FromArgb(adjusted, adjusted, adjusted));
                }
            }

            return enhanced;
        }

        /// <summary>
        /// Extracts text from multiple regions in a single image.
        /// </summary>
        /// <param name="bitmap">Source bitmap.</param>
        /// <param name="regions">List of regions to extract from.</param>
        /// <param name="preprocess">Whether to preprocess regions.</param>
        /// <returns>List of extracted texts in same order as regions.</returns>
        public List<string> ExtractTextFromRegions(Bitmap bitmap, List<Rectangle> regions, bool preprocess = true)
        {
            var results = new List<string>();

            foreach (var region in regions)
            {
                var extracted = ExtractRegion(bitmap, region, preprocess);
                if (extracted != null)
                {
                    var text = _ocrEngine.ExtractText(extracted);
                    results.Add(text);
                    extracted.Dispose();
                }
                else
                {
                    results.Add(string.Empty);
                }
            }

            return results;
        }

        /// <summary>
        /// Detects text regions in an image using simple edge detection.
        /// </summary>
        /// <param name="bitmap">Source bitmap.</param>
        /// <param name="minHeight">Minimum region height in pixels.</param>
        /// <returns>List of detected text regions.</returns>
        public List<Rectangle> DetectTextRegions(Bitmap bitmap, int minHeight = 10)
        {
            var regions = new List<Rectangle>();

            try
            {
                // Simple row-based detection
                var hasContent = new bool[bitmap.Height];

                for (int y = 0; y < bitmap.Height; y++)
                {
                    int nonWhitePixels = 0;

                    for (int x = 0; x < bitmap.Width; x++)
                    {
                        var color = bitmap.GetPixel(x, y);
                        // Check if pixel is not white (low brightness)
                        if (color.R < 200 || color.G < 200 || color.B < 200)
                        {
                            nonWhitePixels++;
                        }
                    }

                    hasContent[y] = nonWhitePixels > bitmap.Width * 0.1; // At least 10% non-white
                }

                // Find continuous regions
                bool inRegion = false;
                int regionStart = 0;

                for (int y = 0; y < bitmap.Height; y++)
                {
                    if (hasContent[y] && !inRegion)
                    {
                        inRegion = true;
                        regionStart = y;
                    }
                    else if (!hasContent[y] && inRegion)
                    {
                        inRegion = false;
                        int regionHeight = y - regionStart;

                        if (regionHeight >= minHeight)
                        {
                            regions.Add(new Rectangle(0, regionStart, bitmap.Width, regionHeight));
                        }
                    }
                }

                // Handle region that extends to bottom
                if (inRegion)
                {
                    int regionHeight = bitmap.Height - regionStart;
                    if (regionHeight >= minHeight)
                    {
                        regions.Add(new Rectangle(0, regionStart, bitmap.Width, regionHeight));
                    }
                }

                _logger.LogInformation("Detected {Count} text regions", regions.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting text regions");
            }

            return regions;
        }
    }
}