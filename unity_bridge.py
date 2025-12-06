#!/usr/bin/env python3
"""
Antigravity Unity Bridge - Enhanced Python Client with Primitives Support
This script allows Antigravity to send commands to Unity via HTTP REST API
Includes built-in support for Unity primitives with default components
"""

import requests
import json
import sys

# Port range to try when discovering Unity server
UNITY_DEFAULT_PORTS = [8080, 8081, 8082, 8083, 8084, 8085, 8086, 8087, 8088, 8089, 8090]
UNITY_BASE_URL = "http://localhost:8080"  # Will be updated by discover_server()
UNITY_SERVER_PORT = 8080

def discover_server():
    """Try to find Unity server by testing ports 8080-8090"""
    global UNITY_BASE_URL, UNITY_SERVER_PORT
    
    for port in UNITY_DEFAULT_PORTS:
        try:
            url = f"http://localhost:{port}/unity/health"
            response = requests.get(url, timeout=0.5)
            if response.status_code == 200:
                UNITY_BASE_URL = f"http://localhost:{port}"
                UNITY_SERVER_PORT = port
                return port
        except:
            continue
    return None

def ensure_server_connection():
    """Verify Unity server is reachable, with helpful error messages"""
    try:
        response = requests.get(f"{UNITY_BASE_URL}/unity/health", timeout=2)
        return True
    except requests.exceptions.ConnectionError:
        # Try to discover on other ports
        port = discover_server()
        if port:
            return True
        print("╔══════════════════════════════════════════════════════════════╗")
        print("║  ✗ Cannot connect to Unity - Server not running              ║")
        print("║                                                              ║")
        print("║  Solutions:                                                  ║")
        print("║  1. Open Unity Editor                                        ║")
        print("║  2. Go to Window > Antigravity Bridge > Start Server        ║")
        print("║  3. Or enable auto-start in server settings                 ║")
        print("╚══════════════════════════════════════════════════════════════╝")
        return False
    except requests.exceptions.Timeout:
        print("╔══════════════════════════════════════════════════════════════╗")
        print("║  ✗ Unity server timeout - Unity may be busy                  ║")
        print("║                                                              ║")
        print("║  Possible causes:                                            ║")
        print("║  1. Unity is compiling scripts                              ║")
        print("║  2. Unity is in Play mode and frozen                        ║")
        print("║  3. Heavy scene loading                                      ║")
        print("║                                                              ║")
        print("║  Try again in a few seconds                                  ║")
        print("╚══════════════════════════════════════════════════════════════╝")
        return False

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

# Color presets (RGB values 0-1)
COLOR_PRESETS = {
    "white": (1, 1, 1),
    "black": (0, 0, 0),
    "red": (1, 0, 0),
    "green": (0, 1, 0),
    "blue": (0, 0, 1),
    "yellow": (1, 1, 0),
    "cyan": (0, 1, 1),
    "magenta": (1, 0, 1),
    "gray": (0.5, 0.5, 0.5),
    "grey": (0.5, 0.5, 0.5),
    "orange": (1, 0.5, 0),
    "purple": (0.5, 0, 1),
    "pink": (1, 0.75, 0.8),
    "brown": (0.6, 0.3, 0),
    "lime": (0.5, 1, 0),
    "navy": (0, 0, 0.5),
    "teal": (0, 0.5, 0.5),
    "maroon": (0.5, 0, 0),
    "olive": (0.5, 0.5, 0),
    "silver": (0.75, 0.75, 0.75),
    "gold": (1, 0.84, 0),
}

