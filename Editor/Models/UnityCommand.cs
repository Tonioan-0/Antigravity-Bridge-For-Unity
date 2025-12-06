using System;
using System.Collections.Generic;
using UnityEngine;

namespace AntigravityBridge.Editor.Models
{
    /// <summary>
    /// Base command structure for all Unity API requests
    /// </summary>
    [Serializable]
    public class UnityCommand
    {
        public string command;
        public CommandParameters parameters;
    }

    /// <summary>
    /// v2: Unix-like command request
    /// POST /unity/command
    /// Body: { "cmd": "find . --component Light" }
    /// </summary>
    [Serializable]
    public class CommandRequest
    {
        public string cmd;
    }

    /// <summary>
    /// Parameters for Unity commands
    /// </summary>
    [Serializable]
    public class CommandParameters
    {
        // Find/filter criteria
        public string parent;
        public string name;
        public string tag;
        public string component;
        public FilterCriteria filter;

        // Object creation
        public Vector3Data position;
        public Vector3Data rotation;
        public Vector3Data scale;
        public string[] components;

        // Object modification
        public string[] objects;
        public PropertyModification[] properties;
        public Operation[] operations;

        // Component operations
        public string componentType;
        public Dictionary<string, object> componentProperties;

        // Settings
        public string category;
        public Dictionary<string, object> settings;
    }

    /// <summary>
    /// Filter criteria for finding GameObjects
    /// </summary>
    [Serializable]
    public class FilterCriteria
    {
        public string type;           // "name", "tag", "component", "layer"
        public string value;
        public string component;      // Component type to filter by
        public bool includeInactive;  // Include inactive objects
        public bool recursive;        // Search recursively in children
    }

    /// <summary>
    /// Operation to perform on found objects
    /// </summary>
    [Serializable]
    public class Operation
    {
        public string type;           // "add_component", "remove_component", "modify_property", "delete", "set_active"
        public string component;
        public string property;
        public string value;
        public bool boolValue;
    }

    /// <summary>
    /// Property modification data
    /// </summary>
    [Serializable]
    public class PropertyModification
    {
        public string propertyPath;   // e.g., "transform.position.x" or "m_Color.r"
        public string value;
        public string type;           // "float", "int", "bool", "string", "vector3", "color"
    }

    /// <summary>
    /// Vector3 data for JSON serialization (Unity's Vector3 doesn't serialize well)
    /// </summary>
    [Serializable]
    public class Vector3Data
    {
        public float x;
        public float y;
        public float z;

        public Vector3Data() { }

        public Vector3Data(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Vector3Data(Vector3 vector)
        {
            this.x = vector.x;
            this.y = vector.y;
            this.z = vector.z;
        }

        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }
    }

    /// <summary>
    /// Find and modify command - combines finding objects with applying operations
    /// </summary>
    [Serializable]
    public class FindAndModifyCommand
    {
        public string parent;
        public FilterCriteria filter;
        public Operation[] operations;
    }

    /// <summary>
    /// Color data for JSON serialization
    /// Note: JsonUtility creates instances with default values, so we need IsSet() to check if it was actually provided
    /// </summary>
    [Serializable]
    public class ColorData
    {
        public float r = -1f;  // -1 = not set (default white would be 1,1,1)
        public float g = -1f;
        public float b = -1f;
        public float a = 1f;

        public ColorData() { }

        public ColorData(float r, float g, float b, float a = 1f)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        /// <summary>
        /// Check if color was actually set (not just default values)
        /// </summary>
        public bool IsSet()
        {
            return r >= 0 && g >= 0 && b >= 0;
        }

        public Color ToColor()
        {
            // If not set, return white as default
            if (!IsSet())
            {
                return Color.white;
            }
            return new Color(r, g, b, a);
        }
    }

    /// <summary>
    /// Create GameObject command
    /// </summary>
    [Serializable]
    public class CreateGameObjectCommand
    {
        public string name;
        public string parent;
        public Vector3Data position;
        public Vector3Data rotation;
        public Vector3Data scale;
        public string[] components;
        public ColorData color;  // Material color (optional)
    }

    /// <summary>
    /// Component operation command
    /// </summary>
    [Serializable]
    public class ComponentCommand
    {
        public string[] objects;      // GameObject names or paths
        public string component;      // Component type name
        public Dictionary<string, object> properties;  // For programmatic use (not deserialized by JsonUtility)
        public PropertyValue[] propertyValues;  // For JSON serialization
    }

    /// <summary>
    /// Property key-value for JSON serialization (JsonUtility cannot deserialize Dictionary)
    /// </summary>
    [Serializable]
    public class PropertyValue
    {
        public string key;
        public string stringValue;
        public float floatValue;
        public int intValue;
        public bool boolValue;
        public string valueType;  // "string", "float", "int", "bool"

        // Helper to get actual value
        public object GetValue()
        {
            switch (valueType?.ToLower())
            {
                case "float": return floatValue;
                case "int": return intValue;
                case "bool": return boolValue;
                default: return stringValue;
            }
        }
    }
}
