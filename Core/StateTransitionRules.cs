namespace TarkovBuddy.Core
{
    /// <summary>
    /// Defines the valid state transitions in the application.
    /// This ensures the state machine only transitions through logically valid sequences.
    /// </summary>
    public class StateTransitionRules
    {
        private static readonly Dictionary<GameState, HashSet<GameState>> ValidTransitions = new()
        {
            // From InLauncher: can go to InLobby (user starts game)
            {
                GameState.InLauncher,
                new HashSet<GameState>
                {
                    GameState.InLobby
                }
            },

            // From InLobby: can go to InHideout (user accesses hideout) or LoadingRaid (user starts a raid)
            {
                GameState.InLobby,
                new HashSet<GameState>
                {
                    GameState.InHideout,
                    GameState.LoadingRaid,
                    GameState.FleaMarketScreen,
                    GameState.InLauncher
                }
            },

            // From InHideout: can go to InLobby (user exits hideout) or LoadingRaid
            {
                GameState.InHideout,
                new HashSet<GameState>
                {
                    GameState.InLobby,
                    GameState.LoadingRaid,
                    GameState.InLauncher
                }
            },

            // From LoadingRaid: can go to InRaid (loading complete) or back to InLobby (loading cancelled)
            {
                GameState.LoadingRaid,
                new HashSet<GameState>
                {
                    GameState.InRaid,
                    GameState.InLobby,
                    GameState.InLauncher
                }
            },

            // From InRaid: can go to Extracted, Died, InventoryScreen, or InLauncher (game closed)
            {
                GameState.InRaid,
                new HashSet<GameState>
                {
                    GameState.Extracted,
                    GameState.Died,
                    GameState.InventoryScreen,
                    GameState.InLauncher
                }
            },

            // From Extracted: can go to InLobby (raid end screen) or InLauncher
            {
                GameState.Extracted,
                new HashSet<GameState>
                {
                    GameState.InLobby,
                    GameState.InLauncher
                }
            },

            // From Died: can go to InLobby or InLauncher
            {
                GameState.Died,
                new HashSet<GameState>
                {
                    GameState.InLobby,
                    GameState.InLauncher
                }
            },

            // From InventoryScreen: can go back to InRaid or InLobby depending on context
            {
                GameState.InventoryScreen,
                new HashSet<GameState>
                {
                    GameState.InRaid,
                    GameState.InLobby,
                    GameState.InHideout,
                    GameState.InLauncher
                }
            },

            // From FleaMarketScreen: can go to InLobby or InLauncher
            {
                GameState.FleaMarketScreen,
                new HashSet<GameState>
                {
                    GameState.InLobby,
                    GameState.InLauncher
                }
            }
        };

        /// <summary>
        /// Determines if a transition from one state to another is valid.
        /// </summary>
        public bool IsTransitionValid(GameState fromState, GameState toState)
        {
            if (!ValidTransitions.TryGetValue(fromState, out var allowedTransitions))
            {
                return false;
            }

            return allowedTransitions.Contains(toState);
        }

        /// <summary>
        /// Gets all valid transitions from the specified state.
        /// </summary>
        public IEnumerable<GameState> GetValidTransitions(GameState fromState)
        {
            if (ValidTransitions.TryGetValue(fromState, out var transitions))
            {
                return transitions;
            }

            return Enumerable.Empty<GameState>();
        }

        /// <summary>
        /// Gets a human-readable description of all valid state transitions.
        /// </summary>
        public string GetTransitionMap()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== Valid State Transitions ===");

            foreach (var kvp in ValidTransitions.OrderBy(x => x.Key))
            {
                sb.AppendLine($"{kvp.Key} -> [{string.Join(", ", kvp.Value.OrderBy(x => x))}]");
            }

            return sb.ToString();
        }
    }
}