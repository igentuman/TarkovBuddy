using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.IO;

namespace TarkovBuddy.Services
{
    /// <summary>
    /// Default implementation of IConfigurationService.
    /// Manages application configuration loaded from app-settings.json.
    /// Thread-safe using concurrent collections.
    /// </summary>
    public class ConfigurationService : IConfigurationService
    {
        private readonly ILogger<ConfigurationService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _configPath;
        private readonly ConcurrentDictionary<string, object?> _cache;
        private bool _isRunning;

        public string ServiceName => "ConfigurationService";
        public bool IsRunning => _isRunning;

        public ConfigurationService(ILogger<ConfigurationService> logger, IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _cache = new ConcurrentDictionary<string, object?>();
            
            // Determine config path
            var execPath = AppDomain.CurrentDomain.BaseDirectory;
            _configPath = Path.Combine(execPath, "Config", "app-settings.json");
            
            _isRunning = false;
        }

        public async Task InitializeAsync()
        {
            try
            {
                await LoadConfigurationAsync();
                _isRunning = true;
                _logger.LogInformation("ConfigurationService initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize ConfigurationService");
                throw;
            }
        }

        public T? GetSetting<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            // Check cache first
            if (_cache.TryGetValue(key, out var cachedValue))
            {
                return (T?)cachedValue;
            }

            // Try to get from configuration
            var value = _configuration[key];
            
            if (value == null)
            {
                return default;
            }

            try
            {
                // Convert string value to requested type
                var converted = Convert.ChangeType(value, typeof(T));
                _cache.TryAdd(key, converted);
                return (T?)converted;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to convert configuration value for key '{Key}'", key);
                return default;
            }
        }

        public T GetSetting<T>(string key, T defaultValue)
        {
            var value = GetSetting<T>(key);
            return value ?? defaultValue;
        }

        public void SetSetting<T>(string key, T value)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key cannot be null or empty", nameof(key));
            }

            _cache.AddOrUpdate(key, value, (k, oldValue) => value);
            _logger.LogDebug("Configuration setting '{Key}' set to {Value}", key, value);
        }

        public bool HasSetting(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            return _cache.ContainsKey(key) || !string.IsNullOrEmpty(_configuration[key]);
        }

        public async Task LoadConfigurationAsync()
        {
            try
            {
                if (!File.Exists(_configPath))
                {
                    _logger.LogWarning("Configuration file not found at {ConfigPath}, using defaults", _configPath);
                    CreateDefaultConfiguration();
                    return;
                }

                string jsonContent = await File.ReadAllTextAsync(_configPath);
                var jObject = JObject.Parse(jsonContent);

                _cache.Clear();
                FlattenJsonAndCache(jObject);

                _logger.LogInformation("Configuration loaded successfully from {ConfigPath}", _configPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load configuration from {ConfigPath}", _configPath);
                throw;
            }
        }

        public async Task SaveConfigurationAsync()
        {
            try
            {
                // Create directory if it doesn't exist
                var configDirectory = Path.GetDirectoryName(_configPath);
                if (!string.IsNullOrEmpty(configDirectory) && !Directory.Exists(configDirectory))
                {
                    Directory.CreateDirectory(configDirectory);
                }

                // Build JSON object from cache
                var jObject = new JObject();
                foreach (var kvp in _cache)
                {
                    var keys = kvp.Key.Split(':');
                    JObject current = jObject;

                    for (int i = 0; i < keys.Length - 1; i++)
                    {
                        if (current[keys[i]] == null)
                        {
                            current[keys[i]] = new JObject();
                        }
                        current = (JObject)current[keys[i]]!;
                    }

                    current[keys[^1]] = JToken.FromObject(kvp.Value!);
                }

                string jsonContent = jObject.ToString();
                await File.WriteAllTextAsync(_configPath, jsonContent);

                _logger.LogInformation("Configuration saved successfully to {ConfigPath}", _configPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save configuration to {ConfigPath}", _configPath);
                throw;
            }
        }

        public IEnumerable<string> GetAllKeys()
        {
            return _cache.Keys;
        }

        public void ClearCache()
        {
            _cache.Clear();
            _logger.LogDebug("Configuration cache cleared");
        }

        public void ResetToDefaults()
        {
            _cache.Clear();
            CreateDefaultConfiguration();
            _logger.LogInformation("Configuration reset to defaults");
        }

        private void FlattenJsonAndCache(JObject jObject, string prefix = "")
        {
            foreach (var property in jObject.Properties())
            {
                string key = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}:{property.Name}";

                if (property.Value is JObject nestedObject)
                {
                    FlattenJsonAndCache(nestedObject, key);
                }
                else
                {
                    _cache.TryAdd(key, property.Value?.Value<object>());
                }
            }
        }

        private void CreateDefaultConfiguration()
        {
            var defaults = new Dictionary<string, object>
            {
                { "Application:Name", "TarkovBuddy" },
                { "Application:Version", "1.0.0.0" },
                { "Application:LogLevel", "Information" },
                { "Capture:TargetFPS", 30 },
                { "Capture:MaxLatencyMs", 50 },
                { "Modules:Enabled", true },
                { "Overlay:Enabled", true },
                { "Logging:FilePath", "logs/tarkov-buddy.log" }
            };

            foreach (var kvp in defaults)
            {
                _cache.TryAdd(kvp.Key, kvp.Value);
            }

            _logger.LogInformation("Default configuration created");
        }

        public void Dispose()
        {
            _cache.Clear();
            _isRunning = false;
            GC.SuppressFinalize(this);
        }
    }
}