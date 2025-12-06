using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using AntigravityBridge.Editor.Models;

namespace AntigravityBridge.Editor
{
    /// <summary>
    /// Fluent command builder for creating, modifying, and deleting GameObjects
    /// Provides Unix-like command structure: create Cube --position 0,1,0 --components BoxCollider
    /// </summary>
    public class CommandBuilder
    {
        public enum CommandType { Create, Modify, Delete }
        
        private CommandType _type;
        private string _name;
        private string _parent;
        private Vector3? _position;
        private Vector3? _rotation;
        private Vector3? _scale;
        private List<string> _components = new List<string>();
        private List<Operation> _operations = new List<Operation>();
        private List<string> _targets = new List<string>();
        private bool _force = false;
        private bool _recursive = false;

        private CommandBuilder(CommandType type)
        {
            _type = type;
        }

        #region Static Factory Methods

        /// <summary>
        /// Create a new GameObject (create Cube)
        /// </summary>
        public static CommandBuilder Create(string name)
        {
            return new CommandBuilder(CommandType.Create) { _name = name };
        }

        /// <summary>
        /// Modify existing GameObjects (modify Player)
        /// </summary>
        public static CommandBuilder Modify(params string[] targets)
        {
            var builder = new CommandBuilder(CommandType.Modify);
            builder._targets.AddRange(targets);
            return builder;
        }

        /// <summary>
        /// Delete GameObjects (delete TempObjects)
        /// </summary>
        public static CommandBuilder Delete(params string[] targets)
        {
            var builder = new CommandBuilder(CommandType.Delete);
            builder._targets.AddRange(targets);
            return builder;
        }

        #endregion

        #region Create Modifiers

        /// <summary>
        /// Set position (--position 0,1,0)
        /// </summary>
        public CommandBuilder Position(float x, float y, float z)
        {
            _position = new Vector3(x, y, z);
            return this;
        }

        /// <summary>
        /// Set position from Vector3
        /// </summary>
        public CommandBuilder Position(Vector3 pos)
        {
            _position = pos;
            return this;
        }

        /// <summary>
        /// Set rotation (--rotation 0,90,0)
        /// </summary>
        public CommandBuilder Rotation(float x, float y, float z)
        {
            _rotation = new Vector3(x, y, z);
            return this;
        }

        /// <summary>
        /// Set scale (--scale 2,2,2)
        /// </summary>
        public CommandBuilder Scale(float x, float y, float z)
        {
            _scale = new Vector3(x, y, z);
            return this;
        }

        /// <summary>
        /// Set uniform scale
        /// </summary>
        public CommandBuilder Scale(float uniform)
        {
            _scale = new Vector3(uniform, uniform, uniform);
            return this;
        }

        /// <summary>
        /// Set parent object (--parent Environment)
        /// </summary>
        public CommandBuilder Parent(string parent)
        {
            _parent = parent;
            return this;
        }

        /// <summary>
        /// Add components (--components BoxCollider,Rigidbody)
        /// </summary>
        public CommandBuilder Components(params string[] components)
        {
            _components.AddRange(components);
            return this;
        }

        /// <summary>
        /// Add a single component
        /// </summary>
        public CommandBuilder AddComponent(string component)
        {
            _components.Add(component);
            return this;
        }

        #endregion

        #region Modify Modifiers

        /// <summary>
        /// Set active state (--set active=false)
        /// </summary>
        public CommandBuilder SetActive(bool active)
        {
            _operations.Add(new Operation
            {
                type = "set_active",
                boolValue = active
            });
            return this;
        }

        /// <summary>
        /// Add component operation (--add BoxCollider)
        /// </summary>
        public CommandBuilder Add(string component)
        {
            _operations.Add(new Operation
            {
                type = "add_component",
                component = component
            });
            return this;
        }

        /// <summary>
        /// Remove component operation (--remove AudioSource)
        /// </summary>
        public CommandBuilder Remove(string component)
        {
            _operations.Add(new Operation
            {
                type = "remove_component",
                component = component
            });
            return this;
        }

        /// <summary>
        /// Set property value (--set transform.position.x=5)
        /// </summary>
        public CommandBuilder Set(string property, string value)
        {
            _operations.Add(new Operation
            {
                type = "modify_property",
                property = property,
                value = value
            });
            return this;
        }

        #endregion

        #region Delete Modifiers

        /// <summary>
        /// Force delete without confirmation (--force)
        /// </summary>
        public CommandBuilder Force(bool force = true)
        {
            _force = force;
            return this;
        }

        /// <summary>
        /// Delete recursively (--recursive)
        /// </summary>
        public CommandBuilder Recursive(bool recursive = true)
        {
            _recursive = recursive;
            return this;
        }

        #endregion

        #region Execute Methods

        /// <summary>
        /// Execute the command
        /// </summary>
        public UnityResponse Execute()
        {
            switch (_type)
            {
                case CommandType.Create:
                    return ExecuteCreate();
                case CommandType.Modify:
                    return ExecuteModify();
                case CommandType.Delete:
                    return ExecuteDelete();
                default:
                    return UnityResponse.Error("Unknown command type");
            }
        }

