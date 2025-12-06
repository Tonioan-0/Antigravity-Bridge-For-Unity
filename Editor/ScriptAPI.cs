using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using AntigravityBridge.Editor.Models;

namespace AntigravityBridge.Editor
{
    /// <summary>
    /// API for creating and managing C# scripts
    /// Supports creating MonoBehaviour scripts with custom methods
    /// </summary>
    public static class ScriptAPI
    {
        /// <summary>
        /// Create a new C# script file
        /// POST /unity/script/create
        /// </summary>
        public static UnityResponse CreateScript(string json)
        {
            try
            {
                var command = JsonUtility.FromJson<ScriptCreateCommand>(json);
                
                if (string.IsNullOrEmpty(command.name))
                {
                    return UnityResponse.Error("Script name not specified");
                }

                // Sanitize script name (remove spaces, special chars)
                string scriptName = SanitizeScriptName(command.name);
                
                // Default path to Assets/Scripts
                string folder = string.IsNullOrEmpty(command.path) ? "Assets/Scripts" : command.path;
                
                // Ensure folder exists
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    CreateFolderRecursive(folder);
                }

                string filePath = $"{folder}/{scriptName}.cs";
                
                // Check if file already exists
                if (File.Exists(filePath))
                {
                    return UnityResponse.Error($"Script '{scriptName}' already exists at {filePath}");
                }

                // Generate script content
                string content = GenerateScriptContent(scriptName, command);
                
                // Write file
                File.WriteAllText(filePath, content);
                
                // Import the asset
                AssetDatabase.Refresh();

                return UnityResponse.Success($"Created script '{scriptName}' at {filePath}", new ResponseData
                {
                    affected_objects = new[] { filePath },
                    count = 1
                });
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Script creation failed: {e.Message}");
            }
        }

        /// <summary>
        /// Get all available MonoBehaviour scripts in project
        /// GET /unity/script/list
        /// </summary>
        public static UnityResponse GetAvailableScripts()
        {
            try
            {
                var scripts = SceneQueryAPI.GetAvailableScripts();
                return scripts;
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Failed to list scripts: {e.Message}");
            }
        }

        #region Private Helpers

        private static string SanitizeScriptName(string name)
        {
            // Remove invalid characters for C# class names
            var sanitized = new System.Text.StringBuilder();
            foreach (char c in name)
            {
                if (char.IsLetterOrDigit(c) || c == '_')
                {
                    sanitized.Append(c);
                }
            }
            
            string result = sanitized.ToString();
            
            // Ensure it doesn't start with a number
            if (result.Length > 0 && char.IsDigit(result[0]))
            {
                result = "_" + result;
            }
            
            return result;
        }

        private static void CreateFolderRecursive(string path)
        {
            string[] folders = path.Split('/');
            string currentPath = "";
            
            for (int i = 0; i < folders.Length; i++)
            {
                string parent = i == 0 ? "" : currentPath;
                string folder = folders[i];
                string newPath = i == 0 ? folder : $"{currentPath}/{folder}";
                
                if (!AssetDatabase.IsValidFolder(newPath))
                {
                    if (string.IsNullOrEmpty(parent))
                    {
                        // Can't create at root, skip
                    }
                    else
                    {
                        AssetDatabase.CreateFolder(parent, folder);
                    }
                }
                
                currentPath = newPath;
            }
        }

        private static string GenerateScriptContent(string className, ScriptCreateCommand command)
        {
            var sb = new System.Text.StringBuilder();
            
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine();
            sb.AppendLine($"public class {className} : MonoBehaviour");
            sb.AppendLine("{");
            
            // Add requested methods
            if (command.methods != null)
            {
                foreach (var method in command.methods)
                {
                    sb.AppendLine();
                    switch (method.ToLower())
                    {
                        case "start":
                            sb.AppendLine("    void Start()");
                            sb.AppendLine("    {");
                            sb.AppendLine("        ");
                            sb.AppendLine("    }");
                            break;
                        case "update":
                            sb.AppendLine("    void Update()");
                            sb.AppendLine("    {");
                            sb.AppendLine("        ");
                            sb.AppendLine("    }");
                            break;
                        case "fixedupdate":
                            sb.AppendLine("    void FixedUpdate()");
                            sb.AppendLine("    {");
                            sb.AppendLine("        ");
                            sb.AppendLine("    }");
                            break;
                        case "awake":
                            sb.AppendLine("    void Awake()");
                            sb.AppendLine("    {");
                            sb.AppendLine("        ");
                            sb.AppendLine("    }");
                            break;
                        case "ontriggerenter":
                            sb.AppendLine("    void OnTriggerEnter(Collider other)");
                            sb.AppendLine("    {");
                            sb.AppendLine("        ");
                            sb.AppendLine("    }");
                            break;
                        case "ontriggerexit":
                            sb.AppendLine("    void OnTriggerExit(Collider other)");
                            sb.AppendLine("    {");
                            sb.AppendLine("        ");
                            sb.AppendLine("    }");
                            break;
                        case "oncollisionenter":
                            sb.AppendLine("    void OnCollisionEnter(Collision collision)");
                            sb.AppendLine("    {");
                            sb.AppendLine("        ");
                            sb.AppendLine("    }");
                            break;
                        case "ondestroy":
                            sb.AppendLine("    void OnDestroy()");
                            sb.AppendLine("    {");
                            sb.AppendLine("        ");
                            sb.AppendLine("    }");
                            break;
                        case "onenable":
                            sb.AppendLine("    void OnEnable()");
                            sb.AppendLine("    {");
                            sb.AppendLine("        ");
                            sb.AppendLine("    }");
                            break;
                        case "ondisable":
                            sb.AppendLine("    void OnDisable()");
                            sb.AppendLine("    {");
                            sb.AppendLine("        ");
                            sb.AppendLine("    }");
                            break;
                    }
                }
            }
            else
            {
                // Default: Start and Update
                sb.AppendLine();
                sb.AppendLine("    void Start()");
                sb.AppendLine("    {");
                sb.AppendLine("        ");
                sb.AppendLine("    }");
                sb.AppendLine();
                sb.AppendLine("    void Update()");
                sb.AppendLine("    {");
                sb.AppendLine("        ");
                sb.AppendLine("    }");
            }
            
            sb.AppendLine("}");
            
            return sb.ToString();
        }

        #endregion
    }

    #region Script Models

    [Serializable]
    public class ScriptCreateCommand
    {
        public string name;
        public string path;          // Folder path, e.g., "Assets/Scripts"
        public string[] methods;     // Methods to include: "Start", "Update", "OnTriggerEnter", etc.
    }

    #endregion
}
