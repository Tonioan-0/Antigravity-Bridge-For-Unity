using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using AntigravityBridge.Editor.Models;

namespace AntigravityBridge.Editor
{
    /// <summary>
    /// API for capturing screenshots from Unity Editor
    /// Supports Game View and Scene View capture
    /// </summary>
    public static class ScreenshotAPI
    {
        /// <summary>
        /// Capture screenshot from Game View
        /// POST /unity/screenshot/capture
        /// </summary>
        public static UnityResponse CaptureScreenshot(string json)
        {
            try
            {
                var command = JsonUtility.FromJson<ScreenshotCommand>(json);
                
                // Default filename with timestamp
                string filename = command.filename;
                if (string.IsNullOrEmpty(filename))
                {
                    filename = $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                }
                
                // Ensure .png extension
                if (!filename.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                {
                    filename += ".png";
                }
                
                // Default path to project root
                string fullPath = command.path;
                if (string.IsNullOrEmpty(fullPath))
                {
                    fullPath = Path.Combine(Application.dataPath, "..", "Screenshots");
                }
                
                // Create directory if needed
                if (!Directory.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                }
                
                string outputPath = Path.Combine(fullPath, filename);
                
                // Get resolution
                int width = command.width > 0 ? command.width : Screen.width;
                int height = command.height > 0 ? command.height : Screen.height;
                int superSize = command.superSize > 0 ? command.superSize : 1;
                
                // Capture screenshot
                ScreenCapture.CaptureScreenshot(outputPath, superSize);
                
                var data = new ResponseData
                {
                    affected_objects = new[] { outputPath },
                    count = 1
                };
                
                return UnityResponse.Success($"Screenshot saved to: {outputPath}", data);
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Screenshot capture failed: {e.Message}");
            }
        }
        
        /// <summary>
        /// Capture screenshot from a specific camera
        /// POST /unity/screenshot/camera
        /// </summary>
        public static UnityResponse CaptureFromCamera(string json)
        {
            try
            {
                var command = JsonUtility.FromJson<CameraScreenshotCommand>(json);
                
                // Find camera
                Camera camera = null;
                if (!string.IsNullOrEmpty(command.cameraName))
                {
                    var cameraObj = GameObject.Find(command.cameraName);
                    if (cameraObj != null)
                    {
                        camera = cameraObj.GetComponent<Camera>();
                    }
                }
                
                if (camera == null)
                {
                    camera = Camera.main;
                }
                
                if (camera == null)
                {
                    return UnityResponse.Error("No camera found");
                }
                
                // Set resolution
                int width = command.width > 0 ? command.width : 1920;
                int height = command.height > 0 ? command.height : 1080;
                
                // Create render texture
                RenderTexture rt = new RenderTexture(width, height, 24);
                camera.targetTexture = rt;
                
                // Render
                Texture2D screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
                camera.Render();
                RenderTexture.active = rt;
                screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                screenshot.Apply();
                
                // Reset camera
                camera.targetTexture = null;
                RenderTexture.active = null;
                UnityEngine.Object.DestroyImmediate(rt);
                
                // Save to file
                string filename = command.filename;
                if (string.IsNullOrEmpty(filename))
                {
                    filename = $"camera_screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                }
                
                if (!filename.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                {
                    filename += ".png";
                }
                
                string fullPath = command.path;
                if (string.IsNullOrEmpty(fullPath))
                {
                    fullPath = Path.Combine(Application.dataPath, "..", "Screenshots");
                }
                
                if (!Directory.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                }
                
                string outputPath = Path.Combine(fullPath, filename);
                byte[] bytes = screenshot.EncodeToPNG();
                File.WriteAllBytes(outputPath, bytes);
                
                UnityEngine.Object.DestroyImmediate(screenshot);
                
                var data = new ResponseData
                {
                    affected_objects = new[] { outputPath },
                    count = 1
                };
                
                return UnityResponse.Success($"Camera screenshot saved to: {outputPath}", data);
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Camera screenshot failed: {e.Message}");
            }
        }
        
        /// <summary>
        /// Capture Scene View screenshot
        /// POST /unity/screenshot/scene
        /// </summary>
        public static UnityResponse CaptureSceneView(string json)
        {
            try
            {
                var command = JsonUtility.FromJson<ScreenshotCommand>(json);
                
                // Get Scene View
                SceneView sceneView = SceneView.lastActiveSceneView;
                if (sceneView == null)
                {
                    return UnityResponse.Error("No active Scene View found");
                }
                
                // Force repaint
                sceneView.Repaint();
                
                string filename = command.filename;
                if (string.IsNullOrEmpty(filename))
                {
                    filename = $"sceneview_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                }
                
                if (!filename.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                {
                    filename += ".png";
                }
                
                string fullPath = command.path;
                if (string.IsNullOrEmpty(fullPath))
                {
                    fullPath = Path.Combine(Application.dataPath, "..", "Screenshots");
                }
                
                if (!Directory.Exists(fullPath))
                {
                    Directory.CreateDirectory(fullPath);
                }
                
                string outputPath = Path.Combine(fullPath, filename);
                
                // Capture using Scene View camera
                Camera sceneCam = sceneView.camera;
                int width = command.width > 0 ? command.width : (int)sceneView.position.width;
                int height = command.height > 0 ? command.height : (int)sceneView.position.height;
                
                RenderTexture rt = new RenderTexture(width, height, 24);
                sceneCam.targetTexture = rt;
                
                Texture2D screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
                sceneCam.Render();
                RenderTexture.active = rt;
                screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                screenshot.Apply();
                
                sceneCam.targetTexture = null;
                RenderTexture.active = null;
                UnityEngine.Object.DestroyImmediate(rt);
                
                byte[] bytes = screenshot.EncodeToPNG();
                File.WriteAllBytes(outputPath, bytes);
                
                UnityEngine.Object.DestroyImmediate(screenshot);
                
                var data = new ResponseData
                {
                    affected_objects = new[] { outputPath },
                    count = 1
                };
                
                return UnityResponse.Success($"Scene View screenshot saved to: {outputPath}", data);
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Scene View screenshot failed: {e.Message}");
            }
        }
    }
    
    #region Screenshot Command Models
    
    [Serializable]
    public class ScreenshotCommand
    {
        public string filename;     // Output filename (auto-generated if empty)
        public string path;         // Output directory (default: project/Screenshots)
        public int width = -1;      // -1 = current resolution
        public int height = -1;
        public int superSize = 1;   // Resolution multiplier (1-4)
    }
    
    [Serializable]
    public class CameraScreenshotCommand
    {
        public string cameraName;   // Camera to use (default: Main Camera)
        public string filename;
        public string path;
        public int width = 1920;
        public int height = 1080;
    }
    
    #endregion
}
