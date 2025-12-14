using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using AntigravityBridge.Editor.Models;

namespace AntigravityBridge.Editor
{
    /// <summary>
    /// API for managing Unity UI (uGUI) elements
    /// Supports creating Canvas, Buttons, Text, Images and manipulating RectTransforms
    /// </summary>
    public static class UIAPI
    {
        /// <summary>
        /// Create a UI element
        /// POST /unity/ui/create
        /// </summary>
        public static UnityResponse CreateUIElement(string json)
        {
            try
            {
                var command = JsonUtility.FromJson<UICreateCommand>(json);

                if (string.IsNullOrEmpty(command.type))
                {
                    return UnityResponse.Error("UI element type not specified");
                }

                GameObject newObj = null;
                GameObject parent = null;

                // Find parent if specified
                if (!string.IsNullOrEmpty(command.parent))
                {
                    parent = GameObject.Find(command.parent);
                    if (parent == null)
                    {
                        return UnityResponse.Error($"Parent '{command.parent}' not found");
                    }
                }

                // If no parent specified and not creating a Canvas, find or create default Canvas
                if (parent == null && command.type.ToLower() != "canvas" && command.type.ToLower() != "eventsystem")
                {
                    parent = GetOrCreateCanvas();
                }

                string name = string.IsNullOrEmpty(command.name) ? $"New {command.type}" : command.name;

                // Create specific UI element
                switch (command.type.ToLower())
                {
                    case "canvas":
                        newObj = CreateCanvas(name);
                        break;
                    case "eventsystem":
                        CreateEventSystem(); // Ensure one exists
                        return UnityResponse.Success("EventSystem checked/created", new ResponseData { count = 1 });
                    case "panel":
                        newObj = CreatePanel(name, parent);
                        break;
                    case "text":
                        newObj = CreateText(name, parent, command.text ?? "New Text");
                        break;
                    case "button":
                        newObj = CreateButton(name, parent, command.text ?? "Button");
                        break;
                    case "image":
                        newObj = CreateImage(name, parent);
                        break;
                    case "rawimage":
                        newObj = CreateRawImage(name, parent);
                        break;
                    default:
                        return UnityResponse.Error($"Unknown UI type: {command.type}. Supported: Canvas, Panel, Text, Button, Image, RawImage");
                }

                if (newObj != null)
                {
                    Undo.RegisterCreatedObjectUndo(newObj, $"Create UI {command.type}");

                    // Apply common properties if provided
                    if (command.position != null)
                    {
                        var rect = newObj.GetComponent<RectTransform>();
                        if (rect != null) rect.anchoredPosition = command.position.ToVector3();
                    }

                    if (command.size != null)
                    {
                        var rect = newObj.GetComponent<RectTransform>();
                        if (rect != null) rect.sizeDelta = command.size.ToVector3();
                    }

                    // Apply color if applicable
                    if (command.color != null)
                    {
                        var graphic = newObj.GetComponent<Graphic>();
                        if (graphic != null) graphic.color = command.color.ToColor();
                    }

                    EditorUtility.SetDirty(newObj);
                }

                return UnityResponse.Success($"Created UI element '{name}'", new ResponseData
                {
                    affected_objects = newObj != null ? new[] { newObj.name } : new string[0],
                    count = 1
                });
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"UI creation failed: {e.Message}");
            }
        }

