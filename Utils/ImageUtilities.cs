using System.Drawing;
using System.Drawing.Imaging;
using TarkovBuddy.Models;

namespace TarkovBuddy.Utils
{
    /// <summary>
    /// Utility functions for image processing and manipulation.
    /// Used for frame resizing, cropping, and format conversion.
    /// </summary>
    public static class ImageUtilities
    {
        /// <summary>
        /// Resizes a frame to a new size using bilinear interpolation.
        /// </summary>
        /// <param name="frame">Source frame to resize.</param>
        /// <param name="newWidth">New width in pixels.</param>
        /// <param name="newHeight">New height in pixels.</param>
        /// <returns>Resized frame with new dimensions.</returns>
        public static Frame ResizeFrame(Frame frame, int newWidth, int newHeight)
        {
            if (frame == null)
                throw new ArgumentNullException(nameof(frame));
            if (newWidth < 1 || newHeight < 1)
                throw new ArgumentException("Width and height must be at least 1");

            // If dimensions are the same, return a clone
            if (frame.Width == newWidth && frame.Height == newHeight)
                return frame.Clone();

            // Convert frame to bitmap
            using var sourceBitmap = FrameToBitmap(frame);
            using var resizedBitmap = new Bitmap(newWidth, newHeight, PixelFormat.Format32bppArgb);

            using (var graphics = Graphics.FromImage(resizedBitmap))
            {
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;
                graphics.DrawImage(sourceBitmap, 0, 0, newWidth, newHeight);
            }

            return BitmapToFrame(resizedBitmap);
        }

        /// <summary>
        /// Crops a frame to a rectangular region.
        /// </summary>
        /// <param name="frame">Source frame to crop.</param>
        /// <param name="x">Left edge of crop region.</param>
        /// <param name="y">Top edge of crop region.</param>
        /// <param name="width">Width of crop region.</param>
        /// <param name="height">Height of crop region.</param>
        /// <returns>Cropped frame.</returns>
        public static Frame CropFrame(Frame frame, int x, int y, int width, int height)
        {
            if (frame == null)
                throw new ArgumentNullException(nameof(frame));
            if (x < 0 || y < 0)
                throw new ArgumentException("X and Y must be non-negative");
            if (width < 1 || height < 1)
                throw new ArgumentException("Width and height must be at least 1");
            if (x + width > frame.Width || y + height > frame.Height)
                throw new ArgumentException("Crop region exceeds frame bounds");

            return frame.ExtractRegion(new Rectangle(x, y, width, height));
        }

        /// <summary>
        /// Converts a frame to a System.Drawing.Bitmap.
        /// Note: Caller is responsible for disposing the bitmap.
        /// </summary>
        /// <param name="frame">Frame to convert.</param>
        /// <returns>Bitmap representation of the frame.</returns>
        public static Bitmap FrameToBitmap(Frame frame)
        {
            if (frame == null)
                throw new ArgumentNullException(nameof(frame));
            if (frame.PixelData.Length == 0)
                throw new ArgumentException("Frame has no pixel data");

            var bitmap = new Bitmap(frame.Width, frame.Height, frame.Stride, 
                PixelFormat.Format32bppArgb, 
                System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(
                    frame.PixelData, 0));

            return bitmap;
        }

        /// <summary>
        /// Converts a System.Drawing.Bitmap to a Frame.
        /// </summary>
        /// <param name="bitmap">Bitmap to convert.</param>
        /// <returns>Frame representation of the bitmap.</returns>
        public static Frame BitmapToFrame(Bitmap bitmap)
        {
            if (bitmap == null)
                throw new ArgumentNullException(nameof(bitmap));

            var frame = new Frame(bitmap.Width, bitmap.Height);
            
            var bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

            try
            {
                System.Runtime.InteropServices.Marshal.Copy(
                    bitmapData.Scan0, frame.PixelData, 0, frame.PixelData.Length);
                frame.Stride = bitmapData.Stride;
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }

            return frame;
        }

