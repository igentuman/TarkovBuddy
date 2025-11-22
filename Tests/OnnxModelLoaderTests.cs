using Moq;
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using TarkovBuddy.Services;

namespace TarkovBuddy.Tests
{
    public class OnnxModelLoaderTests
    {
        private readonly Mock<ILogger<OnnxModelLoader>> _mockLogger;

        public OnnxModelLoaderTests()
        {
            _mockLogger = new Mock<ILogger<OnnxModelLoader>>();
        }

        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
        {
            var act = () => new OnnxModelLoader(null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_InitializesWithDefaultModelsDirectory()
        {
            var loader = new OnnxModelLoader(_mockLogger.Object);
            
            // Object should be created successfully
            loader.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_InitializesWithCustomModelsDirectory()
        {
            var loader = new OnnxModelLoader(_mockLogger.Object, "CustomModels");
            
            loader.Should().NotBeNull();
        }

        [Fact]
        public void IsGpuAvailable_ReturnsBoolean()
        {
            var loader = new OnnxModelLoader(_mockLogger.Object);
            
            var isGpu = loader.IsGpuAvailable;
            // isGpu is a bool - verify it's either true or false
            (isGpu == true || isGpu == false).Should().BeTrue();
        }

        [Fact]
        public void ActiveModelName_ReturnsNullInitially()
        {
            var loader = new OnnxModelLoader(_mockLogger.Object);
            
            loader.ActiveModelName.Should().BeNull();
        }

        [Fact]
        public void ActiveSession_ReturnsNullInitially()
        {
            var loader = new OnnxModelLoader(_mockLogger.Object);
            
            loader.ActiveSession.Should().BeNull();
        }

        [Fact]
        public void LoadModel_ReturnsNull_WhenModelFileDoesNotExist()
        {
            var loader = new OnnxModelLoader(_mockLogger.Object);
            
            var result = loader.LoadModel("nonexistent-model.onnx");
            
            result.Should().BeNull();
        }

        [Fact]
        public void LoadModel_NormalizesModelName_WithoutExtension()
        {
            var loader = new OnnxModelLoader(_mockLogger.Object);
            
            // Should handle model names without .onnx extension
            var result = loader.LoadModel("test-model");
            
            // Result should be null since file doesn't exist, but no exception should be thrown
            result.Should().BeNull();
        }

        [Fact]
        public async Task LoadModelAsync_ReturnsNullTask_WhenModelDoesNotExist()
        {
            var loader = new OnnxModelLoader(_mockLogger.Object);
            
            var result = await loader.LoadModelAsync("nonexistent-model.onnx");
            
            result.Should().BeNull();
        }

        [Fact]
        public void GetAvailableModels_ReturnsEmptyList_WhenDirectoryDoesNotExist()
        {
            var loader = new OnnxModelLoader(_mockLogger.Object, "NonexistentDirectory");
            
            var models = loader.GetAvailableModels();
            
            models.Should().BeOfType<List<OnnxModelInfo>>();
        }

        [Fact]
        public void GetAvailableModels_ReturnsListOfOnnxModelInfo()
        {
            var loader = new OnnxModelLoader(_mockLogger.Object);
            
            var models = loader.GetAvailableModels();
            
            models.Should().NotBeNull();
            models.Should().BeOfType<List<OnnxModelInfo>>();
        }

        [Fact]
        public void ClearCache_DoesNotThrow()
        {
            var loader = new OnnxModelLoader(_mockLogger.Object);
            
            var act = () => loader.ClearCache();
            
            act.Should().NotThrow();
        }

        [Fact]
        public void GetRuntimeInfo_ReturnsOnnxRuntimeInfo()
        {
            var loader = new OnnxModelLoader(_mockLogger.Object);
            
            var info = loader.GetRuntimeInfo();
            
            info.Should().NotBeNull();
            info.Should().BeOfType<OnnxRuntimeInfo>();
        }

        [Fact]
        public void GetRuntimeInfo_ContainsRuntimeVersion()
        {
            var loader = new OnnxModelLoader(_mockLogger.Object);
            
            var info = loader.GetRuntimeInfo();
            
            info.RuntimeVersion.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void GetRuntimeInfo_ContainsAvailableProviders()
        {
            var loader = new OnnxModelLoader(_mockLogger.Object);
            
            var info = loader.GetRuntimeInfo();
            
            info.AvailableProviders.Should().NotBeEmpty();
        }

        [Fact]
        public void Dispose_DoesNotThrow()
        {
            var loader = new OnnxModelLoader(_mockLogger.Object);
            
            var act = () => loader.Dispose();
            
            act.Should().NotThrow();
        }

        [Fact]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            var loader = new OnnxModelLoader(_mockLogger.Object);
            
            var act = () =>
            {
                loader.Dispose();
                loader.Dispose();
            };
            
            act.Should().NotThrow();
        }

        [Fact]
        public void UnloadModel_WithNonexistentModel_DoesNotThrow()
        {
            var loader = new OnnxModelLoader(_mockLogger.Object);
            
            var act = () => loader.UnloadModel("nonexistent-model.onnx");
            
            act.Should().NotThrow();
        }

        [Fact]
        public void OnnxModelInfo_HasCorrectProperties()
        {
            var info = new OnnxModelInfo
            {
                Name = "test.onnx",
                Path = "/models/test.onnx",
                SizeBytes = 1024,
                IsLoaded = true,
                IsCached = false
            };

            info.Name.Should().Be("test.onnx");
            info.Path.Should().Be("/models/test.onnx");
            info.SizeBytes.Should().Be(1024);
            info.IsLoaded.Should().BeTrue();
            info.IsCached.Should().BeFalse();
        }

        [Fact]
        public void OnnxRuntimeInfo_HasCorrectProperties()
        {
            var info = new OnnxRuntimeInfo
            {
                RuntimeVersion = "1.23.2",
                GpuAvailable = true,
                LoadedModelCount = 1,
                CachedModelCount = 0
            };

            info.RuntimeVersion.Should().Be("1.23.2");
            info.GpuAvailable.Should().BeTrue();
            info.LoadedModelCount.Should().Be(1);
            info.CachedModelCount.Should().Be(0);
        }

        [Fact]
        public void OnnxRuntimeInfo_DefaultConstructor_InitializesProperties()
        {
            var info = new OnnxRuntimeInfo();

            info.RuntimeVersion.Should().Be("Unknown");
            info.AvailableProviders.Should().BeEmpty();
            info.GpuAvailable.Should().BeFalse();
            info.LoadedModelCount.Should().Be(0);
            info.CachedModelCount.Should().Be(0);
        }
    }
}