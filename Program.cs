using System.Windows;

namespace TarkovBuddy
{
    /// <summary>
    /// Program entry point with proper exception handling.
    /// </summary>
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
                var app = new App();
                
                // Handle any unhandled exceptions in the dispatcher
                app.DispatcherUnhandledException += (sender, e) =>
                {
                    MessageBox.Show(
                        $"An unhandled exception occurred:\n\n{e.Exception.Message}\n\n{e.Exception.StackTrace}",
                        "Application Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    e.Handled = true;
                };
                
                // Handle any unhandled exceptions in other threads
                AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
                {
                    var ex = e.ExceptionObject as Exception;
                    MessageBox.Show(
                        $"A critical error occurred:\n\n{ex?.Message}\n\n{ex?.StackTrace}",
                        "Critical Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                };

                app.Run();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to start TarkovBuddy:\n\n{ex.Message}\n\n{ex.StackTrace}",
                    "Startup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Environment.Exit(1);
            }
        }
    }
}