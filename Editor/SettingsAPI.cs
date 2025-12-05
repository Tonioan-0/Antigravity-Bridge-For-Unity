using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using AntigravityBridge.Editor.Models;

namespace AntigravityBridge.Editor
{
    /// <summary>
    /// Handles Unity project settings access and modification
    /// </summary>
    public static class SettingsAPI
    {
        /// <summary>
        /// Get settings for a specific category
        /// GET /unity/settings/{category}
        /// </summary>
        public static UnityResponse GetSettings(string category)
        {
            try
            {
                var settings = new Dictionary<string, object>();

                switch (category.ToLower())
                {
                    case "player":
                        settings = GetPlayerSettings();
                        break;

                    case "quality":
                        settings = GetQualitySettings();
                        break;

                    case "physics":
                        settings = GetPhysicsSettings();
                        break;

                    case "audio":
                        settings = GetAudioSettings();
                        break;

                    case "graphics":
                        settings = GetGraphicsSettings();
                        break;

                    default:
                        return UnityResponse.Error($"Unknown settings category: {category}");
                }

                var data = new ResponseData
                {
                    settings = settings
                };

                return UnityResponse.Success($"Retrieved {category} settings", data);
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Failed to get settings: {e.Message}");
            }
        }

        /// <summary>
        /// Modify settings for a specific category
        /// POST /unity/settings/{category}
        /// </summary>
        public static UnityResponse ModifySettings(string category, string json)
        {
            try
            {
                // Parse settings dictionary from JSON
                var wrapper = JsonUtility.FromJson<SettingsWrapper>(json);
                
                if (wrapper?.settings == null)
                {
                    return UnityResponse.Error("No settings provided");
                }

                switch (category.ToLower())
                {
                    case "player":
                        ModifyPlayerSettings(wrapper.settings);
                        break;

                    case "quality":
                        ModifyQualitySettings(wrapper.settings);
                        break;

                    case "physics":
                        ModifyPhysicsSettings(wrapper.settings);
                        break;

                    default:
                        return UnityResponse.Error($"Modification not supported for category: {category}");
                }

                return UnityResponse.Success($"Modified {category} settings");
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Failed to modify settings: {e.Message}");
            }
        }

        #region Get Settings

        private static Dictionary<string, object> GetPlayerSettings()
        {
            return new Dictionary<string, object>
            {
                { "companyName", PlayerSettings.companyName },
                { "productName", PlayerSettings.productName },
                { "version", PlayerSettings.bundleVersion },
                { "defaultScreenWidth", PlayerSettings.defaultScreenWidth },
                { "defaultScreenHeight", PlayerSettings.defaultScreenHeight },
                { "runInBackground", PlayerSettings.runInBackground },
                { "fullscreenMode", PlayerSettings.fullScreenMode.ToString() },
                { "colorSpace", PlayerSettings.colorSpace.ToString() }
            };
        }

        private static Dictionary<string, object> GetQualitySettings()
        {
            return new Dictionary<string, object>
            {
                { "currentLevel", QualitySettings.GetQualityLevel() },
                { "qualityLevelName", QualitySettings.names[QualitySettings.GetQualityLevel()] },
                { "pixelLightCount", QualitySettings.pixelLightCount },
                { "shadows", QualitySettings.shadows.ToString() },
                { "shadowResolution", QualitySettings.shadowResolution.ToString() },
                { "shadowDistance", QualitySettings.shadowDistance },
                { "antiAliasing", QualitySettings.antiAliasing },
                { "vSyncCount", QualitySettings.vSyncCount }
            };
        }

        private static Dictionary<string, object> GetPhysicsSettings()
        {
            return new Dictionary<string, object>
            {
                { "gravity_x", Physics.gravity.x },
                { "gravity_y", Physics.gravity.y },
                { "gravity_z", Physics.gravity.z },
                { "defaultSolverIterations", Physics.defaultSolverIterations },
                { "defaultSolverVelocityIterations", Physics.defaultSolverVelocityIterations },
                { "bounceThreshold", Physics.bounceThreshold },
                { "sleepThreshold", Physics.sleepThreshold },
                { "queriesHitTriggers", Physics.queriesHitTriggers }
            };
        }

