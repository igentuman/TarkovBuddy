using System.Drawing;
using TarkovBuddy.Models;

namespace TarkovBuddy.Services
{
    /// <summary>
    /// Captures screen frames from the game window at 30 FPS.
    /// Provides real-time pixel data for analysis by OCR and object detection services.
    /// </summary>
    public interface IScreenCaptureService : IService
    {
        /// <summary>
        /// Starts the screen capture worker thread.
        /// Must be called before GetFramesAsync().
        /// </summary>
        void Start();

        /// <summary>
        /// Stops the screen capture worker thread and releases resources.
        /// </summary>
        void Stop();

        /// <summary>
        /// Asynchronously yields frames from the capture buffer.
        /// Blocks until frames are available.
        /// </summary>
        /// <param name="ct">Cancellation token to stop frame enumeration.</param>
        /// <returns>Async enumerable of captured frames.</returns>
        IAsyncEnumerable<Frame> GetFramesAsync(CancellationToken ct = default);

        /// <summary>
        /// Captures a single screenshot immediately without buffering.
        /// </summary>
        /// <returns>Bitmap of the current screen or null if capture failed.</returns>
        Bitmap? CaptureScreenshot();

        /// <summary>
        /// Gets the current number of frames in the buffer.
        /// </summary>
        int BufferedFrameCount { get; }

        /// <summary>
        /// Gets the average FPS since capture started.
        /// </summary>
        double CurrentFps { get; }

        /// <summary>
        /// Gets whether the capture service is currently active.
        /// </summary>
        new bool IsRunning { get; }

        /// <summary>
        /// Raised when a new frame is captured.
        /// </summary>
        event EventHandler<FrameCapturedEventArgs> FrameCaptured;
    }

    /// <summary>
    /// Event arguments for frame capture events.
    /// </summary>
    public class FrameCapturedEventArgs : EventArgs
    {
        /// <summary>
        /// The captured frame.
        /// </summary>
        public Frame Frame { get; set; } = null!;

        /// <summary>
        /// Number of dropped frames (due to buffer overflow).
        /// </summary>
        public int DroppedFrames { get; set; }

        /// <summary>
        /// Time taken to capture this frame in milliseconds.
        /// </summary>
        public long CaptureTimeMs { get; set; }
    }
}