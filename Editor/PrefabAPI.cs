using System;
using UnityEditor;
using UnityEngine;
using AntigravityBridge.Editor.Models;

namespace AntigravityBridge.Editor
{
    /// <summary>
    /// Handles Prefab operations
    /// </summary>
    public static class PrefabAPI
    {
        /// <summary>
        /// Instantiate a prefab from the project
        /// POST /unity/prefab/instantiate
        /// </summary>
        public static UnityResponse Instantiate(string json)
        {
            try
            {
                var command = JsonUtility.FromJson<InstantiatePrefabCommand>(json);

                if (string.IsNullOrEmpty(command.prefabPath))
                {
                    return UnityResponse.Error("Prefab path is missing");
                }

                // Load Prefab asset
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(command.prefabPath);
                if (prefab == null)
                {
                    // Try adding .prefab extension if missing
                    if (!command.prefabPath.EndsWith(".prefab"))
                    {
                        prefab = AssetDatabase.LoadAssetAtPath<GameObject>(command.prefabPath + ".prefab");
                    }

                    if (prefab == null)
                    {
                        return UnityResponse.Error($"Prefab not found at path: {command.prefabPath}");
                    }
                }

                // Instantiate Prefab
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                Undo.RegisterCreatedObjectUndo(instance, "Instantiate Prefab");

                // Set parent
                if (!string.IsNullOrEmpty(command.parent))
                {
                    var parentObj = GameObject.Find(command.parent);
                    if (parentObj != null)
                    {
                        instance.transform.SetParent(parentObj.transform);
                    }
                }

                // Apply transform
                if (command.position != null)
                {
                    instance.transform.position = command.position.ToVector3();
                }
                if (command.rotation != null)
                {
                    instance.transform.eulerAngles = command.rotation.ToVector3();
                }
                if (command.scale != null)
                {
                    instance.transform.localScale = command.scale.ToVector3();
                }

                // Rename if requested
                if (!string.IsNullOrEmpty(command.name))
                {
                    instance.name = command.name;
                }

                var data = new ResponseData
                {
                    affected_objects = new[] { instance.name },
                    count = 1
                };

                return UnityResponse.Success($"Instantiated prefab '{instance.name}'", data);
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Prefab instantiation failed: {e.Message}");
            }
        }
    }
}
