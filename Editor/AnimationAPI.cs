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
        
        /// <summary>
        /// Get all Animator parameters (READ)
        /// GET /unity/animator/parameters/{objectName}
        /// </summary>
        public static UnityResponse GetAnimatorParameters(string objectName)
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
                
                if (animator.runtimeAnimatorController == null)
                {
                    return UnityResponse.Error($"'{objectName}' Animator has no controller assigned");
                }
                
                var parameters = new List<AnimatorParameterInfo>();
                foreach (var param in animator.parameters)
                {
                    var info = new AnimatorParameterInfo
                    {
                        name = param.name,
                        type = param.type.ToString()
                    };
                    
                    // Get current value based on type
                    switch (param.type)
                    {
                        case AnimatorControllerParameterType.Float:
                            info.floatValue = animator.GetFloat(param.name);
                            break;
                        case AnimatorControllerParameterType.Int:
                            info.intValue = animator.GetInteger(param.name);
                            break;
                        case AnimatorControllerParameterType.Bool:
                            info.boolValue = animator.GetBool(param.name);
                            break;
                        case AnimatorControllerParameterType.Trigger:
                            // Triggers don't have a readable value
                            break;
                    }
                    
                    parameters.Add(info);
                }
                
                return UnityResponse.Success($"Found {parameters.Count} parameters on '{objectName}'", new ResponseData
                {
                    affected_objects = new[] { objectName },
                    count = parameters.Count,
                    animator_parameters = parameters.ToArray()
                });
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Get animator parameters failed: {e.Message}");
            }
        }
        
        /// <summary>
        /// Add parameter to AnimatorController (CREATE)
        /// POST /unity/animator/parameter/add
        /// </summary>
        public static UnityResponse AddAnimatorParameter(string json)
        {
            try
            {
                var command = JsonUtility.FromJson<AnimatorParameterCommand>(json);
                
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
                    
                    var controller = animator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
                    if (controller == null)
                    {
                        // Try to get the source controller if it's an AnimatorOverrideController
                        var overrideController = animator.runtimeAnimatorController as AnimatorOverrideController;
                        if (overrideController != null)
                        {
                            controller = overrideController.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
                        }
                        
                        if (controller == null)
                        {
                            errors.Add($"'{objectName}' AnimatorController is not editable (may need to be a project asset)");
                            continue;
                        }
                    }
                    
                    // Check if parameter already exists
                    bool exists = false;
                    foreach (var p in controller.parameters)
                    {
                        if (p.name == command.parameterName)
                        {
                            exists = true;
                            errors.Add($"'{objectName}' already has parameter '{command.parameterName}'");
                            break;
                        }
                    }
                    
                    if (exists) continue;
                    
                    // Parse parameter type
                    AnimatorControllerParameterType paramType = AnimatorControllerParameterType.Float;
                    switch (command.parameterType?.ToLower())
                    {
                        case "float":
                            paramType = AnimatorControllerParameterType.Float;
                            break;
                        case "int":
                        case "integer":
                            paramType = AnimatorControllerParameterType.Int;
                            break;
                        case "bool":
                        case "boolean":
                            paramType = AnimatorControllerParameterType.Bool;
                            break;
                        case "trigger":
                            paramType = AnimatorControllerParameterType.Trigger;
                            break;
                        default:
                            paramType = AnimatorControllerParameterType.Float;
                            break;
                    }
                    
                    Undo.RecordObject(controller, "Add Animator Parameter");
                    controller.AddParameter(command.parameterName, paramType);
                    
                    // Set default value if provided
                    if (command.floatValue != 0 || command.intValue != 0 || command.boolValue)
                    {
                        switch (paramType)
                        {
                            case AnimatorControllerParameterType.Float:
                                animator.SetFloat(command.parameterName, command.floatValue);
                                break;
                            case AnimatorControllerParameterType.Int:
                                animator.SetInteger(command.parameterName, command.intValue);
                                break;
                            case AnimatorControllerParameterType.Bool:
                                animator.SetBool(command.parameterName, command.boolValue);
                                break;
                        }
                    }
                    
                    EditorUtility.SetDirty(controller);
                    modifiedObjects.Add(objectName);
                }
                
                var data = new ResponseData
                {
                    affected_objects = modifiedObjects.ToArray(),
                    count = modifiedObjects.Count,
                    errors = errors.ToArray()
                };
                
                if (modifiedObjects.Count == 0)
                {
                    return UnityResponse.Error("No parameters added", errors.ToArray());
                }
                
                return UnityResponse.Success($"Added parameter '{command.parameterName}' to {modifiedObjects.Count} controllers", data);
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Add animator parameter failed: {e.Message}");
            }
        }
        
        /// <summary>
        /// Remove parameter from AnimatorController (DELETE)
        /// POST /unity/animator/parameter/remove
        /// </summary>
        public static UnityResponse RemoveAnimatorParameter(string json)
        {
            try
            {
                var command = JsonUtility.FromJson<AnimatorParameterCommand>(json);
                
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
                    
                    var controller = animator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
                    if (controller == null)
                    {
                        errors.Add($"'{objectName}' AnimatorController is not editable");
                        continue;
                    }
                    
                    // Find parameter index
                    int paramIndex = -1;
                    for (int i = 0; i < controller.parameters.Length; i++)
                    {
                        if (controller.parameters[i].name == command.parameterName)
                        {
                            paramIndex = i;
                            break;
                        }
                    }
                    
                    if (paramIndex < 0)
                    {
                        errors.Add($"'{objectName}' has no parameter '{command.parameterName}'");
                        continue;
                    }
                    
                    Undo.RecordObject(controller, "Remove Animator Parameter");
                    controller.RemoveParameter(paramIndex);
                    EditorUtility.SetDirty(controller);
                    modifiedObjects.Add(objectName);
                }
                
                var data = new ResponseData
                {
                    affected_objects = modifiedObjects.ToArray(),
                    count = modifiedObjects.Count,
                    errors = errors.ToArray()
                };
                
                return UnityResponse.Success($"Removed parameter '{command.parameterName}' from {modifiedObjects.Count} controllers", data);
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Remove animator parameter failed: {e.Message}");
            }
        }
        
        /// <summary>
        /// Crossfade to animation state with smooth transition
        /// POST /unity/animator/crossfade
        /// </summary>
        public static UnityResponse CrossfadeAnimation(string json)
        {
            try
            {
                var command = JsonUtility.FromJson<AnimatorCrossfadeCommand>(json);
                
                if (command.objects == null || command.objects.Length == 0)
                {
                    return UnityResponse.Error("No objects specified");
                }
                
                if (string.IsNullOrEmpty(command.stateName))
                {
                    return UnityResponse.Error("State name not specified");
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
                    
                    float duration = command.transitionDuration > 0 ? command.transitionDuration : 0.25f;
                    animator.CrossFade(command.stateName, duration, command.layer, command.normalizedTime);
                    modifiedObjects.Add(objectName);
                }
                
                var data = new ResponseData
                {
                    affected_objects = modifiedObjects.ToArray(),
                    count = modifiedObjects.Count,
                    errors = errors.ToArray()
                };
                
                return UnityResponse.Success($"Crossfade to '{command.stateName}' on {modifiedObjects.Count} objects", data);
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Crossfade animation failed: {e.Message}");
            }
        }
        
        /// <summary>
        /// Reset animator trigger
        /// POST /unity/animator/trigger/reset
        /// </summary>
        public static UnityResponse ResetAnimatorTrigger(string json)
        {
            try
            {
                var command = JsonUtility.FromJson<AnimatorTriggerCommand>(json);
                
                if (command.objects == null || command.objects.Length == 0)
                {
                    return UnityResponse.Error("No objects specified");
                }
                
                if (string.IsNullOrEmpty(command.triggerName))
                {
                    return UnityResponse.Error("Trigger name not specified");
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
                    
                    animator.ResetTrigger(command.triggerName);
                    modifiedObjects.Add(objectName);
                }
                
                var data = new ResponseData
                {
                    affected_objects = modifiedObjects.ToArray(),
                    count = modifiedObjects.Count,
                    errors = errors.ToArray()
                };
                
                return UnityResponse.Success($"Reset trigger '{command.triggerName}' on {modifiedObjects.Count} objects", data);
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Reset animator trigger failed: {e.Message}");
            }
        }
        
        /// <summary>
        /// Set animator speed
        /// POST /unity/animator/speed
        /// </summary>
        public static UnityResponse SetAnimatorSpeed(string json)
        {
            try
            {
                var command = JsonUtility.FromJson<AnimatorSpeedCommand>(json);
                
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
                    
                    var animator = obj.GetComponent<Animator>();
                    if (animator == null)
                    {
                        errors.Add($"'{objectName}' has no Animator component");
                        continue;
                    }
                    
                    Undo.RecordObject(animator, "Set Animator Speed");
                    animator.speed = command.speed;
                    modifiedObjects.Add(objectName);
                }
                
                var data = new ResponseData
                {
                    affected_objects = modifiedObjects.ToArray(),
                    count = modifiedObjects.Count,
                    errors = errors.ToArray()
                };
                
                return UnityResponse.Success($"Set animator speed to {command.speed} on {modifiedObjects.Count} objects", data);
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Set animator speed failed: {e.Message}");
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
    
    [Serializable]
    public class AnimatorParameterCommand
    {
        public string[] objects;
        public string parameterName;
        public string parameterType;  // "float", "int", "bool", "trigger"
        public float floatValue;      // Default value for float
        public int intValue;          // Default value for int
        public bool boolValue;        // Default value for bool
    }
    
    [Serializable]
    public class AnimatorCrossfadeCommand
    {
        public string[] objects;
        public string stateName;
        public float transitionDuration = 0.25f;
        public int layer = 0;
        public float normalizedTime = 0f;
    }
    
    [Serializable]
    public class AnimatorTriggerCommand
    {
        public string[] objects;
        public string triggerName;
    }
    
    [Serializable]
    public class AnimatorSpeedCommand
    {
        public string[] objects;
        public float speed = 1f;
    }
    
    [Serializable]
    public class AnimatorParameterInfo
    {
        public string name;
        public string type;      // "Float", "Int", "Bool", "Trigger"
        public float floatValue;
        public int intValue;
        public bool boolValue;
    }
    
    #endregion
}
