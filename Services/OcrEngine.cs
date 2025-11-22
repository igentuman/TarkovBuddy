using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Microsoft.Extensions.Logging;
using Tesseract;

namespace TarkovBuddy.Services
{
    /// <summary>
    /// Wrapper around Tesseract OCR engine for text extraction.
    /// Manages engine lifecycle, configuration, and text recognition operations.
    /// </summary>
    public class OcrEngine : IDisposable
    {
        private readonly ILogger _logger;
        private TesseractEngine? _engine;
        private bool _isDisposed;
        private string? _characterWhitelist;

        private const string TesseractDataPath = "./tessdata";
        private const string DefaultLanguage = "eng+rus";

        public OcrEngine(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _isDisposed = false;
        }

        /// <summary>
        /// Initializes the Tesseract engine.
        /// </summary>
        /// <returns>True if initialization succeeded, false otherwise.</returns>
        public bool Initialize()
        {
            try
            {
                if (_engine != null)
                {
                    _logger.LogWarning("OcrEngine already initialized");
                    return true;
                }

                // Ensure tessdata directory exists
                if (!Directory.Exists(TesseractDataPath))
                {
                    _logger.LogWarning($"Tessdata directory not found at {TesseractDataPath}. OCR may fail.");
                }

                // Initialize with English and Russian languages
                _engine = new TesseractEngine(TesseractDataPath, DefaultLanguage, EngineMode.Default);

                // Set page segmentation mode for better accuracy
                _engine.SetVariable("tessedit_pageseg_mode", "1"); // Automatic page segmentation

                _logger.LogInformation("OcrEngine initialized successfully with languages: {Languages}", DefaultLanguage);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize OcrEngine");
                return false;
            }
        }

        /// <summary>
        /// Extracts text from a bitmap image.
        /// </summary>
        /// <param name="bitmap">Image to process.</param>
        /// <returns>Extracted text or empty string if operation failed.</returns>
        public string ExtractText(Bitmap bitmap)
        {
            ThrowIfDisposed();

            if (_engine == null)
            {
                _logger.LogWarning("OcrEngine not initialized");
                return string.Empty;
            }

            if (bitmap == null)
            {
                _logger.LogWarning("Bitmap is null");
                return string.Empty;
            }

            try
            {
                // Save bitmap to temporary file and load as Pix
                var tempFile = Path.GetTempFileName();
                try
                {
                    bitmap.Save(tempFile, System.Drawing.Imaging.ImageFormat.Bmp);
                    
                    using (var pix = Pix.LoadFromFile(tempFile))
                    {
                        if (pix == null)
                        {
                            _logger.LogWarning("Failed to load image for OCR");
                            return string.Empty;
                        }

                        using (var page = _engine.Process(pix))
                        {
                            var text = page.GetText();
                            return text?.Trim() ?? string.Empty;
                        }
                    }
                }
                finally
                {
                    try
                    {
                        if (File.Exists(tempFile))
                            File.Delete(tempFile);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete temporary file");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from bitmap");
                return string.Empty;
            }
        }

        /// <summary>
        /// Extracts text with confidence scores from a bitmap.
        /// </summary>
        /// <param name="bitmap">Image to process.</param>
        /// <returns>Tuple of (text, confidence) where confidence is 0-1.</returns>
        public (string Text, double Confidence) ExtractTextWithConfidence(Bitmap bitmap)
        {
            ThrowIfDisposed();

            if (_engine == null)
            {
                return (string.Empty, 0.0);
            }

            if (bitmap == null)
            {
                _logger.LogWarning("Bitmap is null");
                return (string.Empty, 0.0);
            }

            try
            {
                // Save bitmap to temporary file and load as Pix
                var tempFile = Path.GetTempFileName();
                try
                {
                    bitmap.Save(tempFile, System.Drawing.Imaging.ImageFormat.Bmp);
                    
                    using (var pix = Pix.LoadFromFile(tempFile))
                    {
                        if (pix == null)
                        {
                            _logger.LogWarning("Failed to load image for OCR");
                            return (string.Empty, 0.0);
                        }

                        using (var page = _engine.Process(pix))
                        {
                            var text = page.GetText()?.Trim() ?? string.Empty;
                            var confidence = page.GetMeanConfidence() / 100.0; // Tesseract returns 0-100
                            return (text, Math.Clamp(confidence, 0.0, 1.0));
                        }
                    }
                }
                finally
                {
                    try
                    {
                        if (File.Exists(tempFile))
                            File.Delete(tempFile);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete temporary file");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text with confidence");
                return (string.Empty, 0.0);
            }
        }

        /// <summary>
        /// Sets character whitelist for OCR.
        /// </summary>
        /// <param name="whitelist">String containing allowed characters.</param>
        public void SetCharacterWhitelist(string whitelist)
        {
            ThrowIfDisposed();

            if (_engine == null)
            {
                _logger.LogWarning("OcrEngine not initialized");
                return;
            }

            try
            {
                _characterWhitelist = whitelist;
                _engine.SetVariable("tessedit_char_whitelist", whitelist);
                _logger.LogInformation("Character whitelist set to: {Whitelist}", whitelist);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting character whitelist");
            }
        }

        /// <summary>
        /// Resets character whitelist to allow all characters.
        /// </summary>
        public void ResetCharacterWhitelist()
        {
            ThrowIfDisposed();

            if (_engine == null)
            {
                return;
            }

            try
            {
                _characterWhitelist = null;
                _engine.SetVariable("tessedit_char_whitelist", "");
                _logger.LogInformation("Character whitelist reset");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting character whitelist");
            }
        }

        /// <summary>
        /// Checks if engine is initialized and ready.
        /// </summary>
        public bool IsInitialized => _engine != null && !_isDisposed;

        /// <summary>
        /// Gets the current character whitelist.
        /// </summary>
        public string? CurrentWhitelist => _characterWhitelist;

        /// <summary>
        /// Disposes the Tesseract engine and releases resources.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            try
            {
                _engine?.Dispose();
                _logger.LogInformation("OcrEngine disposed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing OcrEngine");
            }

            _isDisposed = true;
            GC.SuppressFinalize(this);
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(OcrEngine));
        }

        ~OcrEngine()
        {
            Dispose();
        }
    }
}