        /// <summary>
        /// Modify RectTransform properties
        /// POST /unity/ui/rect
        /// </summary>
        public static UnityResponse ModifyRectTransform(string json)
        {
            try
            {
                var command = JsonUtility.FromJson<RectTransformCommand>(json);

                if (command.objects == null || command.objects.Length == 0)
                {
                    return UnityResponse.Error("No objects specified");
                }

                var modifiedObjects = new List<string>();
                var errors = new List<string>();

                foreach (var objectName in command.objects)
                {
                    var obj = GameObject.Find(objectName);
                    if (obj == null)
                    {
                        errors.Add($"Object '{objectName}' not found");
                        continue;
                    }

                    var rect = obj.GetComponent<RectTransform>();
                    if (rect == null)
                    {
                        errors.Add($"Object '{objectName}' is not a UI element (no RectTransform)");
                        continue;
                    }

                    Undo.RecordObject(rect, "Modify RectTransform");

                    if (command.anchorMin != null) { var v = command.anchorMin.ToVector3(); rect.anchorMin = new Vector2(v.x, v.y); }
                    if (command.anchorMax != null) { var v = command.anchorMax.ToVector3(); rect.anchorMax = new Vector2(v.x, v.y); }
                    if (command.pivot != null) { var v = command.pivot.ToVector3(); rect.pivot = new Vector2(v.x, v.y); }
                    if (command.anchoredPosition != null) rect.anchoredPosition = command.anchoredPosition.ToVector3();
                    if (command.sizeDelta != null) rect.sizeDelta = command.sizeDelta.ToVector3();

                    // Helper for presets
                    if (!string.IsNullOrEmpty(command.preset))
                    {
                        ApplyAnchorPreset(rect, command.preset);
                    }

                    EditorUtility.SetDirty(rect);
                    modifiedObjects.Add(objectName);
                }

                return UnityResponse.Success($"Modified RectTransform on {modifiedObjects.Count} objects", new ResponseData
                {
                    affected_objects = modifiedObjects.ToArray(),
                    count = modifiedObjects.Count,
                    errors = errors.ToArray()
                });
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"RectTransform modification failed: {e.Message}");
            }
        }

        #region Helpers

        private static GameObject GetOrCreateCanvas()
        {
            var canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            if (canvas != null) return canvas.gameObject;
            return CreateCanvas("Canvas");
        }

        private static GameObject CreateCanvas(string name)
        {
            GameObject go = new GameObject(name);
            go.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();

            // Ensure EventSystem exists
            CreateEventSystem();

            return go;
        }

        private static void CreateEventSystem()
        {
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject go = new GameObject("EventSystem");
                go.AddComponent<EventSystem>();
                go.AddComponent<StandaloneInputModule>();
            }
        }

        private static GameObject CreatePanel(string name, GameObject parent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);

            Image image = go.AddComponent<Image>();
            // Default panel look
            image.color = new Color(1, 1, 1, 0.39f);
            // Stretch to fill
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return go;
        }

        private static GameObject CreateImage(string name, GameObject parent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.AddComponent<Image>();
            return go;
        }

        private static GameObject CreateRawImage(string name, GameObject parent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.AddComponent<RawImage>();
            return go;
        }

        private static GameObject CreateText(string name, GameObject parent, string content)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);

            Text textComp = go.AddComponent<Text>();
            textComp.text = content;
            textComp.font = GetDefaultFont();
            textComp.color = Color.black;
            textComp.alignment = TextAnchor.MiddleCenter;

            return go;
        }

        private static GameObject CreateButton(string name, GameObject parent, string buttonText)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);

            Image image = go.AddComponent<Image>();
            image.color = Color.white;

            Button btn = go.AddComponent<Button>();
            btn.targetGraphic = image;

            // Add Text child
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(go.transform, false);

            Text textComp = textObj.AddComponent<Text>();
            textComp.text = buttonText;
            textComp.font = GetDefaultFont();
            textComp.color = Color.black;
            textComp.alignment = TextAnchor.MiddleCenter;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return go;
        }

        private static Font GetDefaultFont()
        {
            var font = Resources.FindObjectsOfTypeAll<Font>().FirstOrDefault(f => f.name == "Arial");
            return font != null ? font : Font.CreateDynamicFontFromOSFont("Arial", 14);
        }

        private static void ApplyAnchorPreset(RectTransform rect, string preset)
        {
            switch (preset.ToLower())
            {
                case "top-left":
                    rect.anchorMin = new Vector2(0, 1);
                    rect.anchorMax = new Vector2(0, 1);
                    rect.pivot = new Vector2(0, 1);
                    break;
                case "top-center":
                    rect.anchorMin = new Vector2(0.5f, 1);
                    rect.anchorMax = new Vector2(0.5f, 1);
                    rect.pivot = new Vector2(0.5f, 1);
                    break;
                case "top-right":
                    rect.anchorMin = new Vector2(1, 1);
                    rect.anchorMax = new Vector2(1, 1);
                    rect.pivot = new Vector2(1, 1);
                    break;
                case "middle-left":
                    rect.anchorMin = new Vector2(0, 0.5f);
                    rect.anchorMax = new Vector2(0, 0.5f);
                    rect.pivot = new Vector2(0, 0.5f);
                    break;
                case "middle-center":
                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    break;
                case "middle-right":
                    rect.anchorMin = new Vector2(1, 0.5f);
                    rect.anchorMax = new Vector2(1, 0.5f);
                    rect.pivot = new Vector2(1, 0.5f);
                    break;
                case "bottom-left":
                    rect.anchorMin = new Vector2(0, 0);
                    rect.anchorMax = new Vector2(0, 0);
                    rect.pivot = new Vector2(0, 0);
                    break;
                case "bottom-center":
                    rect.anchorMin = new Vector2(0.5f, 0);
                    rect.anchorMax = new Vector2(0.5f, 0);
                    rect.pivot = new Vector2(0.5f, 0);
                    break;
                case "bottom-right":
                    rect.anchorMin = new Vector2(1, 0);
                    rect.anchorMax = new Vector2(1, 0);
                    rect.pivot = new Vector2(1, 0);
                    break;
                case "stretch-all":
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.sizeDelta = Vector2.zero;
                    break;
                case "stretch-width":
                    rect.anchorMin = new Vector2(0, rect.anchorMin.y);
                    rect.anchorMax = new Vector2(1, rect.anchorMax.y);
                    break;
                case "stretch-height":
                    rect.anchorMin = new Vector2(rect.anchorMin.x, 0);
                    rect.anchorMax = new Vector2(rect.anchorMax.x, 1);
                    break;
            }
        }

        #endregion
    }

    #region UI Models

    [Serializable]
    public class UICreateCommand
    {
        public string type;           // "Canvas", "Button", "Text", "Image", "Panel"
        public string name;
        public string parent;         // Name of parent object
        public string text;           // For Text/Button
        public Vector3Data position;  // AnchoredPosition
        public Vector3Data size;      // SizeDelta
        public ColorData color;
    }

    [Serializable]
    public class RectTransformCommand
    {
        public string[] objects;
        public Vector3Data anchoredPosition;
        public Vector3Data sizeDelta;
        public Vector3Data anchorMin;
        public Vector3Data anchorMax;
        public Vector3Data pivot;
        public string preset;         // "top-left", "stretch-all", etc.
    }

    #endregion
}
