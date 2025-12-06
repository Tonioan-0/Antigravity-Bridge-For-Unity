using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using AntigravityBridge.Editor.Models;

namespace AntigravityBridge.Editor
{
    /// <summary>
    /// API for querying Unity Editor state - console logs, compilation status, play mode
    /// Enables AI to understand the current state of the editor before/after operations
    /// </summary>
    public static class EditorStateAPI
    {
        // Cache for console logs (Unity doesn't provide direct API, we use reflection)
        private static List<ConsoleLogEntry> cachedLogs = new List<ConsoleLogEntry>();
        private static bool isLogCallbackRegistered = false;
        private const int MAX_LOG_ENTRIES = 100;

        /// <summary>
        /// Initialize log callback to capture Unity console messages
        /// </summary>
        public static void Initialize()
        {
            if (!isLogCallbackRegistered)
            {
                Application.logMessageReceived += OnLogMessageReceived;
                isLogCallbackRegistered = true;
            }
        }

        /// <summary>
        /// Cleanup log callback
        /// </summary>
        public static void Cleanup()
        {
            if (isLogCallbackRegistered)
            {
                Application.logMessageReceived -= OnLogMessageReceived;
                isLogCallbackRegistered = false;
            }
        }

        /// <summary>
        /// Callback for Unity log messages
        /// </summary>
        private static void OnLogMessageReceived(string message, string stackTrace, LogType type)
        {
            var entry = new ConsoleLogEntry
            {
                message = message,
                stackTrace = stackTrace,
                timestamp = DateTime.Now.ToString("HH:mm:ss.fff"),
                count = 1,
                type = LogTypeToString(type)
            };

            cachedLogs.Add(entry);

            // Keep only the last MAX_LOG_ENTRIES
            while (cachedLogs.Count > MAX_LOG_ENTRIES)
            {
                cachedLogs.RemoveAt(0);
            }
        }

        private static string LogTypeToString(LogType type)
        {
            switch (type)
            {
                case LogType.Error: return "error";
                case LogType.Assert: return "error";
                case LogType.Warning: return "warning";
                case LogType.Log: return "info";
                case LogType.Exception: return "exception";
                default: return "info";
            }
        }

        /// <summary>
        /// Get current editor state
        /// GET /unity/editor/state
        /// </summary>
        public static UnityResponse GetEditorState()
        {
            try
            {
                var stateData = new EditorStateData
                {
                    is_playing = EditorApplication.isPlaying,
                    is_paused = EditorApplication.isPaused,
                    is_compiling = EditorApplication.isCompiling,
                    last_compilation_result = GetLastCompilationResult(),
                    has_errors = HasConsoleErrors(),
                    has_warnings = HasConsoleWarnings()
                };

                var data = new ResponseData
                {
                    editor_state = stateData
                };

                return new UnityResponse
                {
                    status = "success",
                    message = "Editor state retrieved",
                    data = data,
                    timestamp = DateTime.UtcNow.ToString("o")
                };
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Failed to get editor state: {e.Message}");
            }
        }

        /// <summary>
        /// Get console logs
        /// GET /unity/editor/console
        /// Optional query params: ?type=error&limit=50
        /// </summary>
        public static UnityResponse GetConsoleLogs(string typeFilter = null, int limit = 50)
        {
            try
            {
                var logs = new List<ConsoleLogEntry>();
                
                // Filter and limit logs
                for (int i = cachedLogs.Count - 1; i >= 0 && logs.Count < limit; i--)
                {
                    var log = cachedLogs[i];
                    if (typeFilter == null || log.type == typeFilter)
                    {
                        logs.Add(log);
                    }
                }

                // Reverse to get chronological order
                logs.Reverse();

                var summary = GetConsoleSummary();
                
                var consoleData = new ConsoleLogsData
                {
                    logs = logs.ToArray(),
                    summary = summary
                };

                var data = new ResponseData
                {
                    console_logs = consoleData
                };
                
                return new UnityResponse
                {
                    status = "success",
                    message = $"Retrieved {logs.Count} log entries",
                    data = data,
                    timestamp = DateTime.UtcNow.ToString("o")
                };
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Failed to get console logs: {e.Message}");
            }
        }

