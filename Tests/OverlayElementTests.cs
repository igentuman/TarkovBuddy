using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using TarkovBuddy.UI;
using TarkovBuddy.UI.Overlays;
using TarkovBuddy.Core;

namespace TarkovBuddy.Tests
{
    /// <summary>
    /// Integration tests for overlay elements.
    /// </summary>
    public class OverlayElementTests
    {
        [Fact]
        public void QuestOverlay_ImplementsIOverlayElement()
        {
            // Arrange & Act
            var loggerMock = new Mock<ILogger<QuestOverlay>>();
            var overlay = new QuestOverlay(loggerMock.Object);

            // Assert
            Assert.IsAssignableFrom<IOverlayElement>(overlay);
        }

        [Fact]
        public void LootEvaluatorOverlay_ImplementsIOverlayElement()
        {
            // Arrange & Act
            var loggerMock = new Mock<ILogger<LootEvaluatorOverlay>>();
            var overlay = new LootEvaluatorOverlay(loggerMock.Object);

            // Assert
            Assert.IsAssignableFrom<IOverlayElement>(overlay);
        }

        [Fact]
        public void StashStatsOverlay_ImplementsIOverlayElement()
        {
            // Arrange & Act
            var loggerMock = new Mock<ILogger<StashStatsOverlay>>();
            var overlay = new StashStatsOverlay(loggerMock.Object);

            // Assert
            Assert.IsAssignableFrom<IOverlayElement>(overlay);
        }

        [Fact]
        public void MapOverlay_ImplementsIOverlayElement()
        {
            // Arrange & Act
            var loggerMock = new Mock<ILogger<MapOverlay>>();
            var overlay = new MapOverlay(loggerMock.Object);

            // Assert
            Assert.IsAssignableFrom<IOverlayElement>(overlay);
        }

        [Fact]
        public async Task AllOverlayElements_CanUpdateAsync()
        {
            // Arrange
            var mapOverlayLogger = new Mock<ILogger<MapOverlay>>();
            var questOverlayLogger = new Mock<ILogger<QuestOverlay>>();
            var lootOverlayLogger = new Mock<ILogger<LootEvaluatorOverlay>>();
            var stashOverlayLogger = new Mock<ILogger<StashStatsOverlay>>();

            var elements = new IOverlayElement[]
            {
                new MapOverlay(mapOverlayLogger.Object),
                new QuestOverlay(questOverlayLogger.Object),
                new LootEvaluatorOverlay(lootOverlayLogger.Object),
                new StashStatsOverlay(stashOverlayLogger.Object)
            };

            // Act & Assert
            foreach (var element in elements)
            {
                await element.UpdateAsync(GameState.InRaid, null);
            }
        }

        [Fact]
        public async Task AllOverlayElements_CanRenderAsync()
        {
            // Arrange
            var mapOverlayLogger = new Mock<ILogger<MapOverlay>>();
            var questOverlayLogger = new Mock<ILogger<QuestOverlay>>();
            var lootOverlayLogger = new Mock<ILogger<LootEvaluatorOverlay>>();
            var stashOverlayLogger = new Mock<ILogger<StashStatsOverlay>>();

            var elements = new IOverlayElement[]
            {
                new MapOverlay(mapOverlayLogger.Object),
                new QuestOverlay(questOverlayLogger.Object),
                new LootEvaluatorOverlay(lootOverlayLogger.Object),
                new StashStatsOverlay(stashOverlayLogger.Object)
            };

            // Act & Assert
            foreach (var element in elements)
            {
                await element.RenderAsync(16f);
            }
        }

        [Fact]
        public void AllOverlayElements_HaveUniqueNames()
        {
            // Arrange
            var mapOverlayLogger = new Mock<ILogger<MapOverlay>>();
            var questOverlayLogger = new Mock<ILogger<QuestOverlay>>();
            var lootOverlayLogger = new Mock<ILogger<LootEvaluatorOverlay>>();
            var stashOverlayLogger = new Mock<ILogger<StashStatsOverlay>>();

            var elements = new IOverlayElement[]
            {
                new MapOverlay(mapOverlayLogger.Object),
                new QuestOverlay(questOverlayLogger.Object),
                new LootEvaluatorOverlay(lootOverlayLogger.Object),
                new StashStatsOverlay(stashOverlayLogger.Object)
            };

            // Act
            var names = elements.Select(e => e.ElementName).ToList();

            // Assert
            Assert.Equal(4, names.Distinct().Count()); // All names should be unique
        }
    }
}