def parse_color(color_input):
    """
    Parse color from various formats:
    - Hex string: "#FF0000", "#ff0000", "FF0000"
    - Named color: "red", "blue", "green"
    - RGB tuple: (1.0, 0.5, 0.0)
    - RGB dict: {"r": 1.0, "g": 0.5, "b": 0.0}
    
    Returns: tuple (r, g, b) with values 0-1, or None if invalid
    """
    if color_input is None:
        return None
    
    # Handle dict input
    if isinstance(color_input, dict):
        return (color_input.get("r", 0), color_input.get("g", 0), color_input.get("b", 0))
    
    # Handle tuple/list input
    if isinstance(color_input, (tuple, list)):
        if len(color_input) >= 3:
            return (float(color_input[0]), float(color_input[1]), float(color_input[2]))
        return None
    
    # Handle string input
    if isinstance(color_input, str):
        color_str = color_input.strip()
        
        # Check for hex color
        if color_str.startswith("#"):
            color_str = color_str[1:]
        
        # Try to parse as hex (6 characters = RRGGBB)
        if len(color_str) == 6:
            try:
                r = int(color_str[0:2], 16) / 255.0
                g = int(color_str[2:4], 16) / 255.0
                b = int(color_str[4:6], 16) / 255.0
                return (r, g, b)
            except ValueError:
                pass  # Not a valid hex, try as named color
        
        # Try to parse as hex (3 characters = RGB shorthand)
        if len(color_str) == 3:
            try:
                r = int(color_str[0] * 2, 16) / 255.0
                g = int(color_str[1] * 2, 16) / 255.0
                b = int(color_str[2] * 2, 16) / 255.0
                return (r, g, b)
            except ValueError:
                pass  # Not a valid hex, try as named color
        
        # Check named colors
        if color_str.lower() in COLOR_PRESETS:
            return COLOR_PRESETS[color_str.lower()]
    
    return None

def print_json(data):
    """Pretty print JSON response"""
    print(json.dumps(data, indent=2))


def send_get_request(endpoint):
    """Send GET request to Unity with automatic port discovery"""
    global UNITY_BASE_URL
    url = f"{UNITY_BASE_URL}{endpoint}"
    try:
        response = requests.get(url, timeout=5)
        print(f"✓ Status: {response.status_code}")
        result = response.json()
        print_json(result)
        return result
    except requests.exceptions.Timeout:
        print("╔══════════════════════════════════════════════════════════════╗")
        print("║  ✗ Request timeout - Unity may be busy                       ║")
        print("║  (compiling, loading scene, or in Play mode)                 ║")
        print("╚══════════════════════════════════════════════════════════════╝")
        return None
    except requests.exceptions.ConnectionError:
        # Try other ports
        port = discover_server()
        if port:
            print(f"✓ Found Unity server on port {port}")
            url = f"{UNITY_BASE_URL}{endpoint}"
            try:
                response = requests.get(url, timeout=5)
                print(f"✓ Status: {response.status_code}")
                result = response.json()
                print_json(result)
                return result
            except:
                pass
        print("╔══════════════════════════════════════════════════════════════╗")
        print("║  ✗ Cannot connect to Unity server                            ║")
        print("║                                                              ║")
        print("║  Solutions:                                                  ║")
        print("║  1. Make sure Unity Editor is open                          ║")
        print("║  2. Server auto-starts on load (check console for port)     ║")
        print("║  3. Or go to Window > Antigravity Bridge > Start Server     ║")
        print("╚══════════════════════════════════════════════════════════════╝")
        return None
    except json.JSONDecodeError:
        print("✗ Error: Invalid response from server (not JSON)")
        print("  The server may be recompiling. Try again in a moment.")
        return None
    except Exception as e:
        print(f"✗ Error: {e}")
        return None

def send_post_request(endpoint, data):
    """Send POST request to Unity with automatic port discovery"""
    global UNITY_BASE_URL
    url = f"{UNITY_BASE_URL}{endpoint}"
    headers = {"Content-Type": "application/json"}
    try:
        response = requests.post(url, json=data, headers=headers, timeout=10)
        print(f"✓ Status: {response.status_code}")
        result = response.json()
        print_json(result)
        return result
    except requests.exceptions.Timeout:
        print("╔══════════════════════════════════════════════════════════════╗")
        print("║  ✗ Request timeout - Unity may be busy                       ║")
        print("║  (compiling, loading scene, or in Play mode)                 ║")
        print("╚══════════════════════════════════════════════════════════════╝")
        return None
    except requests.exceptions.ConnectionError:
        # Try other ports
        port = discover_server()
        if port:
            print(f"✓ Found Unity server on port {port}")
            url = f"{UNITY_BASE_URL}{endpoint}"
            try:
                response = requests.post(url, json=data, headers=headers, timeout=10)
                print(f"✓ Status: {response.status_code}")
                result = response.json()
                print_json(result)
                return result
            except:
                pass
        print("╔══════════════════════════════════════════════════════════════╗")
        print("║  ✗ Cannot connect to Unity server                            ║")
        print("║                                                              ║")
        print("║  Solutions:                                                  ║")
        print("║  1. Make sure Unity Editor is open                          ║")
        print("║  2. Server auto-starts on load (check console for port)     ║")
        print("║  3. Or go to Window > Antigravity Bridge > Start Server     ║")
        print("╚══════════════════════════════════════════════════════════════╝")
        return None
    except json.JSONDecodeError:
        print("✗ Error: Invalid response from server (not JSON)")
        print("  The server may be recompiling. Try again in a moment.")
        return None
    except Exception as e:
        print(f"✗ Error: {e}")
        return None

