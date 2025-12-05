# Quick Start Testing Guide

## Installation Steps

1. **Open Unity Project** (any Unity 2020.3+ project)

2. **Add Package**:
   - Copy the entire `AntiUnity` folder to your Unity project's `Packages/` directory
   - OR use Package Manager: `Window > Package Manager > + > Add package from disk...` and select `package.json`

3. **Wait for Compilation**:
   - Unity should automatically compile the package
   - Check the Console for any errors

## Starting the Server

1. In Unity, go to `Window > Antigravity Bridge`

2. The Antigravity Bridge window should open with:
   - Server status (currently STOPPED)
   - Port configuration (default 8080)
   - Start Server button

3. Click **Start Server**

4. Status should change to "RUNNING" (green)

## Testing with PowerShell

Open PowerShell and test these commands:

### 1. Server Status
```powershell
curl http://localhost:8080/unity/status
```

Expected response:
```json
{
  "status": "success",
  "message": "Server is running",
  "data": {
    "server_status": {
      "is_running": true,
      "port": 8080
    }
  }
}
```

### 2. Get Scene Hierarchy
```powershell
curl http://localhost:8080/unity/scene/hierarchy
```

### 3. Get Scene Info
```powershell
curl http://localhost:8080/unity/scene/info
```

### 4. Get Available Scripts
```powershell
curl http://localhost:8080/unity/project/scripts
```

### 5. Create a Test Object
```powershell
$body = @'
{
  "name": "TestCube",
  "position": {"x": 0, "y": 1, "z": 0},
  "components": ["MeshFilter", "MeshRenderer"]
}
'@

curl -X POST http://localhost:8080/unity/scene/create `
  -H "Content-Type: application/json" `
  -Body $body
```

### 6. Test Find Operation
First, create a test hierarchy in Unity:
- Create GameObject named "viale"
- Add 3 child GameObjects with Light components

Then test:
```powershell
$body = @'
{
  "parent": "viale",
  "filter": {"component": "Light"}
}
'@

curl -X POST http://localhost:8080/unity/scene/find `
  -H "Content-Type: application/json" `
  -Body $body
```

### 7. Test Find and Add Component
```powershell
$body = @'
{
  "parent": "viale",
  "filter": {"component": "Light"},
  "operations": [
    {"type": "add_component", "component": "AudioSource"}
  ]
}
'@

curl -X POST http://localhost:8080/unity/scene/find_and_modify `
  -H "Content-Type: application/json" `
  -Body $body
```

Check Unity - all lights under "viale" should now have AudioSource components!

## Verifying in Unity

After running commands:

1. Check the **Antigravity Bridge window**:
   - Command log should show all requests
   - Statistics should update
   - Success/error counts should be accurate

2. Check the **Unity Console**:
   - Should see log messages for server start/stop
   - Any errors will appear here

3. Check the **Scene Hierarchy**:
   - Created objects should appear
   - Modified objects should have new components

4. **Test Undo**:
   - Press `Ctrl+Z` (Windows) or `Cmd+Z` (Mac)
   - Changes made via API should undo!

## Troubleshooting

### Server won't start
- Check if port 8080 is in use
- Try a different port in the Antigravity Bridge window

### "Connection refused" error
- Make sure server is running (green status in Unity)
- Check port number matches your curl command

### "GameObject not found" errors
- Make sure objects exist in the active scene
- Use full hierarchy path: "Parent/Child/Object"

### Compilation errors
- Make sure you're using Unity 2020.3 or higher
- Check Unity Console for specific error messages
- Verify all files are in the correct folders

## Success Criteria

✅ Server starts without errors  
✅ `/unity/status` returns success  
✅ Can create GameObjects via API  
✅ Can find GameObjects by component  
✅ Can add components to found objects  
✅ Undo (Ctrl+Z) works for API operations  
✅ Editor Window shows commands in real-time

## Next Steps

Once basic testing works:

1. Test with more complex scenarios
2. Try modifying component properties
3. Test project settings access
4. Integrate with Antigravity AI IDE

## Example Antigravity Integration

When Antigravity receives this command:
> "Find all lights in the viale object and add the ActivateDisableLights script"

It should translate to:
```http
POST http://localhost:8080/unity/scene/find_and_modify
Content-Type: application/json

{
  "parent": "viale",
  "filter": { "component": "Light" },
  "operations": [
    { "type": "add_component", "component": "ActivateDisableLights" }
  ]
}
```
