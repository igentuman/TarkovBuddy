using TarkovBuddy.Models;

namespace TarkovBuddy.Services
{
    /// <summary>
    /// Thread-safe circular buffer for storing captured frames.
    /// Maintains a fixed-size queue to prevent unbounded memory growth.
    /// </summary>
    public class FrameBuffer : IDisposable
    {
        private readonly Frame[] _buffer;
        private readonly int _capacity;
        private int _head;
        private int _tail;
        private int _count;
        private int _totalFramesDropped;
        private readonly object _lockObject = new();

        /// <summary>
        /// Creates a new frame buffer with specified capacity.
        /// </summary>
        /// <param name="capacity">Maximum number of frames to buffer (default: 5).</param>
        /// <exception cref="ArgumentException">Thrown if capacity is less than 1.</exception>
        public FrameBuffer(int capacity = 5)
        {
            if (capacity < 1)
                throw new ArgumentException("Capacity must be at least 1", nameof(capacity));

            _capacity = capacity;
            _buffer = new Frame[capacity];
            _head = 0;
            _tail = 0;
            _count = 0;
            _totalFramesDropped = 0;
        }

        /// <summary>
        /// Gets the current number of frames in the buffer.
        /// </summary>
        public int Count
        {
            get
            {
                lock (_lockObject)
                {
                    return _count;
                }
            }
        }

        /// <summary>
        /// Gets the capacity of the buffer.
        /// </summary>
        public int Capacity => _capacity;

        /// <summary>
        /// Gets the total number of frames dropped due to buffer overflow.
        /// </summary>
        public int TotalFramesDropped
        {
            get
            {
                lock (_lockObject)
                {
                    return _totalFramesDropped;
                }
            }
        }

        /// <summary>
        /// Gets whether the buffer is full.
        /// </summary>
        public bool IsFull
        {
            get
            {
                lock (_lockObject)
                {
                    return _count >= _capacity;
                }
            }
        }

        /// <summary>
        /// Gets whether the buffer is empty.
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                lock (_lockObject)
                {
                    return _count == 0;
                }
            }
        }

        /// <summary>
        /// Enqueues a frame into the buffer.
        /// If buffer is full, the oldest frame is discarded.
        /// </summary>
        /// <param name="frame">Frame to enqueue.</param>
        /// <returns>Number of frames dropped (0 or 1).</returns>
        public int Enqueue(Frame frame)
        {
            if (frame == null)
                throw new ArgumentNullException(nameof(frame));

            lock (_lockObject)
            {
                int droppedFrames = 0;

                // If buffer is full, drop the oldest frame
                if (_count >= _capacity)
                {
                    var oldFrame = _buffer[_tail];
                    oldFrame?.Dispose();

                    _tail = (_tail + 1) % _capacity;
                    _totalFramesDropped++;
                    droppedFrames = 1;
                }
                else
                {
                    _count++;
                }

                _buffer[_head] = frame;
                _head = (_head + 1) % _capacity;

                return droppedFrames;
            }
        }

        /// <summary>
        /// Dequeues and returns the next frame from the buffer.
        /// </summary>
        /// <param name="frame">The dequeued frame, or null if buffer is empty.</param>
        /// <returns>True if a frame was dequeued, false if buffer is empty.</returns>
        public bool TryDequeue(out Frame? frame)
        {
            frame = null;

            lock (_lockObject)
            {
                if (_count == 0)
                    return false;

                frame = _buffer[_tail];
                _buffer[_tail] = null!;
                _tail = (_tail + 1) % _capacity;
                _count--;

                return true;
            }
        }

        /// <summary>
        /// Peeks at the next frame without removing it.
        /// </summary>
        /// <param name="frame">The next frame, or null if buffer is empty.</param>
        /// <returns>True if a frame is available, false if buffer is empty.</returns>
        public bool TryPeek(out Frame? frame)
        {
            frame = null;

            lock (_lockObject)
            {
                if (_count == 0)
                    return false;

                frame = _buffer[_tail];
                return true;
            }
        }

        /// <summary>
        /// Clears all frames from the buffer.
        /// </summary>
        public void Clear()
        {
            lock (_lockObject)
            {
                for (int i = 0; i < _capacity; i++)
                {
                    _buffer[i]?.Dispose();
                    _buffer[i] = null!;
                }

                _head = 0;
                _tail = 0;
                _count = 0;
            }
        }

        /// <summary>
        /// Disposes all frames in the buffer.
        /// </summary>
        public void Dispose()
        {
            Clear();
            GC.SuppressFinalize(this);
        }
    }
}