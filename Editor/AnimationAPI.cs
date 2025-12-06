using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using AntigravityBridge.Editor.Models;

namespace AntigravityBridge.Editor
{
    /// <summary>
    /// API for controlling Unity Animation and Animator components
    /// Supports play/stop clips and Animator parameter manipulation
    /// </summary>
    public static class AnimationAPI
    {
        /// <summary>
        /// Play animation clip on objects
        /// POST /unity/animation/play
        /// </summary>
        public static UnityResponse PlayAnimation(string json)
        {
            try
            {
                var command = JsonUtility.FromJson<AnimationPlayCommand>(json);
                
                if (command.objects == null || command.objects.Length == 0)
                {
                    return UnityResponse.Error("No objects specified");
                }
                
                var playedObjects = new List<string>();
                var errors = new List<string>();
                
                foreach (var objectName in command.objects)
                {
                    var obj = GameObject.Find(objectName);
                    if (obj == null)
                    {
                        errors.Add($"Object '{objectName}' not found");
                        continue;
                    }
                    
                    // Try Animation component first (legacy)
                    var animation = obj.GetComponent<Animation>();
                    if (animation != null)
                    {
                        if (!string.IsNullOrEmpty(command.clipName))
                        {
                            animation.Play(command.clipName);
                        }
                        else
                        {
                            animation.Play();
                        }
                        playedObjects.Add(objectName);
                        continue;
                    }
                    
                    // Try Animator component
                    var animator = obj.GetComponent<Animator>();
                    if (animator != null)
                    {
                        if (!string.IsNullOrEmpty(command.stateName))
                        {
                            animator.Play(command.stateName, command.layer);
                        }
                        else if (!string.IsNullOrEmpty(command.triggerName))
                        {
                            animator.SetTrigger(command.triggerName);
                        }
                        playedObjects.Add(objectName);
                        continue;
                    }
                    
                    errors.Add($"'{objectName}' has no Animation or Animator component");
                }
                
                var data = new ResponseData
                {
                    affected_objects = playedObjects.ToArray(),
                    count = playedObjects.Count,
                    errors = errors.ToArray()
                };
                
                if (playedObjects.Count == 0)
                {
                    return UnityResponse.Error("No animations played", errors.ToArray());
                }
                
                return UnityResponse.Success($"Started animation on {playedObjects.Count} objects", data);
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Play animation failed: {e.Message}");
            }
        }
        
        /// <summary>
        /// Stop animation on objects
        /// POST /unity/animation/stop
        /// </summary>
        public static UnityResponse StopAnimation(string json)
        {
            try
            {
                var command = JsonUtility.FromJson<AnimationStopCommand>(json);
                
                if (command.objects == null || command.objects.Length == 0)
                {
                    return UnityResponse.Error("No objects specified");
                }
                
                var stoppedObjects = new List<string>();
                var errors = new List<string>();
                
                foreach (var objectName in command.objects)
                {
                    var obj = GameObject.Find(objectName);
                    if (obj == null)
                    {
                        errors.Add($"Object '{objectName}' not found");
                        continue;
                    }
                    
                    var animation = obj.GetComponent<Animation>();
                    if (animation != null)
                    {
                        animation.Stop();
                        stoppedObjects.Add(objectName);
                        continue;
                    }
                    
                    var animator = obj.GetComponent<Animator>();
                    if (animator != null)
                    {
                        animator.StopPlayback();
                        // Or set speed to 0
                        if (command.pause)
                        {
                            animator.speed = 0;
                        }
                        stoppedObjects.Add(objectName);
                        continue;
                    }
                    
                    errors.Add($"'{objectName}' has no Animation or Animator component");
                }
                
                var data = new ResponseData
                {
                    affected_objects = stoppedObjects.ToArray(),
                    count = stoppedObjects.Count,
                    errors = errors.ToArray()
                };
                
                return UnityResponse.Success($"Stopped animation on {stoppedObjects.Count} objects", data);
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Stop animation failed: {e.Message}");
            }
        }
        
