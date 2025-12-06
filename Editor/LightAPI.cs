using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using AntigravityBridge.Editor.Models;

namespace AntigravityBridge.Editor
{
    /// <summary>
    /// API for controlling Light components
    /// Supports color, intensity, range, spotAngle, shadows
    /// </summary>
    public static class LightAPI
    {
        /// <summary>
        /// Modify light properties
        /// POST /unity/light/modify
        /// </summary>
        public static UnityResponse ModifyLight(string json)
        {
            try
            {
                var command = JsonUtility.FromJson<LightCommand>(json);
                
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

                        var light = obj.GetComponent<Light>();
                        if (light == null)
                        {
                            errors.Add($"'{objectName}' has no Light component");
                            continue;
                        }

                        Undo.RecordObject(light, "Modify Light");
                        ApplyLightProperties(light, command);
                        EditorUtility.SetDirty(light);
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
                    return UnityResponse.Error("No lights modified", errors.ToArray());
                }

                return UnityResponse.Success($"Modified {modifiedObjects.Count} lights", data);
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Light modification failed: {e.Message}");
            }
        }

        /// <summary>
        /// Find all lights in scene with optional filtering
        /// GET /unity/light/list
        /// </summary>
        public static UnityResponse GetAllLights(string typeFilter = null)
        {
            try
            {
                var lights = UnityEngine.Object.FindObjectsOfType<Light>();
                var lightInfos = new List<LightInfo>();

                foreach (var light in lights)
                {
                    // Apply type filter if specified
                    if (!string.IsNullOrEmpty(typeFilter))
                    {
                        var requestedType = ParseLightType(typeFilter);
                        if (light.type != requestedType) continue;
                    }

                    lightInfos.Add(new LightInfo
                    {
                        name = light.gameObject.name,
                        path = GetGameObjectPath(light.gameObject),
                        type = light.type.ToString(),
                        color = new ColorData(light.color.r, light.color.g, light.color.b, light.color.a),
                        intensity = light.intensity,
                        range = light.range,
                        spotAngle = light.spotAngle,
                        shadows = light.shadows.ToString(),
                        enabled = light.enabled
                    });
                }

                return UnityResponse.Success($"Found {lightInfos.Count} lights", new ResponseData
                {
                    affected_objects = lightInfos.Select(l => l.name).ToArray(),
                    count = lightInfos.Count
                });
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Failed to list lights: {e.Message}");
            }
        }

        #region Private Helpers

        private static void ApplyLightProperties(Light light, LightCommand command)
        {
            // Color
            if (command.color != null)
            {
                light.color = command.color.ToColor();
            }

            // Intensity
            if (command.intensity > 0)
            {
                light.intensity = command.intensity;
            }

            // Range (for Point/Spot lights)
            if (command.range > 0)
            {
                light.range = command.range;
            }

            // Spot Angle (for Spot lights)
            if (command.spotAngle > 0 && light.type == LightType.Spot)
            {
                light.spotAngle = command.spotAngle;
            }

            // Shadows
            if (!string.IsNullOrEmpty(command.shadows))
            {
                light.shadows = ParseShadowType(command.shadows);
            }

            // Light Type
            if (!string.IsNullOrEmpty(command.lightType))
            {
                light.type = ParseLightType(command.lightType);
            }

            // Enabled
            if (command.enabled.HasValue)
            {
                light.enabled = command.enabled.Value;
            }
        }

        private static LightShadows ParseShadowType(string shadowType)
        {
            switch (shadowType.ToLower())
            {
                case "none":
                case "off":
                    return LightShadows.None;
                case "hard":
                    return LightShadows.Hard;
                case "soft":
                    return LightShadows.Soft;
                default:
                    return LightShadows.None;
            }
        }

        private static LightType ParseLightType(string lightType)
        {
            switch (lightType.ToLower())
            {
                case "spot":
                    return LightType.Spot;
                case "directional":
                case "sun":
                    return LightType.Directional;
                case "point":
                    return LightType.Point;
                case "area":
                case "rect":
                    return LightType.Rectangle;
                default:
                    return LightType.Point;
            }
        }

        private static string GetGameObjectPath(GameObject obj)
        {
            string path = obj.name;
            Transform parent = obj.transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }

        #endregion
    }

    #region Light Models

    /// <summary>
    /// Command for modifying lights
    /// </summary>
    [Serializable]
    public class LightCommand
    {
        public string[] objects;
        public ColorData color;
        public float intensity;
        public float range;
        public float spotAngle;
        public string shadows;      // "none", "hard", "soft"
        public string lightType;    // "spot", "point", "directional"
        public bool? enabled;
    }

    /// <summary>
    /// Light information for queries
    /// </summary>
    [Serializable]
    public class LightInfo
    {
        public string name;
        public string path;
        public string type;
        public ColorData color;
        public float intensity;
        public float range;
        public float spotAngle;
        public string shadows;
        public bool enabled;
    }

    #endregion
}