def create_primitive(primitive_type, name, x=0, y=0, z=0, scale_x=1, scale_y=1, scale_z=1, 
                      color_r=None, color_g=None, color_b=None, add_physics=False):
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
        "scale": {"x": scale_x, "y": scale_y, "z": scale_z},
        "components": components
    }
    
    # Add color if specified
    if color_r is not None:
        data["color"] = {
            "r": color_r,
            "g": color_g if color_g is not None else color_r,
            "b": color_b if color_b is not None else color_r,
            "a": 1.0
        }
        print(f"Creating {primitive_type}: {name}")
        print(f"Position: ({x}, {y}, {z})")
        print(f"Scale: ({scale_x}, {scale_y}, {scale_z})")
        print(f"Color: RGB({data['color']['r']}, {data['color']['g']}, {data['color']['b']})")
        print(f"Components: {', '.join(components)}")
    else:
        print(f"Creating {primitive_type}: {name}")
        print(f"Position: ({x}, {y}, {z})")
        print(f"Scale: ({scale_x}, {scale_y}, {scale_z})")
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

# === Play Mode Control ===
def enter_play_mode():
    """Enter Unity Play Mode"""
    return send_post_request("/unity/editor/play", {})

def exit_play_mode():
    """Exit Unity Play Mode (return to Edit Mode)"""
    return send_post_request("/unity/editor/stop", {})

def toggle_pause():
    """Pause/Resume Play Mode"""
    return send_post_request("/unity/editor/pause", {})

def step_frame():
    """Step one frame (while paused in Play Mode)"""
    return send_post_request("/unity/editor/step", {})

# === Light Control ===
def modify_light(object_names, color=None, intensity=None, shadows=None, range_val=None, spot_angle=None):
    """Modify light properties"""
    data = {"objects": object_names if isinstance(object_names, list) else [object_names]}
    if color:
        c = parse_color(color)
        if c:
            data["color"] = {"r": c[0], "g": c[1], "b": c[2], "a": 1}
    if intensity is not None:
        data["intensity"] = intensity
    if shadows:
        data["shadows"] = shadows
    if range_val is not None:
        data["range"] = range_val
    if spot_angle is not None:
        data["spotAngle"] = spot_angle
    return send_post_request("/unity/light/modify", data)

# === Material Control ===
def modify_material(object_names, color=None, metallic=None, smoothness=None, emission=None):
    """Modify material properties"""
    data = {"objects": object_names if isinstance(object_names, list) else [object_names]}
    if color:
        c = parse_color(color)
        if c:
            data["color"] = {"r": c[0], "g": c[1], "b": c[2], "a": 1}
    if metallic is not None:
        data["metallic"] = metallic
    if smoothness is not None:
        data["smoothness"] = smoothness
    if emission:
        e = parse_color(emission)
        if e:
            data["emission"] = {"r": e[0], "g": e[1], "b": e[2], "a": 1}
    return send_post_request("/unity/material/modify", data)

def assign_material(object_names, material_path):
    """Assign existing material from Assets"""
    data = {
        "objects": object_names if isinstance(object_names, list) else [object_names],
        "materialPath": material_path
    }
    return send_post_request("/unity/material/assign", data)

# === Audio Control ===
def play_audio(object_name, clip_path=None, volume=1.0, pitch=1.0, loop=False):
    """Play audio on object"""
    data = {"objectName": object_name, "volume": volume, "pitch": pitch, "loop": loop}
    if clip_path:
        data["clipPath"] = clip_path
    return send_post_request("/unity/audio/play", data)

def stop_audio(object_name):
    """Stop audio on object"""
    return send_post_request("/unity/audio/stop", {"objectName": object_name})

