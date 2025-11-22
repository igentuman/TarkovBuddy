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
            InitializeComponent();

            try
            {
                // Get dependencies from the global app host
                var serviceProvider = App.Host.Services;
                _stateMachine = serviceProvider.GetRequiredService<AppStateMachine>();
                _logger = serviceProvider.GetRequiredService<ILogger<MainWindow>>();

                // Subscribe to state changes
                _stateMachine.StateChanged += OnStateChanged;

                // Display initial state
                UpdateStateDisplay();

                _logger.LogInformation("MainWindow initialized");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to initialize MainWindow:\n\n{ex.Message}",
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