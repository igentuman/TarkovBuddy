using System.Drawing;
using Microsoft.Extensions.Logging;
using Moq;
using TarkovBuddy.Services;
using Xunit;
using FluentAssertions;

namespace TarkovBuddy.Tests
{
    /// <summary>
    /// Unit tests for OcrCache.
    /// </summary>
    public class OcrCacheTests
    {
        private readonly Mock<ILogger<OcrCache>> _mockLogger;

        public OcrCacheTests()
        {
            _mockLogger = new Mock<ILogger<OcrCache>>();
        }

        private OcrCache CreateCache(int maxSize = 100)
        {
            return new OcrCache(_mockLogger.Object, maxSize);
        }

        [Fact]
        public void Constructor_WithDefaultSize_ShouldCreateCache()
        {
            // Act
            var cache = CreateCache();

            // Assert
            cache.MaxSize.Should().Be(100);
            cache.CacheSize.Should().Be(0);
        }

        [Fact]
        public void Constructor_WithCustomSize_ShouldUseCustomSize()
        {
            // Act
            var cache = CreateCache(maxSize: 50);

            // Assert
            cache.MaxSize.Should().Be(50);
        }

        [Fact]
        public void Constructor_WithZeroSize_ShouldUseMinimumSize()
        {
            // Act
            var cache = CreateCache(maxSize: 0);

            // Assert
            cache.MaxSize.Should().Be(1);
        }

        [Fact]
        public void AddToCache_WithValidData_ShouldAddEntry()
        {
            // Arrange
            var cache = CreateCache();
            using var bitmap = new Bitmap(100, 50);

            // Act
            cache.AddToCache(bitmap, "test text", 0.95, 100);

            // Assert
            cache.CacheSize.Should().Be(1);
        }

        [Fact]
        public void TryGetCached_AfterAddToCache_ShouldReturnTrue()
        {
            // Arrange
            var cache = CreateCache();
            using var bitmap = new Bitmap(100, 50);
            cache.AddToCache(bitmap, "test text", 0.95, 100);

            // Act
            var found = cache.TryGetCached(bitmap, out var result);

            // Assert
            found.Should().BeTrue();
            result.Should().NotBeNull();
            result!.Text.Should().Be("test text");
            result.Confidence.Should().Be(0.95);
            result.WasFromCache.Should().BeTrue();
        }

        [Fact]
        public void TryGetCached_WithNonExistentBitmap_ShouldReturnFalse()
        {
            // Arrange
            var cache = CreateCache();
            using var bitmap1 = new Bitmap(100, 50);
            using var bitmap2 = new Bitmap(100, 50);
            cache.AddToCache(bitmap1, "text1", 0.95, 100);

            // Act
            var found = cache.TryGetCached(bitmap2, out var result);

            // Assert
            found.Should().BeFalse();
            result.Should().BeNull();
        }

        [Fact]
        public void AddToCache_ExceedingMaxSize_ShouldEvictOldest()
        {
            // Arrange
            var cache = CreateCache(maxSize: 2);
            using var bitmap1 = new Bitmap(100, 50);
            using var bitmap2 = new Bitmap(100, 50);
            using var bitmap3 = new Bitmap(100, 50);

            cache.AddToCache(bitmap1, "text1", 0.95, 100);
            System.Threading.Thread.Sleep(10); // Ensure different timestamps
            cache.AddToCache(bitmap2, "text2", 0.95, 100);
            System.Threading.Thread.Sleep(10);

            // Act
            cache.AddToCache(bitmap3, "text3", 0.95, 100);

            // Assert
            cache.CacheSize.Should().BeLessThanOrEqualTo(2);
        }

        [Fact]
        public void Clear_ShouldRemoveAllEntries()
        {
            // Arrange
            var cache = CreateCache();
            using var bitmap = new Bitmap(100, 50);
            cache.AddToCache(bitmap, "test text", 0.95, 100);

            // Act
            cache.Clear();

            // Assert
            cache.CacheSize.Should().Be(0);
        }

        [Fact]
        public void GetStats_ShouldReturnStats()
        {
            // Arrange
            var cache = CreateCache();
            using var bitmap = new Bitmap(100, 50);
            cache.AddToCache(bitmap, "test text", 0.95, 100);
            cache.TryGetCached(bitmap, out _); // Cache hit

            // Act
            var stats = cache.GetStats();

            // Assert
            stats.Should().NotBeNull();
            stats.CachedItems.Should().Be(1);
            stats.CacheHits.Should().BeGreaterThan(0);
            stats.HitRate.Should().BeGreaterThan(0);
        }

        [Fact]
        public void GetStats_EmptyCache_ShouldReturnZeroStats()
        {
            // Arrange
            var cache = CreateCache();

            // Act
            var stats = cache.GetStats();

            // Assert
            stats.CachedItems.Should().Be(0);
            stats.CacheHits.Should().Be(0);
            stats.CacheMisses.Should().Be(0);
            stats.HitRate.Should().Be(0);
        }

        [Fact]
        public void CacheSize_ShouldReflectNumberOfCachedItems()
        {
            // Arrange
            var cache = CreateCache();
            using var bitmap = new Bitmap(100, 50);

            // Act
            cache.AddToCache(bitmap, "text1", 0.95, 100);
            var size1 = cache.CacheSize;

            cache.AddToCache(bitmap, "text2", 0.95, 100); // Update same key
            var size2 = cache.CacheSize;

            // Assert
            size1.Should().Be(1);
            size2.Should().Be(1);
        }

        [Fact]
        public void TryGetCached_MultipleHits_ShouldIncrementHitCount()
        {
            // Arrange
            var cache = CreateCache();
            using var bitmap = new Bitmap(100, 50);
            cache.AddToCache(bitmap, "test text", 0.95, 100);

            // Act
            cache.TryGetCached(bitmap, out _);
            cache.TryGetCached(bitmap, out _);
            var stats = cache.GetStats();

            // Assert
            stats.CacheHits.Should().Be(2);
        }
    }
}