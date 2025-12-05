# Antigravity Unity Bridge

**HTTP REST API bridge that allows Antigravity AI IDE to control and manipulate Unity Editor scenes through natural language commands.**

## Overview

The Antigravity Unity Bridge is a Unity Package Manager (UPM) package that runs an HTTP server inside Unity Editor, enabling external tools (specifically Antigravity AI) to control scene editing operations via REST API calls. This allows for natural language commands like *"find all lights in the 'viale' object and add the ActivateDisableLights script to them"* to be executed automatically in Unity.

### Key Features

✅ **HTTP REST API** - Complete API for scene manipulation  
✅ **Natural Language Support** - Designed for AI-driven commands  
✅ **Thread-Safe** - Background HTTP listener with main-thread execution  
✅ **Editor Window** - User-friendly UI for server control and monitoring  
✅ **Undo Support** - All operations support Unity's Undo system  
✅ **Scene Query** - Read scene hierarchy, object details, available scripts  
✅ **Object Manipulation** - Create, modify, delete GameObjects  
✅ **Component Management** - Add, remove, modify components  
✅ **Settings Access** - Read and modify project settings  
✅ **Command Logging** - Real-time command log with statistics  

## Installation

### Method 1: Local Package Installation

1. Download or clone this package to your computer
2. In Unity, open `Window > Package Manager`
3. Click the `+` button in the top-left
4. Select `Add package from disk...`
5. Browse to this folder and select `package.json`
6. Unity will install the package

### Method 2: Manual Package Directory

1. Copy this entire folder to the `Packages/` directory in your Unity project
2. Unity will automatically detect and load it

### Method 3: Git URL (if hosted on GitHub)

1. In Unity, open `Window > Package Manager`
2. Click `+` > `Add package from git URL...`
3. Enter: `https://github.com/yourusername/com.antigravity.unity-bridge.git`

## Quick Start

### 1. Open the Antigravity Bridge Window

After installation, go to `Window > Antigravity Bridge` in Unity Editor.

### 2. Start the Server

1. Configure the port (default: 8080)
2. Click **Start Server**
3. Server status will show "RUNNING" in green

### 3. Test the Connection

Click **Test Connection (Ping)** to verify the server is working.

### 4. Send Your First Command

Using curl, PowerShell, or Antigravity, send a simple query:

```bash
curl http://localhost:8080/unity/status
```

Response:
```json
{
  "status": "success",
  "message": "Server is running",
  "data": {
    "server_status": {
      "is_running": true,
      "port": 8080,
      "unity_version": "2022.3.10f1",
      "editor_mode": "Edit"
    }
  },
  "timestamp": "2024-01-15T10:30:00.000Z"
}
```

## Example Use Cases

### Find All Lights and Add Script

**Request:**
```bash
curl -X POST http://localhost:8080/unity/scene/find_and_modify \
  -H "Content-Type: application/json" \
  -d '{
    "parent": "viale",
    "filter": {
      "component": "Light"
    },
    "operations": [{
      "type": "add_component",
      "component": "ActivateDisableLights"
    }]
  }'
```

**Response:**
```json
{
  "status": "success",
  "message": "Modified 5 objects",
  "data": {
    "affected_objects": ["Light_01", "Light_02", "Light_03", "Light_04", "Light_05"],
    "count": 5
  }
}
```

### Get Scene Hierarchy

**Request:**
```bash
curl http://localhost:8080/unity/scene/hierarchy
```

**Response:**
```json
{
  "status": "success",
  "data": {
    "scene_hierarchy": {
      "scene_name": "MainScene",
      "root_objects": [
        {
          "name": "viale",
          "active": true,
          "components": ["Transform"],
          "children": [
            {
              "name": "Light_01",
              "components": ["Transform", "Light"],
              "position": {"x": 0, "y": 5, "z": 0}
            }
          ]
        }
      ]
    }
  }
}
```

### Create a GameObject