        /// <summary>
        /// Get only console errors
        /// GET /unity/editor/console/errors
        /// </summary>
        public static UnityResponse GetConsoleErrors(int limit = 50)
        {
            return GetConsoleLogs("error", limit);
        }

        /// <summary>
        /// Get compilation status
        /// GET /unity/editor/compilation
        /// </summary>
        public static UnityResponse GetCompilationStatus()
        {
            try
            {
                var compilationData = new CompilationData
                {
                    is_compiling = EditorApplication.isCompiling,
                    progress = EditorApplication.isCompiling ? 0.5f : 1.0f, // Unity doesn't expose progress
                    status = EditorApplication.isCompiling ? "compiling" : "idle",
                    has_errors = HasCompilationErrors(),
                    compilation_errors = GetCompilationErrors()
                };

                var data = new ResponseData
                {
                    compilation = compilationData
                };

                return new UnityResponse
                {
                    status = "success",
                    message = compilationData.is_compiling ? "Compilation in progress" : "Compilation idle",
                    data = data,
                    timestamp = DateTime.UtcNow.ToString("o")
                };
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Failed to get compilation status: {e.Message}");
            }
        }

        /// <summary>
        /// Wait for compilation to complete
        /// GET /unity/editor/wait_compilation?timeout=30
        /// Note: This blocks the request until compilation finishes or timeout
        /// </summary>
        public static UnityResponse WaitForCompilation(int timeoutSeconds = 30)
        {
            try
            {
                var startTime = DateTime.Now;
                var timeout = TimeSpan.FromSeconds(timeoutSeconds);

                // If not compiling, return immediately
                if (!EditorApplication.isCompiling)
                {
                    var notCompilingResult = new WaitCompilationResult
                    {
                        completed = true,
                        wait_time_seconds = 0f,
                        has_errors = HasCompilationErrors(),
                        errors = GetCompilationErrors()
                    };
                    
                    return new UnityResponse
                    {
                        status = "success",
                        message = "No compilation in progress",
                        data = new ResponseData { wait_result = notCompilingResult },
                        timestamp = DateTime.UtcNow.ToString("o")
                    };
                }

                // Poll until compilation finishes or timeout
                while (EditorApplication.isCompiling)
                {
                    if (DateTime.Now - startTime > timeout)
                    {
                        var timeoutResult = new WaitCompilationResult
                        {
                            completed = false,
                            wait_time_seconds = (float)timeout.TotalSeconds,
                            has_errors = false,
                            errors = new string[0]
                        };
                        
                        return new UnityResponse
                        {
                            status = "partial",
                            message = $"Compilation still in progress after {timeoutSeconds}s timeout",
                            data = new ResponseData { wait_result = timeoutResult },
                            timestamp = DateTime.UtcNow.ToString("o")
                        };
                    }

                    // Small delay to avoid busy waiting
                    Thread.Sleep(100);
                }

                var waitTime = (float)(DateTime.Now - startTime).TotalSeconds;
                var hasErrors = HasCompilationErrors();
                
                var successResult = new WaitCompilationResult
                {
                    completed = true,
                    wait_time_seconds = waitTime,
                    has_errors = hasErrors,
                    errors = hasErrors ? GetCompilationErrors() : new string[0]
                };

                return new UnityResponse
                {
                    status = hasErrors ? "partial" : "success",
                    message = hasErrors 
                        ? $"Compilation completed with errors after {waitTime:F1}s" 
                        : $"Compilation completed successfully after {waitTime:F1}s",
                    data = new ResponseData { wait_result = successResult },
                    timestamp = DateTime.UtcNow.ToString("o")
                };
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Failed to wait for compilation: {e.Message}");
            }
        }

        /// <summary>
        /// Clear cached console logs
        /// POST /unity/editor/console/clear
        /// </summary>
        public static UnityResponse ClearConsoleLogs()
        {
            cachedLogs.Clear();
            return UnityResponse.Success("Console logs cleared");
        }

        /// <summary>
        /// Refresh assets and request script recompilation
        /// POST /unity/editor/refresh
        /// This forces Unity to reimport assets and recompile scripts
        /// </summary>
        public static UnityResponse RefreshAssets()
        {
            try
            {
                // Request script compilation
                CompilationPipeline.RequestScriptCompilation();
                
                // Refresh asset database (reimports changed assets)
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                
                return UnityResponse.Success("Requested asset refresh and script recompilation");
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Failed to refresh assets: {e.Message}");
            }
        }