        /// <summary>
        /// Saves a frame to disk as a PNG file.
        /// Useful for debugging and testing.
        /// </summary>
        /// <param name="frame">Frame to save.</param>
        /// <param name="filePath">Path where to save the PNG file.</param>
        public static void SaveFrameToPng(Frame frame, string filePath)
        {
            if (frame == null)
                throw new ArgumentNullException(nameof(frame));
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be empty", nameof(filePath));

            using var bitmap = FrameToBitmap(frame);
            bitmap.Save(filePath, ImageFormat.Png);
        }

        /// <summary>
        /// Calculates the average luminance (brightness) of a frame.
        /// Used for detecting light/dark scenes.
        /// </summary>
        /// <param name="frame">Frame to analyze.</param>
        /// <returns>Average luminance value (0-255).</returns>
        public static double CalculateLuminance(Frame frame)
        {
            if (frame == null)
                throw new ArgumentNullException(nameof(frame));
            if (frame.PixelData.Length == 0)
                return 0;

            long totalBrightness = 0;
            long pixelCount = 0;

            // Process every 4th pixel to sample (skip alpha channel, sample BGRA)
            for (int i = 0; i < frame.PixelData.Length; i += 4)
            {
                // BGRA format: B=i, G=i+1, R=i+2, A=i+3
                byte b = frame.PixelData[i];
                byte g = frame.PixelData[i + 1];
                byte r = frame.PixelData[i + 2];

                // Standard luminance formula: Y = 0.299R + 0.587G + 0.114B
                double luminance = (0.299 * r) + (0.587 * g) + (0.114 * b);
                totalBrightness += (long)luminance;
                pixelCount++;
            }

            return pixelCount > 0 ? totalBrightness / pixelCount : 0;
        }

        /// <summary>
        /// Converts a frame to grayscale.
        /// </summary>
        /// <param name="frame">Frame to convert.</param>
        /// <returns>Grayscale frame.</returns>
        public static Frame ToGrayscale(Frame frame)
        {
            if (frame == null)
                throw new ArgumentNullException(nameof(frame));

            var grayFrame = new Frame(frame.Width, frame.Height);
            grayFrame.Timestamp = frame.Timestamp;
            grayFrame.FrameNumber = frame.FrameNumber;

            for (int i = 0; i < frame.PixelData.Length; i += 4)
            {
                byte b = frame.PixelData[i];
                byte g = frame.PixelData[i + 1];
                byte r = frame.PixelData[i + 2];
                byte a = frame.PixelData[i + 3];

                // Calculate grayscale value
                byte gray = (byte)((0.299 * r) + (0.587 * g) + (0.114 * b));

                // Store as BGRA
                grayFrame.PixelData[i] = gray;
                grayFrame.PixelData[i + 1] = gray;
                grayFrame.PixelData[i + 2] = gray;
                grayFrame.PixelData[i + 3] = a;
            }

            return grayFrame;
        }

        /// <summary>
        /// Computes the difference between two frames (absolute difference).
        /// Returns a frame showing the delta between frames.
        /// </summary>
        /// <param name="frame1">First frame.</param>
        /// <param name="frame2">Second frame.</param>
        /// <returns>Difference frame.</returns>
        public static Frame ComputeDifference(Frame frame1, Frame frame2)
        {
            if (frame1 == null)
                throw new ArgumentNullException(nameof(frame1));
            if (frame2 == null)
                throw new ArgumentNullException(nameof(frame2));
            if (frame1.Width != frame2.Width || frame1.Height != frame2.Height)
                throw new ArgumentException("Frames must have same dimensions");

            var diffFrame = new Frame(frame1.Width, frame1.Height);
            diffFrame.Timestamp = DateTime.UtcNow;

            for (int i = 0; i < frame1.PixelData.Length; i++)
            {
                diffFrame.PixelData[i] = (byte)Math.Abs(
                    frame1.PixelData[i] - frame2.PixelData[i]);
            }

            return diffFrame;
        }
    }
}