**Request:**
```bash
curl -X POST http://localhost:8080/unity/scene/create \
  -H "Content-Type: application/json" \
  -d '{
    "name": "NewCamera",
    "parent": "Cameras",
    "position": {"x": 0, "y": 10, "z": -10},
    "components": ["Camera", "AudioListener"]
  }'
```

## API Endpoints

### Status & Health
- `GET /unity/status` - Server status
- `GET /unity/health` - Detailed health check

### Scene Queries
- `GET /unity/scene/hierarchy` - Complete scene hierarchy
- `GET /unity/scene/objects/{name}` - GameObject details
- `GET /unity/scene/info` - Scene metadata
- `GET /unity/project/scripts` - Available scripts
- `GET /unity/components/list` - Available components

### Scene Operations
- `POST /unity/scene/find` - Find GameObjects
- `POST /unity/scene/create` - Create GameObject
- `POST /unity/scene/modify` - Modify GameObject properties
- `POST /unity/scene/delete` - Delete GameObject
- `POST /unity/scene/find_and_modify` - Find and modify in one operation

### Component Operations
- `POST /unity/component/add` - Add component
- `POST /unity/component/remove` - Remove component
- `POST /unity/component/modify` - Modify component properties

### Settings
- `GET /unity/settings/{category}` - Read settings
- `POST /unity/settings/{category}` - Modify settings

For complete API documentation, see [API_DOCUMENTATION.md](Documentation~/API_DOCUMENTATION.md).

## Security

- **Localhost Only**: Server only accepts connections from `localhost` / `127.0.0.1`
- **Editor Only**: Package code only compiles in Unity Editor (not in builds)
- **Script Validation**: Component names are validated before adding
- **Batch Limits**: Maximum 1000 objects per batch operation

> ⚠️ **Warning**: This server is designed for local development only. Do not expose it to external networks.

## Troubleshooting

### Server won't start

- **Check if port is in use**: Another application might be using port 8080
- **Try a different port**: Change the port in the Editor Window
- **Check Unity Console**: Look for error messages

### Commands not executing

- **Verify server is running**: Check the Editor Window status
- **Check command syntax**: Ensure JSON is valid
- **Look at command log**: The Editor Window shows all commands with status

### "GameObject not found" errors

- **Use full hierarchy path**: Instead of "Light", use "viale/Light_01"
- **Check object is in active scene**: Objects in other scenes won't be found
- **Verify spelling**: GameObject names are case-sensitive

### Performance issues

- **Limit batch sizes**: Don't modify more than 100 objects at once
- **Check Unity Console**: Look for warnings or errors
- **Restart the server**: Use the Stop/Start buttons in Editor Window

## Requirements

- **Unity Version**: 2020.3 or higher
- **Platform**: Windows, macOS, Linux
- **Mode**: Editor only (not available in Play mode or builds)

## Architecture

### Thread Safety

The server uses a thread-safe command queue pattern:

1. **Background Thread**: HTTP listener receives requests
2. **Command Queue**: Requests are queued (thread-safe)
3. **Main Thread**: Commands execute on Unity's main thread via `EditorApplication.update`

This ensures all Unity API calls happen on the main thread, preventing threading errors.

### Undo Support

All destructive operations use Unity's Undo system:
- `Undo.AddComponent()` - Adding components
- `Undo.DestroyObjectImmediate()` - Deleting objects/components
- `Undo.RecordObject()` - Modifying objects
- `Undo.RegisterCreatedObjectUndo()` - Creating objects

Users can undo API operations with `Ctrl+Z` (Cmd+Z on Mac).

## Contributing

Contributions are welcome! Please submit issues and pull requests on GitHub.

## License

[Your License Here - e.g., MIT License]

## Credits

Created for integration with **Antigravity** - Google's AI IDE

---

**Need help?** Check the [API Documentation](Documentation~/API_DOCUMENTATION.md) or open an issue on GitHub.
