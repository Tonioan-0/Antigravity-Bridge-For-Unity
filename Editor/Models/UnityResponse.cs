using System;
using System.Collections.Generic;
using UnityEngine;

namespace AntigravityBridge.Editor.Models
{
    /// <summary>
    /// Standard response structure for all Unity API endpoints
    /// </summary>
    [Serializable]
    public class UnityResponse
    {
        public string status;         // "success", "error", "partial"
        public string message;
        public ResponseData data;
        public string timestamp;

        public UnityResponse()
        {
            timestamp = DateTime.UtcNow.ToString("o"); // ISO 8601 format
        }

        public static UnityResponse Success(string message, ResponseData data = null)
        {
            return new UnityResponse
            {
                status = "success",
                message = message,
                data = data ?? new ResponseData()
            };
        }

        public static UnityResponse Error(string message, string[] errors = null)
        {
            return new UnityResponse
            {
                status = "error",
                message = message,
                data = new ResponseData
                {
                    errors = errors ?? new string[0]
                }
            };
        }

        public static UnityResponse Partial(string message, ResponseData data)
        {
            return new UnityResponse
            {
                status = "partial",
                message = message,
                data = data
            };
        }
    }

    /// <summary>
    /// Response data payload
    /// </summary>
    [Serializable]
    public class ResponseData
    {
        public string[] affected_objects;
        public int count;
        public string[] errors;
        public SceneHierarchyData scene_hierarchy;
        public GameObjectData object_info;
        public string[] available_scripts;
        public string[] available_components;
        public SceneInfoData scene_info;
        public Dictionary<string, object> settings;
        public ServerStatusData server_status;
        
        // v2: Editor State API fields
        public EditorStateData editor_state;
        public ConsoleLogsData console_logs;
        public CompilationData compilation;
        public WaitCompilationResult wait_result;
        
        // Animation API fields
        public AnimatorParameterInfo[] animator_parameters;
    }

    /// <summary>
    /// Scene hierarchy data
    /// </summary>
    [Serializable]
    public class SceneHierarchyData
    {
        public string scene_name;
        public string scene_path;
        public GameObjectNode[] root_objects;
    }

    /// <summary>
    /// GameObject node in hierarchy tree
    /// </summary>
    [Serializable]
    public class GameObjectNode
    {
        public string name;
        public string path;           // Full hierarchy path
        public bool active;
        public string tag;
        public int layer;
        public string[] components;
        public GameObjectNode[] children;
        public Vector3Data position;
        public Vector3Data rotation;
        public Vector3Data scale;
    }

    /// <summary>
    /// Detailed GameObject information
    /// </summary>
    [Serializable]
    public class GameObjectData
    {
        public string name;
        public string path;
        public bool active;
        public string tag;
        public int layer;
        public ComponentInfo[] components;
        public Vector3Data position;
        public Vector3Data rotation;
        public Vector3Data scale;
        public string[] children;
    }

    /// <summary>
    /// Component information
    /// </summary>
    [Serializable]
    public class ComponentInfo
    {
        public string type;
        public string name;
        public Dictionary<string, object> properties;  // For internal use
        public ComponentProperty[] serialized_properties;  // For JSON serialization
    }

    /// <summary>
    /// Single property key-value pair for JSON serialization
    /// </summary>
    [Serializable]
    public class ComponentProperty
    {
        public string key;
        public string value;
        public string valueType;

        public ComponentProperty() { }

        public ComponentProperty(string key, object value)
        {
            this.key = key;
            this.value = value?.ToString() ?? "null";
            this.valueType = value?.GetType().Name ?? "null";
        }
    }

    /// <summary>
    /// Scene metadata
    /// </summary>
    [Serializable]
    public class SceneInfoData
    {
        public string name;
        public string path;
        public bool is_loaded;
        public bool is_modified;
        public int object_count;
        public int root_object_count;
        public string build_index;
    }

    /// <summary>
    /// Server status information
    /// </summary>
    [Serializable]
    public class ServerStatusData
    {
        public bool is_running;
        public int port;
        public string unity_version;
        public string editor_mode;
        public int commands_processed;
        public int success_count;
        public int error_count;
        public float uptime_seconds;
    }

    /// <summary>
    /// Command log entry for UI display
    /// </summary>
    [Serializable]
    public class CommandLogEntry
    {
        public string timestamp;
        public string endpoint;
        public string method;
        public string status;
        public string message;
        public float execution_time;

        public CommandLogEntry(string endpoint, string method)
        {
            this.timestamp = DateTime.Now.ToString("HH:mm:ss");
            this.endpoint = endpoint;
            this.method = method;
        }
    }
}
