using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using AntigravityBridge.Editor.Models;

namespace AntigravityBridge.Editor
{
    /// <summary>
    /// API for controlling Unity Physics simulation
    /// Supports simulate, step, raycast, and gravity control
    /// </summary>
    public static class PhysicsAPI
    {
        /// <summary>
        /// Simulate physics for a specified duration
        /// POST /unity/physics/simulate
        /// </summary>
        public static UnityResponse Simulate(string json)
        {
            try
            {
                var command = JsonUtility.FromJson<PhysicsSimulateCommand>(json);
                float duration = command.seconds > 0 ? command.seconds : 1f;
                float stepSize = command.stepSize > 0 ? command.stepSize : Time.fixedDeltaTime;
                
                int steps = Mathf.CeilToInt(duration / stepSize);
                
                // Simulate physics
                for (int i = 0; i < steps; i++)
                {
                    Physics.Simulate(stepSize);
                }
                
                var data = new ResponseData
                {
                    count = steps
                };
                
                return UnityResponse.Success($"Simulated {duration}s of physics ({steps} steps)", data);
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Physics simulation failed: {e.Message}");
            }
        }
        
        /// <summary>
        /// Step physics by one fixed delta time
        /// POST /unity/physics/step
        /// </summary>
        public static UnityResponse Step(string json)
        {
            try
            {
                var command = JsonUtility.FromJson<PhysicsStepCommand>(json);
                float deltaTime = command.deltaTime > 0 ? command.deltaTime : Time.fixedDeltaTime;
                int steps = command.steps > 0 ? command.steps : 1;
                
                for (int i = 0; i < steps; i++)
                {
                    Physics.Simulate(deltaTime);
                }
                
                return UnityResponse.Success($"Stepped physics {steps} time(s) at {deltaTime}s each");
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Physics step failed: {e.Message}");
            }
        }
        
        /// <summary>
        /// Cast a ray and return hit information
        /// POST /unity/physics/raycast
        /// </summary>
        public static UnityResponse Raycast(string json)
        {
            try
            {
                var command = JsonUtility.FromJson<RaycastCommand>(json);
                
                Vector3 origin = command.origin?.ToVector3() ?? Vector3.zero;
                Vector3 direction = command.direction?.ToVector3() ?? Vector3.down;
                float maxDistance = command.maxDistance > 0 ? command.maxDistance : Mathf.Infinity;
                
                RaycastHit hit;
                bool didHit = Physics.Raycast(origin, direction.normalized, out hit, maxDistance);
                
                var data = new ResponseData();
                
                if (didHit)
                {
                    // Create hit info
                    var hitInfo = new Dictionary<string, object>
                    {
                        { "hit", true },
                        { "point", new { x = hit.point.x, y = hit.point.y, z = hit.point.z } },
                        { "normal", new { x = hit.normal.x, y = hit.normal.y, z = hit.normal.z } },
                        { "distance", hit.distance },
                        { "objectName", hit.collider.gameObject.name },
                        { "colliderName", hit.collider.name }
                    };
                    
                    data.affected_objects = new[] { hit.collider.gameObject.name };
                    data.count = 1;
                    
                    return UnityResponse.Success($"Raycast hit '{hit.collider.gameObject.name}' at distance {hit.distance:F2}", data);
                }
                else
                {
                    data.count = 0;
                    return UnityResponse.Success("Raycast missed (no hit)", data);
                }
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Raycast failed: {e.Message}");
            }
        }
        
        /// <summary>
        /// Set or get gravity
        /// POST /unity/physics/gravity
        /// </summary>
        public static UnityResponse SetGravity(string json)
        {
            try
            {
                var command = JsonUtility.FromJson<GravityCommand>(json);
                
                if (command.gravity != null)
                {
                    Vector3 newGravity = command.gravity.ToVector3();
                    Physics.gravity = newGravity;
                    return UnityResponse.Success($"Gravity set to ({newGravity.x}, {newGravity.y}, {newGravity.z})");
                }
                else
                {
                    Vector3 current = Physics.gravity;
                    return UnityResponse.Success($"Current gravity: ({current.x}, {current.y}, {current.z})");
                }
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Gravity setting failed: {e.Message}");
            }
        }
        
        /// <summary>
        /// Get current physics settings
        /// GET /unity/physics/settings
        /// </summary>
        public static UnityResponse GetSettings()
        {
            try
            {
                var gravity = Physics.gravity;
                var settings = new
                {
                    gravity = new { x = gravity.x, y = gravity.y, z = gravity.z },
                    fixedDeltaTime = Time.fixedDeltaTime,
                    defaultSolverIterations = Physics.defaultSolverIterations,
                    defaultContactOffset = Physics.defaultContactOffset,
                    bounceThreshold = Physics.bounceThreshold
                };
                
                return UnityResponse.Success("Physics settings retrieved", new ResponseData());
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Failed to get physics settings: {e.Message}");
            }
        }
    }
    
    #region Physics Command Models
    
    [Serializable]
    public class PhysicsSimulateCommand
    {
        public float seconds = 1f;
        public float stepSize = -1f;  // -1 = use fixedDeltaTime
    }
    
    [Serializable]
    public class PhysicsStepCommand
    {
        public float deltaTime = -1f;  // -1 = use fixedDeltaTime
        public int steps = 1;
    }
    
    [Serializable]
    public class RaycastCommand
    {
        public Vector3Data origin;
        public Vector3Data direction;
        public float maxDistance = -1f;  // -1 = infinity
        public int layerMask = -1;  // -1 = all layers
    }
    
    [Serializable]
    public class GravityCommand
    {
        public Vector3Data gravity;
    }
    
    #endregion
}
