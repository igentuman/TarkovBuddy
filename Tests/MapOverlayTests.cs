using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using TarkovBuddy.UI.Overlays;
using TarkovBuddy.Core;
using TarkovBuddy.Models;

namespace TarkovBuddy.Tests
{
    /// <summary>
    /// Unit tests for MapOverlay class.
    /// </summary>
    public class MapOverlayTests
    {
        private readonly Mock<ILogger<MapOverlay>> _loggerMock;

        public MapOverlayTests()
        {
            _loggerMock = new Mock<ILogger<MapOverlay>>();
        }

        [Fact]
        public void Constructor_WithValidLogger_CreatesInstance()
        {
            // Act
            var overlay = new MapOverlay(_loggerMock.Object);

            // Assert
            Assert.NotNull(overlay);
            Assert.Equal("MapOverlay", overlay.ElementName);
            Assert.True(overlay.IsVisible);
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new MapOverlay(null!));
        }

        [Fact]
        public void IsVisible_CanBeToggled()
        {
            // Arrange
            var overlay = new MapOverlay(_loggerMock.Object);

            // Act
            overlay.IsVisible = false;

            // Assert
            Assert.False(overlay.IsVisible);
        }

        [Fact]
        public void Opacity_CanBeSet()
        {
            // Arrange
            var overlay = new MapOverlay(_loggerMock.Object);

            // Act
            overlay.Opacity = 0.5f;

            // Assert
            Assert.Equal(0.5f, overlay.Opacity);
        }

        [Fact]
        public void Opacity_IsClamped_WhenSetAboveOne()
        {
            // Arrange
            var overlay = new MapOverlay(_loggerMock.Object);

            // Act
            overlay.Opacity = 1.5f;

            // Assert
            Assert.Equal(1.0f, overlay.Opacity);
        }

        [Fact]
        public void Opacity_IsClamped_WhenSetBelowZero()
        {
            // Arrange
            var overlay = new MapOverlay(_loggerMock.Object);

            // Act
            overlay.Opacity = -0.5f;

            // Assert
            Assert.Equal(0.0f, overlay.Opacity);
        }

        [Fact]
        public void Position_CanBeSet()
        {
            // Arrange
            var overlay = new MapOverlay(_loggerMock.Object);

            // Act
            overlay.PositionX = 100;
            overlay.PositionY = 200;

            // Assert
            Assert.Equal(100, overlay.PositionX);
            Assert.Equal(200, overlay.PositionY);
        }

        [Fact]
        public void Size_CanBeSet()
        {
            // Arrange
            var overlay = new MapOverlay(_loggerMock.Object);

            // Act
            overlay.Width = 500;
            overlay.Height = 300;

            // Assert
            Assert.Equal(500, overlay.Width);
            Assert.Equal(300, overlay.Height);
        }

        [Fact]
        public async Task RenderAsync_WithValidDeltaTime_Succeeds()
        {
            // Arrange
            var overlay = new MapOverlay(_loggerMock.Object);

            // Act
            await overlay.RenderAsync(16f);

            // Assert - no exception thrown
        }

        [Fact]
        public async Task UpdateAsync_WithInRaidState_MakesVisible()
        {
            // Arrange
            var overlay = new MapOverlay(_loggerMock.Object);
            overlay.IsVisible = false;

            // Act
            await overlay.UpdateAsync(GameState.InRaid, null);

            // Assert
            Assert.True(overlay.IsVisible);
        }

        [Fact]
        public async Task UpdateAsync_WithInLobbyState_HidesOverlay()
        {
            // Arrange
            var overlay = new MapOverlay(_loggerMock.Object);
            overlay.IsVisible = true;

            // Act
            await overlay.UpdateAsync(GameState.InLobby, null);

            // Assert
            Assert.False(overlay.IsVisible);
        }

        [Fact]
        public async Task OnHotkeyAsync_WithToggleMapOverlay_TogglesVisibility()
        {
            // Arrange
            var overlay = new MapOverlay(_loggerMock.Object);
            var initialVisibility = overlay.IsVisible;

            // Act
            await overlay.OnHotkeyAsync("ToggleMapOverlay");

            // Assert
            Assert.NotEqual(initialVisibility, overlay.IsVisible);
        }

        [Fact]
        public void Dispose_WhenCalled_Succeeds()
        {
            // Arrange
            var overlay = new MapOverlay(_loggerMock.Object);

            // Act
            overlay.Dispose();

            // Assert - no exception thrown
        }

        [Fact]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            // Arrange
            var overlay = new MapOverlay(_loggerMock.Object);

            // Act
            overlay.Dispose();
            overlay.Dispose();

            // Assert - no exception thrown
        }

        [Fact]
        public async Task RenderAsync_AfterDispose_ThrowsObjectDisposedException()
        {
            // Arrange
            var overlay = new MapOverlay(_loggerMock.Object);
            overlay.Dispose();

            // Act & Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(() => overlay.RenderAsync(16f));
        }
    }
}