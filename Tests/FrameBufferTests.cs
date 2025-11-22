using TarkovBuddy.Models;
using TarkovBuddy.Services;
using Xunit;
using FluentAssertions;

namespace TarkovBuddy.Tests
{
    /// <summary>
    /// Unit tests for FrameBuffer.
    /// Tests circular buffer operations, thread safety, and frame management.
    /// </summary>
    public class FrameBufferTests
    {
        private static Frame CreateTestFrame(int width = 1920, int height = 1080)
        {
            return new Frame(width, height);
        }

        [Fact]
        public void Constructor_WithValidCapacity_Succeeds()
        {
            // Act
            var buffer = new FrameBuffer(5);

            // Assert
            buffer.Capacity.Should().Be(5);
            buffer.Count.Should().Be(0);
            buffer.IsEmpty.Should().BeTrue();
        }

        [Fact]
        public void Constructor_WithZeroCapacity_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new FrameBuffer(0));
        }

        [Fact]
        public void Constructor_WithNegativeCapacity_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => new FrameBuffer(-1));
        }

        [Fact]
        public void Enqueue_AddsSingleFrame()
        {
            // Arrange
            var buffer = new FrameBuffer(5);
            var frame = CreateTestFrame();

            // Act
            var droppedFrames = buffer.Enqueue(frame);

            // Assert
            buffer.Count.Should().Be(1);
            buffer.IsEmpty.Should().BeFalse();
            buffer.IsFull.Should().BeFalse();
            droppedFrames.Should().Be(0);
        }

        [Fact]
        public void Enqueue_WithNullFrame_ThrowsArgumentNullException()
        {
            // Arrange
            var buffer = new FrameBuffer(5);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => buffer.Enqueue(null!));
        }

        [Fact]
        public void Enqueue_FillsBufferUpToCapacity()
        {
            // Arrange
            var buffer = new FrameBuffer(3);

            // Act
            buffer.Enqueue(CreateTestFrame());
            buffer.Enqueue(CreateTestFrame());
            buffer.Enqueue(CreateTestFrame());

            // Assert
            buffer.Count.Should().Be(3);
            buffer.IsFull.Should().BeTrue();
        }

        [Fact]
        public void Enqueue_DropsOldestFrameWhenFull()
        {
            // Arrange
            var buffer = new FrameBuffer(2);
            buffer.Enqueue(CreateTestFrame());
            buffer.Enqueue(CreateTestFrame());

            // Act
            var droppedFrames = buffer.Enqueue(CreateTestFrame());

            // Assert
            buffer.Count.Should().Be(2);
            buffer.IsFull.Should().BeTrue();
            droppedFrames.Should().Be(1);
        }

        [Fact]
        public void TryDequeue_ReturnsFrameWhenAvailable()
        {
            // Arrange
            var buffer = new FrameBuffer(5);
            var frame = CreateTestFrame();
            buffer.Enqueue(frame);

            // Act
            var success = buffer.TryDequeue(out var dequeuedFrame);

            // Assert
            success.Should().BeTrue();
            dequeuedFrame.Should().NotBeNull();
            dequeuedFrame!.Width.Should().Be(frame.Width);
            buffer.Count.Should().Be(0);
        }

        [Fact]
        public void TryDequeue_ReturnsFalseWhenEmpty()
        {
            // Arrange
            var buffer = new FrameBuffer(5);

            // Act
            var success = buffer.TryDequeue(out var frame);

            // Assert
            success.Should().BeFalse();
            frame.Should().BeNull();
        }

        [Fact]
        public void TryPeek_ReturnsFrameWithoutRemoving()
        {
            // Arrange
            var buffer = new FrameBuffer(5);
            var frame = CreateTestFrame();
            buffer.Enqueue(frame);

            // Act
            var success = buffer.TryPeek(out var peekedFrame);

            // Assert
            success.Should().BeTrue();
            peekedFrame.Should().NotBeNull();
            buffer.Count.Should().Be(1);
        }

        [Fact]
        public void TryPeek_ReturnsFalseWhenEmpty()
        {
            // Arrange
            var buffer = new FrameBuffer(5);

            // Act
            var success = buffer.TryPeek(out var frame);

            // Assert
            success.Should().BeFalse();
            frame.Should().BeNull();
        }

        [Fact]
        public void Clear_RemovesAllFrames()
        {
            // Arrange
            var buffer = new FrameBuffer(5);
            buffer.Enqueue(CreateTestFrame());
            buffer.Enqueue(CreateTestFrame());
            buffer.Enqueue(CreateTestFrame());

            // Act
            buffer.Clear();

            // Assert
            buffer.Count.Should().Be(0);
            buffer.IsEmpty.Should().BeTrue();
        }

        [Fact]
        public void CircularQueue_WrapsAround()
        {
            // Arrange
            var buffer = new FrameBuffer(3);

            // Act & Assert
            for (int i = 0; i < 10; i++)
            {
                buffer.Enqueue(CreateTestFrame());
                buffer.Should().HaveLessOrEqualTo(3).Items();
            }

            buffer.Count.Should().Be(3);
        }

        [Fact]
        public void FIFO_OrderIsPreserved()
        {
            // Arrange
            var buffer = new FrameBuffer(5);
            var frame1 = CreateTestFrame(100, 100);
            var frame2 = CreateTestFrame(200, 200);
            var frame3 = CreateTestFrame(300, 300);

            // Act
            buffer.Enqueue(frame1);
            buffer.Enqueue(frame2);
            buffer.Enqueue(frame3);

            // Assert
            buffer.TryDequeue(out var first);
            first!.Width.Should().Be(100);

            buffer.TryDequeue(out var second);
            second!.Width.Should().Be(200);

            buffer.TryDequeue(out var third);
            third!.Width.Should().Be(300);
        }

        [Fact]
        public void TotalFramesDropped_TracksDroppedFrames()
        {
            // Arrange
            var buffer = new FrameBuffer(2);

            // Act
            buffer.Enqueue(CreateTestFrame());
            buffer.Enqueue(CreateTestFrame());
            buffer.Enqueue(CreateTestFrame());
            buffer.Enqueue(CreateTestFrame());

            // Assert
            buffer.TotalFramesDropped.Should().Be(2);
        }

        [Fact]
        public void Dispose_DisposesAllFrames()
        {
            // Arrange
            var buffer = new FrameBuffer(3);
            buffer.Enqueue(CreateTestFrame());
            buffer.Enqueue(CreateTestFrame());

            // Act & Assert
            buffer.Invoking(b => b.Dispose()).Should().NotThrow();
            buffer.Count.Should().Be(0);
        }

        [Fact]
        public void ThreadSafety_ConcurrentEnqueueDequeue()
        {
            // Arrange
            var buffer = new FrameBuffer(10);
            var enqueueCount = 0;
            var dequeueCount = 0;
            var exception = (Exception?)null;

            // Act
            var enqueueTask = Task.Run(() =>
            {
                try
                {
                    for (int i = 0; i < 100; i++)
                    {
                        buffer.Enqueue(CreateTestFrame());
                        Interlocked.Increment(ref enqueueCount);
                    }
                }
                catch (Exception ex)
                {
                    Interlocked.Exchange(ref exception, ex);
                }
            });

            var dequeueTask = Task.Run(() =>
            {
                try
                {
                    while (Interlocked.Read(ref dequeueCount) < 100)
                    {
                        if (buffer.TryDequeue(out var frame))
                        {
                            frame?.Dispose();
                            Interlocked.Increment(ref dequeueCount);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Interlocked.Exchange(ref exception, ex);
                }
            });

            Task.WaitAll(enqueueTask, dequeueTask);

            // Assert
            exception.Should().BeNull();
            enqueueCount.Should().Be(100);
            dequeueCount.Should().Be(100);
        }

        [Fact]
        public void IsFull_CorrectlyIndicatesBufferState()
        {
            // Arrange
            var buffer = new FrameBuffer(2);

            // Act & Assert
            buffer.IsFull.Should().BeFalse();

            buffer.Enqueue(CreateTestFrame());
            buffer.IsFull.Should().BeFalse();

            buffer.Enqueue(CreateTestFrame());
            buffer.IsFull.Should().BeTrue();

            buffer.TryDequeue(out _);
            buffer.IsFull.Should().BeFalse();
        }

        [Fact]
        public void IsEmpty_CorrectlyIndicatesBufferState()
        {
            // Arrange
            var buffer = new FrameBuffer(3);

            // Act & Assert
            buffer.IsEmpty.Should().BeTrue();

            buffer.Enqueue(CreateTestFrame());
            buffer.IsEmpty.Should().BeFalse();

            buffer.TryDequeue(out _);
            buffer.IsEmpty.Should().BeTrue();
        }

        [Fact]
        public void EnqueueDequeue_HighThroughput()
        {
            // Arrange
            var buffer = new FrameBuffer(5);
            const int operationCount = 1000;

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < operationCount; i++)
            {
                buffer.Enqueue(CreateTestFrame());
                buffer.TryDequeue(out _);
            }
            stopwatch.Stop();

            // Assert
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // Should complete quickly
        }
    }
}