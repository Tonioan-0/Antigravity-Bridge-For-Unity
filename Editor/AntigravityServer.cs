using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;
using AntigravityBridge.Editor.Models;

namespace AntigravityBridge.Editor
{
    /// <summary>
    /// Main HTTP server that receives commands from Antigravity and executes them in Unity Editor
    /// Thread-safe implementation with background listener and main-thread command execution
    /// </summary>
    public static class AntigravityServer
    {
        private static HttpListener listener;
        private static Thread listenerThread;
        private static bool isRunning = false;
        private static int port = 8080;

        // Thread-safe command queue
        private static Queue<Action> commandQueue = new Queue<Action>();
        private static object queueLock = new object();

        // Statistics
        private static int commandsProcessed = 0;
        private static int successCount = 0;
        private static int errorCount = 0;
        private static DateTime startTime;

        // Command log for UI
        private static Queue<CommandLogEntry> commandLog = new Queue<CommandLogEntry>();
        private const int MAX_LOG_ENTRIES = 50;

        public static bool IsRunning => isRunning;
        public static int Port => port;
        public static int CommandsProcessed => commandsProcessed;
        public static int SuccessCount => successCount;
        public static int ErrorCount => errorCount;
        public static Queue<CommandLogEntry> CommandLog => commandLog;

        /// <summary>
        /// Start the HTTP server
        /// </summary>
        public static bool Start(int serverPort = 8080)
        {
            if (isRunning)
            {
                Debug.LogWarning("[Antigravity] Server is already running");
                return false;
            }

            port = serverPort;

            try
            {
                listener = new HttpListener();
                listener.Prefixes.Add($"http://localhost:{port}/");
                listener.Start();

                isRunning = true;
                startTime = DateTime.UtcNow;

                // Start listener thread
                listenerThread = new Thread(ListenForRequests);
                listenerThread.IsBackground = true;
                listenerThread.Start();

                // Register main-thread processor
                EditorApplication.update += ProcessCommandQueue;

                // Initialize Editor State API for console log capture
                EditorStateAPI.Initialize();

                Debug.Log($"[Antigravity] Server started on http://localhost:{port}/");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Antigravity] Failed to start server: {e.Message}");
                isRunning = false;
                return false;
            }
        }