def modify_audio(object_names, volume=None, pitch=None, spatial_blend=None, loop=None):
    """Modify AudioSource properties"""
    data = {"objects": object_names if isinstance(object_names, list) else [object_names]}
    if volume is not None:
        data["volume"] = volume
    if pitch is not None:
        data["pitch"] = pitch
    if spatial_blend is not None:
        data["spatialBlend"] = spatial_blend
    if loop is not None:
        data["loop"] = loop
    return send_post_request("/unity/audio/modify", data)

# === Tag/Layer Control ===
def create_tag(tag_name):
    """Create a new tag"""
    return send_post_request("/unity/tag/create", {"name": tag_name})

def assign_tag(object_names, tag):
    """Assign tag to objects"""
    data = {
        "objects": object_names if isinstance(object_names, list) else [object_names],
        "tag": tag
    }
    return send_post_request("/unity/tag/assign", data)

def get_tags():
    """Get all available tags"""
    return send_get_request("/unity/tag/list")

def assign_layer(object_names, layer):
    """Assign layer to objects (by name or number)"""
    data = {"objects": object_names if isinstance(object_names, list) else [object_names]}
    if isinstance(layer, int):
        data["layer"] = layer
    else:
        data["layerName"] = layer
    return send_post_request("/unity/layer/assign", data)

# === Script Control ===
def create_script(name, path=None, methods=None):
    """Create a new C# script"""
    data = {"name": name}
    if path:
        data["path"] = path
    if methods:
        data["methods"] = methods
    return send_post_request("/unity/script/create", data)

# === Physics Control ===
def physics_simulate(seconds=1.0, step_size=None):
    """Simulate physics for specified duration"""
    data = {"seconds": seconds}
    if step_size:
        data["stepSize"] = step_size
    return send_post_request("/unity/physics/simulate", data)

def physics_step(steps=1, delta_time=None):
    """Step physics by N frames"""
    data = {"steps": steps}
    if delta_time:
        data["deltaTime"] = delta_time
    return send_post_request("/unity/physics/step", data)

def physics_raycast(origin, direction, max_distance=None):
    """Cast a ray and return hit info"""
    data = {
        "origin": {"x": origin[0], "y": origin[1], "z": origin[2]},
        "direction": {"x": direction[0], "y": direction[1], "z": direction[2]}
    }
    if max_distance:
        data["maxDistance"] = max_distance
    return send_post_request("/unity/physics/raycast", data)

def set_gravity(x, y, z):
    """Set physics gravity"""
    return send_post_request("/unity/physics/gravity", {"gravity": {"x": x, "y": y, "z": z}})

# === Animation Control ===
def play_animation(object_names, clip_name=None, state_name=None, trigger=None):
    """Play animation on objects"""
    data = {"objects": object_names if isinstance(object_names, list) else [object_names]}
    if clip_name:
        data["clipName"] = clip_name
    if state_name:
        data["stateName"] = state_name
    if trigger:
        data["triggerName"] = trigger
    return send_post_request("/unity/animation/play", data)

def stop_animation(object_names, pause=False):
    """Stop animation on objects"""
    data = {
        "objects": object_names if isinstance(object_names, list) else [object_names],
        "pause": pause
    }
    return send_post_request("/unity/animation/stop", data)

def set_animator_param(object_names, param_name, value, param_type=None):
    """Set Animator parameter"""
    data = {
        "objects": object_names if isinstance(object_names, list) else [object_names],
        "parameterName": param_name
    }
    if param_type:
        data["parameterType"] = param_type
    if isinstance(value, bool):
        data["boolValue"] = value
        data["parameterType"] = param_type or "bool"
    elif isinstance(value, int):
        data["intValue"] = value
        data["parameterType"] = param_type or "int"
    elif isinstance(value, float):
        data["floatValue"] = value
        data["parameterType"] = param_type or "float"
    elif value == "trigger":
        data["parameterType"] = "trigger"
    return send_post_request("/unity/animator/set", data)

# === Particle Control ===
def play_particles(object_names, with_children=True):
    """Play particle systems"""
    data = {
        "objects": object_names if isinstance(object_names, list) else [object_names],
        "withChildren": with_children
    }
    return send_post_request("/unity/particles/play", data)

def stop_particles(object_names, clear=False):
    """Stop particle systems"""
    data = {
        "objects": object_names if isinstance(object_names, list) else [object_names],
        "clear": clear
    }
    return send_post_request("/unity/particles/stop", data)

