# Changelog

All notable changes to the Antigravity Unity Bridge package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