        private static Dictionary<string, object> GetAudioSettings()
        {
            var config = AudioSettings.GetConfiguration();
            
            return new Dictionary<string, object>
            {
                { "sampleRate", config.sampleRate },
                { "speakerMode", config.speakerMode.ToString() },
                { "dspBufferSize", config.dspBufferSize },
                { "numRealVoices", config.numRealVoices },
                { "numVirtualVoices", config.numVirtualVoices }
            };
        }

        private static Dictionary<string, object> GetGraphicsSettings()
        {
            var settings = new Dictionary<string, object>();
            
            // Try to get render pipeline asset if it exists
            try
            {
                var pipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
                settings.Add("renderPipeline", pipeline != null ? pipeline.name : "Built-in");
            }
            catch
            {
                settings.Add("renderPipeline", "Built-in");
            }
            
            // Add basic graphics info
            settings.Add("activeColorSpace", QualitySettings.activeColorSpace.ToString());
            
            return settings;
        }

        #endregion

        #region Modify Settings

        private static void ModifyPlayerSettings(Dictionary<string, object> settings)
        {
            foreach (var kvp in settings)
            {
                try
                {
                    switch (kvp.Key.ToLower())
                    {
                        case "companyname":
                            PlayerSettings.companyName = kvp.Value.ToString();
                            break;
                        case "productname":
                            PlayerSettings.productName = kvp.Value.ToString();
                            break;
                        case "version":
                            PlayerSettings.bundleVersion = kvp.Value.ToString();
                            break;
                        case "defaultscreenwidth":
                            PlayerSettings.defaultScreenWidth = Convert.ToInt32(kvp.Value);
                            break;
                        case "defaultscreenheight":
                            PlayerSettings.defaultScreenHeight = Convert.ToInt32(kvp.Value);
                            break;
                        case "runinbackground":
                            PlayerSettings.runInBackground = Convert.ToBoolean(kvp.Value);
                            break;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to set PlayerSetting '{kvp.Key}': {e.Message}");
                }
            }
        }

        private static void ModifyQualitySettings(Dictionary<string, object> settings)
        {
            foreach (var kvp in settings)
            {
                try
                {
                    switch (kvp.Key.ToLower())
                    {
                        case "qualitylevel":
                            QualitySettings.SetQualityLevel(Convert.ToInt32(kvp.Value));
                            break;
                        case "pixellightcount":
                            QualitySettings.pixelLightCount = Convert.ToInt32(kvp.Value);
                            break;
                        case "shadowdistance":
                            QualitySettings.shadowDistance = Convert.ToSingle(kvp.Value);
                            break;
                        case "antialiasing":
                            QualitySettings.antiAliasing = Convert.ToInt32(kvp.Value);
                            break;
                        case "vsynccount":
                            QualitySettings.vSyncCount = Convert.ToInt32(kvp.Value);
                            break;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to set QualitySetting '{kvp.Key}': {e.Message}");
                }
            }
        }

        private static void ModifyPhysicsSettings(Dictionary<string, object> settings)
        {
            foreach (var kvp in settings)
            {
                try
                {
                    switch (kvp.Key.ToLower())
                    {
                        case "gravity_y":
                            var gravity = Physics.gravity;
                            gravity.y = Convert.ToSingle(kvp.Value);
                            Physics.gravity = gravity;
                            break;
                        case "defaultsolveriterations":
                            Physics.defaultSolverIterations = Convert.ToInt32(kvp.Value);
                            break;
                        case "bouncethreshold":
                            Physics.bounceThreshold = Convert.ToSingle(kvp.Value);
                            break;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to set PhysicsSetting '{kvp.Key}': {e.Message}");
                }
            }
        }

        #endregion

        /// <summary>
        /// Wrapper class for JSON deserialization
        /// </summary>
        [Serializable]
        private class SettingsWrapper
        {
            public Dictionary<string, object> settings;
        }
    }
}
