using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace TarkovBuddy.Services
{
    /// <summary>
    /// Manages global hotkey registration and handling for the overlay system.
    /// Supports multiple hotkeys for different overlay features.
    /// </summary>
    public class HotkeyManager : IDisposable
    {
        private readonly ILogger<HotkeyManager> _logger;
        private readonly ConcurrentDictionary<int, Func<Task>> _hotkeyHandlers;
        private nint _windowHandle;
        private int _nextHotkeyId = 1;
        private bool _isInitialized;
        private bool _disposed;

        // Win32 constants
        private const int WM_HOTKEY = 0x0312;
        private const int MOD_ALT = 0x0001;
        private const int MOD_CTRL = 0x0002;
        private const int MOD_SHIFT = 0x0004;
        private const int VK_M = 0x4D;
        private const int VK_Q = 0x51;
        private const int VK_L = 0x4C;
        private const int VK_S = 0x53;
        private const int VK_R = 0x52;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(nint hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(nint hWnd, int id);

        /// <summary>
        /// Initializes a new instance of the HotkeyManager class.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        public HotkeyManager(ILogger<HotkeyManager> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _hotkeyHandlers = new ConcurrentDictionary<int, Func<Task>>();
            _isInitialized = false;
        }

        /// <summary>
        /// Initializes the hotkey manager with a window handle.
        /// </summary>
        /// <param name="windowHandle">Handle to the window that will receive hotkey messages.</param>
        public void Initialize(nint windowHandle)
        {
            if (_isInitialized)
                return;

            _windowHandle = windowHandle;
            _isInitialized = true;
            _logger.LogInformation("HotkeyManager initialized");
        }

        /// <summary>
        /// Registers a hotkey combination with a handler.
        /// </summary>
        /// <param name="modifiers">Key modifiers (Alt, Ctrl, Shift).</param>
        /// <param name="virtualKey">Virtual key code.</param>
        /// <param name="handler">Async handler to invoke when hotkey is pressed.</param>
        /// <returns>Hotkey ID for later unregistration.</returns>
        public int RegisterHotkey(int modifiers, int virtualKey, Func<Task> handler)
        {
            if (!_isInitialized)
                throw new InvalidOperationException("HotkeyManager not initialized");

            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            try
            {
                int hotkeyId = _nextHotkeyId++;
                
                if (RegisterHotKey(_windowHandle, hotkeyId, modifiers, virtualKey))
                {
                    _hotkeyHandlers.TryAdd(hotkeyId, handler);
                    _logger.LogInformation("Registered hotkey ID={HotkeyId} with modifiers={Modifiers} and key={Key}",
                        hotkeyId, modifiers, virtualKey);
                    return hotkeyId;
                }
                else
                {
                    _logger.LogWarning("Failed to register hotkey with modifiers={Modifiers} and key={Key}",
                        modifiers, virtualKey);
                    return -1;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering hotkey");
                return -1;
            }
        }

        /// <summary>
        /// Registers default hotkeys for overlay features.
        /// </summary>
        /// <param name="mapOverlayHandler">Handler for map overlay hotkey (Alt+M).</param>
        /// <param name="questOverlayHandler">Handler for quest overlay hotkey (Alt+Q).</param>
        /// <param name="lootOverlayHandler">Handler for loot evaluator hotkey (Alt+L).</param>
        /// <param name="stashOverlayHandler">Handler for stash stats hotkey (Alt+S).</param>
        /// <param name="refreshHandler">Handler for refresh capture hotkey (Alt+R).</param>
        public void RegisterDefaultHotkeys(
            Func<Task> mapOverlayHandler,
            Func<Task> questOverlayHandler,
            Func<Task> lootOverlayHandler,
            Func<Task> stashOverlayHandler,
            Func<Task> refreshHandler)
        {
            // Alt+M - Toggle Map Overlay
            RegisterHotkey(MOD_ALT, VK_M, mapOverlayHandler ?? (() => Task.CompletedTask));

            // Alt+Q - Toggle Quest List
            RegisterHotkey(MOD_ALT, VK_Q, questOverlayHandler ?? (() => Task.CompletedTask));

            // Alt+L - Toggle Loot Helper
            RegisterHotkey(MOD_ALT, VK_L, lootOverlayHandler ?? (() => Task.CompletedTask));

            // Alt+S - Toggle Stash Stats
            RegisterHotkey(MOD_ALT, VK_S, stashOverlayHandler ?? (() => Task.CompletedTask));

            // Alt+R - Refresh Capture
            RegisterHotkey(MOD_ALT, VK_R, refreshHandler ?? (() => Task.CompletedTask));

            _logger.LogInformation("Default hotkeys registered (Alt+M, Alt+Q, Alt+L, Alt+S, Alt+R)");
        }

        /// <summary>
        /// Unregisters a previously registered hotkey.
        /// </summary>
        /// <param name="hotkeyId">The hotkey ID returned from RegisterHotkey.</param>
        public void UnregisterHotkey(int hotkeyId)
        {
            try
            {
                if (UnregisterHotKey(_windowHandle, hotkeyId))
                {
                    _hotkeyHandlers.TryRemove(hotkeyId, out _);
                    _logger.LogInformation("Unregistered hotkey ID={HotkeyId}", hotkeyId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unregistering hotkey ID={HotkeyId}", hotkeyId);
            }
        }

        /// <summary>
        /// Invokes a registered hotkey handler.
        /// Called when a hotkey message is received.
        /// </summary>
        /// <param name="hotkeyId">The ID of the hotkey that was pressed.</param>
        public async Task InvokeHotkeyAsync(int hotkeyId)
        {
            if (_hotkeyHandlers.TryGetValue(hotkeyId, out var handler))
            {
                try
                {
                    await handler();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error invoking hotkey handler for ID={HotkeyId}", hotkeyId);
                }
            }
        }

        /// <summary>
        /// Disposes the hotkey manager and unregisters all hotkeys.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            try
            {
                var hotkeyIds = _hotkeyHandlers.Keys.ToList();
                foreach (var id in hotkeyIds)
                {
                    UnregisterHotkey(id);
                }
                _hotkeyHandlers.Clear();
                _logger.LogInformation("HotkeyManager disposed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing HotkeyManager");
            }

            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}