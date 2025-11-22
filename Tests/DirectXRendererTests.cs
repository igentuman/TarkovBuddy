using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using TarkovBuddy.UI;

namespace TarkovBuddy.Tests
{
    /// <summary>
    /// Unit tests for DirectXRenderer class.
    /// </summary>
    public class DirectXRendererTests
    {
        private readonly Mock<ILogger<DirectXRenderer>> _loggerMock;

        public DirectXRendererTests()
        {
            _loggerMock = new Mock<ILogger<DirectXRenderer>>();
        }

        [Fact]
        public void Constructor_WithValidLogger_CreatesInstance()
        {
            // Act
            var renderer = new DirectXRenderer(_loggerMock.Object);

            // Assert
            Assert.NotNull(renderer);
            Assert.False(renderer.IsInitialized);
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new DirectXRenderer(null!));
        }

        [Fact]
        public void Dispose_WhenNotInitialized_DoesNotThrow()
        {
            // Arrange
            var renderer = new DirectXRenderer(_loggerMock.Object);

            // Act
            renderer.Dispose();

            // Assert - no exception thrown
        }

        [Fact]
        public void Clear_WhenNotInitialized_DoesNotThrow()
        {
            // Arrange
            var renderer = new DirectXRenderer(_loggerMock.Object);

            // Act
            renderer.Clear();

            // Assert - no exception thrown
        }

        [Fact]
        public void Present_WhenNotInitialized_DoesNotThrow()
        {
            // Arrange
            var renderer = new DirectXRenderer(_loggerMock.Object);

            // Act
            renderer.Present();

            // Assert - no exception thrown
        }

        [Fact]
        public void GetDeviceContext_WhenNotInitialized_ReturnsNull()
        {
            // Arrange
            var renderer = new DirectXRenderer(_loggerMock.Object);

            // Act
            var context = renderer.GetDeviceContext();

            // Assert
            Assert.Null(context);
        }

        [Fact]
        public void GetDevice_WhenNotInitialized_ReturnsNull()
        {
            // Arrange
            var renderer = new DirectXRenderer(_loggerMock.Object);

            // Act
            var device = renderer.GetDevice();

            // Assert
            Assert.Null(device);
        }

        [Fact]
        public void ScreenDimensions_AreInitiallyZero()
        {
            // Arrange
            var renderer = new DirectXRenderer(_loggerMock.Object);

            // Assert
            Assert.Equal(0, renderer.ScreenWidth);
            Assert.Equal(0, renderer.ScreenHeight);
        }
    }
}