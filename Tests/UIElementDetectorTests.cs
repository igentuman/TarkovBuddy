using Moq;
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using TarkovBuddy.Services;
using TarkovBuddy.Models;
using System.Drawing;

namespace TarkovBuddy.Tests
{
    public class UIElementDetectorTests
    {
        private readonly Mock<IObjectDetectionService> _mockDetectionService;
        private readonly Mock<ILogger<UIElementDetector>> _mockLogger;
        private readonly UIElementDetector _detector;

        public UIElementDetectorTests()
        {
            _mockDetectionService = new Mock<IObjectDetectionService>();
            _mockLogger = new Mock<ILogger<UIElementDetector>>();

            _detector = new UIElementDetector(_mockDetectionService.Object, _mockLogger.Object);
        }

        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenDetectionServiceIsNull()
        {
            var act = () => new UIElementDetector(null!, _mockLogger.Object);
            act.Should().Throw<ArgumentNullException>().WithParameterName("detectionService");
        }

        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
        {
            var act = () => new UIElementDetector(_mockDetectionService.Object, null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
        }

        [Fact]
        public async Task DetectUIElementsAsync_ReturnsEmptyResult_WhenNoDetections()
        {
            _mockDetectionService.Setup(x => x.DetectAsync(It.IsAny<Frame>(), It.IsAny<float>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<DetectionResult>());

            var frame = new Frame(640, 480) { FrameNumber = 1 };
            var result = await _detector.DetectUIElementsAsync(frame);

            result.Should().NotBeNull();
            result.TotalDetections.Should().Be(0);
            result.TotalUIElements.Should().Be(0);
        }

        [Fact]
        public async Task DetectUIElementsAsync_CategorizesItemDetections()
        {
            var detections = new List<DetectionResult>
            {
                new DetectionResult("item_icon_rare", 0.95f, new Rectangle(10, 10, 50, 50)),
                new DetectionResult("item_icon_common", 0.85f, new Rectangle(70, 10, 50, 50))
            };

            _mockDetectionService.Setup(x => x.DetectAsync(It.IsAny<Frame>(), It.IsAny<float>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(detections);

            var frame = new Frame(640, 480) { FrameNumber = 1 };
            var result = await _detector.DetectUIElementsAsync(frame);

            result.ItemIcons.Should().HaveCount(2);
        }

        [Fact]
        public async Task DetectUIElementsAsync_CategorizesExtractionDetections()
        {
            var detections = new List<DetectionResult>
            {
                new DetectionResult("extraction_button_active", 0.9f, new Rectangle(500, 450, 100, 50))
            };

            _mockDetectionService.Setup(x => x.DetectAsync(It.IsAny<Frame>(), It.IsAny<float>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(detections);

            var frame = new Frame(640, 480) { FrameNumber = 1 };
            var result = await _detector.DetectUIElementsAsync(frame);

            result.ExtractionPoints.Should().HaveCount(1);
        }

        [Fact]
        public async Task DetectUIElementsAsync_CategorizesMapDetections()
        {
            var detections = new List<DetectionResult>
            {
                new DetectionResult("map_customs", 0.88f, new Rectangle(100, 100, 300, 300))
            };

            _mockDetectionService.Setup(x => x.DetectAsync(It.IsAny<Frame>(), It.IsAny<float>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(detections);

            var frame = new Frame(640, 480) { FrameNumber = 1 };
            var result = await _detector.DetectUIElementsAsync(frame);

            result.MapElements.Should().HaveCount(1);
        }

        [Fact]
        public async Task DetectUIElementsAsync_CategorizesStatusBarDetections()
        {
            var detections = new List<DetectionResult>
            {
                new DetectionResult("health_bar", 0.92f, new Rectangle(50, 450, 300, 20)),
                new DetectionResult("energy_bar", 0.92f, new Rectangle(50, 475, 300, 20))
            };

            _mockDetectionService.Setup(x => x.DetectAsync(It.IsAny<Frame>(), It.IsAny<float>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(detections);

            var frame = new Frame(640, 480) { FrameNumber = 1 };
            var result = await _detector.DetectUIElementsAsync(frame);

            result.StatusBars.Should().HaveCount(2);
        }

        [Fact]
        public async Task DetectUIElementsAsync_CategorizesButtonDetections()
        {
            var detections = new List<DetectionResult>
            {
                new DetectionResult("button_confirm", 0.91f, new Rectangle(300, 400, 100, 50))
            };

            _mockDetectionService.Setup(x => x.DetectAsync(It.IsAny<Frame>(), It.IsAny<float>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(detections);

            var frame = new Frame(640, 480) { FrameNumber = 1 };
            var result = await _detector.DetectUIElementsAsync(frame);

            result.Buttons.Should().HaveCount(1);
        }

        [Fact]
        public async Task DetectUIElementsAsync_CachesResults()
        {
            var detections = new List<DetectionResult>
            {
                new DetectionResult("item_icon", 0.95f, new Rectangle(10, 10, 50, 50))
            };

            _mockDetectionService.Setup(x => x.DetectAsync(It.IsAny<Frame>(), It.IsAny<float>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(detections);

            var frame = new Frame(640, 480) { FrameNumber = 1 };
            
            // First call
            var result1 = await _detector.DetectUIElementsAsync(frame);
            
            // Second call should use cache
            var result2 = await _detector.DetectUIElementsAsync(frame);

            // Service should be called only once due to caching
            _mockDetectionService.Verify(
                x => x.DetectAsync(It.IsAny<Frame>(), It.IsAny<float>(), It.IsAny<CancellationToken>()),
                Times.Once);

            result1.FrameNumber.Should().Be(result2.FrameNumber);
        }

        [Fact]
        public void ClearCache_RemovesAllCachedResults()
        {
            _mockDetectionService.Setup(x => x.DetectAsync(It.IsAny<Frame>(), It.IsAny<float>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<DetectionResult>());

            _detector.ClearCache();
            
            var stats = _detector.GetCacheStats();
            stats.CachedFrames.Should().Be(0);
        }

        [Fact]
        public void GetCacheStats_ReturnsCorrectValues()
        {
            var stats = _detector.GetCacheStats();

            stats.CachedFrames.Should().BeGreaterThanOrEqualTo(0);
            stats.MaxCacheSize.Should().Be(100);
        }

        [Fact]
        public async Task DetectItemIconsAsync_ReturnsItemDetections()
        {
            var detections = new List<DetectionResult>
            {
                new DetectionResult("item_ammo", 0.95f, new Rectangle(10, 10, 50, 50)),
                new DetectionResult("item_medical", 0.90f, new Rectangle(70, 10, 50, 50))
            };

            _mockDetectionService.Setup(x => x.DetectInRegionAsync(It.IsAny<Frame>(), It.IsAny<Rectangle>(), It.IsAny<float>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(detections);

            var frame = new Frame(640, 480) { FrameNumber = 1 };
            var gridRegion = new Rectangle(0, 0, 640, 480);

            var result = await _detector.DetectItemIconsAsync(frame, gridRegion);

            result.Should().HaveCount(2);
            result.All(r => r.Confidence > 0).Should().BeTrue();
        }

        [Fact]
        public async Task DetectExtractionPointsAsync_ReturnsExtractionDetections()
        {
            var detections = new List<DetectionResult>
            {
                new DetectionResult("extraction_tunnel", 0.92f, new Rectangle(500, 450, 100, 50))
            };

            _mockDetectionService.Setup(x => x.DetectAsync(It.IsAny<Frame>(), It.IsAny<float>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(detections);

            var frame = new Frame(640, 480) { FrameNumber = 1 };
            var result = await _detector.DetectExtractionPointsAsync(frame);

            result.Should().HaveCount(1);
            result[0].ScreenPosition.Should().NotBe(default);
        }

        [Fact]
        public async Task DetectMapElementsAsync_ReturnsMapDetections()
        {
            var detections = new List<DetectionResult>
            {
                new DetectionResult("map_factory", 0.88f, new Rectangle(100, 100, 300, 300)),
                new DetectionResult("map_shoreline", 0.85f, new Rectangle(400, 100, 300, 300))
            };

            _mockDetectionService.Setup(x => x.DetectAsync(It.IsAny<Frame>(), It.IsAny<float>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(detections);

            var frame = new Frame(640, 480) { FrameNumber = 1 };
            var result = await _detector.DetectMapElementsAsync(frame);

            result.Should().HaveCount(2);
            result.All(r => !string.IsNullOrEmpty(r.ElementType)).Should().BeTrue();
        }

        [Fact]
        public async Task DetectStatusBarsAsync_ReturnsStatusDetections()
        {
            var detections = new List<DetectionResult>
            {
                new DetectionResult("health_bar", 0.95f, new Rectangle(50, 450, 300, 20))
            };

            _mockDetectionService.Setup(x => x.DetectAsync(It.IsAny<Frame>(), It.IsAny<float>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(detections);

            var frame = new Frame(640, 480) { FrameNumber = 1 };
            var result = await _detector.DetectStatusBarsAsync(frame);

            result.Should().HaveCount(1);
            result[0].BarType.Should().Be("Health");
        }

        [Fact]
        public async Task DetectStatusBarsAsync_IdentifiesBarType_AsGeneric_WhenOnlyBarKeyword()
        {
            var detections = new List<DetectionResult>
            {
                new DetectionResult("status_bar", 0.90f, new Rectangle(50, 450, 300, 20))
            };

            _mockDetectionService.Setup(x => x.DetectAsync(It.IsAny<Frame>(), It.IsAny<float>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(detections);

            var frame = new Frame(640, 480) { FrameNumber = 1 };
            var result = await _detector.DetectStatusBarsAsync(frame);

            result.Should().HaveCount(1);
            result[0].BarType.Should().Be("Generic");
        }

        [Fact]
        public async Task DetectUIElementsAsync_HandlesExceptionGracefully()
        {
            _mockDetectionService.Setup(x => x.DetectAsync(It.IsAny<Frame>(), It.IsAny<float>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Test exception"));

            var frame = new Frame(640, 480) { FrameNumber = 1 };
            
            var act = async () => await _detector.DetectUIElementsAsync(frame);
            
            await act.Should().ThrowAsync<Exception>();
        }

        [Theory]
        [InlineData("customs_map", "Customs")]
        [InlineData("factory_level", "Factory")]
        [InlineData("shoreline_beach", "Shoreline")]
        [InlineData("woods_north", "Woods")]
        [InlineData("lighthouse_map", "Lighthouse")]
        [InlineData("streets_of_tarkov", "Streets")]
        [InlineData("reserve_bunker", "Reserve")]
        [InlineData("ground_zero_area", "Ground Zero")]
        public async Task DetectMapElementsAsync_CorrectlyIdentifiesMapType(string label, string expectedType)
        {
            var detections = new List<DetectionResult>
            {
                new DetectionResult(label, 0.9f, new Rectangle(100, 100, 300, 300))
            };

            _mockDetectionService.Setup(x => x.DetectAsync(It.IsAny<Frame>(), It.IsAny<float>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(detections);

            var frame = new Frame(640, 480) { FrameNumber = 1 };
            var result = await _detector.DetectMapElementsAsync(frame);

            result[0].ElementType.Should().Be(expectedType);
        }
    }
}