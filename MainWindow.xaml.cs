using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Windows;
using TarkovBuddy.Core;

namespace TarkovBuddy
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// Main application window with state machine integration.
    /// </summary>
    public partial class MainWindow : Window
    {
        private AppStateMachine? _stateMachine;
        private ILogger<MainWindow>? _logger;

        public MainWindow()
        {
            Console.WriteLine("MainWindow: Constructor called, calling InitializeComponent...");
            InitializeComponent();
            Console.WriteLine("MainWindow: InitializeComponent completed");

            try
            {
                // Get dependencies from the global app host
                Console.WriteLine("MainWindow: Getting service provider...");
                var serviceProvider = App.Host.Services;
                Console.WriteLine("MainWindow: Service provider obtained");
                
                Console.WriteLine("MainWindow: Getting AppStateMachine service...");
                _stateMachine = serviceProvider.GetRequiredService<AppStateMachine>();
                Console.WriteLine("MainWindow: AppStateMachine obtained");
                
                Console.WriteLine("MainWindow: Getting logger service...");
                _logger = serviceProvider.GetRequiredService<ILogger<MainWindow>>();
                Console.WriteLine("MainWindow: Logger obtained");

                // Subscribe to state changes
                Console.WriteLine("MainWindow: Subscribing to StateChanged event...");
                _stateMachine.StateChanged += OnStateChanged;
                Console.WriteLine("MainWindow: Event subscribed");

                // Display initial state
                Console.WriteLine("MainWindow: Updating state display...");
                UpdateStateDisplay();
                Console.WriteLine("MainWindow: State display updated");

                _logger.LogInformation("MainWindow initialized");
                Console.WriteLine("MainWindow: Initialization completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MainWindow: Exception in constructor: {ex.Message}");
                Console.WriteLine($"MainWindow: StackTrace: {ex.StackTrace}");
                MessageBox.Show(
                    $"Failed to initialize MainWindow:\n\n{ex.Message}\n\n{ex.StackTrace}",
                    "Initialization Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Close();
            }
        }

        /// <summary>
        /// Updates the UI to reflect the current game state.
        /// </summary>
        private void UpdateStateDisplay()
        {
            if (_stateMachine != null)
            {
                CurrentStateText.Text = _stateMachine.CurrentState.ToString();
                StatusText.Text = $"Status: Monitoring game state - Current: {_stateMachine.CurrentState}";
            }
        }

        /// <summary>
        /// Handles state change notifications from the state machine.
        /// </summary>
        private void OnStateChanged(object? sender, StateChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (_logger != null)
                {
                    _logger.LogInformation("State changed: {PreviousState} -> {NewState}", e.PreviousState, e.NewState);
                }
                UpdateStateDisplay();
            });
        }

        protected override void OnClosed(EventArgs e)
        {
            if (_stateMachine != null)
            {
                _stateMachine.StateChanged -= OnStateChanged;
                _stateMachine.Dispose();
            }
            base.OnClosed(e);
        }
    }
}