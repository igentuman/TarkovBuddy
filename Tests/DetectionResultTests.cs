using Xunit;
using FluentAssertions;
using TarkovBuddy.Services;
using System.Drawing;

namespace TarkovBuddy.Tests
{
    public class DetectionResultTests
    {
        [Fact]
        public void Constructor_InitializesProperties()
        {
            var bbox = new Rectangle(10, 20, 100, 50);
            var result = new DetectionResult("test_label", 0.95f, bbox);

            result.Label.Should().Be("test_label");
            result.Confidence.Should().Be(0.95f);
            result.BoundingBox.Should().Be(bbox);
            result.TrackingId.Should().Be(0);
            result.Metadata.Should().BeEmpty();
        }

        [Fact]
        public void GetCenter_CalculatesCorrectCenter()
        {
            var bbox = new Rectangle(10, 20, 100, 50);
            var result = new DetectionResult("label", 0.9f, bbox);

            var center = result.GetCenter();

            center.X.Should().Be(60); // 10 + 100/2
            center.Y.Should().Be(45); // 20 + 50/2
        }

        [Fact]
        public void GetCenter_WithZeroOrigin()
        {
            var bbox = new Rectangle(0, 0, 100, 100);
            var result = new DetectionResult("label", 0.9f, bbox);

            var center = result.GetCenter();

            center.X.Should().Be(50);
            center.Y.Should().Be(50);
        }

        [Fact]
        public void GetCenter_WithOddDimensions()
        {
            var bbox = new Rectangle(5, 15, 99, 79);
            var result = new DetectionResult("label", 0.9f, bbox);

            var center = result.GetCenter();

            center.X.Should().Be(54); // 5 + 99/2 = 5 + 49 = 54
            center.Y.Should().Be(54); // 15 + 79/2 = 15 + 39 = 54
        }

        [Fact]
        public void ToString_ContainsLabel()
        {
            var bbox = new Rectangle(10, 20, 100, 50);
            var result = new DetectionResult("item_icon", 0.95f, bbox);

            var str = result.ToString();

            str.Should().Contain("item_icon");
        }

        [Fact]
        public void ToString_ContainsConfidence()
        {
            var bbox = new Rectangle(10, 20, 100, 50);
            var result = new DetectionResult("label", 0.95f, bbox);

            var str = result.ToString();

            str.Should().Contain("0.95");
        }

        [Fact]
        public void ToString_ContainsBoundingBoxInfo()
        {
            var bbox = new Rectangle(10, 20, 100, 50);
            var result = new DetectionResult("label", 0.95f, bbox);

            var str = result.ToString();

            str.Should().Contain("10");
            str.Should().Contain("20");
            str.Should().Contain("100");
            str.Should().Contain("50");
        }

        [Fact]
        public void ToString_ContainsDetectionKeyword()
        {
            var bbox = new Rectangle(10, 20, 100, 50);
            var result = new DetectionResult("label", 0.95f, bbox);

            var str = result.ToString();

            str.Should().Contain("Detection");
        }

        [Fact]
        public void ToString_IsConsistent()
        {
            var bbox = new Rectangle(10, 20, 100, 50);
            var result = new DetectionResult("label", 0.95f, bbox);

            var str1 = result.ToString();
            var str2 = result.ToString();

            str1.Should().Be(str2);
        }

        [Fact]
        public void Label_CanBeSetAfterConstruction()
        {
            var bbox = new Rectangle(10, 20, 100, 50);
            var result = new DetectionResult("initial", 0.95f, bbox);

            result.Label = "updated";

            result.Label.Should().Be("updated");
        }

        [Fact]
        public void Confidence_CanBeSetAfterConstruction()
        {
            var bbox = new Rectangle(10, 20, 100, 50);
            var result = new DetectionResult("label", 0.95f, bbox);

            result.Confidence = 0.85f;

            result.Confidence.Should().Be(0.85f);
        }

        [Fact]
        public void BoundingBox_CanBeSetAfterConstruction()
        {
            var bbox = new Rectangle(10, 20, 100, 50);
            var result = new DetectionResult("label", 0.95f, bbox);

            var newBbox = new Rectangle(30, 40, 200, 100);
            result.BoundingBox = newBbox;

            result.BoundingBox.Should().Be(newBbox);
        }

        [Fact]
        public void TrackingId_CanBeSetAfterConstruction()
        {
            var bbox = new Rectangle(10, 20, 100, 50);
            var result = new DetectionResult("label", 0.95f, bbox);

            result.TrackingId = 42;

            result.TrackingId.Should().Be(42);
        }

        [Fact]
        public void Metadata_CanStoreArbitraryValues()
        {
            var bbox = new Rectangle(10, 20, 100, 50);
            var result = new DetectionResult("label", 0.95f, bbox);

            result.Metadata["rarity"] = "rare";
            result.Metadata["itemId"] = 12345;
            result.Metadata["active"] = true;

            result.Metadata.Should().HaveCount(3);
            result.Metadata["rarity"].Should().Be("rare");
            result.Metadata["itemId"].Should().Be(12345);
            result.Metadata["active"].Should().Be(true);
        }

        [Fact]
        public void Metadata_InitiallyEmpty()
        {
            var bbox = new Rectangle(10, 20, 100, 50);
            var result = new DetectionResult("label", 0.95f, bbox);

            result.Metadata.Should().BeEmpty();
        }

        [Fact]
        public void Constructor_WithDifferentConfidenceValues()
        {
            var bbox = new Rectangle(0, 0, 100, 100);

            var result1 = new DetectionResult("label", 0.0f, bbox);
            var result2 = new DetectionResult("label", 0.5f, bbox);
            var result3 = new DetectionResult("label", 1.0f, bbox);

            result1.Confidence.Should().Be(0.0f);
            result2.Confidence.Should().Be(0.5f);
            result3.Confidence.Should().Be(1.0f);
        }

        [Fact]
        public void Constructor_WithEmptyLabel()
        {
            var bbox = new Rectangle(10, 20, 100, 50);
            var result = new DetectionResult(string.Empty, 0.95f, bbox);

            result.Label.Should().Be(string.Empty);
        }

        [Fact]
        public void Constructor_WithLongLabel()
        {
            var bbox = new Rectangle(10, 20, 100, 50);
            var longLabel = new string('a', 1000);
            var result = new DetectionResult(longLabel, 0.95f, bbox);

            result.Label.Should().HaveLength(1000);
        }

        [Fact]
        public void GetCenter_WithLargeCoordinates()
        {
            var bbox = new Rectangle(5000, 3000, 2000, 1500);
            var result = new DetectionResult("label", 0.95f, bbox);

            var center = result.GetCenter();

            center.X.Should().Be(6000); // 5000 + 2000/2
            center.Y.Should().Be(3750); // 3000 + 1500/2
        }

        [Fact]
        public void ToString_FormatIsWellFormed()
        {
            var bbox = new Rectangle(10, 20, 100, 50);
            var result = new DetectionResult("test_label", 0.85f, bbox);

            var str = result.ToString();

            // Should start with "Detection("
            str.Should().StartWith("Detection(");
            // Should end with ")"
            str.Should().EndWith(")");
            // Should be reasonably formatted
            str.Should().Contain("Label=");
            str.Should().Contain("Confidence=");
            str.Should().Contain("Box=");
            str.Should().Contain("TrackingId=");
        }
    }
}