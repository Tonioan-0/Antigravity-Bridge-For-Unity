# Changelog

All notable changes to the Antigravity Unity Bridge package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.0] - 2024-12-05

### Added

#### Editor State API (AI Awareness)
- GET /unity/editor/state - Editor state (playing, paused, compiling, has_errors)
- GET /unity/editor/console - Console logs with filtering (?type=error&limit=50)
- GET /unity/editor/console/errors - Quick access to errors only
- GET /unity/editor/compilation - Compilation status
- GET /unity/editor/wait_compilation - Block until compilation finishes (?timeout=30)

#### Output Optimization
- Query parameter `?format=names_only` for minimal output (just object names)
- Query parameter `?format=exists_only` for existence checks
- Query parameter `?depth=N` for hierarchy depth control (0=root only)
- Query parameter `?select=field1,field2` for field selection
- Query parameter `?precision=N` for transform rounding (default: 3 decimals)

#### Models
- EditorStateModels.cs - Data structures for editor state, console logs, compilation
- QueryOptions - Model for query parameters (select, depth, format, precision)

#### Unix-like Command Architecture
- POST /unity/command - Unified endpoint accepting command strings
- QueryBuilder - Fluent API for building find queries
- CommandBuilder - Fluent API for create/modify/delete operations
- CommandParser - Parses Unix-like command strings

**Command Examples:**
```bash
find . --component Light --format names_only
create MyCube --position 0,1,0 --components BoxCollider,Rigidbody
modify Player --add AudioSource --set active=false
delete TempObject --force
get MainCamera --select components
```

#### Game Element Control API
- POST /unity/light/modify - Modify light color, intensity, shadows, range
- POST /unity/material/modify - Modify URP material properties (color, metallic, smoothness, emission)
- POST /unity/material/assign - Assign existing material from Assets
- POST /unity/audio/play - Play audio clip on object
- POST /unity/audio/stop - Stop audio playback
- POST /unity/audio/modify - Modify AudioSource properties
- POST /unity/tag/create - Create new tag
- POST /unity/tag/assign - Assign tag to objects
- GET /unity/tag/list - Get all available tags
- POST /unity/layer/assign - Assign layer to objects
- POST /unity/script/create - Create C# MonoBehaviour script

**Python Client Commands:**
```bash
# Light control
python unity_bridge.py light "Spot Light" --color red --intensity 3

# Material control
python unity_bridge.py material "Cube" --color blue --metallic 0.8

# Audio control  
python unity_bridge.py audio play "Player" --clip "Assets/Audio/jump.wav"

# Tag management
python unity_bridge.py tag create --name Enemy
python unity_bridge.py tag assign --name Enemy --objects Cube Sphere

# Script creation
python unity_bridge.py script EnemyAI --path Assets/Scripts --methods Start Update
```

### Changed
- Position/rotation/scale now rounded to 3 decimal places by default
- SceneQueryAPI methods now accept QueryOptions for output control
- AntigravityServer routes parse query parameters for v2 features

### Technical Details
- Console log capture via Application.logMessageReceived
- Thread-safe log storage with 100 entry limit
- Backward compatible - existing endpoints work without changes

---

## [1.0.0] - 2024-01-15

### Added

#### Core Features
- HTTP REST API server running inside Unity Editor
- Thread-safe command queue with background HTTP listener
- Main-thread command execution via EditorApplication.update
- Complete Undo support for all operations
- Automatic scene dirty marking

#### Editor Window
- Server start/stop controls
- Port configuration
- Real-time status monitoring
- Command log (last 50 commands) with color coding
- Statistics tracking (success rate, error count)
- Connection testing
- Auto-start option

#### Scene Query API
- GET /unity/scene/hierarchy - Complete scene structure
- GET /unity/scene/objects/{name} - GameObject details
- GET /unity/scene/info - Scene metadata
- GET /unity/project/scripts - Available MonoBehaviour scripts
- GET /unity/components/list - Available component types

#### Scene Operation API
- POST /unity/scene/find - Find GameObjects by criteria
- POST /unity/scene/create - Create new GameObjects
- POST /unity/scene/modify - Modify GameObject properties
- POST /unity/scene/delete - Delete GameObjects
- POST /unity/scene/find_and_modify - Combined find and modify operation

#### Component Operation API
- POST /unity/component/add - Add components to GameObjects
- POST /unity/component/remove - Remove components
- POST /unity/component/modify - Modify component properties via reflection

#### Settings API
- GET /unity/settings/{category} - Read project settings
- POST /unity/settings/{category} - Modify project settings
- Support for Player, Quality, Physics, Audio, Graphics settings

#### Status API
- GET /unity/status - Server status and statistics
- GET /unity/health - Health check

#### Documentation
- Comprehensive README with installation instructions
- Complete API documentation with examples
- Example workflows and use cases
- Troubleshooting guide
- Architecture documentation

#### Security
- Localhost-only connections
- Editor-only compilation (excluded from builds)
- Script name validation
- Batch operation limits (max 1000 objects)
- Input sanitization

### Technical Details

- **Unity Version**: 2020.3+
- **Package Type**: Editor-only UPM package
- **Assembly Definition**: Separate editor assembly
- **Thread Model**: Background listener + main thread execution
- **JSON Serialization**: Unity JsonUtility
- **HTTP Server**: System.Net.HttpListener

### Known Limitations

- Single Unity instance support only
- No authentication (localhost only)
- No HTTPS support
- No multi-scene editing support (active scene only)
- Maximum 1000 objects per batch operation

## [Unreleased]

### Planned Features
- Authentication tokens for remote access
- HTTPS support
- Multi-scene editing support
- Prefab editing support
- Asset import/export operations
- Build automation API
- Play mode control API
- Animation and timeline manipulation
- Custom serialization for complex types

---

[1.0.0]: https://github.com/yourusername/com.antigravity.unity-bridge/releases/tag/v1.0.0