def emit_particles(object_names, count=10):
    """Emit particles instantly"""
    data = {
        "objects": object_names if isinstance(object_names, list) else [object_names],
        "count": count
    }
    return send_post_request("/unity/particles/emit", data)

def modify_particles(object_names, duration=None, lifetime=None, speed=None, size=None, 
                     color=None, max_particles=None, loop=None, emission_rate=None):
    """Modify particle system properties"""
    data = {"objects": object_names if isinstance(object_names, list) else [object_names]}
    if duration is not None:
        data["duration"] = duration
    if lifetime is not None:
        data["startLifetime"] = lifetime
    if speed is not None:
        data["startSpeed"] = speed
    if size is not None:
        data["startSize"] = size
    if color:
        c = parse_color(color)
        if c:
            data["startColor"] = {"r": c[0], "g": c[1], "b": c[2], "a": 1}
    if max_particles is not None:
        data["maxParticles"] = max_particles
    if loop is not None:
        data["loop"] = loop
    if emission_rate is not None:
        data["emissionRate"] = emission_rate
    return send_post_request("/unity/particles/modify", data)

# === Screenshot ===
def capture_screenshot(filename=None, path=None, super_size=1):
    """Capture Game View screenshot"""
    data = {"superSize": super_size}
    if filename:
        data["filename"] = filename
    if path:
        data["path"] = path
    return send_post_request("/unity/screenshot/capture", data)

def capture_camera_screenshot(camera_name=None, filename=None, width=1920, height=1080):
    """Capture from specific camera"""
    data = {"width": width, "height": height}
    if camera_name:
        data["cameraName"] = camera_name
    if filename:
        data["filename"] = filename
    return send_post_request("/unity/screenshot/camera", data)

def capture_scene_view(filename=None, width=None, height=None):
    """Capture Scene View screenshot"""
    data = {}
    if filename:
        data["filename"] = filename
    if width:
        data["width"] = width
    if height:
        data["height"] = height
    return send_post_request("/unity/screenshot/scene", data)

