using System.Drawing;

namespace TarkovBuddy.Models
{
    /// <summary>
    /// Pixel format enumeration for frame data.
    /// </summary>
    public enum PixelFormatType
    {
        /// <summary>32-bit BGRA format (Blue, Green, Red, Alpha)</summary>
        Bgra32 = 0,

        /// <summary>32-bit ARGB format (Alpha, Red, Green, Blue)</summary>
        Argb32 = 1,

        /// <summary>24-bit BGR format (Blue, Green, Red)</summary>
        Bgr24 = 2
    }

    /// <summary>
    /// Represents a single captured frame from the game screen.
    /// Contains pixel data and metadata about the capture.
    /// </summary>
    public class Frame : IDisposable
    {
        /// <summary>
        /// Raw pixel data in BGRA32 format (4 bytes per pixel).
        /// </summary>
        public byte[] PixelData { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Width of the frame in pixels.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Height of the frame in pixels.
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// Number of bytes per row (stride) in the pixel data.
        /// May be larger than Width*4 due to alignment.
        /// </summary>
        public int Stride { get; set; }

        /// <summary>
        /// Timestamp when the frame was captured.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Sequential frame number since capture started.
        /// Used for ordering and tracking.
        /// </summary>
        public long FrameNumber { get; set; }

        /// <summary>
        /// Pixel format of the frame (typically BGRA32).
        /// </summary>
        public PixelFormatType Format { get; set; } = PixelFormatType.Bgra32;

        /// <summary>
        /// Initializes a new instance of the Frame class.
        /// </summary>
        public Frame()
        {
            Timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// Initializes a new instance of the Frame class with specific dimensions.
        /// </summary>
        public Frame(int width, int height, int stride = 0)
        {
            Width = width;
            Height = height;
            Stride = stride > 0 ? stride : width * 4;
            PixelData = new byte[Stride * height];
            Timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// Creates a deep copy of this frame.
        /// </summary>
        public Frame Clone()
        {
            var clone = new Frame
            {
                Width = Width,
                Height = Height,
                Stride = Stride,
                Timestamp = Timestamp,
                FrameNumber = FrameNumber,
                Format = Format
            };

            clone.PixelData = new byte[PixelData.Length];
            Array.Copy(PixelData, clone.PixelData, PixelData.Length);

            return clone;
        }

        /// <summary>
        /// Gets the size of the frame in bytes.
        /// </summary>
        public long SizeInBytes => PixelData.LongLength;

        /// <summary>
        /// Gets the total number of pixels in the frame.
        /// </summary>
        public long TotalPixels => (long)Width * Height;

        /// <summary>
        /// Extracts a rectangular region from the frame as a new Frame.
        /// </summary>
        public Frame ExtractRegion(Rectangle region)
        {
            if (region.X < 0 || region.Y < 0 || 
                region.Right > Width || region.Bottom > Height)
            {
                throw new ArgumentException("Region is outside frame bounds", nameof(region));
            }

            var regionFrame = new Frame(region.Width, region.Height);
            regionFrame.Timestamp = Timestamp;
            regionFrame.FrameNumber = FrameNumber;

            // Copy pixel data for the region
            for (int y = 0; y < region.Height; y++)
            {
                int srcOffset = ((region.Y + y) * Stride) + (region.X * 4);
                int dstOffset = y * regionFrame.Stride;
                Array.Copy(PixelData, srcOffset, regionFrame.PixelData, dstOffset, region.Width * 4);
            }

            return regionFrame;
        }

        public void Dispose()
        {
            // Pixel data will be garbage collected
            GC.SuppressFinalize(this);
        }
    }
}