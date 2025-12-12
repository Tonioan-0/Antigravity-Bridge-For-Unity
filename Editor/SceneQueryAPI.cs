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
        // Default precision for position/rotation/scale (3 decimal places)
        private const int DEFAULT_PRECISION = 3;

        /// <summary>
        /// Round a float to specified decimal places
        /// </summary>
        private static float Round(float value, int precision)
        {
            float multiplier = (float)Math.Pow(10, precision);
            return (float)Math.Round(value * multiplier) / multiplier;
        }

        /// <summary>
        /// Create a Vector3Data with rounded values
        /// </summary>
        private static Vector3Data RoundedVector3(Vector3 v, int precision)
        {
            return new Vector3Data
            {
                x = Round(v.x, precision),
                y = Round(v.y, precision),
                z = Round(v.z, precision)
            };
        }

        /// <summary>
        /// Get complete scene hierarchy with all GameObjects
        /// </summary>
        public static UnityResponse GetSceneHierarchy(QueryOptions options = null)
        {
            try
            {
                options = options ?? new QueryOptions();
                var scene = SceneManager.GetActiveScene();
                var rootObjects = scene.GetRootGameObjects();

                // Limit depth to prevent JsonUtility serialization overflow (max 10 levels)
                // Use 3 as default to stay safely under limit (JSON structure adds extra levels)
                // Note: GameObjectNode nested structure counts toward the limit
                int maxDepth = options.depth <= 0 ? 3 : Math.Min(options.depth, 3);

                // Apply format option - names_only returns Unity-style hierarchy
                if (options.format == "names_only")
                {
                    var hierarchyLines = new List<string>();
                    foreach (var root in rootObjects)
                    {
                        BuildHierarchyTree(root, hierarchyLines, 0, maxDepth);
                    }
                    
                    // Return as simple list of hierarchy lines
                    var data = new ResponseData
                    {
                        affected_objects = hierarchyLines.ToArray(),
                        count = hierarchyLines.Count
                    };
                    return UnityResponse.Success($"Scene hierarchy ({hierarchyLines.Count} objects)", data);
                }

                var hierarchyData = new SceneHierarchyData
                {
                    scene_name = scene.name,
                    scene_path = scene.path,
                    root_objects = rootObjects.Select(o => BuildGameObjectNode(o, maxDepth, 0, options.precision)).ToArray()
                };

                var responseData = new ResponseData
                {
                    scene_hierarchy = hierarchyData
                };

                return UnityResponse.Success($"Retrieved hierarchy with {rootObjects.Length} root objects", responseData);
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Failed to get scene hierarchy: {e.Message}");
            }
        }

        /// <summary>
        /// Build simple hierarchical tree with indentation (like Unity hierarchy view)
        /// Format: "  └─ ChildName" with depth-based indentation
        /// </summary>
        private static void BuildHierarchyTree(GameObject obj, List<string> lines, int depth, int maxDepth)
        {
            // Create indented line
            string indent = depth == 0 ? "" : new string(' ', (depth - 1) * 3) + "└─ ";
            lines.Add(indent + obj.name);
            
            // Recurse into children if within depth limit
            if (maxDepth == -1 || depth < maxDepth)
            {
                for (int i = 0; i < obj.transform.childCount; i++)
                {
                    BuildHierarchyTree(obj.transform.GetChild(i).gameObject, lines, depth + 1, maxDepth);
                }
            }
        }

        /// <summary>
        /// Build GameObject node with children recursively
        /// </summary>
        private static GameObjectNode BuildGameObjectNode(GameObject obj, int maxDepth = -1, int currentDepth = 0, int precision = DEFAULT_PRECISION)
        {
            var transform = obj.transform;
            var components = obj.GetComponents<Component>()
                .Where(c => c != null)
                .Select(c => c.GetType().Name)
                .ToArray();

            // Build children only if within depth limit
            GameObjectNode[] children;
            if (maxDepth == -1 || currentDepth < maxDepth)
            {
                children = new GameObjectNode[transform.childCount];
                for (int i = 0; i < transform.childCount; i++)
                {
                    children[i] = BuildGameObjectNode(transform.GetChild(i).gameObject, maxDepth, currentDepth + 1, precision);
                }
            }
            else
            {
                children = new GameObjectNode[0]; // Don't recurse deeper
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
                position = RoundedVector3(transform.position, precision),
                rotation = RoundedVector3(transform.eulerAngles, precision),
                scale = RoundedVector3(transform.localScale, precision)
            };
        }

        /// <summary>
        /// Get detailed information about a specific GameObject
        /// Supports QueryOptions for select fields and format
        /// </summary>
        public static UnityResponse GetObjectInfo(string objectName, QueryOptions options = null)
        {
            try
            {
                options = options ?? new QueryOptions();
                
                var obj = GameObject.Find(objectName);
                if (obj == null)
                {
                    // Try to find by path
                    obj = FindObjectByPath(objectName);
                }

                // Handle exists_only format
                if (options.format == "exists_only")
                {
                    var existsData = new ResponseData
                    {
                        count = obj != null ? 1 : 0
                    };
                    return obj != null 
                        ? UnityResponse.Success($"Object '{objectName}' exists", existsData)
                        : UnityResponse.Error($"Object '{objectName}' not found");
                }

                if (obj == null)
                {
                    return UnityResponse.Error($"GameObject '{objectName}' not found");
                }

                var transform = obj.transform;
                var precision = options.precision;

                // Check what fields are selected
                bool selectAll = options.select == null || options.select.Length == 0;
                bool includeComponents = selectAll || options.select.Contains("components");
                bool includeChildren = selectAll || options.select.Contains("children");
                bool includeTransform = selectAll || options.select.Contains("position") || 
                                        options.select.Contains("rotation") || options.select.Contains("scale");

                ComponentInfo[] components = null;
                if (includeComponents)
                {
                    components = obj.GetComponents<Component>()
                        .Where(c => c != null)
                        .Select(c => {
                            var props = GetComponentProperties(c);
                            return new ComponentInfo
                            {
                                type = c.GetType().Name,
                                name = c.GetType().FullName,
                                properties = props,
                                serialized_properties = props.Select(kvp => new ComponentProperty(kvp.Key, kvp.Value)).ToArray()
                            };
                        })
                        .ToArray();
                }

                string[] children = null;
                if (includeChildren)
                {
                    children = new string[transform.childCount];
                    for (int i = 0; i < transform.childCount; i++)
                    {
                        children[i] = transform.GetChild(i).name;
                    }
                }

                var objectData = new GameObjectData
                {
                    name = obj.name,
                    path = GetGameObjectPath(obj),
                    active = obj.activeSelf,
                    tag = obj.tag,
                    layer = obj.layer,
                    components = components,
                    position = includeTransform ? RoundedVector3(transform.position, precision) : null,
                    rotation = includeTransform ? RoundedVector3(transform.eulerAngles, precision) : null,
                    scale = includeTransform ? RoundedVector3(transform.localScale, precision) : null,
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

        /// <summary>
        /// Extract properties from a component using reflection
        /// Returns serialized fields and common properties
        /// </summary>
        private static Dictionary<string, object> GetComponentProperties(Component component)
        {
            var properties = new Dictionary<string, object>();
            
            if (component == null) return properties;
            
            try
            {
                var type = component.GetType();
                
                // Get all serialized fields (public or with [SerializeField])
                var fields = type.GetFields(System.Reflection.BindingFlags.Public | 
                                            System.Reflection.BindingFlags.NonPublic | 
                                            System.Reflection.BindingFlags.Instance);
                
                foreach (var field in fields)
                {
                    // Skip if not serializable
                    bool isPublic = field.IsPublic;
                    bool hasSerializeField = field.GetCustomAttributes(typeof(SerializeField), true).Length > 0;
                    bool hasHideInInspector = field.GetCustomAttributes(typeof(HideInInspector), true).Length > 0;
                    
                    if ((isPublic || hasSerializeField) && !hasHideInInspector)
                    {
                        try
                        {
                            var value = field.GetValue(component);
                            properties[field.Name] = ConvertValueForJson(value);
                        }
                        catch
                        {
                            // Skip fields that can't be accessed
                        }
                    }
                }
                
                // For common Unity components, add specific useful properties
                AddCommonComponentProperties(component, properties);
            }
            catch
            {
                // Return empty dict on error
            }
            
            return properties;
        }

        /// <summary>
        /// Add common properties for standard Unity components
        /// </summary>
        private static void AddCommonComponentProperties(Component component, Dictionary<string, object> properties)
        {
            // Light properties
            if (component is Light light)
            {
                properties["color"] = new { r = light.color.r, g = light.color.g, b = light.color.b, a = light.color.a };
                properties["intensity"] = light.intensity;
                properties["range"] = light.range;
                properties["type"] = light.type.ToString();
                properties["shadows"] = light.shadows.ToString();
            }
            // AudioSource properties
            else if (component is AudioSource audio)
            {
                properties["volume"] = audio.volume;
                properties["pitch"] = audio.pitch;
                properties["loop"] = audio.loop;
                properties["spatialBlend"] = audio.spatialBlend;
                properties["clip"] = audio.clip != null ? audio.clip.name : null;
            }
            // Camera properties
            else if (component is Camera cam)
            {
                properties["fieldOfView"] = cam.fieldOfView;
                properties["nearClipPlane"] = cam.nearClipPlane;
                properties["farClipPlane"] = cam.farClipPlane;
                properties["depth"] = cam.depth;
            }
            // Rigidbody properties
            else if (component is Rigidbody rb)
            {
                properties["mass"] = rb.mass;
                properties["drag"] = rb.linearDamping;          // Use drag for Unity version compatibility
                properties["angularDrag"] = rb.angularDamping;  // Use angularDrag for Unity version compatibility
                properties["useGravity"] = rb.useGravity;
                properties["isKinematic"] = rb.isKinematic;
            }
            // CharacterController properties
            else if (component is CharacterController cc)
            {
                properties["height"] = cc.height;
                properties["radius"] = cc.radius;
                properties["slopeLimit"] = cc.slopeLimit;
                properties["stepOffset"] = cc.stepOffset;
            }
            // Collider properties
            else if (component is Collider col)
            {
                properties["isTrigger"] = col.isTrigger;
                properties["enabled"] = col.enabled;
            }
            // Renderer properties (for Material info)
            else if (component is Renderer rend)
            {
                properties["enabled"] = rend.enabled;
                if (rend.sharedMaterial != null)
                {
                    properties["material"] = rend.sharedMaterial.name;
                    properties["shader"] = rend.sharedMaterial.shader.name;
                }
            }
        }

        /// <summary>
        /// Convert value to JSON-safe format
        /// </summary>
        private static object ConvertValueForJson(object value)
        {
            if (value == null) return null;
            
            var type = value.GetType();
            
            // Primitives and strings
            if (type.IsPrimitive || value is string || value is decimal)
            {
                return value;
            }
            
            // Vector3
            if (value is Vector3 v3)
            {
                return new { x = v3.x, y = v3.y, z = v3.z };
            }
            
            // Vector2
            if (value is Vector2 v2)
            {
                return new { x = v2.x, y = v2.y };
            }
            
            // Color
            if (value is Color c)
            {
                return new { r = c.r, g = c.g, b = c.b, a = c.a };
            }
            
            // Quaternion
            if (value is Quaternion q)
            {
                return new { x = q.x, y = q.y, z = q.z, w = q.w };
            }
            
            // Enums
            if (type.IsEnum)
            {
                return value.ToString();
            }
            
            // UnityEngine.Object references (GameObjects, Components, Assets)
            if (value is UnityEngine.Object unityObj)
            {
                return unityObj != null ? unityObj.name : null;
            }
            
            // Arrays and lists - return count only to avoid huge responses
            if (type.IsArray)
            {
                var arr = value as Array;
                return $"[Array: {arr?.Length ?? 0} items]";
            }
            
            // For other types, return type name
            return $"[{type.Name}]";
        }
    }
}
