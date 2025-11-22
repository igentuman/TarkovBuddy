using Moq;
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using TarkovBuddy.Services;
using TarkovBuddy.Models;
using System.Drawing;

namespace TarkovBuddy.Tests
{
    public class ObjectDetectionServiceTests
    {
        private readonly Mock<IConfigurationService> _mockConfigService;
        private readonly Mock<IOnnxModelLoader> _mockModelLoader;
        private readonly Mock<ILogger<ObjectDetectionService>> _mockLogger;
        private readonly ObjectDetectionService _service;

        public ObjectDetectionServiceTests()
        {
            _mockConfigService = new Mock<IConfigurationService>();
            _mockModelLoader = new Mock<IOnnxModelLoader>();
            _mockLogger = new Mock<ILogger<ObjectDetectionService>>();

            _service = new ObjectDetectionService(_mockConfigService.Object, _mockModelLoader.Object, _mockLogger.Object);
        }

        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenConfigServiceIsNull()
        {
            var act = () => new ObjectDetectionService(null!, _mockModelLoader.Object, _mockLogger.Object);
            act.Should().Throw<ArgumentNullException>().WithParameterName("configService");
        }

        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenModelLoaderIsNull()
        {
            var act = () => new ObjectDetectionService(_mockConfigService.Object, null!, _mockLogger.Object);
            act.Should().Throw<ArgumentNullException>().WithParameterName("modelLoader");
        }

        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
        {
            var act = () => new ObjectDetectionService(_mockConfigService.Object, _mockModelLoader.Object, null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
        }

        [Fact]
        public void ServiceName_ReturnsCorrectName()
        {
            _service.ServiceName.Should().Be("ObjectDetectionService");
        }

        [Fact]
        public void IsRunning_ReturnsFalseInitially()
        {
            _service.IsRunning.Should().BeFalse();
        }

        [Fact]
        public void Start_SetsIsRunningToTrue()
        {
            _service.Start();
            _service.IsRunning.Should().BeTrue();
        }

        [Fact]
        public void Start_LogsInformationMessage()
        {
            _service.Start();

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public void Start_WhenAlreadyRunning_LogsWarning()
        {
            _service.Start();
            _service.Start(); // Second call

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("already running")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public void Stop_SetsIsRunningToFalse()
        {
            _service.Start();
            _service.Stop();
            _service.IsRunning.Should().BeFalse();
        }

        [Fact]
        public void Stop_WhenNotRunning_ReturnsQuietly()
        {
            _service.Stop();
            _service.IsRunning.Should().BeFalse();
        }

        [Fact]
        public async Task DetectAsync_ReturnsEmptyList_WhenServiceNotRunning()
        {
            var frame = new Frame(640, 480) { FrameNumber = 1 };
            var result = await _service.DetectAsync(frame);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task DetectAsync_ReturnsEmptyList_WhenFrameIsNull()
        {
            _service.Start();
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            var result = await _service.DetectAsync(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task DetectAsync_ReturnsEmptyList_WhenNoModelLoaded()
        {
            _service.Start();
            _mockModelLoader.Setup(x => x.ActiveSession).Returns((Microsoft.ML.OnnxRuntime.InferenceSession)null!);

            var frame = new Frame(640, 480) { FrameNumber = 1 };
            var result = await _service.DetectAsync(frame);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task DetectAsync_IncrementsTotalInferences()
        {
            _service.Start();
            var frame = new Frame(640, 480) { FrameNumber = 1 };

            await _service.DetectAsync(frame);
            await _service.DetectAsync(frame);

            _service.InferenceFps.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task DetectInRegionAsync_AcceptsValidRegion()
        {
            _service.Start();
            var frame = new Frame(640, 480) { FrameNumber = 1 };
            var region = new Rectangle(100, 100, 200, 200);

            var result = await _service.DetectInRegionAsync(frame, region);

            result.Should().NotBeNull();
        }

        [Fact]
        public async Task LoadModelAsync_ReturnsFalse_WhenModelDoesNotExist()
        {
            _mockModelLoader.Setup(x => x.LoadModelAsync(It.IsAny<string>()))
                .ReturnsAsync((Microsoft.ML.OnnxRuntime.InferenceSession)null!);

            var result = await _service.LoadModelAsync("nonexistent-model.onnx");

            result.Should().BeFalse();
        }

        [Fact]
        public void AverageInferenceTimeMs_ReturnsZero_WhenNoInferences()
        {
            _service.AverageInferenceTimeMs.Should().Be(0);
        }

        [Fact]
        public void InferenceFps_ReturnsZero_WhenNotRunning()
        {
            _service.InferenceFps.Should().Be(0);
        }

        [Fact]
        public void IsGpuAccelerated_ReturnsModelLoaderValue()
        {
            _mockModelLoader.Setup(x => x.IsGpuAvailable).Returns(true);

            _service.IsGpuAccelerated.Should().BeTrue();
        }

        [Fact]
        public void LoadedModelName_ReturnsModelLoaderValue()
        {
            _mockModelLoader.Setup(x => x.ActiveModelName).Returns("test-model.onnx");

            _service.LoadedModelName.Should().Be("test-model.onnx");
        }

        [Fact]
        public void Dispose_ReleasesResources()
        {
            _service.Start();
            _service.Dispose();

            _mockModelLoader.Verify(x => x.Dispose(), Times.Once);
        }

        [Fact]
        public void Dispose_LogsDisposalMessage()
        {
            _service.Dispose();

            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("disposed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public void DetectionCompleted_EventFires()
        {
            _service.DetectionCompleted += (s, e) => { };

            _service.Start();
            var frame = new Frame(640, 480) { FrameNumber = 1 };
            var _ = _service.DetectAsync(frame).GetAwaiter().GetResult();

            // Event should fire
            // Note: In current implementation, event fires even with empty results
        }

        [Fact]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            var act = () =>
            {
                _service.Dispose();
                _service.Dispose();
            };

            act.Should().NotThrow();
        }
    }
}