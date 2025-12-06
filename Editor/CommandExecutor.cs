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
    /// Executes commands that modify the scene (create, modify, delete GameObjects and components)
    /// All operations support Undo and mark scene as dirty
    /// </summary>
    public static class CommandExecutor
    {
        private const int MAX_BATCH_SIZE = 1000;

        /// <summary>
        /// Find GameObjects matching criteria
        /// POST /unity/scene/find
        /// </summary>
        public static UnityResponse FindObjects(string json)
        {
            try
            {
                var command = JsonUtility.FromJson<FindAndModifyCommand>(json);
                var foundObjects = FindGameObjects(command.parent, command.filter);

                var data = new ResponseData
                {
                    affected_objects = foundObjects.Select(obj => obj.name).ToArray(),
                    count = foundObjects.Count
                };

                return UnityResponse.Success($"Found {foundObjects.Count} objects", data);
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Find operation failed: {e.Message}");
            }
        }

        /// <summary>
        /// Create a new GameObject
        /// POST /unity/scene/create
        /// </summary>
        public static UnityResponse CreateObject(string json)
        {
            try
            {
                var command = JsonUtility.FromJson<CreateGameObjectCommand>(json);

                // Create GameObject
                GameObject newObj = new GameObject(command.name);
                Undo.RegisterCreatedObjectUndo(newObj, "Create GameObject");

                // Set parent
                if (!string.IsNullOrEmpty(command.parent))
                {
                    var parent = GameObject.Find(command.parent);
                    if (parent != null)
                    {
                        newObj.transform.SetParent(parent.transform);
                    }
                }

                // Set transform
                if (command.position != null)
                {
                    newObj.transform.position = command.position.ToVector3();
                }
                if (command.rotation != null)
                {
                    newObj.transform.eulerAngles = command.rotation.ToVector3();
                }
                
                // CRITICAL FIX: Set scale to (1,1,1) if not specified
                if (command.scale != null)
                {
                    newObj.transform.localScale = command.scale.ToVector3();
                }
                else
                {
                    // Default scale to (1,1,1) instead of (0,0,0)
                    newObj.transform.localScale = Vector3.one;
                }

                // Add components
                if (command.components != null && command.components.Length > 0)
                {
                    foreach (var componentName in command.components)
                    {
                        AddComponentByName(newObj, componentName);
                    }
                }
                
                // CRITICAL FIX: Setup mesh and material (always, not just when components exist)
                try
                {
                    SetupPrimitiveMeshAndMaterial(newObj, command.color);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to setup mesh/material for {newObj.name}: {ex.Message}");
                }

                EditorUtility.SetDirty(newObj);
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

                var data = new ResponseData
                {
                    affected_objects = new[] { newObj.name },
                    count = 1
                };

                return UnityResponse.Success($"Created GameObject '{command.name}'", data);
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Create operation failed: {e.Message}");
            }
        }

        /// <summary>
        /// Modify existing GameObjects
        /// POST /unity/scene/modify
        /// </summary>
        public static UnityResponse ModifyObjects(string json)
        {
            try
            {
                var command = JsonUtility.FromJson<ComponentCommand>(json);
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

                        Undo.RecordObject(obj, "Modify GameObject");

                        // Apply properties from the properties dictionary
                        if (command.properties != null)
                        {
                            ApplyProperties(obj, command.properties);
                        }

                        EditorUtility.SetDirty(obj);
                        modifiedObjects.Add(objectName);
                    }
                    catch (Exception e)
                    {
                        errors.Add($"{objectName}: {e.Message}");
                    }
                }

                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

                var data = new ResponseData
                {
                    affected_objects = modifiedObjects.ToArray(),
                    count = modifiedObjects.Count,
                    errors = errors.ToArray()
                };

                if (errors.Count > 0 && modifiedObjects.Count == 0)
                {
                    return UnityResponse.Error("All modifications failed", errors.ToArray());
                }
                else if (errors.Count > 0)
                {
                    return UnityResponse.Partial($"Modified {modifiedObjects.Count} objects with {errors.Count} errors", data);
                }

                return UnityResponse.Success($"Modified {modifiedObjects.Count} objects", data);
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Modify operation failed: {e.Message}");
            }
        }

        /// <summary>
        /// Delete GameObjects
        /// POST /unity/scene/delete
        /// </summary>
        public static UnityResponse DeleteObjects(string json)
        {
            try
            {
                var command = JsonUtility.FromJson<ComponentCommand>(json);
                
                if (command.objects == null || command.objects.Length == 0)
                {
                    return UnityResponse.Error("No objects specified for deletion");
                }

                if (command.objects.Length > MAX_BATCH_SIZE)
                {
                    return UnityResponse.Error($"Batch size exceeds maximum ({MAX_BATCH_SIZE})");
                }

                var deletedObjects = new List<string>();
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

                        Undo.DestroyObjectImmediate(obj);
                        deletedObjects.Add(objectName);
                    }
                    catch (Exception e)
                    {
                        errors.Add($"{objectName}: {e.Message}");
                    }
                }

                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

                var data = new ResponseData
                {
                    affected_objects = deletedObjects.ToArray(),
                    count = deletedObjects.Count,
                    errors = errors.ToArray()
                };

                return UnityResponse.Success($"Deleted {deletedObjects.Count} objects", data);
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Delete operation failed: {e.Message}");
            }
        }

        /// <summary>
        /// Add component to GameObjects
        /// POST /unity/component/add
        /// </summary>
        public static UnityResponse AddComponent(string json)
        {
            try
            {
                var command = JsonUtility.FromJson<ComponentCommand>(json);
                var modifiedObjects = new List<string>();
                var errors = new List<string>();

                if (string.IsNullOrEmpty(command.component))
                {
                    return UnityResponse.Error("Component type not specified");
                }

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

                        var component = AddComponentByName(obj, command.component);
                        if (component == null)
                        {
                            errors.Add($"{objectName}: Failed to add component '{command.component}'");
                            continue;
                        }

                        modifiedObjects.Add(objectName);
                    }
                    catch (Exception e)
                    {
                        errors.Add($"{objectName}: {e.Message}");
                    }
                }

                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

                var data = new ResponseData
                {
                    affected_objects = modifiedObjects.ToArray(),
                    count = modifiedObjects.Count,
                    errors = errors.ToArray()
                };

                if (errors.Count > 0 && modifiedObjects.Count == 0)
                {
                    return UnityResponse.Error("All component additions failed", errors.ToArray());
                }
                else if (errors.Count > 0)
                {
                    return UnityResponse.Partial($"Added component to {modifiedObjects.Count} objects with {errors.Count} errors", data);
                }

                return UnityResponse.Success($"Added component to {modifiedObjects.Count} objects", data);
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Add component operation failed: {e.Message}");
            }
        }

        /// <summary>
        /// Remove component from GameObjects
        /// POST /unity/component/remove
        /// </summary>
        public static UnityResponse RemoveComponent(string json)
        {
            try
            {
                var command = JsonUtility.FromJson<ComponentCommand>(json);
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

                        var component = obj.GetComponent(command.component);
                        if (component == null)
                        {
                            errors.Add($"{objectName}: Component '{command.component}' not found");
                            continue;
                        }

                        Undo.DestroyObjectImmediate(component);
                        modifiedObjects.Add(objectName);
                    }
                    catch (Exception e)
                    {
                        errors.Add($"{objectName}: {e.Message}");
                    }
                }

                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

                var data = new ResponseData
                {
                    affected_objects = modifiedObjects.ToArray(),
                    count = modifiedObjects.Count,
                    errors = errors.ToArray()
                };

                return UnityResponse.Success($"Removed component from {modifiedObjects.Count} objects", data);
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Remove component operation failed: {e.Message}");
            }
        }

        /// <summary>
        /// Modify component properties
        /// POST /unity/component/modify
        /// </summary>
        public static UnityResponse ModifyComponent(string json)
        {
            try
            {
                var command = JsonUtility.FromJson<ComponentCommand>(json);
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

                        var component = obj.GetComponent(command.component);
                        if (component == null)
                        {
                            errors.Add($"{objectName}: Component '{command.component}' not found");
                            continue;
                        }

                        Undo.RecordObject(component, "Modify Component");

                        // Use propertyValues array (JsonUtility compatible)
                        if (command.propertyValues != null && command.propertyValues.Length > 0)
                        {
                            ApplyPropertyValuesToComponent(component, command.propertyValues);
                        }
                        // Fallback to dictionary (for programmatic use)
                        else if (command.properties != null)
                        {
                            ApplyPropertiesToComponent(component, command.properties);
                        }

                        EditorUtility.SetDirty(component);
                        modifiedObjects.Add(objectName);
                    }
                    catch (Exception e)
                    {
                        errors.Add($"{objectName}: {e.Message}");
                    }
                }

                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

                var data = new ResponseData
                {
                    affected_objects = modifiedObjects.ToArray(),
                    count = modifiedObjects.Count,
                    errors = errors.ToArray()
                };

                return UnityResponse.Success($"Modified component on {modifiedObjects.Count} objects", data);
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Modify component operation failed: {e.Message}");
            }
        }

        /// <summary>
        /// Find objects and apply operations - the main command for Antigravity
        /// POST /unity/scene/find_and_modify
        /// Example: "Find all lights in 'viale' and add ActivateDisableLights script"
        /// </summary>
        public static UnityResponse FindAndModify(string json)
        {
            try
            {
                var command = JsonUtility.FromJson<FindAndModifyCommand>(json);
                
                // Find objects matching criteria
                var foundObjects = FindGameObjects(command.parent, command.filter);

                if (foundObjects.Count == 0)
                {
                    return UnityResponse.Success("No objects found matching criteria", new ResponseData { count = 0 });
                }

                if (foundObjects.Count > MAX_BATCH_SIZE)
                {
                    return UnityResponse.Error($"Found {foundObjects.Count} objects, exceeds maximum ({MAX_BATCH_SIZE})");
                }

                // Apply operations
                var modifiedObjects = new List<string>();
                var errors = new List<string>();

                foreach (var obj in foundObjects)
                {
                    try
                    {
                        foreach (var operation in command.operations)
                        {
                            ApplyOperation(obj, operation);
                        }
                        modifiedObjects.Add(obj.name);
                    }
                    catch (Exception e)
                    {
                        errors.Add($"{obj.name}: {e.Message}");
                    }
                }

                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

                var data = new ResponseData
                {
                    affected_objects = modifiedObjects.ToArray(),
                    count = modifiedObjects.Count,
                    errors = errors.ToArray()
                };

                if (errors.Count > 0 && modifiedObjects.Count == 0)
                {
                    return UnityResponse.Error("All operations failed", errors.ToArray());
                }
                else if (errors.Count > 0)
                {
                    return UnityResponse.Partial($"Modified {modifiedObjects.Count} objects with {errors.Count} errors", data);
                }

                return UnityResponse.Success($"Modified {modifiedObjects.Count} objects", data);
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Find and modify operation failed: {e.Message}");
            }
        }

        #region Helper Methods

        /// <summary>
        /// Find GameObjects matching criteria
        /// </summary>
        private static List<GameObject> FindGameObjects(string parentName, FilterCriteria filter)
        {
            var results = new List<GameObject>();
            GameObject parentObject = null;

            // Find parent object if specified
            if (!string.IsNullOrEmpty(parentName))
            {
                parentObject = GameObject.Find(parentName);
                if (parentObject == null)
                {
                    Debug.LogWarning($"Parent object '{parentName}' not found");
                    return results;
                }
            }

            // Get search root
            GameObject[] searchRoot;
            if (parentObject != null)
            {
                // Search only within parent's children
                searchRoot = GetAllChildren(parentObject);
            }
            else
            {
                // Search entire scene
                searchRoot = UnityEngine.Object.FindObjectsOfType<GameObject>(true);
            }

            // Apply filter
            if (filter == null)
            {
                results.AddRange(searchRoot);
            }
            else
            {
                foreach (var obj in searchRoot)
                {
                    if (MatchesFilter(obj, filter))
                    {
                        results.Add(obj);
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Check if GameObject matches filter criteria
        /// </summary>
        private static bool MatchesFilter(GameObject obj, FilterCriteria filter)
        {
            if (filter == null) return true;

            // Check active state
            if (!filter.includeInactive && !obj.activeInHierarchy)
            {
                return false;
            }

            // Check name
            if (!string.IsNullOrEmpty(filter.value))
            {
                if (filter.type == "name" && !obj.name.Contains(filter.value))
                {
                    return false;
                }
            }

            // Check tag
            if (filter.type == "tag" && !obj.CompareTag(filter.value))
            {
                return false;
            }

            // Check component
            if (!string.IsNullOrEmpty(filter.component))
            {
                var component = obj.GetComponent(filter.component);
                if (component == null)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Get all children of a GameObject recursively
        /// </summary>
        private static GameObject[] GetAllChildren(GameObject parent)
        {
            var children = new List<GameObject>();
            GetChildrenRecursive(parent.transform, children);
            return children.ToArray();
        }

        private static void GetChildrenRecursive(Transform parent, List<GameObject> list)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                list.Add(child.gameObject);
                GetChildrenRecursive(child, list);
            }
        }

        /// <summary>
        /// Apply operation to GameObject
        /// </summary>
        private static void ApplyOperation(GameObject obj, Operation operation)
        {
            switch (operation.type)
            {
                case "add_component":
                    AddComponentByName(obj, operation.component);
                    break;
                
                case "remove_component":
                    var comp = obj.GetComponent(operation.component);
                    if (comp != null)
                    {
                        Undo.DestroyObjectImmediate(comp);
                    }
                    break;

                case "set_active":
                    Undo.RecordObject(obj, "Set Active");
                    obj.SetActive(operation.boolValue);
                    break;

                case "delete":
                    Undo.DestroyObjectImmediate(obj);
                    break;

                default:
                    Debug.LogWarning($"Unknown operation type: {operation.type}");
                    break;
            }
        }

        /// <summary>
        /// Add component by name with validation
        /// </summary>
        private static Component AddComponentByName(GameObject obj, string componentName)
        {
            if (string.IsNullOrEmpty(componentName))
            {
                throw new ArgumentException("Component name is empty");
            }

            // Try to find the type
            Type componentType = FindComponentType(componentName);

            if (componentType == null)
            {
                throw new Exception($"Component type '{componentName}' not found");
            }

            // Check if it's a valid component type
            if (!typeof(Component).IsAssignableFrom(componentType))
            {
                throw new Exception($"Type '{componentName}' is not a Component");
            }

            // Add the component with Undo support
            var component = Undo.AddComponent(obj, componentType);
            EditorUtility.SetDirty(obj);

            return component;
        }

        /// <summary>
        /// Find component type by name (supports various formats)
        /// </summary>
        private static Type FindComponentType(string name)
        {
            // Try exact match first
            Type type = Type.GetType(name);
            if (type != null) return type;

            // Try with UnityEngine namespace
            type = Type.GetType($"UnityEngine.{name}");
            if (type != null) return type;

            // Search all loaded assemblies
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    type = assembly.GetType(name);
                    if (type != null) return type;

                    // Try to find by short name
                    var types = assembly.GetTypes();
                    type = types.FirstOrDefault(t => t.Name == name);
                    if (type != null) return type;
                }
                catch
                {
                    // Skip assemblies that can't be queried
                    continue;
                }
            }

            return null;
        }

        /// <summary>
        /// Apply properties to GameObject
        /// </summary>
        private static void ApplyProperties(GameObject obj, Dictionary<string, object> properties)
        {
            foreach (var kvp in properties)
            {
                try
                {
                    switch (kvp.Key.ToLower())
                    {
                        case "name":
                            obj.name = kvp.Value.ToString();
                            break;
                        case "tag":
                            obj.tag = kvp.Value.ToString();
                            break;
                        case "layer":
                            obj.layer = Convert.ToInt32(kvp.Value);
                            break;
                        case "active":
                            obj.SetActive(Convert.ToBoolean(kvp.Value));
                            break;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to set property '{kvp.Key}': {e.Message}");
                }
            }
        }

        /// <summary>
        /// Apply properties to Component using reflection
        /// </summary>
        private static void ApplyPropertiesToComponent(Component component, Dictionary<string, object> properties)
        {
            var type = component.GetType();
            
            foreach (var kvp in properties)
            {
                try
                {
                    var property = type.GetProperty(kvp.Key);
                    if (property != null && property.CanWrite)
                    {
                        var value = Convert.ChangeType(kvp.Value, property.PropertyType);
                        property.SetValue(component, value);
                    }
                    else
                    {
                        var field = type.GetField(kvp.Key);
                        if (field != null)
                        {
                            var value = Convert.ChangeType(kvp.Value, field.FieldType);
                            field.SetValue(component, value);
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to set property '{kvp.Key}' on {type.Name}: {e.Message}");
                }
            }
        }

        /// <summary>
        /// Apply property values from PropertyValue array (JSON serialization compatible)
        /// </summary>
        private static void ApplyPropertyValuesToComponent(Component component, PropertyValue[] propertyValues)
        {
            var type = component.GetType();
            
            foreach (var pv in propertyValues)
            {
                if (string.IsNullOrEmpty(pv.key)) continue;
                
                try
                {
                    object value = pv.GetValue();
                    
                    // Try property first
                    var property = type.GetProperty(pv.key);
                    if (property != null && property.CanWrite)
                    {
                        var convertedValue = Convert.ChangeType(value, property.PropertyType);
                        property.SetValue(component, convertedValue);
                        Debug.Log($"[AntigravityBridge] Set property '{pv.key}' = {convertedValue}");
                        continue;
                    }
                    
                    // Try field
                    var field = type.GetField(pv.key);
                    if (field != null)
                    {
                        var convertedValue = Convert.ChangeType(value, field.FieldType);
                        field.SetValue(component, convertedValue);
                        Debug.Log($"[AntigravityBridge] Set field '{pv.key}' = {convertedValue}");
                        continue;
                    }
                    
                    Debug.LogWarning($"[AntigravityBridge] Property/field '{pv.key}' not found on {type.Name}");
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to set property '{pv.key}' on {type.Name}: {e.Message}");
                }
            }
        }

        /// <summary>
        /// Setup mesh and material for primitive GameObjects
        /// Called after components are added to assign default meshes and materials
        /// </summary>
        private static void SetupPrimitiveMeshAndMaterial(GameObject obj, ColorData color = null)
        {
            Debug.Log($"[AntigravityBridge] SetupPrimitiveMeshAndMaterial called for: '{obj.name}'");
            
            var meshFilter = obj.GetComponent<MeshFilter>();
            var meshRenderer = obj.GetComponent<MeshRenderer>();
            
            Debug.Log($"[AntigravityBridge] MeshFilter={meshFilter != null}, MeshRenderer={meshRenderer != null}");
            
            if (meshFilter != null && meshFilter.sharedMesh == null)
            {
                // Detect primitive type from name and assign appropriate mesh
                string nameLower = obj.name.ToLower();
                
                //Try to load built-in Unity primitive mesh
                Mesh primitiveMesh = null;
                
                if (nameLower.Contains("cube") || nameLower.Contains("box"))
                {
                    primitiveMesh = GetPrimitiveMesh(PrimitiveType.Cube);
                }
                else if (nameLower.Contains("sphere") || nameLower.Contains("ball"))
                {
                    primitiveMesh = GetPrimitiveMesh(PrimitiveType.Sphere);
                }
                else if (nameLower.Contains("plane") || nameLower.Contains("ground") || nameLower.Contains("floor"))
                {
                    primitiveMesh = GetPrimitiveMesh(PrimitiveType.Plane);
                }
                else if (nameLower.Contains("cylinder") || nameLower.Contains("pillar"))
                {
                    primitiveMesh = GetPrimitiveMesh(PrimitiveType.Cylinder);
                }
                else if (nameLower.Contains("capsule"))
                {
                    primitiveMesh = GetPrimitiveMesh(PrimitiveType.Capsule);
                }
                else if (nameLower.Contains("quad"))
                {
                    primitiveMesh = GetPrimitiveMesh(PrimitiveType.Quad);
                }
                else
                {
                    // Default to cube if can't determine
                    primitiveMesh = GetPrimitiveMesh(PrimitiveType.Cube);
                }
                
                if (primitiveMesh != null)
                {
                    meshFilter.sharedMesh = primitiveMesh;
                    Debug.Log($"Assigned {primitiveMesh.name} mesh to {obj.name}");
                }
            }
            
            // Setup default material if MeshRenderer exists but has no material
            if (meshRenderer != null && (meshRenderer.sharedMaterial == null || meshRenderer.sharedMaterials.Length == 0))
            {
                // Use URP Lit material with optional color
                Material defaultMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                defaultMaterial.name = "Default Material";
                
                // Apply color if specified (URP uses _BaseColor instead of _Color)
                if (color != null)
                {
                    defaultMaterial.SetColor("_BaseColor", color.ToColor());
                    Debug.Log($"Applied color ({color.r}, {color.g}, {color.b}) to {obj.name}");
                }
                
                meshRenderer.sharedMaterial = defaultMaterial;
                Debug.Log($"Assigned default URP material to {obj.name}");
            }
            // If material already exists but color is specified, update color
            else if (meshRenderer != null && color != null)
            {
                // Create a new material instance to avoid modifying shared materials
                Material newMaterial = new Material(meshRenderer.sharedMaterial);
                newMaterial.SetColor("_BaseColor", color.ToColor());
                meshRenderer.sharedMaterial = newMaterial;
                Debug.Log($"Updated material color to ({color.r}, {color.g}, {color.b}) on {obj.name}");
            }
        }
        
        /// <summary>
        /// Get Unity built-in primitive mesh
        /// </summary>
        private static Mesh GetPrimitiveMesh(PrimitiveType primitiveType)
        {
            // Create temporary primitive to extract mesh
            GameObject temp = GameObject.CreatePrimitive(primitiveType);
            Mesh mesh = temp.GetComponent<MeshFilter>().sharedMesh;
            UnityEngine.Object.DestroyImmediate(temp);
            return mesh;
        }

        #endregion
    }
}
