using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using AntigravityBridge.Editor.Models;

namespace AntigravityBridge.Editor
{
    /// <summary>
    /// API for controlling Audio (AudioSource, AudioClip)
    /// Supports play, stop, volume, pitch, 3D audio settings
    /// </summary>
    public static class AudioAPI
    {
        /// <summary>
        /// Play audio clip on object
        /// POST /unity/audio/play
        /// </summary>
        public static UnityResponse PlayAudio(string json)
        {
            try
            {
                var command = JsonUtility.FromJson<AudioPlayCommand>(json);
                
                if (string.IsNullOrEmpty(command.objectName))
                {
                    return UnityResponse.Error("Object name not specified");
                }

                var obj = GameObject.Find(command.objectName);
                if (obj == null)
                {
                    return UnityResponse.Error($"Object '{command.objectName}' not found");
                }

                // Get or add AudioSource
                var audioSource = obj.GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = Undo.AddComponent<AudioSource>(obj);
                }

                // Load audio clip if specified
                if (!string.IsNullOrEmpty(command.clipPath))
                {
                    var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(command.clipPath);
                    if (clip == null)
                    {
                        return UnityResponse.Error($"Audio clip not found at '{command.clipPath}'");
                    }
                    audioSource.clip = clip;
                }

                // Apply settings
                if (command.volume >= 0)
                    audioSource.volume = command.volume;
                if (command.pitch > 0)
                    audioSource.pitch = command.pitch;
                
                audioSource.loop = command.loop;
                audioSource.Play();

                EditorUtility.SetDirty(audioSource);

                return UnityResponse.Success($"Playing audio on '{command.objectName}'", new ResponseData
                {
                    affected_objects = new[] { command.objectName },
                    count = 1
                });
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Audio play failed: {e.Message}");
            }
        }

        /// <summary>
        /// Stop audio on object
        /// POST /unity/audio/stop
        /// </summary>
        public static UnityResponse StopAudio(string json)
        {
            try
            {
                var command = JsonUtility.FromJson<AudioStopCommand>(json);
                
                if (string.IsNullOrEmpty(command.objectName))
                {
                    return UnityResponse.Error("Object name not specified");
                }

                var obj = GameObject.Find(command.objectName);
                if (obj == null)
                {
                    return UnityResponse.Error($"Object '{command.objectName}' not found");
                }

                var audioSource = obj.GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    return UnityResponse.Error($"'{command.objectName}' has no AudioSource");
                }

                audioSource.Stop();

                return UnityResponse.Success($"Stopped audio on '{command.objectName}'", new ResponseData
                {
                    affected_objects = new[] { command.objectName },
                    count = 1
                });
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Audio stop failed: {e.Message}");
            }
        }

        /// <summary>
        /// Modify AudioSource properties
        /// POST /unity/audio/modify
        /// </summary>
        public static UnityResponse ModifyAudioSource(string json)
        {
            try
            {
                var command = JsonUtility.FromJson<AudioModifyCommand>(json);
                
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

                        var audioSource = obj.GetComponent<AudioSource>();
                        if (audioSource == null)
                        {
                            errors.Add($"'{objectName}' has no AudioSource");
                            continue;
                        }

                        Undo.RecordObject(audioSource, "Modify AudioSource");
                        ApplyAudioProperties(audioSource, command);
                        EditorUtility.SetDirty(audioSource);
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
                    return UnityResponse.Error("No audio sources modified", errors.ToArray());
                }

                return UnityResponse.Success($"Modified {modifiedObjects.Count} audio sources", data);
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Audio modification failed: {e.Message}");
            }
        }

        #region Private Helpers

        private static void ApplyAudioProperties(AudioSource source, AudioModifyCommand command)
        {
            if (command.volume >= 0)
                source.volume = command.volume;
            if (command.pitch > 0)
                source.pitch = command.pitch;
            if (command.spatialBlend >= 0)
                source.spatialBlend = command.spatialBlend;  // 0=2D, 1=3D
            if (command.minDistance > 0)
                source.minDistance = command.minDistance;
            if (command.maxDistance > 0)
                source.maxDistance = command.maxDistance;
            if (command.loop.HasValue)
                source.loop = command.loop.Value;
            if (command.mute.HasValue)
                source.mute = command.mute.Value;
            if (command.playOnAwake.HasValue)
                source.playOnAwake = command.playOnAwake.Value;
        }

        #endregion
    }

    #region Audio Models

    /// <summary>
    /// Command for playing audio
    /// </summary>
    [Serializable]
    public class AudioPlayCommand
    {
        public string objectName;
        public string clipPath;      // Path in Assets, e.g., "Assets/Audio/jump.wav"
        public float volume = 1f;
        public float pitch = 1f;
        public bool loop = false;
    }

    /// <summary>
    /// Command for stopping audio
    /// </summary>
    [Serializable]
    public class AudioStopCommand
    {
        public string objectName;
    }

    /// <summary>
    /// Command for modifying AudioSource
    /// </summary>
    [Serializable]
    public class AudioModifyCommand
    {
        public string[] objects;
        public float volume = -1;        // -1 means don't change
        public float pitch = -1;
        public float spatialBlend = -1;  // 0=2D, 1=3D
        public float minDistance = -1;
        public float maxDistance = -1;
        public bool? loop;
        public bool? mute;
        public bool? playOnAwake;
    }

    #endregion
}