        /// <summary>
        /// Set Animator parameter
        /// POST /unity/animator/set
        /// </summary>
        public static UnityResponse SetAnimatorParameter(string json)
        {
            try
            {
                var command = JsonUtility.FromJson<AnimatorSetCommand>(json);
                
                if (command.objects == null || command.objects.Length == 0)
                {
                    return UnityResponse.Error("No objects specified");
                }
                
                if (string.IsNullOrEmpty(command.parameterName))
                {
                    return UnityResponse.Error("Parameter name not specified");
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
                    
                    var animator = obj.GetComponent<Animator>();
                    if (animator == null)
                    {
                        errors.Add($"'{objectName}' has no Animator component");
                        continue;
                    }
                    
                    Undo.RecordObject(animator, "Set Animator Parameter");
                    
                    // Set parameter based on type
                    switch (command.parameterType?.ToLower())
                    {
                        case "float":
                            animator.SetFloat(command.parameterName, command.floatValue);
                            break;
                        case "int":
                        case "integer":
                            animator.SetInteger(command.parameterName, command.intValue);
                            break;
                        case "bool":
                        case "boolean":
                            animator.SetBool(command.parameterName, command.boolValue);
                            break;
                        case "trigger":
                            animator.SetTrigger(command.parameterName);
                            break;
                        default:
                            // Auto-detect: try float first, then bool
                            if (command.floatValue != 0)
                            {
                                animator.SetFloat(command.parameterName, command.floatValue);
                            }
                            else if (command.intValue != 0)
                            {
                                animator.SetInteger(command.parameterName, command.intValue);
                            }
                            else
                            {
                                animator.SetBool(command.parameterName, command.boolValue);
                            }
                            break;
                    }
                    
                    modifiedObjects.Add(objectName);
                }
                
                var data = new ResponseData
                {
                    affected_objects = modifiedObjects.ToArray(),
                    count = modifiedObjects.Count,
                    errors = errors.ToArray()
                };
                
                return UnityResponse.Success($"Set animator parameter on {modifiedObjects.Count} objects", data);
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Set animator parameter failed: {e.Message}");
            }
        }
        
        /// <summary>
        /// Get Animator state information
        /// GET /unity/animator/info/{objectName}
        /// </summary>
        public static UnityResponse GetAnimatorInfo(string objectName)
        {
            try
            {
                var obj = GameObject.Find(objectName);
                if (obj == null)
                {
                    return UnityResponse.Error($"Object '{objectName}' not found");
                }
                
                var animator = obj.GetComponent<Animator>();
                if (animator == null)
                {
                    return UnityResponse.Error($"'{objectName}' has no Animator component");
                }
                
                // Get current state info
                var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                var clipInfo = animator.GetCurrentAnimatorClipInfo(0);
                
                string currentClip = clipInfo.Length > 0 ? clipInfo[0].clip.name : "none";
                
                var data = new ResponseData
                {
                    affected_objects = new[] { objectName },
                    count = 1
                };
                
                return UnityResponse.Success($"Animator on '{objectName}': clip='{currentClip}', speed={animator.speed}", data);
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Get animator info failed: {e.Message}");
            }
        }
    }
    
    #region Animation Command Models
    
    [Serializable]
    public class AnimationPlayCommand
    {
        public string[] objects;
        public string clipName;     // For Animation component (legacy)
        public string stateName;    // For Animator state name
        public string triggerName;  // For Animator trigger
        public int layer = 0;
        public float normalizedTime = 0f;
    }
    
    [Serializable]
    public class AnimationStopCommand
    {
        public string[] objects;
        public bool pause = false;  // If true, pause instead of stop
    }
    
    [Serializable]
    public class AnimatorSetCommand
    {
        public string[] objects;
        public string parameterName;
        public string parameterType;  // "float", "int", "bool", "trigger"
        public float floatValue;
        public int intValue;
        public bool boolValue;
    }
    
    #endregion
}
