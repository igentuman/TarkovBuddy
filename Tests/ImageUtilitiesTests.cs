using System;
using System.Drawing;
using System.IO;
using TarkovBuddy.Models;
using TarkovBuddy.Utils;
using Xunit;
using FluentAssertions;

namespace TarkovBuddy.Tests
{
    /// <summary>
    /// Unit tests for ImageUtilities.
    /// Tests image manipulation, conversion, and processing functions.
    /// </summary>
    public class ImageUtilitiesTests
    {
        private static Frame CreateTestFrame(int width = 800, int height = 600, bool fillData = true)
        {
            var frame = new Frame(width, height);
            if (fillData && frame.PixelData.Length > 0)
            {
                // Fill with test pattern (white pixels)
                for (int i = 0; i < frame.PixelData.Length; i += 4)
                {
                    frame.PixelData[i] = 255;     // B
                    frame.PixelData[i + 1] = 255; // G
                    frame.PixelData[i + 2] = 255; // R
                    frame.PixelData[i + 3] = 255; // A
                }
            }
            return frame;
        }

        [Fact]
        public void ResizeFrame_WithValidDimensions_Succeeds()
        {
            // Arrange
            var frame = CreateTestFrame(800, 600);

            // Act
            var resized = ImageUtilities.ResizeFrame(frame, 400, 300);

            // Assert
            resized.Should().NotBeNull();
            resized.Width.Should().Be(400);
            resized.Height.Should().Be(300);
            resized.PixelData.Should().NotBeEmpty();
        }

        [Fact]
        public void ResizeFrame_WithSameDimensions_ReturnsClone()
        {
            // Arrange
            var frame = CreateTestFrame(800, 600);

            // Act
            var resized = ImageUtilities.ResizeFrame(frame, 800, 600);

            // Assert
            resized.Should().NotBeNull();
            resized.Width.Should().Be(800);
            resized.Height.Should().Be(600);
        }

        [Fact]
        public void ResizeFrame_WithInvalidWidth_ThrowsArgumentException()
        {
            // Arrange
            var frame = CreateTestFrame();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                ImageUtilities.ResizeFrame(frame, 0, 300));
        }

