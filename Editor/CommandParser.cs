using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using AntigravityBridge.Editor.Models;

namespace AntigravityBridge.Editor
{
    /// <summary>
    /// Parser for Unix-like command strings
    /// Converts "find . --component Light --limit 10" to QueryBuilder/CommandBuilder calls
    /// </summary>
    public static class CommandParser
    {
        /// <summary>
        /// Parse and execute a Unix-like command string
        /// POST /unity/command
        /// Body: { "cmd": "find . --component Light --format names_only" }
        /// </summary>
        public static UnityResponse ParseAndExecute(string commandString)
        {
            try
            {
                var tokens = Tokenize(commandString);
                if (tokens.Count == 0)
                    return UnityResponse.Error("Empty command");

                var command = tokens[0].ToLower();
                tokens.RemoveAt(0);

                switch (command)
                {
                    case "find":
                        return ExecuteFind(tokens);
                    case "create":
                        return ExecuteCreate(tokens);
                    case "modify":
                        return ExecuteModify(tokens);
                    case "delete":
                        return ExecuteDelete(tokens);
                    case "get":
                        return ExecuteGet(tokens);
                    case "help":
                        return GetHelp();
                    default:
                        return UnityResponse.Error($"Unknown command: {command}. Use 'help' for available commands.");
                }
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Command parse error: {e.Message}");
            }
        }

        #region Command Executors

        private static UnityResponse ExecuteFind(List<string> tokens)
        {
            var builder = QueryBuilder.Find();
            string target = ".";

            int i = 0;
            // First arg is target (if not starting with --)
            if (tokens.Count > 0 && !tokens[0].StartsWith("--"))
            {
                target = tokens[0];
                i = 1;
                if (target != ".")
                    builder.InParent(target);
            }

            // Parse modifiers
            while (i < tokens.Count)
            {
                var flag = tokens[i].ToLower();
                string value = (i + 1 < tokens.Count && !tokens[i + 1].StartsWith("--")) 
                    ? tokens[++i] : null;

                switch (flag)
                {
                    case "--component":
                    case "-c":
                        if (value != null) builder.WithComponent(value);
                        break;
                    case "--tag":
                    case "-t":
                        if (value != null) builder.WithTag(value);
                        break;
                    case "--name":
                    case "-n":
                        if (value != null) builder.WithName(value);
                        break;
                    case "--layer":
                    case "-l":
                        if (value != null && int.TryParse(value, out int layer))
                            builder.OnLayer(layer);
                        break;
                    case "--parent":
                    case "-p":
                        if (value != null) builder.InParent(value);
                        break;
                    case "--limit":
                        if (value != null && int.TryParse(value, out int limit))
                            builder.Limit(limit);
                        break;
                    case "--format":
                    case "-f":
                        if (value != null) builder.Format(value);
                        break;
                    case "--select":
                    case "-s":
                        if (value != null) builder.Select(value.Split(','));
                        break;
                    case "--precision":
                        if (value != null && int.TryParse(value, out int precision))
                            builder.Precision(precision);
                        break;
                    case "--inactive":
                        builder.IncludeInactive();
                        break;
                    case "--no-recursive":
                        builder.Recursive(false);
                        break;
                    case "--names-only":
                        builder.NamesOnly();
                        break;
                }
                i++;
            }

            // Log the parsed command
            Debug.Log($"[Antigravity] Executing: {builder}");

            return builder.ExecuteAsResponse();
        }

        private static UnityResponse ExecuteCreate(List<string> tokens)
        {
            if (tokens.Count == 0)
                return UnityResponse.Error("create: missing object name");

            var name = tokens[0];
            var builder = CommandBuilder.Create(name);

            int i = 1;
            while (i < tokens.Count)
            {
                var flag = tokens[i].ToLower();
                string value = (i + 1 < tokens.Count && !tokens[i + 1].StartsWith("--")) 
                    ? tokens[++i] : null;

                switch (flag)
                {
                    case "--position":
                    case "--pos":
                    case "-p":
                        if (value != null)
                        {
                            var pos = ParseVector3(value);
                            if (pos.HasValue) builder.Position(pos.Value);
                        }
                        break;
                    case "--rotation":
                    case "--rot":
                    case "-r":
                        if (value != null)
                        {
                            var rot = ParseVector3(value);
                            if (rot.HasValue) builder.Rotation(rot.Value.x, rot.Value.y, rot.Value.z);
                        }
                        break;
                    case "--scale":
                        if (value != null)
                        {
                            var scale = ParseVector3(value);
                            if (scale.HasValue) builder.Scale(scale.Value.x, scale.Value.y, scale.Value.z);
                        }
                        break;
                    case "--parent":
                        if (value != null) builder.Parent(value);
                        break;
                    case "--components":
                    case "--comp":
                    case "-c":
                        if (value != null) builder.Components(value.Split(','));
                        break;
                }
                i++;
            }

            Debug.Log($"[Antigravity] Executing: {builder}");

            return builder.Execute();
        }

