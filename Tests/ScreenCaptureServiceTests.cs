using System.Diagnostics;
using Moq;
using TarkovBuddy.Models;
using TarkovBuddy.Services;
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace TarkovBuddy.Tests
{
    /// <summary>
    /// Unit tests for ScreenCaptureService.
    /// Tests frame capture, buffering, and performance characteristics.
    /// </summary>
    public class ScreenCaptureServiceTests : IAsyncLifetime
    {
        private readonly Mock<IConfigurationService> _mockConfigService;
        private readonly Mock<ILogger<ScreenCaptureService>> _mockLogger;
        private ScreenCaptureService _service = null!;

        public ScreenCaptureServiceTests()
        {
            _mockConfigService = new Mock<IConfigurationService>();
            _mockLogger = new Mock<ILogger<ScreenCaptureService>>();
        }

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            if (_service != null)
            {
                _service.Stop();
                _service.Dispose();
                await Task.Delay(500); // Allow cleanup
            }
        }

        [Fact]
        public void Constructor_WithValidDependencies_Succeeds()
        {
            // Arrange & Act
            var service = new ScreenCaptureService(_mockConfigService.Object, _mockLogger.Object);

            // Assert
            service.Should().NotBeNull();
            service.IsRunning.Should().BeFalse();
            service.BufferedFrameCount.Should().Be(0);
        }

        [Fact]
        public void Constructor_WithNullConfigService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ScreenCaptureService(null!, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ScreenCaptureService(_mockConfigService.Object, null!));
        }

        [Fact]
        public void Start_WhenNotRunning_StartsSuccessfully()
        {
            // Arrange
            _service = new ScreenCaptureService(_mockConfigService.Object, _mockLogger.Object);

            // Act
            _service.Start();

            // Assert
            _service.IsRunning.Should().BeTrue();
        }

        [Fact]
        public void Start_WhenAlreadyRunning_LogsWarning()
        {
            // Arrange
            _service = new ScreenCaptureService(_mockConfigService.Object, _mockLogger.Object);
            _service.Start();

            // Act
            _service.Start();

            // Assert
            _service.IsRunning.Should().BeTrue();
            _mockLogger.Verify(
                x => x.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public void Stop_WhenRunning_StopsSuccessfully()
        {
            // Arrange
            _service = new ScreenCaptureService(_mockConfigService.Object, _mockLogger.Object);
            _service.Start();

            // Act
            _service.Stop();

            // Assert
            _service.IsRunning.Should().BeFalse();
        }

        [Fact]
        public void Stop_WhenNotRunning_DoesNotThrow()
        {
            // Arrange
            _service = new ScreenCaptureService(_mockConfigService.Object, _mockLogger.Object);

            // Act & Assert
            _service.Invoking(s => s.Stop()).Should().NotThrow();
        }

        [Fact]
        public async Task GetFramesAsync_WithoutStart_ThrowsInvalidOperationException()
        {
            // Arrange
            _service = new ScreenCaptureService(_mockConfigService.Object, _mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await foreach (var frame in _service.GetFramesAsync())
                {
                    // Won't reach here
                }
            });
        }

        [Fact]
        public async Task GetFramesAsync_AfterStart_ReturnsFrames()
        {
            // Arrange
            _service = new ScreenCaptureService(_mockConfigService.Object, _mockLogger.Object);
            _service.Start();

            // Act
            var frameCount = 0;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));

            try
            {
                await foreach (var frame in _service.GetFramesAsync(cts.Token))
                {
                    frameCount++;
                    if (frameCount >= 3)
                        cts.Cancel();
                }
            }
            catch (OperationCanceledException)
            {
                // Expected
            }

            // Assert
            frameCount.Should().BeGreaterThanOrEqualTo(1);
        }

        [Fact]
        public void CaptureScreenshot_AfterStart_ReturnsValidBitmap()
        {
            // Arrange
            _service = new ScreenCaptureService(_mockConfigService.Object, _mockLogger.Object);
            _service.Start();
            Thread.Sleep(500);

            // Act
            using var bitmap = _service.CaptureScreenshot();

            // Assert
            bitmap.Should().NotBeNull();
            bitmap!.Width.Should().BeGreaterThan(0);
            bitmap.Height.Should().BeGreaterThan(0);
        }

        [Fact]
        public void CaptureScreenshot_BeforeStart_ReturnsNull()
        {
            // Arrange
            _service = new ScreenCaptureService(_mockConfigService.Object, _mockLogger.Object);

            // Act
            var bitmap = _service.CaptureScreenshot();

            // Assert
            bitmap.Should().BeNull();
        }

        [Fact]
        public void BufferedFrameCount_ReflectsCurrent()
        {
            // Arrange
            _service = new ScreenCaptureService(_mockConfigService.Object, _mockLogger.Object);
            _service.Start();
            Thread.Sleep(200);

            // Act
            var count = _service.BufferedFrameCount;

            // Assert
            count.Should().BeGreaterThanOrEqualTo(0);
        }

        [Fact]
        public async Task FrameProperties_AreValid()
        {
            // Arrange
            _service = new ScreenCaptureService(_mockConfigService.Object, _mockLogger.Object);
            _service.Start();

            // Act
            Frame? capturedFrame = null;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

            try
            {
                await foreach (var frame in _service.GetFramesAsync(cts.Token))
                {
                    capturedFrame = frame;
                    cts.Cancel();
                }
            }
            catch (OperationCanceledException)
            {
                // Expected
            }

            // Assert
            capturedFrame.Should().NotBeNull();
            capturedFrame!.Width.Should().BeGreaterThan(0);
            capturedFrame.Height.Should().BeGreaterThan(0);
            capturedFrame.PixelData.Should().NotBeEmpty();
            capturedFrame.Timestamp.Should().BeLessThanOrEqualTo(DateTime.UtcNow);
            capturedFrame.FrameNumber.Should().BeGreaterThanOrEqualTo(0);
        }

        [Fact]
        public void Dispose_CleansUpResources()
        {
            // Arrange
            _service = new ScreenCaptureService(_mockConfigService.Object, _mockLogger.Object);
            _service.Start();

            // Act
            _service.Dispose();

            // Assert
            _service.IsRunning.Should().BeFalse();
        }

        [Fact]
        public void CurrentFps_WhenNotRunning_ReturnsZero()
        {
            // Arrange
            _service = new ScreenCaptureService(_mockConfigService.Object, _mockLogger.Object);

            // Act
            var fps = _service.CurrentFps;

            // Assert
            fps.Should().Be(0);
        }

        [Fact]
        public async Task CurrentFps_AfterCapture_IsReasonable()
        {
            // Arrange
            _service = new ScreenCaptureService(_mockConfigService.Object, _mockLogger.Object);
            _service.Start();

            // Act
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            var frameCount = 0;

            try
            {
                await foreach (var _ in _service.GetFramesAsync(cts.Token))
                {
                    frameCount++;
                }
            }
            catch (OperationCanceledException)
            {
                // Expected
            }

            var fps = _service.CurrentFps;

            // Assert
            fps.Should().BeGreaterThan(0);
            fps.Should().BeLessThan(60); // Should be around 30 FPS
        }

        [Fact]
        public void FrameCapturedEvent_IsFiredWhenFrameCaptured()
        {
            // Arrange
            _service = new ScreenCaptureService(_mockConfigService.Object, _mockLogger.Object);
            var eventFired = false;

            _service.FrameCaptured += (sender, args) =>
            {
                eventFired = true;
            };

            // Act
            _service.Start();
            Thread.Sleep(500);

            // Assert
            eventFired.Should().BeTrue();
        }

        [Fact]
        public void FrameCapturedEventArgs_ContainsValidData()
        {
            // Arrange
            _service = new ScreenCaptureService(_mockConfigService.Object, _mockLogger.Object);
            FrameCapturedEventArgs? capturedArgs = null;

            _service.FrameCaptured += (sender, args) =>
            {
                capturedArgs ??= args;
            };

            // Act
            _service.Start();
            Thread.Sleep(100);

            // Assert
            capturedArgs.Should().NotBeNull();
            capturedArgs!.Frame.Should().NotBeNull();
            capturedArgs.CaptureTimeMs.Should().BeGreaterThanOrEqualTo(0);
            capturedArgs.DroppedFrames.Should().BeGreaterThanOrEqualTo(0);
        }
    }
}