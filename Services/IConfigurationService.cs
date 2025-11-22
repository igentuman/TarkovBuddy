namespace TarkovBuddy.Services
{
    /// <summary>
    /// Interface for application configuration management.
    /// Handles loading, storing, and accessing configuration settings.
    /// </summary>
    public interface IConfigurationService : IService
    {
        /// <summary>
        /// Gets a configuration value by key.
        /// Returns null if the key doesn't exist.
        /// </summary>
        T? GetSetting<T>(string key);

        /// <summary>
        /// Gets a configuration value by key with a default fallback value.
        /// </summary>
        T GetSetting<T>(string key, T defaultValue);

        /// <summary>
        /// Sets a configuration value.
        /// </summary>
        void SetSetting<T>(string key, T value);

        /// <summary>
        /// Checks if a configuration key exists.
        /// </summary>
        bool HasSetting(string key);

        /// <summary>
        /// Loads configuration from the app-settings.json file.
        /// </summary>
        Task LoadConfigurationAsync();

        /// <summary>
        /// Saves current configuration to the app-settings.json file.
        /// </summary>
        Task SaveConfigurationAsync();

        /// <summary>
        /// Gets all configuration keys.
        /// </summary>
        IEnumerable<string> GetAllKeys();

        /// <summary>
        /// Clears all cached configuration (forces reload from file).
        /// </summary>
        void ClearCache();

        /// <summary>
        /// Resets all settings to default values.
        /// </summary>
        void ResetToDefaults();
    }
}