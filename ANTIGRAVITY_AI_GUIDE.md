# Antigravity AI - Unity Bridge Context Guide

**For Antigravity AI**: This guide provides context on how to interact with Unity through the HTTP REST API bridge.

## 🎯 Your Role as Antigravity AI

You can control Unity Editor remotely via HTTP REST API. You have two main tools:

1. **Read operations** (GET): Use `read_url_content` tool directly
2. **Write operations** (POST): Use the Python script `unity_bridge.py`

## 📋 Common Unity Primitives

When creating GameObjects, use these standard Unity primitives with their required components:

### Cube
```json
{
  "name": "MyCube",
  "position": {"x": 0, "y": 0, "z": 0},
  "components": ["MeshFilter", "MeshRenderer", "BoxCollider"]
}
```
**Use for**: Solid 3D objects, platforms, walls, blocks

### Sphere
```json
{
  "name": "MySphere",
  "position": {"x": 0, "y": 1, "z": 0},
  "components": ["MeshFilter", "MeshRenderer", "SphereCollider"]
}
```
**Use for**: Balls, planets, rounded objects

### Plane
```json
{
  "name": "Ground",
  "position": {"x": 0, "y": 0, "z": 0},
  "components": ["MeshFilter", "MeshRenderer", "MeshCollider"]
}
```
**Use for**: Floors, grounds, flat surfaces

### Cylinder
```json
{
  "name": "Pillar",
  "position": {"x": 0, "y": 0, "z": 0},
  "components": ["MeshFilter", "MeshRenderer", "CapsuleCollider"]
}
```
**Use for**: Pillars, columns, poles

### Empty GameObject (Container)
```json
{
  "name": "Container",
  "position": {"x": 0, "y": 0, "z": 0},
  "components": []
}
```
**Use for**: Organizational containers, parent objects

## 🎮 Common Component Combinations

### Interactive Object (Rigidbody + Collider)
```json
{
  "name": "PhysicsObject",
  "position": {"x": 0, "y": 5, "z": 0},
  "components": ["MeshFilter", "MeshRenderer", "BoxCollider", "Rigidbody"]
}
```

### Light Source
```json
{
  "name": "PointLight",
  "position": {"x": 0, "y": 3, "z": 0},
  "components": ["Light"]
}
```

### Camera
```json
{
  "name": "MainCamera",
  "position": {"x": 0, "y": 1, "z": -10},
  "components": ["Camera", "AudioListener"]
}
```

### UI Canvas
```json
{
  "name": "UI",
  "components": ["Canvas", "CanvasScaler", "GraphicRaycaster"]
}
```

## 🔧 Workflow Examples

### Example 1: Create a Simple Scene

**User Request**: "Create a simple scene with a ground, a cube, and a light"

**Your Actions**:
```python
# 1. Create ground
python unity_bridge.py create --name "Ground" --y 0 
# Then manually add: MeshFilter, MeshRenderer, MeshCollider

# 2. Create cube
python unity_bridge.py create --name "Cube" --y 1 
# Add: MeshFilter, MeshRenderer, BoxCollider

# 3. Create light
python unity_bridge.py create --name "Light" --y 5
# Add: Light component
```

### Example 2: Find and Modify Pattern

**User Request**: "Find all lights in the 'Environment' object and add a custom script"

**Your Actions**:
```python
# Use the find_and_modify command
python unity_bridge.py modify \
  --parent Environment \
  --component Light \
  --operation add_component \
  --target MyLightController
```

### Example 3: Query Before Acting

**Best Practice**: Always query the scene first to understand context

```python
# 1. Get scene info
python unity_bridge.py scene

# 2. Get hierarchy to see structure
python unity_bridge.py hierarchy

# 3. Check available scripts
python unity_bridge.py scripts

# 4. Then perform operations based on what you found
```

## 🎨 Naming Conventions

Follow Unity naming conventions:

- **PascalCase** for GameObject names: `PlayerController`, `MainCamera`
- **Descriptive names**: `Ground`, `PlayerSpawnPoint`, `EnemyContainer`
- **Hierarchical structure**: Use parent-child relationships
  ```
  Environment
    ├── Terrain
    ├── Trees
    └── Lights
        ├── Light_Sun
        ├── Light_Fill
        └── Light_Rim
  ```

## ⚠️ Important Constraints

1. **Batch Limit**: Maximum 1000 objects per operation
2. **Scene Validation**: Always check if objects exist before modifying
3. **Component Validation**: Verify script names exist in project before adding
4. **Thread Safety**: Operations execute on Unity's main thread automatically
5. **Undo Support**: All operations support Ctrl+Z in Unity

## 📊 Decision Tree for Operations

```
User wants to...
│
├─ CREATE object
│  ├─ Is it a primitive? → Use appropriate component set
│  ├─ Interactive? → Add Rigidbody + Collider
│  └─ Visual only? → MeshFilter + MeshRenderer
│
├─ MODIFY objects
│  ├─ Single object? → Use direct name
│  ├─ Multiple objects? → Use find_and_modify
│  └─ By criteria? → Use filter (component, tag, parent)
│
└─ QUERY scene
   ├─ Structure? → Get hierarchy
   ├─ Specific object? → Get object info by name
   └─ Available resources? → Get scripts/components list
```

## 🔍 Error Handling

If an operation fails:
1. Check Unity Console for detailed errors
2. Verify object/script names are correct (case-sensitive!)
3. Ensure Unity server is running (check status endpoint)
4. Check if operation exceeded batch limit

## 💡 Tips for Efficient Use

1. **Batch operations** when possible (find_and_modify is faster than multiple individual calls)
2. **Query first** to understand scene state
3. **Use descriptive names** for created objects
4. **Organize hierarchically** with parent containers
5. **Always include required components** for primitives

## 🚀 Advanced Patterns

### Pattern: Setup Complete Environment
```python
# 1. Create container
create Container "Environment"

# 2. Create child objects under container
create Ground with parent=Environment
create Skybox with parent=Environment
create Lights with parent=Environment

# 3. Populate lights
find_and_modify to add scripts to all lights in Environment
```

### Pattern: Clone and Modify
```python
# 1. Create template
create "EnemyTemplate" with components

# 2. Use Unity's Instantiate (via script) for multiple copies
# 3. Modify all instances via find_and_modify
```

## 📝 Template Responses

When user asks to create objects, respond with:
- ✅ What you're creating
- ✅ What components will be added
- ✅ Where it will be positioned
- ✅ Confirmation of execution

Example:
> "I'll create a cube named 'PlayerBlock' at position (0, 1, 0) with MeshFilter, MeshRenderer, and BoxCollider components for physics interaction."

---

**Remember**: You're controlling a live Unity Editor. Changes are immediate and visible. Always confirm before destructive operations (delete, batch modify).
