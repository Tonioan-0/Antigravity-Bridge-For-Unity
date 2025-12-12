#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Antigravity Bridge - Supermarket Generator
Generates a supermarket scene with:
- Floor and walls
- Shelves with products
- Cash registers with sound triggers
- Player with first-person controls
"""

import sys
import os
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from unity_bridge import *

# Configuration
SUPERMARKET_WIDTH = 30   # X axis
SUPERMARKET_LENGTH = 40  # Z axis
SUPERMARKET_HEIGHT = 6   # Y axis (walls)
WALL_THICKNESS = 0.5

SHELF_WIDTH = 2
SHELF_LENGTH = 6
SHELF_HEIGHT = 2
SHELF_ROWS = 3
SHELF_COLUMNS = 4
SHELF_SPACING_X = 5
SHELF_SPACING_Z = 8

NUM_CASH_REGISTERS = 3
CASH_REGISTER_WIDTH = 2
CASH_REGISTER_HEIGHT = 1.2
CASH_REGISTER_DEPTH = 1

def clear_supermarket():
    """Remove existing supermarket"""
    print("🧹 Clearing old supermarket...")
    delete_objects(["SupermarketRoot"])

def create_structure():
    """Create floor and walls"""
    print("🏗️ Creating structure...")
    
    # Root container
    create_primitive("empty", "SupermarketRoot", x=0, y=0, z=0)
    
    # Floor - Gray tile color
    create_primitive("plane", "Floor", parent="SupermarketRoot",
                    x=0, y=0, z=0,
                    sx=SUPERMARKET_WIDTH/10, sy=1, sz=SUPERMARKET_LENGTH/10)
    modify_material(["Floor"], color="#CCCCCC", metallic=0.1, smoothness=0.8)
    
    # Walls container
    create_primitive("empty", "Walls", parent="SupermarketRoot", x=0, y=0, z=0)
    
    # Back wall (far Z)
    create_primitive("cube", "WallBack", parent="Walls",
                    x=0, y=SUPERMARKET_HEIGHT/2, z=SUPERMARKET_LENGTH/2,
                    sx=SUPERMARKET_WIDTH, sy=SUPERMARKET_HEIGHT, sz=WALL_THICKNESS)
    modify_material(["WallBack"], color="#E8E8E8")
    
    # Front wall (near Z) - with entrance gap
    create_primitive("cube", "WallFrontLeft", parent="Walls",
                    x=-SUPERMARKET_WIDTH/4 - 2, y=SUPERMARKET_HEIGHT/2, z=-SUPERMARKET_LENGTH/2,
                    sx=SUPERMARKET_WIDTH/2 - 4, sy=SUPERMARKET_HEIGHT, sz=WALL_THICKNESS)
    modify_material(["WallFrontLeft"], color="#E8E8E8")
    
    create_primitive("cube", "WallFrontRight", parent="Walls",
                    x=SUPERMARKET_WIDTH/4 + 2, y=SUPERMARKET_HEIGHT/2, z=-SUPERMARKET_LENGTH/2,
                    sx=SUPERMARKET_WIDTH/2 - 4, sy=SUPERMARKET_HEIGHT, sz=WALL_THICKNESS)
    modify_material(["WallFrontRight"], color="#E8E8E8")
    
    # Left wall
    create_primitive("cube", "WallLeft", parent="Walls",
                    x=-SUPERMARKET_WIDTH/2, y=SUPERMARKET_HEIGHT/2, z=0,
                    sx=WALL_THICKNESS, sy=SUPERMARKET_HEIGHT, sz=SUPERMARKET_LENGTH)
    modify_material(["WallLeft"], color="#E8E8E8")
    
    # Right wall
    create_primitive("cube", "WallRight", parent="Walls",
                    x=SUPERMARKET_WIDTH/2, y=SUPERMARKET_HEIGHT/2, z=0,
                    sx=WALL_THICKNESS, sy=SUPERMARKET_HEIGHT, sz=SUPERMARKET_LENGTH)
    modify_material(["WallRight"], color="#E8E8E8")

def create_shelves():
    """Create shelving units"""
    print("📦 Creating shelves...")
    
    create_primitive("empty", "Shelves", parent="SupermarketRoot", x=0, y=0, z=0)
    
    # Starting position (offset from center)
    start_x = -((SHELF_COLUMNS - 1) * SHELF_SPACING_X) / 2
    start_z = -SUPERMARKET_LENGTH/2 + 8  # Leave space for entrance
    
    shelf_colors = ["#8B4513", "#A0522D", "#CD853F", "#DEB887"]  # Brown tones
    
    for row in range(SHELF_ROWS):
        for col in range(SHELF_COLUMNS):
            x = start_x + col * SHELF_SPACING_X
            z = start_z + row * SHELF_SPACING_Z
            
            shelf_name = f"Shelf_{row}_{col}"
            
            # Shelf base
            create_primitive("cube", shelf_name, parent="Shelves",
                            x=x, y=SHELF_HEIGHT/2, z=z,
                            sx=SHELF_WIDTH, sy=SHELF_HEIGHT, sz=SHELF_LENGTH)
            
            color = shelf_colors[(row + col) % len(shelf_colors)]
            modify_material([shelf_name], color=color)
            
            # Add some product boxes on top
            for p in range(3):
                product_name = f"Product_{row}_{col}_{p}"
                px = x + (p - 1) * 0.6
                py = SHELF_HEIGHT + 0.25
                pz = z
                
                create_primitive("cube", product_name, parent="Shelves",
                                x=px, y=py, z=pz,
                                sx=0.4, sy=0.5, sz=0.4)
                
                # Random product colors
                product_colors = ["#FF6B6B", "#4ECDC4", "#FFE66D", "#95E1D3", "#F38181"]
                modify_material([product_name], color=product_colors[p % len(product_colors)])

def create_cash_registers():
    """Create cash registers with sound triggers"""
    print("💰 Creating cash registers...")
    
    create_primitive("empty", "CashRegisters", parent="SupermarketRoot", x=0, y=0, z=0)
    
    # Position near front of store
    register_z = -SUPERMARKET_LENGTH/2 + 3
    spacing = CASH_REGISTER_WIDTH * 2
    start_x = -((NUM_CASH_REGISTERS - 1) * spacing) / 2
    
    for i in range(NUM_CASH_REGISTERS):
        x = start_x + i * spacing
        register_name = f"CashRegister_{i}"
        
        # Counter/desk
        create_primitive("cube", register_name, parent="CashRegisters",
                        x=x, y=CASH_REGISTER_HEIGHT/2, z=register_z,
                        sx=CASH_REGISTER_WIDTH, sy=CASH_REGISTER_HEIGHT, sz=CASH_REGISTER_DEPTH)
        modify_material([register_name], color="#404040")
        
        # Register screen (small box on top)
        screen_name = f"Screen_{i}"
        create_primitive("cube", screen_name, parent="CashRegisters",
                        x=x, y=CASH_REGISTER_HEIGHT + 0.2, z=register_z,
                        sx=0.5, sy=0.4, sz=0.3)
        modify_material([screen_name], color="#000000", emission="#00FF00")
        
        # Trigger zone for sound (invisible, larger area in front of register)
        trigger_name = f"CashTrigger_{i}"
        send_post_request("/unity/scene/create", {
            "name": trigger_name,
            "parent": "CashRegisters",
            "position": {"x": x, "y": 1, "z": register_z - 1.5},
            "scale": {"x": 2, "y": 2, "z": 2},
            "components": ["BoxCollider", "AudioSource"]
        })
        
        # Set collider as trigger and add our script
        send_post_request("/unity/component/modify", {
            "object": trigger_name,
            "component": "BoxCollider",
            "properties": {"isTrigger": True}
        })
        
        # Try to add the CashRegisterSound script (requires Unity compilation first)
        send_post_request("/unity/component/add", {
            "object": trigger_name,
            "component": "CashRegisterSound"
        })

def create_player():
    """Create player with first-person camera"""
    print("🧑 Creating player...")
    
    # Player spawn point (at entrance)
    spawn_x = 0
    spawn_y = 1  # Half height of capsule
    spawn_z = -SUPERMARKET_LENGTH/2 + 2
    
    # Create player capsule
    send_post_request("/unity/scene/create", {
        "name": "Player",
        "position": {"x": spawn_x, "y": spawn_y, "z": spawn_z},
        "scale": {"x": 1, "y": 1, "z": 1},
        "components": ["CapsuleCollider", "CharacterController"]
    })
    
    # Set tag to Player
    assign_tag(["Player"], "Player")
    
    # Create camera as child
    send_post_request("/unity/scene/create", {
        "name": "PlayerCamera",
        "parent": "Player",
        "position": {"x": 0, "y": 0.6, "z": 0},  # Eye level
        "components": ["Camera", "AudioListener"]
    })
    
    # Add PlayerController script (requires Unity compilation first)
    send_post_request("/unity/component/add", {
        "object": "Player",
        "component": "PlayerController"
    })
    
    # Disable old main camera if exists
    send_post_request("/unity/scene/modify", {
        "object": "Main Camera",
        "active": False
    })

def create_lighting():
    """Add ceiling lights"""
    print("💡 Adding lights...")
    
    create_primitive("empty", "Lights", parent="SupermarketRoot", x=0, y=0, z=0)
    
    # Grid of ceiling lights
    light_spacing = 10
    for lx in range(-10, 15, light_spacing):
        for lz in range(-15, 20, light_spacing):
            light_name = f"CeilingLight_{lx}_{lz}"
            send_post_request("/unity/scene/create", {
                "name": light_name,
                "parent": "Lights",
                "position": {"x": lx, "y": SUPERMARKET_HEIGHT - 0.5, "z": lz},
                "components": ["Light"]
            })
            modify_light([light_name], color="#FFFAF0", intensity=1.5, range_val=15)

def main():
    print("🛒 Starting Supermarket Generator...")
    print("=" * 50)
    
    if not ensure_server_connection():
        print("❌ Cannot connect to Unity server!")
        print("   Make sure Unity is open and the Antigravity server is running.")
        sys.exit(1)
    
    clear_supermarket()
    create_structure()
    create_shelves()
    create_cash_registers()
    create_player()
    create_lighting()
    
    print("=" * 50)
    print("✅ Supermarket Generation Complete!")
    print("")
    print("📋 Next steps:")
    print("   1. In Unity, go to Edit > Project Settings > Tags and Layers")
    print("   2. Add 'Player' tag if not exists")
    print("   3. Add audio file to Assets (e.g., Assets/Audio/beep.wav)")
    print("   4. Select CashTrigger_X objects and assign the audio clip")
    print("   5. Enter Play Mode and walk with WASD + mouse!")

if __name__ == "__main__":
    main()