        private static UnityResponse ExecuteModify(List<string> tokens)
        {
            if (tokens.Count == 0)
                return UnityResponse.Error("modify: missing target objects");

            // Collect targets until we hit a flag
            var targets = new List<string>();
            int i = 0;
            while (i < tokens.Count && !tokens[i].StartsWith("--"))
            {
                targets.Add(tokens[i]);
                i++;
            }

            if (targets.Count == 0)
                return UnityResponse.Error("modify: no targets specified");

            var builder = CommandBuilder.Modify(targets.ToArray());

            while (i < tokens.Count)
            {
                var flag = tokens[i].ToLower();
                string value = (i + 1 < tokens.Count && !tokens[i + 1].StartsWith("--")) 
                    ? tokens[++i] : null;

                switch (flag)
                {
                    case "--add":
                    case "-a":
                        if (value != null) builder.Add(value);
                        break;
                    case "--remove":
                    case "-r":
                        if (value != null) builder.Remove(value);
                        break;
                    case "--set":
                    case "-s":
                        if (value != null)
                        {
                            var parts = value.Split('=');
                            if (parts.Length == 2)
                            {
                                if (parts[0] == "active")
                                    builder.SetActive(parts[1].ToLower() == "true" || parts[1] == "1");
                                else
                                    builder.Set(parts[0], parts[1]);
                            }
                        }
                        break;
                    case "--position":
                    case "--pos":
                        if (value != null)
                        {
                            var pos = ParseVector3(value);
                            if (pos.HasValue) builder.Position(pos.Value);
                        }
                        break;
                }
                i++;
            }

            Debug.Log($"[Antigravity] Executing: {builder}");

            return builder.Execute();
        }

        private static UnityResponse ExecuteDelete(List<string> tokens)
        {
            if (tokens.Count == 0)
                return UnityResponse.Error("delete: missing target objects");

            var targets = new List<string>();
            bool force = false;
            bool recursive = false;

            foreach (var token in tokens)
            {
                switch (token.ToLower())
                {
                    case "--force":
                    case "-f":
                        force = true;
                        break;
                    case "--recursive":
                    case "-r":
                        recursive = true;
                        break;
                    default:
                        if (!token.StartsWith("--"))
                            targets.Add(token);
                        break;
                }
            }

            var builder = CommandBuilder.Delete(targets.ToArray())
                .Force(force)
                .Recursive(recursive);

            Debug.Log($"[Antigravity] Executing: {builder}");

            return builder.Execute();
        }

        private static UnityResponse ExecuteGet(List<string> tokens)
        {
            if (tokens.Count == 0)
                return UnityResponse.Error("get: missing object name");

            var objectName = tokens[0];
            var options = new QueryOptions();

            int i = 1;
            while (i < tokens.Count)
            {
                var flag = tokens[i].ToLower();
                string value = (i + 1 < tokens.Count && !tokens[i + 1].StartsWith("--")) 
                    ? tokens[++i] : null;

                switch (flag)
                {
                    case "--select":
                    case "-s":
                        if (value != null) options.select = value.Split(',');
                        break;
                    case "--format":
                    case "-f":
                        if (value != null) options.format = value;
                        break;
                    case "--precision":
                        if (value != null && int.TryParse(value, out int precision))
                            options.precision = precision;
                        break;
                }
                i++;
            }

            return SceneQueryAPI.GetObjectInfo(objectName, options);
        }

        private static UnityResponse GetHelp()
        {
            var helpText = @"
Antigravity Unity Bridge - Command Reference

FIND - Search for GameObjects
  find .                                    # Find all objects
  find viale --component Light              # Find Lights in 'viale'
  find . --tag Player --format names_only   # Find by tag, return names only
  find . --name ""Cube*"" --limit 10          # Find by name pattern

CREATE - Create new GameObjects
  create MyCube                             # Create empty object
  create MyCube --position 0,1,0            # Create at position
  create MyCube --components BoxCollider,Rigidbody  # Create with components
  create MyCube --parent Environment        # Create as child

MODIFY - Modify existing GameObjects
  modify Player --set active=false          # Deactivate
  modify Player --add AudioSource           # Add component
  modify Player --remove Rigidbody          # Remove component
  modify Player --position 0,5,0            # Move object

DELETE - Delete GameObjects
  delete TempObject                         # Delete single object
  delete Obj1 Obj2 Obj3 --force            # Delete multiple

GET - Get object info
  get MainCamera                            # Full object info
  get MainCamera --select components        # Only components
  get Player --format exists_only           # Check if exists
";
            return UnityResponse.Success("Command help", new ResponseData
            {
                errors = new[] { helpText }  // Using errors field to pass text
            });
        }

        #endregion

        #region Helpers

        private static List<string> Tokenize(string input)
        {
            var tokens = new List<string>();
            var regex = new Regex(@"[\""].+?[\""]|[^ ]+");
            var matches = regex.Matches(input);

            foreach (Match match in matches)
            {
                var value = match.Value;
                // Remove surrounding quotes
                if (value.StartsWith("\"") && value.EndsWith("\""))
                    value = value.Substring(1, value.Length - 2);
                tokens.Add(value);
            }

            return tokens;
        }

        private static Vector3? ParseVector3(string value)
        {
            var parts = value.Split(',');
            if (parts.Length == 3 &&
                float.TryParse(parts[0], out float x) &&
                float.TryParse(parts[1], out float y) &&
                float.TryParse(parts[2], out float z))
            {
                return new Vector3(x, y, z);
            }
            return null;
        }

        #endregion
    }
}