        /// <summary>
        /// Request only script recompilation without full asset refresh
        /// POST /unity/editor/recompile
        /// </summary>
        public static UnityResponse RequestRecompilation()
        {
            try
            {
                CompilationPipeline.RequestScriptCompilation();
                return UnityResponse.Success("Requested script recompilation");
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Failed to request recompilation: {e.Message}");
            }
        }

        #region Helper Methods

        private static string GetLastCompilationResult()
        {
            if (EditorApplication.isCompiling)
                return "compiling";
            if (HasCompilationErrors())
                return "error";
            return "success";
        }

        private static bool HasConsoleErrors()
        {
            foreach (var log in cachedLogs)
            {
                if (log.type == "error" || log.type == "exception")
                    return true;
            }
            return false;
        }

        private static bool HasConsoleWarnings()
        {
            foreach (var log in cachedLogs)
            {
                if (log.type == "warning")
                    return true;
            }
            return false;
        }

        private static bool HasCompilationErrors()
        {
            // Check for compilation errors in the cached logs
            foreach (var log in cachedLogs)
            {
                if (log.type == "error" && 
                    (log.message.Contains("error CS") || log.message.Contains("Compilation failed")))
                    return true;
            }
            return false;
        }

        private static string[] GetCompilationErrors()
        {
            var errors = new List<string>();
            foreach (var log in cachedLogs)
            {
                if (log.type == "error" && log.message.Contains("error CS"))
                {
                    errors.Add(log.message);
                }
            }
            return errors.ToArray();
        }

        private static ConsoleSummary GetConsoleSummary()
        {
            var summary = new ConsoleSummary();
            foreach (var log in cachedLogs)
            {
                switch (log.type)
                {
                    case "error": summary.errors++; break;
                    case "warning": summary.warnings++; break;
                    case "info": summary.info++; break;
                    case "exception": summary.exceptions++; break;
                }
            }
            return summary;
        }

        private static string BuildConsoleLogsJson(List<ConsoleLogEntry> logs, ConsoleSummary summary)
        {
            // This would be used if we need custom JSON structure
            // For now, using standard response
            return "";
        }

        #endregion

        #region Play Mode Control

        /// <summary>
        /// Enter Play Mode
        /// POST /unity/editor/play
        /// </summary>
        public static UnityResponse EnterPlayMode()
        {
            try
            {
                if (EditorApplication.isPlaying)
                {
                    return UnityResponse.Success("Already in Play Mode");
                }

                EditorApplication.isPlaying = true;
                return UnityResponse.Success("Entered Play Mode");
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Failed to enter Play Mode: {e.Message}");
            }
        }

        /// <summary>
        /// Exit Play Mode
        /// POST /unity/editor/stop
        /// </summary>
        public static UnityResponse ExitPlayMode()
        {
            try
            {
                if (!EditorApplication.isPlaying)
                {
                    return UnityResponse.Success("Already in Edit Mode");
                }

                EditorApplication.isPlaying = false;
                return UnityResponse.Success("Exited Play Mode");
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Failed to exit Play Mode: {e.Message}");
            }
        }

        /// <summary>
        /// Pause/Unpause Play Mode
        /// POST /unity/editor/pause
        /// </summary>
        public static UnityResponse TogglePause()
        {
            try
            {
                if (!EditorApplication.isPlaying)
                {
                    return UnityResponse.Error("Not in Play Mode - cannot pause");
                }

                EditorApplication.isPaused = !EditorApplication.isPaused;
                string state = EditorApplication.isPaused ? "Paused" : "Resumed";
                return UnityResponse.Success($"Play Mode {state}");
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Failed to toggle pause: {e.Message}");
            }
        }

        /// <summary>
        /// Step one frame (while paused)
        /// POST /unity/editor/step
        /// </summary>
        public static UnityResponse StepFrame()
        {
            try
            {
                if (!EditorApplication.isPlaying)
                {
                    return UnityResponse.Error("Not in Play Mode - cannot step");
                }

                EditorApplication.Step();
                return UnityResponse.Success("Stepped one frame");
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Failed to step frame: {e.Message}");
            }
        }

        #endregion
    }
}