        /// <summary>
        /// Stop the HTTP server
        /// </summary>
        public static void Stop()
        {
            if (!isRunning)
            {
                return;
            }

            isRunning = false;

            try
            {
                if (listener != null)
                {
                    listener.Stop();
                    listener.Close();
                }

                if (listenerThread != null && listenerThread.IsAlive)
                {
                    listenerThread.Abort();
                }

                EditorApplication.update -= ProcessCommandQueue;

                Debug.Log("[Antigravity] Server stopped");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Antigravity] Error stopping server: {e.Message}");
            }
        }

        /// <summary>
        /// Background thread that listens for HTTP requests
        /// </summary>
        private static void ListenForRequests()
        {
            while (isRunning)
            {
                try
                {
                    var context = listener.GetContext();

                    // Queue the request for processing on main thread
                    lock (queueLock)
                    {
                        commandQueue.Enqueue(() => ProcessRequest(context));
                    }
                }
                catch (HttpListenerException)
                {
                    // Expected when stopping the listener
                    break;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Antigravity] Listener error: {e.Message}");
                }
            }
        }

        /// <summary>
        /// Process queued commands on Unity's main thread
        /// </summary>
        private static void ProcessCommandQueue()
        {
            if (!isRunning) return;

            lock (queueLock)
            {
                while (commandQueue.Count > 0)
                {
                    try
                    {
                        var action = commandQueue.Dequeue();
                        action.Invoke();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[Antigravity] Command execution error: {e.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Process individual HTTP request
        /// </summary>
        private static void ProcessRequest(HttpListenerContext context)
        {
            var startTime = DateTime.Now;
            var request = context.Request;
            var response = context.Response;

            // Set CORS headers for localhost
            response.AddHeader("Access-Control-Allow-Origin", "*");
            response.AddHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            response.AddHeader("Access-Control-Allow-Headers", "Content-Type");

            // Handle OPTIONS preflight
            if (request.HttpMethod == "OPTIONS")
            {
                response.StatusCode = 200;
                response.Close();
                return;
            }

            string responseString = "";
            UnityResponse unityResponse = null;

            try
            {
                string path = request.Url.AbsolutePath;
                string method = request.HttpMethod;

                // Log the request
                var logEntry = new CommandLogEntry(path, method);

                // Route the request
                unityResponse = RouteRequest(path, method, request);

                // Update statistics
                commandsProcessed++;
                if (unityResponse.status == "success")
                {
                    successCount++;
                }
                else
                {
                    errorCount++;
                }

                // Update log entry
                logEntry.status = unityResponse.status;
                logEntry.message = unityResponse.message;
                logEntry.execution_time = (float)(DateTime.Now - startTime).TotalMilliseconds;

                AddLogEntry(logEntry);

                // Serialize response
                responseString = JsonUtility.ToJson(unityResponse);
                response.StatusCode = 200;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Antigravity] Request processing error: {e.Message}\n{e.StackTrace}");
                unityResponse = UnityResponse.Error($"Internal server error: {e.Message}");
                responseString = JsonUtility.ToJson(unityResponse);
                response.StatusCode = 500;
                errorCount++;
            }

            // Send response
            byte[] buffer = Encoding.UTF8.GetBytes(responseString);
            response.ContentType = "application/json";
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.Close();
        }

        /// <summary>
        /// Route request to appropriate handler
        /// </summary>
        private static UnityResponse RouteRequest(string path, string method, HttpListenerRequest request)
        {
            // Status endpoints
            if (path == "/unity/status" && method == "GET")
            {
                return GetServerStatus();
            }
            if (path == "/unity/health" && method == "GET")
            {
                return GetHealthStatus();
            }

            // Scene query endpoints (v2: with QueryOptions support)
            if (path == "/unity/scene/hierarchy" && method == "GET")
            {
                var queryParams = ParseQueryParams(request.Url.Query);
                var options = ParseQueryOptions(queryParams);
                return SceneQueryAPI.GetSceneHierarchy(options);
            }
            if (path.StartsWith("/unity/scene/objects/") && method == "GET")
            {
                // URL decode to handle spaces and special characters (e.g., "Main%20Camera" -> "Main Camera")
                string objectName = Uri.UnescapeDataString(path.Substring("/unity/scene/objects/".Length));
                var queryParams = ParseQueryParams(request.Url.Query);
                var options = ParseQueryOptions(queryParams);
                return SceneQueryAPI.GetObjectInfo(objectName, options);
            }
            if (path == "/unity/scene/info" && method == "GET")
            {
                return SceneQueryAPI.GetSceneInfo();
            }
            if (path == "/unity/project/scripts" && method == "GET")
            {
                return SceneQueryAPI.GetAvailableScripts();
            }
            if (path == "/unity/components/list" && method == "GET")
            {
                return SceneQueryAPI.GetAvailableComponents();
            }

            // Editor State endpoints (NEW v2)
            if (path == "/unity/editor/state" && method == "GET")
            {
                return EditorStateAPI.GetEditorState();
            }
            if (path == "/unity/editor/console" && method == "GET")
            {
                // Parse query params for type filter and limit
                var queryParams = ParseQueryParams(request.Url.Query);
                string typeFilter = queryParams.ContainsKey("type") ? queryParams["type"] : null;
                int limit = queryParams.ContainsKey("limit") ? int.Parse(queryParams["limit"]) : 50;
                return EditorStateAPI.GetConsoleLogs(typeFilter, limit);
            }
            if (path == "/unity/editor/console/errors" && method == "GET")
            {
                var queryParams = ParseQueryParams(request.Url.Query);
                int limit = queryParams.ContainsKey("limit") ? int.Parse(queryParams["limit"]) : 50;
                return EditorStateAPI.GetConsoleErrors(limit);
            }
            if (path == "/unity/editor/compilation" && method == "GET")
            {
                return EditorStateAPI.GetCompilationStatus();
            }
            if (path == "/unity/editor/wait_compilation" && method == "GET")
            {
                var queryParams = ParseQueryParams(request.Url.Query);
                int timeout = queryParams.ContainsKey("timeout") ? int.Parse(queryParams["timeout"]) : 30;
                return EditorStateAPI.WaitForCompilation(timeout);
            }
            if (path == "/unity/editor/refresh" && method == "POST")
            {
                return EditorStateAPI.RefreshAssets();
            }
            if (path == "/unity/editor/recompile" && method == "POST")
            {
                return EditorStateAPI.RequestRecompilation();
            }
            if (path == "/unity/editor/console/clear" && method == "POST")
            {
                return EditorStateAPI.ClearConsoleLogs();
            }
            // Play Mode control
            if (path == "/unity/editor/play" && method == "POST")
            {
                return EditorStateAPI.EnterPlayMode();
            }
            if (path == "/unity/editor/stop" && method == "POST")
            {
                return EditorStateAPI.ExitPlayMode();
            }
            if (path == "/unity/editor/pause" && method == "POST")
            {
                return EditorStateAPI.TogglePause();
            }
            if (path == "/unity/editor/step" && method == "POST")
            {
                return EditorStateAPI.StepFrame();
            }

            // Tag list (GET)
            if (path == "/unity/tag/list" && method == "GET")
            {
                return TagAPI.GetAllTags();
            }

            // Command execution endpoints (POST)
            if (method == "POST")
            {
                string body = ReadRequestBody(request);

                if (path == "/unity/scene/find")
                {
                    return CommandExecutor.FindObjects(body);
                }
                if (path == "/unity/scene/create")
                {
                    return CommandExecutor.CreateObject(body);
                }
                if (path == "/unity/scene/modify")
                {
                    return CommandExecutor.ModifyObjects(body);
                }
                if (path == "/unity/scene/delete")
                {
                    return CommandExecutor.DeleteObjects(body);
                }
                if (path == "/unity/component/add")
                {
                    return CommandExecutor.AddComponent(body);
                }
                if (path == "/unity/component/remove")
                {
                    return CommandExecutor.RemoveComponent(body);
                }
                if (path == "/unity/component/modify")
                {
                    return CommandExecutor.ModifyComponent(body);
                }
                if (path == "/unity/scene/find_and_modify")
                {
                    return CommandExecutor.FindAndModify(body);
                }

                // Light endpoints
                if (path == "/unity/light/modify")
                {
                    return LightAPI.ModifyLight(body);
                }

                // Material endpoints
                if (path == "/unity/material/modify")
                {
                    return MaterialAPI.ModifyMaterial(body);
                }
                if (path == "/unity/material/assign")
                {
                    return MaterialAPI.AssignMaterial(body);
                }

                // Audio endpoints
                if (path == "/unity/audio/play")
                {
                    return AudioAPI.PlayAudio(body);
                }
                if (path == "/unity/audio/stop")
                {
                    return AudioAPI.StopAudio(body);
                }
                if (path == "/unity/audio/modify")
                {
                    return AudioAPI.ModifyAudioSource(body);
                }

                // Tag endpoints
                if (path == "/unity/tag/create")
                {
                    return TagAPI.CreateTag(body);
                }
                if (path == "/unity/tag/assign")
                {
                    return TagAPI.AssignTag(body);
                }

                // Layer endpoints
                if (path == "/unity/layer/assign")
                {
                    return TagAPI.AssignLayer(body);
                }

                // Script endpoints
                if (path == "/unity/script/create")
                {
                    return ScriptAPI.CreateScript(body);
                }

                // Prefab endpoints
                if (path == "/unity/prefab/instantiate")
                {
                    return PrefabAPI.Instantiate(body);
                }

                // Transform endpoints
                if (path == "/unity/transform/modify")
                {
                    return TransformAPI.ModifyTransform(body);
                }

                // Physics endpoints
                if (path == "/unity/physics/simulate")
                {
                    return PhysicsAPI.Simulate(body);
                }
                if (path == "/unity/physics/step")
                {
                    return PhysicsAPI.Step(body);
                }
                if (path == "/unity/physics/raycast")
                {
                    return PhysicsAPI.Raycast(body);
                }
                if (path == "/unity/physics/gravity")
                {
                    return PhysicsAPI.SetGravity(body);
                }

                // Animation endpoints
                if (path == "/unity/animation/play")
                {
                    return AnimationAPI.PlayAnimation(body);
                }
                if (path == "/unity/animation/stop")
                {
                    return AnimationAPI.StopAnimation(body);
                }
                if (path == "/unity/animator/set")
                {
                    return AnimationAPI.SetAnimatorParameter(body);
                }

                // Particle endpoints
                if (path == "/unity/particles/play")
                {
                    return ParticleAPI.PlayParticles(body);
                }
                if (path == "/unity/particles/stop")
                {
                    return ParticleAPI.StopParticles(body);
                }
                if (path == "/unity/particles/emit")
                {
                    return ParticleAPI.EmitParticles(body);
                }
                if (path == "/unity/particles/modify")
                {
                    return ParticleAPI.ModifyParticles(body);
                }

                // Screenshot endpoints
                if (path == "/unity/screenshot/capture")
                {
                    return ScreenshotAPI.CaptureScreenshot(body);
                }
                if (path == "/unity/screenshot/camera")
                {
                    return ScreenshotAPI.CaptureFromCamera(body);
                }
                if (path == "/unity/screenshot/scene")
                {
                    return ScreenshotAPI.CaptureSceneView(body);
                }

                // Settings endpoints
                if (path.StartsWith("/unity/settings/"))
                {
                    string category = path.Substring("/unity/settings/".Length);
                    return SettingsAPI.ModifySettings(category, body);
                }
            }

            // Settings GET endpoints
            if (path.StartsWith("/unity/settings/") && method == "GET")
            {
                string category = path.Substring("/unity/settings/".Length);
                return SettingsAPI.GetSettings(category);
            }

            // v2: Unified command endpoint (Unix-like commands)
            if (path == "/unity/command" && method == "POST")
            {
                string body = ReadRequestBody(request);
                try
                {
                    var cmdObj = JsonUtility.FromJson<CommandRequest>(body);
                    if (cmdObj != null && !string.IsNullOrEmpty(cmdObj.cmd))
                    {
                        return CommandParser.ParseAndExecute(cmdObj.cmd);
                    }
                    return UnityResponse.Error("Missing 'cmd' field in request body");
                }
                catch (Exception e)
                {
                    return UnityResponse.Error($"Failed to parse command: {e.Message}");
                }
            }

            return UnityResponse.Error($"Unknown endpoint: {method} {path}");
        }

        /// <summary>
        /// Read request body as string
        /// </summary>
        private static string ReadRequestBody(HttpListenerRequest request)
        {
            if (!request.HasEntityBody)
            {
                return "";
            }

            using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
            {
                return reader.ReadToEnd();
            }
        }

        /// <summary>
        /// Parse URL query parameters into dictionary
        /// </summary>
        private static Dictionary<string, string> ParseQueryParams(string query)
        {
            var result = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(query) || query == "?")
                return result;

            // Remove leading ?
            if (query.StartsWith("?"))
                query = query.Substring(1);

            foreach (var param in query.Split('&'))
            {
                var parts = param.Split('=');
                if (parts.Length == 2)
                {
                    result[parts[0]] = System.Net.WebUtility.UrlDecode(parts[1]);
                }
            }
            return result;
        }

        /// <summary>
        /// Parse QueryOptions from URL query parameters
        /// </summary>
        private static QueryOptions ParseQueryOptions(Dictionary<string, string> queryParams)
        {
            var options = new QueryOptions();
            
            if (queryParams.ContainsKey("select"))
            {
                options.select = queryParams["select"].Split(',');
            }
            if (queryParams.ContainsKey("depth"))
            {
                int.TryParse(queryParams["depth"], out options.depth);
            }
            if (queryParams.ContainsKey("format"))
            {
                options.format = queryParams["format"];
            }
            if (queryParams.ContainsKey("limit"))
            {
                int.TryParse(queryParams["limit"], out options.limit);
            }
            if (queryParams.ContainsKey("precision"))
            {
                int.TryParse(queryParams["precision"], out options.precision);
            }
            
            return options;
        }

        /// <summary>
        /// Get server status
        /// </summary>
        private static UnityResponse GetServerStatus()
        {
            var data = new ResponseData
            {
                server_status = new ServerStatusData
                {
                    is_running = isRunning,
                    port = port,
                    unity_version = Application.unityVersion,
                    editor_mode = EditorApplication.isPlaying ? "Play" : "Edit",
                    commands_processed = commandsProcessed,
                    success_count = successCount,
                    error_count = errorCount,
                    uptime_seconds = (float)(DateTime.UtcNow - startTime).TotalSeconds
                }
            };

            return UnityResponse.Success("Server is running", data);
        }

        /// <summary>
        /// Get detailed health status
        /// </summary>
        private static UnityResponse GetHealthStatus()
        {
            return GetServerStatus();
        }

        /// <summary>
        /// Add log entry with size limit
        /// </summary>
        private static void AddLogEntry(CommandLogEntry entry)
        {
            commandLog.Enqueue(entry);
            while (commandLog.Count > MAX_LOG_ENTRIES)
            {
                commandLog.Dequeue();
            }
        }

        /// <summary>
        /// Clear command log
        /// </summary>
        public static void ClearLog()
        {
            commandLog.Clear();
        }

        /// <summary>
        /// Reset statistics
        /// </summary>
        public static void ResetStatistics()
        {
            commandsProcessed = 0;
            successCount = 0;
            errorCount = 0;
            startTime = DateTime.UtcNow;
        }
    }
}
