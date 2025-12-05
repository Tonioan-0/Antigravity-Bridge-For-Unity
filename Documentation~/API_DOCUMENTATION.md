# Antigravity Unity Bridge - API Documentation

Complete REST API reference for controlling Unity Editor scenes.

**Base URL**: `http://localhost:8080`

## Table of Contents

- [Response Format](#response-format)
- [Status Endpoints](#status-endpoints)
- [Scene Query Endpoints](#scene-query-endpoints)
- [Scene Operation Endpoints](#scene-operation-endpoints)
- [Component Operation Endpoints](#component-operation-endpoints)
- [Settings Endpoints](#settings-endpoints)
- [Error Handling](#error-handling)

---

## Response Format

All responses follow this JSON structure:

```json
{
  "status": "success|error|partial",
  "message": "Human-readable description",
  "data": {
    // Response-specific data
  },
  "timestamp": "2024-01-15T10:30:00.000Z"
}
```

### Status Values

- **success**: Operation completed successfully
- **error**: Operation failed completely
- **partial**: Operation partially succeeded (some items failed)

---

## Status Endpoints

### GET /unity/status

Get server status and basic information.

**Response:**
```json
{
  "status": "success",
  "message": "Server is running",
  "data": {
    "server_status": {
      "is_running": true,
      "port": 8080,
      "unity_version": "2022.3.10f1",
      "editor_mode": "Edit",
      "commands_processed": 42,
      "success_count": 40,
      "error_count": 2,
      "uptime_seconds": 3600.5
    }
  }
}
```

### GET /unity/health

Alias for `/unity/status`.

---

## Scene Query Endpoints

### GET /unity/scene/hierarchy

Get complete scene hierarchy with all GameObjects, components, and structure.

**Response:**
```json
{
  "status": "success",
  "message": "Retrieved hierarchy with 3 root objects",
  "data": {
    "scene_hierarchy": {
      "scene_name": "MainScene",
      "scene_path": "Assets/Scenes/MainScene.unity",
      "root_objects": [
        {
          "name": "Main Camera",
          "path": "Main Camera",
          "active": true,
          "tag": "MainCamera",
          "layer": 0,
          "components": ["Transform", "Camera", "AudioListener"],
          "position": {"x": 0, "y": 1, "z": -10},
          "rotation": {"x": 0, "y": 0, "z": 0},
          "scale": {"x": 1, "y": 1, "z": 1},
          "children": []
        }
      ]
    }
  }
}
```

### GET /unity/scene/objects/{name}

Get detailed information about a specific GameObject.

**Parameters:**
- `{name}`: GameObject name or hierarchy path (e.g., "Player" or "Environment/Lights/Light_01")

**Example:**
```bash
curl http://localhost:8080/unity/scene/objects/Main%20Camera
```

**Response:**
```json
{
  "status": "success",
  "message": "Retrieved info for 'Main Camera'",
  "data": {
    "object_info": {
      "name": "Main Camera",
      "path": "Main Camera",
      "active": true,
      "tag": "MainCamera",
      "layer": 0,
      "components": [
        {
          "type": "Camera",
          "name": "UnityEngine.Camera"
        },
        {
          "type": "AudioListener",
          "name": "UnityEngine.AudioListener"
        }
      ],
      "position": {"x": 0, "y": 1, "z": -10},
      "rotation": {"x": 0, "y": 0, "z": 0},
      "scale": {"x": 1, "y": 1, "z": 1},
      "children": []
    }
  }
}
```

### GET /unity/scene/info

Get current scene metadata.

**Response:**
```json
{
  "status": "success",
  "message": "Scene: MainScene",
  "data": {
    "scene_info": {
      "name": "MainScene",
      "path": "Assets/Scenes/MainScene.unity",
      "is_loaded": true,
      "is_modified": false,
      "object_count": 15,
      "root_object_count": 3,
      "build_index": "0"
    }
  }
}
```

### GET /unity/project/scripts

List all available MonoBehaviour scripts in the project.

**Response:**
```json
{
  "status": "success",
  "message": "Found 42 scripts",
  "data": {
    "available_scripts": [
      "ActivateDisableLights",
      "PlayerController",
      "CameraFollow",
      "GameManager"
    ],
    "count": 42
  }
}
```

### GET /unity/components/list

List all available component types (Unity built-in + project scripts).

**Response:**
```json
{
  "status": "success",
  "message": "Found 120 components",
  "data": {
    "available_components": [
      "Animator",
      "AudioSource",
      "BoxCollider",
      "Camera",
      "Light",
      "Rigidbody",
      "ActivateDisableLights"
    ],
    "count": 120
  }
}
```

---

## Scene Operation Endpoints

### POST /unity/scene/find

Find GameObjects matching criteria.

**Request Body:**
```json
{
  "parent": "Environment",
  "filter": {
    "component": "Light",
    "includeInactive": false,
    "recursive": true
  }
}
```

**Response:**
```json
{
  "status": "success",
  "message": "Found 5 objects",
  "data": {
    "affected_objects": ["Light_01", "Light_02", "Light_03", "Light_04", "Light_05"],
    "count": 5
  }
}
```

**Filter Options:**
- `type`: "name", "tag", "component", "layer"
- `value`: Value to match
- `component`: Component type name to filter by
- `includeInactive`: Include inactive objects (default: false)
- `recursive`: Search recursively in children (default: true)

### POST /unity/scene/create

Create a new GameObject.

**Request Body:**
```json
{
  "name": "NewLight",
  "parent": "Lights",
  "position": {"x": 0, "y": 5, "z": 10},
  "rotation": {"x": 0, "y": 0, "z": 0},
  "scale": {"x": 1, "y": 1, "z": 1},
  "components": ["Light"]
}
```

**Response:**
```json
{
  "status": "success",
  "message": "Created GameObject 'NewLight'",
  "data": {
    "affected_objects": ["NewLight"],
    "count": 1
  }
}
```

### POST /unity/scene/modify

Modify GameObject properties.

**Request Body:**
```json
{
  "objects": ["Player", "Enemy_01"],
  "properties": {
    "active": true,
    "tag": "Player",
    "layer": 8
  }
}
```

**Response:**
```json
{
  "status": "success",
  "message": "Modified 2 objects",
  "data": {
    "affected_objects": ["Player", "Enemy_01"],
    "count": 2
  }
}
```

### POST /unity/scene/delete

Delete GameObjects.

**Request Body:**
```json
{
  "objects": ["TempObject", "DebugCube"]
}
```

**Response:**
```json
{
  "status": "success",
  "message": "Deleted 2 objects",
  "data": {
    "affected_objects": ["TempObject", "DebugCube"],
    "count": 2
  }
}
```

### POST /unity/scene/find_and_modify

**The main command for Antigravity**: Find objects and apply operations in one call.

**Request Body:**
```json
{
  "parent": "viale",
  "filter": {
    "component": "Light"
  },
  "operations": [
    {
      "type": "add_component",
      "component": "ActivateDisableLights"
    }
  ]
}
```

**Operation Types:**
- `add_component`: Add a component
- `remove_component`: Remove a component
- `set_active`: Set active state (requires `boolValue`)
- `delete`: Delete the GameObject

**Response:**
```json
{
  "status": "success",
  "message": "Modified 5 objects",
  "data": {
    "affected_objects": ["Light_01", "Light_02", "Light_03", "Light_04", "Light_05"],
    "count": 5,
    "errors": []
  }
}
```

---

## Component Operation Endpoints

### POST /unity/component/add

Add component to GameObjects.

**Request Body:**
```json
{
  "objects": ["Player", "Enemy"],
  "component": "Rigidbody"
}
```

**Response:**
```json
{
  "status": "success",
  "message": "Added component to 2 objects",
  "data": {
    "affected_objects": ["Player", "Enemy"],
    "count": 2
  }
}
```

### POST /unity/component/remove

Remove component from GameObjects.

**Request Body:**
```json
{
  "objects": ["Player"],
  "component": "AudioSource"
}
```

### POST /unity/component/modify

Modify component properties using reflection.

**Request Body:**
```json
{
  "objects": ["Main Camera"],
  "component": "Camera",
  "properties": {
    "fieldOfView": 60,
    "nearClipPlane": 0.3
  }
}
```

---

## Settings Endpoints

### GET /unity/settings/{category}

Read project settings.

**Categories:**
- `player`: Player settings
- `quality`: Quality settings
- `physics`: Physics settings
- `audio`: Audio settings
- `graphics`: Graphics settings

**Example:**
```bash
curl http://localhost:8080/unity/settings/player
```

**Response:**
```json
{
  "status": "success",
  "message": "Retrieved player settings",
  "data": {
    "settings": {
      "companyName": "MyCompany",
      "productName": "MyGame",
      "version": "1.0.0",
      "defaultScreenWidth": 1920,
      "defaultScreenHeight": 1080
    }
  }
}
```

### POST /unity/settings/{category}

Modify project settings.

**Request Body:**
```json
{
  "settings": {
    "productName": "My Awesome Game",
    "version": "1.1.0"
  }
}
```

---

## Error Handling

### Error Response Example

```json
{
  "status": "error",
  "message": "GameObject 'InvalidName' not found",
  "data": {
    "errors": ["Object not found in scene"]
  },
  "timestamp": "2024-01-15T10:30:00.000Z"
}
```

### Partial Success Example

When some operations succeed and some fail:

```json
{
  "status": "partial",
  "message": "Modified 3 objects with 2 errors",
  "data": {
    "affected_objects": ["Light_01", "Light_02", "Light_03"],
    "count": 3,
    "errors": [
      "Light_04: Component 'InvalidComponent' not found",
      "Light_05: GameObject not found"
    ]
  }
}
```

### Common Error Codes

- **404**: Endpoint not found
- **500**: Internal server error
- **200**: Success (even for error responses - check `status` field)

---

## Example Workflows

### Workflow 1: Setup Scene Lighting

```bash
# 1. Find all lights
curl -X POST http://localhost:8080/unity/scene/find \
  -H "Content-Type: application/json" \
  -d '{"filter": {"component": "Light"}}'

# 2. Add control script to all lights
curl -X POST http://localhost:8080/unity/component/add \
  -H "Content-Type: application/json" \
  -d '{
    "objects": ["Light_01", "Light_02", "Light_03"],
    "component": "LightController"
  }'

# 3. Modify light properties
curl -X POST http://localhost:8080/unity/component/modify \
  -H "Content-Type: application/json" \
  -d '{
    "objects": ["Light_01"],
    "component": "Light",
    "properties": {"intensity": 2.5, "color": "#FFFFFF"}
  }'
```

### Workflow 2: Create Camera System

```bash
# 1. Create main camera container
curl -X POST http://localhost:8080/unity/scene/create \
  -H "Content-Type: application/json" \
  -d '{
    "name": "CameraSystem",
    "position": {"x": 0, "y": 0, "z": 0}
  }'

# 2. Create camera
curl -X POST http://localhost:8080/unity/scene/create \
  -H "Content-Type: application/json" \
  -d '{
    "name": "MainCamera",
    "parent": "CameraSystem",
    "position": {"x": 0, "y": 10, "z": -10},
    "components": ["Camera", "AudioListener", "CameraFollow"]
  }'
```

---

## Rate Limiting & Batch Limits

- **Batch Operations**: Maximum 1000 objects per operation
- **No Rate Limiting**: Currently no rate limiting (local use only)
- **Timeout**: Operations timeout after 30 seconds

---

## Best Practices

1. **Use find_and_modify for bulk operations**: More efficient than separate find + modify
2. **Specify parent when possible**: Narrows search scope
3. **Check available scripts first**: Use `/unity/project/scripts` before adding components
4. **Handle partial success**: Check `errors` array in responses
5. **Use full hierarchy paths**: More reliable than simple names

---

For more information, see the [README](../README.md).
