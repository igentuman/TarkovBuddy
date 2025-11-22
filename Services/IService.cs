namespace TarkovBuddy.Services
{
    /// <summary>
    /// Base interface for all services in the application.
    /// Provides common lifecycle management for all services.
    /// </summary>
    public interface IService : IDisposable
    {
        /// <summary>
        /// Gets the name of the service.
        /// </summary>
        string ServiceName { get; }

        /// <summary>
        /// Initializes the service asynchronously.
        /// This is called after dependency injection but before the service is used.
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Determines if the service is currently running.
        /// </summary>
        bool IsRunning { get; }
    }
}