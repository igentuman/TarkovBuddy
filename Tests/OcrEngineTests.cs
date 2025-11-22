using System.Drawing;
using Microsoft.Extensions.Logging;
using Moq;
using TarkovBuddy.Services;
using Xunit;
using FluentAssertions;

namespace TarkovBuddy.Tests
{
    /// <summary>
    /// Unit tests for OcrEngine.
    /// </summary>
    public class OcrEngineTests
    {
        private readonly Mock<ILogger<OcrEngine>> _mockLogger;

        public OcrEngineTests()
        {
            _mockLogger = new Mock<ILogger<OcrEngine>>();
        }

        private OcrEngine CreateEngine()
        {
            return new OcrEngine(_mockLogger.Object);
        }

        [Fact]
        public void Initialize_ShouldSucceed()
        {
            // Arrange
            var engine = CreateEngine();

            // Act
            var result = engine.Initialize();

            // Assert
            result.Should().BeTrue();
            engine.IsInitialized.Should().BeTrue();
            engine.Dispose();
        }

        [Fact]
        public void Initialize_CalledTwice_ShouldNotReinitialize()
        {
            // Arrange
            var engine = CreateEngine();
            engine.Initialize();

            // Act
            var result = engine.Initialize();

            // Assert
            result.Should().BeTrue();
            engine.Dispose();
        }

        [Fact]
        public void ExtractText_BeforeInitialize_ShouldReturnEmptyString()
        {
            // Arrange
            var engine = CreateEngine();
            using var bitmap = new Bitmap(100, 50);

            // Act
            var result = engine.ExtractText(bitmap);

            // Assert
            result.Should().BeEmpty();
            engine.Dispose();
        }

        [Fact]
        public void ExtractText_WithValidBitmap_ShouldReturnString()
        {
            // Arrange
            var engine = CreateEngine();
            engine.Initialize();

            using var bitmap = new Bitmap(100, 50);
            using var g = Graphics.FromImage(bitmap);
            g.Clear(System.Drawing.Color.White);

            // Act
            var result = engine.ExtractText(bitmap);

            // Assert
            result.Should().BeOfType<string>();
            engine.Dispose();
        }

        [Fact]
        public void ExtractTextWithConfidence_ShouldReturnTuple()
        {
            // Arrange
            var engine = CreateEngine();
            engine.Initialize();

            using var bitmap = new Bitmap(100, 50);
            using var g = Graphics.FromImage(bitmap);
            g.Clear(System.Drawing.Color.White);

            // Act
            var (text, confidence) = engine.ExtractTextWithConfidence(bitmap);

            // Assert
            text.Should().BeOfType<string>();
            confidence.Should().BeGreaterThanOrEqualTo(0).And.BeLessThanOrEqualTo(1);
            engine.Dispose();
        }

        [Fact]
        public void SetCharacterWhitelist_ShouldSucceed()
        {
            // Arrange
            var engine = CreateEngine();
            engine.Initialize();

            // Act
            var action = () => engine.SetCharacterWhitelist("ABC123");

            // Assert
            action.Should().NotThrow();
            engine.CurrentWhitelist.Should().Be("ABC123");
            engine.Dispose();
        }

        [Fact]
        public void ResetCharacterWhitelist_ShouldClearWhitelist()
        {
            // Arrange
            var engine = CreateEngine();
            engine.Initialize();
            engine.SetCharacterWhitelist("ABC");

            // Act
            engine.ResetCharacterWhitelist();

            // Assert
            engine.CurrentWhitelist.Should().BeNull();
            engine.Dispose();
        }

        [Fact]
        public void SetCharacterWhitelist_BeforeInitialize_ShouldLogWarning()
        {
            // Arrange
            var engine = CreateEngine();

            // Act
            engine.SetCharacterWhitelist("ABC");

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
            engine.Dispose();
        }

        [Fact]
        public void Dispose_ShouldNotThrow()
        {
            // Arrange
            var engine = CreateEngine();

            // Act
            var action = () => engine.Dispose();

            // Assert
            action.Should().NotThrow();
        }

        [Fact]
        public void Dispose_CalledTwice_ShouldNotThrow()
        {
            // Arrange
            var engine = CreateEngine();
            engine.Initialize();
            engine.Dispose();

            // Act
            var action = () => engine.Dispose();

            // Assert
            action.Should().NotThrow();
        }
    }
}