        private UnityResponse ExecuteCreate()
        {
            try
            {
                // Create the GameObject
                var go = new GameObject(_name);
                Undo.RegisterCreatedObjectUndo(go, $"Create {_name}");

                // Set parent
                if (!string.IsNullOrEmpty(_parent))
                {
                    var parentObj = GameObject.Find(_parent);
                    if (parentObj != null)
                    {
                        go.transform.SetParent(parentObj.transform);
                    }
                }

                // Set transform
                if (_position.HasValue)
                    go.transform.position = _position.Value;
                if (_rotation.HasValue)
                    go.transform.eulerAngles = _rotation.Value;
                if (_scale.HasValue)
                    go.transform.localScale = _scale.Value;

                // Add components
                foreach (var compName in _components)
                {
                    var compType = FindComponentType(compName);
                    if (compType != null)
                    {
                        go.AddComponent(compType);
                    }
                }

                EditorUtility.SetDirty(go);

                return UnityResponse.Success($"Created '{_name}'", new ResponseData
                {
                    affected_objects = new[] { _name },
                    count = 1
                });
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Create failed: {e.Message}");
            }
        }

        private UnityResponse ExecuteModify()
        {
            try
            {
                var modified = new List<string>();

                foreach (var targetName in _targets)
                {
                    var obj = GameObject.Find(targetName);
                    if (obj == null) continue;

                    Undo.RecordObject(obj, $"Modify {targetName}");

                    foreach (var op in _operations)
                    {
                        ApplyOperation(obj, op);
                    }

                    // Apply transform changes
                    if (_position.HasValue)
                        obj.transform.position = _position.Value;
                    if (_rotation.HasValue)
                        obj.transform.eulerAngles = _rotation.Value;
                    if (_scale.HasValue)
                        obj.transform.localScale = _scale.Value;

                    EditorUtility.SetDirty(obj);
                    modified.Add(targetName);
                }

                return UnityResponse.Success($"Modified {modified.Count} objects", new ResponseData
                {
                    affected_objects = modified.ToArray(),
                    count = modified.Count
                });
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Modify failed: {e.Message}");
            }
        }

        private UnityResponse ExecuteDelete()
        {
            try
            {
                var deleted = new List<string>();

                foreach (var targetName in _targets)
                {
                    var obj = GameObject.Find(targetName);
                    if (obj == null) continue;

                    Undo.DestroyObjectImmediate(obj);
                    deleted.Add(targetName);
                }

                return UnityResponse.Success($"Deleted {deleted.Count} objects", new ResponseData
                {
                    affected_objects = deleted.ToArray(),
                    count = deleted.Count
                });
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Delete failed: {e.Message}");
            }
        }

        private void ApplyOperation(GameObject obj, Operation op)
        {
            switch (op.type)
            {
                case "set_active":
                    obj.SetActive(op.boolValue);
                    break;
                case "add_component":
                    var addType = FindComponentType(op.component);
                    if (addType != null)
                        obj.AddComponent(addType);
                    break;
                case "remove_component":
                    var removeType = FindComponentType(op.component);
                    if (removeType != null)
                    {
                        var comp = obj.GetComponent(removeType);
                        if (comp != null)
                            Undo.DestroyObjectImmediate(comp);
                    }
                    break;
            }
        }

        private Type FindComponentType(string typeName)
        {
            // Try common Unity types first
            var unityType = Type.GetType($"UnityEngine.{typeName}, UnityEngine");
            if (unityType != null) return unityType;

            // Try UI types
            var uiType = Type.GetType($"UnityEngine.UI.{typeName}, UnityEngine.UI");
            if (uiType != null) return uiType;

            // Try finding in all assemblies
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.Name == typeName && typeof(Component).IsAssignableFrom(type))
                            return type;
                    }
                }
                catch { }
            }

            return null;
        }

        #endregion

        #region String Representation

        /// <summary>
        /// Get Unix-like command representation
        /// </summary>
        public override string ToString()
        {
            var parts = new List<string>();

            switch (_type)
            {
                case CommandType.Create:
                    parts.Add($"create {_name}");
                    if (_position.HasValue)
                        parts.Add($"--position {_position.Value.x},{_position.Value.y},{_position.Value.z}");
                    if (_rotation.HasValue)
                        parts.Add($"--rotation {_rotation.Value.x},{_rotation.Value.y},{_rotation.Value.z}");
                    if (_scale.HasValue)
                        parts.Add($"--scale {_scale.Value.x},{_scale.Value.y},{_scale.Value.z}");
                    if (!string.IsNullOrEmpty(_parent))
                        parts.Add($"--parent {_parent}");
                    if (_components.Count > 0)
                        parts.Add($"--components {string.Join(",", _components)}");
                    break;

                case CommandType.Modify:
                    parts.Add($"modify {string.Join(" ", _targets)}");
                    foreach (var op in _operations)
                    {
                        switch (op.type)
                        {
                            case "set_active":
                                parts.Add($"--set active={op.boolValue}");
                                break;
                            case "add_component":
                                parts.Add($"--add {op.component}");
                                break;
                            case "remove_component":
                                parts.Add($"--remove {op.component}");
                                break;
                        }
                    }
                    break;

                case CommandType.Delete:
                    parts.Add($"delete {string.Join(" ", _targets)}");
                    if (_force) parts.Add("--force");
                    if (_recursive) parts.Add("--recursive");
                    break;
            }

            return string.Join(" ", parts);
        }

        #endregion
    }
}
