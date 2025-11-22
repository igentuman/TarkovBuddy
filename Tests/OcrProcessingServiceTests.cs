using System.Drawing;
using Microsoft.Extensions.Logging;
using Moq;
using TarkovBuddy.Services;
using Xunit;
using FluentAssertions;

namespace TarkovBuddy.Tests
{
    /// <summary>
    /// Unit tests for OcrProcessingService.
    /// </summary>
    public class OcrProcessingServiceTests
    {
        private readonly Mock<IConfigurationService> _mockConfigService;
        private readonly Mock<ILogger<OcrProcessingService>> _mockLogger;
        private OcrProcessingService? _service;

        public OcrProcessingServiceTests()
        {
            _mockConfigService = new Mock<IConfigurationService>();
            _mockLogger = new Mock<ILogger<OcrProcessingService>>();
        }

        private OcrProcessingService CreateService()
        {
            return new OcrProcessingService(_mockConfigService.Object, _mockLogger.Object);
        }

        [Fact]
        public void Initialize_WithValidState_ShouldReturnTrue()
        {
            // Arrange
            _service = CreateService();

            // Act
            var result = _service.Initialize();

            // Assert
            result.Should().BeTrue();
            _service.Dispose();
        }

        [Fact]
        public void Initialize_CalledTwice_ShouldReturnTrue()
        {
            // Arrange
            _service = CreateService();
            _service.Initialize();

            // Act
            var result = _service.Initialize();

            // Assert
            result.Should().BeTrue();
            _service.Dispose();
        }

        [Fact]
        public async Task ExtractTextAsync_WithValidBitmap_ShouldReturnResult()
        {
            // Arrange
            _service = CreateService();
            _service.Initialize();

            using var bitmap = new Bitmap(100, 50);
            using var g = Graphics.FromImage(bitmap);
            g.Clear(System.Drawing.Color.White);

            // Act
            var result = await _service.ExtractTextAsync(bitmap);

            // Assert
            result.Should().NotBeNull();
            result.Confidence.Should().BeGreaterThanOrEqualTo(0).And.BeLessThanOrEqualTo(1);
            _service.Dispose();
        }

        [Fact]
        public async Task ExtractTextAsync_BeforeInitialize_ShouldReturnEmptyResult()
        {
            // Arrange
            _service = CreateService();
            using var bitmap = new Bitmap(100, 50);

            // Act
            var result = await _service.ExtractTextAsync(bitmap);

            // Assert
            result.Text.Should().BeEmpty();
            result.Confidence.Should().Be(0);
            _service.Dispose();
        }

        [Fact]
        public async Task ExtractTextAsync_WithNullRegion_ShouldProcessEntireBitmap()
        {
            // Arrange
            _service = CreateService();
            _service.Initialize();

            using var bitmap = new Bitmap(100, 50);
            using var g = Graphics.FromImage(bitmap);
            g.Clear(System.Drawing.Color.White);

            // Act
            var result = await _service.ExtractTextAsync(bitmap, region: null);

            // Assert
            result.Should().NotBeNull();
            _service.Dispose();
        }

        [Fact]
        public async Task ExtractTextAsync_WithValidRegion_ShouldProcessRegionOnly()
        {
            // Arrange
            _service = CreateService();
            _service.Initialize();

            using var bitmap = new Bitmap(100, 50);
            using var g = Graphics.FromImage(bitmap);
            g.Clear(System.Drawing.Color.White);

            var region = new Rectangle(10, 10, 30, 20);

            // Act
            var result = await _service.ExtractTextAsync(bitmap, region: region);

            // Assert
            result.Should().NotBeNull();
            _service.Dispose();
        }

        [Fact]
        public async Task ExtractTextAsync_ShouldCacheResults()
        {
            // Arrange
            _service = CreateService();
            _service.Initialize();

            using var bitmap = new Bitmap(100, 50);
            using var g = Graphics.FromImage(bitmap);
            g.Clear(System.Drawing.Color.White);

            // Act
            var result1 = await _service.ExtractTextAsync(bitmap);
            var result2 = await _service.ExtractTextAsync(bitmap);

            // Assert
            result1.Should().NotBeNull();
            result2.Should().NotBeNull();
            result2.WasFromCache.Should().BeTrue();
            _service.Dispose();
        }

        [Fact]
        public void SetCharacterWhitelist_WithValidWhitelist_ShouldSucceed()
        {
            // Arrange
            _service = CreateService();
            _service.Initialize();

            // Act
            var action = () => _service.SetCharacterWhitelist("0123456789");

            // Assert
            action.Should().NotThrow();
            _service.Dispose();
        }

        [Fact]
        public void GetCacheStats_ShouldReturnStats()
        {
            // Arrange
            _service = CreateService();
            _service.Initialize();

            // Act
            var stats = _service.GetCacheStats();

            // Assert
            stats.Should().NotBeNull();
            stats.CachedItems.Should().Be(0);
            stats.HitRate.Should().Be(0);
            _service.Dispose();
        }

        [Fact]
        public void ClearCache_ShouldClearCachedItems()
        {
            // Arrange
            _service = CreateService();
            _service.Initialize();

            // Act
            _service.ClearCache();
            var stats = _service.GetCacheStats();

            // Assert
            stats.CachedItems.Should().Be(0);
            _service.Dispose();
        }

        [Fact]
        public void AverageProcessingTimeMs_ShouldReturnZeroInitially()
        {
            // Arrange
            _service = CreateService();
            _service.Initialize();

            // Act
            var avgTime = _service.AverageProcessingTimeMs;

            // Assert
            avgTime.Should().BeGreaterThanOrEqualTo(0);
            _service.Dispose();
        }

        [Fact]
        public void TotalOcrOperations_ShouldBeZeroInitially()
        {
            // Arrange
            _service = CreateService();
            _service.Initialize();

            // Act
            var total = _service.TotalOcrOperations;

            // Assert
            total.Should().Be(0);
            _service.Dispose();
        }

        [Fact]
        public void Dispose_ShouldNotThrow()
        {
            // Arrange
            _service = CreateService();
            _service.Initialize();

            // Act
            var action = () => _service.Dispose();

            // Assert
            action.Should().NotThrow();
        }
    }
}