using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using Microsoft.Extensions.Logging;

namespace TarkovBuddy.Services
{
    /// <summary>
    /// Manages ONNX model loading, caching, and session management.
    /// Provides GPU acceleration through ONNX Runtime with CUDA support.
    /// </summary>
    public class OnnxModelLoader : IOnnxModelLoader
    {
        private readonly ILogger<OnnxModelLoader> _logger;
        private readonly string _modelsDirectory;
        private Dictionary<string, InferenceSession> _sessions = new();
        private Dictionary<string, byte[]> _modelCache = new();
        private InferenceSession? _activeSession;
        private string? _activeModelName;
        private bool _isGpuAvailable;
        private const string ModelExtension = ".onnx";

        /// <summary>
        /// Initializes a new instance of OnnxModelLoader.
        /// </summary>
        public OnnxModelLoader(ILogger<OnnxModelLoader> logger, string modelsDirectory = "Models/Onnx")
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _modelsDirectory = modelsDirectory;
            DetectGpuAvailability();
        }

        /// <summary>
        /// Gets whether GPU acceleration (CUDA) is available.
        /// </summary>
        public bool IsGpuAvailable => _isGpuAvailable;

        /// <summary>
        /// Gets the name of the currently loaded model.
        /// </summary>
        public string? ActiveModelName => _activeModelName;

        /// <summary>
        /// Gets the currently active inference session.
        /// </summary>
        public InferenceSession? ActiveSession => _activeSession;

        /// <summary>
        /// Loads an ONNX model from disk and caches it.
        /// Creates an inference session with GPU acceleration if available.
        /// </summary>
        /// <param name="modelName">Name of the model file (with or without .onnx extension).</param>
        /// <returns>The loaded inference session or null if load failed.</returns>
        public InferenceSession? LoadModel(string modelName)
        {
            try
            {
                // Normalize model name
                if (!modelName.EndsWith(ModelExtension))
                    modelName = modelName + ModelExtension;

                // Check if already loaded
                if (_sessions.TryGetValue(modelName, out var cachedSession))
                {
                    _logger.LogInformation($"Using cached ONNX model: {modelName}");
                    _activeSession = cachedSession;
                    _activeModelName = modelName;
                    return cachedSession;
                }

                // Load model bytes from disk
                var modelPath = Path.Combine(_modelsDirectory, modelName);
                if (!File.Exists(modelPath))
                {
                    _logger.LogError($"ONNX model file not found: {modelPath}");
                    return null;
                }

                _logger.LogInformation($"Loading ONNX model: {modelPath}");
                var modelBytes = File.ReadAllBytes(modelPath);

                // Create inference session with GPU/CPU providers
                var session = CreateInferenceSession(modelBytes);
                if (session == null)
                {
                    _logger.LogError($"Failed to create inference session for {modelName}");
                    return null;
                }

                // Cache the session and bytes
                _sessions[modelName] = session;
                _modelCache[modelName] = modelBytes;
                _activeSession = session;
                _activeModelName = modelName;

                _logger.LogInformation($"Successfully loaded ONNX model: {modelName} (GPU: {IsGpuAvailable})");
                return session;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to load ONNX model: {modelName}");
                return null;
            }
        }

        /// <summary>
        /// Loads a model asynchronously.
        /// </summary>
        public async Task<InferenceSession?> LoadModelAsync(string modelName)
        {
            return await Task.Run(() => LoadModel(modelName));
        }

