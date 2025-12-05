using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using AntigravityBridge.Editor.Models;

namespace AntigravityBridge.Editor
{
    /// <summary>
    /// Handles all scene query operations - read-only queries that don't modify the scene
    /// </summary>
    public static class SceneQueryAPI
    {
        /// <summary>
        /// Get complete scene hierarchy with all GameObjects
        /// </summary>
        public static UnityResponse GetSceneHierarchy()
        {
            try
            {
                var scene = SceneManager.GetActiveScene();
                var rootObjects = scene.GetRootGameObjects();

                var hierarchyData = new SceneHierarchyData
                {
                    scene_name = scene.name,
                    scene_path = scene.path,
                    root_objects = rootObjects.Select(BuildGameObjectNode).ToArray()
                };

                var data = new ResponseData
                {
                    scene_hierarchy = hierarchyData
                };

                return UnityResponse.Success($"Retrieved hierarchy with {rootObjects.Length} root objects", data);
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Failed to get scene hierarchy: {e.Message}");
            }
        }

        /// <summary>
        /// Build GameObject node with children recursively
        /// </summary>
        private static GameObjectNode BuildGameObjectNode(GameObject obj)
        {
            var transform = obj.transform;
            var components = obj.GetComponents<Component>()
                .Where(c => c != null)
                .Select(c => c.GetType().Name)
                .ToArray();

            var children = new GameObjectNode[transform.childCount];
            for (int i = 0; i < transform.childCount; i++)
            {
                children[i] = BuildGameObjectNode(transform.GetChild(i).gameObject);
            }

            return new GameObjectNode
            {
                name = obj.name,
                path = GetGameObjectPath(obj),
                active = obj.activeSelf,
                tag = obj.tag,
                layer = obj.layer,
                components = components,
                children = children,
                position = new Vector3Data(transform.position),
                rotation = new Vector3Data(transform.eulerAngles),
                scale = new Vector3Data(transform.localScale)
            };
        }

        /// <summary>
        /// Get detailed information about a specific GameObject
        /// </summary>
        public static UnityResponse GetObjectInfo(string objectName)
        {
            try
            {
                var obj = GameObject.Find(objectName);
                if (obj == null)
                {
                    // Try to find by path
                    obj = FindObjectByPath(objectName);
                }

                if (obj == null)
                {
                    return UnityResponse.Error($"GameObject '{objectName}' not found");
                }

                var transform = obj.transform;
                var components = obj.GetComponents<Component>()
                    .Where(c => c != null)
                    .Select(c => new ComponentInfo
                    {
                        type = c.GetType().Name,
                        name = c.GetType().FullName,
                        properties = new Dictionary<string, object>()
                    })
                    .ToArray();

                var children = new string[transform.childCount];
                for (int i = 0; i < transform.childCount; i++)
                {
                    children[i] = transform.GetChild(i).name;
                }

                var objectData = new GameObjectData
                {
                    name = obj.name,
                    path = GetGameObjectPath(obj),
                    active = obj.activeSelf,
                    tag = obj.tag,
                    layer = obj.layer,
                    components = components,
                    position = new Vector3Data(transform.position),
                    rotation = new Vector3Data(transform.eulerAngles),
                    scale = new Vector3Data(transform.localScale),
                    children = children
                };

                var data = new ResponseData
                {
                    object_info = objectData
                };

                return UnityResponse.Success($"Retrieved info for '{objectName}'", data);
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Failed to get object info: {e.Message}");
            }
        }

        /// <summary>
        /// Get current scene information
        /// </summary>
        public static UnityResponse GetSceneInfo()
        {
            try
            {
                var scene = SceneManager.GetActiveScene();
                var rootObjects = scene.GetRootGameObjects();
                int totalObjects = CountAllObjects(rootObjects);

                var sceneInfo = new SceneInfoData
                {
                    name = scene.name,
                    path = scene.path,
                    is_loaded = scene.isLoaded,
                    is_modified = scene.isDirty,
                    object_count = totalObjects,
                    root_object_count = rootObjects.Length,
                    build_index = scene.buildIndex.ToString()
                };

                var data = new ResponseData
                {
                    scene_info = sceneInfo
                };

                return UnityResponse.Success($"Scene: {scene.name}", data);
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Failed to get scene info: {e.Message}");
            }
        }

