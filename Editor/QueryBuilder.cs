using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AntigravityBridge.Editor.Models;

namespace AntigravityBridge.Editor
{
    /// <summary>
    /// Fluent query builder for constructing Unity scene queries
    /// Provides Unix-like command structure: find . --component Light --parent viale
    /// </summary>
    public class QueryBuilder
    {
        private string _parent;
        private string _component;
        private string _tag;
        private string _name;
        private int _layer = -1;
        private bool _recursive = true;
        private bool _includeInactive = false;
        private string[] _select;
        private int _limit = 100;
        private int _offset = 0;
        private int _precision = 3;
        private string _format = "full";

        private QueryBuilder() { }

        #region Static Factory Methods

        /// <summary>
        /// Start a find query (equivalent to: find .)
        /// </summary>
        public static QueryBuilder Find()
        {
            return new QueryBuilder();
        }

        /// <summary>
        /// Start a find query in a specific parent (equivalent to: find parent/)
        /// </summary>
        public static QueryBuilder Find(string parent)
        {
            return new QueryBuilder().InParent(parent);
        }

        #endregion

        #region Filter Methods (--modifiers)

        /// <summary>
        /// Filter by component type (--component Light)
        /// </summary>
        public QueryBuilder WithComponent(string componentType)
        {
            _component = componentType;
            return this;
        }

        /// <summary>
        /// Filter by tag (--tag Player)
        /// </summary>
        public QueryBuilder WithTag(string tag)
        {
            _tag = tag;
            return this;
        }

        /// <summary>
        /// Filter by name pattern (--name "Cube*")
        /// </summary>
        public QueryBuilder WithName(string namePattern)
        {
            _name = namePattern;
            return this;
        }

        /// <summary>
        /// Filter by layer (--layer 5)
        /// </summary>
        public QueryBuilder OnLayer(int layer)
        {
            _layer = layer;
            return this;
        }

        /// <summary>
        /// Search in parent object (--parent viale)
        /// </summary>
        public QueryBuilder InParent(string parent)
        {
            _parent = parent;
            return this;
        }

        /// <summary>
        /// Search recursively in children (--recursive, default: true)
        /// </summary>
        public QueryBuilder Recursive(bool recursive = true)
        {
            _recursive = recursive;
            return this;
        }

        /// <summary>
        /// Include inactive objects (--inactive)
        /// </summary>
        public QueryBuilder IncludeInactive(bool include = true)
        {
            _includeInactive = include;
            return this;
        }

        #endregion

        #region Output Methods (--format, --select)

        /// <summary>
        /// Select specific fields (--select name,path,components)
        /// </summary>
        public QueryBuilder Select(params string[] fields)
        {
            _select = fields;
            return this;
        }

        /// <summary>
        /// Limit number of results (--limit 10)
        /// </summary>
        public QueryBuilder Limit(int limit)
        {
            _limit = limit;
            return this;
        }

        /// <summary>
        /// Skip first N results (--offset 20)
        /// </summary>
        public QueryBuilder Offset(int offset)
        {
            _offset = offset;
            return this;
        }

        /// <summary>
        /// Set decimal precision for transforms (--precision 3)
        /// </summary>
        public QueryBuilder Precision(int decimals)
        {
            _precision = decimals;
            return this;
        }

        /// <summary>
        /// Set output format (--format names_only)
        /// </summary>
        public QueryBuilder Format(string format)
        {
            _format = format;
            return this;
        }

        /// <summary>
        /// Shorthand for --format names_only
        /// </summary>
        public QueryBuilder NamesOnly()
        {
            _format = "names_only";
            return this;
        }

        /// <summary>
        /// Shorthand for --format minimal
        /// </summary>
        public QueryBuilder Minimal()
        {
            _format = "minimal";
            return this;
        }

        #endregion

        #region Build Methods

        /// <summary>
        /// Build FilterCriteria for use with CommandExecutor
        /// </summary>
        public FilterCriteria BuildFilter()
        {
            return new FilterCriteria
            {
                component = _component,
                type = !string.IsNullOrEmpty(_tag) ? "tag" : 
                       !string.IsNullOrEmpty(_name) ? "name" : null,
                value = _tag ?? _name,
                recursive = _recursive,
                includeInactive = _includeInactive
            };
        }

        /// <summary>
        /// Build QueryOptions for use with SceneQueryAPI
        /// </summary>
        public QueryOptions BuildOptions()
        {
            return new QueryOptions
            {
                select = _select,
                limit = _limit,
                offset = _offset,
                precision = _precision,
                format = _format
            };
        }

        /// <summary>
        /// Get parent name for queries
        /// </summary>
        public string GetParent()
        {
            return _parent;
        }

