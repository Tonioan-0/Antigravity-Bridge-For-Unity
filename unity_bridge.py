#!/usr/bin/env python3
"""
Antigravity Unity Bridge - Enhanced Python Client with Primitives Support
This script allows Antigravity to send commands to Unity via HTTP REST API
Includes built-in support for Unity primitives with default components
"""

import requests
import json
import sys

UNITY_BASE_URL = "http://localhost:8080"

# Unity Primitive Templates with Required Components
PRIMITIVES = {
    "cube": {
        "components": ["MeshFilter", "MeshRenderer", "BoxCollider"],
        "description": "3D Cube with mesh and box collider"
    },
    "sphere": {
        "components": ["MeshFilter", "MeshRenderer", "SphereCollider"],
        "description": "3D Sphere with mesh and sphere collider"
    },
    "plane": {
        "components": ["MeshFilter", "MeshRenderer", "MeshCollider"],
        "description": "Flat plane with mesh collider"
    },
    "cylinder": {
        "components": ["MeshFilter", "MeshRenderer", "CapsuleCollider"],
        "description": "Cylinder with capsule collider"
    },
    "capsule": {
        "components": ["MeshFilter", "MeshRenderer", "CapsuleCollider"],
        "description": "Capsule with collider"
    },
    "quad": {
        "components": ["MeshFilter", "MeshRenderer"],
        "description": "2D Quad (flat square)"
    },
    "empty": {
        "components": [],
        "description": "Empty GameObject (container)"
    },
    "light": {
        "components": ["Light"],
        "description": "Light source"
    },
    "camera": {
        "components": ["Camera", "AudioListener"],
        "description": "Camera with audio listener"
    },
    "canvas": {
        "components": ["Canvas", "CanvasScaler", "GraphicRaycaster"],
        "description": "UI Canvas"
    }
}

def print_json(data):
    """Pretty print JSON response"""
    print(json.dumps(data, indent=2))

def send_get_request(endpoint):
    """Send GET request to Unity"""
    url = f"{UNITY_BASE_URL}{endpoint}"
    try:
        response = requests.get(url, timeout=5)
        print(f"✓ Status: {response.status_code}")
        result = response.json()
        print_json(result)
        return result
    except requests.exceptions.Timeout:
        print("✗ Error: Request timeout (Unity server not responding)")
        return None
    except requests.exceptions.ConnectionError:
        print("✗ Error: Cannot connect to Unity server")
        print("  Make sure Unity is running and Antigravity Bridge is started")
        return None
    except Exception as e:
        print(f"✗ Error: {e}")
        return None

def send_post_request(endpoint, data):
    """Send POST request to Unity"""
    url = f"{UNITY_BASE_URL}{endpoint}"
    headers = {"Content-Type": "application/json"}
    try:
        response = requests.post(url, json=data, headers=headers, timeout=10)
        print(f"✓ Status: {response.status_code}")
        result = response.json()
        print_json(result)
        return result
    except Exception as e:
        print(f"✗ Error: {e}")
        return None

def create_primitive(primitive_type, name, x=0, y=0, z=0, add_physics=False):
    """Create a Unity primitive GameObject with default components"""
    
    if primitive_type not in PRIMITIVES:
        print(f"✗ Unknown primitive type: {primitive_type}")
        print(f"Available primitives: {', '.join(PRIMITIVES.keys())}")
        return None
    
    template = PRIMITIVES[primitive_type]
    components = template["components"].copy()
    
    # Add Rigidbody for physics
    if add_physics and primitive_type not in ["empty", "light", "camera", "canvas"]:
        components.append("Rigidbody")
    
    data = {
        "name": name,
        "position": {"x": x, "y": y, "z": z},
        "components": components
    }
    
    print(f"Creating {primitive_type}: {name}")
    print(f"Position: ({x}, {y}, {z})")
    print(f"Components: {', '.join(components)}")
    
    return send_post_request("/unity/scene/create", data)

def create_gameobject(name, x=0, y=0, z=0, components=None, parent=None):
    """Create a custom GameObject"""
    data = {
        "name": name,
        "position": {"x": x, "y": y, "z": z}
    }
    
    if components:
        data["components"] = components
    
    if parent:
        data["parent"] = parent
    
    return send_post_request("/unity/scene/create", data)

def find_objects(component=None, parent=None, tag=None):
    """Find GameObjects in scene"""
    data = {"filter": {}}
    
    if component:
        data["filter"]["component"] = component
    if parent:
        data["parent"] = parent
    if tag:
        data["filter"]["type"] = "tag"
        data["filter"]["value"] = tag
    
    return send_post_request("/unity/scene/find", data)

def find_and_modify(parent, component, operation_type, operation_component):
    """Find objects and apply operations - THE MAIN ANTIGRAVITY COMMAND"""
    data = {
        "parent": parent,
        "filter": {"component": component},
        "operations": [{
            "type": operation_type,
            "component": operation_component
        }]
    }
    
    print(f"Finding {component}s in '{parent}'...")
    print(f"Operation: {operation_type} → {operation_component}")
    
    return send_post_request("/unity/scene/find_and_modify", data)

