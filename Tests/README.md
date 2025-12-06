# Antigravity Unity Bridge - Tests

This folder contains test scripts, data, and instructions for verifying the Antigravity Unity Bridge.

## Files

- **test_api.ps1**: PowerShell script that sends various HTTP requests to the Unity Bridge server to test endpoints (Create, Find, Modify, etc.).
- **TEST_MESH_ASSIGNMENT.md**: Instructions for manually verifying mesh and material assignment using the `unity_bridge.py` tool.
- ***.json**: JSON payloads used for testing specific API endpoints (Create, Find, etc.).

## How to Run Tests

### 1. API Tests
Ensure Unity is running and the Antigravity Bridge server is started (Play Mode in Unity).
Run the PowerShell script from this folder or root:
```powershell
./test_api.ps1
```

### 2. Mesh Assignment Test
Follow the instructions in `TEST_MESH_ASSIGNMENT.md`.
**Note:** Commands in the instructions assume you are running from the **repository root** where `unity_bridge.py` is located.
