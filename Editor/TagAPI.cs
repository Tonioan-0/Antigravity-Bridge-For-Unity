using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using AntigravityBridge.Editor.Models;

namespace AntigravityBridge.Editor
{
    /// <summary>
    /// API for managing Tags and Layers
    /// Supports creating new tags and assigning them to objects
    /// </summary>
    public static class TagAPI
    {
        /// <summary>
        /// Create a new tag
        /// POST /unity/tag/create
        /// </summary>
        public static UnityResponse CreateTag(string json)
        {
            try
            {
                var command = JsonUtility.FromJson<TagCreateCommand>(json);
                
                if (string.IsNullOrEmpty(command.name))
                {
                    return UnityResponse.Error("Tag name not specified");
                }

                // Check if tag already exists
                var existingTags = UnityEditorInternal.InternalEditorUtility.tags;
                if (existingTags.Contains(command.name))
                {
                    return UnityResponse.Success($"Tag '{command.name}' already exists", new ResponseData
                    {
                        affected_objects = new[] { command.name },
                        count = 1
                    });
                }

                // Add new tag via TagManager
                SerializedObject tagManager = new SerializedObject(
                    AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
                
                SerializedProperty tagsProp = tagManager.FindProperty("tags");
                
                // Find first empty slot or add new
                int insertIndex = tagsProp.arraySize;
                tagsProp.InsertArrayElementAtIndex(insertIndex);
                SerializedProperty newTag = tagsProp.GetArrayElementAtIndex(insertIndex);
                newTag.stringValue = command.name;
                
                tagManager.ApplyModifiedProperties();

                return UnityResponse.Success($"Created tag '{command.name}'", new ResponseData
                {
                    affected_objects = new[] { command.name },
                    count = 1
                });
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Tag creation failed: {e.Message}");
            }
        }

        /// <summary>
        /// Assign tag to objects
        /// POST /unity/tag/assign
        /// </summary>
        public static UnityResponse AssignTag(string json)
        {
            try
            {
                var command = JsonUtility.FromJson<TagAssignCommand>(json);
                
                if (string.IsNullOrEmpty(command.tag))
                {
                    return UnityResponse.Error("Tag not specified");
                }

                if (command.objects == null || command.objects.Length == 0)
                {
                    return UnityResponse.Error("No objects specified");
                }

                // Check if tag exists
                var existingTags = UnityEditorInternal.InternalEditorUtility.tags;
                if (!existingTags.Contains(command.tag))
                {
                    return UnityResponse.Error($"Tag '{command.tag}' does not exist. Create it first.");
                }

                var modifiedObjects = new List<string>();
                var errors = new List<string>();

                foreach (var objectName in command.objects)
                {
                    try
                    {
                        var obj = GameObject.Find(objectName);
                        if (obj == null)
                        {
                            errors.Add($"Object '{objectName}' not found");
                            continue;
                        }

                        Undo.RecordObject(obj, "Assign Tag");
                        obj.tag = command.tag;
                        EditorUtility.SetDirty(obj);
                        modifiedObjects.Add(objectName);
                    }
                    catch (Exception e)
                    {
                        errors.Add($"{objectName}: {e.Message}");
                    }
                }

                var data = new ResponseData
                {
                    affected_objects = modifiedObjects.ToArray(),
                    count = modifiedObjects.Count,
                    errors = errors.ToArray()
                };

                if (modifiedObjects.Count == 0)
                {
                    return UnityResponse.Error("No objects tagged", errors.ToArray());
                }

                return UnityResponse.Success($"Tagged {modifiedObjects.Count} objects with '{command.tag}'", data);
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Tag assignment failed: {e.Message}");
            }
        }

        /// <summary>
        /// Get all available tags
        /// GET /unity/tag/list
        /// </summary>
        public static UnityResponse GetAllTags()
        {
            try
            {
                var tags = UnityEditorInternal.InternalEditorUtility.tags;
                
                return UnityResponse.Success($"Found {tags.Length} tags", new ResponseData
                {
                    affected_objects = tags,
                    count = tags.Length
                });
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Failed to list tags: {e.Message}");
            }
        }

        /// <summary>
        /// Assign layer to objects
        /// POST /unity/layer/assign
        /// </summary>
        public static UnityResponse AssignLayer(string json)
        {
            try
            {
                var command = JsonUtility.FromJson<LayerAssignCommand>(json);
                
                if (command.objects == null || command.objects.Length == 0)
                {
                    return UnityResponse.Error("No objects specified");
                }

                int layer = command.layer;
                
                // Try to get layer by name if not a number
                if (!string.IsNullOrEmpty(command.layerName))
                {
                    layer = LayerMask.NameToLayer(command.layerName);
                    if (layer == -1)
                    {
                        return UnityResponse.Error($"Layer '{command.layerName}' not found");
                    }
                }

                var modifiedObjects = new List<string>();
                var errors = new List<string>();

                foreach (var objectName in command.objects)
                {
                    try
                    {
                        var obj = GameObject.Find(objectName);
                        if (obj == null)
                        {
                            errors.Add($"Object '{objectName}' not found");
                            continue;
                        }

                        Undo.RecordObject(obj, "Assign Layer");
                        obj.layer = layer;
                        EditorUtility.SetDirty(obj);
                        modifiedObjects.Add(objectName);
                    }
                    catch (Exception e)
                    {
                        errors.Add($"{objectName}: {e.Message}");
                    }
                }

                var data = new ResponseData
                {
                    affected_objects = modifiedObjects.ToArray(),
                    count = modifiedObjects.Count,
                    errors = errors.ToArray()
                };

                return UnityResponse.Success($"Set layer on {modifiedObjects.Count} objects", data);
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Layer assignment failed: {e.Message}");
            }
        }
    }

    #region Tag/Layer Models

    [Serializable]
    public class TagCreateCommand
    {
        public string name;
    }

    [Serializable]
    public class TagAssignCommand
    {
        public string[] objects;
        public string tag;
    }

    [Serializable]
    public class LayerAssignCommand
    {
        public string[] objects;
        public int layer;
        public string layerName;  // Alternative to layer number
    }

    #endregion
}
