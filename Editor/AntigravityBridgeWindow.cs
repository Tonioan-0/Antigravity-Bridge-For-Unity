using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using AntigravityBridge.Editor.Models;

namespace AntigravityBridge.Editor
{
    /// <summary>
    /// Unity Editor Window for controlling the Antigravity Bridge server
    /// Accessible via Window > Antigravity Bridge
    /// </summary>
    public class AntigravityBridgeWindow : EditorWindow
    {
        private int port = 8080;
        private Vector2 scrollPosition;
        private bool autoStart = false;

        // UI Colors
        private readonly Color greenColor = new Color(0.2f, 0.8f, 0.2f);
        private readonly Color redColor = new Color(0.8f, 0.2f, 0.2f);
        private readonly Color yellowColor = new Color(0.8f, 0.8f, 0.2f);

        [MenuItem("Window/Antigravity Bridge")]
        public static void ShowWindow()
        {
            var window = GetWindow<AntigravityBridgeWindow>("Antigravity Bridge");
            window.minSize = new Vector2(400, 500);
            window.Show();
        }

        private void OnEnable()
        {
            // Load saved preferences
            port = EditorPrefs.GetInt("AntigravityBridge.Port", 8080);
            autoStart = EditorPrefs.GetBool("AntigravityBridge.AutoStart", false);

            // Auto-start if enabled
            if (autoStart && !AntigravityServer.IsRunning)
            {
                AntigravityServer.Start(port);
            }

            // Repaint every 0.5 seconds to update UI
            EditorApplication.update += RepaintOnUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= RepaintOnUpdate;
        }

        private void RepaintOnUpdate()
        {
            // Repaint window periodically to show live updates
            if (Time.realtimeSinceStartup % 0.5f < 0.1f)
            {
                Repaint();
            }
        }

        private void OnGUI()
        {
            // Header
            DrawHeader();

            GUILayout.Space(10);

            // Server Status Section
            DrawServerStatus();

            GUILayout.Space(10);

            // Server Controls Section
            DrawServerControls();

            GUILayout.Space(10);

            // Statistics Section
            DrawStatistics();

            GUILayout.Space(10);

            // Command Log Section
            DrawCommandLog();

            GUILayout.Space(10);

            // Quick Actions
            DrawQuickActions();
        }

        private void DrawHeader()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Antigravity Unity Bridge", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUILayout.Label($"v1.0.0", EditorStyles.miniLabel);
            GUILayout.EndHorizontal();

            EditorGUILayout.HelpBox(
                "HTTP REST API server for controlling Unity Editor from Antigravity AI IDE",
                MessageType.Info
            );
        }

