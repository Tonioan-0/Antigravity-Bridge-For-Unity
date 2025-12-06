#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Antigravity Bridge - AI Demo Scene Builder
Questo script mostra come un AI puo costruire una scena di gioco completa.

Crea:
- Un pavimento
- 4 muri
- Un player con Rigidbody
- 5 sfere collezionabili con suono
- Script C# per il suono al tocco
"""

import sys
import os
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from unity_bridge import *
import time

print("=" * 60)
print("  🎮 ANTIGRAVITY AI SCENE BUILDER")
print("=" * 60)
print()

# ========================================
# STEP 1: Pulizia scena esistente
# ========================================
print("🧹 Step 1: Pulizia scena...")
delete_objects(["Floor", "Player", "WallNorth", "WallSouth", "WallEast", "WallWest"])
delete_objects(["Sphere1", "Sphere2", "Sphere3", "Sphere4", "Sphere5"])
time.sleep(0.5)

# ========================================
# STEP 2: Creazione pavimento
# ========================================
print("🟫 Step 2: Creazione pavimento...")
create_primitive("cube", "Floor", 
    x=0, y=-0.5, z=0, 
    scale_x=20, scale_y=1, scale_z=20)
modify_material(["Floor"], color="#505050")  # Grigio scuro

# ========================================
# STEP 3: Creazione 4 muri
# ========================================
print("🧱 Step 3: Creazione muri...")

# Muro Nord
create_primitive("cube", "WallNorth",
    x=0, y=1.5, z=10,
    scale_x=20, scale_y=4, scale_z=1)
modify_material(["WallNorth"], color="#8B4513")  # Marrone

# Muro Sud  
create_primitive("cube", "WallSouth",
    x=0, y=1.5, z=-10,
    scale_x=20, scale_y=4, scale_z=1)
modify_material(["WallSouth"], color="#8B4513")

# Muro Est
create_primitive("cube", "WallEast",
    x=10, y=1.5, z=0,
    scale_x=1, scale_y=4, scale_z=20)
modify_material(["WallEast"], color="#8B4513")

# Muro Ovest
create_primitive("cube", "WallWest",
    x=-10, y=1.5, z=0,
    scale_x=1, scale_y=4, scale_z=20)
modify_material(["WallWest"], color="#8B4513")

# ========================================
# STEP 4: Creazione Player
# ========================================
print("🧍 Step 4: Creazione player...")
create_primitive("capsule", "Player",
    x=0, y=1, z=0)
modify_material(["Player"], color="#00FF00")  # Verde brillante

# Aggiungi Rigidbody per fisica
send_post_request("/unity/component/add", {
    "objects": ["Player"],
    "component": "Rigidbody"
})

# ========================================
# STEP 5: Creazione sfere collezionabili
# ========================================
print("🔵 Step 5: Creazione sfere...")

sphere_positions = [
    (5, 0.5, 5),
    (-5, 0.5, 5),
    (5, 0.5, -5),
    (-5, 0.5, -5),
    (0, 0.5, 0)
]

for i, pos in enumerate(sphere_positions, 1):
    name = f"Sphere{i}"
    create_primitive("sphere", name,
        x=pos[0], y=pos[1], z=pos[2],
        scale_x=0.8, scale_y=0.8, scale_z=0.8)
    
    # Colore azzurro brillante
    modify_material([name], color="#00BFFF")
    
    # Aggiungi AudioSource per il suono
    send_post_request("/unity/component/add", {
        "objects": [name],
        "component": "AudioSource"
    })

# ========================================
# STEP 6: Creazione script collisione
# ========================================
print("📜 Step 6: Creazione script CollectibleSound...")

script_code = '''using UnityEngine;

public class CollectibleSound : MonoBehaviour
{
    private AudioSource audioSource;
    
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.name == "Player")
        {
            // Genera un beep sintetico
            audioSource.PlayOneShot(CreateBeepClip());
            Debug.Log("Player ha toccato: " + gameObject.name);
        }
    }
    
    AudioClip CreateBeepClip()
    {
        int sampleRate = 44100;
        float frequency = 440f + Random.Range(0, 200);
        float duration = 0.2f;
        
        int length = (int)(sampleRate * duration);
        AudioClip clip = AudioClip.Create("Beep", length, 1, sampleRate, false);
        float[] samples = new float[length];
        
        for (int i = 0; i < length; i++)
        {
            float t = (float)i / sampleRate;
            samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * t) * (1 - t / duration);
        }
        
        clip.SetData(samples, 0);
        return clip;
    }
}
'''

create_script("CollectibleSound", path="Assets/Scripts", methods=["Start", "OnCollisionEnter"])

# ========================================
# STEP 7: Illuminazione
# ========================================
print("💡 Step 7: Configurazione luce...")

# Crea una point light sopra la scena
create_primitive("light", "MainLight",
    x=0, y=10, z=0)
modify_light(["MainLight"], intensity=2.0, color="#FFFFEE")

# ========================================
# STEP 8: Screenshot della scena
# ========================================
print("📸 Step 8: Screenshot della scena...")
time.sleep(0.5)
capture_camera_screenshot("Main Camera", "scene_complete.png", 1920, 1080)

# ========================================
# STEP 9: Query per verificare
# ========================================
print("🔍 Step 9: Verifica scena...")
result = send_get_request("/unity/scene/hierarchy")

print()
print("=" * 60)
print("  ✅ SCENA COMPLETATA!")
print("=" * 60)
print()
print("Oggetti creati:")
print("  - Floor (pavimento 20x20)")
print("  - 4 Muri (WallNorth, WallSouth, WallEast, WallWest)")
print("  - Player (capsula con Rigidbody)")
print("  - 5 Sfere (con AudioSource)")
print("  - MainLight (illuminazione)")
print("  - Script: CollectibleSound.cs")
print()
print("📸 Screenshot salvato in: Screenshots/scene_complete.png")
print()
print("🎮 Per testare:")
print("  1. Apri Unity")
print("  2. Aggiungi CollectibleSound alle sfere")
print("  3. Entra in Play Mode")
print("  4. Muovi il Player verso le sfere")
print()
