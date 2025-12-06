using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using AntigravityBridge.Editor.Models;

namespace AntigravityBridge.Editor
{
    /// <summary>
    /// API for controlling Materials (URP compatible)
    /// Supports color, metallic, smoothness, emission
    /// </summary>
    public static class MaterialAPI
    {
        // URP shader property names
        private const string URP_BASE_COLOR = "_BaseColor";
        private const string URP_METALLIC = "_Metallic";
        private const string URP_SMOOTHNESS = "_Smoothness";
        private const string URP_EMISSION_COLOR = "_EmissionColor";

        /// <summary>
        /// Modify material properties on objects
        /// POST /unity/material/modify
        /// </summary>
        public static UnityResponse ModifyMaterial(string json)
        {
            try
            {
                var command = JsonUtility.FromJson<MaterialCommand>(json);
                
                if (command.objects == null || command.objects.Length == 0)
                {
                    return UnityResponse.Error("No objects specified");
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

                        var renderer = obj.GetComponent<Renderer>();
                        if (renderer == null)
                        {
                            errors.Add($"'{objectName}' has no Renderer component");
                            continue;
                        }

                        // Create new material instance to avoid modifying shared materials
                        Material mat = new Material(renderer.sharedMaterial);
                        Undo.RecordObject(renderer, "Modify Material");
                        
                        ApplyMaterialProperties(mat, command);
                        renderer.sharedMaterial = mat;
                        
                        EditorUtility.SetDirty(renderer);
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
                    return UnityResponse.Error("No materials modified", errors.ToArray());
                }

                return UnityResponse.Success($"Modified materials on {modifiedObjects.Count} objects", data);
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Material modification failed: {e.Message}");
            }
        }

        /// <summary>
        /// Assign existing material from Assets to objects
        /// POST /unity/material/assign
        /// </summary>
        public static UnityResponse AssignMaterial(string json)
        {
            try
            {
                var command = JsonUtility.FromJson<MaterialAssignCommand>(json);
                
                if (string.IsNullOrEmpty(command.materialPath))
                {
                    return UnityResponse.Error("Material path not specified");
                }

                // Load material from Assets
                var material = AssetDatabase.LoadAssetAtPath<Material>(command.materialPath);
                if (material == null)
                {
                    // Try with .mat extension
                    material = AssetDatabase.LoadAssetAtPath<Material>(command.materialPath + ".mat");
                }
                if (material == null)
                {
                    return UnityResponse.Error($"Material not found at '{command.materialPath}'");
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

                        var renderer = obj.GetComponent<Renderer>();
                        if (renderer == null)
                        {
                            errors.Add($"'{objectName}' has no Renderer component");
                            continue;
                        }

                        Undo.RecordObject(renderer, "Assign Material");
                        renderer.sharedMaterial = material;
                        EditorUtility.SetDirty(renderer);
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

                return UnityResponse.Success($"Assigned material to {modifiedObjects.Count} objects", data);
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Material assignment failed: {e.Message}");
            }
        }

        #region Private Helpers

        private static void ApplyMaterialProperties(Material mat, MaterialCommand command)
        {
            // Color (URP uses _BaseColor) - only apply if actually set
            if (command.color != null && command.color.IsSet())
            {
                if (mat.HasProperty(URP_BASE_COLOR))
                {
                    mat.SetColor(URP_BASE_COLOR, command.color.ToColor());
                }
                else if (mat.HasProperty("_Color"))
                {
                    mat.SetColor("_Color", command.color.ToColor());
                }
                else
                {
                    mat.color = command.color.ToColor();
                }
            }

            // Metallic
            if (command.metallic >= 0)
            {
                if (mat.HasProperty(URP_METALLIC))
                {
                    mat.SetFloat(URP_METALLIC, command.metallic);
                }
                else if (mat.HasProperty("_Metallic"))
                {
                    mat.SetFloat("_Metallic", command.metallic);
                }
            }

            // Smoothness
            if (command.smoothness >= 0)
            {
                if (mat.HasProperty(URP_SMOOTHNESS))
                {
                    mat.SetFloat(URP_SMOOTHNESS, command.smoothness);
                }
                else if (mat.HasProperty("_Glossiness"))
                {
                    mat.SetFloat("_Glossiness", command.smoothness);
                }
            }

            // Emission - only enable if actually specified in command
            if (command.emission != null && command.emission.IsSet())
            {
                mat.EnableKeyword("_EMISSION");
                if (mat.HasProperty(URP_EMISSION_COLOR))
                {
                    mat.SetColor(URP_EMISSION_COLOR, command.emission.ToColor());
                }
                else if (mat.HasProperty("_EmissionColor"))
                {
                    mat.SetColor("_EmissionColor", command.emission.ToColor());
                }
            }

            // Textures
            ApplyTexture(mat, command.mainTexture, new[] { "_BaseMap", "_MainTex" });
            ApplyTexture(mat, command.normalMap, new[] { "_BumpMap", "_NormalMap" }, true);
            ApplyTexture(mat, command.maskMap, new[] { "_MetallicGlossMap", "_MaskMap", "_Metallic" });
        }

        private static void ApplyTexture(Material mat, string texturePath, string[] propertyNames, bool isNormal = false)
        {
            if (string.IsNullOrEmpty(texturePath)) return;

            Texture tex = AssetDatabase.LoadAssetAtPath<Texture>(texturePath);
            if (tex == null)
            {
                // Try extensions
                string[] extensions = { ".png", ".jpg", ".jpeg", ".tga", ".psd" };
                foreach (var ext in extensions)
                {
                    tex = AssetDatabase.LoadAssetAtPath<Texture>(texturePath + ext);
                    if (tex != null) break;
                }
            }

            if (tex != null)
            {
                foreach (var prop in propertyNames)
                {
                    if (mat.HasProperty(prop))
                    {
                        mat.SetTexture(prop, tex);
                        if (isNormal) mat.EnableKeyword("_NORMALMAP");
                        return; // Set on first matching property
                    }
                }
            }
        }

        #endregion
    }

    #region Material Models

    /// <summary>
    /// Command for modifying material properties
    /// </summary>
    [Serializable]
    public class MaterialCommand
    {
        public string[] objects;
        public ColorData color;
        public float metallic = -1;     // -1 means don't change
        public float smoothness = -1;   // -1 means don't change
        public ColorData emission;

        // Texture paths
        public string mainTexture;
        public string normalMap;
        public string maskMap;
    }

    /// <summary>
    /// Command for assigning existing material
    /// </summary>
    [Serializable]
    public class MaterialAssignCommand
    {
        public string[] objects;
        public string materialPath;  // Path in Assets, e.g., "Assets/Materials/Red.mat"
    }

    #endregion
}
