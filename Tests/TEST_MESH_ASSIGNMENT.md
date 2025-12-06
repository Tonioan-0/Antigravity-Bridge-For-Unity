# Test Script - Create Cube with Full Components

## Step 1: Wait for Unity to Recompile
Unity should automatically recompile the updated `CommandExecutor.cs` file.
Check the Unity Console - you should see "Compilation finished" or similar.

## Step 2: Create Test Cube
Run this command:

```powershell
python unity_bridge.py primitive cube --name "TestCubeWithLogging" --y 3
```

## Step 3: Check Unity Console
After running the command, check the Unity Console for these logs:

**Expected logs:**
```
[AntigravityBridge] SetupPrimitiveMeshAndMaterial called for: 'TestCubeWithLogging'
[AntigravityBridge] MeshFilter=True, MeshRenderer=True
Assigned Cube mesh to TestCubeWithLogging
Assigned default material to TestCubeWithLogging
```

## Step 4: Inspect the GameObject in Unity

Select "TestCubeWithLogging" in the Hierarchy and check:
- ✅ Transform Scale should be (1, 1, 1)
- ✅ MeshFilter should have "Cube" mesh
- ✅ MeshRenderer should have a material assigned
- ✅ BoxCollider should be present
- ✅ Object should be VISIBLE in Scene view

## Possible Issues & Solutions

### If NO logs appear:
**Problem**: Unity hasn't recompiled or the function isn't being called
**Solution**: 
1. Check Unity Console for compilation errors
2. Force recompile: Right-click in Project panel → Reimport All
3. Restart Unity if needed

### If logs show "MeshFilter=False":
**Problem**: MeshFilter component wasn't added
**Solution**: The Python script should include "MeshFilter" in components array - check `unity_bridge.py`

### If logs show "MeshFilter=True" but mesh NOT assigned:
**Problem**: GetPrimitiveMesh() failing
**Solution**: Check Unity Console for errors in GetPrimitiveMesh function

### If logs show material NOT assigned:
**Problem**: Material creation failing
**Solution**: Check if "Standard" shader exists (should be built-in Unity shader)

## Manual Verification

If automated creation isn't working, try creating manually in Unity:
1. GameObject → 3D Object → Cube
2. Compare with API-created object
3. Note any differences

## Debug Command

If you need more  detailed debugging, you can modify the script to show exactly what's happening:

```powershell
# This will show all debug output
python unity_bridge.py primitive cube --name "DebugCube" --y 1 2>&1 | Out-File debug_output.txt
```

Then check `debug_output.txt` for full output.