        /// <summary>
        /// Execute the query and return matching GameObjects
        /// </summary>
        public GameObject[] Execute()
        {
            var results = new List<GameObject>();
            GameObject[] searchSpace;

            // Determine search space
            if (!string.IsNullOrEmpty(_parent))
            {
                var parentObj = GameObject.Find(_parent);
                if (parentObj == null) return new GameObject[0];
                
                if (_recursive)
                {
                    searchSpace = GetAllChildren(parentObj).ToArray();
                }
                else
                {
                    searchSpace = new GameObject[parentObj.transform.childCount];
                    for (int i = 0; i < parentObj.transform.childCount; i++)
                    {
                        searchSpace[i] = parentObj.transform.GetChild(i).gameObject;
                    }
                }
            }
            else
            {
                // Search entire scene
                searchSpace = _includeInactive 
                    ? Resources.FindObjectsOfTypeAll<GameObject>()
                    : GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            }

            // Apply filters
            foreach (var obj in searchSpace)
            {
                if (!_includeInactive && !obj.activeInHierarchy) continue;
                if (!MatchesFilters(obj)) continue;
                
                results.Add(obj);
                
                if (results.Count >= _limit) break;
            }

            // Apply offset
            if (_offset > 0 && _offset < results.Count)
            {
                results = results.Skip(_offset).ToList();
            }

            return results.ToArray();
        }

        /// <summary>
        /// Execute and return as UnityResponse
        /// </summary>
        public UnityResponse ExecuteAsResponse()
        {
            try
            {
                var objects = Execute();
                
                if (_format == "names_only")
                {
                    var names = objects.Select(o => o.name).ToArray();
                    return UnityResponse.Success($"Found {names.Length} objects", new ResponseData
                    {
                        affected_objects = names,
                        count = names.Length
                    });
                }

                var objectNames = objects.Select(o => o.name).ToArray();
                return UnityResponse.Success($"Found {objects.Length} objects", new ResponseData
                {
                    affected_objects = objectNames,
                    count = objects.Length
                });
            }
            catch (Exception e)
            {
                return UnityResponse.Error($"Query failed: {e.Message}");
            }
        }

        #endregion

        #region Private Helpers

        private bool MatchesFilters(GameObject obj)
        {
            // Component filter
            if (!string.IsNullOrEmpty(_component))
            {
                var componentType = FindComponentType(_component);
                if (componentType == null || obj.GetComponent(componentType) == null)
                    return false;
            }

            // Tag filter
            if (!string.IsNullOrEmpty(_tag) && obj.tag != _tag)
                return false;

            // Name filter (supports * wildcard)
            if (!string.IsNullOrEmpty(_name))
            {
                if (_name.Contains("*"))
                {
                    var pattern = _name.Replace("*", "");
                    if (_name.StartsWith("*") && _name.EndsWith("*"))
                    {
                        if (!obj.name.Contains(pattern)) return false;
                    }
                    else if (_name.StartsWith("*"))
                    {
                        if (!obj.name.EndsWith(pattern)) return false;
                    }
                    else if (_name.EndsWith("*"))
                    {
                        if (!obj.name.StartsWith(pattern)) return false;
                    }
                }
                else if (obj.name != _name)
                {
                    return false;
                }
            }

            // Layer filter
            if (_layer >= 0 && obj.layer != _layer)
                return false;

            return true;
        }

        private Type FindComponentType(string typeName)
        {
            // Try common Unity types first
            var unityType = Type.GetType($"UnityEngine.{typeName}, UnityEngine");
            if (unityType != null) return unityType;

            // Try finding in all assemblies
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var type = assembly.GetTypes()
                        .FirstOrDefault(t => t.Name == typeName && typeof(Component).IsAssignableFrom(t));
                    if (type != null) return type;
                }
                catch { }
            }

            return null;
        }

        private List<GameObject> GetAllChildren(GameObject parent)
        {
            var children = new List<GameObject>();
            GetChildrenRecursive(parent.transform, children);
            return children;
        }

        private void GetChildrenRecursive(Transform parent, List<GameObject> list)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                list.Add(child.gameObject);
                GetChildrenRecursive(child, list);
            }
        }

        #endregion

        #region String Representation

        /// <summary>
        /// Get Unix-like command representation
        /// </summary>
        public override string ToString()
        {
            var parts = new List<string> { "find" };
            
            parts.Add(string.IsNullOrEmpty(_parent) ? "." : _parent);
            
            if (!string.IsNullOrEmpty(_component))
                parts.Add($"--component {_component}");
            if (!string.IsNullOrEmpty(_tag))
                parts.Add($"--tag {_tag}");
            if (!string.IsNullOrEmpty(_name))
                parts.Add($"--name \"{_name}\"");
            if (_layer >= 0)
                parts.Add($"--layer {_layer}");
            if (!_recursive)
                parts.Add("--no-recursive");
            if (_includeInactive)
                parts.Add("--inactive");
            if (_select != null && _select.Length > 0)
                parts.Add($"--select {string.Join(",", _select)}");
            if (_limit != 100)
                parts.Add($"--limit {_limit}");
            if (_format != "full")
                parts.Add($"--format {_format}");

            return string.Join(" ", parts);
        }

        #endregion
    }
}
