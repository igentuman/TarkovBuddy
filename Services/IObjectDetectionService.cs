using TarkovBuddy.Models;

namespace TarkovBuddy.Services
{
    /// <summary>
    /// Service for detecting UI elements and objects in game frames using YOLO object detection.
    /// Provides GPU-accelerated detection targeting 60-120 FPS performance.
    /// </summary>
    public interface IObjectDetectionService : IService
    {
        /// <summary>
        /// Detects objects in the provided frame.
        /// Asynchronously performs inference on GPU.
        /// </summary>
        /// <param name="frame">The frame to analyze.</param>
        /// <param name="confidenceThreshold">Minimum confidence score (0-1) for detections to include.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>List of detected objects with bounding boxes and confidence scores.</returns>
        Task<List<DetectionResult>> DetectAsync(Frame frame, float confidenceThreshold = 0.5f, CancellationToken ct = default);

        /// <summary>
        /// Detects objects in a specific region of the frame for faster processing.
        /// </summary>
        /// <param name="frame">The frame to analyze.</param>
        /// <param name="region">Region to search within.</param>
        /// <param name="confidenceThreshold">Minimum confidence score (0-1).</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>List of detected objects within the region.</returns>
        Task<List<DetectionResult>> DetectInRegionAsync(Frame frame, System.Drawing.Rectangle region, float confidenceThreshold = 0.5f, CancellationToken ct = default);

        /// <summary>
        /// Gets the average inference time in milliseconds.
        /// </summary>
        double AverageInferenceTimeMs { get; }

        /// <summary>
        /// Gets the current inference FPS (frames per second).
        /// </summary>
        double InferenceFps { get; }

        /// <summary>
        /// Gets the ONNX model name currently loaded.
        /// </summary>
        string? LoadedModelName { get; }

        /// <summary>
        /// Gets whether GPU acceleration is available and active.
        /// </summary>
        bool IsGpuAccelerated { get; }

        /// <summary>
        /// Loads a specific ONNX model by name.
        /// </summary>
        /// <param name="modelName">Name of the model file (without path).</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>True if model loaded successfully, false otherwise.</returns>
        Task<bool> LoadModelAsync(string modelName, CancellationToken ct = default);

        /// <summary>
        /// Starts the detection service and initializes GPU resources.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops the detection service and releases GPU resources.
        /// </summary>
        void Stop();
    }

    /// <summary>
    /// Represents a single detected object in an image.
    /// </summary>
    public class DetectionResult
    {
        /// <summary>
        /// Object class label (e.g., "item_icon", "map_element", "extraction_button").
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Confidence score from 0 to 1.
        /// </summary>
        public float Confidence { get; set; }

        /// <summary>
        /// Bounding box of the detected object (X, Y, Width, Height).
        /// </summary>
        public System.Drawing.Rectangle BoundingBox { get; set; }

        /// <summary>
        /// Unique ID for tracking this detection across frames.
        /// </summary>
        public int TrackingId { get; set; }

        /// <summary>
        /// Additional metadata specific to the detection (e.g., item rarity).
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();

        /// <summary>
        /// Initializes a new DetectionResult.
        /// </summary>
        public DetectionResult(string label, float confidence, System.Drawing.Rectangle boundingBox)
        {
            Label = label;
            Confidence = confidence;
            BoundingBox = boundingBox;
        }

        /// <summary>
        /// Gets the center point of the bounding box.
        /// </summary>
        public System.Drawing.Point GetCenter() => new(BoundingBox.X + BoundingBox.Width / 2, BoundingBox.Y + BoundingBox.Height / 2);

        /// <summary>
        /// Returns string representation of the detection result.
        /// </summary>
        public override string ToString() =>
            $"Detection(Label={Label}, Confidence={Confidence:F2}, " +
            $"Box=[{BoundingBox.X},{BoundingBox.Y},{BoundingBox.Width}x{BoundingBox.Height}], " +
            $"TrackingId={TrackingId})";
    }
}