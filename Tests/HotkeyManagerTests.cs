using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using TarkovBuddy.Services;

namespace TarkovBuddy.Tests
{
    /// <summary>
    /// Unit tests for HotkeyManager class.
    /// </summary>
    public class HotkeyManagerTests
    {
        private readonly Mock<ILogger<HotkeyManager>> _loggerMock;

        public HotkeyManagerTests()
        {
            _loggerMock = new Mock<ILogger<HotkeyManager>>();
        }

        [Fact]
        public void Constructor_WithValidLogger_CreatesInstance()
        {
            // Act
            var manager = new HotkeyManager(_loggerMock.Object);

            // Assert
            Assert.NotNull(manager);
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new HotkeyManager(null!));
        }

        [Fact]
        public void Initialize_WithValidWindowHandle_Succeeds()
        {
            // Arrange
            var manager = new HotkeyManager(_loggerMock.Object);
            var windowHandle = (nint)12345;

            // Act
            manager.Initialize(windowHandle);

            // Assert - no exception thrown
        }

        [Fact]
        public void RegisterHotkey_WithoutInitialization_ThrowsInvalidOperationException()
        {
            // Arrange
            var manager = new HotkeyManager(_loggerMock.Object);
            var handler = () => Task.CompletedTask;

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => manager.RegisterHotkey(0, 0, handler));
        }

        [Fact]
        public void RegisterHotkey_WithNullHandler_ThrowsArgumentNullException()
        {
            // Arrange
            var manager = new HotkeyManager(_loggerMock.Object);
            manager.Initialize((nint)12345);

            // Act & Assert - RegisterHotkey will fail silently due to Win32 API, returns -1
            // But null handler should still throw
            Assert.Throws<ArgumentNullException>(() => manager.RegisterHotkey(0, 0, null!));
        }

        [Fact]
        public async Task InvokeHotkeyAsync_WithInvalidHotkeyId_DoesNotThrow()
        {
            // Arrange
            var manager = new HotkeyManager(_loggerMock.Object);
            manager.Initialize((nint)12345);

            // Act
            await manager.InvokeHotkeyAsync(99999);

            // Assert - no exception thrown
        }

        [Fact]
        public void Dispose_WhenCalled_Succeeds()
        {
            // Arrange
            var manager = new HotkeyManager(_loggerMock.Object);
            manager.Initialize((nint)12345);

            // Act
            manager.Dispose();

            // Assert - no exception thrown
        }

        [Fact]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            // Arrange
            var manager = new HotkeyManager(_loggerMock.Object);
            manager.Initialize((nint)12345);

            // Act
            manager.Dispose();
            manager.Dispose();

            // Assert - no exception thrown
        }
    }
}