        [Fact]
        public void ResizeFrame_WithNullFrame_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                ImageUtilities.ResizeFrame(null!, 400, 300));
        }

        [Fact]
        public void CropFrame_WithValidRegion_Succeeds()
        {
            // Arrange
            var frame = CreateTestFrame(800, 600);

            // Act
            var cropped = ImageUtilities.CropFrame(frame, 100, 100, 200, 150);

            // Assert
            cropped.Should().NotBeNull();
            cropped.Width.Should().Be(200);
            cropped.Height.Should().Be(150);
        }

        [Fact]
        public void CropFrame_WithOutOfBoundsRegion_ThrowsArgumentException()
        {
            // Arrange
            var frame = CreateTestFrame(800, 600);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                ImageUtilities.CropFrame(frame, 600, 400, 300, 300));
        }

        [Fact]
        public void CropFrame_WithNegativeCoordinates_ThrowsArgumentException()
        {
            // Arrange
            var frame = CreateTestFrame();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                ImageUtilities.CropFrame(frame, -10, 100, 200, 150));
        }

        [Fact]
        public void FrameToBitmap_CreatesValidBitmap()
        {
            // Arrange
            var frame = CreateTestFrame(400, 300);

            // Act
            using var bitmap = ImageUtilities.FrameToBitmap(frame);

            // Assert
            bitmap.Should().NotBeNull();
            bitmap.Width.Should().Be(400);
            bitmap.Height.Should().Be(300);
        }

        [Fact]
        public void FrameToBitmap_WithNullFrame_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                ImageUtilities.FrameToBitmap(null!));
        }

        [Fact]
        public void FrameToBitmap_WithEmptyPixelData_ThrowsArgumentException()
        {
            // Arrange
            var frame = new Frame(0, 0);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                ImageUtilities.FrameToBitmap(frame));
        }

        [Fact]
        public void BitmapToFrame_CreatesValidFrame()
        {
            // Arrange
            using var bitmap = new Bitmap(400, 300);

            // Act
            var frame = ImageUtilities.BitmapToFrame(bitmap);

            // Assert
            frame.Should().NotBeNull();
            frame.Width.Should().Be(400);
            frame.Height.Should().Be(300);
            frame.PixelData.Should().NotBeEmpty();
        }

        [Fact]
        public void BitmapToFrame_WithNullBitmap_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                ImageUtilities.BitmapToFrame(null!));
        }

        [Fact]
        public void SaveFrameToPng_CreatesFile()
        {
            // Arrange
            var frame = CreateTestFrame(100, 100);
            var tempFile = Path.GetTempFileName();
            tempFile = Path.ChangeExtension(tempFile, ".png");

            try
            {
                // Act
                ImageUtilities.SaveFrameToPng(frame, tempFile);

                // Assert
                File.Exists(tempFile).Should().BeTrue();
                new FileInfo(tempFile).Length.Should().BeGreaterThan(0);
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        [Fact]
        public void SaveFrameToPng_WithNullFrame_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                ImageUtilities.SaveFrameToPng(null!, "test.png"));
        }

        [Fact]
        public void SaveFrameToPng_WithEmptyPath_ThrowsArgumentException()
        {
            // Arrange
            var frame = CreateTestFrame();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                ImageUtilities.SaveFrameToPng(frame, ""));
        }

        [Fact]
        public void CalculateLuminance_ReturnsValidValue()
        {
            // Arrange
            var frame = CreateTestFrame(100, 100);

            // Act
            var luminance = ImageUtilities.CalculateLuminance(frame);

            // Assert
            luminance.Should().BeGreaterThanOrEqualTo(0);
            luminance.Should().BeLessThanOrEqualTo(255);
        }

        [Fact]
        public void CalculateLuminance_WithBrightFrame_ReturnsHighValue()
        {
            // Arrange
            var frame = CreateTestFrame(100, 100);
            // Frame is already filled with white pixels (255, 255, 255, 255)

            // Act
            var luminance = ImageUtilities.CalculateLuminance(frame);

            // Assert
            luminance.Should().BeGreaterThan(200);
        }

        [Fact]
        public void CalculateLuminance_WithDarkFrame_ReturnsLowValue()
        {
            // Arrange
            var frame = new Frame(100, 100);
            // Fill with black pixels
            for (int i = 0; i < frame.PixelData.Length; i += 4)
            {
                frame.PixelData[i] = 0;     // B
                frame.PixelData[i + 1] = 0; // G
                frame.PixelData[i + 2] = 0; // R
                frame.PixelData[i + 3] = 255; // A
            }

            // Act
            var luminance = ImageUtilities.CalculateLuminance(frame);

            // Assert
            luminance.Should().BeLessThan(10);
        }

        [Fact]
        public void CalculateLuminance_WithNullFrame_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                ImageUtilities.CalculateLuminance(null!));
        }

        [Fact]
        public void ToGrayscale_CreatesGrayscaleFrame()
        {
            // Arrange
            var frame = CreateTestFrame(100, 100);

            // Act
            var grayFrame = ImageUtilities.ToGrayscale(frame);

            // Assert
            grayFrame.Should().NotBeNull();
            grayFrame.Width.Should().Be(100);
            grayFrame.Height.Should().Be(100);
            grayFrame.PixelData.Should().NotBeEmpty();
        }

        [Fact]
        public void ToGrayscale_WithColoredFrame_ProducesCorrectValues()
        {
            // Arrange
            var frame = new Frame(2, 2);
            // Create test pattern with known RGB values
            for (int i = 0; i < frame.PixelData.Length; i += 4)
            {
                frame.PixelData[i] = 100;     // B
                frame.PixelData[i + 1] = 150; // G
                frame.PixelData[i + 2] = 200; // R
                frame.PixelData[i + 3] = 255; // A
            }

            // Act
            var grayFrame = ImageUtilities.ToGrayscale(frame);

            // Assert - All pixels should have same R, G, B values (grayscale)
            for (int i = 0; i < grayFrame.PixelData.Length; i += 4)
            {
                byte b = grayFrame.PixelData[i];
                byte g = grayFrame.PixelData[i + 1];
                byte r = grayFrame.PixelData[i + 2];

                b.Should().Be(g);
                g.Should().Be(r);
            }
        }

        [Fact]
        public void ToGrayscale_WithNullFrame_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                ImageUtilities.ToGrayscale(null!));
        }

        [Fact]
        public void ComputeDifference_WithIdenticalFrames_ReturnsAllZeros()
        {
            // Arrange
            var frame1 = CreateTestFrame(100, 100);
            var frame2 = CreateTestFrame(100, 100);

            // Act
            var diffFrame = ImageUtilities.ComputeDifference(frame1, frame2);

            // Assert
            diffFrame.PixelData.Should().AllSatisfy(b => b.Should().Be(0));
        }

        [Fact]
        public void ComputeDifference_WithDifferentFrames_ReturnsNonZero()
        {
            // Arrange
            var frame1 = CreateTestFrame(100, 100);
            var frame2 = new Frame(100, 100);
            // Frame2 filled with different pixel values
            for (int i = 0; i < frame2.PixelData.Length; i++)
                frame2.PixelData[i] = 100;

            // Act
            var diffFrame = ImageUtilities.ComputeDifference(frame1, frame2);

            // Assert
            diffFrame.PixelData.Should().Contain(b => b > 0);
        }

        [Fact]
        public void ComputeDifference_WithDifferentDimensions_ThrowsArgumentException()
        {
            // Arrange
            var frame1 = CreateTestFrame(100, 100);
            var frame2 = CreateTestFrame(200, 200);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                ImageUtilities.ComputeDifference(frame1, frame2));
        }

        [Fact]
        public void ComputeDifference_WithNullFrame_ThrowsArgumentNullException()
        {
            // Arrange
            var frame = CreateTestFrame();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                ImageUtilities.ComputeDifference(null!, frame));
            Assert.Throws<ArgumentNullException>(() => 
                ImageUtilities.ComputeDifference(frame, null!));
        }
    }
}