# Color presets (moved here for module-level access)
COLOR_PRESETS = {
    "white": (1, 1, 1),
    "black": (0, 0, 0),
    "red": (1, 0, 0),
    "green": (0, 1, 0),
    "blue": (0, 0, 1),
    "yellow": (1, 1, 0),
    "cyan": (0, 1, 1),
    "magenta": (1, 0, 1),
    "gray": (0.5, 0.5, 0.5),
    "grey": (0.5, 0.5, 0.5),
    "orange": (1, 0.5, 0),
    "purple": (0.5, 0, 1),
}

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
    prim_parser.add_argument("--sx", type=float, default=1, help="X scale")
    prim_parser.add_argument("--sy", type=float, default=1, help="Y scale")
    prim_parser.add_argument("--sz", type=float, default=1, help="Z scale")
    prim_parser.add_argument("--cr", type=float, help="Red color (0-1)")
    prim_parser.add_argument("--cg", type=float, help="Green color (0-1)")
    prim_parser.add_argument("--cb", type=float, help="Blue color (0-1)")
    prim_parser.add_argument("--color", type=str, help="Color name: white, red, green, blue, black, yellow")
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
    
    # Light command
    light_parser = subparsers.add_parser("light", help="Modify light properties")
    light_parser.add_argument("objects", nargs="+", help="Light object names")
    light_parser.add_argument("--color", help="Color name or preset")
    light_parser.add_argument("--intensity", type=float, help="Light intensity")
    light_parser.add_argument("--shadows", choices=["none", "hard", "soft"], help="Shadow type")
    light_parser.add_argument("--range", type=float, dest="range_val", help="Light range")
    light_parser.add_argument("--spot-angle", type=float, help="Spot light angle")
    
    # Material command
    mat_parser = subparsers.add_parser("material", help="Modify material properties")
    mat_parser.add_argument("objects", nargs="+", help="Object names")
    mat_parser.add_argument("--color", help="Color name or preset")
    mat_parser.add_argument("--metallic", type=float, help="Metallic value (0-1)")
    mat_parser.add_argument("--smoothness", type=float, help="Smoothness value (0-1)")
    mat_parser.add_argument("--emission", help="Emission color")
    mat_parser.add_argument("--assign", help="Assign material from path (Assets/...)")
    
    # Audio command
    audio_parser = subparsers.add_parser("audio", help="Control audio")
    audio_parser.add_argument("action", choices=["play", "stop", "modify"], help="Audio action")
    audio_parser.add_argument("object", help="Object name")
    audio_parser.add_argument("--clip", help="Audio clip path (Assets/...)")
    audio_parser.add_argument("--volume", type=float, help="Volume (0-1)")
    audio_parser.add_argument("--pitch", type=float, help="Pitch")
    audio_parser.add_argument("--loop", action="store_true", help="Loop audio")
    audio_parser.add_argument("--spatial", type=float, help="Spatial blend (0=2D, 1=3D)")
    
    # Tag command
    tag_parser = subparsers.add_parser("tag", help="Manage tags")
    tag_parser.add_argument("action", choices=["create", "assign", "list"], help="Tag action")
    tag_parser.add_argument("--name", help="Tag name")
    tag_parser.add_argument("--objects", nargs="+", help="Objects to tag")
    
    # Layer command
    layer_parser = subparsers.add_parser("layer", help="Assign layer")
    layer_parser.add_argument("objects", nargs="+", help="Object names")
    layer_parser.add_argument("--layer", required=True, help="Layer name or number")
    
    # Script command
    script_parser = subparsers.add_parser("script", help="Create C# script")
    script_parser.add_argument("name", help="Script name")
    script_parser.add_argument("--path", help="Folder path (Assets/Scripts)")
    script_parser.add_argument("--methods", nargs="+", help="Methods: Start Update FixedUpdate etc.")
    
    # Query commands
    subparsers.add_parser("status", help="Get server status")
    subparsers.add_parser("scene", help="Get scene info")
    subparsers.add_parser("hierarchy", help="Get scene hierarchy")
    subparsers.add_parser("scripts", help="Get available scripts")
    subparsers.add_parser("list", help="List available primitives")
    subparsers.add_parser("tags", help="Get all available tags")
    
    args = parser.parse_args()
    
    if not args.command:
        parser.print_help()
        sys.exit(1)
    
    # Execute command
    if args.command == "primitive":
        # Handle color - supports named colors, hex (#RRGGBB), and RGB values
        color_r, color_g, color_b = None, None, None
        
        if args.color:
            c = parse_color(args.color)
            if c:
                color_r, color_g, color_b = c
        elif args.cr is not None:
            color_r = args.cr
            color_g = args.cg
            color_b = args.cb
        
        create_primitive(args.type, args.name, args.x, args.y, args.z, 
                        args.sx, args.sy, args.sz, color_r, color_g, color_b, args.physics)
    elif args.command == "create":
        create_gameobject(args.name, args.x, args.y, args.z, parent=args.parent)
    elif args.command == "find":
        find_objects(args.component, args.parent, args.tag)
    elif args.command == "modify":
        find_and_modify(args.parent, args.component, args.operation, args.target)
    elif args.command == "delete":
        delete_objects(args.names)
    elif args.command == "light":
        modify_light(args.objects, color=args.color, intensity=args.intensity, 
                     shadows=args.shadows, range_val=args.range_val, spot_angle=getattr(args, 'spot_angle', None))
    elif args.command == "material":
        if args.assign:
            assign_material(args.objects, args.assign)
        else:
            modify_material(args.objects, color=args.color, metallic=args.metallic, 
                          smoothness=args.smoothness, emission=args.emission)
    elif args.command == "audio":
        if args.action == "play":
            play_audio(args.object, clip_path=args.clip, volume=args.volume or 1.0, 
                      pitch=args.pitch or 1.0, loop=args.loop)
        elif args.action == "stop":
            stop_audio(args.object)
        elif args.action == "modify":
            modify_audio(args.object, volume=args.volume, pitch=args.pitch, 
                        spatial_blend=args.spatial, loop=args.loop if args.loop else None)
    elif args.command == "tag":
        if args.action == "create":
            create_tag(args.name)
        elif args.action == "assign":
            assign_tag(args.objects, args.name)
        elif args.action == "list":
            get_tags()
    elif args.command == "layer":
        # Try to parse as int, else use as name
        try:
            layer = int(args.layer)
        except ValueError:
            layer = args.layer
        assign_layer(args.objects, layer)
    elif args.command == "script":
        create_script(args.name, path=args.path, methods=args.methods)
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
    elif args.command == "tags":
        get_tags()
    else:
        parser.print_help()