def delete_objects(names):
    """Delete GameObjects by name"""
    data = {"objects": names}
    return send_post_request("/unity/scene/delete", data)

def list_primitives():
    """List all available primitive types"""
    print("\n=== Unity Primitives ===\n")
    for prim_type, info in PRIMITIVES.items():
        print(f"{prim_type:12} - {info['description']}")
        print(f"{'':12}   Components: {', '.join(info['components']) if info['components'] else 'none'}")
        print()

# Query functions
def get_scene_info():
    """Get current scene information"""
    return send_get_request("/unity/scene/info")

def get_hierarchy():
    """Get scene hierarchy"""
    return send_get_request("/unity/scene/hierarchy")

def get_status():
    """Get server status"""
    return send_get_request("/unity/status")

def get_available_scripts():
    """Get list of available scripts"""
    return send_get_request("/unity/project/scripts")

# Command-line interface
if __name__ == "__main__":
    import argparse
    
    parser = argparse.ArgumentParser(
        description="Antigravity Unity Bridge Client",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  # Create a cube
  python unity_bridge.py primitive cube --name MyCube --y 2
  
  # Create a sphere with physics
  python unity_bridge.py primitive sphere --name Ball --y 5 --physics
  
  # Create custom object
  python unity_bridge.py create --name Player --x 0 --y 1 --z 0
  
  # Find all lights
  python unity_bridge.py find --component Light
  
  # Add script to all lights in 'viale'
  python unity_bridge.py modify --parent viale --component Light --operation add_component --target AudioSource
        """
    )
    
    subparsers = parser.add_subparsers(dest="command", help="Command to execute")
    
    # Primitive command
    prim_parser = subparsers.add_parser("primitive", help="Create Unity primitive")
    prim_parser.add_argument("type", choices=PRIMITIVES.keys(), help="Primitive type")
    prim_parser.add_argument("--name", required=True, help="GameObject name")
    prim_parser.add_argument("--x", type=float, default=0, help="X position")
    prim_parser.add_argument("--y", type=float, default=0, help="Y position")
    prim_parser.add_argument("--z", type=float, default=0, help="Z position")
    prim_parser.add_argument("--physics", action="store_true", help="Add Rigidbody for physics")
    
    # Create command
    create_parser = subparsers.add_parser("create", help="Create custom GameObject")
    create_parser.add_argument("--name", required=True, help="GameObject name")
    create_parser.add_argument("--x", type=float, default=0, help="X position")
    create_parser.add_argument("--y", type=float, default=0, help="Y position")
    create_parser.add_argument("--z", type=float, default=0, help="Z position")
    create_parser.add_argument("--parent", help="Parent GameObject")
    
    # Find command
    find_parser = subparsers.add_parser("find", help="Find GameObjects")
    find_parser.add_argument("--component", help="Component type")
    find_parser.add_argument("--parent", help="Parent GameObject name")
    find_parser.add_argument("--tag", help="Tag filter")
    
    # Modify command
    mod_parser = subparsers.add_parser("modify", help="Find and modify objects")
    mod_parser.add_argument("--parent", required=True, help="Parent GameObject")
    mod_parser.add_argument("--component", required=True, help="Component filter")
    mod_parser.add_argument("--operation", required=True, choices=["add_component", "remove_component", "set_active", "delete"])
    mod_parser.add_argument("--target", help="Target component/value")
    
    # Delete command
    del_parser = subparsers.add_parser("delete", help="Delete GameObjects")
    del_parser.add_argument("names", nargs="+", help="GameObject names to delete")
    
    # Query commands
    subparsers.add_parser("status", help="Get server status")
    subparsers.add_parser("scene", help="Get scene info")
    subparsers.add_parser("hierarchy", help="Get scene hierarchy")
    subparsers.add_parser("scripts", help="Get available scripts")
    subparsers.add_parser("list", help="List available primitives")
    
    args = parser.parse_args()
    
    if not args.command:
        parser.print_help()
        sys.exit(1)
    
    # Execute command
    if args.command == "primitive":
        create_primitive(args.type, args.name, args.x, args.y, args.z, args.physics)
    elif args.command == "create":
        create_gameobject(args.name, args.x, args.y, args.z, parent=args.parent)
    elif args.command == "find":
        find_objects(args.component, args.parent, args.tag)
    elif args.command == "modify":
        find_and_modify(args.parent, args.component, args.operation, args.target)
    elif args.command == "delete":
        delete_objects(args.names)
    elif args.command == "status":
        get_status()
    elif args.command == "scene":
        get_scene_info()
    elif args.command == "hierarchy":
        get_hierarchy()
    elif args.command == "scripts":
        get_available_scripts()
    elif args.command == "list":
        list_primitives()
    else:
        parser.print_help()
