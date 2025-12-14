# Antigravity Unity Bridge - API Documentation

Complete REST API reference for controlling Unity Editor scenes.

**Base URL**: `http://localhost:8080`

## Table of Contents

- [Response Format](#response-format)
- [Status Endpoints](#status-endpoints)
- [Scene Query Endpoints](#scene-query-endpoints)
- [Scene Operation Endpoints](#scene-operation-endpoints)
- [Component Operation Endpoints](#component-operation-endpoints)
- [Transform Endpoints](#transform-endpoints)
- [Light Endpoints](#light-endpoints)
- [Material Endpoints](#material-endpoints)
- [Audio Endpoints](#audio-endpoints)
- [Physics Endpoints](#physics-endpoints)
- [Animation Endpoints](#animation-endpoints)
- [Particle Endpoints](#particle-endpoints)
- [Tag & Layer Endpoints](#tag--layer-endpoints)
- [Prefab Endpoints](#prefab-endpoints)
- [Script Endpoints](#script-endpoints)
- [Screenshot Endpoints](#screenshot-endpoints)
- [Editor State Endpoints](#editor-state-endpoints)
- [Settings Endpoints](#settings-endpoints)
- [Unified Command Endpoint](#unified-command-endpoint)
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

Get complete scene hierarchy with all GameObjects.

**Query Parameters:**
- `depth` (int): Max hierarchy depth (default: unlimited)
- `format` (string): `names_only` for lightweight response
- `limit` (int): Max objects to return

**Response:**
```json
{
  "status": "success",
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
- `{name}`: GameObject name or hierarchy path (e.g., `Environment/Lights/Light_01`)

### GET /unity/scene/info

Get current scene metadata.

### GET /unity/project/scripts

List all available MonoBehaviour scripts in the project.

### GET /unity/components/list

List all available component types.

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

### POST /unity/scene/create

Create a new GameObject.

**Request Body:**
```json
{
  "name": "NewObject",
  "parent": "ParentName",
  "position": {"x": 0, "y": 5, "z": 10},
  "rotation": {"x": 0, "y": 0, "z": 0},
  "scale": {"x": 1, "y": 1, "z": 1},
  "components": ["Light", "AudioSource"],
  "color": {"r": 1.0, "g": 0.5, "b": 0.0, "a": 1.0}
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

### POST /unity/scene/delete

Delete GameObjects.

**Request Body:**
```json
{
  "objects": ["TempObject", "DebugCube"]
}
```

### POST /unity/scene/find_and_modify

Find objects and apply operations in one call.

**Request Body:**
```json
{
  "parent": "viale",
  "filter": {"component": "Light"},
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
- `set_active`: Set active state
- `delete`: Delete the GameObject

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

## Transform Endpoints

### POST /unity/transform/modify

Modify transform of existing objects (position, rotation, scale).

**Request Body:**
```json
{
  "objects": ["Player", "Enemy"],
  "position": {"x": 0, "y": 5, "z": 10},
  "rotation": {"x": 0, "y": 90, "z": 0},
  "scale": {"x": 2, "y": 2, "z": 2},
  "useLocalSpace": false,
  "positionMask": {"x": 1, "y": 1, "z": 0},
  "rotationMask": {"x": 0, "y": 1, "z": 0},
  "scaleMask": {"x": 1, "y": 1, "z": 1}
}
```

**Properties:**
- `position`: Target position (x, y, z)
- `rotation`: Target rotation in Euler angles
- `scale`: Target scale
- `useLocalSpace`: Use local vs world coordinates
- `positionMask/rotationMask/scaleMask`: 1 = apply, 0 = keep current

---

## Light Endpoints

### POST /unity/light/modify

Modify Light component properties.

**Request Body:**
```json
{
  "objects": ["DirectionalLight", "SpotLight_01"],
  "color": {"r": 1.0, "g": 0.9, "b": 0.8, "a": 1.0},
  "intensity": 2.5,
  "range": 15.0,
  "spotAngle": 45.0,
  "shadows": "soft",
  "lightType": "spot",
  "enabled": true
}
```

**Properties:**
- `color`: RGBA color (0-1 range)
- `intensity`: Light intensity
- `range`: Range for Point/Spot lights
- `spotAngle`: Angle for Spot lights
- `shadows`: `"none"`, `"hard"`, `"soft"`
- `lightType`: `"spot"`, `"point"`, `"directional"`
- `enabled`: Enable/disable light

---

## Material Endpoints

### POST /unity/material/modify

Modify material properties (URP compatible).

**Request Body:**
```json
{
  "objects": ["Cube", "Sphere"],
  "color": {"r": 1.0, "g": 0.0, "b": 0.0, "a": 1.0},
  "metallic": 0.8,
  "smoothness": 0.5,
  "emission": {"r": 1.0, "g": 0.5, "b": 0.0, "a": 1.0},
  "mainTexture": "Assets/Textures/Brick.png",
  "normalMap": "Assets/Textures/Brick_Normal.png",
  "maskMap": "Assets/Textures/Brick_Mask.png"
}
```

### POST /unity/material/assign

Assign existing material from Assets to objects.

**Request Body:**
```json
{
  "objects": ["Cube", "Sphere"],
  "materialPath": "Assets/Materials/Red.mat"
}
```

---

## Audio Endpoints

### POST /unity/audio/play

Play audio clip on object (adds AudioSource if needed).

**Request Body:**
```json
{
  "objectName": "Player",
  "clipPath": "Assets/Audio/jump.wav",
  "volume": 1.0,
  "pitch": 1.0,
  "loop": false
}
```

### POST /unity/audio/stop

Stop audio on object.

**Request Body:**
```json
{
  "objectName": "Player"
}
```

### POST /unity/audio/modify

Modify AudioSource properties.

**Request Body:**
```json
{
  "objects": ["MusicPlayer", "SFXPlayer"],
  "volume": 0.8,
  "pitch": 1.2,
  "spatialBlend": 1.0,
  "minDistance": 1.0,
  "maxDistance": 50.0,
  "loop": true,
  "mute": false,
  "playOnAwake": false
}
```

---

## Physics Endpoints

### POST /unity/physics/simulate

Simulate physics for specified duration.

**Request Body:**
```json
{
  "seconds": 2.0,
  "stepSize": 0.02
}
```

### POST /unity/physics/step

Step physics by N frames.

**Request Body:**
```json
{
  "steps": 5,
  "deltaTime": 0.02
}
```

### POST /unity/physics/raycast

Cast a ray and return hit information.

**Request Body:**
```json
{
  "origin": {"x": 0, "y": 10, "z": 0},
  "direction": {"x": 0, "y": -1, "z": 0},
  "maxDistance": 100.0,
  "layerMask": -1
}
```

**Response:**
```json
{
  "status": "success",
  "message": "Raycast hit 'Ground' at distance 10.00",
  "data": {
    "affected_objects": ["Ground"],
    "count": 1
  }
}
```

### POST /unity/physics/gravity

Set physics gravity.

**Request Body:**
```json
{
  "gravity": {"x": 0, "y": -9.81, "z": 0}
}
```

---

## Animation Endpoints

### POST /unity/animation/play

Play animation on objects.

**Request Body:**
```json
{
  "objects": ["Character"],
  "clipName": "Walk",
  "stateName": "Walking",
  "triggerName": "StartWalk",
  "layer": 0
}
```

**Properties:**
- `clipName`: For legacy Animation component
- `stateName`: For Animator state name
- `triggerName`: For Animator trigger

### POST /unity/animation/stop

Stop animation on objects.

**Request Body:**
```json
{
  "objects": ["Character"],
  "pause": false
}
```

### GET /unity/animator/info/{objectName}

Get Animator current state info.

**Response:**
```json
{
  "status": "success",
  "message": "Animator on 'Player': clip='Run', speed=1",
  "data": { "count": 1 }
}
```

### GET /unity/animator/parameters/{objectName}

List all Animator parameters with current values.

**Response:**
```json
{
  "status": "success",
  "data": {
    "animator_parameters": [
      { "name": "Speed", "type": "Float", "floatValue": 5.0 },
      { "name": "IsRunning", "type": "Bool", "boolValue": true },
      { "name": "Jump", "type": "Trigger" }
    ],
    "count": 3
  }
}
```

### POST /unity/animator/set

Set Animator parameter value (UPDATE).

**Request Body:**
```json
{
  "objects": ["Character"],
  "parameterName": "Speed",
  "parameterType": "float",
  "floatValue": 5.0
}
```

**Parameter Types:** `"float"`, `"int"`, `"bool"`, `"trigger"`

### POST /unity/animator/parameter/add

Add parameter to AnimatorController (CREATE).

**Request Body:**
```json
{
  "objects": ["Character"],
  "parameterName": "IsJumping",
  "parameterType": "bool",
  "boolValue": false
}
```

### POST /unity/animator/parameter/remove

Remove parameter from AnimatorController (DELETE).

**Request Body:**
```json
{
  "objects": ["Character"],
  "parameterName": "ObsoleteParam"
}
```

### POST /unity/animator/crossfade

Crossfade to animation state with smooth transition.

**Request Body:**
```json
{
  "objects": ["Character"],
  "stateName": "Run",
  "transitionDuration": 0.25,
  "layer": 0
}
```

### POST /unity/animator/speed

Set Animator playback speed.

**Request Body:**
```json
{
  "objects": ["Character"],
  "speed": 1.5
}
```

### POST /unity/animator/trigger/reset

Reset a trigger parameter.

**Request Body:**
```json
{
  "objects": ["Character"],
  "triggerName": "Jump"
}
```

---

## Particle Endpoints

### POST /unity/particles/play

Play particle system on objects.

**Request Body:**
```json
{
  "objects": ["Explosion", "Smoke"],
  "withChildren": true
}
```

### POST /unity/particles/stop

Stop particle system on objects.

**Request Body:**
```json
{
  "objects": ["Explosion"],
  "withChildren": true,
  "clear": false
}
```

### POST /unity/particles/emit

Emit particles instantly.

**Request Body:**
```json
{
  "objects": ["Sparks"],
  "count": 50
}
```

### POST /unity/particles/modify

Modify particle system properties.

**Request Body:**
```json
{
  "objects": ["Fire"],
  "duration": 5.0,
  "startLifetime": 2.0,
  "startSpeed": 5.0,
  "startSize": 0.5,
  "startColor": {"r": 1.0, "g": 0.5, "b": 0.0, "a": 1.0},
  "maxParticles": 1000,
  "loop": true,
  "emissionRate": 50.0
}
```

---

## Tag & Layer Endpoints

### GET /unity/tag/list

Get all available tags.

### POST /unity/tag/create

Create a new tag.

**Request Body:**
```json
{
  "name": "Enemy"
}
```

### POST /unity/tag/assign

Assign tag to objects.

**Request Body:**
```json
{
  "objects": ["Monster_01", "Monster_02"],
  "tag": "Enemy"
}
```

### POST /unity/layer/assign

Assign layer to objects.

**Request Body:**
```json
{
  "objects": ["Player"],
  "layer": 8,
  "layerName": "PlayerLayer"
}
```

---

## Prefab Endpoints

### POST /unity/prefab/instantiate

Instantiate a prefab from Assets.

**Request Body:**
```json
{
  "prefabPath": "Assets/Prefabs/Enemy.prefab",
  "name": "Enemy_01",
  "parent": "Enemies",
  "position": {"x": 10, "y": 0, "z": 5},
  "rotation": {"x": 0, "y": 180, "z": 0},
  "scale": {"x": 1, "y": 1, "z": 1}
}
```

---

## Script Endpoints

### POST /unity/script/create

Create a new C# MonoBehaviour script.

**Request Body:**
```json
{
  "name": "PlayerController",
  "path": "Assets/Scripts",
  "methods": ["Start", "Update", "OnTriggerEnter"]
}
```

---

## Screenshot Endpoints

### POST /unity/screenshot/capture

Capture Game View screenshot.

**Request Body:**
```json
{
  "filename": "screenshot.png",
  "path": "C:/Screenshots",
  "width": 1920,
  "height": 1080,
  "superSize": 2
}
```

### POST /unity/screenshot/camera

Capture from specific camera.

**Request Body:**
```json
{
  "cameraName": "SecurityCamera",
  "filename": "security_view.png",
  "path": "C:/Screenshots",
  "width": 1920,
  "height": 1080
}
```

### POST /unity/screenshot/scene

Capture Scene View screenshot.

**Request Body:**
```json
{
  "filename": "scene_view.png",
  "width": 1920,
  "height": 1080
}
```

---

## Editor State Endpoints

### GET /unity/editor/state

Get current editor state (playing, paused, compiling).

**Response:**
```json
{
  "status": "success",
  "data": {
    "editor_state": {
      "is_playing": false,
      "is_paused": false,
      "is_compiling": false,
      "last_compilation_result": "success",
      "has_errors": false,
      "has_warnings": false
    }
  }
}
```

### GET /unity/editor/console

Get console log messages.

**Query Parameters:**
- `type`: Filter by type (`error`, `warning`, `info`)
- `limit`: Max entries (default: 50)

### GET /unity/editor/console/errors

Get only console errors.

### GET /unity/editor/compilation

Get compilation status.

### GET /unity/editor/wait_compilation

Wait for compilation to complete (blocking).

**Query Parameters:**
- `timeout`: Seconds to wait (default: 30)

### POST /unity/editor/refresh

Refresh assets and request script recompilation.

### POST /unity/editor/recompile

Request only script recompilation.

### POST /unity/editor/console/clear

Clear cached console logs.

### POST /unity/editor/play

Enter Play Mode.

### POST /unity/editor/stop

Exit Play Mode (return to Edit Mode).

### POST /unity/editor/pause

Pause/Resume Play Mode.

### POST /unity/editor/step

Step one frame (while paused in Play Mode).

---

## Settings Endpoints

### GET /unity/settings/{category}

Read project settings.

**Categories:** `player`, `quality`, `physics`, `audio`, `graphics`

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

## Unified Command Endpoint

### POST /unity/command

Execute Unix-like commands in a single request.

**Request Body:**
```json
{
  "cmd": "create cube MyCube --position 0 5 0 --color red"
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
  }
}
```

### Partial Success Example

```json
{
  "status": "partial",
  "message": "Modified 3 objects with 2 errors",
  "data": {
    "affected_objects": ["Light_01", "Light_02", "Light_03"],
    "count": 3,
    "errors": [
      "Light_04: Component not found",
      "Light_05: GameObject not found"
    ]
  }
}
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
6. **Wait for compilation**: After creating scripts, use `/unity/editor/wait_compilation`

---

For more information, see the [README](../README.md).
