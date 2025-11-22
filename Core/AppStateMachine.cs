using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace TarkovBuddy.Core
{
    /// <summary>
    /// Manages the application's game state transitions and notifies subscribers of state changes.
    /// Thread-safe implementation using lock-free design patterns where possible.
    /// </summary>
    public class AppStateMachine : IDisposable
    {
        private readonly ILogger<AppStateMachine> _logger;
        private readonly StateTransitionRules _transitionRules;
        
        private GameState _currentState;
        private readonly object _stateLock = new object();
        private readonly ConcurrentDictionary<string, List<Action<GameState, GameState>>> _stateChangeSubscribers;

        /// <summary>
        /// Raised when the game state changes.
        /// </summary>
        public event EventHandler<StateChangedEventArgs>? StateChanged;

        /// <summary>
        /// Gets the current game state.
        /// </summary>
        public GameState CurrentState
        {
            get
            {
                lock (_stateLock)
                {
                    return _currentState;
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the AppStateMachine class.
        /// </summary>
        public AppStateMachine(ILogger<AppStateMachine> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _transitionRules = new StateTransitionRules();
            _currentState = GameState.InLauncher;
            _stateChangeSubscribers = new ConcurrentDictionary<string, List<Action<GameState, GameState>>>();
            
            _logger.LogInformation("AppStateMachine initialized with initial state: {InitialState}", _currentState);
        }

        /// <summary>
        /// Attempts to transition to the specified state.
        /// Only valid transitions (defined in StateTransitionRules) are allowed.
        /// </summary>
        public bool TransitionTo(GameState newState)
        {
            lock (_stateLock)
            {
                if (_currentState == newState)
                {
                    _logger.LogDebug("Already in state {CurrentState}, ignoring transition", newState);
                    return true;
                }

                if (!_transitionRules.IsTransitionValid(_currentState, newState))
                {
                    _logger.LogWarning("Invalid state transition: {FromState} -> {ToState}", _currentState, newState);
                    return false;
                }

                GameState previousState = _currentState;
                _currentState = newState;

                _logger.LogInformation("State transition: {FromState} -> {ToState}", previousState, newState);

                // Notify subscribers
                OnStateChanged(previousState, newState);

                return true;
            }
        }

        /// <summary>
        /// Registers a callback to be invoked when transitioning between two specific states.
        /// </summary>
        public void Subscribe(string subscriberId, Action<GameState, GameState> callback)
        {
            if (string.IsNullOrEmpty(subscriberId))
            {
                throw new ArgumentException("Subscriber ID cannot be null or empty", nameof(subscriberId));
            }

            _stateChangeSubscribers.AddOrUpdate(
                subscriberId,
                new List<Action<GameState, GameState>> { callback },
                (key, existingList) =>
                {
                    existingList.Add(callback);
                    return existingList;
                });

            _logger.LogDebug("Subscriber '{SubscriberId}' registered for state changes", subscriberId);
        }

        /// <summary>
        /// Unregisters a previously registered callback.
        /// </summary>
        public void Unsubscribe(string subscriberId)
        {
            if (_stateChangeSubscribers.TryRemove(subscriberId, out _))
            {
                _logger.LogDebug("Subscriber '{SubscriberId}' unregistered from state changes", subscriberId);
            }
        }

        /// <summary>
        /// Resets the state machine to the initial state (InLauncher).
        /// </summary>
        public void Reset()
        {
            lock (_stateLock)
            {
                GameState previousState = _currentState;
                _currentState = GameState.InLauncher;
                _logger.LogInformation("State machine reset: {FromState} -> {ToState}", previousState, _currentState);
                OnStateChanged(previousState, GameState.InLauncher);
            }
        }

        /// <summary>
        /// Determines if a transition from the current state to the specified state is valid.
        /// </summary>
        public bool CanTransitionTo(GameState targetState)
        {
            lock (_stateLock)
            {
                return _transitionRules.IsTransitionValid(_currentState, targetState);
            }
        }

        /// <summary>
        /// Gets all valid next states from the current state.
        /// </summary>
        public IEnumerable<GameState> GetValidNextStates()
        {
            lock (_stateLock)
            {
                return _transitionRules.GetValidTransitions(_currentState);
            }
        }

        private void OnStateChanged(GameState previousState, GameState newState)
        {
            StateChanged?.Invoke(this, new StateChangedEventArgs(previousState, newState));

            // Invoke all registered callbacks
            foreach (var subscriberCallbacks in _stateChangeSubscribers.Values)
            {
                foreach (var callback in subscriberCallbacks)
                {
                    try
                    {
                        callback.Invoke(previousState, newState);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error invoking state change callback");
                    }
                }
            }
        }

        public void Dispose()
        {
            _stateChangeSubscribers.Clear();
            StateChanged = null;
        }
    }

    /// <summary>
    /// Event arguments for state change notifications.
    /// </summary>
    public class StateChangedEventArgs : EventArgs
    {
        public GameState PreviousState { get; }
        public GameState NewState { get; }

        public StateChangedEventArgs(GameState previousState, GameState newState)
        {
            PreviousState = previousState;
            NewState = newState;
        }
    }
}