        /// <summary>
        /// Unloads a model and releases associated resources.
        /// </summary>
        public void UnloadModel(string modelName)
        {
            try
            {
                if (!modelName.EndsWith(ModelExtension))
                    modelName = modelName + ModelExtension;

                if (_sessions.TryGetValue(modelName, out var session))
                {
                    session?.Dispose();
                    _sessions.Remove(modelName);
                    _modelCache.Remove(modelName);
                    _logger.LogInformation($"Unloaded ONNX model: {modelName}");

                    if (_activeModelName == modelName)
                    {
                        _activeSession = null;
                        _activeModelName = null;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error unloading model: {modelName}");
            }
        }

        /// <summary>
        /// Gets information about available ONNX models in the models directory.
        /// </summary>
        public List<OnnxModelInfo> GetAvailableModels()
        {
            var models = new List<OnnxModelInfo>();

            try
            {
                if (!Directory.Exists(_modelsDirectory))
                {
                    _logger.LogWarning($"Models directory not found: {_modelsDirectory}");
                    return models;
                }

                var modelFiles = Directory.GetFiles(_modelsDirectory, $"*{ModelExtension}");
                foreach (var file in modelFiles)
                {
                    var fileName = Path.GetFileName(file);
                    var fileInfo = new FileInfo(file);
                    models.Add(new OnnxModelInfo
                    {
                        Name = fileName,
                        Path = file,
                        SizeBytes = fileInfo.Length,
                        IsLoaded = _sessions.ContainsKey(fileName),
                        IsCached = _modelCache.ContainsKey(fileName)
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scanning models directory");
            }

            return models;
        }

        /// <summary>
        /// Clears all cached models from memory.
        /// </summary>
        public void ClearCache()
        {
            _modelCache.Clear();
            _logger.LogInformation("Cleared ONNX model cache");
        }

        /// <summary>
        /// Gets ONNX Runtime information and available providers.
        /// </summary>
        public OnnxRuntimeInfo GetRuntimeInfo()
        {
            try
            {
                var providers = GetAvailableProviders();
                return new OnnxRuntimeInfo
                {
                    RuntimeVersion = "1.23.2", // ONNX Runtime version from package
                    AvailableProviders = providers,
                    GpuAvailable = IsGpuAvailable,
                    LoadedModelCount = _sessions.Count,
                    CachedModelCount = _modelCache.Count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get ONNX runtime info");
                return new OnnxRuntimeInfo();
            }
        }

        /// <summary>
        /// Creates an inference session with appropriate execution providers.
        /// Prioritizes GPU (CUDA/TensorRT) over CPU.
        /// </summary>
        private InferenceSession? CreateInferenceSession(byte[] modelBytes)
        {
            try
            {
                var sessionOptions = new SessionOptions();

                // Set log level for ONNX Runtime
                sessionOptions.LogSeverityLevel = OrtLoggingLevel.ORT_LOGGING_LEVEL_WARNING;

                // Add execution providers in order of preference
                if (_isGpuAvailable)
                {
                    // Try CUDA first (GPU acceleration)
                    try
                    {
                        sessionOptions.AppendExecutionProvider_CUDA(0);
                        _logger.LogInformation("CUDA execution provider enabled");
                    }
                    catch
                    {
                        _logger.LogWarning("CUDA provider not available, falling back to CPU");
                    }
                }

                // Always append CPU provider as fallback
                sessionOptions.AppendExecutionProvider_CPU();

                // Create session from model bytes
                var session = new InferenceSession(modelBytes, sessionOptions);

                return session;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ONNX inference session");
                return null;
            }
        }

        /// <summary>
        /// Detects available GPU and ONNX Runtime capabilities.
        /// </summary>
        private void DetectGpuAvailability()
        {
            try
            {
                var providers = GetAvailableProviders();
                _isGpuAvailable = providers.Contains("CUDAExecutionProvider") || providers.Contains("TensorrtExecutionProvider");
                _logger.LogInformation($"GPU Available: {_isGpuAvailable}, Providers: {string.Join(", ", providers)}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not detect GPU availability");
                _isGpuAvailable = false;
            }
        }

        /// <summary>
        /// Gets list of available execution providers in ONNX Runtime.
        /// </summary>
        private List<string> GetAvailableProviders()
        {
            try
            {
                // Use SessionOptions to detect available providers
                var sessionOptions = new SessionOptions();
                var providers = new List<string>();

                // Check for CUDA provider
                try
                {
                    sessionOptions.AppendExecutionProvider_CUDA();
                    providers.Add("CUDAExecutionProvider");
                }
                catch { }

                // Check for TensorRT provider
                try
                {
                    sessionOptions.AppendExecutionProvider_Tensorrt();
                    providers.Add("TensorrtExecutionProvider");
                }
                catch { }

                // CPU is always available
                try
                {
                    sessionOptions.AppendExecutionProvider_CPU();
                    providers.Add("CPUExecutionProvider");
                }
                catch { }

                return providers.Count > 0 ? providers : new List<string> { "CPUExecutionProvider" };
            }
            catch
            {
                return new List<string> { "CPUExecutionProvider" };
            }
        }

        /// <summary>
        /// Disposes all loaded sessions and clears cache.
        /// </summary>
        public void Dispose()
        {
            try
            {
                foreach (var session in _sessions.Values)
                {
                    session?.Dispose();
                }
                _sessions.Clear();
                _modelCache.Clear();
                _activeSession = null;
                _logger.LogInformation("OnnxModelLoader disposed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing OnnxModelLoader");
            }

            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Information about an available ONNX model.
    /// </summary>
    public class OnnxModelInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public bool IsLoaded { get; set; }
        public bool IsCached { get; set; }
    }

    /// <summary>
    /// Runtime information about ONNX Runtime and available capabilities.
    /// </summary>
    public class OnnxRuntimeInfo
    {
        public string RuntimeVersion { get; set; } = "Unknown";
        public List<string> AvailableProviders { get; set; } = new();
        public bool GpuAvailable { get; set; }
        public int LoadedModelCount { get; set; }
        public int CachedModelCount { get; set; }
    }
}