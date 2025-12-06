using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using AntigravityBridge.Editor.Models;

namespace AntigravityBridge.Editor
{
    /// <summary>
    /// API for controlling Unity Particle Systems
    /// Supports play, stop, emit, and property modification
    /// </summary>
    public static class ParticleAPI
    {
        /// <summary>
        /// Play particle system on objects
        /// POST /unity/particles/play
        /// </summary>
        public static UnityResponse PlayParticles(string json)
        {
            try
            {
                var command = JsonUtility.FromJson<ParticlePlayCommand>(json);
                
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
                    
                    var ps = obj.GetComponent<ParticleSystem>();
                    if (ps == null)
                    {
                        errors.Add($"'{objectName}' has no ParticleSystem component");
                        continue;
                    }
                    
                    if (command.withChildren)
                    {
                        ps.Play(true);
                    }
                    else
                    {
                        ps.Play(false);
                    }
                    
                    playedObjects.Add(objectName);
                }
                
                var data = new ResponseData
                {
                    affected_objects = playedObjects.ToArray(),
                    count = playedObjects.Count,
                    errors = errors.ToArray()
                };
                
                if (playedObjects.Count == 0)
                {
                    return UnityResponse.Error("No particle systems played", errors.ToArray());
                }
                
                return UnityResponse.Success($"Started particles on {playedObjects.Count} objects", data);
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Play particles failed: {e.Message}");
            }
        }
        
        /// <summary>
        /// Stop particle system on objects
        /// POST /unity/particles/stop
        /// </summary>
        public static UnityResponse StopParticles(string json)
        {
            try
            {
                var command = JsonUtility.FromJson<ParticleStopCommand>(json);
                
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
                    
                    var ps = obj.GetComponent<ParticleSystem>();
                    if (ps == null)
                    {
                        errors.Add($"'{objectName}' has no ParticleSystem component");
                        continue;
                    }
                    
                    var stopBehavior = command.clear ? 
                        ParticleSystemStopBehavior.StopEmittingAndClear : 
                        ParticleSystemStopBehavior.StopEmitting;
                    
                    ps.Stop(command.withChildren, stopBehavior);
                    
                    stoppedObjects.Add(objectName);
                }
                
                var data = new ResponseData
                {
                    affected_objects = stoppedObjects.ToArray(),
                    count = stoppedObjects.Count,
                    errors = errors.ToArray()
                };
                
                return UnityResponse.Success($"Stopped particles on {stoppedObjects.Count} objects", data);
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Stop particles failed: {e.Message}");
            }
        }
        
        /// <summary>
        /// Emit particles instantly
        /// POST /unity/particles/emit
        /// </summary>
        public static UnityResponse EmitParticles(string json)
        {
            try
            {
                var command = JsonUtility.FromJson<ParticleEmitCommand>(json);
                
                if (command.objects == null || command.objects.Length == 0)
                {
                    return UnityResponse.Error("No objects specified");
                }
                
                int count = command.count > 0 ? command.count : 10;
                
                var emittedObjects = new List<string>();
                var errors = new List<string>();
                
                foreach (var objectName in command.objects)
                {
                    var obj = GameObject.Find(objectName);
                    if (obj == null)
                    {
                        errors.Add($"Object '{objectName}' not found");
                        continue;
                    }
                    
                    var ps = obj.GetComponent<ParticleSystem>();
                    if (ps == null)
                    {
                        errors.Add($"'{objectName}' has no ParticleSystem component");
                        continue;
                    }
                    
                    ps.Emit(count);
                    emittedObjects.Add(objectName);
                }
                
                var data = new ResponseData
                {
                    affected_objects = emittedObjects.ToArray(),
                    count = emittedObjects.Count,
                    errors = errors.ToArray()
                };
                
                return UnityResponse.Success($"Emitted {count} particles on {emittedObjects.Count} objects", data);
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Emit particles failed: {e.Message}");
            }
        }
        
        /// <summary>
        /// Modify particle system properties
        /// POST /unity/particles/modify
        /// </summary>
        public static UnityResponse ModifyParticles(string json)
        {
            try
            {
                var command = JsonUtility.FromJson<ParticleModifyCommand>(json);
                
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
                    
                    var ps = obj.GetComponent<ParticleSystem>();
                    if (ps == null)
                    {
                        errors.Add($"'{objectName}' has no ParticleSystem component");
                        continue;
                    }
                    
                    Undo.RecordObject(ps, "Modify Particle System");
                    
                    // Modify main module
                    var main = ps.main;
                    
                    if (command.duration > 0)
                    {
                        main.duration = command.duration;
                    }
                    
                    if (command.startLifetime > 0)
                    {
                        main.startLifetime = command.startLifetime;
                    }
                    
                    if (command.startSpeed >= 0)
                    {
                        main.startSpeed = command.startSpeed;
                    }
                    
                    if (command.startSize > 0)
                    {
                        main.startSize = command.startSize;
                    }
                    
                    if (command.startColor != null && command.startColor.IsSet())
                    {
                        main.startColor = command.startColor.ToColor();
                    }
                    
                    if (command.maxParticles > 0)
                    {
                        main.maxParticles = command.maxParticles;
                    }
                    
                    main.loop = command.loop;
                    
                    // Modify emission module
                    if (command.emissionRate >= 0)
                    {
                        var emission = ps.emission;
                        emission.rateOverTime = command.emissionRate;
                    }
                    
                    EditorUtility.SetDirty(ps);
                    modifiedObjects.Add(objectName);
                }
                
                var data = new ResponseData
                {
                    affected_objects = modifiedObjects.ToArray(),
                    count = modifiedObjects.Count,
                    errors = errors.ToArray()
                };
                
                return UnityResponse.Success($"Modified particles on {modifiedObjects.Count} objects", data);
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Modify particles failed: {e.Message}");
            }
        }
        
        /// <summary>
        /// Get particle system info
        /// GET /unity/particles/info/{objectName}
        /// </summary>
        public static UnityResponse GetParticleInfo(string objectName)
        {
            try
            {
                var obj = GameObject.Find(objectName);
                if (obj == null)
                {
                    return UnityResponse.Error($"Object '{objectName}' not found");
                }
                
                var ps = obj.GetComponent<ParticleSystem>();
                if (ps == null)
                {
                    return UnityResponse.Error($"'{objectName}' has no ParticleSystem component");
                }
                
                var main = ps.main;
                
                var data = new ResponseData
                {
                    affected_objects = new[] { objectName },
                    count = ps.particleCount
                };
                
                return UnityResponse.Success(
                    $"ParticleSystem '{objectName}': " +
                    $"playing={ps.isPlaying}, " +
                    $"particles={ps.particleCount}, " +
                    $"duration={main.duration}, " +
                    $"loop={main.loop}", 
                    data);
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Get particle info failed: {e.Message}");
            }
        }
    }
    
    #region Particle Command Models
    
    [Serializable]
    public class ParticlePlayCommand
    {
        public string[] objects;
        public bool withChildren = true;
    }
    
    [Serializable]
    public class ParticleStopCommand
    {
        public string[] objects;
        public bool withChildren = true;
        public bool clear = false;  // Clear existing particles
    }
    
    [Serializable]
    public class ParticleEmitCommand
    {
        public string[] objects;
        public int count = 10;
    }
    
    [Serializable]
    public class ParticleModifyCommand
    {
        public string[] objects;
        public float duration = -1f;
        public float startLifetime = -1f;
        public float startSpeed = -1f;
        public float startSize = -1f;
        public ColorData startColor;
        public int maxParticles = -1;
        public bool loop = true;
        public float emissionRate = -1f;
    }
    
    #endregion
}
