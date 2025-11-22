using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using TarkovBuddy.Core;
using TarkovBuddy.Services;
using System.IO;
using System.Windows;

namespace TarkovBuddy
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// Application entry point with dependency injection setup.
    /// </summary>
    public partial class App : Application
    {
        private static IHost? _host;

        /// <summary>
        /// Gets the application's dependency injection host.
        /// </summary>
        public static IHost Host => _host ?? throw new InvalidOperationException("Host not initialized");

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                // Build and configure the host
                _host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder([])
                    .ConfigureAppConfiguration((context, config) =>
                    {
                        config
                            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                            .AddJsonFile("Config/app-settings.json", optional: false, reloadOnChange: true)
                            .AddEnvironmentVariables();
                    })
                    .ConfigureServices((context, services) =>
                    {
                        // Register configuration service
                        services.AddSingleton<IConfigurationService, ConfigurationService>();

                        // Register core infrastructure
                        services.AddSingleton<AppStateMachine>();
                    })
                    .UseSerilog((context, configuration) =>
                    {
                        var logPath = context.Configuration["Logging:FilePath"] ?? "logs/tarkov-buddy.log";
                        var logDirectory = Path.GetDirectoryName(logPath);
                        
                        if (!string.IsNullOrEmpty(logDirectory) && !Directory.Exists(logDirectory))
                        {
                            Directory.CreateDirectory(logDirectory);
                        }

                        configuration
                            .MinimumLevel.Information()
                            .WriteTo.Console(
                                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                            .WriteTo.File(
                                logPath,
                                rollingInterval: RollingInterval.Day,
                                fileSizeLimitBytes: 104857600, // 100 MB
                                retainedFileCountLimit: 10,
                                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                            .Enrich.FromLogContext()
                            .Enrich.WithEnvironmentUserName()
                            .Enrich.WithThreadId();
                    })
                    .Build();

                // Initialize services
                var configService = _host.Services.GetRequiredService<IConfigurationService>();
                configService.InitializeAsync().Wait();

                var logger = _host.Services.GetRequiredService<ILogger<App>>();
                logger.LogInformation("TarkovBuddy v1.0.0.0 starting...");

                // Create and show main window
                MainWindow = new MainWindow();
                MainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to start TarkovBuddy:\n\n{ex.Message}\n\n{ex.StackTrace}",
                    "Startup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Shutdown(1);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                _host?.Dispose();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error during shutdown: {ex.Message}");
            }

            base.OnExit(e);
        }
    }
}