        /// <summary>
        /// Get all available MonoBehaviour scripts in the project
        /// </summary>
        public static UnityResponse GetAvailableScripts()
        {
            try
            {
                // Find all MonoBehaviour types in the project
                var scripts = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(assembly =>
                    {
                        try
                        {
                            return assembly.GetTypes();
                        }
                        catch
                        {
                            return new Type[0];
                        }
                    })
                    .Where(type => type.IsSubclassOf(typeof(MonoBehaviour)) && !type.IsAbstract)
                    .Select(type => type.FullName)
                    .OrderBy(name => name)
                    .ToArray();

                var data = new ResponseData
                {
                    available_scripts = scripts,
                    count = scripts.Length
                };

                return UnityResponse.Success($"Found {scripts.Length} scripts", data);
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Failed to get available scripts: {e.Message}");
            }
        }

        /// <summary>
        /// Get all available component types
        /// </summary>
        public static UnityResponse GetAvailableComponents()
        {
            try
            {
                var components = new List<string>
                {
                    // Common Unity components
                    "Transform", "RectTransform", "Camera", "Light", "AudioSource", "AudioListener",
                    "MeshFilter", "MeshRenderer", "SkinnedMeshRenderer", "ParticleSystem",
                    "BoxCollider", "SphereCollider", "CapsuleCollider", "MeshCollider",
                    "Rigidbody", "CharacterController", "Animator", "Animation",
                    "Canvas", "CanvasRenderer", "Image", "Text", "Button", "Slider",
                    "ScrollRect", "InputField", "Dropdown", "Toggle"
                };

                // Add all MonoBehaviour scripts
                var scripts = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(assembly =>
                    {
                        try
                        {
                            return assembly.GetTypes();
                        }
                        catch
                        {
                            return new Type[0];
                        }
                    })
                    .Where(type => type.IsSubclassOf(typeof(MonoBehaviour)) && !type.IsAbstract)
                    .Select(type => type.Name);

                components.AddRange(scripts);

                var distinctComponents = components.Distinct().OrderBy(c => c).ToArray();

                var data = new ResponseData
                {
                    available_components = distinctComponents,
                    count = distinctComponents.Length
                };

                return UnityResponse.Success($"Found {distinctComponents.Length} components", data);
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Failed to get available components: {e.Message}");
            }
        }

        /// <summary>
        /// Get full hierarchy path of a GameObject
        /// </summary>
        private static string GetGameObjectPath(GameObject obj)
        {
            string path = obj.name;
            Transform current = obj.transform.parent;

            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return path;
        }

        /// <summary>
        /// Find GameObject by hierarchy path (e.g., "Parent/Child/Object")
        /// </summary>
        private static GameObject FindObjectByPath(string path)
        {
            var parts = path.Split('/');
            GameObject current = null;

            // Find root object
            var rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            current = rootObjects.FirstOrDefault(obj => obj.name == parts[0]);

            if (current == null) return null;

            // Traverse hierarchy
            for (int i = 1; i < parts.Length; i++)
            {
                Transform child = current.transform.Find(parts[i]);
                if (child == null) return null;
                current = child.gameObject;
            }

            return current;
        }

        /// <summary>
        /// Count all GameObjects including children recursively
        /// </summary>
        private static int CountAllObjects(GameObject[] rootObjects)
        {
            int count = 0;
            foreach (var obj in rootObjects)
            {
                count += CountObjectAndChildren(obj);
            }
            return count;
        }

        private static int CountObjectAndChildren(GameObject obj)
        {
            int count = 1;
            for (int i = 0; i < obj.transform.childCount; i++)
            {
                count += CountObjectAndChildren(obj.transform.GetChild(i).gameObject);
            }
            return count;
        }
    }
}
