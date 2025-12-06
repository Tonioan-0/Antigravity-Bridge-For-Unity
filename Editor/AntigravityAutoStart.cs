using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace AntigravityBridge.Editor
{
    /// <summary>
    /// Automatically starts the Antigravity server when Unity Editor loads
    /// and restarts it after recompilation (domain reload)
    /// </summary>
    [InitializeOnLoad]
    public static class AntigravityAutoStart
    {
        // EditorPrefs keys
        private const string PREF_AUTO_START = "Antigravity_AutoStart";
        private const string PREF_LAST_PORT = "Antigravity_LastPort";
        private const string PREF_WAS_RUNNING = "Antigravity_WasRunning";

        // Port range to try
        private const int PORT_START = 8080;
        private const int PORT_END = 8090;

        /// <summary>
        /// Static constructor - called when Unity loads or after recompilation
        /// </summary>
        static AntigravityAutoStart()
        {
            // Delay start to let Unity finish initialization
            EditorApplication.delayCall += OnEditorReady;

            // Subscribe to compilation events to save state before domain reloads
            CompilationPipeline.compilationStarted += OnCompilationStarted;
        }

        private static void OnEditorReady()
        {
            // Check if auto-start is enabled (default: true)
            bool autoStart = EditorPrefs.GetBool(PREF_AUTO_START, true);
            bool wasRunning = EditorPrefs.GetBool(PREF_WAS_RUNNING, false);

            // Auto-start if enabled, or restart if was running before recompilation
            if (autoStart || wasRunning)
            {
                StartServerWithFallback();
                // Clear was running flag after restart
                EditorPrefs.SetBool(PREF_WAS_RUNNING, false);
            }
        }

        /// <summary>
        /// Called when compilation starts - save running state
        /// </summary>
        private static void OnCompilationStarted(object context)
        {
            if (AntigravityServer.IsRunning)
            {
                // Remember that server was running before compilation
                EditorPrefs.SetBool(PREF_WAS_RUNNING, true);
                EditorPrefs.SetInt(PREF_LAST_PORT, AntigravityServer.Port);
                Debug.Log("[Antigravity] Server will restart after compilation...");
            }
        }

        /// <summary>
        /// Start server with automatic port fallback
        /// Tries ports 8080-8090 until one works
        /// </summary>
        public static bool StartServerWithFallback()
        {
            // Try last successful port first
            int lastPort = EditorPrefs.GetInt(PREF_LAST_PORT, PORT_START);

            if (TryStartOnPort(lastPort))
            {
                return true;
            }

            // Try other ports in range
            for (int port = PORT_START; port <= PORT_END; port++)
            {
                if (port == lastPort) continue; // Already tried

                if (TryStartOnPort(port))
                {
                    EditorPrefs.SetInt(PREF_LAST_PORT, port);
                    return true;
                }
            }

            Debug.LogError($"[Antigravity] Failed to start server on any port from {PORT_START} to {PORT_END}");
            return false;
        }

        /// <summary>
        /// Try to start server on specific port
        /// </summary>
        private static bool TryStartOnPort(int port)
        {
            try
            {
                if (AntigravityServer.Start(port))
                {
                    Debug.Log($"[Antigravity] Server auto-started on port {port}");
                    return true;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Antigravity] Port {port} unavailable: {e.Message}");
            }
            return false;
        }

        /// <summary>
        /// Get or set auto-start preference
        /// </summary>
        public static bool AutoStartEnabled
        {
            get => EditorPrefs.GetBool(PREF_AUTO_START, true);
            set => EditorPrefs.SetBool(PREF_AUTO_START, value);
        }

        /// <summary>
        /// Get last used port
        /// </summary>
        public static int LastPort => EditorPrefs.GetInt(PREF_LAST_PORT, PORT_START);
    }
}