        private void DrawServerStatus()
        {
            GUILayout.Label("Server Status", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Status indicator
            GUILayout.BeginHorizontal();
            GUILayout.Label("Status:", GUILayout.Width(100));

            var statusColor = AntigravityServer.IsRunning ? greenColor : redColor;
            var statusText = AntigravityServer.IsRunning ? "RUNNING" : "STOPPED";

            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = statusColor;
            GUILayout.Box(statusText, GUILayout.Width(100), GUILayout.Height(25));
            GUI.backgroundColor = originalColor;

            GUILayout.EndHorizontal();

            // Port display
            GUILayout.BeginHorizontal();
            GUILayout.Label("Port:", GUILayout.Width(100));
            if (AntigravityServer.IsRunning)
            {
                GUILayout.Label($"{AntigravityServer.Port}", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                GUILayout.Label($"http://localhost:{AntigravityServer.Port}/", EditorStyles.miniLabel);
            }
            else
            {
                GUILayout.Label("N/A", EditorStyles.label);
            }
            GUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawServerControls()
        {
            GUILayout.Label("Server Controls", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Port configuration
            GUILayout.BeginHorizontal();
            GUILayout.Label("Port:", GUILayout.Width(100));
            EditorGUI.BeginDisabledGroup(AntigravityServer.IsRunning);
            port = EditorGUILayout.IntField(port, GUILayout.Width(100));
            EditorGUI.EndDisabledGroup();
            
            if (port != EditorPrefs.GetInt("AntigravityBridge.Port", 8080))
            {
                EditorPrefs.SetInt("AntigravityBridge.Port", port);
            }
            GUILayout.EndHorizontal();

            // Auto-start option
            GUILayout.BeginHorizontal();
            autoStart = EditorGUILayout.Toggle("Auto-start on load", autoStart);
            if (autoStart != EditorPrefs.GetBool("AntigravityBridge.AutoStart", false))
            {
                EditorPrefs.SetBool("AntigravityBridge.Auto Start", autoStart);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            // Start/Stop buttons
            GUILayout.BeginHorizontal();

            if (!AntigravityServer.IsRunning)
            {
                GUI.backgroundColor = greenColor;
                if (GUILayout.Button("Start Server", GUILayout.Height(30)))
                {
                    if (AntigravityServer.Start(port))
                    {
                        Debug.Log($"[Antigravity] Server started on port {port}");
                    }
                }
                GUI.backgroundColor = Color.white;
            }
            else
            {
                GUI.backgroundColor = redColor;
                if (GUILayout.Button("Stop Server", GUILayout.Height(30)))
                {
                    AntigravityServer.Stop();
                    Debug.Log("[Antigravity] Server stopped");
                }
                GUI.backgroundColor = Color.white;
            }

            GUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawStatistics()
        {
            GUILayout.Label("Statistics", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (!AntigravityServer.IsRunning)
            {
                GUILayout.Label("Server not running", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Commands Processed:", GUILayout.Width(150));
                GUILayout.Label($"{AntigravityServer.CommandsProcessed}", EditorStyles.boldLabel);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label($"Successful:", GUILayout.Width(150));
                GUI.contentColor = greenColor;
                GUILayout.Label($"{AntigravityServer.SuccessCount}", EditorStyles.boldLabel);
                GUI.contentColor = Color.white;
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label($"Errors:", GUILayout.Width(150));
                GUI.contentColor = redColor;
                GUILayout.Label($"{AntigravityServer.ErrorCount}", EditorStyles.boldLabel);
                GUI.contentColor = Color.white;
                GUILayout.EndHorizontal();

                // Success rate
                if (AntigravityServer.CommandsProcessed > 0)
                {
                    float successRate = (float)AntigravityServer.SuccessCount / AntigravityServer.CommandsProcessed * 100f;
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"Success Rate:", GUILayout.Width(150));
                    GUILayout.Label($"{successRate:F1}%", EditorStyles.boldLabel);
                    GUILayout.EndHorizontal();
                }

                GUILayout.Space(5);

                // Reset button
                if (GUILayout.Button("Reset Statistics", GUILayout.Height(25)))
                {
                    AntigravityServer.ResetStatistics();
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawCommandLog()
        {
            GUILayout.Label("Command Log (Last 50)", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (AntigravityServer.CommandLog.Count == 0)
            {
                GUILayout.Label("No commands logged yet", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));

                var logEntries = AntigravityServer.CommandLog.ToArray();
                
                // Display in reverse order (newest first)
                for (int i = logEntries.Length - 1; i >= 0; i--)
                {
                    var entry = logEntries[i];
                    DrawLogEntry(entry);
                }

                EditorGUILayout.EndScrollView();

                GUILayout.Space(5);

                if (GUILayout.Button("Clear Log", GUILayout.Height(25)))
                {
                    AntigravityServer.ClearLog();
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawLogEntry(CommandLogEntry entry)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            // Timestamp
            GUILayout.Label(entry.timestamp, EditorStyles.miniLabel, GUILayout.Width(60));

            // Method
            var methodColor = entry.method == "GET" ? Color.cyan : Color.yellow;
            GUI.contentColor = methodColor;
            GUILayout.Label(entry.method, EditorStyles.boldLabel, GUILayout.Width(45));
            GUI.contentColor = Color.white;

            // Endpoint
            GUILayout.Label(entry.endpoint, EditorStyles.miniLabel, GUILayout.Width(180));

            // Status
            var statusColor = entry.status == "success" ? greenColor :
                            entry.status == "error" ? redColor : yellowColor;
            GUI.contentColor = statusColor;
            GUILayout.Label(entry.status.ToUpper(), EditorStyles.miniLabel, GUILayout.Width(70));
            GUI.contentColor = Color.white;

            // Execution time
            GUILayout.FlexibleSpace();
            GUILayout.Label($"{entry.execution_time:F0}ms", EditorStyles.miniLabel, GUILayout.Width(50));

            EditorGUILayout.EndHorizontal();

            // Message (if not empty)
            if (!string.IsNullOrEmpty(entry.message))
            {
                EditorGUI.indentLevel++;
                GUILayout.Label(entry.message, EditorStyles.wordWrappedMiniLabel);
                EditorGUI.indentLevel--;
            }
        }

        private void DrawQuickActions()
        {
            GUILayout.Label("Quick Actions", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (GUILayout.Button("Test Connection (Ping)", GUILayout.Height(25)))
            {
                TestConnection();
            }

            if (GUILayout.Button("Copy Base URL to Clipboard", GUILayout.Height(25)))
            {
                string url = $"http://localhost:{(AntigravityServer.IsRunning ? AntigravityServer.Port : port)}/";
                GUIUtility.systemCopyBuffer = url;
                Debug.Log($"[Antigravity] Copied URL to clipboard: {url}");
            }

            if (GUILayout.Button("Open API Documentation", GUILayout.Height(25)))
            {
                Application.OpenURL("https://github.com/your-repo/antigravity-unity-bridge#api-documentation");
            }

            EditorGUILayout.EndVertical();
        }

        private void TestConnection()
        {
            if (!AntigravityServer.IsRunning)
            {
                EditorUtility.DisplayDialog(
                    "Test Connection",
                    "Server is not running. Please start the server first.",
                    "OK"
                );
                return;
            }

            try
            {
                var request = System.Net.WebRequest.Create($"http://localhost:{AntigravityServer.Port}/unity/status");
                request.Method = "GET";
                request.Timeout = 2000;

                using (var response = request.GetResponse())
                {
                    EditorUtility.DisplayDialog(
                        "Connection Test",
                        $"✓ Connection successful!\n\nServer is running on port {AntigravityServer.Port}",
                        "OK"
                    );
                }
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog(
                    "Connection Test",
                    $"✗ Connection failed!\n\nError: {e.Message}",
                    "OK"
                );
            }
        }
    }
}
