using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using AntigravityBridge.Editor.Models;

namespace AntigravityBridge.Editor
{
    /// <summary>
    /// API for specific Transform manipulations
    /// Allows moving, rotating, and scaling existing objects
    /// </summary>
    public static class TransformAPI
    {
        /// <summary>
        /// Modify transform of objects
        /// POST /unity/transform/modify
        /// </summary>
        public static UnityResponse ModifyTransform(string json)
        {
            try
            {
                var command = JsonUtility.FromJson<TransformCommand>(json);

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

                        Undo.RecordObject(obj.transform, "Modify Transform");

                        // Position
                        if (command.position != null)
                        {
                            Vector3 current = command.useLocalSpace ? obj.transform.localPosition : obj.transform.position;
                            Vector3 target = command.position.ToVector3();

                            // Apply masks if available
                            if (command.positionMask != null)
                            {
                                if (command.positionMask.x < 0.5f) target.x = current.x;
                                if (command.positionMask.y < 0.5f) target.y = current.y;
                                if (command.positionMask.z < 0.5f) target.z = current.z;
                            }

                            if (command.useLocalSpace)
                                obj.transform.localPosition = target;
                            else
                                obj.transform.position = target;
                        }

                        // Rotation
                        if (command.rotation != null)
                        {
                            Vector3 current = command.useLocalSpace ? obj.transform.localEulerAngles : obj.transform.eulerAngles;
                            Vector3 target = command.rotation.ToVector3();

                            if (command.rotationMask != null)
                            {
                                if (command.rotationMask.x < 0.5f) target.x = current.x;
                                if (command.rotationMask.y < 0.5f) target.y = current.y;
                                if (command.rotationMask.z < 0.5f) target.z = current.z;
                            }

                            if (command.useLocalSpace)
                                obj.transform.localEulerAngles = target;
                            else
                                obj.transform.eulerAngles = target;
                        }

                        // Scale (always local)
                        if (command.scale != null)
                        {
                            Vector3 current = obj.transform.localScale;
                            Vector3 target = command.scale.ToVector3();

                            if (command.scaleMask != null)
                            {
                                if (command.scaleMask.x < 0.5f) target.x = current.x;
                                if (command.scaleMask.y < 0.5f) target.y = current.y;
                                if (command.scaleMask.z < 0.5f) target.z = current.z;
                            }

                            obj.transform.localScale = target;
                        }

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

                return UnityResponse.Success($"Modified transform of {modifiedObjects.Count} objects", data);
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Transform modification failed: {e.Message}");
            }
        }
    }

    [Serializable]
    public class TransformCommand
    {
        public string[] objects;
        public Vector3Data position;
        public Vector3Data rotation;
        public Vector3Data scale;
        public bool useLocalSpace;

        // Masks (1 = set, 0 = keep current)
        public Vector3Data positionMask;
        public Vector3Data rotationMask;
        public Vector3Data scaleMask;
    }
}
