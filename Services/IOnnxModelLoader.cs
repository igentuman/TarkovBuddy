using Microsoft.ML.OnnxRuntime;

namespace TarkovBuddy.Services
{
    /// <summary>
    /// Manages ONNX model loading, caching, and session management.
    /// Provides GPU acceleration through ONNX Runtime with CUDA support.
    /// </summary>
    public interface IOnnxModelLoader : IDisposable
    {
        /// <summary>
        /// Gets whether GPU acceleration (CUDA) is available.
        /// </summary>
        bool IsGpuAvailable { get; }

        /// <summary>
        /// Gets the name of the currently loaded model.
        /// </summary>
        string? ActiveModelName { get; }

        /// <summary>
        /// Gets the currently active inference session.
        /// </summary>
        InferenceSession? ActiveSession { get; }

        /// <summary>
        /// Loads an ONNX model from disk and caches it.
        /// Creates an inference session with GPU acceleration if available.
        /// </summary>
        /// <param name="modelName">Name of the model file (with or without .onnx extension).</param>
        /// <returns>The loaded inference session or null if load failed.</returns>
        InferenceSession? LoadModel(string modelName);

        /// <summary>
        /// Loads a model asynchronously.
        /// </summary>
        Task<InferenceSession?> LoadModelAsync(string modelName);

        /// <summary>
        /// Unloads a model and releases associated resources.
        /// </summary>
        void UnloadModel(string modelName);

        /// <summary>
        /// Gets information about available ONNX models in the models directory.
        /// </summary>
        List<OnnxModelInfo> GetAvailableModels();

        /// <summary>
        /// Clears all cached models from memory.
        /// </summary>
        void ClearCache();

        /// <summary>
        /// Gets ONNX Runtime information and available providers.
        /// </summary>
        OnnxRuntimeInfo GetRuntimeInfo();
    }
}